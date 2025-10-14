using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;

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

    private void Start()
    {
        SetupButtonListeners();
        HideError();
        HideLoading();
    }

    private void OnEnable()
    {
        AuthenticationManager.OnSignInSuccess += HandleSignInSuccess;
        AuthenticationManager.OnSignInFailed += HandleSignInFailed;
    }

    private void OnDisable()
    {
        AuthenticationManager.OnSignInSuccess -= HandleSignInSuccess;
        AuthenticationManager.OnSignInFailed -= HandleSignInFailed;
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
    /// 익명 로그인 (Play Now)
    /// </summary>
    private async void OnPlayNowClicked()
    {
        HideError();
        ShowLoading("연결 중...");
        SetButtonsInteractable(false);

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

        HideError();
        ShowLoading("로그인 중...");
        SetButtonsInteractable(false);

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

        HideError();
        ShowLoading("계정 생성 중...");
        SetButtonsInteractable(false);

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
            ShowError("사용자명을 입력하세요");
            return false;
        }

        if (username.Length < 3)
        {
            ShowError("사용자명은 3자 이상이어야 합니다");
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowError("비밀번호를 입력하세요");
            return false;
        }

        if (password.Length < 6)
        {
            ShowError("비밀번호는 6자 이상이어야 합니다");
            return false;
        }

        return true;
    }

    private void HandleSignInSuccess(string playerId)
    {
        Debug.Log($"로그인 성공: {playerId}");
        ShowLoading("캐릭터 데이터 로드 중...");
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
}