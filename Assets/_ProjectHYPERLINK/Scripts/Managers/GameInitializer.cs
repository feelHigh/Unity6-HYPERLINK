using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using TMPro;

/// <summary>
/// 게임 씬 초기화 및 데이터 로드
/// 
/// 변경사항:
/// - PlayerInitializationManager 통합
/// - WaitForPlayerSpawn → PlayerInitializationManager.StartInitialization() 사용
/// - InitializeSystemReferences 로직 → PlayerInitializationManager로 이동
/// - [NEW] 퀘스트 시스템 초기화 통합
/// - 초기화 프로세스 단순화
/// 
/// 역할:
/// - TutorialTestScene 진입 시 실행
/// - 캐릭터 데이터 로드
/// - PlayerInitializationManager를 통한 시스템 초기화 조율
/// - QuestManager 초기화 및 퀘스트 시작
/// - 씬 전환 시 위치 저장
/// - 로드 화면 제어
/// 
/// 위치:
/// - TutorialTestScene의 GameManager GameObject에 추가
/// - PlayerInitializationManager와 함께 사용
/// 
/// 실행 순서:
/// 1. Awake: 씬 전환 이벤트 등록
/// 2. Start: 데이터 로드 및 PlayerInitializationManager 시작
/// 3. [NEW] 퀘스트 시스템 초기화
/// 4. 성공: 게임 시작
/// 5. 실패: 캐릭터 선택 화면으로 복귀
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("씬 설정")]
    [SerializeField] private string _characterSelectionScene = "CharacterSelectionScene";

    [Header("퀘스트 설정")]
    [Tooltip("게임 시작 시 자동으로 시작할 퀘스트 ID 목록")]
    [SerializeField] private string[] _autoStartQuestIDs = new string[] { "tutorial_complete" };

    [Header("로딩 UI (Optional)")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TextMeshProUGUI _loadingText;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private void Awake()
    {
        // 씬 전환 이벤트 등록 (위치 저장용)
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private async void Start()
    {
        await InitializeGame();
    }

    private void OnDestroy()
    {
        // 이벤트 등록 해제
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    /// <summary>
    /// 게임 초기화 메인 프로세스
    /// PlayerInitializationManager를 사용하여 초기화 간소화
    /// </summary>
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

            // 2. 시스템 참조 확인 (PlayerSpawner, EnemySpawner 등)
            UpdateLoadingText("시스템 로드 중...");
            if (!VerifyGameSystems())
            {
                LogError("게임 시스템 확인 실패");
                ReturnToCharacterSelection();
                return;
            }

            // 3. 캐릭터 데이터 로드
            UpdateLoadingText("캐릭터 데이터 로드 중...");
            bool loadSuccess = await CharacterDataManager.Instance.LoadCharacterData();

            if (!loadSuccess)
            {
                LogError("캐릭터 데이터 로드 실패");
                ReturnToCharacterSelection();
                return;
            }

            // 4. 퀘스트 시스템 초기화
            UpdateLoadingText("퀘스트 시스템 초기화 중...");
            InitializeQuestSystem();

            // 5. PlayerInitializationManager를 통한 Player 초기화
            UpdateLoadingText("플레이어 준비 중...");

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

            // 6. 초기화 완료
            UpdateLoadingText("게임 시작!");
            Log("게임 초기화 완료!");

            await Task.Delay(500); // 짧은 딜레이
            HideLoadingScreen();
        }
        catch (System.Exception e)
        {
            LogError($"초기화 중 예외 발생: {e.Message}");
            ReturnToCharacterSelection();
        }
    }

    /// <summary>
    /// Unity Services 준비 확인
    /// </summary>
    private async Task<bool> EnsureServicesReady()
    {
        // UGSInitializer 확인
        if (!UGSInitializer.IsInitialized)
        {
            Log("Unity Services 초기화 대기 중...");
            await UGSInitializer.Initialize();
        }

        // 인증 확인
        if (!AuthenticationManager.IsSignedIn)
        {
            LogError("사용자 인증 안 됨");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 게임 시스템 존재 확인
    /// PlayerSpawner와 EnemySpawner가 씬에 있는지 검증
    /// </summary>
    private bool VerifyGameSystems()
    {
        // PlayerSpawner 확인
        if (PlayerSpawner.Instance == null)
        {
            LogError("PlayerSpawner를 찾을 수 없습니다!");
            return false;
        }

        // PlayerInitializationManager 확인
        if (PlayerInitializationManager.Instance == null)
        {
            LogError("PlayerInitializationManager를 찾을 수 없습니다!");
            return false;
        }

        // EnemySpawner 확인 (선택 사항)
        var enemySpawner = FindFirstObjectByType<EnemySpawner>();
        if (enemySpawner == null)
        {
            Log("EnemySpawner를 찾을 수 없습니다 (선택사항)");
        }

        // ItemSpawner 확인
        if (ItemSpawner.Instance == null)
        {
            LogError("ItemSpawner를 찾을 수 없습니다!");
            return false;
        }

        // QuestManager 확인
        if (QuestManager.Instance == null)
        {
            LogError("QuestManager를 찾을 수 없습니다! QuestManager GameObject를 씬에 추가하세요.");
            return false;
        }

        Log("모든 게임 시스템 확인 완료");
        return true;
    }

    /// <summary>
    /// 퀘스트 시스템 초기화
    /// - 저장된 퀘스트 진행 상황 로드
    /// - 자동 시작 퀘스트 활성화
    /// </summary>
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

        // 저장된 데이터로 퀘스트 시스템 초기화
        CharacterSaveData saveData = GameSessionManager.Instance.CurrentCharacterData;
        if (saveData != null)
        {
            QuestManager.Instance.Initialize(saveData);
            Log("퀘스트 시스템 초기화 완료");

            // 자동 시작 퀘스트 활성화
            StartAutoQuests();
        }
        else
        {
            LogError("캐릭터 저장 데이터를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 자동 시작 퀘스트 활성화
    /// - 이미 완료되지 않은 퀘스트만 시작
    /// - 진행 중이 아닌 퀘스트만 시작
    /// </summary>
    private void StartAutoQuests()
    {
        if (_autoStartQuestIDs == null || _autoStartQuestIDs.Length == 0)
        {
            Log("자동 시작 퀘스트가 설정되지 않았습니다");
            return;
        }

        foreach (string questID in _autoStartQuestIDs)
        {
            if (string.IsNullOrEmpty(questID))
                continue;

            // 이미 완료했거나 진행 중이면 건너뛰기
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

            // 퀘스트 시작
            QuestManager.Instance.StartQuest(questID);
            Log($"자동 시작 퀘스트 활성화: {questID}");
        }
    }

    /// <summary>
    /// 씬 언로드 시 호출 (씬 전환 전)
    /// 플레이어 위치를 자동으로 저장
    /// </summary>
    private void OnSceneUnloaded(Scene scene)
    {
        // 현재 씬이 게임 씬이면 위치 저장
        if (scene.name == "TutorialScene" ||
            scene.name == "ForestScene" ||
            scene.name == "CaveScene" ||
            scene.name == "BossArena")
        {
            SavePlayerPosition();
        }
    }

    /// <summary>
    /// 플레이어 위치 저장
    /// PlayerSpawner의 SavePlayerPosition 호출
    /// </summary>
    private void SavePlayerPosition()
    {
        if (PlayerSpawner.Instance != null)
        {
            PlayerSpawner.Instance.SavePlayerPosition();
            Log("플레이어 위치 저장 완료");
        }
    }

    /// <summary>
    /// 캐릭터 선택 화면으로 복귀
    /// </summary>
    private void ReturnToCharacterSelection()
    {
        UpdateLoadingText("캐릭터 선택 화면으로 이동...");
        Log("캐릭터 선택 화면으로 복귀");

        // 짧은 딜레이 후 씬 전환
        Invoke(nameof(LoadCharacterSelectionScene), 2f);
    }

    private void LoadCharacterSelectionScene()
    {
        SceneManager.LoadScene(_characterSelectionScene);
    }

    #region Public Methods (외부 호출용)

    /// <summary>
    /// 다른 씬으로 전환 (외부 호출용)
    /// 자동으로 위치를 저장하고 씬 로드
    /// </summary>
    public void ChangeScene(string sceneName)
    {
        SavePlayerPosition();
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 특정 위치로 텔레포트 후 저장
    /// </summary>
    public void TeleportAndSave(string locationName)
    {
        if (PlayerSpawner.Instance != null)
        {
            PlayerSpawner.Instance.TeleportToLocation(locationName);
        }
    }

    #endregion

    #region UI 업데이트

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

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[GameInitializer] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GameInitializer] {message}");
    }

    #endregion
}
