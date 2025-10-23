using UnityEngine;

/// <summary>
/// 플레이어 상호작용 감지 컨트롤러
/// 
/// 변경사항:
/// - E키 입력 처리 제거
/// - 마우스 호버 감지만 수행
/// - UI 프롬프트용 이벤트만 발생
/// - 실제 상호작용은 PlayerNavController가 처리
/// 
/// 역할:
/// - 마우스 호버로 IInteractable 오브젝트 감지
/// - InteractionPromptUI에 이벤트 전달
/// - UI 표시용 정보 제공
/// 
/// 상호작용 흐름:
/// 1. PlayerInteractionController: 마우스 호버 감지 → UI 표시
/// 2. PlayerNavController: 왼쪽 클릭 → 이동 후 상호작용 실행
/// </summary>
public class PlayerInteractionController : MonoBehaviour
{
    [Header("감지 설정")]
    [Tooltip("마우스 커서 기준 Raycast")]
    [SerializeField] private bool _useMouseRaycast = true;
    [SerializeField] private float _raycastDistance = 100f;

    [Tooltip("플레이어 주변 범위 감지")]
    [SerializeField] private bool _useOverlapSphere = false;
    [SerializeField] private float _overlapRadius = 3.0f;

    [Header("필터")]
    [SerializeField] private LayerMask _interactableLayer = ~0;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = false;
    [SerializeField] private bool _showDebugRay = true;

    // 참조
    private PlayerCharacter _playerCharacter;
    private Camera _mainCamera;

    // 현재 감지된 대상
    private IInteractable _currentInteractable;
    private GameObject _currentInteractableObject;

    // UI 연동 이벤트
    public static event System.Action<IInteractable> OnInteractableDetected;
    public static event System.Action OnInteractableLost;

    private void Awake()
    {
        _playerCharacter = GetComponent<PlayerCharacter>();
        _mainCamera = Camera.main;

        if (_playerCharacter == null)
        {
            Debug.LogError("[PlayerInteractionController] PlayerCharacter 컴포넌트가 필요합니다!");
            enabled = false;
        }
    }

    private void Update()
    {
        // 마우스 호버로 상호작용 대상 감지만 수행
        DetectInteractable();
    }

    #region 감지 로직

    /// <summary>
    /// 상호작용 가능한 오브젝트 감지
    /// </summary>
    private void DetectInteractable()
    {
        IInteractable newInteractable = null;
        GameObject newInteractableObject = null;

        // 마우스 커서 Raycast
        if (_useMouseRaycast)
        {
            newInteractable = DetectWithMouseRaycast(out newInteractableObject);
        }

        // 주변 범위 감지 (보조)
        if (_useOverlapSphere && newInteractable == null)
        {
            newInteractable = DetectWithOverlapSphere(out newInteractableObject);
        }

        // 감지 대상 변경 시 이벤트 발생
        if (newInteractable != _currentInteractable)
        {
            if (_currentInteractable != null)
            {
                OnInteractableLost?.Invoke();
                Log($"호버 종료: {_currentInteractableObject?.name}");
            }

            _currentInteractable = newInteractable;
            _currentInteractableObject = newInteractableObject;

            if (_currentInteractable != null)
            {
                OnInteractableDetected?.Invoke(_currentInteractable);
                Log($"호버 감지: {_currentInteractableObject.name}");
            }
        }
    }

    /// <summary>
    /// 마우스 커서 위치로 Raycast 감지
    /// </summary>
    private IInteractable DetectWithMouseRaycast(out GameObject hitObject)
    {
        hitObject = null;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (_showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * _raycastDistance, Color.yellow);
        }

        if (Physics.Raycast(ray, out hit, _raycastDistance, _interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null && interactable.CanInteract(_playerCharacter))
            {
                hitObject = hit.collider.gameObject;
                return interactable;
            }
        }

        return null;
    }

    /// <summary>
    /// 주변 범위로 가장 가까운 대상 감지
    /// </summary>
    private IInteractable DetectWithOverlapSphere(out GameObject closestObject)
    {
        closestObject = null;

        Collider[] colliders = Physics.OverlapSphere(transform.position, _overlapRadius, _interactableLayer);
        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            IInteractable interactable = col.GetComponent<IInteractable>();

            if (interactable != null && interactable.CanInteract(_playerCharacter))
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);

                if (distance <= interactable.GetInteractionRange() && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                    closestObject = col.gameObject;
                }
            }
        }

        return closestInteractable;
    }

    #endregion

    #region Public API (UI용)

    /// <summary>
    /// 현재 호버 중인 상호작용 대상 반환
    /// </summary>
    public IInteractable GetCurrentInteractable()
    {
        return _currentInteractable;
    }

    /// <summary>
    /// 현재 상호작용 가능한 대상이 있는지 확인
    /// </summary>
    public bool HasInteractable()
    {
        return _currentInteractable != null;
    }

    /// <summary>
    /// 현재 프롬프트 텍스트 가져오기
    /// </summary>
    public string GetCurrentPrompt()
    {
        return _currentInteractable?.GetInteractionPrompt() ?? "";
    }

    #endregion

    #region 디버그

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[PlayerInteractionController] {message}");
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        // 범위 시각화
        if (_useOverlapSphere)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _overlapRadius);
        }

        // 현재 호버 대상 강조
        if (_currentInteractableObject != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _currentInteractableObject.transform.position);
            Gizmos.DrawWireSphere(_currentInteractableObject.transform.position, 0.5f);
        }
    }

    #endregion
}
