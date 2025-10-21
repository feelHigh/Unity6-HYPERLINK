using UnityEngine;
using System.Threading.Tasks;

/// 시스템 분류: 데이터 관리 시스템
/// 
/// 의존성: CloudSaveManager, PlayerCharacter, ExperienceManager, EquipmentManager
/// 피의존성: GameInitializer, UI 시스템
/// 
/// 핵심 기능: 캐릭터 데이터의 중앙 관리 및 Cloud Save 연동
/// 
/// 기능:
/// - 데이터 로드: 클라우드에서 캐릭터 데이터 가져와 각 시스템에 분배
/// - 데이터 저장: 모든 시스템의 현재 상태 수집하여 클라우드에 저장
/// - 자동 저장: 5분마다 자동으로 진행 상황 저장
/// - 플레이 시간: 세션 시간 추적 및 누적
/// - 시스템 조율: 여러 시스템 간 데이터 흐름 관리
/// 
/// 주의사항:
/// - 싱글톤 패턴 사용, 씬 전환 시에도 유지
/// - 데이터 로드 순서 중요: Experience → Character → Equipment
/// - 게임 씬 로드 후 InitializeSystemReferences 호출 필수
/// - ApplicationQuit에서 async 메서드 사용 시 완료 보장 안 됨

public class CharacterDataManager : MonoBehaviour
{
    private static CharacterDataManager _instance;
    public static CharacterDataManager Instance => _instance;

    [Header("자동 저장 설정")]
    [SerializeField] private float _autoSaveInterval = 300f; // 5분

    private float _autoSaveTimer = 0f;
    private float _sessionStartTime;
    private long _totalPlayTimeSeconds;

    // 현재 로드된 데이터
    private CharacterSaveData _currentCharacterData;

    // 시스템 참조
    private PlayerCharacter _playerCharacter;
    private ExperienceManager _experienceManager;
    private EquipmentManager _equipmentManager;

    public CharacterSaveData CurrentCharacterData => _currentCharacterData;
    public bool IsDataLoaded => _currentCharacterData != null;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _sessionStartTime = Time.time;
    }

    private void Update()
    {
        // 자동 저장 타이머
        if (IsDataLoaded)
        {
            _autoSaveTimer += Time.deltaTime;

            if (_autoSaveTimer >= _autoSaveInterval)
            {
                _autoSaveTimer = 0f;
                _ = AutoSave();
            }
        }
    }

    /// 게임 시스템 참조 설정
    /// GameManager에서 게임 씬 로드 후 호출
    public void InitializeSystemReferences()
    {
        _playerCharacter = FindFirstObjectByType<PlayerCharacter>();
        _experienceManager = FindFirstObjectByType<ExperienceManager>();
        _equipmentManager = FindFirstObjectByType<EquipmentManager>();

        if (_playerCharacter == null)
            Debug.LogError("PlayerCharacter를 찾을 수 없습니다");

        if (_experienceManager == null)
            Debug.LogError("ExperienceManager를 찾을 수 없습니다");

        if (_equipmentManager == null)
            Debug.LogError("EquipmentManager를 찾을 수 없습니다");
    }

    /// 클라우드에서 캐릭터 데이터 로드 및 적용
    /// 
    /// 호출 시점: 게임 씬 진입 시 (캐릭터 선택 후)
    /// 
    /// 처리 순서:
    /// 1. CloudSaveManager에서 데이터 로드
    /// 2. 시스템 참조 확인
    /// 3. 각 시스템에 데이터 적용 (순서 중요)
    /// 4. 플레이 시간 초기화
    public async Task<bool> LoadCharacterData()
    {
        Debug.Log("캐릭터 데이터 로드 시작");

        // 1. 클라우드에서 로드
        _currentCharacterData = await CloudSaveManager.Instance.LoadCharacterDataAsync();

        if (_currentCharacterData == null)
        {
            Debug.LogError("캐릭터 데이터 로드 실패");
            return false;
        }

        // 2. 시스템 참조 확인
        if (_playerCharacter == null || _experienceManager == null)
        {
            InitializeSystemReferences();
        }

        // 3. 각 시스템에 데이터 적용 (순서 중요)
        ApplyDataToSystems(_currentCharacterData);

        // 4. 플레이 시간 초기화
        _totalPlayTimeSeconds = _currentCharacterData.metadata.playTimeSeconds;
        _sessionStartTime = Time.time;

        Debug.Log($"캐릭터 로드 완료: {_currentCharacterData.character.characterName}, " +
                  $"레벨 {_currentCharacterData.character.level}");
        return true;
    }

    /// 로드된 데이터를 각 시스템에 적용
    /// 
    /// 적용 순서 중요:
    /// 1. ExperienceManager: 레벨, 경험치
    /// 2. PlayerCharacter: 스탯, 체력 마나, 스킬
    /// 3. EquipmentManager: 장비
    private void ApplyDataToSystems(CharacterSaveData data)
    {
        // 경험치 시스템
        if (_experienceManager != null)
        {
            _experienceManager.LoadFromSaveData(data);
        }

        // 캐릭터 시스템
        if (_playerCharacter != null)
        {
            _playerCharacter.LoadFromSaveData(data);
        }

        // 장비 시스템
        if (_equipmentManager != null)
        {
            _equipmentManager.LoadFromSaveData(data);
        }
    }

    /// 현재 게임 상태를 수집하여 클라우드에 저장
    /// 
    /// 호출 시점:
    /// - 자동 저장 (5분마다)
    /// - 수동 저장 (게임 종료, 씬 전환)
    /// - 중요 이벤트 (레벨업, 장비 변경)
    public async Task<bool> CollectAndSaveData()
    {
        if (!IsDataLoaded)
        {
            Debug.LogWarning("저장할 데이터가 없습니다");
            return false;
        }

        Debug.Log("캐릭터 데이터 수집 및 저장 시작");

        // 1. 메타데이터 업데이트
        UpdateMetadata();

        // 2. 각 시스템에서 데이터 수집
        CollectDataFromSystems();

        // 3. 클라우드에 저장
        bool success = await CloudSaveManager.Instance.SaveCharacterDataAsync(_currentCharacterData);

        if (success)
        {
            Debug.Log("캐릭터 데이터 저장 완료");
        }
        else
        {
            Debug.LogError("캐릭터 데이터 저장 실패");
        }

        return success;
    }

    /// 각 시스템에서 현재 상태 수집
    /// 
    /// 수집 항목:
    /// - 경험치 레벨
    /// - 캐릭터 스탯, 체력 마나, 스킬
    /// - 장비
    /// - 위치 정보 (씬 이름, 좌표)
    private void CollectDataFromSystems()
    {
        // 경험치 레벨
        if (_experienceManager != null)
        {
            _experienceManager.SaveToData(_currentCharacterData);
        }

        // 캐릭터 상태
        if (_playerCharacter != null)
        {
            _playerCharacter.SaveToData(_currentCharacterData);
        }

        // 장비
        if (_equipmentManager != null)
        {
            _equipmentManager.SaveToData(_currentCharacterData);
        }

        // 위치 정보 (씬 이름 + 좌표)
        if (_playerCharacter != null)
        {
            _currentCharacterData.position.scene =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            Transform playerTransform = _playerCharacter.transform;
            _currentCharacterData.position.x = playerTransform.position.x;
            _currentCharacterData.position.y = playerTransform.position.y;
            _currentCharacterData.position.z = playerTransform.position.z;

            Debug.Log($"위치 저장: 씬={_currentCharacterData.position.scene}, " +
                     $"좌표=({_currentCharacterData.position.x:F2}, " +
                     $"{_currentCharacterData.position.y:F2}, " +
                     $"{_currentCharacterData.position.z:F2})");
        }
    }

    /// 메타데이터 업데이트
    /// 플레이 시간 및 최종 플레이 시각 갱신
    private void UpdateMetadata()
    {
        // 현재 세션 플레이 시간 계산
        float sessionTime = Time.time - _sessionStartTime;
        _totalPlayTimeSeconds += (long)sessionTime;
        _sessionStartTime = Time.time;

        // 메타데이터 업데이트
        _currentCharacterData.metadata.lastPlayed = System.DateTime.UtcNow.ToString("o");
        _currentCharacterData.metadata.playTimeSeconds = _totalPlayTimeSeconds;
    }

    /// 자동 저장 실행
    private async Task AutoSave()
    {
        Debug.Log("자동 저장 실행");
        await CollectAndSaveData();
    }

    /// 게임 종료 시 최종 저장
    private async void OnApplicationQuit()
    {
        if (IsDataLoaded)
        {
            Debug.Log("게임 종료 - 최종 저장 실행");
            await CollectAndSaveData();
        }
    }

    /// 현재 캐릭터 이름 반환
    public string GetCharacterName()
    {
        return _currentCharacterData?.character.characterName ?? "Unknown";
    }
}
