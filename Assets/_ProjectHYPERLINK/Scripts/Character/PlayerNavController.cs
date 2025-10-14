using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Diablo 스타일 클릭 이동 및 전투 컨트롤러 (PlayerCharacter 리팩토링 반영)
/// 
/// 주요 변경사항:
/// - PlayerCharacter.GetMainStat() 사용
/// - PlayerCharacter.CurrentStats 접근 방식 업데이트
/// 
/// 핵심 기능:
/// - 지형 클릭: 해당 위치로 이동 (NavMeshAgent)
/// - 적 클릭: 적에게 이동 후 자동 공격
/// - 대시 공격: Root Motion 돌진
/// - 데미지 계산: 스탯 + 크리티컬
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
    private Transform _currentTarget = null;
    private bool _isOnCooldown = false;

    [Header("애니메이션 설정")]
    [SerializeField] private float _animationDampTime = 0.1f;
    [SerializeField] private float _attackAnimationDuration = 1.0f;

    [Header("전투 설정")]
    [SerializeField] private float _attackRange = 2.5f;
    [SerializeField] private float _attackDamage = 25f;
    [SerializeField] private float _attackCooldown = 1.5f;
    [SerializeField] private LayerMask _enemyLayer = 1;

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

    private void Update()
    {
        HandleMouseInput();
        UpdateAnimator();

        if (_currentTarget != null)
        {
            FollowTarget();
        }
    }

    /// <summary>
    /// 마우스 입력 처리
    /// </summary>
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 적 클릭
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, _enemyLayer))
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    _currentTarget = hit.collider.transform;
                    _agent.SetDestination(_currentTarget.position);
                }
            }
            // 지형 클릭
            else if (Input.GetMouseButtonDown(0))
            {
                _currentTarget = null;

                if (Physics.Raycast(ray, out hit))
                {
                    _agent.SetDestination(hit.point);
                }
            }
        }
    }

    /// <summary>
    /// 타겟 추적 및 자동 공격
    /// </summary>
    private void FollowTarget()
    {
        if (_currentTarget == null)
            return;

        if (!_isAttacking && !_agent.isStopped)
        {
            _agent.SetDestination(_currentTarget.position);
        }

        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);

        if (distanceToTarget <= _attackRange && !_isAttacking && !_isOnCooldown)
        {
            StartCoroutine(PerformDashAttack());
        }
    }

    /// <summary>
    /// 애니메이터 업데이트
    /// </summary>
    private void UpdateAnimator()
    {
        if (_isAttacking)
            return;

        float currentSpeed = _agent.velocity.magnitude / _agent.speed;
        _animator.SetFloat(SPEED_HASH, currentSpeed, _animationDampTime, Time.deltaTime);
    }

    /// <summary>
    /// 대시 공격 코루틴
    /// </summary>
    private IEnumerator PerformDashAttack()
    {
        _isAttacking = true;

        // 1. 이동 멈춤
        _agent.isStopped = true;
        _agent.ResetPath();

        // 2. 타겟 방향으로 회전
        if (_currentTarget != null)
        {
            Vector3 dir = (_currentTarget.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        // 3. Root Motion 활성화 + 공격 애니메이션
        _animator.applyRootMotion = true;
        _animator.SetTrigger(ATTACK_HASH);

        // 4. 50% 지점까지 대기 (돌진)
        yield return new WaitForSeconds(_attackAnimationDuration * ATTACK_DAMAGE_TIMING);

        // 5. 데미지 적용
        if (_currentTarget != null)
        {
            Enemy enemyScript = _currentTarget.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                float totalDamage = GetCalculatedDamage();
                enemyScript.TakeDamage(totalDamage);
            }
        }

        // 6. 나머지 애니메이션 재생
        yield return new WaitForSeconds(_attackAnimationDuration * (1f - ATTACK_DAMAGE_TIMING));

        // 7. 정리
        _animator.applyRootMotion = false;
        _agent.isStopped = false;
        _agent.Warp(transform.position);
        _isAttacking = false;

        // 8. 쿨다운 시작
        StartCoroutine(AttackCooldownTimer());
    }

    /// <summary>
    /// 데미지 계산 (Diablo 3 공식)
    /// 
    /// 공식:
    /// 1. 기본 데미지
    /// 2. 주요 스탯 보너스 (1% per point)
    /// 3. 크리티컬 체크
    /// </summary>
    private float GetCalculatedDamage()
    {
        // 1. 기본 데미지
        float baseValue = _attackDamage;

        // 2. 주요 스탯 가져오기 (직업별)
        int mainStat = _playerCharacter != null ? _playerCharacter.GetMainStat() : 0;

        // 3. 스탯으로 데미지 증폭
        float valueWithStat = baseValue * (1f + mainStat / 100f);
        float finalDamage = valueWithStat;

        // 4. 크리티컬 스탯
        float critChance = _playerCharacter?.CurrentStats.CriticalChance ?? 0f;
        float critDamage = _playerCharacter?.CurrentStats.CriticalDamage ?? 0f;

        // 5. 크리티컬 체크
        if (Random.Range(0f, 100f) < critChance)
        {
            finalDamage *= (1f + critDamage / 100f);
            Debug.Log("크리티컬 히트!");
        }

        return finalDamage;
    }

    /// <summary>
    /// 공격 쿨다운 타이머
    /// </summary>
    private IEnumerator AttackCooldownTimer()
    {
        _isOnCooldown = true;
        yield return new WaitForSeconds(_attackCooldown);
        _isOnCooldown = false;
    }

    /// <summary>
    /// 공격 범위 시각화 (Scene 뷰)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}