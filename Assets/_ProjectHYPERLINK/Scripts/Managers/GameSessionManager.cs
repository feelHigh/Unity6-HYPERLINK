using UnityEngine;

/// <summary>
/// 게임 세션 데이터 관리
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
        Debug.Log($"세션 시작: {data.character.characterName}");
    }

    public void UpdateCharacterData(CharacterSaveData data)
    {
        _currentCharacterData = data;
    }

    /// <summary>
    /// 수동 저장
    /// </summary>
    public async void SaveCurrentCharacter()
    {
        if (_currentCharacterData != null)
        {
            await CloudSaveManager.Instance.SaveCharacterDataAsync(_currentCharacterData);
        }
    }

    /// <summary>
    /// 자동 저장 (5분마다)
    /// </summary>
    private async void AutoSave()
    {
        if (_currentCharacterData != null)
        {
            UpdatePlayTime();
            await CloudSaveManager.Instance.SaveCharacterDataAsync(_currentCharacterData);
            Debug.Log("자동 저장 완료");
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