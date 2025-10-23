using UnityEngine;
using DG.Tweening;

/// <summary>
/// 스킬 데이터 ScriptableObject
/// 
/// 대시 거리 모드:
/// - Fixed: 고정 거리 (_dashDistance 사용)
/// - MouseDistance: 마우스까지 거리 기반 (_minDashDistance ~ _maxDashDistance)
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "Character/Skill Data")]
public class SkillData : ScriptableObject
{
    #region 스킬 기본 정보

    [Header("스킬 정보")]
    [SerializeField] private string _skillName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _skillIcon;
    [SerializeField] private int _requiredLevel;

    #endregion

    #region 스킬 속성

    [Header("스킬 속성")]
    [SerializeField] private float _manaCost;
    [SerializeField] private float _cooldown;
    [SerializeField] private float _damage;
    [SerializeField] private float _range;
    [SerializeField] private SkillType _skillType;

    [Header("원거리 스킬 설정 (Ranged 타입 전용)")]
    [Tooltip("투사체 프리팹 (Projectile 스크립트 포함 필수)")]
    [SerializeField] private GameObject _projectilePrefab;

    [Header("버프 스킬 설정 (Buff 타입 전용)")]
    [Tooltip("버프 증가량 (주요 스탯에 추가)")]
    [SerializeField] private float _buffAmount;

    [Tooltip("버프 지속시간 (초)")]
    [SerializeField] private float _buffDuration = 10f;

    [Header("AOE 스킬 설정 (AreaOfEffect 타입 전용)")]
    [Tooltip("AOE 형태 (Sphere 또는 Box)")]
    [SerializeField] private AOEShape _aoeShape = AOEShape.Sphere;

    [Tooltip("데미지 적용 타이밍 (0.0 ~ 1.0, 애니메이션 진행률)")]
    [Range(0f, 1f)]
    [SerializeField] private float _damagePointTiming = 0.7f;

    [Tooltip("AOE 생성 위치 오프셋 (플레이어 로컬 좌표)")]
    [SerializeField] private Vector3 _aoeOffset = Vector3.zero;

    [Tooltip("Box 형태일 때만 사용 - 박스 크기")]
    [SerializeField] private Vector3 _boxSize = new Vector3(3f, 2f, 4f);

    [Header("애니메이션 설정")]
    [Tooltip("애니메이션 전체 재생 시간 (초)")]
    [SerializeField] private float _animationDuration = 1.0f;

    [Tooltip("Root Motion 사용 여부 (false: DOTween 대시)")]
    [SerializeField] private bool _useRootMotion = true;

    [Header("DOTween 대시 설정 (UseRootMotion=false일 때만)")]
    [Tooltip("대시 거리 결정 방식")]
    [SerializeField] private DashDistanceMode _dashDistanceMode = DashDistanceMode.Fixed;

    [Header("→ Fixed 모드 설정")]
    [Tooltip("고정 대시 거리 (미터)")]
    [SerializeField] private float _dashDistance = 5f;

    [Header("→ MouseDistance 모드 설정")]
    [Tooltip("최소 대시 거리 (미터)")]
    [SerializeField] private float _minDashDistance = 2f;

    [Tooltip("최대 대시 거리 (미터)")]
    [SerializeField] private float _maxDashDistance = 8f;

    [Header("→ 공통 설정")]
    [Tooltip("대시 지속 시간 (초)")]
    [SerializeField] private float _dashDuration = 0.3f;

    [Tooltip("대시 시작 타이밍 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float _dashTiming = 0.1f;

    [Tooltip("대시 이징 (DOTween Ease 타입)")]
    [SerializeField] private Ease _dashEase = Ease.OutQuad;

    [Header("벽 충돌 감지")]
    [Tooltip("대시 전 벽 충돌 체크")]
    [SerializeField] private bool _checkWallCollision = true;

    [Tooltip("벽으로 인식할 레이어")]
    [SerializeField] private LayerMask _wallLayer = 1 << 6;

    [Tooltip("벽 앞 안전 거리 (미터)")]
    [SerializeField] private float _wallStopBuffer = 0.5f;

    #endregion

    #region Public 프로퍼티

    public string SkillName => _skillName;
    public string Description => _description;
    public Sprite SkillIcon => _skillIcon;
    public int RequiredLevel => _requiredLevel;
    public float ManaCost => _manaCost;
    public float Cooldown => _cooldown;
    public float Damage => _damage;
    public float Range => _range;
    public SkillType SkillType => _skillType;
    public GameObject ProjectilePrefab => _projectilePrefab;
    public float BuffAmount => _buffAmount;
    public float BuffDuration => _buffDuration;

    public AOEShape AoeShape => _aoeShape;
    public float DamagePointTiming => _damagePointTiming;
    public Vector3 AoeOffset => _aoeOffset;
    public Vector3 BoxSize => _boxSize;
    public float AnimationDuration => _animationDuration;
    public bool UseRootMotion => _useRootMotion;

    // DOTween 대시 프로퍼티
    public DashDistanceMode DashDistanceMode => _dashDistanceMode;
    public float DashDistance => _dashDistance;
    public float MinDashDistance => _minDashDistance;
    public float MaxDashDistance => _maxDashDistance;
    public float DashDuration => _dashDuration;
    public float DashTiming => _dashTiming;
    public Ease DashEase => _dashEase;

    // 벽 충돌 감지 프로퍼티
    public bool CheckWallCollision => _checkWallCollision;
    public LayerMask WallLayer => _wallLayer;
    public float WallStopBuffer => _wallStopBuffer;

    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 기본 검증
        _manaCost = Mathf.Max(0f, _manaCost);
        _cooldown = Mathf.Max(0f, _cooldown);
        _damage = Mathf.Max(0f, _damage);
        _range = Mathf.Max(0f, _range);
        _buffAmount = Mathf.Max(0f, _buffAmount);
        _buffDuration = Mathf.Max(0f, _buffDuration);
        _animationDuration = Mathf.Max(0.1f, _animationDuration);

        // DOTween 대시 검증
        _dashDistance = Mathf.Max(0f, _dashDistance);
        _minDashDistance = Mathf.Max(0f, _minDashDistance);
        _maxDashDistance = Mathf.Max(_minDashDistance, _maxDashDistance);
        _dashDuration = Mathf.Max(0.01f, _dashDuration);
        _dashTiming = Mathf.Clamp01(_dashTiming);
        _wallStopBuffer = Mathf.Max(0.1f, _wallStopBuffer);
        _damagePointTiming = Mathf.Clamp01(_damagePointTiming);

        // 타입별 경고
        if (_skillType == SkillType.Ranged && _projectilePrefab == null)
        {
            Debug.LogWarning($"[{_skillName}] 원거리 스킬은 Projectile 프리팹이 필요합니다!", this);
        }

        if (_skillType == SkillType.Buff)
        {
            if (_buffAmount <= 0)
                Debug.LogWarning($"[{_skillName}] BuffAmount > 0이어야 합니다!", this);
            if (_buffDuration <= 0)
                Debug.LogWarning($"[{_skillName}] BuffDuration > 0이어야 합니다!", this);
        }

        if (_skillType == SkillType.AreaOfEffect)
        {
            if (_range <= 0)
                Debug.LogWarning($"[{_skillName}] Range > 0이어야 합니다!", this);

            if (_aoeShape == AOEShape.Box)
            {
                _boxSize.x = Mathf.Max(0.1f, _boxSize.x);
                _boxSize.y = Mathf.Max(0.1f, _boxSize.y);
                _boxSize.z = Mathf.Max(0.1f, _boxSize.z);
            }
        }

        if (_projectilePrefab != null && _skillType == SkillType.Ranged)
        {
            Projectile projectile = _projectilePrefab.GetComponent<Projectile>();
            if (projectile == null)
            {
                Debug.LogError($"[{_skillName}] 투사체 프리팹에 Projectile 스크립트가 없습니다!", this);
            }
        }

        if (!_useRootMotion && _dashDistance <= 0 && _dashDistanceMode == DashDistanceMode.Fixed)
        {
            Debug.LogWarning($"[{_skillName}] Fixed 모드에서 DashDistance > 0이어야 합니다!", this);
        }

        if (!_useRootMotion && _minDashDistance >= _maxDashDistance)
        {
            Debug.LogWarning($"[{_skillName}] MinDashDistance < MaxDashDistance여야 합니다!", this);
        }

        if (!_useRootMotion && _checkWallCollision && _wallLayer.value == 0)
        {
            Debug.LogWarning($"[{_skillName}] Wall Layer가 설정되지 않았습니다!", this);
        }
    }
#endif
}

/// <summary>
/// 대시 거리 결정 모드
/// </summary>
public enum DashDistanceMode
{
    /// <summary>
    /// 고정 거리 (DashDistance 사용)
    /// </summary>
    Fixed,

    /// <summary>
    /// 마우스까지 거리 기반 (MinDashDistance ~ MaxDashDistance)
    /// </summary>
    MouseDistance
}

public enum SkillType
{
    Melee,
    Ranged,
    AreaOfEffect,
    Buff,
    Heal
}

public enum AOEShape
{
    Sphere,
    Box
}
