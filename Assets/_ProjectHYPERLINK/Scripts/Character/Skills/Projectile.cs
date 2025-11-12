using UnityEngine;

/// <summary>
/// 스킬 투사체 시스템
/// 
/// 변경사항:
/// - HitVfxConfig 사용 (GameObject에서 변경)
/// - Offset, Rotation, Size 지원
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
    private HitVfxConfig _hitVfxConfig;
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

        if (!_rigidbody.isKinematic)
        {
            Debug.LogWarning("[Projectile] Rigidbody는 Kinematic이어야 합니다!");
            _rigidbody.isKinematic = true;
        }

        if (!_collider.isTrigger)
        {
            Debug.LogWarning("[Projectile] Collider는 Trigger여야 합니다!");
            _collider.isTrigger = true;
        }
    }

    /// <summary>
    /// 투사체 초기화
    /// </summary>
    public void Initialize(float damage, float maxRange, PlayerCharacter owner, HitVfxConfig hitVfxConfig = null)
    {
        _damage = damage;
        _maxRange = maxRange;
        _owner = owner;
        _hitVfxConfig = hitVfxConfig;
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

        MoveForward();
        CheckRange();
    }

    private void MoveForward()
    {
        Vector3 movement = transform.forward * _speed * Time.deltaTime;
        _rigidbody.MovePosition(_rigidbody.position + movement);
    }

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

    private void OnTriggerEnter(Collider other)
    {
        if (!_isInitialized)
            return;

        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy == null)
            return;

        // AttackInfo 생성 (HitVfxConfig 사용)
        AttackInfo attackInfo = AttackInfo.CreatePlayerSkill(
            _damage,
            other.ClosestPoint(transform.position),
            _hitVfxConfig
        );
        enemy.TakeDamage(attackInfo);
        Debug.Log($"투사체 명중: {enemy.name}에게 {_damage} 데미지");

        SpawnHitEffect(other.ClosestPoint(transform.position));
        HandlePiercing();
    }

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

    private void SpawnHitEffect(Vector3 position)
    {
        if (_hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(_hitEffectPrefab, position, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 3f);
            }
        }
    }

    private void DestroyProjectile(bool showEffect)
    {
        if (_trailRenderer != null)
        {
            _trailRenderer.transform.SetParent(null);
            Destroy(_trailRenderer.gameObject, _trailRenderer.time);
        }

        if (showEffect)
        {
            SpawnHitEffect(transform.position);
        }

        Destroy(gameObject);
    }

    #endregion

    #region 디버그

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !_isInitialized)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        Vector3 maxRangePos = _startPosition + transform.forward * _maxRange;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(maxRangePos, 0.5f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_startPosition, transform.position);
    }

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        _speed = Mathf.Max(1f, _speed);
        _maxPierceCount = Mathf.Max(0, _maxPierceCount);
    }
#endif

    #endregion
}
