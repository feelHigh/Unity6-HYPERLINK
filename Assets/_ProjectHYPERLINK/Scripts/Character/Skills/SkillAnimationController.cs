using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 스킬 애니메이션 컨트롤러
/// 
/// 역할:
/// - SkillActivationSystem과 Animator 연결
/// - 스킬 실행 시 애니메이션 트리거 발동
/// - Root Motion 기반 스킬 처리
/// - 애니메이션 이벤트 수신
/// 
/// 사용법:
/// 1. PlayerCharacter GameObject에 추가
/// 2. Animator 자동 검색
/// 3. SkillActivationSystem에서 자동 호출
/// 
/// 애니메이션 트리거:
/// - SkillJudgement
/// - SkillSwiftSlash
/// - SkillConviction
/// - Hit
/// - Dead (Bool)
/// </summary>
[RequireComponent(typeof(Animator))]
public class SkillAnimationController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Animator _animator;
    [SerializeField] private SkillActivationSystem _skillActivationSystem;

    [Header("Root Motion 설정")]
    [Tooltip("스킬 사용 중 NavMeshAgent 비활성화")]
    [SerializeField] private bool _disableAgentDuringSkill = true;

    [Header("마우스 방향 회전")]
    [Tooltip("스킬 사용 시 마우스 커서 방향으로 회전")]
    [SerializeField] private bool _rotateTowardsMouse = true;

    [Tooltip("회전 속도 (도/초)")]
    [SerializeField] private float _rotationSpeed = 720f;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    // 컴포넌트 캐시
    private UnityEngine.AI.NavMeshAgent _navAgent;
    private PlayerCharacter _playerCharacter;
    private Camera _mainCamera;

    // 상태 추적
    private bool _isPerformingSkill = false;
    private SkillData _currentSkill = null;

    // 애니메이터 파라미터 해시 (최적화)
    private static readonly int HASH_SKILL_JUDGEMENT = Animator.StringToHash("SkillJudgement");
    private static readonly int HASH_SKILL_SWIFT_SLASH = Animator.StringToHash("SkillSwiftSlash");
    private static readonly int HASH_SKILL_CONVICTION = Animator.StringToHash("SkillConviction");
    private static readonly int HASH_HIT = Animator.StringToHash("Hit");
    private static readonly int HASH_DEAD = Animator.StringToHash("Dead");

    #region 초기화

    private void Awake()
    {
        // 자동 검색
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_skillActivationSystem == null)
        {
            _skillActivationSystem = GetComponent<SkillActivationSystem>();
        }

        _navAgent = GetComponent<NavMeshAgent>();
        _playerCharacter = GetComponent<PlayerCharacter>();
        _mainCamera = Camera.main;

        if (_animator == null)
        {
            Debug.LogError("[SkillAnimationController] Animator를 찾을 수 없습니다!");
            enabled = false;
        }

        if (_mainCamera == null)
        {
            Debug.LogError("[SkillAnimationController] Main Camera를 찾을 수 없습니다!");
        }
    }

    private void OnEnable()
    {
        // SkillActivationSystem 이벤트 구독
        if (_skillActivationSystem != null)
        {
            SkillActivationSystem.OnSkillExecuted += HandleSkillExecuted;
        }

        // PlayerCharacter 이벤트 구독
        if (_playerCharacter != null)
        {
            PlayerCharacter.OnPlayerHit += HandlePlayerHit;
            PlayerCharacter.OnPlayerDead += HandlePlayerDead;
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (_skillActivationSystem != null)
        {
            SkillActivationSystem.OnSkillExecuted -= HandleSkillExecuted;
        }

        if (_playerCharacter != null)
        {
            PlayerCharacter.OnPlayerHit -= HandlePlayerHit;
            PlayerCharacter.OnPlayerDead -= HandlePlayerDead;
        }
    }

    #endregion

    #region 이벤트 핸들러

    /// <summary>
    /// 스킬 실행 시 호출
    /// SkillActivationSystem에서 발생
    /// </summary>
    private void HandleSkillExecuted(SkillData skill)
    {
        if (skill == null) return;

        _isPerformingSkill = true;
        _currentSkill = skill;

        // NavMeshAgent 비활성화 (Root Motion 적용 위해)
        if (_disableAgentDuringSkill && _navAgent != null)
        {
            _navAgent.enabled = false;
        }

        // 마우스 방향으로 회전
        if (_rotateTowardsMouse)
        {
            RotateTowardsMousePosition();
        }

        // 스킬 이름에 따라 애니메이션 트리거
        PlaySkillAnimation(skill.SkillName);

        Log($"스킬 애니메이션 실행: {skill.SkillName}");
    }

    /// <summary>
    /// 플레이어 피격 시 호출
    /// </summary>
    private void HandlePlayerHit(float damage)
    {
        // 스킬 실행 중이 아닐 때만 피격 애니메이션 재생
        if (!_isPerformingSkill)
        {
            _animator.SetTrigger(HASH_HIT);
            Log("피격 애니메이션 재생");
        }
    }

    /// <summary>
    /// 플레이어 사망 시 호출
    /// </summary>
    private void HandlePlayerDead()
    {
        _animator.SetBool(HASH_DEAD, true);
        _isPerformingSkill = false;

        // NavMeshAgent 완전 정지
        if (_navAgent != null)
        {
            _navAgent.enabled = false;
        }

        Log("사망 애니메이션 재생");
    }

    #endregion

    #region 회전 제어

    /// <summary>
    /// 마우스 커서 방향으로 캐릭터 회전
    /// 
    /// 처리 과정:
    /// 1. 마우스 위치로 Raycast
    /// 2. 지면과의 교차점 계산
    /// 3. 해당 방향으로 즉시 회전
    /// </summary>
    private void RotateTowardsMousePosition()
    {
        if (_mainCamera == null)
        {
            Debug.LogWarning("[SkillAnimationController] 카메라가 없어 회전할 수 없습니다!");
            return;
        }

        // 마우스 위치로 Ray 발사
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 지면과 충돌 체크
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // 마우스 위치로의 방향 계산
            Vector3 targetPosition = hit.point;
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0; // Y축 회전만 사용

            // 방향이 유효한 경우 회전
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = targetRotation; // 즉시 회전

                Log($"마우스 방향으로 회전: {targetRotation.eulerAngles.y:F0}도");
            }
        }
    }

    #endregion

    #region 애니메이션 제어

    /// <summary>
    /// 스킬 이름에 따라 애니메이션 트리거 실행
    /// </summary>
    private void PlaySkillAnimation(string skillName)
    {
        switch (skillName)
        {
            case "Judgement":
                _animator.SetTrigger(HASH_SKILL_JUDGEMENT);
                break;

            case "Swift Slash":
            case "SwiftSlash":
                _animator.SetTrigger(HASH_SKILL_SWIFT_SLASH);
                break;

            case "Conviction":
                _animator.SetTrigger(HASH_SKILL_CONVICTION);
                break;

            default:
                Debug.LogWarning($"[SkillAnimationController] 알 수 없는 스킬 이름: {skillName}");
                OnSkillAnimationEnd(); // 안전장치
                break;
        }
    }

    #endregion

    #region 애니메이션 이벤트 (Animation Event에서 호출)

    /// <summary>
    /// 애니메이션 이벤트: 스킬 데미지 적용 시점
    /// 
    /// 사용법:
    /// 1. 애니메이션 클립 선택
    /// 2. Animation 창에서 이벤트 추가
    /// 3. 함수: OnSkillDamagePoint
    /// 4. 타이밍: 공격 모션이 적에게 닿는 순간
    /// </summary>
    public void OnSkillDamagePoint()
    {
        if (_currentSkill == null)
        {
            Debug.LogWarning("[SkillAnimationController] 현재 스킬이 없습니다!");
            return;
        }

        Log($"스킬 데미지 적용: {_currentSkill.SkillName}");

        // 스킬 타입에 따라 데미지 처리
        switch (_currentSkill.SkillType)
        {
            case SkillType.AreaOfEffect:
                ApplyAOEDamage();
                break;

            case SkillType.Melee:
                ApplyMeleeDamage();
                break;

            default:
                Debug.LogWarning($"[SkillAnimationController] 지원하지 않는 스킬 타입: {_currentSkill.SkillType}");
                break;
        }
    }

    /// <summary>
    /// 애니메이션 이벤트: 스킬 애니메이션 종료
    /// 
    /// 사용법:
    /// 애니메이션 끝 부분에 이벤트 추가
    /// </summary>
    public void OnSkillAnimationEnd()
    {
        _isPerformingSkill = false;
        _currentSkill = null;

        // NavMeshAgent 재활성화
        if (_disableAgentDuringSkill && _navAgent != null)
        {
            _navAgent.enabled = true;
        }

        Log("스킬 애니메이션 종료");
    }

    #endregion

    #region 데미지 처리

    /// <summary>
    /// AOE 스킬 데미지 적용
    /// 
    /// 처리:
    /// - 플레이어 위치 기준 범위 내 적 탐색
    /// - 각 적에게 데미지 적용
    /// </summary>
    private void ApplyAOEDamage()
    {
        Vector3 centerPosition = transform.position;

        // 스킬 특성에 따라 범위 조정
        float range = _currentSkill.Range;

        // Swift Slash는 전방 직선 범위
        if (_currentSkill.SkillName.Contains("Swift"))
        {
            centerPosition += transform.forward * (range * 0.5f);
        }

        Collider[] hits = Physics.OverlapSphere(centerPosition, range);
        int enemyCount = 0;

        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                float damage = CalculateSkillDamage();
                enemy.TakeDamage(damage);
                enemyCount++;
            }
        }

        Log($"AOE 데미지 적용: {enemyCount}명의 적 타격");
    }

    /// <summary>
    /// 근거리 스킬 데미지 적용 (단일 대상)
    /// 
    /// 처리:
    /// - 범위 내 가장 가까운 적 찾기
    /// - 해당 적에게 집중 데미지
    /// </summary>
    private void ApplyMeleeDamage()
    {
        Vector3 centerPosition = transform.position + transform.forward * 2f;
        Collider[] hits = Physics.OverlapSphere(centerPosition, _currentSkill.Range);

        EnemyController closestEnemy = null;
        float closestDistance = float.MaxValue;

        // 가장 가까운 적 찾기
        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                float distance = Vector3.Distance(centerPosition, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }

        // 가장 가까운 적에게 데미지
        if (closestEnemy != null)
        {
            float damage = CalculateSkillDamage();
            closestEnemy.TakeDamage(damage);
            Log($"단일 대상 데미지 적용: {damage:F0}");
        }
        else
        {
            Log("근처에 적이 없습니다");
        }
    }

    /// <summary>
    /// 스킬 데미지 계산
    /// 
    /// 공식:
    /// 기본 데미지 × (1 + 주요 스탯 / 100)
    /// </summary>
    private float CalculateSkillDamage()
    {
        if (_currentSkill == null || _playerCharacter == null)
            return 0f;

        float baseDamage = _currentSkill.Damage;
        int mainStat = _playerCharacter.GetMainStat();

        float finalDamage = baseDamage * (1f + mainStat / 100f);

        return finalDamage;
    }

    #endregion

    #region 디버그

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[SkillAnimationController] {message}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_currentSkill == null || !_isPerformingSkill)
            return;

        // 스킬 범위 시각화
        Gizmos.color = Color.red;
        Vector3 centerPosition = transform.position;

        if (_currentSkill.SkillName.Contains("Swift"))
        {
            centerPosition += transform.forward * (_currentSkill.Range * 0.5f);
        }

        Gizmos.DrawWireSphere(centerPosition, _currentSkill.Range);
    }

    #endregion
}
