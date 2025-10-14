using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudSave;
using UnityEngine;

// Unity Cloud Save의 Item 클래스와 게임의 Item 클래스 간 이름 충돌 해결
using CloudSaveItem = Unity.Services.CloudSave.Models.Item;

/// <summary>
/// Unity Cloud Save를 사용한 캐릭터 데이터 저장/로드 시스템
/// 
/// 핵심 기능:
/// - 클라우드에 캐릭터 데이터 저장
/// - 클라우드에서 캐릭터 데이터 로드
/// - 캐릭터 존재 여부 확인
/// - 캐릭터 삭제
/// - 에러 처리 및 이벤트 발생
/// 
/// Unity Cloud Save 버전:
/// - Cloud Save 3.2.2+ 호환
/// - 이전 버전과 API 차이 있음
/// 
/// 데이터 구조:
/// - CharacterSaveData를 JSON으로 직렬화
/// - 단일 키로 저장 (CloudSaveKeys.CHARACTER_DATA)
/// - 모든 캐릭터 정보 통합 저장
/// 
/// 이벤트 시스템:
/// - OnCharacterDataLoaded: 로드 완료 시
/// - OnCharacterDataSaved: 저장 완료 시
/// - OnCloudSaveError: 에러 발생 시
/// 
/// 사용 흐름:
/// 1. AuthenticationManager로 인증
/// 2. LoadCharacterDataAsync() 호출
/// 3. 있으면 CharacterSelectionController에서 표시
/// 4. 없으면 새 캐릭터 생성
/// 5. SaveCharacterDataAsync() 주기적 호출 (자동 저장)
/// 
/// 에러 처리:
/// - CloudSaveValidationException: 잘못된 데이터 형식
/// - CloudSaveRateLimitedException: 너무 많은 요청
/// - CloudSaveException: 일반 클라우드 저장 오류
/// - Exception: 기타 오류
/// 
/// 중요 주의사항:
/// - 반드시 AuthenticationManager로 인증 후 사용
/// - 비동기 메서드 (async/await 사용)
/// - 네트워크 연결 필요
/// - 속도 제한 준수 (rate limiting)
/// </summary>
public class CloudSaveManager : MonoBehaviour
{
    private static CloudSaveManager _instance;
    public static CloudSaveManager Instance => _instance;

    // 이벤트: 다른 시스템에 알림용
    public static event Action<CharacterSaveData> OnCharacterDataLoaded;  // 로드 완료
    public static event Action OnCharacterDataSaved;                      // 저장 완료
    public static event Action<string> OnCloudSaveError;                  // 에러 발생

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);  // 씬 전환 시에도 유지
    }

    /// <summary>
    /// 클라우드에서 캐릭터 데이터 로드 (비동기)
    /// 
    /// 처리 과정:
    /// 1. 인증 상태 확인
    /// 2. Cloud Save API 호출
    /// 3. JSON 데이터 역직렬화
    /// 4. CharacterSaveData 객체 반환
    /// 5. 이벤트 발생
    /// 
    /// 사용 시나리오:
    /// - 게임 시작 시
    /// - 캐릭터 선택 화면
    /// - 씬 전환 후 복원
    /// 
    /// Returns:
    ///     CharacterSaveData: 로드된 데이터 (없으면 null)
    ///     
    /// Throws:
    ///     Exception: 인증 안 됨
    ///     CloudSaveValidationException: 잘못된 데이터
    ///     CloudSaveRateLimitedException: 속도 제한 초과
    ///     CloudSaveException: 클라우드 저장 오류
    /// </summary>
    public async Task<CharacterSaveData> LoadCharacterDataAsync()
    {
        try
        {
            // 1. 인증 확인
            if (!AuthenticationManager.IsSignedIn)
            {
                throw new Exception("사용자가 인증되지 않았습니다");
            }

            // 2. 로드할 키 설정
            var keys = new HashSet<string> { CloudSaveKeys.CHARACTER_DATA };

            // 3. Cloud Save 3.2.2+ API 호출
            // CloudSaveItem 별칭 사용 (이름 충돌 방지)
            Dictionary<string, CloudSaveItem> savedData =
                await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            // 4. 데이터 존재 확인
            if (savedData.TryGetValue(CloudSaveKeys.CHARACTER_DATA, out CloudSaveItem item))
            {
                // 5. JSON 역직렬화
                string characterDataJson = item.Value.GetAsString();
                CharacterSaveData data = JsonConvert.DeserializeObject<CharacterSaveData>(characterDataJson);

                Debug.Log($"캐릭터 로드 완료: {data.character.characterName}");

                // 6. 이벤트 발생
                OnCharacterDataLoaded?.Invoke(data);
                return data;
            }

            // 데이터 없음
            Debug.Log("저장된 캐릭터 데이터가 없습니다");
            return null;
        }
        catch (CloudSaveValidationException e)
        {
            Debug.LogError($"유효성 검사 오류: {e.Message}");
            OnCloudSaveError?.Invoke("잘못된 데이터 형식입니다");
            return null;
        }
        catch (CloudSaveRateLimitedException e)
        {
            Debug.LogError($"속도 제한 초과: {e.Message}");
            OnCloudSaveError?.Invoke("요청이 너무 많습니다. 잠시 후 다시 시도하세요.");
            return null;
        }
        catch (CloudSaveException e)
        {
            Debug.LogError($"클라우드 저장 오류: {e.Message}");
            OnCloudSaveError?.Invoke(e.Message);
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"캐릭터 로드 실패: {e.Message}");
            OnCloudSaveError?.Invoke(e.Message);
            return null;
        }
    }

    /// <summary>
    /// 클라우드에 캐릭터 데이터 저장 (비동기)
    /// 
    /// 처리 과정:
    /// 1. 인증 상태 확인
    /// 2. CharacterSaveData를 JSON으로 직렬화
    /// 3. Cloud Save API 호출
    /// 4. 저장 완료 이벤트 발생
    /// 
    /// 사용 시나리오:
    /// - 캐릭터 생성 시
    /// - 자동 저장 (5분마다)
    /// - 수동 저장 (게임 종료 시)
    /// - 레벨업/장비 변경 등 주요 이벤트
    /// 
    /// 주의사항:
    /// - 네트워크 연결 필요
    /// - 속도 제한 고려 (너무 자주 호출 금지)
    /// - 기존 데이터 덮어쓰기 (백업 없음)
    /// 
    /// Parameters:
    ///     data: 저장할 캐릭터 데이터
    ///     
    /// Returns:
    ///     true: 저장 성공
    ///     false: 저장 실패
    ///     
    /// Throws:
    ///     Exception: 인증 안 됨
    ///     CloudSaveValidationException: 잘못된 데이터
    ///     CloudSaveRateLimitedException: 속도 제한 초과
    ///     CloudSaveException: 클라우드 저장 오류
    /// </summary>
    public async Task<bool> SaveCharacterDataAsync(CharacterSaveData data)
    {
        try
        {
            // 1. 인증 확인
            if (!AuthenticationManager.IsSignedIn)
            {
                throw new Exception("사용자가 인증되지 않았습니다");
            }

            // 2. JSON 직렬화 (압축 없음)
            string json = JsonConvert.SerializeObject(data, Formatting.None);

            // 3. 저장 데이터 준비
            var saveData = new Dictionary<string, object>
            {
                { CloudSaveKeys.CHARACTER_DATA, json }
            };

            // 4. Cloud Save 3.2.2+ API 호출
            // SaveAsync()는 기존 데이터를 덮어씀
            await CloudSaveService.Instance.Data.Player.SaveAsync(saveData);

            Debug.Log($"캐릭터 저장 완료: {data.character.characterName}");

            // 5. 이벤트 발생
            OnCharacterDataSaved?.Invoke();
            return true;
        }
        catch (CloudSaveValidationException e)
        {
            Debug.LogError($"유효성 검사 오류: {e.Message}");
            OnCloudSaveError?.Invoke("잘못된 데이터 형식입니다");
            return false;
        }
        catch (CloudSaveRateLimitedException e)
        {
            Debug.LogError($"속도 제한 초과: {e.Message}");
            OnCloudSaveError?.Invoke("요청이 너무 많습니다. 잠시 후 다시 시도하세요.");
            return false;
        }
        catch (CloudSaveException e)
        {
            Debug.LogError($"클라우드 저장 오류: {e.Message}");
            OnCloudSaveError?.Invoke(e.Message);
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"캐릭터 저장 실패: {e.Message}");
            OnCloudSaveError?.Invoke(e.Message);
            return false;
        }
    }

    /// <summary>
    /// 캐릭터 존재 여부 확인 (비동기)
    /// 
    /// 사용 시나리오:
    /// - 캐릭터 선택 화면 진입 시
    /// - 기존 캐릭터 vs 새 캐릭터 생성 분기
    /// 
    /// Returns:
    ///     true: 저장된 캐릭터 있음
    ///     false: 저장된 캐릭터 없음 또는 에러
    ///     
    /// 가벼운 체크:
    /// - 실제 데이터는 로드하지 않음
    /// - 키 존재 여부만 확인
    /// - 빠른 응답
    /// </summary>
    public async Task<bool> HasCharacterAsync()
    {
        try
        {
            var keys = new HashSet<string> { CloudSaveKeys.CHARACTER_DATA };
            Dictionary<string, CloudSaveItem> savedData =
                await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            return savedData.ContainsKey(CloudSaveKeys.CHARACTER_DATA) &&
                   savedData[CloudSaveKeys.CHARACTER_DATA] != null;
        }
        catch (Exception e)
        {
            Debug.LogError($"캐릭터 존재 확인 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 캐릭터 삭제 (비동기)
    /// 
    /// 처리 과정:
    /// 1. 클라우드에서 데이터 영구 삭제
    /// 2. 복구 불가능 (백업 없음)
    /// 
    /// 사용 시나리오:
    /// - 캐릭터 선택 화면에서 삭제 버튼
    /// - 계정 초기화
    /// 
    /// 주의사항:
    /// - 복구 불가능!
    /// - 사용자 확인 필수
    /// - 로컬 데이터도 삭제 권장
    /// 
    /// Returns:
    ///     true: 삭제 성공
    ///     false: 삭제 실패
    ///     
    /// Throws:
    ///     CloudSaveValidationException: 잘못된 요청
    ///     CloudSaveException: 클라우드 저장 오류
    /// </summary>
    public async Task<bool> DeleteCharacterAsync()
    {
        try
        {
            // Use the new API with Models.Data.Player namespace
            await CloudSaveService.Instance.Data.Player.DeleteAsync(
                CloudSaveKeys.CHARACTER_DATA,
                new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions()
            );
            Debug.Log("캐릭터 삭제 완료");
            return true;
        }
        catch (CloudSaveValidationException e)
        {
            Debug.LogError($"유효성 검사 오류: {e.Message}");
            return false;
        }
        catch (CloudSaveException e)
        {
            Debug.LogError($"클라우드 저장 오류: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"캐릭터 삭제 실패: {e.Message}");
            return false;
        }
    }
}
