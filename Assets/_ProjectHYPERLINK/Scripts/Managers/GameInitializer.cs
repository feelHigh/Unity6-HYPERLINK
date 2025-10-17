using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using TMPro;

/// <summary>
/// 게임 씬 초기화 및 데이터 로드
/// 
/// 역할:
/// - TutorialTestScene 진입 시 실행
/// - 캐릭터 데이터 로드
/// - 시스템 초기화 조율
/// - PlayerSpawner와 연동
/// - 씬 전환 시 위치 저장
/// - 로드 화면 제어
/// 
/// 위치:
/// - TutorialTestScene의 GameManager GameObject에 추가
/// 
/// 실행 순서:
/// 1. Awake: 씬 전환 이벤트 등록
/// 2. Start: 데이터 로드 및 초기화
/// 3. 성공: 게임 시작
/// 4. 실패: 캐릭터 선택 화면으로 복귀
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("씬 설정")]
    [SerializeField] private string _characterSelectionScene = "CharacterSelectionScene";

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

            // 4. 플레이어 캐릭터에 데이터 적용 대기
            UpdateLoadingText("플레이어 준비 중...");
            await WaitForPlayerSpawn();

            // 5. 시스템 참조 설정
            if (!InitializeSystemReferences())
            {
                LogError("시스템 참조 설정 실패");
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

        // EnemySpawner 확인 (Optional)
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

        Log("모든 게임 시스템 확인 완료");
        return true;
    }

    /// <summary>
    /// 플레이어 스폰 대기
    /// PlayerSpawner가 플레이어를 생성할 때까지 대기
    /// </summary>
    private async Task WaitForPlayerSpawn()
    {
        int maxAttempts = 50; // 5초 대기 (50 * 100ms)
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            GameObject player = PlayerSpawner.Instance.GetPlayer();
            if (player != null)
            {
                Log("플레이어 스폰 확인");
                return;
            }

            await Task.Delay(100);
            attempts++;
        }

        LogError("플레이어 스폰 타임아웃");
        throw new System.Exception("Player spawn timeout");
    }

    /// <summary>
    /// 게임 시스템 참조 설정
    /// PlayerCharacter에 데이터 적용
    /// </summary>
    private bool InitializeSystemReferences()
    {
        if (CharacterDataManager.Instance == null)
        {
            LogError("CharacterDataManager를 찾을 수 없습니다");
            return false;
        }

        // CharacterDataManager에서 시스템 참조 초기화
        CharacterDataManager.Instance.InitializeSystemReferences();

        // 필수 시스템 확인
        var player = FindFirstObjectByType<PlayerCharacter>();
        var exp = FindFirstObjectByType<ExperienceManager>();
        var equip = FindFirstObjectByType<EquipmentManager>();

        if (player == null)
        {
            LogError("PlayerCharacter를 찾을 수 없습니다");
            return false;
        }

        if (exp == null)
        {
            LogError("ExperienceManager를 찾을 수 없습니다");
            return false;
        }

        if (equip == null)
        {
            LogError("EquipmentManager를 찾을 수 없습니다");
            return false;
        }

        Log("시스템 참조 설정 완료");
        return true;
    }

    /// <summary>
    /// 씬 언로드 시 호출 (씬 전환 전)
    /// 플레이어 위치를 자동으로 저장
    /// </summary>
    private void OnSceneUnloaded(Scene scene)
    {
        // 현재 씬이 게임 씬이면 위치 저장
        if (scene.name == "TutorialTestScene" ||
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

        // 위치 저장 (Optional - 실패 시에도 저장할지 결정)
        // SavePlayerPosition();

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
            // PlayerSpawner.TeleportToLocation이 이미 저장하므로 추가 저장 불필요
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
