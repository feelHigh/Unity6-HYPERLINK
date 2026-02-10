using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using TMPro;

/// <summary>
/// 게임 씬 초기화 및 데이터 로드 (최종 수정)
/// 
/// 최종 변경사항:
/// - PlayerSpawner를 맵 생성 완료 후에 호출
/// - 플레이어가 완성된 맵에 스폰되도록 보장
/// - PlayerInitializationManager는 플레이어 스폰 후 데이터 로드
/// 
/// 초기화 순서:
/// 1. Unity Services 확인
/// 2. 시스템 검증
/// 3. 맵 생성 (Generator 씬인 경우)
/// 4. 캐릭터 데이터 로드
/// 5. 퀘스트 초기화
/// 6. 플레이어 스폰 ← 여기서 스폰!
/// 7. 플레이어 초기화 (데이터 적용)
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("씬 설정")]
    [SerializeField] private string _characterSelectionScene = "CharacterSelectionScene";

    [Header("게임 씬 목록")]
    [SerializeField]
    private string[] _gameScenes = new string[]
    {
        "Tutorial",
        "Billage",
        "Act1_1",
        "Act1_2",
        "BossStage",
    };

    [Header("퀘스트 설정")]
    [SerializeField] private string[] _autoStartQuestIDs = new string[] { "tutorial_complete" };

    [Header("로딩 UI")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TextMeshProUGUI _loadingText;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private void Awake()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private async void Start()
    {
        await InitializeGame();
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private async Task InitializeGame()
    {
        UpdateLoadingText("게임 초기화 중...");

        try
        {
            // 1. Unity Services 확인
            if (!await EnsureServicesReady())
            {
                LogError("Unity Services 초기화 실패");
                ReturnToCharacterSelection();
                return;
            }

            // 2. 시스템 참조 확인
            UpdateLoadingText("시스템 로드 중...");
            if (!VerifyGameSystems())
            {
                LogError("게임 시스템 확인 실패");
                ReturnToCharacterSelection();
                return;
            }

            // 3. 맵 생성 (Generator 씬인 경우)
            if (MapGeneratorInitializer.Instance != null)
            {
                UpdateLoadingText("맵 생성 중...");
                Log("MapGenerator 초기화 시작");

                bool mapInitSuccess = await MapGeneratorInitializer.Instance.InitializeMapGenerationAsync();

                if (!mapInitSuccess)
                {
                    LogError("맵 생성 실패!");
                    ReturnToCharacterSelection();
                    return;
                }

                Log("맵 생성 완료 - 텔레포트 포인트 등록됨");
            }
            else
            {
                Log("수동 제작 씬 - 맵 생성 건너뜀");
            }

            // 4. 플레이어 스폰 (맵이 완성된 후, 데이터 로드 전!)
            UpdateLoadingText("플레이어 준비 중...");

            if (PlayerSpawner.Instance == null)
            {
                LogError("PlayerSpawner를 찾을 수 없습니다!");
                ReturnToCharacterSelection();
                return;
            }

            Log("플레이어 스폰 시작");
            PlayerSpawner.Instance.SpawnPlayerAtCorrectLocation();
            Log("플레이어 스폰 완료");

            await WaitForPlayerComponents();

            // 5. 캐릭터 데이터 로드 (플레이어가 스폰된 후!)
            UpdateLoadingText("캐릭터 데이터 로드 중...");
            bool loadSuccess = await CharacterDataManager.Instance.LoadCharacterData();

            if (!loadSuccess)
            {
                LogError("캐릭터 데이터 로드 실패");
                ReturnToCharacterSelection();
                return;
            }

            if (GameSessionManager.Instance == null || GameSessionManager.Instance.CurrentCharacterData == null)
            {
                LogError("GameSessionManager 동기화 실패!");
                ReturnToCharacterSelection();
                return;
            }

            Log($"데이터 동기화 확인: {CharacterDataManager.Instance.CurrentCharacterData.character.characterName}");

            // 6. 퀘스트 시스템 초기화
            UpdateLoadingText("퀘스트 시스템 초기화 중...");
            InitializeQuestSystem();

            // 7. PlayerInitializationManager를 통한 플레이어 데이터 적용 확인
            UpdateLoadingText("캐릭터 데이터 적용 중...");

            if (PlayerInitializationManager.Instance == null)
            {
                LogError("PlayerInitializationManager를 찾을 수 없습니다!");
                ReturnToCharacterSelection();
                return;
            }

            bool initSuccess = await PlayerInitializationManager.Instance.StartInitialization();

            if (!initSuccess)
            {
                LogError("Player 초기화 실패");
                ReturnToCharacterSelection();
                return;
            }

            // 8. 초기화 완료
            UpdateLoadingText("게임 시작!");
            Log("게임 초기화 완료!");

            await Task.Delay(500);
            HideLoadingScreen();
        }
        catch (System.Exception e)
        {
            LogError($"초기화 중 예외 발생: {e.Message}");
            ReturnToCharacterSelection();
        }
    }

    private async Task<bool> EnsureServicesReady()
    {
        if (!UGSInitializer.IsInitialized)
        {
            Log("Unity Services 초기화 대기 중...");
            await UGSInitializer.Initialize();
        }

        if (!AuthenticationManager.IsSignedIn)
        {
            LogError("사용자 인증 안 됨");
            return false;
        }

        return true;
    }

    private bool VerifyGameSystems()
    {
        if (PlayerSpawner.Instance == null)
        {
            LogError("PlayerSpawner를 찾을 수 없습니다!");
            return false;
        }

        if (PlayerInitializationManager.Instance == null)
        {
            LogError("PlayerInitializationManager를 찾을 수 없습니다!");
            return false;
        }

        if (ItemSpawner.Instance == null)
        {
            LogError("ItemSpawner를 찾을 수 없습니다!");
            return false;
        }

        if (QuestManager.Instance == null)
        {
            LogError("QuestManager를 찾을 수 없습니다!");
            return false;
        }

        Log("모든 게임 시스템 확인 완료");
        return true;
    }

    private void InitializeQuestSystem()
    {
        if (QuestManager.Instance == null)
        {
            LogError("QuestManager가 없습니다!");
            return;
        }

        if (GameSessionManager.Instance == null)
        {
            LogError("GameSessionManager가 없습니다!");
            return;
        }

        CharacterSaveData saveData = GameSessionManager.Instance.CurrentCharacterData;
        if (saveData != null)
        {
            QuestManager.Instance.Initialize(saveData);
            Log("퀘스트 시스템 초기화 완료");
            StartAutoQuests();
        }
        else
        {
            LogError("GameSessionManager에 캐릭터 데이터가 없습니다!");
        }
    }

    private void StartAutoQuests()
    {
        if (_autoStartQuestIDs == null || _autoStartQuestIDs.Length == 0)
        {
            return;
        }

        foreach (string questID in _autoStartQuestIDs)
        {
            if (string.IsNullOrEmpty(questID))
                continue;

            if (QuestManager.Instance.IsQuestCompleted(questID))
            {
                Log($"퀘스트 이미 완료됨: {questID}");
                continue;
            }

            if (QuestManager.Instance.IsQuestActive(questID))
            {
                Log($"퀘스트 이미 진행 중: {questID}");
                continue;
            }

            QuestManager.Instance.StartQuest(questID);
            Log($"자동 시작 퀘스트 활성화: {questID}");
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        bool isGameScene = System.Array.Exists(_gameScenes, s => s == scene.name);

        if (isGameScene)
        {
            Log($"씬 언로드 감지: {scene.name} - 데이터 저장 시작");
            SaveAllGameData();
        }
    }

    private async void SaveAllGameData()
    {
        if (CharacterDataManager.Instance == null)
        {
            LogWarning("CharacterDataManager를 찾을 수 없습니다. 데이터 저장 건너뜀.");
            return;
        }

        Log("전체 게임 데이터 저장 중...");

        try
        {
            bool saveSuccess = await CharacterDataManager.Instance.CollectAndSaveData();

            if (saveSuccess)
            {
                Log("전체 게임 데이터 저장 완료");
            }
            else
            {
                LogError("게임 데이터 저장 실패!");
            }
        }
        catch (System.Exception e)
        {
            LogError($"데이터 저장 중 예외 발생: {e.Message}");
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SaveQuestProgress();
            Log("퀘스트 데이터 저장 완료");
        }
    }

    private void ReturnToCharacterSelection()
    {
        UpdateLoadingText("캐릭터 선택 화면으로 이동...");
        Log("캐릭터 선택 화면으로 복귀");
        Invoke(nameof(LoadCharacterSelectionScene), 2f);
    }

    private void LoadCharacterSelectionScene()
    {
        SceneManager.LoadScene(_characterSelectionScene);
    }

    /// <summary>
    /// 플레이어 컴포넌트 초기화 대기
    /// </summary>
    private async Task WaitForPlayerComponents()
    {
        float elapsed = 0f;
        float timeout = 5f;

        while (elapsed < timeout)
        {
            var equipMgr = FindFirstObjectByType<EquipmentManager>();
            var skillMgr = FindFirstObjectByType<SkillTreeManager>();

            if (equipMgr != null && skillMgr != null)
            {
                Log("플레이어 컴포넌트 준비 완료");
                return;
            }

            await Task.Delay(50);
            elapsed += 0.05f;
        }

        LogWarning("플레이어 컴포넌트 대기 타임아웃");
    }

    #region Public Methods (레거시)

    public async void ChangeScene(string sceneName)
    {
        Log($"ChangeScene 호출됨: {sceneName}");

        if (CharacterDataManager.Instance != null)
        {
            await CharacterDataManager.Instance.CollectAndSaveData();
        }

        SceneManager.LoadScene(sceneName);
    }

    public void TeleportAndSave(string locationName)
    {
        if (PlayerSpawner.Instance != null)
        {
            PlayerSpawner.Instance.TeleportToLocation(locationName);
        }
    }

    #endregion

    #region UI

    private void UpdateLoadingText(string message)
    {
        if (_loadingText != null)
        {
            _loadingText.text = message;
        }

        Log(message);
    }

    private void HideLoadingScreen()
    {
        if (_loadingPanel != null)
        {
            _loadingPanel.SetActive(false);
        }
    }

    #endregion

    #region 로깅

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[GameInitializer] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[GameInitializer] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogError(string message)
    {
        Debug.LogError($"[GameInitializer] {message}");
    }

    #endregion
}
