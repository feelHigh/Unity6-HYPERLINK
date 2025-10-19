using UnityEngine;

/// <summary>
/// 플레이어 상호작용 컨트롤러
/// 
/// 역할:
/// - IInteractable 오브젝트 감지
/// - E키 입력 처리
/// - 가장 가까운 상호작용 대상 선택
/// - UI 프롬프트 표시 트리거
/// 
/// 감지 방식:
/// 1. Raycast: 플레이어가 보는 방향으로 레이 발사
/// 2. OverlapSphere: 플레이어 주변 범위 내 모든 오브젝트 검색
/// 
/// 현재 구현: Raycast (정확한 조준 필요)
/// 대안: OverlapSphere (주변 자동 감지)
/// 
/// 사용 위치:
/// - Player GameObject에 추가
/// - CharacterUIController와 분리된 독립 시스템
/// 
/// 설정:
/// 1. Player GameObject에 컴포넌트 추가
/// 2. Inspector에서 Interaction Key 설정 (기본: E)
/// 3. Raycast Distance 조정
/// 4. Layer Mask 설정 (선택)
/// </summary>
public class PlayerInteractionController : MonoBehaviour
{
    [Header("입력 설정")]
    [SerializeField] private KeyCode _interactionKey = KeyCode.E;

    [Header("감지 설정")]
    [Tooltip("Raycast 방식: 플레이어가 보는 방향으로 레이 발사")] // 메인 카메라 정중앙 기준
    [SerializeField] private bool _useRaycast = false;
    [SerializeField] private float _raycastDistance = 3.5f;

    [Tooltip("OverlapSphere 방식: 주변 모든 오브젝트 검색")]
    [SerializeField] private bool _useOverlapSphere = true;
    [SerializeField] private float _overlapRadius = 3.0f;

    [Header("필터 (선택)")]
    [Tooltip("특정 레이어만 감지 (설정 안 하면 모두 감지)")]
    [SerializeField] private LayerMask _interactableLayer = ~0; // 모든 레이어

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;
    [SerializeField] private bool _showDebugRay = true;

    // 참조
    private PlayerCharacter _playerCharacter;
    private Camera _mainCamera;

    // 현재 상호작용 대상
    private IInteractable _currentInteractable;
    private GameObject _currentInteractableObject;

    // 이벤트 (UI 연동용)
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
        // 상호작용 대상 감지
        DetectInteractable();

        // E키 입력 처리
        if (Input.GetKeyDown(_interactionKey))
        {
            TryInteract();
        }
    }

    #region 감지 로직

    /// <summary>
    /// 상호작용 가능한 오브젝트 감지
    /// </summary>
    private void DetectInteractable()
    {
        IInteractable newInteractable = null;
        GameObject newInteractableObject = null;

        // Raycast 방식
        if (_useRaycast)
        {
            newInteractable = DetectWithRaycast(out newInteractableObject);
        }

        // OverlapSphere 방식
        if (_useOverlapSphere && newInteractable == null)
        {
            newInteractable = DetectWithOverlapSphere(out newInteractableObject);
        }

        // 상호작용 대상 변경 확인
        if (newInteractable != _currentInteractable)
        {
            if (_currentInteractable != null)
            {
                // 이전 대상 잃음
                OnInteractableLost?.Invoke();
                Log($"상호작용 대상 잃음: {_currentInteractableObject?.name}");
            }

            _currentInteractable = newInteractable;
            _currentInteractableObject = newInteractableObject;

            if (_currentInteractable != null)
            {
                // 새 대상 발견
                OnInteractableDetected?.Invoke(_currentInteractable);
                Log($"상호작용 대상 발견: {_currentInteractableObject.name}");
            }
        }
    }

    /// <summary>
    /// Raycast로 상호작용 대상 감지
    /// 
    /// 장점:
    /// - 정확한 조준 필요 (의도적 상호작용)
    /// - FPS 스타일 게임에 적합
    /// 
    /// 단점:
    /// - 작은 오브젝트는 조준하기 어려움
    /// </summary>
    private IInteractable DetectWithRaycast(out GameObject hitObject)
    {
        hitObject = null;

        // 카메라 중심에서 레이 발사
        Ray ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        // 디버그 레이 시각화
        if (_showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * _raycastDistance, Color.yellow);
        }

        // Raycast 실행
        if (Physics.Raycast(ray, out hit, _raycastDistance, _interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // 거리 확인 (각 오브젝트마다 다른 범위 가능)
                float distance = Vector3.Distance(transform.position, hit.point);
                if (distance <= interactable.GetInteractionRange())
                {
                    // 상호작용 가능 여부 확인
                    if (interactable.CanInteract(_playerCharacter))
                    {
                        hitObject = hit.collider.gameObject;
                        return interactable;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// OverlapSphere로 상호작용 대상 감지
    /// 
    /// 장점:
    /// - 조준 불필요 (주변 자동 감지)
    /// - Diablo 스타일 게임에 적합
    /// - 작은 오브젝트도 쉽게 상호작용
    /// 
    /// 단점:
    /// - 여러 오브젝트가 겹치면 혼란
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

                // 오브젝트 자체 범위 확인
                if (distance <= interactable.GetInteractionRange())
                {
                    // 가장 가까운 대상 선택
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestInteractable = interactable;
                        closestObject = col.gameObject;
                    }
                }
            }
        }

        return closestInteractable;
    }

    #endregion

    #region 상호작용 실행

    /// <summary>
    /// 상호작용 시도
    /// </summary>
    private void TryInteract()
    {
        if (_currentInteractable == null)
        {
            Log("상호작용 대상 없음");
            return;
        }

        if (!_currentInteractable.CanInteract(_playerCharacter))
        {
            Log($"상호작용 불가: {_currentInteractableObject.name}");
            return;
        }

        // 상호작용 실행
        Log($"상호작용 실행: {_currentInteractableObject.name}");
        _currentInteractable.Interact(_playerCharacter);
    }

    #endregion

    #region Public 접근자 (UI용)

    /// <summary>
    /// 현재 상호작용 대상 반환 (UI 프롬프트용)
    /// </summary>
    public IInteractable GetCurrentInteractable()
    {
        return _currentInteractable;
    }

    /// <summary>
    /// 현재 상호작용 가능 여부
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
        if (_currentInteractable != null)
        {
            return _currentInteractable.GetInteractionPrompt();
        }
        return "";
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

        // OverlapSphere 범위 시각화
        if (_useOverlapSphere)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _overlapRadius);
        }

        // 현재 상호작용 대상 강조
        if (_currentInteractableObject != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _currentInteractableObject.transform.position);
            Gizmos.DrawWireSphere(_currentInteractableObject.transform.position, 0.5f);
        }
    }

    #endregion
}
