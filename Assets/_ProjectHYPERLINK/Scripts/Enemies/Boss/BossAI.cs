using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static EnemyAI;

/// <summary>
/// 보스 AI 시스템 (전투)
///
/// 공격 패턴 :
/// 1. 쿨타임이 돌아온 패턴 중 랜덤 선택
/// 2. 모든 패턴이 쿨타임이면 일반 공격
/// 3. 거리가 멀면 추격
///
/// 패턴 목록 :
/// 1. 내려찍기
/// 2. 돌진
/// 3. 3연속 베기 (전진하며 공격)
/// 4. 화염 브레스
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("----- 참조 -----")]
    [SerializeField] EnemyController _controller;
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] Animator _animator;

    [Header("----- 전투 -----")]
    [SerializeField] Transform _target;  //플레이어
    [SerializeField] LayerMask _playerLayerMask;    //플레이어 레이어
    [SerializeField] LayerMask _obstacleLayerMask;  //장애물 레이어 (벽, 오브젝트)

    [Header("----- 근거리 공격 감지 -----")]
    [SerializeField] TargetDetector _basicAttackDetector;  //방망이 타격 감지

    [Header("----- 패턴 감지기 -----")]
    [SerializeField] TargetDetector _comboDetector;     //3연타 감지

    [Header("----- 이펙트 -----")]
    [SerializeField] GameObject _slamEffect;     //내려찍기 경고 이펙트
    [SerializeField] GameObject _breathEffect;          //화염 브레스 이펙트
    [SerializeField] Transform _breathSpawnPos;         //브레스 발사 위치

    //상태
    public enum BossState
    {
        Idle,       //대기
        Chase,      //추격
        Attack,     //공격
        Pattern,    //패턴 실행
        Stunned,    //기절 (돌진 실패)
        Dead        //사망
    }
    [SerializeField] BossState _curState = BossState.Idle;

    BossData _data;

    bool _isActive = false;            //AI 활성화 플래그
    bool _isExecutingPattern = false;  //패턴 실행 중 플래그
    bool _isRotatingToTarget = false;  //타겟을 향해 회전 중 플래그

    //쿨타임 관리
    float _lastBasicAttackTime;     //마지막 일반 공격 실행 시간
    float _lastPatternTime;         //마지막 패턴 실행 시간
    float _lastSlamTime;            //내려찍기 패턴 실행 시간
    float _lastChargeTime;          //돌진 패턴 실행 시간
    float _lastComboTime;           //3연타 패턴 실행 시간
    float _lastBreathTime;          //브레스 패턴 실행 시간

    // 돌진 관련
    bool _isCharging = false;       //돌진 중 플래그
    Vector3 _chargeDirection;       //돌진 방향
    Vector3 _chargeStartPos;        //돌진 시작 위치

    // 애니메이터 해시
    private readonly int _hashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private readonly int _hashBasicAttack = Animator.StringToHash("Attack");
    private readonly int _hashSlam = Animator.StringToHash("Slam");
    private readonly int _hashCharge = Animator.StringToHash("Charge");
    private readonly int _hashCombo = Animator.StringToHash("Combo");
    private readonly int _hashBreath = Animator.StringToHash("Breath");
    private readonly int _hashRoar = Animator.StringToHash("Roar");
    private readonly int _hashStun = Animator.StringToHash("Stun");
    private readonly int _hashTakeHit = Animator.StringToHash("TakeHit");
    private readonly int _hashDie = Animator.StringToHash("Die");

    private void Awake()
    {
        if (_controller == null)
            _controller = GetComponent<EnemyController>();

        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();

        if (_animator == null)
            _animator = GetComponent<Animator>();

        //이벤트 연결
        _controller.OnInitialized += StartAI;
        _controller.OnHit += OnHit;
        _controller.OnDie += OnDeath;
    }

    /// <summary>
    /// AI 시작
    /// </summary>
    void StartAI()
    {
        _data = _controller.BossData;
        _isActive = true;
        _curState = BossState.Idle;

        // NavMeshAgent 설정
        if (_agent != null)
        {
            _agent.speed = _data.MoveSpeed;
            _agent.stoppingDistance = _data.BasicAttackRange * 0.8f;
        }

        // 타격 감지기 초기화
        if (_basicAttackDetector != null)
        {
            _basicAttackDetector.Initialize(_controller.Atk * _data.BasicAttackDamageMultiplier);
        }

        Debug.Log("[BossAI] AI 시작!");
    }

    private void Update()
    {
        if (!_isActive || _controller.IsDead) return;

        //돌진 중에는 별도 처리
        if (_isCharging)
        {
            UpdateChargeMovement();
            return;
        }

        switch (_curState)
        {
            case BossState.Idle:
                UpdateIdleState();
                break;
            case BossState.Chase:
                UpdateChaseState();
                break;
            case BossState.Attack:
                UpdateAttackState();
                break;
            case BossState.Pattern:
                //패턴 실행 중에는 대기
                break;
            case BossState.Stunned:
                //기절 중에는 대기
                break;
        }

        //애니메이터 이동 속도 업데이트
        if (_agent != null && _animator != null)
        {
            _animator.SetFloat(_hashMoveSpeed, _agent.velocity.magnitude / _agent.speed);
        }
    }

    #region 상태별 함수

    /// <summary>
    /// 대기 상태 - 플레이어 탐지
    /// </summary>
    void UpdateIdleState()
    {
        // 플레이어 탐지
        Collider[] colliders = Physics.OverlapSphere(transform.position, _data.DetectionRange, _playerLayerMask);

        if (colliders.Length > 0)
        {
            _target = colliders[0].transform;
            ChangeState(BossState.Chase);
            Debug.Log("[BossAI] 플레이어 발견! 전투 시작!");
        }
    }

    /// <summary>
    /// 추격 상태
    /// </summary>
    void UpdateChaseState()
    {
        if (_target == null)
        {
            ChangeState(BossState.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.position);

        //공격 범위 안에 들어오고, 시야가 있을 경우 공격 상태로 전환
        if (distance <= _data.BasicAttackRange && HasLineOfSight(_target))
        {
            //공격 상태로 전환
            ChangeState(BossState.Attack);
            return;
        }

        //추격
        _agent.isStopped = false;
        _agent.SetDestination(_target.position);
    }

    /// <summary>
    /// 공격 상태 - 패턴 선택 및 실행
    /// </summary>
    void UpdateAttackState()
    {
        if (_target == null)
        {
            ChangeState(BossState.Idle);
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.position);

        //공격 범위 밖으로 나가면 추격
        if (distance > _data.BasicAttackRange)
        {
            ChangeState(BossState.Chase);
            return;
        }

        //패턴 실행 중이거나 회전 중이면 대기
        if (_isExecutingPattern || _isRotatingToTarget) return;

        //이동 정지
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;

        //타겟을 향해 회전 후 공격 코루틴 실행
        StartCoroutine(RotateAndAttack());
    }

    /// <summary>
    /// 타겟을 향해 회전한 후 공격을 실행하는 함수
    /// </summary>
    /// <returns></returns>
    IEnumerator RotateAndAttack()
    {
        _isRotatingToTarget = true;

        //타겟 방향 계산
        Vector3 dir = _target.position - transform.position;
        dir.y = 0f;

        //회전
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);

            float elapsed = 0f;
            float rotTime = 0.5f;

            while (elapsed < rotTime)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, elapsed /  rotTime);

                elapsed += Time.deltaTime;
                yield return null;

                if (Quaternion.Angle(transform.rotation, targetRot) < 5f)
                {
                    transform.rotation = targetRot;
                    break;
                }
            }

            //최종 회전 보정
            transform.rotation = targetRot;
        }

        _isRotatingToTarget = false;

        //회전 완료 후 공격 실행
        TryExecutePattern();
    }

    #endregion

    #region 패턴 선택 및 실행

    /// <summary>
    /// 사용 가능한 패턴 시도
    /// </summary>
    void TryExecutePattern()
    {
        //전역 패턴 쿨타임 체크
        float timeSinceLastPattern = Time.time - _lastPatternTime;
        float globalCooldown = _data.GlobalPatternCooldown;

        //전역 패턴 쿨타임이 안 돌았으면 기본 공격만 가능
        if (timeSinceLastPattern < globalCooldown)
        {
            //기본 공격 쿨타임도 체크
            if (Time.time >= _lastBasicAttackTime + _data.BasicAttackCooldown)
            {
                StartCoroutine(BasicAttackPattern());
            }
            return;
        }

        //사용 가능한 패턴 리스트
        List<System.Action> availablePatterns = new List<System.Action>();

        //개별 패턴 쿨타임 체크
        if (Time.time >= _lastSlamTime + _data.SlamCooldown)
        {
            availablePatterns.Add(() => StartCoroutine(SlamPattern()));
        }

        if (Time.time >= _lastChargeTime + _data.ChargeCooldown)
        {
            availablePatterns.Add(() => StartCoroutine(ChargePattern()));
        }

        if (Time.time >= _lastComboTime + _data.ComboCooldown)
        {
            availablePatterns.Add(() => StartCoroutine(ComboPattern()));
        }

        if (Time.time >= _lastBreathTime + _data.BreathCooldown)
        {
            availablePatterns.Add(() => StartCoroutine(BreathPattern()));
        }

        //사용 가능한 패턴이 있으면 랜덤 선택
        if (availablePatterns.Count > 0)
        {
            int randomIndex = Random.Range(0, availablePatterns.Count);
            availablePatterns[randomIndex]?.Invoke();

            //전역 패턴 쿨타임 갱신
            _lastPatternTime = Time.time;
            //기본 공격 쿨타임도 갱신 (패턴 후 바로 기본 공격 방지)
            _lastBasicAttackTime = Time.time;
        }
        //모든 패턴이 쿨타임이면 기본 공격
        else if (Time.time >= _lastBasicAttackTime + _data.BasicAttackCooldown)
        {
            StartCoroutine(BasicAttackPattern());
        }
    }

    #endregion

    #region 패턴 구현

    /// <summary>
    /// 기본 공격: 방망이 휘두르기
    /// </summary>
    IEnumerator BasicAttackPattern()
    {
        _isExecutingPattern = true;
        _lastBasicAttackTime = Time.time;

        Debug.Log("[BossAI] 기본 공격!");

        _animator.SetTrigger(_hashBasicAttack);

        yield return new WaitForSeconds(2f);

        _isExecutingPattern = false;
    }

    /// <summary>
    /// 애니메이션 이벤트: 기본 공격 타격 시점
    /// </summary>
    public void OnBasicAttackHit()
    {
        if (_basicAttackDetector != null)
        {
            _basicAttackDetector.EnableDetector();
        }
    }

    /// <summary>
    /// 패턴 1: 내려찍기
    /// - 1초 준비 동작
    /// - 범위 공격 (반경 5m)
    /// </summary>
    IEnumerator SlamPattern()
    {
        _isExecutingPattern = true;
        _lastSlamTime = Time.time;
        ChangeState(BossState.Pattern);

        Debug.Log("[BossAI] 패턴 1: 내려찍기 준비!");

        //애니메이션 재생
        _animator.SetTrigger(_hashSlam);

        //이펙트 생성
        GameObject slamEffect = null;
        if (_slamEffect != null)
        {
            slamEffect = Instantiate(_slamEffect, transform.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(_data.SlamPrepareTime);

        Debug.Log("[BossAI] 내려찍기 발동!");

        yield return new WaitForSeconds(0.5f);

        //이펙트 제거
        if (slamEffect != null)
        {
            Destroy(slamEffect);
        }

        _isExecutingPattern = false;
        ChangeState(BossState.Attack);
    }

    /// <summary>
    /// 애니메이션 이벤트: 내려찍기 타격 시점
    /// </summary>
    public void OnSlamHit()
    {
        Debug.Log("[BossAI] 내려찍기 타격!");

        //범위 내 모든 플레이어에게 피해
        Collider[] hits = Physics.OverlapSphere(transform.position, _data.SlamRadius, _playerLayerMask);

        foreach (Collider hit in hits)
        {
            PlayerCombat player = hit.GetComponent<PlayerCombat>();
            if (player != null)
            {
                float damage = _controller.Atk * _data.SlamDamageMultiplier;
                player.TakeDamage(damage);
                Debug.Log($"[BossAI] 내려찍기 피해: {damage}");
            }
        }
    }

    /// <summary>
    /// 패턴 2: 돌진
    /// - 플레이어 방향으로 직선 돌진
    /// - 오브젝트 또는 벽에 박으면 1초 기절
    /// - 플레이어 맞으면 밀려남 + 기절
    /// </summary>
    IEnumerator ChargePattern()
    {
        _isExecutingPattern = true;
        _lastChargeTime = Time.time;
        ChangeState(BossState.Pattern);

        Debug.Log("[BossAI] 패턴 2: 돌진 준비!");

        //돌진 방향은 이미 RotateAndAttack()에서 설정 완료
        _chargeDirection = transform.forward;
        _chargeStartPos = transform.position;

        yield return new WaitForSeconds(0.5f);  // 준비 동작

        //돌진 애니메이션
        _animator.SetBool(_hashCharge, true);

        //돌진 시작
        _isCharging = true;
        _agent.enabled = false;  // NavMesh 비활성화

        Debug.Log("[BossAI] 돌진 시작!");

        //돌진은 Update에서 처리
        //여기서는 최대 시간만 대기 (안전장치)
        yield return new WaitForSeconds(5f);

        //만약 아직 돌진 중이면 강제 종료
        if (_isCharging)
        {
            EndCharge(true);
        }
    }

    /// <summary>
    /// 돌진 이동 처리 (Update에서 호출)
    /// </summary>
    void UpdateChargeMovement()
    {
        //돌진 이동
        Vector3 nextPosition = transform.position + _chargeDirection * _data.ChargeSpeed * Time.deltaTime;

        //장애물 충돌 체크
        float checkDistance = _data.ChargeSpeed * Time.deltaTime + 1f;
        if (Physics.Raycast(transform.position, _chargeDirection, out RaycastHit hit, checkDistance, _obstacleLayerMask))
        {
            Debug.Log($"[BossAI] 장애물 충돌! ({hit.collider.name}) 기절!");
            Debug.DrawRay(transform.position, _chargeDirection * checkDistance, Color.red, 2f);
            EndCharge(true);
            return;
        }

        transform.position = nextPosition;

        //플레이어 충돌 체크
        if (Physics.SphereCast(transform.position, 2f, _chargeDirection, out RaycastHit playerHit, 1f, _playerLayerMask))
        {
            Debug.Log("[BossAI] 돌진 플레이어 명중!");

            //플레이어에게 피해
            PlayerCombat player = playerHit.collider.GetComponent<PlayerCombat>();
            if (player != null)
            {
                float damage = _controller.Atk * _data.ChargeDamageMultiplier;
                player.TakeDamage(damage);
                Debug.Log($"[BossAI] 돌진 피해: {damage}");

                //넉백 및 기절
                PlayerStateController stateController = playerHit.collider.GetComponent<PlayerStateController>();
                PlayerNavController navController = playerHit.collider.GetComponent<PlayerNavController>();

                if (navController != null)
                {
                    navController.ApplyKnockback(_data.ChargeKnockbackPower, _chargeDirection);
                }

                if (stateController != null)
                {
                    StartCoroutine(PlayerStunCoroutine(stateController, _data.ChargePlayerStunDuration));
                }
            }

            EndCharge(false);
            return;
        }

        //최대 거리 안전장치 (무한 돌진 방지)
        float chargeDistance = Vector3.Distance(_chargeStartPos, transform.position);
        if (chargeDistance > 50f)
        {
            Debug.Log("[BossAI] 돌진 최대 거리 도달!");
            EndCharge(true);
        }
    }

    /// <summary>
    /// 돌진 종료
    /// </summary>
    void EndCharge(bool hitWall)
    {
        _animator.SetBool(_hashCharge, false);
        _isCharging = false;
        _agent.enabled = true;

        if (hitWall)
        {
            //보스 기절
            StartCoroutine(BossStunCoroutine(_data.ChargeStunDuration));
        }
        else
        {
            _isExecutingPattern = false;
            ChangeState(BossState.Attack);
        }
    }

    /// <summary>
    /// 보스 기절 코루틴
    /// </summary>
    IEnumerator BossStunCoroutine(float duration)
    {
        ChangeState(BossState.Stunned);
        _animator.SetTrigger(_hashStun);

        Debug.Log($"[BossAI] 보스 기절 ({duration}초)");

        yield return new WaitForSeconds(duration);

        _isExecutingPattern = false;
        ChangeState(BossState.Attack);
    }

    /// <summary>
    /// 플레이어 기절 코루틴
    /// </summary>
    IEnumerator PlayerStunCoroutine(PlayerStateController stateController, float duration)
    {
        stateController.SetKnockState(true);
        yield return new WaitForSeconds(duration);
        stateController.SetKnockState(false);
    }

    /// <summary>
    /// 패턴 3: 3연타
    /// - 짧은 포효 후
    /// - 전진하면서 3번 크게 휘두르기
    /// </summary>
    IEnumerator ComboPattern()
    {
        _isExecutingPattern = true;
        _lastComboTime = Time.time;
        ChangeState(BossState.Pattern);

        Debug.Log("[BossAI] 패턴 3: 3연타 준비!");

        //포효
        _animator.SetTrigger(_hashRoar);
        yield return new WaitForSeconds(_data.ComboRoarDuration);

        //공격 애니메이션
        _animator.SetTrigger(_hashCombo);

        //3연타
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"[BossAI] 3연타 {i + 1}번째 공격!");

            //전진
            Vector3 moveDir = transform.forward;
            Vector3 targetPos = transform.position + moveDir * _data.ComboMoveDistance;

            // NavMesh 상 유효한 위치로 이동
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                _agent.enabled = true;
                _agent.isStopped = false;
                _agent.SetDestination(hit.position);
            }

            yield return new WaitForSeconds(_data.ComboAttackInterval);

            _agent.isStopped = true;
        }

        yield return new WaitForSeconds(0.5f);

        _isExecutingPattern = false;
        ChangeState(BossState.Attack);
    }

    /// <summary>
    /// 애니메이션 이벤트: 3연타 타격 시점
    /// </summary>
    public void OnComboHit()
    {
        if (_comboDetector != null)
        {
            _comboDetector.EnableDetector();
        }

        Debug.Log("[BossAI] 3연타 타격!");
    }

    /// <summary>
    /// 패턴 4: 화염 브레스
    /// - 짧은 포효 후
    /// - 직선으로 화염 발사
    /// </summary>
    IEnumerator BreathPattern()
    {
        _isExecutingPattern = true;
        _lastBreathTime = Time.time;
        ChangeState(BossState.Pattern);

        Debug.Log("[BossAI] 패턴 4: 화염 브레스 준비!");

        //포효
        _animator.SetTrigger(_hashRoar);
        yield return new WaitForSeconds(_data.BreathRoarDuration);

        //브레스 애니메이션
        _animator.SetTrigger(_hashBreath);

        yield return new WaitForSeconds(1f);

        Debug.Log("[BossAI] 화염 브레스 발사!");

        //브레스 이펙트 생성
        if (_breathEffect != null && _breathSpawnPos != null)
        {
            Quaternion breatRot = Quaternion.LookRotation(transform.forward);

            GameObject breathEffect = Instantiate(_breathEffect, _breathSpawnPos.position, breatRot);

            WaveAttackController waveAttackController = breathEffect.GetComponent<WaveAttackController>();
            if (waveAttackController != null)
            {
                float damage = _controller.Atk * _data.BreathDamageMultiplier;
                waveAttackController.Initialize(damage : damage);
            }
        }

        Debug.Log("[BossAI] 화염 브레스 종료!");

        yield return new WaitForSeconds(1f);

        _isExecutingPattern = false;
        ChangeState(BossState.Attack);
    }

    #endregion

    #region 유틸리티

    /// <summary>
    /// 상태 변경
    /// </summary>
    void ChangeState(BossState newState)
    {
        if (_curState == newState) return;

        _curState = newState;
        Debug.Log($"[BossAI] 상태 변경: {newState}");
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
        Vector3 startPos = transform.position + Vector3.up;
        //타겟 위치 (플레이어 위치)
        Vector3 targetPos = target.position + Vector3.up;

        //방향 및 거리 계산
        Vector3 dir = targetPos - startPos;
        float distance = dir.magnitude;

        //만약 발사한 레이캐스트가 장애물 레이어에 맞았다면
        if (Physics.Raycast(startPos, dir.normalized, out RaycastHit hit, distance, _obstacleLayerMask))
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
    /// 피격 시 호출
    /// </summary>
    void OnHit()
    {
        if (_curState == BossState.Dead) return;
        _animator.SetTrigger(_hashTakeHit);
    }

    /// <summary>
    /// 사망 시 호출
    /// </summary>
    void OnDeath()
    {
        _isActive = false;
        _isCharging = false;

        ChangeState(BossState.Dead);

        // NavMeshAgent 정지
        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        _animator.SetTrigger(_hashDie);

        Debug.Log("[BossAI] 보스 사망!");
    }

    #endregion

    #region 기즈모

    private void OnDrawGizmosSelected()
    {
        if (_data == null) return;

        //탐지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _data.DetectionRange);

        //기본 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _data.BasicAttackRange);

        //내려찍기 범위
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _data.SlamRadius);

        //브레스 범위
        Gizmos.color = Color.cyan;
        Vector3 breathStart = transform.position + transform.forward * 2f;
        Gizmos.DrawRay(breathStart, transform.forward * _data.BreathRange);
    }

    #endregion
}
