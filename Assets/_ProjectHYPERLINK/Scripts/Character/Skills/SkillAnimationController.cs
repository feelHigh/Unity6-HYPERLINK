using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// 스킬 애니메이션 컨트롤러 (리팩토링 버전)
/// 
/// 주요 변경사항:
/// - [FIXED] NavMeshAgent 비활성화 제거 → isStopped 사용
/// - [NEW] Root Motion 기반 코루틴 처리 (PerformDashAttack 패턴)
/// - [NEW] AOE 형태별 데미지 처리 (Sphere/Box)
/// - [NEW] 타이밍 기반 데미지 적용
/// - [NEW] 오프셋 위치 지원
/// 
/// 핵심 개선:
/// 1. NavMeshAgent를 비활성화하지 않음 (Root Motion과 충돌 방지)
/// 2. agent.isStopped + ResetPath로 제어
/// 3. 애니메이션 종료 시 agent.Warp()로 동기화
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class SkillAnimationController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Animator _animator;
    [SerializeField] private SkillActivationSystem _skillActivationSystem;

    [Header("마우스 방향 회전")]
    [Tooltip("스킬 사용 시 마우스 커서 방향으로 회전")]
    [SerializeField] private bool _rotateTowardsMouse = true;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;
    [SerializeField] private bool _showDebugGizmos = true;

    // 컴포넌트 캐시
    private NavMeshAgent _navAgent;
    private PlayerCharacter _playerCharacter;
    private Camera _mainCamera;

    // 상태 추적
    private bool _isPerformingSkill = false;
    private SkillData _currentSkill = null;
    private Coroutine _skillCoroutine = null;

    // 애니메이터 파라미터 해시 (최적화)
    private static readonly int HASH_SKILL_JUDGEMENT = Animator.StringToHash("SkillJudgement");
    private static readonly int HASH_SKILL_SWIFT_SLASH = Animator.StringToHash("SkillSwiftSlash");
    private static readonly int HASH_SKILL_CONVICTION = Animator.StringToHash("SkillConviction");
    private static readonly int HASH_HIT = Animator.StringToHash("Hit");
    private static readonly int HASH_DEAD = Animator.StringToHash("Dead");

    #region 초기화

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (_skillActivationSystem == null)
            _skillActivationSystem = GetComponent<SkillActivationSystem>();

        _navAgent = GetComponent<NavMeshAgent>();
        _playerCharacter = GetComponent<PlayerCharacter>();
        _mainCamera = Camera.main;

        if (_animator == null)
        {
            Debug.LogError("[SkillAnimationController] Animator를 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        if (_navAgent == null)
        {
            Debug.LogError("[SkillAnimationController] NavMeshAgent를 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        if (_mainCamera == null)
        {
            Debug.LogWarning("[SkillAnimationController] Main Camera를 찾을 수 없습니다!");
        }
    }

    private void OnEnable()
    {
        if (_skillActivationSystem != null)
            SkillActivationSystem.OnSkillExecuted += HandleSkillExecuted;

        if (_playerCharacter != null)
        {
            PlayerCharacter.OnPlayerHit += HandlePlayerHit;
            PlayerCharacter.OnPlayerDead += HandlePlayerDead;
        }
    }

    private void OnDisable()
    {
        if (_skillActivationSystem != null)
            SkillActivationSystem.OnSkillExecuted -= HandleSkillExecuted;

        if (_playerCharacter != null)
        {
            PlayerCharacter.OnPlayerHit -= HandlePlayerHit;
            PlayerCharacter.OnPlayerDead -= HandlePlayerDead;
        }

        // 진행 중인 코루틴 정리
        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }
    }

    #endregion

    #region 이벤트 핸들러

    /// <summary>
    /// 스킬 실행 시 호출
    /// </summary>
    private void HandleSkillExecuted(SkillData skill)
    {
        if (skill == null) return;

        // 이미 스킬 실행 중이면 무시
        if (_isPerformingSkill)
        {
            Log("이미 스킬 실행 중입니다!");
            return;
        }

        _currentSkill = skill;

        // 마우스 방향으로 회전
        if (_rotateTowardsMouse)
            RotateTowardsMousePosition();

        // 스킬 코루틴 시작
        _skillCoroutine = StartCoroutine(PerformSkillCoroutine(skill));
    }

    /// <summary>
    /// 플레이어 피격 시 호출
    /// </summary>
    private void HandlePlayerHit(float damage)
    {
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
        _currentSkill = null;

        // NavMeshAgent 완전 정지
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
        }

        // 진행 중인 스킬 코루틴 정지
        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        Log("사망 - 모든 행동 정지");
    }

    #endregion

    #region 스킬 실행 코루틴

    /// <summary>
    /// 스킬 실행 코루틴 (PerformDashAttack 패턴 적용)
    /// 
    /// 처리 과정:
    /// 1. NavMeshAgent 이동 중지 (enabled는 유지!)
    /// 2. Root Motion 활성화 (필요 시)
    /// 3. 애니메이션 트리거
    /// 4. 타이밍에 맞춰 데미지 적용
    /// 5. 애니메이션 종료 대기
    /// 6. NavMesh 위치 동기화 (Warp)
    /// 7. 정리 및 복구
    /// </summary>
    private IEnumerator PerformSkillCoroutine(SkillData skill)
    {
        _isPerformingSkill = true;
        Log($"스킬 시작: {skill.SkillName}");

        // 1. NavMeshAgent 이동 중지 (비활성화 X)
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
            Log("NavMeshAgent 이동 중지");
        }

        // 2. Root Motion 활성화 (스킬 설정에 따라)
        bool wasUsingRootMotion = _animator.applyRootMotion;
        if (skill.UseRootMotion)
        {
            _animator.applyRootMotion = true;
            Log("Root Motion 활성화");
        }

        // 3. 애니메이션 트리거 실행
        PlaySkillAnimation(skill.SkillName);

        // 4. 데미지 적용 타이밍까지 대기
        float damageDelay = skill.AnimationDuration * skill.DamagePointTiming;
        yield return new WaitForSeconds(damageDelay);

        // 5. 데미지 적용
        ApplySkillDamage(skill);

        // 6. 나머지 애니메이션 재생 대기
        float remainingTime = skill.AnimationDuration * (1f - skill.DamagePointTiming);
        yield return new WaitForSeconds(remainingTime);

        // 7. Root Motion 복구
        if (skill.UseRootMotion)
        {
            _animator.applyRootMotion = wasUsingRootMotion;
            Log("Root Motion 복구");
        }

        // 8. NavMesh 위치 동기화 (중요!)
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.Warp(transform.position);
            Log("NavMesh 위치 동기화");
        }

        // 9. NavMeshAgent 이동 재개
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.isStopped = false;
            Log("NavMeshAgent 이동 재개");
        }

        // 10. 정리
        _isPerformingSkill = false;
        _currentSkill = null;
        _skillCoroutine = null;
        Log($"스킬 종료: {skill.SkillName}");
    }

    #endregion

    #region 회전 제어

    /// <summary>
    /// 마우스 커서 방향으로 캐릭터 회전
    /// </summary>
    private void RotateTowardsMousePosition()
    {
        if (_mainCamera == null)
        {
            Log("카메라가 없어 회전할 수 없습니다!");
            return;
        }

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 targetPosition = hit.point;
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = targetRotation;
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
                break;
        }
    }

    #endregion

    #region 데미지 처리

    /// <summary>
    /// 스킬 데미지 적용 (형태별 분기)
    /// </summary>
    private void ApplySkillDamage(SkillData skill)
    {
        if (skill.SkillType == SkillType.AreaOfEffect)
        {
            ApplyAOEDamage(skill);
        }
        else if (skill.SkillType == SkillType.Melee)
        {
            ApplyMeleeDamage(skill);
        }
        else
        {
            Log($"데미지 적용 생략 (타입: {skill.SkillType})");
        }
    }

    /// <summary>
    /// AOE 데미지 적용 (Sphere 또는 Box)
    /// 
    /// 처리:
    /// 1. 오프셋을 적용한 중심 위치 계산
    /// 2. 형태에 따라 Collider 검색 (Sphere/Box)
    /// 3. 범위 내 적에게 데미지 적용
    /// </summary>
    private void ApplyAOEDamage(SkillData skill)
    {
        // 오프셋 적용한 중심 위치 계산 (로컬 → 월드)
        Vector3 centerPosition = transform.position + transform.TransformDirection(skill.AoeOffset);

        Collider[] hits;
        int enemyCount = 0;

        // 형태에 따라 Collider 검색
        if (skill.AoeShape == AOEShape.Sphere)
        {
            // Sphere 검색
            hits = Physics.OverlapSphere(centerPosition, skill.Range);
            Log($"Sphere AOE 검색: 중심({centerPosition}), 반경({skill.Range})");
        }
        else // AOEShape.Box
        {
            // Box 검색 (전방 향하는 박스)
            Quaternion boxRotation = transform.rotation;
            hits = Physics.OverlapBox(centerPosition, skill.BoxSize * 0.5f, boxRotation);
            Log($"Box AOE 검색: 중심({centerPosition}), 크기({skill.BoxSize})");
        }

        // 범위 내 적에게 데미지 적용
        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                float damage = CalculateSkillDamage(skill);
                enemy.TakeDamage(damage);
                enemyCount++;
            }
        }

        Log($"AOE 데미지 적용: {enemyCount}명 타격");
    }

    /// <summary>
    /// 근거리 스킬 데미지 적용 (단일 대상)
    /// </summary>
    private void ApplyMeleeDamage(SkillData skill)
    {
        Vector3 centerPosition = transform.position + transform.forward * 2f;
        Collider[] hits = Physics.OverlapSphere(centerPosition, skill.Range);

        EnemyController closestEnemy = null;
        float closestDistance = float.MaxValue;

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

        if (closestEnemy != null)
        {
            float damage = CalculateSkillDamage(skill);
            closestEnemy.TakeDamage(damage);
            Log($"근거리 데미지 적용: {damage:F0}");
        }
        else
        {
            Log("근처에 적이 없습니다");
        }
    }

    /// <summary>
    /// 스킬 데미지 계산
    /// 
    /// 공식: 기본 데미지 × (1 + 주요 스탯 / 100)
    /// </summary>
    private float CalculateSkillDamage(SkillData skill)
    {
        if (skill == null || _playerCharacter == null)
            return 0f;

        float baseDamage = skill.Damage;
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

    /// <summary>
    /// Gizmo로 AOE 범위 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!_showDebugGizmos || _currentSkill == null || !_isPerformingSkill)
            return;

        Gizmos.color = Color.red;

        // 오프셋 적용한 중심 위치
        Vector3 centerPosition = transform.position + transform.TransformDirection(_currentSkill.AoeOffset);

        // 형태에 따라 다르게 그리기
        if (_currentSkill.AoeShape == AOEShape.Sphere)
        {
            Gizmos.DrawWireSphere(centerPosition, _currentSkill.Range);
        }
        else // Box
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(centerPosition, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _currentSkill.BoxSize);
            Gizmos.matrix = oldMatrix;
        }

        // 오프셋 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, centerPosition);
    }

    #endregion
}
