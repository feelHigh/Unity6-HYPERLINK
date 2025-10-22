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
/// - 피격/사망 애니메이션 지원
/// - 스킬 실행 중 이동 제한
/// - PlayerCharacter 이벤트 구독
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

    private bool _isAttacking = false;
    private bool _isPerformingSkill = false;
    private bool _isDead = false;
    private Transform _currentTarget = null;
    private bool _isOnCooldown = false;

    // 상호작용 시스템
    private IInteractable _pendingInteraction;
    private Coroutine _interactionCoroutine;

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

    [Header("레이어 설정")]
    [SerializeField] private LayerMask _groundLayer = ~0;

    #region 초기화

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _mainCamera = Camera.main;
        _playerCharacter = GetComponent<PlayerCharacter>();
    }

    private void Start()
    {
        _animator.applyRootMotion = false;
        _agent.stoppingDistance = _attackRange;
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
    /// - 모든 이동/공격 정지
    /// - NavMeshAgent 비활성화
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
    /// - 스킬 실행 중 플래그 설정
    /// - NavMeshAgent는 SkillAnimationController에서 제어
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

        // 스킬 애니메이션이 끝날 때까지 대기 (대략적인 시간)
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
        // 사망 시 모든 입력 무시
        if (_isDead) return;

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
        // 스킬 실행 중에는 입력 무시
        if (_isPerformingSkill) return;

        // 좌클릭: 이동 & 상호작용
        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
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
            lookDirection.y = 0; // y축 회전만

            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            // 전방 원뿔 범위 내 적 탐색
            List<Transform> enemiesInFront = GetEnemiesInFrontCone();

            if (enemiesInFront.Count > 0)
            {
                // 범위 내 모든 적 공격
                PerformMultiAttack(enemiesInFront);
            }
            else
            {
                // 적 없으면 헛스윙
                PerformAttack(null);
            }
        }
    }

    /// <summary>
    /// 전방 원뿔 범위 내 적 탐색
    /// 
    /// 작동 방식:
    /// 1. OverlapSphere로 범위 내 모든 적 찾기
    /// 2. Vector3.Angle로 플레이어 전방과의 각도 계산
    /// 3. 각도가 _attackAngle/2 이하인 적만 반환
    /// </summary>
    private List<Transform> GetEnemiesInFrontCone()
    {
        List<Transform> validEnemies = new List<Transform>();

        // 범위 내 모든 적 찾기
        Collider[] enemies = Physics.OverlapSphere(transform.position, _attackRange, _enemyLayer);

        foreach (Collider enemy in enemies)
        {
            // 적 방향 벡터 계산 (수평만)
            Vector3 directionToEnemy = enemy.transform.position - transform.position;
            directionToEnemy.y = 0;

            // 전방과의 각도 계산
            float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);

            // 원뿔 범위 내에 있는지 체크
            if (angleToEnemy <= _attackAngle / 2f)
            {
                validEnemies.Add(enemy.transform);
            }
        }

        return validEnemies;
    }

    #endregion

    #region 상호작용 시스템

    /// <summary>
    /// 목표까지 이동 후 상호작용 실행
    /// </summary>
    private IEnumerator MoveAndInteract(GameObject target, IInteractable interactable)
    {
        _pendingInteraction = interactable;
        float interactionRange = interactable.GetInteractionRange();

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

        StartCoroutine(AttackSequence(target));
    }

    /// <summary>
    /// 다중 공격 (범위 내 모든 적)
    /// </summary>
    private void PerformMultiAttack(List<Transform> targets)
    {
        if (_isAttacking || _isOnCooldown || _isDead) return;

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

        // 데미지 타이밍까지 대기
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

        // 애니메이션 종료 대기
        yield return new WaitForSeconds(_attackAnimationDuration - ATTACK_DAMAGE_TIMING);

        _isAttacking = false;

        // ⭐ 사망 시 NavMeshAgent 재활성화하지 않음
        if (!_isDead)
        {
            _agent.isStopped = false;
        }

        // 쿨다운 대기
        yield return new WaitForSeconds(_attackCooldown - _attackAnimationDuration);

        _isOnCooldown = false;
    }

    /// <summary>
    /// 다중 공격 시퀀스
    /// 
    /// 타이밍:
    /// 0.0s: 애니메이션 시작
    /// 0.5s: 모든 적에게 데미지 적용
    /// 1.0s: 애니메이션 종료, 이동 가능
    /// 1.5s: 쿨다운 종료, 재공격 가능
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

        // 사망 시 NavMeshAgent 재활성화하지 않음
        if (!_isDead)
        {
            _agent.isStopped = false;
        }

        yield return new WaitForSeconds(_attackCooldown - _attackAnimationDuration);

        _isOnCooldown = false;
    }

    /// <summary>
    /// 데미지 계산
    /// 
    /// 공식: 기본 데미지 × (1 + 주요스탯/100) × 크리티컬 배율
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

        return damage;
    }

    /// <summary>
    /// 타겟 추적 (적 클릭 시)
    /// </summary>
    private void FollowTarget()
    {
        if (_currentTarget == null || _isDead)
            return;

        float distance = Vector3.Distance(transform.position, _currentTarget.position);

        if (distance > _attackRange && !_isAttacking)
        {
            _agent.SetDestination(_currentTarget.position);
        }
        else if (distance <= _attackRange && !_isAttacking && !_isOnCooldown)
        {
            // 범위 내 도달 시 전방 원뿔 공격
            List<Transform> enemies = GetEnemiesInFrontCone();
            if (enemies.Count > 0)
            {
                PerformMultiAttack(enemies);
            }
        }
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

    /// <summary>
    /// 씬 뷰에서 공격 범위 시각화
    /// 
    /// 표시 내용:
    /// - 빨간 원: 공격 범위
    /// - 노란 원뿔: 공격 각도
    /// - 빨간 선/구체: 공격 가능한 적
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Vector3 position = transform.position;

        // 1. 공격 범위 구체 (외곽선)
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(position, _attackRange);

        // 2. 공격 원뿔 그리기
        DrawAttackCone(position);

        // 3. 범위 내 적 강조
        DrawEnemiesInCone();
    }

    /// <summary>
    /// 공격 원뿔 시각화
    /// </summary>
    private void DrawAttackCone(Vector3 position)
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);

        Vector3 forward = transform.forward * _attackRange;
        int segments = 20; // 원뿔 부드러움
        float angleStep = _attackAngle / segments;

        // 원뿔 외곽선 그리기
        Vector3 prevPoint = position + Quaternion.Euler(0, -_attackAngle / 2f, 0) * forward;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -_attackAngle / 2f + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
            Vector3 point = position + direction;

            // 원뿔 가장자리 연결
            Gizmos.DrawLine(prevPoint, point);

            // 중심에서 가장자리로 선 (5개마다)
            if (i % 5 == 0)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
                Gizmos.DrawLine(position, point);
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            }

            prevPoint = point;
        }

        // 원뿔 경계선 강조
        Gizmos.color = Color.yellow;
        Vector3 leftBound = Quaternion.Euler(0, -_attackAngle / 2f, 0) * forward;
        Vector3 rightBound = Quaternion.Euler(0, _attackAngle / 2f, 0) * forward;
        Gizmos.DrawLine(position, position + leftBound);
        Gizmos.DrawLine(position, position + rightBound);
    }

    /// <summary>
    /// 공격 가능한 적 강조 표시
    /// </summary>
    private void DrawEnemiesInCone()
    {
        List<Transform> enemiesInCone = GetEnemiesInFrontCone();

        foreach (Transform enemy in enemiesInCone)
        {
            if (enemy == null) continue;

            // 플레이어에서 적으로 선
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, enemy.position);

            // 적 위치에 구체
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(enemy.position, 0.5f);
        }
    }

    #endregion
}
