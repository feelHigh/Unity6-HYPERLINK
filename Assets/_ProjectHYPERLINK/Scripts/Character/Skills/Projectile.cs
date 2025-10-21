using UnityEngine;

/// <summary>
/// 스킬 투사체 시스템 (신규 파일)
/// 
/// 목적:
/// - 원거리 스킬의 투사체 이동 및 충돌 처리
/// - 데미지 적용 및 시각 효과
/// - 범위 제한 및 자동 파괴
/// 
/// 사용 흐름:
/// 1. SkillActivationSystem이 투사체 프리팹 인스턴스화
/// 2. Initialize()로 데미지, 범위, 발사자 설정
/// 3. Update()에서 전진 이동
/// 4. OnTriggerEnter()에서 적 충돌 감지
/// 5. 데미지 적용 후 파괴
/// 6. 최대 범위 도달 시 자동 파괴
/// 
/// 프리팹 설정:
/// - GameObject + Sphere Mesh
/// - Rigidbody (Is Kinematic = true)
/// - Sphere Collider (Is Trigger = true)
/// - Projectile 스크립트
/// - Trail Renderer (선택)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [Header("투사체 설정")]
    [SerializeField] private float _speed = 15f;
    [Tooltip("적 충돌 시 파괴되는지 여부")]
    [SerializeField] private bool _destroyOnHit = true;
    [Tooltip("관통 가능 적 수 (0 = 첫 충돌 시 파괴)")]
    [SerializeField] private int _maxPierceCount = 0;

    [Header("시각 효과")]
    [SerializeField] private GameObject _hitEffectPrefab;
    [SerializeField] private TrailRenderer _trailRenderer;

    // 런타임 데이터
    private float _damage;
    private float _maxRange;
    private PlayerCharacter _owner;
    private Vector3 _startPosition;
    private int _currentPierceCount = 0;
    private bool _isInitialized = false;

    // 컴포넌트 캐싱
    private Rigidbody _rigidbody;
    private Collider _collider;

    #region 초기화

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        // Rigidbody 설정 확인
        if (!_rigidbody.isKinematic)
        {
            Debug.LogWarning("[Projectile] Rigidbody는 Kinematic이어야 합니다!");
            _rigidbody.isKinematic = true;
        }

        // Collider 설정 확인
        if (!_collider.isTrigger)
        {
            Debug.LogWarning("[Projectile] Collider는 Trigger여야 합니다!");
            _collider.isTrigger = true;
        }
    }

    /// <summary>
    /// 투사체 초기화
    /// 
    /// SkillActivationSystem.ExecuteRangedSkill()에서 호출
    /// 
    /// Parameters:
    ///     damage: 투사체 데미지 (스킬 계산 완료된 값)
    ///     maxRange: 최대 비행 거리 (스킬의 Range)
    ///     owner: 발사한 캐릭터 (자가 피해 방지용)
    ///     
    /// 사용 예:
    /// GameObject proj = Instantiate(projectilePrefab, spawnPos, rotation);
    /// Projectile script = proj.GetComponent<Projectile>();
    /// script.Initialize(150f, 20f, playerCharacter);
    /// </summary>
    public void Initialize(float damage, float maxRange, PlayerCharacter owner)
    {
        _damage = damage;
        _maxRange = maxRange;
        _owner = owner;
        _startPosition = transform.position;
        _isInitialized = true;

        Debug.Log($"투사체 발사: 데미지 {_damage}, 사거리 {_maxRange}m");
    }

    #endregion

    #region 이동 및 수명

    private void Update()
    {
        if (!_isInitialized)
            return;

        // 전진 이동
        MoveForward();

        // 범위 체크
        CheckRange();
    }

    /// <summary>
    /// 투사체 전진 이동
    /// 
    /// Rigidbody.MovePosition 사용:
    /// - Transform.Translate보다 안정적
    /// - 물리 엔진과 호환
    /// - 프레임 독립적
    /// </summary>
    private void MoveForward()
    {
        Vector3 movement = transform.forward * _speed * Time.deltaTime;
        _rigidbody.MovePosition(_rigidbody.position + movement);
    }

    /// <summary>
    /// 최대 사거리 체크
    /// 
    /// 사거리 초과 시:
    /// - 투사체 파괴
    /// - 이펙트 없음 (공중에서 소멸)
    /// </summary>
    private void CheckRange()
    {
        float distanceTraveled = Vector3.Distance(_startPosition, transform.position);

        if (distanceTraveled >= _maxRange)
        {
            Debug.Log($"투사체 사거리 초과: {distanceTraveled:F1}m / {_maxRange}m");
            DestroyProjectile(showEffect: false);
        }
    }

    #endregion

    #region 충돌 처리

    /// <summary>
    /// 적 충돌 처리
    /// 
    /// 트리거 조건:
    /// - Is Trigger = true
    /// - 상대 Collider도 활성화
    /// - 둘 중 하나는 Rigidbody 필요
    /// 
    /// 처리 순서:
    /// 1. Enemy 컴포넌트 확인
    /// 2. 자가 피해 방지 (owner 체크)
    /// 3. 데미지 적용
    /// 4. 히트 이펙트 생성
    /// 5. 관통 처리 또는 파괴
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!_isInitialized)
            return;

        // Enemy인지 확인
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            return;

        // 자가 피해 방지 (필요시)
        // if (other.GetComponent<PlayerCharacter>() == _owner)
        //     return;

        // 데미지 적용
        enemy.TakeDamage(_damage);
        Debug.Log($"투사체 명중: {enemy.name}에게 {_damage} 데미지");

        // 히트 이펙트
        SpawnHitEffect(other.ClosestPoint(transform.position));

        // 관통 처리
        HandlePiercing();
    }

    /// <summary>
    /// 관통 처리
    /// 
    /// 관통 로직:
    /// - _maxPierceCount = 0: 첫 충돌 시 파괴
    /// - _maxPierceCount = 1: 1회 관통 후 파괴
    /// - _maxPierceCount = 2: 2회 관통 후 파괴
    /// 
    /// 사용 예:
    /// - 일반 화살: maxPierceCount = 0
    /// - 관통 화살: maxPierceCount = 2
    /// - 레이저: maxPierceCount = 999 (무한 관통)
    /// </summary>
    private void HandlePiercing()
    {
        _currentPierceCount++;

        if (_destroyOnHit && _currentPierceCount > _maxPierceCount)
        {
            Debug.Log($"투사체 관통 한계: {_currentPierceCount} / {_maxPierceCount}");
            DestroyProjectile(showEffect: true);
        }
    }

    #endregion

    #region 시각 효과

    /// <summary>
    /// 히트 이펙트 생성
    /// 
    /// Parameters:
    ///     position: 이펙트 생성 위치 (충돌 지점)
    ///     
    /// 이펙트 설정:
    /// - 파티클 시스템
    /// - Auto-destroy (재생 완료 후 자동 파괴)
    /// - 3초 후 강제 파괴 (안전장치)
    /// </summary>
    private void SpawnHitEffect(Vector3 position)
    {
        if (_hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(_hitEffectPrefab, position, Quaternion.identity);

            // 파티클 시스템 자동 파괴
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // 파티클 없으면 3초 후 파괴
                Destroy(effect, 3f);
            }
        }
    }

    /// <summary>
    /// 투사체 파괴
    /// 
    /// Parameters:
    ///     showEffect: 파괴 이펙트 표시 여부
    ///     
    /// 처리 과정:
    /// 1. Trail Renderer 정리 (있으면)
    /// 2. 파괴 이펙트 (선택)
    /// 3. GameObject 파괴
    /// </summary>
    private void DestroyProjectile(bool showEffect)
    {
        // Trail Renderer가 있으면 분리하여 자연스럽게 소멸
        if (_trailRenderer != null)
        {
            _trailRenderer.transform.SetParent(null);
            Destroy(_trailRenderer.gameObject, _trailRenderer.time);
        }

        // 파괴 이펙트
        if (showEffect)
        {
            SpawnHitEffect(transform.position);
        }

        // 투사체 파괴
        Destroy(gameObject);
    }

    #endregion

    #region 디버그 & 유틸리티

    /// <summary>
    /// Gizmo 시각화
    /// 
    /// Scene 뷰 표시:
    /// - 초록색 선: 투사체 이동 방향
    /// - 노란색 구: 최대 사거리 도달 지점
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !_isInitialized)
            return;

        // 이동 방향 표시
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        // 최대 사거리 표시
        Vector3 maxRangePos = _startPosition + transform.forward * _maxRange;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(maxRangePos, 0.5f);

        // 이동 경로 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_startPosition, transform.position);
    }

    /// <summary>
    /// 투사체 정보 출력 (디버그용)
    /// </summary>
    [ContextMenu("Debug: Print Projectile Info")]
    private void DebugPrintInfo()
    {
        Debug.Log("===== 투사체 정보 =====");
        Debug.Log($"초기화 상태: {_isInitialized}");
        Debug.Log($"데미지: {_damage}");
        Debug.Log($"속도: {_speed} m/s");
        Debug.Log($"최대 사거리: {_maxRange}m");

        if (_isInitialized)
        {
            float distanceTraveled = Vector3.Distance(_startPosition, transform.position);
            Debug.Log($"비행 거리: {distanceTraveled:F1}m / {_maxRange}m");
            Debug.Log($"관통 횟수: {_currentPierceCount} / {_maxPierceCount}");
        }
    }

    #endregion

    #region Unity Editor 전용

#if UNITY_EDITOR
    /// <summary>
    /// Inspector 값 변경 시 검증
    /// </summary>
    private void OnValidate()
    {
        _speed = Mathf.Max(1f, _speed);
        _maxPierceCount = Mathf.Max(0, _maxPierceCount);
    }
#endif

    #endregion
}
