using UnityEngine;
using TMPro;

/// <summary>
/// 월드 스페이스 상호작용 UI
/// 
/// 기능:
/// - IInteractable 오브젝트 위에 UI 표시
/// - 오브젝트 타입별 이름 표시 (예: "문", "상자", "상인")
/// - 카메라를 항상 바라보도록 회전
/// - 페이드 인/아웃 애니메이션
/// 
/// 사용법:
/// 1. IInteractable 오브젝트의 자식으로 Canvas 생성
/// 2. Canvas 설정:
///    - Render Mode: World Space
///    - Width/Height: 200 x 50
///    - Scale: 0.01, 0.01, 0.01
/// 3. Canvas 자식으로 Panel 생성
/// 4. Panel 자식으로 TextMeshProUGUI 생성
/// 5. Canvas에 이 스크립트 추가
/// </summary>
[RequireComponent(typeof(Canvas))]
public class WorldSpaceInteractionUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("표시 설정")]
    [Tooltip("오브젝트 위 오프셋 (Y축)")]
    [SerializeField] private float _verticalOffset = 2.0f;

    [Header("애니메이션")]
    [SerializeField] private float _fadeSpeed = 8f;

    [Header("디버그")]
    [SerializeField] private bool _alwaysShow = false;

    private Camera _mainCamera;
    private IInteractable _interactable;
    private GameObject _targetObject;
    private bool _isVisible = false;
    private Canvas _canvas;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _canvas = GetComponent<Canvas>();
        _canvas.worldCamera = _mainCamera;

        // 부모 오브젝트에서 IInteractable 찾기
        _targetObject = transform.parent?.gameObject;
        if (_targetObject != null)
        {
            _interactable = _targetObject.GetComponent<IInteractable>();
        }

        // CanvasGroup 자동 추가
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 초기 상태: 숨김
        _canvasGroup.alpha = 0f;
        _canvas.enabled = false;
    }

    private void OnEnable()
    {
        PlayerInteractionController.OnInteractableDetected += HandleInteractableDetected;
        PlayerInteractionController.OnInteractableLost += HandleInteractableLost;
    }

    private void OnDisable()
    {
        PlayerInteractionController.OnInteractableDetected -= HandleInteractableDetected;
        PlayerInteractionController.OnInteractableLost -= HandleInteractableLost;
    }

    private void Update()
    {
        UpdatePosition();
        UpdateRotation();
        UpdateFade();
    }

    #region 이벤트 핸들러

    private void HandleInteractableDetected(IInteractable interactable)
    {
        // 이 UI의 IInteractable과 일치하는지 확인
        if (interactable == _interactable)
        {
            ShowUI();
        }
    }

    private void HandleInteractableLost()
    {
        HideUI();
    }

    #endregion

    #region UI 제어

    private void ShowUI()
    {
        if (_interactable == null || _nameText == null)
            return;

        // 텍스트 업데이트
        string displayName = _interactable.GetInteractionName();
        _nameText.text = displayName;

        _isVisible = true;
        _canvas.enabled = true;
    }

    private void HideUI()
    {
        if (!_alwaysShow)
        {
            _isVisible = false;
        }
    }

    #endregion

    #region 위치 및 회전

    private void UpdatePosition()
    {
        if (_targetObject == null)
            return;

        // 오브젝트 위에 배치
        Vector3 targetPos = _targetObject.transform.position;
        targetPos.y += _verticalOffset;
        transform.position = targetPos;
    }

    private void UpdateRotation()
    {
        if (_mainCamera == null)
            return;

        // 카메라를 항상 바라보도록
        transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                         _mainCamera.transform.rotation * Vector3.up);
    }

    #endregion

    #region 페이드 애니메이션

    private void UpdateFade()
    {
        if (_canvasGroup == null)
            return;

        float targetAlpha = (_isVisible || _alwaysShow) ? 1f : 0f;
        _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, Time.deltaTime * _fadeSpeed);

        // 완전히 투명하면 비활성화
        if (_canvasGroup.alpha < 0.01f && !_isVisible && !_alwaysShow)
        {
            _canvas.enabled = false;
        }
    }

    #endregion

    #region 에디터 헬퍼

#if UNITY_EDITOR
    [ContextMenu("Setup UI")]
    private void SetupUI()
    {
        // Canvas 설정
        _canvas = GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rectTransform = _canvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);
        transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        Debug.Log("WorldSpaceInteractionUI 설정 완료");
    }
#endif

    #endregion
}
