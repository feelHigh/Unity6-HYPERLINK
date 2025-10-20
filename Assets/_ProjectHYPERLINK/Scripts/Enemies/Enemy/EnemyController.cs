using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 기본 적 클래스
/// 
/// </summary>
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("----- 컴포넌트 -----")]
    [SerializeField] EnemyData _data;
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] Transform _target;
    [SerializeField] Animator _animator;
    
    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Dead
    }
    [SerializeField] EnemyState _curState = EnemyState.Patrol;
    public EnemyState CurState => _curState;

    [Header("----- 전투 -----")]
    [SerializeField] LayerMask _playerLayerMask;
    [SerializeField] SpecialAttackBase _specialAttack;

    [Header(" ----- 드랍 설정 -----")]
    [SerializeField] private ItemDropTableData _dropTable;         // 아이템 드랍 테이블
    [SerializeField][Range(0f, 1f)] private float _dropChance = 0.5f;  // 드랍 확률 (50%)

    // 현재 속한 그룹 //
    [SerializeField] EnemyGroup _group;

    // 현재 상태 스탯 //
    bool _isEpic = false;
    float _maxHp;
    float _curHp;

    float _atk;
    float _attackRange;
    float _patrolRadius;

    int _expReward;
    int _goldReward;

    // 타이머 및 쿨타임 //
    float _patrolWaitTimer;
    float _lastAttackTime;
    float _lastSpecialAttackTime;

    // 애니메이터 파라미터 해시값 //
    private readonly int _hashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private readonly int _hashAttack = Animator.StringToHash("Attack");
    private readonly int _hashSpecialAttack = Animator.StringToHash("SpecialAttack");
    private readonly int _hashSpecialAttackID = Animator.StringToHash("SpecialAttackID");
    private readonly int _hashTakeHit = Animator.StringToHash("TakeHit");
    private readonly int _hashDie = Animator.StringToHash("Die");

    private void Awake()
    {
        _curState = EnemyState.Patrol;
    }

    /// <summary>
    /// Enemy를 초기화하는 함수
    /// </summary>
    /// <param name="canBeEpic"></param>
    public void Initialize(bool isEpic, SpecialAttackBase specialAttack)
    {
        _group = transform.parent.GetComponent<EnemyGroup>();

        //에픽 여부
        _isEpic = isEpic;
        _specialAttack = specialAttack;

        //스탯 초기화
        _maxHp = _data.MaxHp * (_isEpic ? _data.EpicHpMultiplier : 1);
        _curHp = _maxHp;

        _atk = _data.Atk * (_isEpic ? _data.EpicAtkMultiplier : 1);
        _attackRange = _data.AttackRange;
        _patrolRadius = _data.PatrolRadius;
        _agent.speed = _data.MoveSpeed;
        _agent.stoppingDistance = 0.25f;

        _expReward = _data.RewardExp * (_isEpic ? _data.EpicExpMultiplier : 1);
        _goldReward = _data.RewardGold * (_isEpic ? _data.EpicGoldMultiplier : 1);

        if (_isEpic)
        {
            transform.localScale *= 1.2f;
            Debug.Log($"{name}이(가) {_specialAttack.Type}타입 에픽 몬스터로 등장!");

            //에픽 몬스터 이펙트 (오브) 생성
            if (_specialAttack != null && _specialAttack.EpicEffect != null)
            {
                int orbCount = 2;
                float radius = 1.5f;

                for (int i = 0; i < orbCount; i++)
                {
                    float angle = i * Mathf.PI;

                    GameObject orbGO = Instantiate(_specialAttack.EpicEffect, transform);
                    orbGO.transform.localRotation = Quaternion.identity;

                    EffectOrbit orb = orbGO.GetComponent<EffectOrbit>();

                    if (orb == null)
                    {
                        orbGO.AddComponent<EffectOrbit>();
                    }

                    orb.Initialize(transform, 90f, radius, angle * Mathf.Rad2Deg);
                }
            }
        }
    }

    void Update()
    {
        if (_curState == EnemyState.Dead) return;

        _animator.SetFloat(_hashMoveSpeed, _agent.velocity.magnitude / _agent.speed);

        switch (_curState)
        {
            case EnemyState.Patrol:
                UpdatePatrolState();
                break;
            case EnemyState.Chase:
                UpdateChaseState();
                break;
            case EnemyState.Attack:
                UpdateAttackState();
                break;
            default:
                break;
        }
    }

    // 상태 별 행동 함수 //

    /// <summary>
    /// 배회 상태일 때 실행되는 함수
    /// </summary>
    void UpdatePatrolState()
    {
        //감지 범위 내에서 플레이어를 찾는다.
        Collider[] colliders = Physics.OverlapSphere(transform.position, _data.DetectionRange, _playerLayerMask);

        //플레이어를 찾으면
        if (colliders.Length > 0)
        {
            //플레이어를 타겟으로 설정
            _target = colliders[0].transform;

            //상태를 추격 상태로 바꾸기
            ChangeState(EnemyState.Chase);

            //그룹에 타겟 공유
            if (_group != null)
            {
                _group.ShareAggro(_target);
            }

            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _patrolWaitTimer += Time.deltaTime;
            if (_patrolWaitTimer >= _data.PatrolWaitTime)
            {
                SetNewPatrolPoint();
                _patrolWaitTimer = 0;
            }
        }
    }

    /// <summary>
    /// 그룹의 명령을 받아 추격을 시작하는 함수
    /// </summary>
    /// <param name="target"></param>
    public void ActivateChase(Transform target)
    {
        if (_curState == EnemyState.Patrol)
        {
            _target = target;
            ChangeState(EnemyState.Chase);
        }
    }

    /// <summary>
    /// 배회 지점을 새로 설정하는 함수
    /// </summary>
    void SetNewPatrolPoint()
    {
        Vector3 ranDir = Random.insideUnitSphere * _patrolRadius;
        ranDir += transform.position;
        if (NavMesh.SamplePosition(ranDir, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// 추격 상태일 때 실행되는 함수
    /// </summary>
    void UpdateChaseState()
    {
        //타겟이 null이면
        if (_target == null)
        {
            //상태를 배회 상태로 바꾸고 리턴
            ChangeState(EnemyState.Patrol);
            return;
        }

        NavMeshPath path = new NavMeshPath();
        if (_agent.CalculatePath(_target.position, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            //타겟을 따라가도록 설정
            _agent.isStopped = false;
            _agent.SetDestination(_target.position);

            //자신과 타겟 사이의 거리 구하기
            float distance = Vector3.Distance(transform.position, _target.position);

            //자신과 타겟 사이의 거리가 감지 범위보다 크다면
            if (distance > _data.ChaseDistance)
            {
                Debug.Log("타겟을 찾을 수 없음. 추격 중지");

                //상태를 배회 상태로 바꾸고 리턴
                ChangeState(EnemyState.Patrol);
                return;
            }

            //자신과 타겟 사이의 거리가 공격 범위보다 작다면
            if (distance <= _attackRange)
            {
                //상태를 공격 상태로 변경
                ChangeState(EnemyState.Attack);
            }
        }
        else
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    /// <summary>
    /// 공격 상태일 때 실행되는 함수
    /// </summary>
    void UpdateAttackState()
    {
        if (_target == null)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        //타겟을 바라보도록 설정
        transform.LookAt(_target);
        //위치는 제자리 고정
        _agent.SetDestination(transform.position);
        _agent.isStopped = true;

        //자신과 타겟 사이의 거리 구하기
        float distance = Vector3.Distance(transform.position, _target.position);
        
        //만약 자신과 타겟 사이의 거리가 공격 범위보다 크다면
        if (distance > _attackRange)
        {
            //상태를 추격 상태로 바꾸고 리턴
            ChangeState(EnemyState.Chase);
            return;
        }

        //만약 에픽 몬스터이고, 특수 공격 쿨타임이 다 찼다면
        if (_isEpic && _specialAttack != null && Time.time >= _lastSpecialAttackTime + _specialAttack.CoolTime)
        {
            //특수 공격 실행
            PerformSpecialAttack();
        }
        //공격 쿨타임이 다 찼다면
        else if (Time.time >= _lastAttackTime + _data.AttackCoolTime)
        {
            //일반 공격 실행
            PerformBasicAttack();
        }
    }

    /// <summary>
    /// 특수 공격을 실행하는 함수
    /// </summary>
    void PerformSpecialAttack()
    {
        _lastSpecialAttackTime = Time.time;

        _animator.SetTrigger(_hashSpecialAttack);
        _animator.SetInteger(_hashSpecialAttackID, _specialAttack.SpecialAttackAnim);

        _specialAttack.Execute(transform, _target);
    }

    /// <summary>
    /// 일반 공격을 실행하는 함수
    /// </summary>
    void PerformBasicAttack()
    {
        _lastAttackTime = Time.time;
        _animator.SetTrigger(_hashAttack);

        IMonsterDamageable damageable = _target.GetComponent<IMonsterDamageable>();
        
        if (damageable != null)
        {
            damageable.TakeDamage(_atk);
            Debug.Log("공격!! 데미지 : " + _atk);
        }
    }

    /// <summary>
    /// 상태를 바꾸는 함수
    /// </summary>
    /// <param name="state"></param>
    void ChangeState(EnemyState state)
    {
        if (_curState == state) return;
        _curState = state;

        //상태가 배회 상태로 변경되면
        if (_curState == EnemyState.Patrol)
        {
            //타겟을 비우고
            _target = null;
            //네이게이션 경로 초기화
            _agent.ResetPath();
        }
    }

    public void TakeDamage(float damage)
    {
        //현재 상태가 죽음 상태면 리턴
        if (_curState == EnemyState.Dead) return;

        //현재 체력을 데미지 만큼 감소
        _curHp -= damage;

        //애니메이션 재생
        _animator.SetTrigger(_hashTakeHit);

        //현재 체력이 0보다 작거나 같으면
        if (_curHp <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        //현재 상태가 죽음 상태면 리턴
        if (_curState == EnemyState.Dead) return;

        //상태를 죽음 상태로 바꾸기
        ChangeState(EnemyState.Dead);

        //보상 지급
        //경험치
        ExperienceManager playerExperienceManager = FindFirstObjectByType<ExperienceManager>();
        if (playerExperienceManager != null)
        {
            playerExperienceManager.GainExperience(_expReward);
        }
        //골드
        PlayerStatus player = _target.GetComponent<PlayerStatus>();
        if (player != null)
        {
            player.AddGold(_goldReward);
        }

        //아이템 드랍
        // ItemSpawner와 드랍 테이블이 설정되어 있는지 확인
        if (ItemSpawner.Instance != null && _dropTable != null)
        {
            // 확률 체크 (0~1 사이 랜덤 값)
            if (UnityEngine.Random.Range(0f, 1f) < _dropChance)
            {
                // 적의 현재 위치에 아이템 드랍
                ItemSpawner.Instance.SpawnItem(transform.position, _dropTable);
                Debug.Log($"아이템 드랍!");
            }
        }

        //파괴 대신 
        //네비게이션 중지
        _agent.isStopped = true;
        //콜라이더 비활성화
        GetComponent<Collider>().enabled = false;

        //애니메이션 재생
        _animator.SetTrigger(_hashDie);
    }

    /// <summary>
    /// Gizmo를 사용한 체력바 시각화
    /// 
    /// Scene 뷰에서 실시간으로 체력 표시:
    /// - 적 머리 위 2.5유닛 위치에 체력바
    /// - 배경: 빨간색 (최대 체력)
    /// - 전경: 초록색 (현재 체력)
    /// - 체력이 줄어들면 초록색 바가 짧아짐
    /// 
    /// 시각화 구조:
    /// - 배경 바: 1유닛 너비 × 0.1유닛 높이 (빨간색)
    /// - 전경 바: (현재체력/최대체력) 비율로 너비 조절 (초록색)
    /// - 전경 바는 약간 더 두껍게 (0.12유닛) 표시
    /// 
    /// 작동 조건:
    /// - Application.isPlaying = true (게임 실행 중에만)
    /// - 에디터 전용 (빌드에는 미포함)
    /// 
    /// 사용 방법:
    /// - Play 모드 진입
    /// - Scene 뷰에서 적 선택
    /// - 머리 위 체력바 확인
    /// - 데미지 받을 때 실시간으로 감소하는 것 확인
    /// </summary>
    private void OnDrawGizmos()
    {
        // 게임 실행 중에만 그리기
        if (Application.isPlaying)
        {
            // 체력바 위치 (적 머리 위 2.5유닛)
            Vector3 healthBarPosition = transform.position + Vector3.up * 2.5f;

            // 체력 비율 계산 (0~1)
            float healthPercentage = _curHp / _maxHp;

            // === 배경 (빨간색 - 최대 체력) ===
            Gizmos.color = Color.red;
            Gizmos.DrawCube(healthBarPosition, new Vector3(1f, 0.1f, 0.1f));

            // === 전경 (초록색 - 현재 체력) ===
            // 체력 비율에 따라 너비 조절
            Gizmos.color = Color.green;
            // X축 오프셋 계산 (왼쪽 정렬)
            // 체력이 50%면 0.25유닛 왼쪽으로 이동
            Gizmos.DrawCube(
                healthBarPosition - Vector3.right * (0.5f - healthPercentage * 0.5f),
                new Vector3(healthPercentage, 0.12f, 0.12f)
            );
        }
    }
}
