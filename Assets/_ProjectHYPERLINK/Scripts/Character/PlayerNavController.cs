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
/// - 데미지 적용 방식 변경: IDamageable → EnemyController 직접 호출 (스킬과 동일)
/// - Attack Speed 기반 Attack Cooldown 동적 조정 추가
/// - Movement Speed 스탯 기반 이동속도 적용 추가
/// - _attackCooldown → _attackSpeed 변수명 변경 (기본 공격 속도 기준값)
/// - [FIX] MovementSpeed와 AttackSpeed를 absolute 값으로 처리하도록 수정
///   * MovementSpeed: percentage → absolute 가산 (Dex 5 → +0.5 speed)
///   * AttackSpeed: percentage → absolute 쿨다운 감소 (Dex 5 → -0.25초)
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

    [Header("런타임 정보")]
    [SerializeField, Tooltip("현재 적용된 공격 쿨다운 (읽기 전용)")]
    private float _currentAttackCooldown;

    [Header("애니메이션 설정")]
    [SerializeField] private float _animationDampTime = 0.1f;
    [SerializeField] private float _attackAnimationDuration = 1.0f;

    [Header("전투 설정")]
    [Tooltip("공격 범위 (미터)")]
    [SerializeField] private float _attackRange = 3f;

    [Tooltip("공격 각도 (전방 원뿔 범위, 90° = 전방 1/4 원)")]
    [SerializeField] private float _attackAngle = 90f;

    [SerializeField] private float _attackDamage = 25f;
    public float AttackDamage => _attackDamage;

    [Tooltip("기본 공격 쿨다운 (초) - Attack Speed 스탯에 의해 감소됨")]
    [SerializeField] private float _attackSpeed = 1f;

    [Header("넉백 설정")]
    [SerializeField] private float _knockbackDuration = 0.3f;

    [Header("레이어 설정")]
    [SerializeField] private LayerMask _groundLayer = ~0;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

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

        Log("사망 - 모든 행동 정지");
    }

    /// <summary>
    /// 스탯 변경 시 처리 (Attack Speed, Movement Speed)
    /// </summary>
    private void HandleStatsChanged(CharacterStats stats)
    {
        if (stats == null)
        {
            Debug.LogWarning("[PlayerNavController] HandleStatsChanged - stats가 null입니다!");
            return;
        }

        Debug.Log($"[PlayerNavController] HandleStatsChanged 호출");
        Debug.Log($"  Movement Speed: {stats.MovementSpeed:F2}");
        Debug.Log($"  Attack Speed: {stats.AttackSpeed:F2}");

        // Attack Speed 기반 쿨다운 재계산
        UpdateAttackCooldown(stats.AttackSpeed);

        // Movement Speed 기반 이동속도 업데이트는 Update()에서 처리
        Log($"스탯 변경 완료");
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

        // 이동속도 업데이트 (스탯 + 상태이상)
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

        // 공격 불가 상태 체크
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
                // 상호작용 시작
                _currentTarget = null;

                if (_interactionCoroutine != null)
                {
                    StopCoroutine(_interactionCoroutine);
                }

                _pendingInteraction = interactable;
                _interactionCoroutine = StartCoroutine(MoveToInteract(hit.collider.transform));
                Log($"상호작용 대상 감지: {hit.collider.name}");
                return;
            }

            // 2순위: 적 클릭 시 추적 시작
            EnemyController enemy = hit.collider.GetComponent<EnemyController>();
            if (enemy != null)
            {
                _currentTarget = enemy.transform;
                _agent.SetDestination(_currentTarget.position);
                Log($"적 타겟 설정: {enemy.name}");
                return;
            }
        }

        // 3순위: 지면 클릭 시 이동
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _groundLayer))
        {
            _currentTarget = null;

            if (_interactionCoroutine != null)
            {
                StopCoroutine(_interactionCoroutine);
                _interactionCoroutine = null;
                _pendingInteraction = null;
            }

            _agent.SetDestination(hit.point);
            Log($"이동 명령: {hit.point}");
        }
    }

    /// <summary>
    /// 우클릭 처리: 마우스 방향으로 회전 후 공격
    /// 적이 없어도 헛스윙 허용 (애니메이션 + 쿨다운 적용)
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
            // 마우스 위치로 회전
            Vector3 targetDirection = (hit.point - transform.position).normalized;
            targetDirection.y = 0;

            if (targetDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(targetDirection);
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
        List<EnemyController> enemiesInCone = new List<EnemyController>();

        Collider[] colliders = Physics.OverlapSphere(transform.position, _attackRange);

        foreach (Collider col in colliders)
        {
            EnemyController enemy = col.GetComponent<EnemyController>();

            if (enemy == null)
                continue;

            Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
            float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);

            if (angleToEnemy <= _attackAngle / 2f)
            {
                enemiesInCone.Add(enemy);
            }
        }

        return enemiesInCone;
    }

    /// <summary>
    /// 다중 타겟 공격
    /// </summary>
    private void PerformMultiAttack(List<EnemyController> targets)
    {
        if (_isAttacking || _isOnCooldown)
            return;

        StartCoroutine(MultiAttackCoroutine(targets));
    }

    /// <summary>
    /// 다중 공격 코루틴
    /// </summary>
    private IEnumerator MultiAttackCoroutine(List<EnemyController> targets)
    {
        _isAttacking = true;
        _isOnCooldown = true;

        _agent.isStopped = true;
        _animator.SetTrigger(ATTACK_HASH);

        yield return new WaitForSeconds(ATTACK_DAMAGE_TIMING);

        if (!_isDead)
        {
            float damage = CalculateDamage();
            int hitCount = 0;

            foreach (EnemyController enemy in targets)
            {
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    hitCount++;
                    Log($"{enemy.name}에게 {damage:F1} 데미지!");
                }
            }

            if (hitCount > 0)
            {
                Debug.Log($"[PlayerNavController] 다중 공격 완료: 총 {hitCount}명 타격!");
            }
        }

        yield return new WaitForSeconds(_attackAnimationDuration - ATTACK_DAMAGE_TIMING);

        _isAttacking = false;

        if (!_isDead)
        {
            _agent.isStopped = false;
        }

        yield return new WaitForSeconds(_currentAttackCooldown - _attackAnimationDuration);

        _isOnCooldown = false;
    }

    /// <summary>
    /// 데미지 계산
    /// 
    /// 새로운 공식: Physical Attack (또는 Magical Attack) + 크리티컬
    /// - Laon, Yujin: Physical Attack 사용
    /// - Sian: Magical Attack 사용
    /// </summary>
    private float CalculateDamage()
    {
        if (_playerCharacter == null)
            return _attackDamage; // 폴백: 기본값

        // 클래스에 따른 공격력 가져오기 (Physical/Magical Attack)
        float damage = _playerCharacter.GetAttackPower();

        // 크리티컬 판정
        CharacterStats stats = _playerCharacter.CurrentStats;
        if (Random.Range(0f, 100f) < stats.CriticalChance)
        {
            damage *= (1f + stats.CriticalDamage / 100f);

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerNavController] 크리티컬 히트!");
            }
        }

        if (_enableDebugLogs)
        {
            string attackType = _playerCharacter.IsPhysicalAttacker() ? "물리" : "마법";
            Debug.Log($"[기본 공격] {attackType} 공격력: {_playerCharacter.GetAttackPower():F1}, 최종 데미지: {damage:F1}");
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
            List<EnemyController> enemies = GetEnemiesInFrontCone();
            if (enemies.Count > 0)
            {
                PerformMultiAttack(enemies);
            }
        }
    }

    #endregion

    #region 스탯 연동

    /// <summary>
    /// Attack Speed 기반 Attack Cooldown 재계산
    /// [FIX] Absolute 값으로 쿨다운 감소 (percentage 방식 제거)
    /// 
    /// 공식: 실제 쿨다운 = 기본 쿨다운 - AttackSpeed
    /// 예시: Dex 5 → Attack Speed 0.25 → Cooldown = 1 - 0.25 = 0.75초
    /// </summary>
    private void UpdateAttackCooldown(float attackSpeed)
    {
        // Absolute 감소 방식
        _currentAttackCooldown = _baseCooldown - attackSpeed;

        // 최소 쿨다운 제한
        _currentAttackCooldown = Mathf.Max(_currentAttackCooldown, MIN_ATTACK_COOLDOWN);

        Debug.Log($"[PlayerNavController] Attack Cooldown 업데이트:");
        Debug.Log($"  - Attack Speed: {attackSpeed:F2}");
        Debug.Log($"  - Base Cooldown: {_baseCooldown:F2}초");
        Debug.Log($"  - Current Cooldown: {_currentAttackCooldown:F2}초");
    }

    /// <summary>
    /// 이동속도 업데이트 (스탯 기반 + 상태이상 배율)
    /// [FIX] MovementSpeed를 absolute 값으로 가산 (percentage 방식 제거)
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

        Log($"넉백: {power}m 밀려남");
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
            Debug.Log($"{attackType} 공격력: {attackPower:F1}");

            CharacterStats stats = _playerCharacter.CurrentStats;
            Debug.Log($"Physical Attack: {stats.PhysicalAttack:F1}");
            Debug.Log($"Magical Attack: {stats.MagicalAttack:F1}");
            Debug.Log($"크리티컬 확률: {stats.CriticalChance:F1}%");
            Debug.Log($"크리티컬 데미지: {stats.CriticalDamage:F1}%");

            // 예상 데미지 계산 (크리티컬 제외)
            Debug.Log($"예상 기본 공격 데미지: {attackPower:F1}");

            // 크리티컬 적용 시
            float critDamage = attackPower * (1f + stats.CriticalDamage / 100f);
            Debug.Log($"크리티컬 히트 시 데미지: {critDamage:F1}");
        }
        else
        {
            Debug.LogWarning("PlayerCharacter가 null입니다!");
        }
    }

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
