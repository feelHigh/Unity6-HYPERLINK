using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// 게임 세션 데이터 관리
/// 
/// 수정 사항:
/// - SaveCurrentCharacterAsync() 메서드 추가 (Task 반환)
/// - AutoSave에서 비동기 저장 완료 대기
/// 
/// 역할:
/// - 현재 플레이 중인 캐릭터 데이터 저장
/// - 자동 저장 (5분마다)
/// - 씬 전환 시 데이터 유지
/// 
/// 사용 흐름:
/// 1. CharacterSelectionController에서 SetCharacterData()
/// 2. GameInitializer에서 데이터 로드
/// 3. 플레이 중 자동 저장
/// </summary>
public class GameSessionManager : MonoBehaviour
{
    private static GameSessionManager _instance;
    public static GameSessionManager Instance => _instance;

    private CharacterSaveData _currentCharacterData;
    public CharacterSaveData CurrentCharacterData => _currentCharacterData;

    private float _autoSaveTimer = 0f;
    private const float AUTO_SAVE_INTERVAL = 300f;  // 5분

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

    private void Update()
    {
        _autoSaveTimer += Time.deltaTime;

        if (_autoSaveTimer >= AUTO_SAVE_INTERVAL)
        {
            _autoSaveTimer = 0f;
            AutoSave();
        }
    }

    /// <summary>
    /// 캐릭터 데이터 설정
    /// CharacterSelectionController에서 호출
    /// </summary>
    public void SetCharacterData(CharacterSaveData data)
    {
        _currentCharacterData = data;
        DebugHelper.Log($"세션 시작: {data.character.characterName}");
    }

    public void UpdateCharacterData(CharacterSaveData data)
    {
        _currentCharacterData = data;
    }

    /// <summary>
    /// 수동 저장 (동기 - fire-and-forget)
    /// 
    /// [주의] 저장 완료를 기다리지 않습니다
    /// 저장 완료를 기다려야 하는 경우 SaveCurrentCharacterAsync()를 사용하세요
    /// </summary>
    public async void SaveCurrentCharacter()
    {
        if (_currentCharacterData != null)
        {
            await CloudSaveManager.Instance.SaveCharacterDataAsync(_currentCharacterData);
        }
    }

    /// <summary>
    /// [NEW] 수동 저장 (비동기)
    /// 
    /// 저장 완료를 기다려야 하는 경우 사용
    /// 예: 씬 전환 전, 앱 종료 전
    /// </summary>
    public async Task<bool> SaveCurrentCharacterAsync()
    {
        if (_currentCharacterData == null)
        {
            DebugHelper.LogWarning("[GameSessionManager] 저장할 캐릭터 데이터가 없습니다");
            return false;
        }

        bool success = await CloudSaveManager.Instance.SaveCharacterDataAsync(_currentCharacterData);

        if (success)
        {
            DebugHelper.Log("[GameSessionManager] 캐릭터 데이터 저장 완료");
        }
        else
        {
            DebugHelper.LogError("[GameSessionManager] 캐릭터 데이터 저장 실패!");
        }

        return success;
    }

    /// <summary>
    /// 자동 저장 (5분마다)
    /// 
    /// [수정] 저장 완료를 기다림
    /// </summary>
    private async void AutoSave()
    {
        if (_currentCharacterData != null)
        {
            UpdatePlayTime();

            // 저장 완료 대기
            bool success = await CloudSaveManager.Instance.SaveCharacterDataAsync(_currentCharacterData);

            if (success)
            {
                DebugHelper.Log("자동 저장 완료");
            }
            else
            {
                DebugHelper.LogError("자동 저장 실패!");
            }
        }
    }

    /// <summary>
    /// 플레이 시간 업데이트
    /// </summary>
    private void UpdatePlayTime()
    {
        _currentCharacterData.metadata.playTimeSeconds += (long)AUTO_SAVE_INTERVAL;
        _currentCharacterData.metadata.lastPlayed = System.DateTime.UtcNow.ToString("o");
    }
}
