using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 스킬 애니메이션 컨트롤러
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

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;
    [SerializeField] private bool _showDebugGizmos = true;

    private NavMeshAgent _navAgent;
    private PlayerCharacter _playerCharacter;
    private PlayerNavController _navController;
    private PlayerAttackController _attackController;
    private PlayerStateController _stateController; // ⭐ 추가
    private Camera _mainCamera;

    private bool _isPerformingSkill = false;
    private SkillData _currentSkill = null;
    private Coroutine _skillCoroutine = null;
    private Tweener _currentDashTween = null;

    private Dictionary<string, int> _animatorHashCache = new Dictionary<string, int>();
    private static readonly int HASH_HIT = Animator.StringToHash("Hit");
    private static readonly int HASH_DEAD = Animator.StringToHash("Dead");

    private List<Coroutine> _activeVfxCoroutines = new List<Coroutine>();
    private List<Coroutine> _activeHitAreaCoroutines = new List<Coroutine>();

    #region 초기화

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (_skillActivationSystem == null)
            _skillActivationSystem = GetComponent<SkillActivationSystem>();

        _navAgent = GetComponent<NavMeshAgent>();
        _playerCharacter = GetComponent<PlayerCharacter>();
        _navController = GetComponent<PlayerNavController>();
        _attackController = GetComponent<PlayerAttackController>();
        _stateController = GetComponent<PlayerStateController>(); // ⭐ 추가
        _mainCamera = Camera.main;

        if (_animator == null || _navAgent == null)
        {
            Debug.LogError("[SkillAnimationController] 필수 컴포넌트 누락!");
            enabled = false;
        }

        if (_attackController == null)
        {
            Debug.LogWarning("[SkillAnimationController] PlayerAttackController가 없습니다!");
        }

        // StateController 확인
        if (_stateController == null)
        {
            Debug.LogWarning("[SkillAnimationController] PlayerStateController가 없습니다!");
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

        CleanupAllVfxCoroutines();
        CleanupAllHitAreaCoroutines();
        CleanupDashTween();

        if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
        {
            _navAgent.updateRotation = true;
            _navAgent.isStopped = false;
        }
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

        if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
            _navAgent.updateRotation = false;
            Log("NavMeshAgent 제어: 이동 정지");
        }

        if (_rotateTowardsMouse)
        {
            RotateTowardsMousePosition();
        }

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
        _animator.SetTrigger(HASH_DEAD);
        _isPerformingSkill = false;
        _currentSkill = null;

        // 스킬 상태 해제
        if (_stateController != null)
        {
            _stateController.SetSkillPerforming(false);
        }

        if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
            _navAgent.updateRotation = false;
        }

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        CleanupAllVfxCoroutines();
        CleanupAllHitAreaCoroutines();
        CleanupDashTween();
    }

    #endregion

    #region 스킬 실행 코루틴

    /// <summary>
    /// 스킬 실행 코루틴
    /// </summary>
    private IEnumerator PerformSkillCoroutine(SkillData skill)
    {
        _isPerformingSkill = true;
        Log($"스킬 시작: {skill.SkillName}");

        bool wasUsingRootMotion = _animator.applyRootMotion;
        bool useDOTweenDash = !skill.UseRootMotion;

        _animator.applyRootMotion = skill.UseRootMotion;

        PlaySkillAnimation(skill);

        // VFX 생성
        SpawnAllSkillVFX(skill);

        // Hit Area 적용
        ApplyAllHitAreas(skill);

        // DOTween 대시
        if (useDOTweenDash)
        {
            float dashStartDelay = skill.AnimationDuration * skill.DashTiming;
            yield return new WaitForSeconds(dashStartDelay);
            PerformDOTweenDash(skill);
        }

        // 애니메이션 종료 대기
        yield return new WaitForSeconds(skill.AnimationDuration);

        _animator.applyRootMotion = wasUsingRootMotion;

        _isPerformingSkill = false;
        _currentSkill = null;
        _skillCoroutine = null;

        // 스킬 실행 상태 해제
        if (_stateController != null)
        {
            _stateController.SetSkillPerforming(false);
        }

        if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
        {
            _navAgent.updateRotation = true;
            _navAgent.isStopped = false;
            Log("NavMeshAgent 상태 복원");
        }

        Log("스킬 종료");
    }

    #endregion

    #region 대시 시스템

    private void PerformDOTweenDash(SkillData skill)
    {
        CleanupDashTween();
        float actualDashDistance = skill.DashDistance;

        if (skill.CheckWallCollision)
        {
            actualDashDistance = CheckWallCollision(actualDashDistance, skill.WallLayer, skill.WallStopBuffer);
        }

        Vector3 targetPosition = transform.position + transform.forward * actualDashDistance;
        Log($"DOTween 대시 실행: {actualDashDistance:F2}m");

        _currentDashTween = transform.DOMove(targetPosition, skill.DashDuration)
            .SetEase(skill.DashEase)
            .OnComplete(() =>
            {
                _currentDashTween = null;
                Log("대시 완료");
            });
    }

    private float CheckWallCollision(float requestedDistance, LayerMask wallLayer, float buffer)
    {
        RaycastHit hit;
        bool didHit = Physics.Raycast(
            transform.position,
            transform.forward,
            out hit,
            requestedDistance,
            wallLayer
        );

        if (didHit)
        {
            float safeDistance = Mathf.Max(0, hit.distance - buffer);
            Log($"벽 감지: {hit.distance:F2}m → 안전 거리: {safeDistance:F2}m");
            return safeDistance;
        }

        return requestedDistance;
    }

    private void CleanupDashTween()
    {
        if (_currentDashTween != null && _currentDashTween.IsActive())
        {
            _currentDashTween.Kill();
            _currentDashTween = null;
            Log("대시 트윈 정리");
        }
    }

    #endregion

    #region 회전

    private void RotateTowardsMousePosition()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 targetDirection = (hit.point - transform.position).normalized;
            targetDirection.y = 0;

            if (targetDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(targetDirection);
                Log($"마우스 방향 회전: {transform.eulerAngles.y:F1}°");
            }
        }
    }

    #endregion

    #region 애니메이션 제어

    private void PlaySkillAnimation(SkillData skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.AnimatorTriggerName))
        {
            Debug.LogError($"[SkillAnimationController] 트리거 이름 누락: {skill?.SkillName}");
            return;
        }

        int hash;
        if (!_animatorHashCache.TryGetValue(skill.AnimatorTriggerName, out hash))
        {
            hash = Animator.StringToHash(skill.AnimatorTriggerName);
            _animatorHashCache[skill.AnimatorTriggerName] = hash;
        }

        _animator.SetTrigger(hash);
        Log($"애니메이션 재생: {skill.SkillName}");
    }

    #endregion

    #region Hit Area 처리

    private void ApplyAllHitAreas(SkillData skill)
    {
        HitAreaConfig[] hitAreaConfigs = skill.GetHitAreaConfigs();

        if (hitAreaConfigs == null || hitAreaConfigs.Length == 0)
        {
            Log($"[{skill.SkillName}] Hit Area 없음");
            return;
        }

        Log($"[{skill.SkillName}] {hitAreaConfigs.Length}개 Hit Area 준비");

        foreach (HitAreaConfig config in hitAreaConfigs)
        {
            if (config.IsValid())
            {
                float delay = skill.AnimationDuration * config.DamagePointTiming;
                Coroutine hitCoroutine = StartCoroutine(ApplySingleHitArea(config, delay, skill));
                _activeHitAreaCoroutines.Add(hitCoroutine);
            }
            else
            {
                Debug.LogWarning($"[{skill.SkillName}] 유효하지 않은 HitAreaConfig");
            }
        }
    }

    private IEnumerator ApplySingleHitArea(HitAreaConfig config, float delay, SkillData skill)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        Vector3 centerPosition = transform.position + transform.TransformDirection(config.PositionOffset);
        Collider[] hits;
        int enemyCount = 0;

        if (config.Shape == HitAreaShape.Sphere)
        {
            hits = Physics.OverlapSphere(centerPosition, config.SphereRadius);
        }
        else // Box
        {
            Quaternion boxRotation = transform.rotation * Quaternion.Euler(config.RotationOffset);
            hits = Physics.OverlapBox(centerPosition, config.BoxSize * 0.5f, boxRotation);
        }

        float baseDamage = CalculateSkillDamage(skill);
        float finalDamage = baseDamage * config.DamageMultiplier;

        foreach (Collider hit in hits)
        {
            if (config.ShouldIgnoreTag(hit.tag))
                continue;

            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(finalDamage);
                enemyCount++;
            }
        }

        Log($"Hit Area 적용: {enemyCount}명 타격 (데미지: {finalDamage:F1})");
    }

    private void CleanupAllHitAreaCoroutines()
    {
        foreach (Coroutine coroutine in _activeHitAreaCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        _activeHitAreaCoroutines.Clear();
        Log("모든 Hit Area 코루틴 정리");
    }

    #endregion

    #region 데미지 계산

    private float CalculateSkillDamage(SkillData skill)
    {
        if (skill == null || _playerCharacter == null || _attackController == null)
        {
            Debug.LogWarning("[SkillAnimationController] 스킬 데미지 계산 실패");
            return 0f;
        }

        float attackDamage = _attackController.AttackDamage;
        int mainStat = _playerCharacter.GetMainStat();

        float damage = ((attackDamage * skill.SkillMultiplier) + skill.SkillBaseDamage)
                     * (1f + (mainStat * skill.MainStatDamageIncrease));

        return damage;
    }

    #endregion

    #region VFX 처리

    private void SpawnAllSkillVFX(SkillData skill)
    {
        VfxConfig[] vfxConfigs = skill.GetVfxConfigs();

        if (vfxConfigs == null || vfxConfigs.Length == 0)
        {
            Log($"[{skill.SkillName}] VFX 없음");
            return;
        }

        Log($"[{skill.SkillName}] {vfxConfigs.Length}개 VFX 준비");

        foreach (VfxConfig config in vfxConfigs)
        {
            if (config.IsValid())
            {
                float delay = skill.AnimationDuration * config.SpawnTiming;
                Coroutine vfxCoroutine = StartCoroutine(SpawnSingleVFX(config, delay, skill.SkillName));
                _activeVfxCoroutines.Add(vfxCoroutine);
            }
        }
    }

    private IEnumerator SpawnSingleVFX(VfxConfig config, float delay, string skillName)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        Vector3 spawnPosition = transform.position + transform.TransformDirection(config.PositionOffset);
        Quaternion spawnRotation = transform.rotation * Quaternion.Euler(config.RotationOffset);

        GameObject vfxInstance = Instantiate(config.VfxPrefab, spawnPosition, spawnRotation);

        if (config.AttachToCharacter)
        {
            vfxInstance.transform.SetParent(transform);
        }

        Log($"[{skillName}] VFX 생성: {vfxInstance.name}");

        float lifetime = config.Lifetime;
        if (lifetime <= 0)
        {
            ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            }
            else
            {
                lifetime = 2f;
            }
        }

        Destroy(vfxInstance, lifetime);
    }

    private void CleanupAllVfxCoroutines()
    {
        foreach (Coroutine coroutine in _activeVfxCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        _activeVfxCoroutines.Clear();
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
        if (!_showDebugGizmos || _currentSkill == null || !_isPerformingSkill)
            return;

        HitAreaConfig[] hitAreas = _currentSkill.GetHitAreaConfigs();
        if (hitAreas == null || hitAreas.Length == 0)
            return;

        foreach (HitAreaConfig config in hitAreas)
        {
            if (!config.IsValid())
                continue;

            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Vector3 centerPosition = transform.position + transform.TransformDirection(config.PositionOffset);

            if (config.Shape == HitAreaShape.Sphere)
            {
                Gizmos.DrawWireSphere(centerPosition, config.SphereRadius);
            }
            else
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Quaternion boxRotation = transform.rotation * Quaternion.Euler(config.RotationOffset);
                Gizmos.matrix = Matrix4x4.TRS(centerPosition, boxRotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, config.BoxSize);
                Gizmos.matrix = oldMatrix;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, centerPosition);
        }
    }

    #endregion
}
