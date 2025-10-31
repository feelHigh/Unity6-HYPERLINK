using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 AI 클래스
///
/// 핵심 기능
/// - 적 이동
/// - 플레이어 탐지
/// - 플레이어 공격
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("----- 참조 -----")]
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] EnemyController _controller;
    [SerializeField] Animator _animator;

    /// <summary>
    /// Enemy 상태 머신 enum
    /// </summary>
    public enum EnemyState
    {
        Idle,       //대기
        Chase,      //추격
        Attack,     //공격
        Dead        //죽음
    }
    [SerializeField] EnemyState _curState = EnemyState.Idle;
    public EnemyState CurState => _curState;

    [Header("----- 전투 -----")]
    [SerializeField] Transform _target;                 //타겟(플레이어) 트랜스폼
    [SerializeField] LayerMask _playerLayerMask;        //플레이어 레이어 마스크
    [SerializeField] LayerMask _enemyLayerMask;        //적 레이어 마스크
    [SerializeField] BasicAttackType _basicAttack;      //일반 공격 타입
    [SerializeField] SpecialAttackBase _specialAttack;  //특수 공격 데이터

    [Header("----- 근거리 기본 공격 -----")]
    [SerializeField] TargetDetector _meleeDetector;    //근거리 타격 지점

    [Header("----- 원거리 기본 공격 -----")]
    [SerializeField] Transform _projectileFirePos;      //발사체 발사(생성) 위치
    [SerializeField] float _heightOffset = 1.1f;        //높이 보정 값

    EnemyData _data;        //적 데이터
    Vector3 _spawnPos;      //스폰될 때의 위치
    bool _isActive = false; //AI 활성화 여부
    bool _isAttacking = false;  //공격 중인지 아닌지 여부

    // 타이머 및 쿨타임 관리 //
    float _lastAttackTime;          //마지막 일반 공격 시간
    float _lastSpecialAttackTime;   //마지막 특수 공격 시간
    float _backToSpawnTimer;        //원위치로 돌아가기까지의 타이머
    float _pathCheckTimer;          //NavMesh 길을 체크하는 타이머
    int _pathCheckCount = 0;        //NavMesh 길을 체크한 횟수

    bool _isFlakingToAttack = false;//우회 이동 중인지 여부
    Vector3 _curFlankTarget;        //현재 우회 목표 위치

    // 애니메이터 파라미터 해시값 //
    private readonly int _hashMoveSpeed = Animator.StringToHash("MoveSpeed");               //이동
    private readonly int _hashAttack = Animator.StringToHash("Attack");                     //일반 공격
    private readonly int _hashSpecialAttack = Animator.StringToHash("SpecialAttack");       //특수 공격
    private readonly int _hashSpecialAttackID = Animator.StringToHash("SpecialAttackID");   //특수 공격 타입에 따른 애니메이션 ID
    private readonly int _hashTakeHit = Animator.StringToHash("TakeHit");                   //피격
    private readonly int _hashDie = Animator.StringToHash("Die");                           //죽음ㄴ

    private void Awake()
    {
        _controller = GetComponent<EnemyController>();

        _controller.OnInitialized += StartAI;
        _controller.OnHit += OnHit;
        _controller.OnDie += OnDeath;

        _spawnPos = transform.position;
    }

    /// <summary>
    /// Enemy가 Initialize된 후 움직이도록 하는 함수
    /// </summary>
    void StartAI()
    {
        _isActive = true;
        _curState = EnemyState.Idle;

        _data = _controller.Data;
        _basicAttack = _data.BasicAttackType;

        _specialAttack = _controller.SpecialAttack;

        if (_agent != null)
        {
            _agent.speed = _data.MoveSpeed;
            float targetStoppingDistance = Mathf.Max(_agent.stoppingDistance, _data.AttackRange * 0.9f);
            _agent.stoppingDistance = targetStoppingDistance;
        }

        if (_basicAttack == BasicAttackType.Melee && _meleeDetector != null)
        {
            _meleeDetector.Initialize(_controller.Atk);
        }
    }

    private void Update()
    {
        if (!_isActive || _curState == EnemyState.Dead) return;

        switch (_curState)
        {
            case EnemyState.Idle:
                UpdateIdleState();
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

        _animator.SetFloat(_hashMoveSpeed, (float)(_agent.velocity.magnitude / _agent.speed));
    }

    #region 상태 별 행동 함수
    /// <summary>
    /// 대기 상태일 때 실행되는 함수
    /// </summary>
    void UpdateIdleState()
    {
        //감지 범위 내에서 플레이어를 찾는다.
        Collider[] colliders = Physics.OverlapSphere(transform.position, _data.DetectionRange, _playerLayerMask);

        //플레이어를 찾으면
        if (colliders.Length > 0)
        {
            //플레이어를 타겟으로 설정
            _target = colliders[0].transform;

            //플레이어를 향한 경로가 유효하면
            if (CheckPath(_target.position))
            {
                //상태를 추격 상태로 바꾸기
                ChangeState(EnemyState.Chase);

                //그룹에 타겟 공유
                if (_controller.Group != null)
                {
                    _controller.Group.ShareAggro(_target);
                }
                return;
            }
        }

        //Idle 상태에서 5초가 지나면
        _backToSpawnTimer += Time.deltaTime;
        if (_backToSpawnTimer >= 5)
        {
            //스폰 위치로 돌아가기
            _agent.SetDestination(_spawnPos);
            _backToSpawnTimer = 0;
        }
    }

    /// <summary>
    /// 그룹의 명령을 받아 추격을 시작하는 함수
    /// </summary>
    /// <param name="target"></param>
    public void ActivateChase(Transform target)
    {
        if (_curState == EnemyState.Idle)
        {
            Debug.Log("ShareAggro! " + transform.localPosition);

            _target = target;
            ChangeState(EnemyState.Chase);
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
            ChangeState(EnemyState.Idle);
            return;
        }

        _agent.isStopped = false;

        //0.5초마다 경로 유효성 확인
        _pathCheckTimer += Time.deltaTime;
        if (_pathCheckTimer >= 0.5f)
        {
            _pathCheckTimer = 0;

            //NavMesh 경로가 막혀있다면
            if (!CheckPath(_target.position))
            {
                //경로 확인 횟수 +1
                _pathCheckCount++;

                //3번 연속 경로 찾기 실패 시
                if (_pathCheckCount >= 3)
                {
                    //상태를 대기 상태로 바꾸고 리턴
                    ChangeState(EnemyState.Idle);
                    _pathCheckCount = 0;
                    return;
                }
            }
            else
            {
                _pathCheckCount = 0;
            }
        }

        //자신과 타겟 사이의 거리 구하기
        float distance = Vector3.Distance(transform.position, _target.position);

        //그룹이 null이 아니고, 어그로 상태가 아닐 때
        if (_controller.Group != null && !_controller.Group.HasAggro)
        {
            //자신과 타겟 사이의 거리가 감지 범위보다 크다면
            if (distance > _data.ChaseDistance)
            {
                Debug.Log("타겟을 찾을 수 없음. 추격 중지");

                //상태를 대기 상태로 바꾸고 리턴
                ChangeState(EnemyState.Idle);
                return;
            }
        }

        //자신과 타겟 사이의 거리가 공격 범위보다 작다면
        if (distance <= _data.AttackRange)
        {
            //시야가 확보 됐을 때
            if (HasLineOfSight(_target))
            {
                //우회 플래그 초기화
                _isFlakingToAttack = false;

                //agent 이동 초기화
                _agent.isStopped = true;
                _agent.ResetPath();

                //상태를 공격 상태로 변경
                ChangeState(EnemyState.Attack);
                return;
            }
            //시야가 막혔을 때
            else
            {
                //처음 시야 막힘 -> 새 우회 위치 탐색
                if (!_isFlakingToAttack)
                {
                    _curFlankTarget = FindFlankPosition(_target.position, _data.AttackRange * 0.8f);
                    _isFlakingToAttack = true;

                    _agent.SetDestination(_curFlankTarget);

                    Debug.Log("우회 위치로 이동 시도");
                }
                //이미 우회 이동 중일 때는 목표 우회 위치 유지
                else
                {
                    float distanceToFlankTarget = Vector3.Distance(transform.position, _curFlankTarget);

                    if (distanceToFlankTarget < 1.5f)
                    {
                        Debug.Log("우회 위치 도착, 새 위치 탐색");

                        //플래그 초기화
                        _isFlakingToAttack = false;
                    }
                }
            }
        }
        else
        {
            _isFlakingToAttack = false;
            _agent.SetDestination(_target.position);
        }
    }

    /// <summary>
    /// 공격 상태일 때 실행되는 함수
    /// </summary>
    void UpdateAttackState()
    {
        //타겟이 null이면 대기 상태로 전환
        if (_target == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        //자신과 타겟 사이의 거리 구하기
        float distance = Vector3.Distance(transform.position, _target.position);

        //만약 자신과 타겟 사이의 거리가 공격 범위보다 크다면 (공격 범위를 벗어났다면)
        if (distance > _data.AttackRange)
        {
            //상태를 추격 상태로 바꾸고 리턴
            ChangeState(EnemyState.Chase);
            return;
        }

        //경로 유효성 확인, 경로가 유효하지 않으면
        if (!CheckPath(_target.position))
        {
            //대기 상태로 변경
            ChangeState(EnemyState.Idle);
            return;
        }

        //시야 확보 여부 체크
        if (!HasLineOfSight(_target))
        {
            Debug.Log("시야 확보 불가능, 추격 상태로 전환");
            ChangeState(EnemyState.Chase);
            return;
        }

        //공격 중이면 리턴
        if (_isAttacking) return;

        //타겟을 바라보도록 설정
        Vector3 lookDir = _target.position - transform.position;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        //위치는 제자리 고정
        _agent.SetDestination(transform.position);
        _agent.isStopped = true;

        //만약 에픽 몬스터이고, 특수 공격 쿨타임이 다 찼다면
        if (_controller.IsEpic && _specialAttack != null && Time.time >= _lastSpecialAttackTime + _specialAttack.CoolTime)
        {
            //특수 공격 실행
            PerformSpecialAttack();
        }
        //일반 공격 쿨타임이 다 찼다면
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
        _lastAttackTime = Time.time;
        _lastSpecialAttackTime = Time.time;

        StartCoroutine(AttackRoutine());

        _animator.SetTrigger(_hashSpecialAttack);
        _animator.SetInteger(_hashSpecialAttackID, _specialAttack.SpecialAttackAnim);
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출할
    /// 특수 공격 실제 실행 함수
    /// </summary>
    public void ExecuteSpecialAttack()
    {
        if (_specialAttack != null && _target != null)
        {
            _specialAttack.Execute(transform, _target, _basicAttack);
        }
    }

    /// <summary>
    /// 일반 공격을 실행하는 함수
    /// </summary>
    void PerformBasicAttack()
    {
        _lastAttackTime = Time.time;

        StartCoroutine(AttackRoutine());

        _animator.SetTrigger(_hashAttack);
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출할
    /// 근거리 타격 감지 콜라이더 활성화하는 함수
    /// </summary>
    public void PerformMeleeAttack()
    {
        if (_meleeDetector != null)
        {
            _meleeDetector.EnableDetector();
        }
        else
        {
            Debug.Log("MeleeDetector 없음");
        }
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출할
    /// 원거리 일반 공격 실행 함수
    /// </summary>
    public void PerformRangedAttack()
    {
        if (_target == null) return;

        //발사 지점이 null이면 기본 발사 지점 구하기
        Vector3 spawnPos = _projectileFirePos != null ? _projectileFirePos.position :
            transform.position + Vector3.up * _heightOffset;

        //목표 지점
        Vector3 targetPos = _target.position + Vector3.up * _heightOffset;

        //방향 구하기
        Vector3 dir = (targetPos - spawnPos).normalized;

        Quaternion spawnRot = Quaternion.LookRotation(dir, Vector3.up);

        //발사체 생성 및 초기화
        GameObject projectileGO = Instantiate(_data.ProjectilePrefab, spawnPos, spawnRot);

        EnemyProjectile projectile = projectileGO.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(_data.ProjectileSpeed, _controller.Atk, _data.AttackRange);
        }
    }

    IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        if (_curState == EnemyState.Attack)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _agent.ResetPath();
        }

        yield return new WaitForSeconds(2f);

        _isAttacking = false;
    }

    /// <summary>
    /// 상태를 바꾸는 함수
    /// </summary>
    /// <param name="state"></param>
    void ChangeState(EnemyState state)
    {
        if (_curState == state) return;

        _curState = state;

        //상태가 대기 상태로 변경되면
        if (_curState == EnemyState.Idle)
        {
            //타겟을 비우고
            _target = null;
            //우회 플래그 초기화
            _isFlakingToAttack = false;
            //네이게이션 경로 초기화
            _agent.ResetPath();

            Debug.Log("대기 상태 전환 완료");
        }
    }

    /// <summary>
    /// 목표 지점까지의 NavMesh 경로가 유효한지 체크하는 함수
    /// </summary>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    bool CheckPath(Vector3 targetPos)
    {
        NavMeshPath path = new NavMeshPath();

        //최단 경로를 찾지 못한 경우 바로 false
        if (!_agent.CalculatePath(targetPos, path))
        {
            return false;
        }

        //찾은 경로가 막혀잇으면 false, 아무 이상 없으면 true
        return path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// 타겟까지 시야가 확보되어 있는지 레이캐스트로 체크하는 함수
    /// </summary>
    /// <param name="target">타겟 (플레이어) 트랜스폼</param>
    /// <returns>시야 확보 여부</returns>
    bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;

        //발사 위치 (적 위치)
        Vector3 startPos = transform.position + Vector3.up * _heightOffset;
        //타겟 위치 (플레이어 위치)
        Vector3 targetPos = target.position + Vector3.up * _heightOffset;

        //방향 및 거리 계산
        Vector3 dir = targetPos - startPos;
        float distance = dir.magnitude;

        int ignoreLayerMask = _playerLayerMask | _enemyLayerMask;

        //만약 발사한 레이캐스트가 플레이어를 제외한 다른 레이어에 맞았다면
        if (Physics.Raycast(startPos, dir.normalized, out RaycastHit hit, distance, ~ignoreLayerMask))
        {
            //시야 막힘 반환
            Debug.DrawRay(startPos, dir.normalized * hit.distance, Color.red, 0.1f);
            return false;
        }

        //장애물 없으면 시야 확보 반환
        Debug.DrawRay(startPos, dir.normalized * distance, Color.green, 0.1f);
        return true;
    }

    /// <summary>
    /// 타겟 주변에서 시야가 확보된 위치를 찾는 함수
    /// </summary>
    /// <param name="targetPos">타겟 위치</param>
    /// <param name="radius">타겟으로부터의 거리</param>
    /// <returns></returns>
    Vector3 FindFlankPosition(Vector3 targetPos, float radius)
    {
        //타겟을 중심으로 이동을 시도할 여러 각도
        float[] angles = { 45f, -45f, 90f, -90f, 135f, -135f, 180f };

        foreach (float angle in angles)
        {
            //타겟 방향 구하기
            Vector3 dirToTarget = (targetPos - transform.position).normalized;

            //해당 각도만큼 회전한 방향 계산
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 offset = rotation * dirToTarget * radius;

            //후보 위치 계산
            Vector3 candidatePos = targetPos + offset;

            //NavMesh에서 유효한 위치인지 확인
            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                if (CheckPath(hit.position))
                {
                    return hit.position;
                }
                /*
                //해당 위치에서 타겟까지 경로가 있는지 확인
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(hit.position, targetPos, NavMesh.AllAreas, path)
                    && path.status == NavMeshPathStatus.PathComplete)
                {
                    //해당 위치에서 타겟까지 시야가 확보되는지 임시 체크
                    Vector3 checkStart = hit.position + Vector3.up * _heightOffset;
                    Vector3 checkTarget = targetPos + Vector3.up * _heightOffset;
                    Vector3 dir = checkTarget - checkStart;

                    //장애물이 없으면 이 위치를 우회 위치로 선택
                    if (!Physics.Raycast(checkStart, dir.normalized, dir.magnitude, ~_playerLayerMask))
                    {
                        return hit.position;
                    }
                }
                */
            }
        }

        Debug.Log("적절한 우회 위치를 찾지 못함");
        return targetPos;
    }
    #endregion

    /// <summary>
    /// 피격 시 애니메이션 트리거를 작동하는 함수
    /// </summary>
    void OnHit()
    {
        if (_curState == EnemyState.Dead) return;
        _animator.SetTrigger(_hashTakeHit);
    }

    /// <summary>
    /// 사망 시 호출되는 함수
    /// 상태 변경, NavMesh 끄기, 애니메이션 트리거 작동
    /// </summary>
    void OnDeath()
    {
        ChangeState(EnemyState.Dead);

        _agent.isStopped = true;

        _agent.enabled = false;

        _animator.SetTrigger(_hashDie);
    }
}
