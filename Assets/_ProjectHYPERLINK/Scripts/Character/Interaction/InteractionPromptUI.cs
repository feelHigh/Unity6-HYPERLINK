using UnityEngine;
using TMPro;

/// <summary>
/// 상호작용 프롬프트 UI (선택 사항)
/// 
/// 기능:
/// - "Press E to Open Door" 메시지 표시
/// - 상호작용 대상 이름 표시
/// - 페이드 인/아웃 애니메이션
/// 
/// UI 구성:
/// Canvas
/// └─ InteractionPrompt (GameObject)
///     ├─ InteractionPromptUI (Script)
///     ├─ Panel (배경)
///     │   ├─ PromptText (TextMeshProUGUI) - "[E] 문 열기"
///     │   └─ ObjectNameText (TextMeshProUGUI) - "나무 문" (선택)
///     └─ KeyIcon (Image) - 키 아이콘 (선택)
/// 
/// 사용법:
/// 1. Canvas에 Panel GameObject 생성
/// 2. InteractionPromptUI 컴포넌트 추가
/// 3. TextMeshProUGUI 연결
/// 4. PlayerInteractionController가 자동으로 이벤트 발생
/// 
/// 커스터마이징:
/// - 폰트, 색상, 크기 조정
/// - 애니메이션 속도 조정
/// - 위치 변경 (화면 중앙 하단 권장)
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject _promptPanel;
    [SerializeField] private TextMeshProUGUI _promptText;
    [SerializeField] private TextMeshProUGUI _objectNameText; // 선택 사항

    [Header("텍스트 포맷")]
    [SerializeField] private string _promptFormat = "[E] {0}";
    [Tooltip("키 이름 (E, F, Space 등)")]
    [SerializeField] private string _keyName = "E";

    [Header("애니메이션")]
    [SerializeField] private bool _useFade = true;
    [SerializeField] private float _fadeSpeed = 5f;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = false;

    // 상태
    private IInteractable _currentInteractable;
    private bool _isVisible = false;
    private CanvasGroup _canvasGroup;
    private float _currentAlpha = 0f;

    private void Awake()
    {
        // CanvasGroup 추가 (페이드용)
        if (_useFade && _promptPanel != null)
        {
            _canvasGroup = _promptPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = _promptPanel.AddComponent<CanvasGroup>();
            }
            _canvasGroup.alpha = 0f;
        }

        // 초기 상태: 숨김
        if (_promptPanel != null)
        {
            _promptPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // PlayerInteractionController 이벤트 구독
        PlayerInteractionController.OnInteractableDetected += HandleInteractableDetected;
        PlayerInteractionController.OnInteractableLost += HandleInteractableLost;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayerInteractionController.OnInteractableDetected -= HandleInteractableDetected;
        PlayerInteractionController.OnInteractableLost -= HandleInteractableLost;
    }

    private void Update()
    {
        // 페이드 애니메이션
        if (_useFade && _canvasGroup != null)
        {
            UpdateFade();
        }
    }

    #region 이벤트 핸들러

    /// <summary>
    /// 상호작용 대상 발견
    /// </summary>
    private void HandleInteractableDetected(IInteractable interactable)
    {
        _currentInteractable = interactable;
        ShowPrompt();
    }

    /// <summary>
    /// 상호작용 대상 잃음
    /// </summary>
    private void HandleInteractableLost()
    {
        _currentInteractable = null;
        HidePrompt();
    }

    #endregion

    #region UI 표시/숨김

    /// <summary>
    /// 프롬프트 표시
    /// </summary>
    private void ShowPrompt()
    {
        if (_currentInteractable == null)
            return;

        // 텍스트 업데이트
        string promptMessage = _currentInteractable.GetInteractionPrompt();
        if (!string.IsNullOrEmpty(promptMessage))
        {
            UpdatePromptText(promptMessage);
            _isVisible = true;

            if (_promptPanel != null)
            {
                _promptPanel.SetActive(true);
            }

            Log($"프롬프트 표시: {promptMessage}");
        }
    }

    /// <summary>
    /// 프롬프트 숨김
    /// </summary>
    private void HidePrompt()
    {
        _isVisible = false;

        if (!_useFade && _promptPanel != null)
        {
            _promptPanel.SetActive(false);
        }

        Log("프롬프트 숨김");
    }

    /// <summary>
    /// 프롬프트 텍스트 업데이트
    /// </summary>
    private void UpdatePromptText(string message)
    {
        if (_promptText != null)
        {
            // 포맷: "[E] 문 열기"
            string formattedText = string.Format(_promptFormat, message);
            _promptText.text = formattedText;
        }

        // 오브젝트 이름 표시 (선택)
        if (_objectNameText != null && _currentInteractable != null)
        {
            // IInteractable을 GameObject로 캐스팅
            GameObject interactableObject = (_currentInteractable as MonoBehaviour)?.gameObject;
            if (interactableObject != null)
            {
                _objectNameText.text = interactableObject.name;
            }
        }
    }

    #endregion

    #region 페이드 애니메이션

    /// <summary>
    /// 페이드 인/아웃 업데이트
    /// </summary>
    private void UpdateFade()
    {
        float targetAlpha = _isVisible ? 1f : 0f;

        // Lerp로 부드러운 전환
        _currentAlpha = Mathf.Lerp(_currentAlpha, targetAlpha, Time.deltaTime * _fadeSpeed);
        _canvasGroup.alpha = _currentAlpha;

        // 완전히 사라지면 패널 비활성화
        if (_currentAlpha < 0.01f && !_isVisible && _promptPanel != null)
        {
            _promptPanel.SetActive(false);
        }
    }

    #endregion

    #region Public 메서드 (수동 제어용)

    /// <summary>
    /// 수동으로 프롬프트 표시 (이벤트 외 사용)
    /// </summary>
    public void ShowCustomPrompt(string message)
    {
        if (_promptText != null)
        {
            _promptText.text = string.Format(_promptFormat, message);
        }

        _isVisible = true;
        if (_promptPanel != null)
        {
            _promptPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 수동으로 프롬프트 숨김
    /// </summary>
    public void ForceHide()
    {
        HidePrompt();
    }

    /// <summary>
    /// 키 이름 변경 (런타임)
    /// </summary>
    public void SetKeyName(string keyName)
    {
        _keyName = keyName;
        _promptFormat = $"[{keyName}] {{0}}";

        // 현재 표시 중이면 텍스트 업데이트
        if (_isVisible && _currentInteractable != null)
        {
            UpdatePromptText(_currentInteractable.GetInteractionPrompt());
        }
    }

    #endregion

    #region 디버그

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[InteractionPromptUI] {message}");
        }
    }

    #endregion
}
