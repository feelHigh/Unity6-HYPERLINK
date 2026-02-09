using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 플레이어 네비게이션 및 이동 컨트롤러
/// 
/// 역할:
/// - 좌클릭 이동 및 상호작용
/// - NavMeshAgent 기반 이동 제어
/// - 애니메이션 업데이트
/// - 넉백 받기 (방어 로직)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PlayerNavController : MonoBehaviour
{
    private static readonly int SPEED_HASH = Animator.StringToHash("Speed");

    private NavMeshAgent _agent;
    private Animator _animator;
    private Camera _mainCamera;
    private PlayerCharacter _playerCharacter;
    private PlayerStateController _stateController;

    private bool _isPerformingSkill = false;
    private bool _isDead = false;

    // 상호작용 시스템
    private IInteractable _pendingInteraction;
    private Coroutine _interactionCoroutine;

    // 기본 이동속도 저장
    private float _baseSpeed;

    [Header("이동 반응성 설정")]
    [Tooltip("방향 전환 각도 임계값 - 이 각도 이상 전환시에만 즉시 회전")]
    [SerializeField] private float _rotationThresholdAngle = 45f;

    [Tooltip("속도 리셋 각도 임계값 - 이 각도 이상시에만 속도 리셋")]
    [SerializeField] private float _velocityResetAngle = 90f;

    [Tooltip("최소 이동 속도 (애니메이션 연속성 유지용)")]
    [SerializeField] private float _minimumMovementSpeed = 1f;

    // 마지막 이동 방향 추적
    private Vector3 _lastMovementDirection = Vector3.zero;
    private bool _isCurrentlyMoving = false;

    // 넉백 디버그 시각화용
    private Vector3 _lastKnockbackStart;
    private Vector3 _lastKnockbackEnd;
    private Vector3 _lastKnockbackIdealEnd;
    private bool _lastKnockbackHitWall;

    [Header("애니메이션 설정")]
    [SerializeField] private float _animationDampTime = 0.1f;

    [Header("넉백 설정")]
    [SerializeField, Tooltip("넉백 이동 시간 (초)")]
    private float _knockbackDuration = 0.3f;

    [SerializeField, Tooltip("벽 충돌 감지 레이어 (Ground, Wall, Obstacle 등)")]
    private LayerMask _knockbackObstacleLayer = ~0;

    [SerializeField, Tooltip("벽과의 최소 안전 거리 (미터)")]
    private float _knockbackWallBuffer = 0.5f;

    [SerializeField, Tooltip("플레이어 캡슐 반지름 (SphereCast용)")]
    private float _playerRadius = 0.5f;

    [SerializeField, Tooltip("Raycast 대신 SphereCast 사용 (더 정확하지만 무거움)")]
    private bool _useSphereCast = false;

    [SerializeField, Tooltip("NavMesh 검색 반경 (미터)")]
    private float _navMeshSearchRadius = 2f;

    [Header("레이어 설정")]
    [SerializeField] private LayerMask _groundLayer = ~0;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    [SerializeField, Tooltip("넉백 경로 시각화 (Gizmo)")]
    private bool _visualizeKnockbackPath = true;

    #region 초기화

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _mainCamera = Camera.main;
        _playerCharacter = GetComponent<PlayerCharacter>();
        _stateController = GetComponent<PlayerStateController>();

        if (_stateController == null)
        {
            Debug.LogError("[PlayerNavController] PlayerStateController가 없습니다!");
        }

        // 기본값 저장
        _baseSpeed = _agent.speed;

        Debug.Log($"[PlayerNavController] Awake - Base Speed: {_baseSpeed}");
    }

    private void Start()
    {
        _animator.applyRootMotion = false;

        // 초기 스탯 적용
        if (_playerCharacter != null)
        {
            CharacterStats initialStats = _playerCharacter.CurrentStats;
            Debug.Log($"[PlayerNavController] Start - 초기 스탯 적용");
            Debug.Log($"  Movement Speed: {initialStats.MovementSpeed:F2}");
        }
        else
        {
            Debug.LogWarning("[PlayerNavController] Start - PlayerCharacter가 null입니다!");
        }
    }

    private void OnEnable()
    {
        // PlayerCharacter 이벤트 구독
        PlayerCharacter.OnPlayerDead += HandlePlayerDead;

        // SkillActivationSystem 이벤트 구독
        SkillActivationSystem.OnSkillExecuted += HandleSkillExecuted;

        // PlayerAttackController 이벤트 구독 (애니메이션 트리거용)
        PlayerAttackController.OnAttackTrigger += HandleAttackAnimationTrigger;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayerCharacter.OnPlayerDead -= HandlePlayerDead;
        SkillActivationSystem.OnSkillExecuted -= HandleSkillExecuted;
        PlayerAttackController.OnAttackTrigger -= HandleAttackAnimationTrigger;
    }

    #endregion

    #region 이벤트 핸들러

    /// <summary>
    /// 플레이어 사망 핸들러
    /// </summary>
    private void HandlePlayerDead()
    {
        _isDead = true;

        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.updateRotation = false;
        }

        StopAllCoroutines();
        _pendingInteraction = null;
        _interactionCoroutine = null;

        Log("플레이어 사망 - 모든 행동 중지");
    }

    /// <summary>
    /// 스킬 시전 시작 핸들러
    /// </summary>
    private void HandleSkillExecuted(SkillData skill)
    {
        _isPerformingSkill = true;

        // 스킬 실행 중 이동 취소
        if (_interactionCoroutine != null)
        {
            StopCoroutine(_interactionCoroutine);
            _interactionCoroutine = null;
            _pendingInteraction = null;
        }

        // 스킬 애니메이션이 끝날 때까지 대기
        StartCoroutine(ResetSkillFlag(1.5f));
    }

    /// <summary>
    /// 공격 애니메이션 트리거 핸들러 (PlayerAttackController로부터)
    /// </summary>
    private void HandleAttackAnimationTrigger(int animationHash)
    {
        if (_animator != null)
        {
            _animator.SetTrigger(animationHash);
            Log("공격 애니메이션 트리거");
        }
    }

    /// <summary>
    /// 스킬 실행 플래그 리셋
    /// </summary>
    private IEnumerator ResetSkillFlag(float delay)
    {
        yield return WaitForSecondsCache.Get(delay);
        _isPerformingSkill = false;
    }

    /// <summary>
    /// 스킬 시전 종료 (외부에서 호출)
    /// </summary>
    public void SetPerformingSkill(bool isPerforming)
    {
        _isPerformingSkill = isPerforming;
    }

    #endregion

    #region Unity Lifecycle

    private void Update()
    {
        // 사망 또는 스킬 시전 중엔 업데이트 안함
        if (_isDead || _isPerformingSkill)
        {
            return;
        }

        // Movement Speed 동적 업데이트
        if (_playerCharacter != null && _agent != null)
        {
            CharacterStats stats = _playerCharacter.CurrentStats;
            float targetSpeed = _baseSpeed + stats.MovementSpeed;

            // 상태이상에 따른 속도 조정
            if (_stateController != null)
            {
                if (_stateController.IsFrozen)
                {
                    targetSpeed = 0f;
                }
                else if (_stateController.IsRoot)
                {
                    targetSpeed = 0f;
                }
            }

            _agent.speed = targetSpeed;
        }

        // 이동 상태 추적
        if (_agent != null && _agent.isOnNavMesh)
        {
            _isCurrentlyMoving = _agent.velocity.magnitude > 0.5f;
            if (_isCurrentlyMoving && _agent.velocity.magnitude > 0.1f)
            {
                _lastMovementDirection = _agent.velocity.normalized;
            }
        }

        // 애니메이션 업데이트
        UpdateAnimator();

        // 입력 처리
        HandleMouseInput();
    }

    #endregion

    #region 입력 처리

    /// <summary>
    /// 마우스 입력 처리 (좌클릭: 이동)
    /// 
    /// 속박(Root) 상태 주의사항:
    /// - 좌클릭: CanMove 체크 → 속박 시 차단
    /// 
    /// UI 처리 로직:
    /// - UI 위 클릭 시 이동 방지
    /// - InputHelper.IsPointerOverUI() 사용
    /// </summary>
    private void HandleMouseInput()
    {
        // 스킬 시전 중에는 이동 불가
        if (_isPerformingSkill)
        {
            return;
        }

        // 좌클릭: 이동 & 상호작용 (속박 상태에서는 불가)
        if (Input.GetMouseButtonDown(0))
        {
            // UI 클릭 체크 - UI 위에서는 이동 하지 않음
            if (InputHelper.IsPointerOverUI())
            {
                Log("UI 클릭 감지 - 이동 취소");
                return;
            }

            // 이동 가능 여부 체크 (빙결/속박/넉다운)
            if (_stateController == null || _stateController.CanMove)
            {
                HandleLeftClick();
            }
            else
            {
                Log($"이동 불가 상태: Frozen={_stateController.IsFrozen}, Root={_stateController.IsRoot}, Stunned={_stateController.IsStunned}, Attacking={_stateController.IsAttacking}");
            }
        }
    }

    /// <summary>
    /// 좌클릭 처리: IInteractable 우선, 없으면 이동
    /// </summary>
    private void HandleLeftClick()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 1순위: 상호작용 가능한 오브젝트 체크
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null && interactable.CanInteract(_playerCharacter))
            {
                _pendingInteraction = interactable;

                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance > 2f)
                {
                    if (_interactionCoroutine != null)
                    {
                        StopCoroutine(_interactionCoroutine);
                    }
                    _interactionCoroutine = StartCoroutine(MoveToInteract(hit.transform));
                    Log($"상호작용 대상으로 이동: {hit.transform.name}");
                }
                else
                {
                    interactable.Interact(_playerCharacter);
                    _pendingInteraction = null;
                    Log($"즉시 상호작용: {hit.transform.name}");
                }
                return;
            }
        }

        // 2순위: 지면 클릭 → 스마트 이동
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _groundLayer))
        {
            _pendingInteraction = null;

            if (_interactionCoroutine != null)
            {
                StopCoroutine(_interactionCoroutine);
                _interactionCoroutine = null;
                _pendingInteraction = null;
            }

            // 스마트 이동 처리
            ProcessSmartMovement(hit.point);

            // 이동 인디케이터 표시
            if (MousePositionIndicator.Instance != null)
            {
                MousePositionIndicator.Instance.ShowMoveIndicator(hit.point);
            }

            Log($"이동 명령: {hit.point}");
        }
    }

    /// <summary>
    /// 스마트 이동 처리
    /// 방향 전환 각도에 따라 최적의 이동 방식 선택
    /// </summary>
    private void ProcessSmartMovement(Vector3 targetPoint)
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        // 목표 방향 계산
        Vector3 targetDirection = (targetPoint - transform.position).normalized;
        targetDirection.y = 0;

        if (targetDirection == Vector3.zero) return;

        // 현재 이동 중인지 확인
        bool isMoving = _agent.velocity.magnitude > 0.5f;
        float currentSpeed = _agent.velocity.magnitude;

        if (isMoving && _lastMovementDirection != Vector3.zero)
        {
            // 이동 중 - 방향 전환 각도 계산
            float directionAngle = Vector3.Angle(_lastMovementDirection, targetDirection);

            Log($"방향 전환 각도: {directionAngle:F1}°, 현재 속도: {currentSpeed:F1}");

            if (directionAngle < _rotationThresholdAngle)
            {
                // 작은 방향 전환 - 부드럽게 경로만 업데이트
                _agent.SetDestination(targetPoint);
                Log("→ 부드러운 경로 업데이트 (속도 유지)");
            }
            else if (directionAngle < _velocityResetAngle)
            {
                // 중간 방향 전환 - 회전 후 최소 속도로 시작
                _agent.velocity = targetDirection * _minimumMovementSpeed;
                _agent.SetDestination(targetPoint);
                Log("→ 방향 전환 (최소 속도 시작)");
            }
            else
            {
                // 큰 방향 전환 - 완전 정지 후 재시작
                _agent.velocity = Vector3.zero;
                _agent.SetDestination(targetPoint);
                Log("→ 완전 정지 후 재시작");
            }
        }
        else
        {
            // 이동 중 아님 - 일반 목적지 설정
            _agent.SetDestination(targetPoint);
            Log("→ 새로운 이동 시작");
        }
    }

    /// <summary>
    /// 상호작용 대상으로 이동 코루틴
    /// </summary>
    private IEnumerator MoveToInteract(Transform target)
    {
        if (_agent == null || target == null)
        {
            yield break;
        }

        // 상호작용 거리 내로 이동
        while (Vector3.Distance(transform.position, target.position) > 2f)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.SetDestination(target.position);
            }
            yield return null;
        }

        // 상호작용 실행
        if (_pendingInteraction != null)
        {
            _pendingInteraction.Interact(_playerCharacter);
            _pendingInteraction = null;
        }

        _interactionCoroutine = null;
    }

    #endregion

    #region 넉백

    /// <summary>
    /// 넉백 힘 적용
    /// 
    /// 기능:
    /// - 벽 충돌 감지 (SphereCast/Raycast)
    /// - NavMesh 검증
    /// - DOTween으로 부드러운 이동
    /// 
    /// 사용법: ApplyKnockback(3f, -transform.forward);
    /// </summary>
    public void ApplyKnockback(float distance, Vector3 direction)
    {
        if (_agent == null || !_agent.isOnNavMesh)
        {
            Log("경고: 넉백 적용 불가 (NavMesh 위에 없음)");
            return;
        }

        direction.y = 0;
        direction.Normalize();

        if (direction == Vector3.zero)
        {
            Log("경고: 잘못된 넉백 방향 (영벡터)");
            return;
        }

        StartCoroutine(ApplyKnockbackCoroutine(distance, direction));
    }

    /// <summary>
    /// 넉백 코루틴
    /// </summary>
    private IEnumerator ApplyKnockbackCoroutine(float distance, Vector3 direction)
    {
        Vector3 startPosition = transform.position;
        Vector3 idealEndPosition = startPosition + (direction * distance);

        // 안전한 종료 위치 계산 (벽 충돌 고려)
        Vector3 safeEndPosition = CalculateSafeKnockbackPosition(startPosition, direction, distance);

        // 디버그 시각화용 저장
        _lastKnockbackStart = startPosition;
        _lastKnockbackEnd = safeEndPosition;
        _lastKnockbackIdealEnd = idealEndPosition;
        _lastKnockbackHitWall = (safeEndPosition != idealEndPosition);

        // NavMesh 검증
        Vector3 validPosition;
        if (!IsPositionOnNavMesh(safeEndPosition, out validPosition))
        {
            Log($"경고: 조정된 위치가 NavMesh 밖, 가장 가까운 유효 위치 사용");
            safeEndPosition = validPosition;
        }

        // NavMeshAgent 일시 정지
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        // DOTween으로 부드러운 넉백
        float elapsedTime = 0f;

        while (elapsedTime < _knockbackDuration)
        {
            float t = elapsedTime / _knockbackDuration;
            transform.position = Vector3.Lerp(startPosition, safeEndPosition, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 최종 위치 설정
        transform.position = safeEndPosition;

        // NavMeshAgent 재활성화
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }

        Log($"넉백 완료: {startPosition} → {safeEndPosition}");
    }

    /// <summary>
    /// 벽 충돌을 고려한 안전한 넉백 위치 계산
    /// 
    /// SphereCast (권장) 또는 Raycast를 사용하여 경로상 장애물 감지
    /// 플레이어 반지름을 고려하여 벽과의 안전 거리 확보
    /// </summary>
    private Vector3 CalculateSafeKnockbackPosition(Vector3 start, Vector3 direction, float distance)
    {
        RaycastHit hit;
        bool hitWall = false;

        if (_useSphereCast)
        {
            // SphereCast: 플레이어 크기를 고려한 정확한 충돌 검사
            hitWall = Physics.SphereCast(
                start + Vector3.up * _playerRadius,
                _playerRadius,
                direction,
                out hit,
                distance,
                _knockbackObstacleLayer
            );
        }
        else
        {
            // Raycast: 가벼운 충돌 검사
            hitWall = Physics.Raycast(
                start,
                direction,
                out hit,
                distance,
                _knockbackObstacleLayer
            );
        }

        if (hitWall)
        {
            // 벽 감지: 안전 거리 확보
            float safeDistance = Mathf.Max(0f, hit.distance - _knockbackWallBuffer);
            Vector3 safePosition = start + (direction * safeDistance);

            Log($"벽 충돌 감지: {hit.collider.name} - 거리 조정: {distance:F2}m → {safeDistance:F2}m");

            return safePosition;
        }

        // 벽 없음: 원래 목표 위치로 이동
        return start + (direction * distance);
    }

    /// <summary>
    /// 위치가 NavMesh 위에 있는지 검증
    /// 
    /// NavMesh 밖이면 가장 가까운 유효한 위치 반환
    /// </summary>
    private bool IsPositionOnNavMesh(Vector3 position, out Vector3 validPosition)
    {
        NavMeshHit hit;

        // NavMesh 위에서 가장 가까운 지점 찾기
        if (NavMesh.SamplePosition(position, out hit, _navMeshSearchRadius, NavMesh.AllAreas))
        {
            validPosition = hit.position;

            // 원래 위치와 매우 가까우면 유효함
            float distance = Vector3.Distance(position, hit.position);

            if (_enableDebugLogs && distance >= 0.1f)
            {
                Debug.Log($"[NavMesh] 보정: {distance:F2}m 이동 ({position} → {hit.position})");
            }

            return distance < 0.5f;
        }

        // NavMesh를 찾지 못함: 현재 위치 유지
        validPosition = transform.position;
        Log("경고: NavMesh를 찾을 수 없음, 현재 위치 유지");
        return false;
    }

    /// <summary>
    /// 강제 정지 (외부에서 호출)
    /// </summary>
    public void ForceStop()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.velocity = Vector3.zero;
            _agent.ResetPath();
            _agent.isStopped = true;
            _isCurrentlyMoving = false;
            _lastMovementDirection = Vector3.zero;
        }
    }

    #endregion

    /// <summary>
    /// 리스폰 시 상태 초기화 (스마트 이동 포함)
    /// </summary>
    public void ResetAfterRespawn()
    {
        _isDead = false;
        _isPerformingSkill = false;

        // 상호작용 초기화
        if (_interactionCoroutine != null)
        {
            StopCoroutine(_interactionCoroutine);
            _interactionCoroutine = null;
        }
        _pendingInteraction = null;

        // ⭐ 스마트 이동 상태 초기화
        _lastMovementDirection = Vector3.zero;
        _isCurrentlyMoving = false;

        // NavMeshAgent 완전 재활성화
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.updateRotation = true;
            _agent.velocity = Vector3.zero;
            _agent.ResetPath();
        }

        // Animator 초기화
        if (_animator != null)
        {
            _animator.SetFloat(SPEED_HASH, 0f);
        }

        Log("리스폰 후 상태 초기화 완료 - 스마트 이동 포함");
    }

    #region 애니메이션

    private void UpdateAnimator()
    {
        float speed = _agent.velocity.magnitude;
        _animator.SetFloat(SPEED_HASH, speed, _animationDampTime, Time.deltaTime);
    }

    #endregion

    #region 디버그

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[PlayerNavController] {message}");
        }
    }

    /// <summary>
    /// 넉백 경로 시각화 (Gizmo)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!_visualizeKnockbackPath) return;

        // 넉백 경로 시각화
        if (_lastKnockbackStart != Vector3.zero)
        {
            // 이상적인 넉백 경로 (회색 점선)
            Gizmos.color = Color.gray;
            DrawDashedLine(_lastKnockbackStart, _lastKnockbackIdealEnd, 0.2f);

            // 실제 넉백 경로 (노란색 or 빨간색)
            Gizmos.color = _lastKnockbackHitWall ? Color.red : Color.yellow;
            Gizmos.DrawLine(_lastKnockbackStart, _lastKnockbackEnd);

            // 시작점 (초록색)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_lastKnockbackStart, 0.2f);

            // 종료점 (빨간색 or 초록색)
            Gizmos.color = _lastKnockbackHitWall ? Color.red : Color.green;
            Gizmos.DrawWireSphere(_lastKnockbackEnd, 0.3f);

            // 이상적인 종료점 (회색)
            if (_lastKnockbackHitWall)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawWireSphere(_lastKnockbackIdealEnd, 0.25f);
            }
        }

        // 넉백 레이캐스트 시각화 (실시간)
        if (_useSphereCast)
        {
            // SphereCast 경로
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Vector3 direction = transform.forward;
            for (float t = 0; t <= 1f; t += 0.1f)
            {
                Vector3 pos = transform.position + direction * (5f * t);
                Gizmos.DrawWireSphere(pos, _playerRadius);
            }
        }
    }

    /// <summary>
    /// 점선 그리기 헬퍼
    /// </summary>
    private void DrawDashedLine(Vector3 start, Vector3 end, float dashSize)
    {
        Vector3 direction = (end - start).normalized;
        float totalDistance = Vector3.Distance(start, end);
        int dashCount = Mathf.CeilToInt(totalDistance / (dashSize * 2));

        for (int i = 0; i < dashCount; i++)
        {
            Vector3 dashStart = start + direction * (i * dashSize * 2);
            Vector3 dashEnd = dashStart + direction * dashSize;

            if (Vector3.Distance(dashEnd, start) > totalDistance)
                dashEnd = end;

            Gizmos.DrawLine(dashStart, dashEnd);
        }
    }

    [ContextMenu("Debug: Print Speed Info")]
    private void DebugPrintSpeedInfo()
    {
        Debug.Log("===== Movement Speed 정보 =====");
        Debug.Log($"기본 속도 (_baseSpeed): {_baseSpeed:F2}");
        Debug.Log($"현재 NavMeshAgent 속도: {_agent.speed:F2}");

        if (_playerCharacter != null)
        {
            CharacterStats stats = _playerCharacter.CurrentStats;
            Debug.Log($"Movement Speed 스탯: {stats.MovementSpeed:F2}");
            Debug.Log($"Dexterity: {stats.Dexterity}");

            float expectedSpeed = _baseSpeed + stats.MovementSpeed;
            Debug.Log($"예상 속도 (상태이상 없음): {expectedSpeed:F2}");
        }
        else
        {
            Debug.LogWarning("PlayerCharacter가 null입니다!");
        }
    }

    [ContextMenu("Debug: Test Knockback (Forward 3m)")]
    private void DebugTestKnockback()
    {
        ApplyKnockback(3f, transform.forward);
    }

    [ContextMenu("Debug: Test Knockback (Backward 4m)")]
    private void DebugTestKnockbackBackward()
    {
        ApplyKnockback(4f, -transform.forward);
    }

    #endregion
}
