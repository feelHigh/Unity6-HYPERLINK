using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using TMPro;

/// <summary>
/// 게임 씬 초기화 및 데이터 로드
/// 
/// 역할:
/// - MainLevel 씬 진입 시 실행
/// - 캐릭터 데이터 로드
/// - 시스템 초기화 조율
/// - 로드 화면 제어 (Optional)
/// 
/// 위치:
/// - MainLevel 씬의 빈 GameObject에 추가
/// 
/// 실행 순서:
/// 1. Awake: 로드 화면 활성화 (Optional)
/// 2. Start: 데이터 로드 및 초기화
/// 3. 성공: 게임 시작
/// 4. 실패: 캐릭터 선택 화면으로 복귀
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("씬 설정")]
    [SerializeField] private string _characterSelectionScene = "CharacterSelection";

    [Header("로딩 UI (Optional)")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TextMeshProUGUI _loadingText;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private async void Start()
    {
        await InitializeGame();
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

            // 2. 시스템 참조 설정
            UpdateLoadingText("시스템 로드 중...");
            if (!InitializeSystemReferences())
            {
                LogError("시스템 참조 설정 실패");
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

            // 4. 플레이어 위치 복원 (Optional)
            UpdateLoadingText("월드 준비 중...");
            RestorePlayerPosition();

            // 5. 초기화 완료
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
    /// 게임 시스템 참조 설정
    /// </summary>
    private bool InitializeSystemReferences()
    {
        if (CharacterDataManager.Instance == null)
        {
            LogError("CharacterDataManager를 찾을 수 없습니다");
            return false;
        }

        CharacterDataManager.Instance.InitializeSystemReferences();

        // 시스템 참조 확인
        var player = FindFirstObjectByType<PlayerCharacter>();
        var exp = FindFirstObjectByType<ExperienceManager>();
        var equip = FindFirstObjectByType<EquipmentManager>();

        if (player == null || exp == null || equip == null)
        {
            LogError("필수 시스템을 찾을 수 없습니다");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 플레이어 위치 복원
    /// </summary>
    private void RestorePlayerPosition()
    {
        var characterData = CharacterDataManager.Instance.CurrentCharacterData;
        if (characterData == null || characterData.position == null)
            return;

        var player = FindFirstObjectByType<PlayerCharacter>();
        if (player != null)
        {
            Vector3 savedPosition = new Vector3(
                characterData.position.x,
                characterData.position.y,
                characterData.position.z
            );

            // 위치가 유효한지 확인
            if (savedPosition != Vector3.zero)
            {
                player.transform.position = savedPosition;
                Log($"플레이어 위치 복원: {savedPosition}");
            }
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