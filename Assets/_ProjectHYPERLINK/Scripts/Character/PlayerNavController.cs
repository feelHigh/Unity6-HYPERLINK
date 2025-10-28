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
/// 
/// 최근 변경사항:
/// - PlayerStateController 연동 추가
/// - 상태이상 체크 (이동/공격 제어)
/// - ForceStop() 메소드 추가 (빙결/속박)
/// - ApplyKnockback() 메소드 추가 (넉백)
/// - 이동속도 배율 적용
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PlayerNavController : MonoBehaviour
{
    private static readonly int SPEED_HASH = Animator.StringToHash("Speed");
    private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    private const float ATTACK_DAMAGE_TIMING = 0.5f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private Camera _mainCamera;
    private PlayerCharacter _playerCharacter;
    private PlayerStateController _stateController;

    private bool _isAttacking = false;
    private bool _isPerformingSkill = false;
    private bool _isDead = false;
    private Transform _currentTarget = null;
    private bool _isOnCooldown = false;

    // 상호작용 시스템
    private IInteractable _pendingInteraction;
    private Coroutine _interactionCoroutine;

    // 기본 이동속도 저장
    private float _baseSpeed;

    [Header("애니메이션 설정")]
    [SerializeField] private float _animationDampTime = 0.1f;
    [SerializeField] private float _attackAnimationDuration = 1.0f;

    [Header("전투 설정")]
    [Tooltip("공격 범위 (미터)")]
    [SerializeField] private float _attackRange = 1f;

    [Tooltip("공격 각도 (전방 원뿔 범위, 90° = 전방 1/4 원)")]
    [SerializeField] private float _attackAngle = 90f;

    [SerializeField] private float _attackDamage = 25f;
    [SerializeField] private float _attackCooldown = 1.5f;
    [SerializeField] private LayerMask _enemyLayer = 1;

    [Header("넉백 설정")]
    [SerializeField] private float _knockbackDuration = 0.3f;

    [Header("레이어 설정")]
    [SerializeField] private LayerMask _groundLayer = ~0;

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

        // 기본 이동속도 저장
        _baseSpeed = _agent.speed;
    }

    private void Start()
    {
        _animator.applyRootMotion = false;
    }

    private void OnEnable()
    {
        // PlayerCharacter 이벤트 구독
        PlayerCharacter.OnPlayerDead += HandlePlayerDead;

        // SkillActivationSystem 이벤트 구독
        SkillActivationSystem.OnSkillExecuted += HandleSkillExecuted;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayerCharacter.OnPlayerDead -= HandlePlayerDead;
        SkillActivationSystem.OnSkillExecuted -= HandleSkillExecuted;
    }

    #endregion

    #region 이벤트 핸들러

    /// <summary>
    /// 플레이어 사망 시 처리
    /// </summary>
    private void HandlePlayerDead()
    {
        _isDead = true;

        // NavMeshAgent 완전 정지
        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        // 실행 중인 코루틴 정지
        StopAllCoroutines();

        _isAttacking = false;
        _currentTarget = null;
        _pendingInteraction = null;
        _interactionCoroutine = null;

        Debug.Log("[PlayerNavController] 사망 - 모든 행동 정지");
    }

    /// <summary>
    /// 스킬 실행 시 처리
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

    #endregion

    private void Update()
    {
        if (_isDead) return;

        // 이동속도 배율 적용
        UpdateMovementSpeed();

        HandleMouseInput();
        UpdateAnimator();

        if (_currentTarget != null)
        {
            FollowTarget();
        }
    }

    #region 마우스 입력

    private void HandleMouseInput()
    {
        // 스킬 실행 중이거나 이동 불가 상태면 입력 무시
        if (_isPerformingSkill) return;

        // 이동 불가 상태 체크 (빙결, 속박, 넉다운)
        if (_stateController != null && !_stateController.CanMove)
        {
            return;
        }

        // 좌클릭: 이동 & 상호작용
        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        // ✅ 공격 불가 상태 체크
        if (_stateController != null && !_stateController.CanAttack)
        {
            return;
        }

        // 우클릭: 회전 & 공격
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
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
                // 상호작용 대상으로 이동
                _agent.SetDestination(hit.point);
                _currentTarget = null;

                // 기존 상호작용 취소
                if (_interactionCoroutine != null)
                {
                    StopCoroutine(_interactionCoroutine);
                }

                _interactionCoroutine = StartCoroutine(MoveAndInteract(hit.collider.gameObject, interactable));
                return;
            }
        }

        // 2순위: 지형 이동
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _groundLayer))
        {
            _agent.SetDestination(hit.point);
            _currentTarget = null;

            // 상호작용 취소
            if (_interactionCoroutine != null)
            {
                StopCoroutine(_interactionCoroutine);
                _interactionCoroutine = null;
                _pendingInteraction = null;
            }
        }
    }

    /// <summary>
    /// 우클릭 처리: 전방 원뿔 범위 내 모든 적 공격
    /// </summary>
    private void HandleRightClick()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // 클릭 위치 바라보기
            Vector3 lookDirection = hit.point - transform.position;
            lookDirection.y = 0;

            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            // 전방 원뿔 범위 내 적 탐색
            List<Transform> enemiesInFront = GetEnemiesInFrontCone();

            if (enemiesInFront.Count > 0)
            {
                PerformMultiAttack(enemiesInFront);
            }
            else
            {
                PerformAttack(null);
            }
        }
    }

    /// <summary>
    /// 전방 원뿔 범위 내 적 탐색
    /// </summary>
    private List<Transform> GetEnemiesInFrontCone()
    {
        List<Transform> validEnemies = new List<Transform>();

        Collider[] enemies = Physics.OverlapSphere(transform.position, _attackRange, _enemyLayer);

        foreach (Collider enemy in enemies)
        {
            Vector3 directionToEnemy = enemy.transform.position - transform.position;
            directionToEnemy.y = 0;

            float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);

            if (angleToEnemy <= _attackAngle / 2f)
            {
                validEnemies.Add(enemy.transform);
            }
        }

        return validEnemies;
    }

    #endregion

    #region 상호작용

    /// <summary>
    /// 목표 지점으로 이동 후 상호작용
    /// </summary>
    private IEnumerator MoveAndInteract(GameObject target, IInteractable interactable)
    {
        _pendingInteraction = interactable;
        float interactionRange = 1.5f;

        // 목표 범위 도달까지 대기
        while (Vector3.Distance(transform.position, target.transform.position) > interactionRange)
        {
            // 이동 중 취소 체크
            if (_pendingInteraction == null || _isDead)
                yield break;

            yield return null;
        }

        // 도착 후 상호작용
        if (_pendingInteraction != null && !_isDead)
        {
            interactable.Interact(_playerCharacter);
            _pendingInteraction = null;
        }

        _interactionCoroutine = null;
    }

    #endregion

    #region 공격 시스템

    /// <summary>
    /// 단일 공격 (헛스윙용)
    /// </summary>
    private void PerformAttack(Transform target)
    {
        if (_isAttacking || _isOnCooldown || _isDead) return;

        // 공격 불가 상태 체크
        if (_stateController != null && !_stateController.CanAttack)
        {
            Debug.Log("[공격] 공격 불가 상태");
            return;
        }

        StartCoroutine(AttackSequence(target));
    }

    /// <summary>
    /// 다중 공격 (범위 내 모든 적)
    /// </summary>
    private void PerformMultiAttack(List<Transform> targets)
    {
        if (_isAttacking || _isOnCooldown || _isDead) return;

        // 공격 불가 상태 체크
        if (_stateController != null && !_stateController.CanAttack)
        {
            Debug.Log("[공격] 공격 불가 상태");
            return;
        }

        StartCoroutine(MultiAttackSequence(targets));
    }

    /// <summary>
    /// 단일 공격 시퀀스
    /// </summary>
    private IEnumerator AttackSequence(Transform target)
    {
        _isAttacking = true;
        _isOnCooldown = true;

        _agent.isStopped = true;
        _animator.SetTrigger(ATTACK_HASH);

        yield return new WaitForSeconds(ATTACK_DAMAGE_TIMING);

        // 데미지 적용
        if (target != null && !_isDead)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float damage = CalculateDamage();
                damageable.TakeDamage(damage);
            }
        }

        yield return new WaitForSeconds(_attackAnimationDuration - ATTACK_DAMAGE_TIMING);

        _isAttacking = false;

        if (!_isDead)
        {
            _agent.isStopped = false;
        }

        yield return new WaitForSeconds(_attackCooldown - _attackAnimationDuration);

        _isOnCooldown = false;
    }

    /// <summary>
    /// 다중 공격 시퀀스
    /// </summary>
    private IEnumerator MultiAttackSequence(List<Transform> targets)
    {
        _isAttacking = true;
        _isOnCooldown = true;

        _agent.isStopped = true;
        _animator.SetTrigger(ATTACK_HASH);

        yield return new WaitForSeconds(ATTACK_DAMAGE_TIMING);

        // 모든 적에게 데미지 적용
        if (!_isDead)
        {
            float damage = CalculateDamage();
            int hitCount = 0;

            foreach (Transform target in targets)
            {
                if (target == null) continue;

                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    hitCount++;
                }
            }

            if (hitCount > 0)
            {
                Debug.Log($"{hitCount}명의 적 공격!");
            }
        }

        yield return new WaitForSeconds(_attackAnimationDuration - ATTACK_DAMAGE_TIMING);

        _isAttacking = false;

        if (!_isDead)
        {
            _agent.isStopped = false;
        }

        yield return new WaitForSeconds(_attackCooldown - _attackAnimationDuration);

        _isOnCooldown = false;
    }

    /// <summary>
    /// 데미지 계산 (방어력 약화 적용)
    /// </summary>
    private float CalculateDamage()
    {
        // 주요 스탯 보너스 적용
        int mainStat = _playerCharacter.GetMainStat();
        float damage = _attackDamage * (1f + mainStat / 100f);

        // 크리티컬 판정
        CharacterStats stats = _playerCharacter.CurrentStats;
        if (Random.Range(0f, 100f) < stats.CriticalChance)
        {
            damage *= (1f + stats.CriticalDamage / 100f);
            Debug.Log("크리티컬 히트!");
        }

        // 방어력 약화 상태 체크
        if (_stateController != null && _stateController.IsWeakened)
        {
            float defenseMultiplier = _stateController.GetDefenseMultiplier();
            // TODO: 적의 방어력 계산에 적용 (현재는 플레이어 데미지만 계산)
            Debug.Log($"[약화] 방어력 배율: {defenseMultiplier:P0}");
        }

        return damage;
    }

    /// <summary>
    /// 타겟 추적
    /// </summary>
    private void FollowTarget()
    {
        if (_currentTarget == null || _isDead)
            return;

        // 이동 불가 상태 체크
        if (_stateController != null && !_stateController.CanMove)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, _currentTarget.position);

        if (distance > _attackRange && !_isAttacking)
        {
            _agent.SetDestination(_currentTarget.position);
        }
        else if (distance <= _attackRange && !_isAttacking && !_isOnCooldown)
        {
            List<Transform> enemies = GetEnemiesInFrontCone();
            if (enemies.Count > 0)
            {
                PerformMultiAttack(enemies);
            }
        }
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

        Debug.Log("[PlayerNavController] 강제 정지");
    }

    /// <summary>
    /// 넉백 적용
    /// </summary>
    public void ApplyKnockback(float knockbackPower)
    {
        if (_isDead || _agent == null) return;

        StartCoroutine(KnockbackCoroutine(knockbackPower));
    }

    /// <summary>
    /// 넉백 코루틴
    /// </summary>
    private IEnumerator KnockbackCoroutine(float power)
    {
        // NavMeshAgent 일시 정지
        bool wasEnabled = _agent.enabled;
        if (wasEnabled)
        {
            _agent.enabled = false;
        }

        // 뒤로 밀려나는 방향 계산
        Vector3 knockbackDirection = -transform.forward;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + (knockbackDirection * power);

        // DOTween으로 부드러운 넉백
        transform.DOMove(targetPosition, _knockbackDuration)
            .SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(_knockbackDuration);

        // NavMeshAgent 재활성화
        if (wasEnabled && !_isDead)
        {
            _agent.enabled = true;
        }

        Debug.Log($"[넉백] {power}m 밀려남");
    }

    /// <summary>
    /// 이동속도 배율 적용
    /// </summary>
    private void UpdateMovementSpeed()
    {
        if (_agent == null || _stateController == null) return;

        float speedMultiplier = _stateController.GetMovementSpeedMultiplier();
        _agent.speed = _baseSpeed * speedMultiplier;
    }

    #endregion

    #region 애니메이션

    private void UpdateAnimator()
    {
        float speed = _agent.velocity.magnitude;
        _animator.SetFloat(SPEED_HASH, speed, _animationDampTime, Time.deltaTime);
    }

    #endregion

    #region 디버그 시각화

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

    private void DrawEnemiesInCone()
    {
        List<Transform> enemiesInCone = GetEnemiesInFrontCone();

        foreach (Transform enemy in enemiesInCone)
        {
            if (enemy == null) continue;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, enemy.position);

            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(enemy.position, 0.5f);
        }
    }

    #endregion
}
