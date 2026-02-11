using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.Localization.Settings;

/// <summary>
/// 로그인 화면 UI 컨트롤러
/// 
/// 기능:
/// - 익명 로그인 (Play Now)
/// - 사용자명/비밀번호 로그인
/// - 계정 생성
/// - 로딩/에러 피드백
/// - 키보드 단축키 (Tab, Enter)
/// 
/// 씬 흐름:
/// 로그인 → 캐릭터 선택 → 게임
/// </summary>
public class LoginController : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button _playNowButton;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _signUpButton;
    [SerializeField] private TMP_InputField _usernameInput;
    [SerializeField] private TMP_InputField _passwordInput;

    [Header("피드백 UI")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TextMeshProUGUI _loadingText;
    [SerializeField] private GameObject _errorPanel;
    [SerializeField] private TextMeshProUGUI _errorText;

    [Header("씬 설정")]
    [SerializeField] private string _characterSelectionScene = "CharacterSelectionScene";

    [Header("초기화 설정")]
    [SerializeField] private float _initializationTimeout = 5f; // 초기화 최대 대기 시간

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private void Start()
    {
        SetupButtonListeners();
        HideError();
        HideLoading();
    }

    private void OnEnable()
    {
        // 이벤트 구독 - null 체크 추가
        if (AuthenticationManager.Instance != null)
        {
            AuthenticationManager.OnSignInSuccess += HandleSignInSuccess;
            AuthenticationManager.OnSignInFailed += HandleSignInFailed;
            Log("AuthenticationManager 이벤트 구독 완료");
        }
        else
        {
            LogWarning("AuthenticationManager.Instance가 null - 이벤트 구독 건너뜀");
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제 - null 체크 추가
        if (AuthenticationManager.Instance != null)
        {
            AuthenticationManager.OnSignInSuccess -= HandleSignInSuccess;
            AuthenticationManager.OnSignInFailed -= HandleSignInFailed;
        }
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    /// <summary>
    /// 키보드 입력 처리
    /// Tab: 입력 필드 전환
    /// Enter: 로그인 실행
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (_usernameInput.isFocused)
            {
                _passwordInput.Select();
            }
            else if (_passwordInput.isFocused)
            {
                _usernameInput.Select();
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (_usernameInput.isFocused || _passwordInput.isFocused)
            {
                OnLoginClicked();
            }
        }
    }

    private void SetupButtonListeners()
    {
        _playNowButton.onClick.AddListener(OnPlayNowClicked);
        _loginButton.onClick.AddListener(OnLoginClicked);
        _signUpButton.onClick.AddListener(OnSignUpClicked);
    }

    /// <summary>
    /// AuthenticationManager 초기화 대기
    /// 
    /// Returns:
    ///     true: AuthenticationManager 사용 가능
    ///     false: 초기화 실패 또는 타임아웃
    /// </summary>
    private async Task<bool> EnsureAuthenticationManager()
    {
        // 이미 존재하면 즉시 반환
        if (AuthenticationManager.Instance != null)
        {
            Log("AuthenticationManager 사용 가능");
            return true;
        }

        Log("AuthenticationManager 초기화 대기 중...");

        // 초기화 대기 (최대 5초)
        float elapsedTime = 0f;
        while (AuthenticationManager.Instance == null && elapsedTime < _initializationTimeout)
        {
            await Task.Delay(100); // 0.1초마다 체크
            elapsedTime += 0.1f;
        }

        // 초기화 성공 여부 확인
        if (AuthenticationManager.Instance != null)
        {
            Log("AuthenticationManager 초기화 완료!");

            // 이벤트 재구독 (OnEnable에서 실패했을 수 있음)
            AuthenticationManager.OnSignInSuccess += HandleSignInSuccess;
            AuthenticationManager.OnSignInFailed += HandleSignInFailed;

            return true;
        }

        // 타임아웃 발생
        LogError($"AuthenticationManager 초기화 실패 (타임아웃: {_initializationTimeout}초)");
        return false;
    }

    /// <summary>
    /// 익명 로그인 (Play Now)
    /// </summary>
    private async void OnPlayNowClicked()
    {
        string text = LocalizationSettings.StringDatabase.GetLocalizedString(
            "SYSTEM_STATUS",
            "SYSTEM_STATUS_CONNECTING");

        HideError();
        ShowLoading(text);
        SetButtonsInteractable(false);

        // AuthenticationManager 초기화 대기
        if (!await EnsureAuthenticationManager())
        {
            ShowError("시스템 초기화 실패. 게임을 재시작해주세요.");
            HideLoading();
            SetButtonsInteractable(true);
            return;
        }

        bool success = await AuthenticationManager.Instance.SignInAnonymouslyAsync();

        if (!success)
        {
            HideLoading();
            SetButtonsInteractable(true);
        }
    }

    /// <summary>
    /// 사용자명/비밀번호 로그인
    /// </summary>
    private async void OnLoginClicked()
    {
        string username = _usernameInput.text.Trim();
        string password = _passwordInput.text;

        if (!ValidateInput(username, password))
        {
            return;
        }

        string text = LocalizationSettings.StringDatabase.GetLocalizedString(
            "UI_LOGIN",
            "LOGIN_STATUS_LOGGING_IN");

        HideError();
        ShowLoading(text);
        SetButtonsInteractable(false);

        // AuthenticationManager 초기화 대기
        if (!await EnsureAuthenticationManager())
        {
            ShowError("시스템 초기화 실패. 게임을 재시작해주세요.");
            HideLoading();
            SetButtonsInteractable(true);
            return;
        }

        // AuthenticationManager가 확실히 존재함
        bool success = await AuthenticationManager.Instance.SignInWithUsernamePasswordAsync(username, password);

        if (!success)
        {
            HideLoading();
            SetButtonsInteractable(true);
        }
    }

    /// <summary>
    /// 계정 생성
    /// </summary>
    private async void OnSignUpClicked()
    {
        string username = _usernameInput.text.Trim();
        string password = _passwordInput.text;

        if (!ValidateInput(username, password))
        {
            return;
        }

        string text = LocalizationSettings.StringDatabase.GetLocalizedString(
            "UI_LOGIN",
            "LOGIN_STATUS-SIGNING_UP");

        HideError();
        ShowLoading(text);
        SetButtonsInteractable(false);

        // AuthenticationManager 초기화 대기
        if (!await EnsureAuthenticationManager())
        {
            ShowError("시스템 초기화 실패. 게임을 재시작해주세요.");
            HideLoading();
            SetButtonsInteractable(true);
            return;
        }

        // AuthenticationManager가 확실히 존재함
        bool success = await AuthenticationManager.Instance.SignUpWithUsernamePasswordAsync(username, password);

        if (!success)
        {
            HideLoading();
            SetButtonsInteractable(true);
        }
    }

    /// <summary>
    /// 입력 유효성 검사
    /// </summary>
    private bool ValidateInput(string username, string password)
    {
        if (string.IsNullOrEmpty(username))
        {
            string msg = LocalizationSettings.StringDatabase.GetLocalizedString(
                "UI_LOGIN",
                "LOGIN_MSG_EMPTY_USERNAME");

            ShowError(msg);
            return false;
        }

        if (username.Length < 3)
        {
            string msg = LocalizationSettings.StringDatabase.GetLocalizedString(
                "UI_LOGIN",
                "LOGIN_MSG_INVALID_USERNAME");

            ShowError(msg);
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            string msg = LocalizationSettings.StringDatabase.GetLocalizedString(
                "UI_LOGIN",
                "LOGIN_MSG_EMPTY_PASSWORD");

            ShowError(msg);
            return false;
        }

        if (password.Length < 6)
        {
            string msg = LocalizationSettings.StringDatabase.GetLocalizedString(
                "UI_LOGIN",
                "LOGIN_MSG_INVALID_PASSWORD");

            ShowError(msg);
            return false;
        }

        return true;
    }

    private void HandleSignInSuccess(string playerId)
    {
        DebugHelper.Log($"로그인 성공: {playerId}");

        string text = LocalizationSettings.StringDatabase.GetLocalizedString(
            "SYSTEM_STATUS",
            "SYSTEM_STATUS_LOADING_CHARACTER_DATA");

        ShowLoading(text);
        LoadCharacterSelectionScene();
    }

    private void HandleSignInFailed(string error)
    {
        ShowError(error);
    }

    private async void LoadCharacterSelectionScene()
    {
        await Task.Delay(500);
        SceneManager.LoadScene(_characterSelectionScene);
    }

    private void ShowLoading(string message)
    {
        if (_loadingPanel != null)
        {
            _loadingPanel.SetActive(true);
        }

        if (_loadingText != null)
        {
            _loadingText.text = message;
        }
    }

    private void HideLoading()
    {
        if (_loadingPanel != null)
        {
            _loadingPanel.SetActive(false);
        }
    }

    private void ShowError(string message)
    {
        if (_errorPanel != null)
        {
            _errorPanel.SetActive(true);
        }

        if (_errorText != null)
        {
            _errorText.text = message;
        }

        HideErrorAfterDelay();
    }

    private void HideError()
    {
        if (_errorPanel != null)
        {
            _errorPanel.SetActive(false);
        }
    }

    private async void HideErrorAfterDelay()
    {
        await Task.Delay(5000);
        HideError();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        _playNowButton.interactable = interactable;
        _loginButton.interactable = interactable;
        _signUpButton.interactable = interactable;
    }

    #region 로깅

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            DebugHelper.Log($"[LoginController] {message}");
        }
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            DebugHelper.LogWarning($"[LoginController] {message}");
        }
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void LogError(string message)
    {
        DebugHelper.LogError($"[LoginController] {message}");
    }

    #endregion
}
