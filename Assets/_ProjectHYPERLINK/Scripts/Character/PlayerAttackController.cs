using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 플레이어 공격 컨트롤러
/// 
/// 역할:
/// - 마우스 우클릭 공격 처리
/// - 적 탐색 (전방 원뿔 범위)
/// - 데미지 계산 및 적용
/// - 공격 쿨다운 관리
/// - 공격 VFX 처리
/// - 적 히트 VFX 처리
/// </summary>
[RequireComponent(typeof(PlayerCharacter))]
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerAttackController : MonoBehaviour
{
    private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    private const float ATTACK_DAMAGE_TIMING = 0.5f;
    private const float MIN_ATTACK_COOLDOWN = 0.1f;

    [Header("참조")]
    private Camera _mainCamera;
    private PlayerCharacter _playerCharacter;
    private PlayerStateController _stateController;
    private Animator _animator;
    private NavMeshAgent _agent;

    // 공격 상태
    private bool _isAttacking = false;
    private bool _isOnCooldown = false;

    // 쿨다운 관리
    private float _baseCooldown;
    private float _currentAttackCooldown;

    [Header("전투 설정")]
    [Tooltip("공격 범위 (미터)")]
    [SerializeField] private float _attackRange = 3f;

    [Tooltip("공격 각도 (전방 원뿔 범위, 90도 = 전방 1/4 원)")]
    [SerializeField] private float _attackAngle = 90f;

    [SerializeField] private float _attackDamage = 25f;
    public float AttackDamage => _attackDamage;

    [Tooltip("기본 공격 쿨다운 (초) - Attack Speed 스탯에 의해 감소됨")]
    [SerializeField] private float _attackSpeed = 1f;

    [Header("애니메이션 설정")]
    [SerializeField] private float _attackAnimationDuration = 1.0f;

    [Header("기본 공격 VFX")]
    [Tooltip("플레이어 위치에 표시할 공격 VFX")]
    [SerializeField] private GameObject _baseAttackVfxPrefab;

    [Tooltip("VFX 위치 오프셋 (로컬 좌표)")]
    [SerializeField] private Vector3 _vfxPositionOffset = new Vector3(0f, 1f, 1f);

    [Tooltip("VFX 회전 오프셋 (오일러 각도)")]
    [SerializeField] private Vector3 _vfxRotationOffset = Vector3.zero;

    [Tooltip("VFX 생성 타이밍")]
    [Range(0f, 1f)]
    [SerializeField] private float _vfxSpawnTiming = 0.5f;

    [Tooltip("VFX 자동 제거 시간 (초)")]
    [SerializeField] private float _vfxLifetime = 2f;

    [Tooltip("적이 맞았을 때 표시할 히트 VFX")]
    [SerializeField] private HitVfxConfig _baseAttackEnemyHitVfxConfig;

    [Header("레이어 설정")]
    [SerializeField] private LayerMask _groundLayer = ~0;

    [Header("런타임 정보")]
    [SerializeField, Tooltip("현재 적용된 공격 쿨다운 (읽기 전용)")]
    private float _currentAttackCooldownDisplay;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;
    [SerializeField] private bool _visualizeAttackRange = true;

    // 이벤트
    public static event System.Action OnAttackStarted;
    public static event System.Action OnAttackEnded;
    public static event System.Action<int> OnAttackTrigger;

    #region 초기화

    private void Awake()
    {
        _mainCamera = Camera.main;
        _playerCharacter = GetComponent<PlayerCharacter>();
        _stateController = GetComponent<PlayerStateController>();
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();

        if (_playerCharacter == null)
        {
            Debug.LogError("[PlayerAttackController] PlayerCharacter가 없습니다!");
        }

        if (_stateController == null)
        {
            Debug.LogError("[PlayerAttackController] PlayerStateController가 없습니다!");
        }

        if (_animator == null)
        {
            Debug.LogError("[PlayerAttackController] Animator가 없습니다!");
        }

        if (_agent == null)
        {
            Debug.LogError("[PlayerAttackController] NavMeshAgent가 없습니다!");
        }

        // 기본값 저장
        _baseCooldown = _attackSpeed;
        _currentAttackCooldown = _baseCooldown;
        _currentAttackCooldownDisplay = _currentAttackCooldown;

        Log($"초기화 완료 - Base Cooldown: {_baseCooldown:F2}초");
    }

    private void Start()
    {
        // 초기 스탯 적용
        if (_playerCharacter != null)
        {
            CharacterStats initialStats = _playerCharacter.CurrentStats;
            UpdateAttackCooldown(initialStats.AttackSpeed);
        }
    }

    private void OnEnable()
    {
        // 스탯 변경 이벤트 구독
        PlayerCharacter.OnStatsChanged += HandleStatsChanged;
    }

    private void OnDisable()
    {
        PlayerCharacter.OnStatsChanged -= HandleStatsChanged;
    }

    #endregion

    #region 이벤트 핸들러

    /// <summary>
    /// 스탯 변경 핸들러 - Attack Speed 업데이트
    /// </summary>
    private void HandleStatsChanged(CharacterStats newStats)
    {
        UpdateAttackCooldown(newStats.AttackSpeed);
        Log($"공격 속도 업데이트: {newStats.AttackSpeed:F2}");
    }

    /// <summary>
    /// 공격 쿨다운 업데이트
    /// </summary>
    private void UpdateAttackCooldown(float attackSpeed)
    {
        _currentAttackCooldown = Mathf.Max(_baseCooldown - attackSpeed, MIN_ATTACK_COOLDOWN);
        _currentAttackCooldownDisplay = _currentAttackCooldown;
        Log($"쿨다운 갱신: {_currentAttackCooldown:F2}초");
    }

    #endregion

    #region 입력 처리

    private void Update()
    {
        HandleAttackInput();
    }

    /// <summary>
    /// 마우스 우클릭 공격 입력 처리
    /// </summary>
    private void HandleAttackInput()
    {
        // 우클릭: 공격
        if (Input.GetMouseButtonDown(1))
        {
            // UI 클릭 체크
            if (InputHelper.IsPointerOverUI())
            {
                Log("UI 클릭 감지 - 공격 취소");
                return;
            }

            if (CanAttack())
            {
                HandleRightClick();
            }
        }
    }

    /// <summary>
    /// 공격 가능 여부 확인
    /// 
    /// 추가된 체크:
    /// - 스킬 실행 중인지 확인
    /// </summary>
    private bool CanAttack()
    {
        // 쿨다운 또는 공격 중
        if (_isAttacking || _isOnCooldown)
        {
            Log("공격 중이거나 쿨다운 상태");
            return false;
        }

        // 사망 상태
        if (_playerCharacter == null || !_playerCharacter.IsAlive)
        {
            return false;
        }

        // 스킬 실행 중 체크 추가
        if (_stateController != null && _stateController.IsPerformingSkill)
        {
            Log("스킬 실행 중이므로 기본 공격 불가");
            return false;
        }

        // 상태이상 체크
        if (_stateController != null && !_stateController.CanAttack)
        {
            Log("상태이상으로 공격 불가");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 우클릭 처리: 마우스 방향으로 회전 후 공격
    /// </summary>
    private void HandleRightClick()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, _groundLayer))
        {
            // NavMeshAgent 완전 정지 (이동 중 공격 방지)
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.velocity = Vector3.zero;
                _agent.ResetPath();
                _agent.isStopped = true;
                Log("NavMeshAgent 정지");
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

        // 기본 공격 상태 설정
        if (_stateController != null)
        {
            _stateController.SetBaseAttacking(true);
        }

        // 공격 이벤트 발생
        OnAttackStarted?.Invoke();
        
        // 애니메이션 트리거 발생
        OnAttackTrigger?.Invoke(ATTACK_HASH);

        // 기본 공격 사운드 재생
        if (AudioManager.Instance?.SoundLibrary != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.SoundLibrary.PlayerBasicAttack);
        }

        Log($"공격 시작! 타겟: {enemies.Count}마리");

        // VFX 생성 코루틴 시작
        StartCoroutine(SpawnAttackVFXCoroutine());

        // 애니메이션 재생 대기
        yield return new WaitForSeconds(ATTACK_DAMAGE_TIMING);

        // 데미지 적용 (AttackInfo 사용)
        if (_playerCharacter != null)
        {
            float attackPower = _playerCharacter.GetAttackPower();

            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    // AttackInfo를 사용한 데미지 적용
                    AttackInfo attackInfo = AttackInfo.CreatePlayerBaseAttack(
                        attackPower,
                        enemy.transform.position,
                        _baseAttackEnemyHitVfxConfig
                    );

                    enemy.TakeDamage(attackInfo);
                    Log($"데미지 적용: {enemy.name} -> {attackPower:F1}");
                }
            }
        }

        // 애니메이션 완료 대기
        float remainingTime = _attackAnimationDuration - ATTACK_DAMAGE_TIMING;
        yield return new WaitForSeconds(remainingTime);

        // 공격 종료
        _isAttacking = false;

        // 기본 공격 상태 해제
        if (_stateController != null)
        {
            _stateController.SetBaseAttacking(false);
        }

        // NavMeshAgent 재활성화
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            Log("NavMeshAgent 재활성화");
        }

        OnAttackEnded?.Invoke();

        Log("공격 완료");

        // 쿨다운 시작
        yield return new WaitForSeconds(_currentAttackCooldown);
        _isOnCooldown = false;

        Log("쿨다운 완료");
    }

    /// <summary>
    /// VFX 생성 코루틴 (타이밍 제어)
    /// </summary>
    private IEnumerator SpawnAttackVFXCoroutine()
    {
        // VFX 생성 타이밍까지 대기
        float delay = _attackAnimationDuration * _vfxSpawnTiming;
        yield return new WaitForSeconds(delay);

        // 플레이어 위치에 공격 VFX 생성
        SpawnAttackVFX(transform.position);

        Log($"VFX 생성 완료 (타이밍: {_vfxSpawnTiming:F2})");
    }

    /// <summary>
    /// 공격 VFX 생성
    /// </summary>
    private void SpawnAttackVFX(Vector3 basePosition)
    {
        if (_baseAttackVfxPrefab == null) return;

        // 위치 계산
        Vector3 vfxPosition = basePosition + transform.TransformDirection(_vfxPositionOffset);

        // 회전 계산
        Quaternion vfxRotation = transform.rotation * Quaternion.Euler(_vfxRotationOffset);

        // VFX 생성
        GameObject vfx = Instantiate(_baseAttackVfxPrefab, vfxPosition, vfxRotation);

        // 수명 후 제거
        Destroy(vfx, _vfxLifetime);

        Log($"VFX 생성: 위치={vfxPosition}, 회전={_vfxRotationOffset}");
    }

    #endregion

    #region 디버그

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[PlayerAttackController] {message}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!_visualizeAttackRange || !Application.isPlaying) return;

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

    [ContextMenu("Debug: Print Attack Info")]
    private void DebugPrintAttackInfo()
    {
        Debug.Log("===== PlayerAttackController 정보 =====");
        Debug.Log($"기본 쿨다운: {_baseCooldown:F2}초");
        Debug.Log($"현재 쿨다운: {_currentAttackCooldown:F2}초");
        Debug.Log($"공격 중: {_isAttacking}");
        Debug.Log($"쿨다운 중: {_isOnCooldown}");
        Debug.Log($"VFX 회전 오프셋: {_vfxRotationOffset}");
        Debug.Log($"VFX 생성 타이밍: {_vfxSpawnTiming:F2}");

        if (_playerCharacter != null)
        {
            CharacterStats stats = _playerCharacter.CurrentStats;
            float attackPower = _playerCharacter.GetAttackPower();
            Debug.Log($"공격 속도 스탯: {stats.AttackSpeed:F2}");
            Debug.Log($"최종 공격력: {attackPower:F1}");
        }

        if (_stateController != null)
        {
            Debug.Log($"스킬 실행 중: {_stateController.IsPerformingSkill}");
        }
    }

    #endregion
}
