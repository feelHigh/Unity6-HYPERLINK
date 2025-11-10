using UnityEngine;

/// <summary>
/// 스킬 투사체 시스템
/// 
/// 목적:
/// - 원거리 스킬의 투사체 이동 및 충돌 처리
/// - 데미지 적용 및 시각 효과
/// - 범위 제한 및 자동 파괴
/// 
/// 변경사항:
/// - AttackInfo를 사용한 데미지 전달
/// - 히트 VFX 지원
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
    private GameObject _hitVfx;
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
    /// </summary>
    /// <param name="damage">데미지 양</param>
    /// <param name="maxRange">최대 비행 거리</param>
    /// <param name="owner">발사한 캐릭터</param>
    /// <param name="hitVfx">적 히트 VFX 프리팹 (선택)</param>
    public void Initialize(float damage, float maxRange, PlayerCharacter owner, GameObject hitVfx = null)
    {
        _damage = damage;
        _maxRange = maxRange;
        _owner = owner;
        _hitVfx = hitVfx;
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
    /// </summary>
    private void MoveForward()
    {
        Vector3 movement = transform.forward * _speed * Time.deltaTime;
        _rigidbody.MovePosition(_rigidbody.position + movement);
    }

    /// <summary>
    /// 최대 사거리 체크
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
    /// 적 충돌 처리 - AttackInfo 사용
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!_isInitialized)
            return;

        // Enemy인지 확인
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            return;

        // AttackInfo를 사용한 데미지 적용
        AttackInfo attackInfo = AttackInfo.CreatePlayerSkill(
            _damage,
            other.ClosestPoint(transform.position),
            _hitVfx
        );
        enemy.TakeDamage(attackInfo);
        Debug.Log($"투사체 명중: {enemy.name}에게 {_damage} 데미지");

        // 히트 이펙트 (기본 이펙트)
        SpawnHitEffect(other.ClosestPoint(transform.position));

        // 관통 처리
        HandlePiercing();
    }

    /// <summary>
    /// 관통 처리
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
