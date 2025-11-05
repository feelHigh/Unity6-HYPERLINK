using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 스킬 애니메이션 컨트롤러
/// 
/// 리팩토링 변경사항:
/// - PlaySkillAnimation() 메서드 단순화: switch-case 제거
/// - SkillData의 AnimatorTriggerName을 직접 사용
/// - 애니메이터 해시 캐싱 시스템 추가 (성능 최적화)
/// - 새 스킬 추가 시 코드 수정 불필요
/// 
/// VFX 시스템 리팩토링 (다중 VFX 지원):
/// - SpawnSkillVFX → SpawnAllSkillVFX로 변경 (여러 VFX 처리)
/// - 각 VfxConfig마다 독립적인 코루틴 시작
/// - 하위 호환성: 레거시 단일 VFX도 여전히 작동
/// 
/// 마우스 거리 기반 대시:
/// - CalculateActualDashDistance(): 모드별 대시 거리 결정
/// - GetMousePositionDistance(): 마우스까지 수평 거리 계산
/// 
/// AOE 크기 설정:
/// - Sphere: SphereRadius 사용
/// - Box: BoxSize 사용
/// 
/// 수정사항 (스킬 회전 문제 해결):
/// - HandleSkillExecuted()에서 회전 전에 NavMeshAgent 제어
/// - NavMeshAgent.updateRotation 비활성화/재활성화 로직 추가
/// - 이동 중 스킬 사용 시 올바른 방향 회전 보장
/// 
/// 수정사항 (데미지 계산 리팩토링):
/// - PlayerNavController 참조 추가
/// - 새로운 데미지 공식: ((공격력 × 배율) + 기본데미지) × (1 + (주요스탯 × 증가율))
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
    private PlayerNavController _navController;
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

    // 마우스 거리 디버그
    private Vector3 _lastMouseWorldPosition;
    private float _lastCalculatedDistance;

    // 애니메이터 해시 캐싱 시스템
    private Dictionary<string, int> _animatorHashCache = new Dictionary<string, int>();

    // 고정 애니메이션 해시 (Hit, Dead 등 플레이어 상태용)
    private static readonly int HASH_HIT = Animator.StringToHash("Hit");
    private static readonly int HASH_DEAD = Animator.StringToHash("Dead");

    // 다중 VFX 관리
    private List<Coroutine> _activeVfxCoroutines = new List<Coroutine>();

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

        // 모든 VFX 코루틴 정리
        CleanupAllVfxCoroutines();

        CleanupDashTween();

        // NavMeshAgent 상태 복원
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.updateRotation = true;
            _navAgent.isStopped = false;
        }
    }

    #endregion

    #region 이벤트 핸들러

    /// <summary>
    /// 스킬 실행 이벤트 핸들러
    /// 
    /// 수정사항:
    /// - 회전 전에 NavMeshAgent 제어 (이동 정지 + 자동 회전 비활성화)
    /// - 이제 이동 중에도 마우스 방향으로 올바르게 회전
    /// </summary>
    private void HandleSkillExecuted(SkillData skill)
    {
        if (skill == null || _isPerformingSkill)
        {
            Log("스킬 실행 불가");
            return;
        }

        _currentSkill = skill;

        // 회전 전에 NavMeshAgent 제어
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.isStopped = true;           // 이동 정지
            _navAgent.ResetPath();                 // 경로 초기화
            _navAgent.updateRotation = false;      // 자동 회전 비활성화
            Log("NavMeshAgent 제어: 이동 정지 + updateRotation OFF");
        }

        // 이제 회전이 NavMeshAgent에 의해 덮어씌워지지 않음
        if (_rotateTowardsMouse)
        {
            RotateTowardsMousePosition();
            Log($"마우스 방향 회전 완료: {transform.rotation.eulerAngles.y:F1}도");
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

    /// <summary>
    /// 플레이어 사망 이벤트 핸들러
    /// 
    /// 수정사항:
    /// - updateRotation 비활성화 추가
    /// - 모든 VFX 코루틴 정리
    /// </summary>
    private void HandlePlayerDead()
    {
        _animator.SetTrigger(HASH_DEAD);
        _isPerformingSkill = false;
        _currentSkill = null;

        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
            _navAgent.updateRotation = false;  // 사망 시 회전 비활성화
        }

        if (_skillCoroutine != null)
        {
            StopCoroutine(_skillCoroutine);
            _skillCoroutine = null;
        }

        CleanupAllVfxCoroutines();
        CleanupDashTween();
    }

    #endregion

    #region 스킬 실행 코루틴

    /// <summary>
    /// 스킬 실행 코루틴
    /// 
    /// 수정사항 (다중 VFX 지원):
    /// - GetVfxConfigs()로 여러 VFX 가져오기
    /// - 각 VfxConfig마다 독립적인 코루틴 시작
    /// - 레거시 VFX 코드 제거 (GetVfxConfigs()가 자동 처리)
    /// </summary>
    private IEnumerator PerformSkillCoroutine(SkillData skill)
    {
        _isPerformingSkill = true;
        Log($"스킬 시작: {skill.SkillName}");

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

        // 애니메이션 트리거 (동적 처리)
        PlaySkillAnimation(skill);

        // VFX 생성 - 다중 VFX 지원
        SpawnAllSkillVFX(skill);

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

        // 애니메이션 종료 대기
        float remainingTime = skill.AnimationDuration - (useDOTweenDash
            ? skill.AnimationDuration * skill.DamagePointTiming
            : skill.AnimationDuration * skill.DamagePointTiming);

        if (remainingTime > 0)
            yield return new WaitForSeconds(remainingTime);

        // Root Motion 복원
        _animator.applyRootMotion = wasUsingRootMotion;

        // 스킬 종료
        _isPerformingSkill = false;
        _currentSkill = null;
        _skillCoroutine = null;

        // NavMeshAgent 상태 복원
        if (_navAgent != null && _navAgent.enabled)
        {
            _navAgent.updateRotation = true;  // 회전 자동 제어 재활성화
            _navAgent.isStopped = false;       // 이동 가능 상태로 복원
            Log("NavMeshAgent 상태 복원: updateRotation ON + 이동 가능");
        }

        Log("스킬 종료");
    }

    #endregion

    #region 대시 시스템

    /// <summary>
    /// DOTween 기반 대시 실행
    /// 
    /// 변경사항:
    /// - 벽 충돌 감지 지원
    /// - MouseDistance 모드 지원
    /// </summary>
    private void PerformDOTweenDash(SkillData skill)
    {
        CleanupDashTween();

        // 실제 대시 거리 계산
        float actualDashDistance = CalculateActualDashDistance(skill);

        // 벽 충돌 체크
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

    /// <summary>
    /// 실제 대시 거리 계산
    /// 
    /// Fixed 모드: DashDistance 사용
    /// MouseDistance 모드: 마우스 거리 기반 (Min ~ Max)
    /// </summary>
    private float CalculateActualDashDistance(SkillData skill)
    {
        if (skill.DashDistanceMode == DashDistanceMode.Fixed)
        {
            return skill.DashDistance;
        }
        else // MouseDistance
        {
            float mouseDistance = GetMousePositionDistance();
            _lastCalculatedDistance = Mathf.Clamp(mouseDistance, skill.MinDashDistance, skill.MaxDashDistance);
            Log($"마우스 거리: {mouseDistance:F2}m → 대시 거리: {_lastCalculatedDistance:F2}m");
            return _lastCalculatedDistance;
        }
    }

    /// <summary>
    /// 마우스 위치까지 수평 거리 계산
    /// </summary>
    private float GetMousePositionDistance()
    {
        if (_mainCamera == null)
            return 0f;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            _lastMouseWorldPosition = hit.point;

            // Y축 무시한 수평 거리
            Vector3 playerPos = transform.position;
            Vector3 mousePos = hit.point;
            playerPos.y = 0;
            mousePos.y = 0;

            return Vector3.Distance(playerPos, mousePos);
        }

        return 0f;
    }

    /// <summary>
    /// 벽 충돌 감지
    /// 
    /// 대시 경로에 벽이 있으면 벽 앞까지만 대시
    /// </summary>
    private float CheckWallCollision(float requestedDistance, LayerMask wallLayer, float buffer)
    {
        _lastRaycastStart = transform.position;
        _lastRaycastEnd = transform.position + transform.forward * requestedDistance;

        RaycastHit hit;
        _lastRaycastHit = Physics.Raycast(
            transform.position,
            transform.forward,
            out hit,
            requestedDistance,
            wallLayer
        );

        if (_lastRaycastHit)
        {
            float safeDistance = Mathf.Max(0f, hit.distance - buffer);
            _lastRaycastEnd = transform.position + transform.forward * safeDistance;
            Log($"벽 감지! 대시 거리 조정: {requestedDistance:F2}m → {safeDistance:F2}m");
            return safeDistance;
        }

        return requestedDistance;
    }

    private void CleanupDashTween()
    {
        if (_currentDashTween != null)
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

    /// <summary>
    /// 스킬 애니메이션 재생
    /// 
    /// 변경사항:
    /// - switch-case 제거
    /// - SkillData의 AnimatorTriggerName을 직접 사용
    /// - 애니메이터 해시 캐싱으로 성능 최적화
    /// - 새 스킬 추가 시 코드 수정 불필요
    /// </summary>
    private void PlaySkillAnimation(SkillData skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.AnimatorTriggerName))
        {
            Debug.LogError($"[SkillAnimationController] 스킬 애니메이션 트리거 이름이 설정되지 않았습니다: {skill?.SkillName}");
            return;
        }

        // 애니메이터 해시 캐싱
        int hash;
        if (!_animatorHashCache.TryGetValue(skill.AnimatorTriggerName, out hash))
        {
            hash = Animator.StringToHash(skill.AnimatorTriggerName);
            _animatorHashCache[skill.AnimatorTriggerName] = hash;
            Log($"애니메이터 해시 캐싱: {skill.AnimatorTriggerName} → {hash}");
        }

        // 애니메이션 트리거 실행
        _animator.SetTrigger(hash);
        Log($"애니메이션 재생: {skill.SkillName} (트리거: {skill.AnimatorTriggerName})");
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
            hits = Physics.OverlapSphere(centerPosition, skill.SphereRadius);
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

    /// <summary>
    /// 스킬 데미지 계산
    /// 
    /// 공식: ((공격력 × 배율) + 기본데미지) × (1 + (주요스탯 × 증가율))
    /// </summary>
    private float CalculateSkillDamage(SkillData skill)
    {
        if (skill == null || _playerCharacter == null || _navController == null)
            return 0f;

        float attackDamage = _navController.AttackDamage;
        int mainStat = _playerCharacter.GetMainStat();

        float damage = ((attackDamage * skill.SkillMultiplier) + skill.SkillBaseDamage)
                     * (1f + (mainStat * skill.MainStatDamageIncrease));

        Log($"스킬 데미지: {damage:F1} = (({attackDamage:F1} × {skill.SkillMultiplier}) + {skill.SkillBaseDamage}) × (1 + ({mainStat} × {skill.MainStatDamageIncrease}))");

        return damage;
    }

    #endregion

    #region VFX 처리 (다중 VFX 지원)

    /// <summary>
    /// 모든 스킬 VFX 생성 (다중 VFX 지원)
    /// 
    /// 개선사항:
    /// - 여러 VfxConfig를 순회하며 각각 코루틴 시작
    /// - GetVfxConfigs()가 레거시 VFX 자동 변환 처리
    /// - 각 VFX는 독립적인 타이밍에 생성됨
    /// </summary>
    private void SpawnAllSkillVFX(SkillData skill)
    {
        // VFX 설정 배열 가져오기 (레거시 자동 변환 포함)
        VfxConfig[] vfxConfigs = skill.GetVfxConfigs();

        if (vfxConfigs == null || vfxConfigs.Length == 0)
        {
            Log($"[{skill.SkillName}] VFX 없음");
            return;
        }

        Log($"[{skill.SkillName}] {vfxConfigs.Length}개 VFX 준비");

        // 각 VfxConfig마다 독립적인 코루틴 시작
        foreach (VfxConfig config in vfxConfigs)
        {
            if (config.IsValid())
            {
                float delay = skill.AnimationDuration * config.SpawnTiming;
                Coroutine vfxCoroutine = StartCoroutine(SpawnSingleVFX(config, delay, skill.SkillName));
                _activeVfxCoroutines.Add(vfxCoroutine);
            }
            else
            {
                Debug.LogWarning($"[{skill.SkillName}] 유효하지 않은 VfxConfig 발견 (프리팹 없음)");
            }
        }
    }

    /// <summary>
    /// 단일 VFX 생성 코루틴
    /// 
    /// 매개변수:
    /// - config: VFX 설정 (프리팹, 위치, 회전 등)
    /// - delay: 생성 지연 시간 (초)
    /// - skillName: 디버그용 스킬 이름
    /// </summary>
    private IEnumerator SpawnSingleVFX(VfxConfig config, float delay, string skillName)
    {
        // VFX 생성 시점까지 대기
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        // VFX 위치 계산 (로컬 좌표 -> 월드 좌표)
        Vector3 spawnPosition = transform.position + transform.TransformDirection(config.PositionOffset);

        // VFX 회전 계산 (캐릭터 회전 + 오프셋)
        Quaternion spawnRotation = transform.rotation * Quaternion.Euler(config.RotationOffset);

        // VFX 인스턴스 생성
        GameObject vfxInstance = Instantiate(
            config.VfxPrefab,
            spawnPosition,
            spawnRotation
        );

        // 캐릭터에 부착 옵션
        if (config.AttachToCharacter)
        {
            vfxInstance.transform.SetParent(transform);
            Log($"[{skillName}] VFX 캐릭터에 부착: {vfxInstance.name}");
        }

        Log($"[{skillName}] VFX 생성: {vfxInstance.name} (타이밍: {delay:F2}초)");

        // VFX 자동 제거 처리
        float lifetime = config.Lifetime;
        if (lifetime <= 0)
        {
            // Lifetime이 0이면 파티클 시스템의 Duration 사용
            ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            }
            else
            {
                lifetime = 2f; // 기본값
            }
        }

        Destroy(vfxInstance, lifetime);
    }

    /// <summary>
    /// 모든 활성 VFX 코루틴 정리
    /// </summary>
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
        Log("모든 VFX 코루틴 정리 완료");
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
                Gizmos.DrawWireSphere(centerPosition, _currentSkill.SphereRadius);
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
