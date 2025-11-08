using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 플레이어 마우스 컨트롤 시스템
/// 
/// 좌클릭: 이동 & 상호작용
/// 우클릭: 회전 & 전방 원뿔 범위 공격
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PlayerNavController : MonoBehaviour
{
    private static readonly int SPEED_HASH = Animator.StringToHash("Speed");
    private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    private const float ATTACK_DAMAGE_TIMING = 0.5f;
    private const float MIN_ATTACK_COOLDOWN = 0.1f; // 최소 쿨다운 제한

    private NavMeshAgent _agent;
    private Animator _animator;
    private Camera _mainCamera;
    private PlayerCharacter _playerCharacter;
    private PlayerStateController _stateController;

    private bool _manualRotationThisFrame = false;
    private bool _isAttacking = false;
    private bool _isPerformingSkill = false;
    private bool _isDead = false;
    private Transform _currentTarget = null;
    private bool _isOnCooldown = false;

    // 상호작용 시스템
    private IInteractable _pendingInteraction;
    private Coroutine _interactionCoroutine;

    // 기본 이동속도 및 쿨다운 저장
    private float _baseSpeed;
    private float _baseCooldown;

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

    [Header("런타임 정보")]
    [SerializeField, Tooltip("현재 적용된 공격 쿨다운 (읽기 전용)")]
    private float _currentAttackCooldown;

    [Header("애니메이션 설정")]
    [SerializeField] private float _animationDampTime = 0.1f;
    [SerializeField] private float _attackAnimationDuration = 1.0f;

    [Header("전투 설정")]
    [Tooltip("공격 범위 (미터)")]
    [SerializeField] private float _attackRange = 3f;

    [Tooltip("공격 각도 (전방 원뿔 범위, 90도 = 전방 1/4 원)")]
    [SerializeField] private float _attackAngle = 90f;

    [SerializeField] private float _attackDamage = 25f;
    public float AttackDamage => _attackDamage;

    [Tooltip("기본 공격 쿨다운 (초) - Attack Speed 스탯에 의해 감소됨")]
    [SerializeField] private float _attackSpeed = 1f;

    [Header("기본 공격 VFX")]
    [SerializeField] private GameObject _baseAttackVfxPrefab;
    [SerializeField] private Vector3 _vfxPositionOffset = new Vector3(0f, 1f, 1f);
    [SerializeField] private float _vfxLifetime = 2f;

    [Header("넉백 설정")]
    [SerializeField, Tooltip("넉백 이동 시간 (초)")]
    private float _knockbackDuration = 0.3f;

    [SerializeField, Tooltip("벽 충돌 감지 레이어 (Ground, Wall, Obstacle 등)")]
    private LayerMask _knockbackObstacleLayer = ~0; // 모든 레이어

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
        _baseCooldown = _attackSpeed;
        _currentAttackCooldown = _baseCooldown;

        Debug.Log($"[PlayerNavController] Awake - Base Speed: {_baseSpeed}, Base Cooldown: {_baseCooldown:F2}초");
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
            Debug.Log($"  Attack Speed: {initialStats.AttackSpeed:F2}");
            UpdateAttackCooldown(initialStats.AttackSpeed);
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
        PlayerCharacter.OnStatsChanged += HandleStatsChanged;

        // SkillActivationSystem 이벤트 구독
        SkillActivationSystem.OnSkillExecuted += HandleSkillExecuted;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayerCharacter.OnPlayerDead -= HandlePlayerDead;
        PlayerCharacter.OnStatsChanged -= HandleStatsChanged;
        SkillActivationSystem.OnSkillExecuted -= HandleSkillExecuted;
    }

    #endregion

    #region 이벤트 핸들러

    /// <summary>
    /// 플레이어 사망 핸들러
    /// </summary>
    private void HandlePlayerDead()
    {
        _isDead = true;
        _isAttacking = false;

        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        // 실행 중인 코루틴 정지
        StopAllCoroutines();

        _isAttacking = false;
        _currentTarget = null;
        _pendingInteraction = null;
        _interactionCoroutine = null;
        
        Log("플레이어 사망 - 모든 행동 중지");
    }

    /// <summary>
    /// 스탯 변경 핸들러
    /// Movement Speed와 Attack Speed 업데이트
    /// </summary>
    private void HandleStatsChanged(CharacterStats newStats)
    {
        // Attack Speed 갱신
        UpdateAttackCooldown(newStats.AttackSpeed);

        // Movement Speed는 Update()에서 지속적으로 갱신됨
        Log($"스탯 변경: MS={newStats.MovementSpeed:F2}, AS={newStats.AttackSpeed:F2}");
    }

    /// <summary>
    /// 스킬 시전 시작 핸들러
    /// </summary>
    private void HandleSkillExecuted(SkillData skill)
    {
        _isPerformingSkill = true;

        // 스킬 실행 중 이동/공격 취소
        _currentTarget = null;

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
    /// 스킬 실행 플래그 리셋
    /// </summary>
    private IEnumerator ResetSkillFlag(float delay)
    {
        yield return new WaitForSeconds(delay);
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
    /// 마우스 입력 처리 (좌클릭: 이동, 우클릭: 공격)
    /// 
    /// 속박(Root) 상태 주의사항:
    /// - 좌클릭과 우클릭을 독립적으로 처리
    /// - 좌클릭: CanMove 체크 → 속박 시 차단
    /// - 우클릭: CanAttack 체크만 → 속박 시 허용
    /// </summary>
    private void HandleMouseInput()
    {
        // 스킬 시전 중에는 이동/공격 불가
        if (_isPerformingSkill)
        {
            return;
        }

        // 좌클릭: 이동 & 상호작용 (속박 상태에서는 불가)
        if (Input.GetMouseButtonDown(0))
        {
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

        // 우클릭: 공격 (속박 상태에서도 가능)
        if (Input.GetMouseButtonDown(1))
        {
            // 공격 가능 여부 체크 (빙결/넉다운만 차단, 속박은 허용)
            if (_stateController == null || _stateController.CanAttack)
            {
                HandleRightClick();
            }
            else
            {
                Log($"공격 불가 상태: Frozen={_stateController.IsFrozen}, Stunned={_stateController.IsStunned}");
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
            _currentTarget = null;
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
            // ⭐ 이동 중 - 방향 전환 각도 계산
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
                // 중간 방향 전환 - 속도 감소 없이 즉시 회전
                transform.rotation = Quaternion.LookRotation(targetDirection);
                _agent.SetDestination(targetPoint);
                Log("→ 즉시 회전 (속도 유지)");
            }
            else
            {
                // 큰 방향 전환 - 최소 속도로 리셋 후 회전
                _agent.velocity = targetDirection * _minimumMovementSpeed;
                transform.rotation = Quaternion.LookRotation(targetDirection);
                _agent.SetDestination(targetPoint);
                Log("→ 큰 방향 전환 (최소 속도 유지)");
            }
        }
        else
        {
            // ⭐ 정지 상태에서 시작 - 즉시 회전
            _agent.velocity = Vector3.zero;
            _agent.ResetPath();
            transform.rotation = Quaternion.LookRotation(targetDirection);
            _agent.SetDestination(targetPoint);
            Log("→ 정지 상태에서 시작");
        }

        // 마지막 방향 저장
        _lastMovementDirection = targetDirection;
    }

    /// <summary>
    /// 우클릭 처리: 마우스 방향으로 회전 후 공격
    /// </summary>
    private void HandleRightClick()
    {
        if (_isAttacking || _isOnCooldown)
        {
            Log("공격 중이거나 쿨다운 상태");
            return;
        }

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _groundLayer))
        {
            // NavMeshAgent 완전 정지
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.velocity = Vector3.zero;
                _agent.ResetPath();
                _agent.isStopped = true;

                // ⭐ 이동 상태 리셋
                _isCurrentlyMoving = false;
                _lastMovementDirection = Vector3.zero;
            }

            // 마우스 위치로 회전
            Vector3 targetDirection = (hit.point - transform.position).normalized;
            targetDirection.y = 0;

            if (targetDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(targetDirection);
            }

            // 공격 인디케이터 표시
            if (MousePositionIndicator.Instance != null)
            {
                MousePositionIndicator.Instance.ShowAttackIndicator(hit.point);
            }

            // 전방 원뿔 범위 내 적 탐색
            List<EnemyController> enemies = GetEnemiesInFrontCone();

            // 적이 없어도 헛스윙 허용
            PerformMultiAttack(enemies);
        }
    }

    /// <summary>
    /// 상호작용을 위한 이동
    /// </summary>
    private IEnumerator MoveToInteract(Transform target)
    {
        float interactionRange = 2f;
        _agent.SetDestination(target.position);

        while (Vector3.Distance(transform.position, target.position) > interactionRange)
        {
            yield return null;
        }

        _agent.isStopped = true;

        // 상호작용 실행
        if (_pendingInteraction != null && _pendingInteraction.CanInteract(_playerCharacter))
        {
            _pendingInteraction.Interact(_playerCharacter);
            Log($"상호작용 완료: {target.name}");
        }

        _pendingInteraction = null;
        _interactionCoroutine = null;
        _agent.isStopped = false;
    }

    #endregion

    #region 전투

    /// <summary>
    /// 전방 원뿔 범위 내 적 탐색
    /// </summary>
    private List<EnemyController> GetEnemiesInFrontCone()
    {
        List<EnemyController> result = new List<EnemyController>();

        Collider[] hits = Physics.OverlapSphere(transform.position, _attackRange);

        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();

            if (enemy != null)
            {
                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToEnemy);

                if (angle <= _attackAngle / 2f)
                {
                    result.Add(enemy);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 다중 적 공격 실행
    /// 데미지 계산 방식: PlayerCharacter에서 계산 후 EnemyController.TakeDamage(float) 호출
    /// </summary>
    private void PerformMultiAttack(List<EnemyController> enemies)
    {
        StartCoroutine(AttackCoroutine(enemies));
    }

    /// <summary>
    /// 공격 코루틴
    /// </summary>
    private IEnumerator AttackCoroutine(List<EnemyController> enemies)
    {
        _isAttacking = true;
        _isOnCooldown = true;

        // 공격 상태 설정 (이동 차단)
        if (_stateController != null)
        {
            _stateController.SetAttacking(true);
        }

        // 공격 애니메이션 재생
        _animator.SetTrigger(ATTACK_HASH);

        Log($"공격 시작! 적 {enemies.Count}명 범위 내");

        // 기본 공격 VFX 생성
        SpawnBaseAttackVFX();

        // 애니메이션 타이밍에 맞춰 데미지 적용
        yield return new WaitForSeconds(ATTACK_DAMAGE_TIMING);

        // 데미지 적용
        if (_playerCharacter != null)
        {
            float attackPower = _playerCharacter.GetAttackPower();

            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.TakeDamage(attackPower);
                    Log($"  → {enemy.name}에게 {attackPower:F1} 데미지");
                }
            }
        }

        // 공격 상태 해제 (이동 가능)
        if (_stateController != null)
        {
            _stateController.SetAttacking(false);
        }

        // 애니메이션 종료 대기
        float remainingTime = _attackAnimationDuration - ATTACK_DAMAGE_TIMING;
        yield return new WaitForSeconds(remainingTime);

        _isAttacking = false;

        // NavMeshAgent 재활성화
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }

        // 쿨다운 시작
        yield return new WaitForSeconds(_currentAttackCooldown);
        _isOnCooldown = false;

        Log("공격 쿨다운 완료");
    }

    /// <summary>
    /// 공격 쿨다운 업데이트 (Attack Speed 스탯 기반)
    /// AttackSpeed를 absolute 감소값으로 처리
    /// 
    /// 공식: 최종 쿨다운 = 기본 쿨다운 - AttackSpeed (최소 0.1초)
    /// 예시: Dex 5 → Attack Speed 0.25 → Cooldown = 1.0 - 0.25 = 0.75초
    /// </summary>
    private void UpdateAttackCooldown(float attackSpeedStat)
    {
        // Absolute 감소 방식
        _currentAttackCooldown = _baseCooldown - attackSpeedStat;

        // 최소 쿨다운 제한 (너무 빠른 공격 방지)
        _currentAttackCooldown = Mathf.Max(_currentAttackCooldown, MIN_ATTACK_COOLDOWN);

        Debug.Log($"[Attack Speed] 쿨다운 업데이트: {_currentAttackCooldown:F2}초 (스탯: {attackSpeedStat:F2})");
    }

    /// <summary>
    /// 기본 공격 VFX 생성
    /// </summary>
    private void SpawnBaseAttackVFX()
    {
        if (_baseAttackVfxPrefab == null)
        {
            Log("기본 공격 VFX 프리팹이 설정되지 않았습니다");
            return;
        }

        Vector3 spawnPosition = transform.position + transform.TransformDirection(_vfxPositionOffset);
        Quaternion spawnRotation = transform.rotation;

        GameObject vfxInstance = Instantiate(_baseAttackVfxPrefab, spawnPosition, spawnRotation);

        if (_vfxLifetime > 0)
        {
            Destroy(vfxInstance, _vfxLifetime);
        }
        else
        {
            ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(vfxInstance, duration);
            }
            else
            {
                Destroy(vfxInstance, 2f);
            }
        }

        Log($"기본 공격 VFX 생성: {vfxInstance.name}");
    }

    /// <summary>
    /// 타겟 추적
    /// </summary>
    private void FollowTarget()
    {
        if (_currentTarget == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, _currentTarget.position);

        if (distance > _attackRange)
        {
            _agent.SetDestination(_currentTarget.position);
        }
        else
        {
            _agent.isStopped = true;
        }
    }

    #endregion

    #region 이동속도 관리

    /// <summary>
    /// 이동속도 업데이트 (스탯 기반 + 상태이상 배율)
    /// 
    /// 공식: 최종 속도 = (기본속도 + MovementSpeed) × 상태배율
    /// 예시: Dex 5 → Movement Speed 0.5 → Speed = (5 + 0.5) × 1 = 5.5
    /// </summary>
    private void UpdateMovementSpeed()
    {
        if (_agent == null || _playerCharacter == null || _stateController == null) return;

        CharacterStats stats = _playerCharacter.CurrentStats;

        // Absolute 가산 방식
        float baseSpeedWithStat = _baseSpeed + stats.MovementSpeed;

        // 상태이상 배율 (둔화, 빙결 등)
        float stateMultiplier = _stateController.GetMovementSpeedMultiplier();

        // 최종 이동속도 = (기본속도 + 스탯) × 상태배율
        _agent.speed = baseSpeedWithStat * stateMultiplier;
    }

    #endregion

    #region 상태이상 효과

    /// <summary>
    /// 강제 정지 (빙결/속박 시 호출)
    /// </summary>
    public void ForceStop()
    {
        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        _currentTarget = null;

        if (_interactionCoroutine != null)
        {
            StopCoroutine(_interactionCoroutine);
            _interactionCoroutine = null;
            _pendingInteraction = null;
        }

        Log("강제 정지");
    }

    /// <summary>
    /// 넉백 적용
    /// </summary>
    public void ApplyKnockback(float knockbackPower, Vector3 direction)
    {
        if (_isDead || _agent == null) return;

        StartCoroutine(KnockbackCoroutine(knockbackPower, direction));
    }

    /// <summary>
    /// 넉백 코루틴 (벽 충돌 감지)
    /// - Raycast/SphereCast로 벽 충돌 사전 감지
    /// - 벽 감지 시 안전한 거리로 자동 조정
    /// - NavMesh 유효성 검증
    /// - 디버그 시각화 추가
    /// </summary>
    private IEnumerator KnockbackCoroutine(float power, Vector3 direction)
    {
        // NavMeshAgent 일시 정지
        bool wasEnabled = _agent.enabled;
        if (wasEnabled)
        {
            _agent.enabled = false;
        }

        // 넉백 방향 계산 (수평만)
        Vector3 knockbackDirection = direction.normalized;
        knockbackDirection.y = 0;

        Vector3 startPosition = transform.position;
        Vector3 idealTargetPosition = startPosition + (knockbackDirection * power);

        // === 핵심: 벽 충돌 검사 ===
        Vector3 finalTargetPosition = CalculateSafeKnockbackPosition(
            startPosition,
            knockbackDirection,
            power
        );

        // === NavMesh 검증 ===
        if (!IsPositionOnNavMesh(finalTargetPosition, out Vector3 validPosition))
        {
            finalTargetPosition = validPosition;
            Log($"넉백: NavMesh 밖 감지, 유효한 위치로 보정 ({validPosition})");
        }

        // 실제 이동 거리 계산
        float actualDistance = Vector3.Distance(startPosition, finalTargetPosition);

        // 디버그 시각화용 저장
        _lastKnockbackStart = startPosition;
        _lastKnockbackEnd = finalTargetPosition;
        _lastKnockbackIdealEnd = idealTargetPosition;
        _lastKnockbackHitWall = (finalTargetPosition != idealTargetPosition);

        // DOTween으로 부드러운 넉백
        transform.DOMove(finalTargetPosition, _knockbackDuration)
            .SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(_knockbackDuration);

        // NavMeshAgent 재활성화
        if (wasEnabled && !_isDead)
        {
            _agent.enabled = true;
        }

        Log($"넉백 완료: {actualDistance:F2}m 이동 (요청: {power}m)");
    }

    /// <summary>
    /// 안전한 넉백 위치 계산
    /// 
    /// Raycast/SphereCast로 벽 충돌 감지하고,
    /// 충돌 시 벽 앞까지만 이동하도록 거리 조정
    /// </summary>
    private Vector3 CalculateSafeKnockbackPosition(Vector3 start, Vector3 direction, float distance)
    {
        RaycastHit hit;
        bool didHit = false;

        if (_useSphereCast)
        {
            // SphereCast: 플레이어의 캡슐 형태를 고려한 정확한 충돌 검사
            didHit = Physics.SphereCast(
                start,
                _playerRadius,
                direction,
                out hit,
                distance,
                _knockbackObstacleLayer
            );

            if (_enableDebugLogs && didHit)
            {
                Debug.Log($"[SphereCast] 충돌 감지: {hit.collider.name} at {hit.distance:F2}m");
            }
        }
        else
        {
            // Raycast: 빠르지만 단순한 선형 충돌 검사
            didHit = Physics.Raycast(
                start,
                direction,
                out hit,
                distance,
                _knockbackObstacleLayer
            );

            if (_enableDebugLogs && didHit)
            {
                Debug.Log($"[Raycast] 충돌 감지: {hit.collider.name} at {hit.distance:F2}m");
            }
        }

        if (didHit)
        {
            // 벽 감지: 충돌 지점에서 안전 버퍼만큼 떨어진 위치
            float safeDistance = Mathf.Max(0f, hit.distance - _knockbackWallBuffer);
            Vector3 safePosition = start + (direction * safeDistance);

            Log($"넉백 벽 충돌 감지! {hit.collider.name} - 거리 조정: {distance:F2}m → {safeDistance:F2}m");

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

    #endregion

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

    [ContextMenu("Debug: Print Attack Cooldown Info")]
    private void DebugPrintCooldownInfo()
    {
        Debug.Log("===== Attack Cooldown 정보 =====");
        Debug.Log($"기본 쿨다운 (_baseCooldown): {_baseCooldown:F2}초");
        Debug.Log($"현재 쿨다운 (_currentAttackCooldown): {_currentAttackCooldown:F2}초");
        Debug.Log($"Inspector 설정값 (_attackSpeed): {_attackSpeed:F2}초");

        if (_playerCharacter != null)
        {
            CharacterStats stats = _playerCharacter.CurrentStats;
            Debug.Log($"Attack Speed 스탯: {stats.AttackSpeed:F2}");
            Debug.Log($"Dexterity: {stats.Dexterity}");

            float expectedCooldown = _baseCooldown - stats.AttackSpeed;
            expectedCooldown = Mathf.Max(expectedCooldown, MIN_ATTACK_COOLDOWN);
            Debug.Log($"예상 쿨다운: {expectedCooldown:F2}초");
        }
        else
        {
            Debug.LogWarning("PlayerCharacter가 null입니다!");
        }
    }

    [ContextMenu("Debug: Print Damage Info")]
    private void DebugPrintDamageInfo()
    {
        Debug.Log("===== Attack Damage 정보 =====");
        Debug.Log($"기본 데미지 (_attackDamage): {_attackDamage:F1} [폴백 용도]");

        if (_playerCharacter != null)
        {
            CharacterClass charClass = _playerCharacter.CharacterClass;
            int mainStat = _playerCharacter.GetMainStat();
            float attackPower = _playerCharacter.GetAttackPower();
            string attackType = _playerCharacter.IsPhysicalAttacker() ? "물리" : "마법";

            Debug.Log($"캐릭터 클래스: {charClass}");
            Debug.Log($"주요 스탯: {mainStat}");
            Debug.Log($"공격 타입: {attackType}");
            Debug.Log($"최종 공격력: {attackPower:F1}");
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

    /// <summary>
    /// 공격 범위 시각화 (Scene View에서 선택 시에만)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector3 position = transform.position;

        // 공격 범위 구체
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(position, _attackRange);

        // 공격 원뿔
        DrawAttackCone(position);

        // 범위 내 적
        DrawEnemiesInCone();
    }

    /// <summary>
    /// 공격 원뿔 그리기
    /// </summary>
    private void DrawAttackCone(Vector3 position)
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);

        Vector3 forward = transform.forward * _attackRange;
        int segments = 20;
        float angleStep = _attackAngle / segments;

        Vector3 prevPoint = position + Quaternion.Euler(0, -_attackAngle / 2f, 0) * forward;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -_attackAngle / 2f + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
            Vector3 point = position + direction;

            Gizmos.DrawLine(prevPoint, point);

            if (i % 5 == 0)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
                Gizmos.DrawLine(position, point);
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            }

            prevPoint = point;
        }

        // 원뿔 경계선
        Gizmos.color = Color.yellow;
        Vector3 leftBound = Quaternion.Euler(0, -_attackAngle / 2f, 0) * forward;
        Vector3 rightBound = Quaternion.Euler(0, _attackAngle / 2f, 0) * forward;
        Gizmos.DrawLine(position, position + leftBound);
        Gizmos.DrawLine(position, position + rightBound);
    }

    /// <summary>
    /// 범위 내 적 시각화
    /// </summary>
    private void DrawEnemiesInCone()
    {
        List<EnemyController> enemiesInCone = GetEnemiesInFrontCone();

        foreach (EnemyController enemy in enemiesInCone)
        {
            if (enemy == null) continue;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, enemy.transform.position);

            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(enemy.transform.position, 0.5f);
        }
    }

    #endregion
}
