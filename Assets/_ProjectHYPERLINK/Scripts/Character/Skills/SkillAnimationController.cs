using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 스킬 애니메이션 컨트롤러
/// 
/// 마우스 거리 기반 대시:
/// - CalculateActualDashDistance(): 모드별 대시 거리 결정
/// - GetMousePositionDistance(): 마우스까지 수평 거리 계산
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class SkillAnimationController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Animator _animator;
    [SerializeField] private SkillActivationSystem _skillActivationSystem;

    [Header("마우스 방향 회전")]
    [SerializeField] private bool _rotateTowardsMouse = true;

    [Header("벽 충돌 감지 디버그")]
    [SerializeField] private bool _showDashRaycast = true;
    [SerializeField] private Color _raycastColorClear = Color.green;
    [SerializeField] private Color _raycastColorBlocked = Color.red;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;
    [SerializeField] private bool _showDebugGizmos = true;

    private NavMeshAgent _navAgent;
    private PlayerCharacter _playerCharacter;
    private Camera _mainCamera;
    private CharacterController _characterController;

    private bool _isPerformingSkill = false;
    private SkillData _currentSkill = null;
    private Coroutine _skillCoroutine = null;
    private Tweener _currentDashTween = null;

    // 레이캐스트 디버그
    private Vector3 _lastRaycastStart;
    private Vector3 _lastRaycastEnd;
    private bool _lastRaycastHit;

    // [NEW] 마우스 거리 디버그
    private Vector3 _lastMouseWorldPosition;
    private float _lastCalculatedDistance;

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
        _characterController = GetComponent<CharacterController>();
        _mainCamera = Camera.main;

        if (_animator == null || _navAgent == null)
        {
            Debug.LogError("[SkillAnimationController] 필수 컴포넌트 누락!");
            enabled = false;
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

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        CleanupDashTween();
    }

    #endregion

    #region 이벤트 핸들러

    private void HandleSkillExecuted(SkillData skill)
    {
        if (skill == null || _isPerformingSkill)
        {
            Log("스킬 실행 불가");
            return;
        }

        _currentSkill = skill;

        if (_rotateTowardsMouse)
            RotateTowardsMousePosition();

        _skillCoroutine = StartCoroutine(PerformSkillCoroutine(skill));
    }

    private void HandlePlayerHit(float damage)
    {
        if (!_isPerformingSkill)
        {
            _animator.SetTrigger(HASH_HIT);
        }
    }

    private void HandlePlayerDead()
    {
        _animator.SetBool(HASH_DEAD, true);
        _isPerformingSkill = false;
        _currentSkill = null;

        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
        }

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        CleanupDashTween();
    }

    #endregion

    #region 스킬 실행 코루틴

    private IEnumerator PerformSkillCoroutine(SkillData skill)
    {
        _isPerformingSkill = true;
        Log($"스킬 시작: {skill.SkillName}");

        // NavMeshAgent 정지
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
        }

        // Root Motion 설정
        bool wasUsingRootMotion = _animator.applyRootMotion;
        bool useDOTweenDash = !skill.UseRootMotion;

        if (skill.UseRootMotion)
        {
            _animator.applyRootMotion = true;
        }
        else
        {
            _animator.applyRootMotion = false;
        }

        // 애니메이션 트리거
        PlaySkillAnimation(skill.SkillName);

        // DOTween 대시
        if (useDOTweenDash)
        {
            float dashStartDelay = skill.AnimationDuration * skill.DashTiming;
            yield return new WaitForSeconds(dashStartDelay);

            PerformDOTweenDash(skill);
        }

        // 데미지 타이밍
        float damageDelay = useDOTweenDash
            ? skill.AnimationDuration * (skill.DamagePointTiming - skill.DashTiming)
            : skill.AnimationDuration * skill.DamagePointTiming;

        if (damageDelay > 0)
            yield return new WaitForSeconds(damageDelay);

        ApplySkillDamage(skill);

        // 나머지 애니메이션
        float remainingTime = skill.AnimationDuration * (1f - skill.DamagePointTiming);
        yield return new WaitForSeconds(remainingTime);

        // 대시 완료 대기
        if (useDOTweenDash && _currentDashTween != null && _currentDashTween.IsActive())
        {
            yield return _currentDashTween.WaitForCompletion();
        }

        // Root Motion 복구
        if (skill.UseRootMotion)
        {
            _animator.applyRootMotion = wasUsingRootMotion;
        }

        // NavMesh 동기화
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.Warp(transform.position);
            _navAgent.isStopped = false;
        }

        // 정리
        _isPerformingSkill = false;
        _currentSkill = null;
        _skillCoroutine = null;
        _currentDashTween = null;
        Log($"스킬 종료: {skill.SkillName}");
    }

    #endregion

    #region DOTween 대시

    /// <summary>
    /// DOTween 전방 대시 (벽 충돌 + 마우스 거리 지원)
    /// </summary>
    private void PerformDOTweenDash(SkillData skill)
    {
        CleanupDashTween();

        // 모드별 대시 거리 계산
        float desiredDashDistance = CalculateActualDashDistance(skill);

        // 벽 충돌 체크
        float safeDashDistance = desiredDashDistance;
        if (skill.CheckWallCollision)
        {
            safeDashDistance = CalculateSafeDashDistance(skill, desiredDashDistance);

            if (safeDashDistance < desiredDashDistance)
            {
                Log($"벽 감지! {desiredDashDistance:F2}m → {safeDashDistance:F2}m");
            }
        }

        // 목표 위치 계산
        Vector3 targetPosition = transform.position + transform.forward * safeDashDistance;
        targetPosition.y = transform.position.y;

        // DOTween 이동
        _currentDashTween = transform.DOMove(targetPosition, skill.DashDuration)
            .SetEase(skill.DashEase)
            .OnComplete(() => _currentDashTween = null);

        Log($"대시: {safeDashDistance:F2}m (모드: {skill.DashDistanceMode})");
    }

    /// <summary>
    /// 모드별 실제 대시 거리 계산
    /// </summary>
    private float CalculateActualDashDistance(SkillData skill)
    {
        switch (skill.DashDistanceMode)
        {
            case DashDistanceMode.Fixed:
                return skill.DashDistance;

            case DashDistanceMode.MouseDistance:
                float mouseDistance = GetMousePositionDistance();
                float clampedDistance = Mathf.Clamp(mouseDistance,
                    skill.MinDashDistance, skill.MaxDashDistance);

                _lastCalculatedDistance = clampedDistance;
                Log($"마우스 거리: {mouseDistance:F2}m → 클램핑: {clampedDistance:F2}m " +
                    $"(범위: {skill.MinDashDistance}-{skill.MaxDashDistance}m)");

                return clampedDistance;

            default:
                return skill.DashDistance;
        }
    }

    /// <summary>
    /// 마우스 위치까지 수평 거리 계산
    /// </summary>
    private float GetMousePositionDistance()
    {
        if (_mainCamera == null)
        {
            Log("카메라 없음 - 기본 거리 반환");
            return 5f;
        }

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            _lastMouseWorldPosition = hit.point;

            // 수평 거리만 계산 (Y축 무시)
            Vector3 characterPos = transform.position;
            Vector3 mousePos = hit.point;
            characterPos.y = 0;
            mousePos.y = 0;

            float distance = Vector3.Distance(characterPos, mousePos);
            return distance;
        }

        Log("마우스 레이캐스트 실패 - 기본 거리 반환");
        return 5f;
    }

    /// <summary>
    /// 벽까지 안전 거리 계산
    /// </summary>
    private float CalculateSafeDashDistance(SkillData skill, float desiredDistance)
    {
        float characterHeight = _characterController != null
            ? _characterController.height * 0.5f
            : 1.0f;

        Vector3 rayStart = transform.position + Vector3.up * characterHeight;
        Vector3 rayDirection = transform.forward;

        _lastRaycastStart = rayStart;
        _lastRaycastEnd = rayStart + rayDirection * desiredDistance;
        _lastRaycastHit = false;

        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit, desiredDistance, skill.WallLayer))
        {
            _lastRaycastHit = true;
            _lastRaycastEnd = hit.point;

            float safeDistance = Mathf.Max(0f, hit.distance - skill.WallStopBuffer);

            if (_showDashRaycast)
            {
                Debug.DrawRay(rayStart, rayDirection * hit.distance, _raycastColorBlocked, 2f);
            }

            return safeDistance;
        }

        if (_showDashRaycast)
        {
            Debug.DrawRay(rayStart, rayDirection * desiredDistance, _raycastColorClear, 2f);
        }

        return desiredDistance;
    }

    private void CleanupDashTween()
    {
        if (_currentDashTween != null && _currentDashTween.IsActive())
        {
            _currentDashTween.Kill();
            _currentDashTween = null;
        }
    }

    #endregion

    #region 회전 제어

    private void RotateTowardsMousePosition()
    {
        if (_mainCamera == null) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 direction = hit.point - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    #endregion

    #region 애니메이션 제어

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
                Debug.LogWarning($"알 수 없는 스킬: {skillName}");
                break;
        }
    }

    #endregion

    #region 데미지 처리

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
    }

    private void ApplyAOEDamage(SkillData skill)
    {
        Vector3 centerPosition = transform.position + transform.TransformDirection(skill.AoeOffset);
        Collider[] hits;
        int enemyCount = 0;

        if (skill.AoeShape == AOEShape.Sphere)
        {
            hits = Physics.OverlapSphere(centerPosition, skill.Range);
        }
        else
        {
            hits = Physics.OverlapBox(centerPosition, skill.BoxSize * 0.5f, transform.rotation);
        }

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

        Log($"AOE: {enemyCount}명 타격");
    }

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
        }
    }

    private float CalculateSkillDamage(SkillData skill)
    {
        if (skill == null || _playerCharacter == null)
            return 0f;

        float baseDamage = skill.Damage;
        int mainStat = _playerCharacter.GetMainStat();
        return baseDamage * (1f + mainStat / 100f);
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
        if (!_showDebugGizmos)
            return;

        // AOE 범위
        if (_currentSkill != null && _isPerformingSkill)
        {
            Gizmos.color = Color.red;
            Vector3 centerPosition = transform.position + transform.TransformDirection(_currentSkill.AoeOffset);

            if (_currentSkill.AoeShape == AOEShape.Sphere)
            {
                Gizmos.DrawWireSphere(centerPosition, _currentSkill.Range);
            }
            else
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(centerPosition, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, _currentSkill.BoxSize);
                Gizmos.matrix = oldMatrix;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, centerPosition);

            // 대시 방향
            if (!_currentSkill.UseRootMotion)
            {
                Gizmos.color = Color.cyan;
                float displayDistance = _currentSkill.DashDistanceMode == DashDistanceMode.MouseDistance
                    ? _lastCalculatedDistance
                    : _currentSkill.DashDistance;

                Vector3 dashEnd = transform.position + transform.forward * displayDistance;
                Gizmos.DrawLine(transform.position, dashEnd);
                Gizmos.DrawWireSphere(dashEnd, 0.5f);
            }
        }

        // 레이캐스트 시각화
        if (_showDashRaycast && _lastRaycastStart != Vector3.zero)
        {
            Gizmos.color = _lastRaycastHit ? _raycastColorBlocked : _raycastColorClear;
            Gizmos.DrawLine(_lastRaycastStart, _lastRaycastEnd);
            Gizmos.DrawWireSphere(_lastRaycastEnd, 0.3f);
        }

        // 마우스 위치 시각화
        if (_currentSkill != null &&
            _currentSkill.DashDistanceMode == DashDistanceMode.MouseDistance &&
            _lastMouseWorldPosition != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_lastMouseWorldPosition, 0.5f);
            Gizmos.DrawLine(transform.position, _lastMouseWorldPosition);
        }
    }

    #endregion
}
