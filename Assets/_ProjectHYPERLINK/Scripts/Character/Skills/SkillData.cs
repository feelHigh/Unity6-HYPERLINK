using UnityEngine;
using DG.Tweening;

/// <summary>
/// 스킬 데이터 ScriptableObject
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

    #region 애니메이션 설정

    [Header("애니메이션 트리거")]
    [Tooltip("Animator Controller의 트리거 파라미터 이름")]
    [SerializeField] private string _animatorTriggerName;

    #endregion

    #region 스킬 속성

    [Header("스킬 속성")]
    [SerializeField] private float _manaCost;
    [SerializeField] private float _cooldown;
    [SerializeField] private float _range;
    [SerializeField] private SkillType _skillType;

    [Header("데미지 계산 파라미터")]
    [Tooltip("스킬 배율 - 캐릭터 공격력에 곱해지는 값")]
    [SerializeField] private float _skillMultiplier = 1f;

    [Tooltip("스킬 기본 데미지 - 계산 후 더해지는 고정 데미지")]
    [SerializeField] private float _skillBaseDamage = 0f;

    [Tooltip("주요 스탯당 데미지 증가%")]
    [SerializeField] private float _mainStatDamageIncrease = 0.01f;

    [Header("원거리 스킬 설정 (Ranged 타입 전용)")]
    [SerializeField] private GameObject _projectilePrefab;

    [Header("버프 스킬 설정 (Buff 타입 전용)")]
    [SerializeField] private float _buffAmount;
    [SerializeField] private float _buffDuration = 10f;

    #endregion

    #region Hit Area 설정 (다중 hit area 지원)

    [Header("Hit Area 설정 (다중 히트 영역 지원)")]
    [Tooltip("여러 hit area를 독립적으로 설정 가능 - 각 영역은 다른 타이밍, 위치, 크기로 적용됨")]
    [SerializeField] private HitAreaConfig[] _hitAreaConfigs = new HitAreaConfig[0];

    #endregion

    #region VFX 설정 (다중 VFX 지원)

    [Header("VFX 설정 (다중 VFX 지원)")]
    [Tooltip("여러 VFX를 독립적으로 설정 가능")]
    [SerializeField] private VfxConfig[] _vfxConfigs = new VfxConfig[0];

    [Header("히트 VFX")]
    [Tooltip("스킬이 적에게 맞았을 때 표시할 VFX")]
    [SerializeField] private HitVfxConfig _enemyHitVfxConfig;

    #endregion

    #region 애니메이션 설정

    [Header("애니메이션 설정")]
    [SerializeField] private float _animationDuration = 1.0f;
    [SerializeField] private bool _useRootMotion = true;

    [Header("DOTween 대시 설정 (UseRootMotion=false일 때만)")]
    [SerializeField] private DashDistanceMode _dashDistanceMode = DashDistanceMode.Fixed;
    [SerializeField] private float _dashDistance = 5f;
    [SerializeField] private float _minDashDistance = 2f;
    [SerializeField] private float _maxDashDistance = 8f;
    [SerializeField] private float _dashDuration = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float _dashTiming = 0.1f;
    [SerializeField] private Ease _dashEase = Ease.OutQuad;

    [Header("벽 충돌 감지")]
    [SerializeField] private bool _checkWallCollision = true;
    [SerializeField] private LayerMask _wallLayer = 1 << 6;
    [SerializeField] private float _wallStopBuffer = 0.5f;

    #endregion

    #region 사운드 설정

    [Header("스킬 사운드")]
    [Tooltip("스킬 캐스팅 시 재생할 사운드")]
    [SerializeField] private AudioClip _skillCastSound;

    [Tooltip("스킬이 적에게 히트할 때 재생할 사운드")]
    [SerializeField] private AudioClip _skillHitSound;

    #endregion

    #region Public 프로퍼티

    public string SkillName => _skillName;
    public string Description => _description;
    public Sprite SkillIcon => _skillIcon;
    public int RequiredLevel => _requiredLevel;
    public string AnimatorTriggerName => _animatorTriggerName;
    public float ManaCost => _manaCost;
    public float Cooldown => _cooldown;
    public float Range => _range;
    public SkillType SkillType => _skillType;
    public float SkillMultiplier => _skillMultiplier;
    public float SkillBaseDamage => _skillBaseDamage;
    public float MainStatDamageIncrease => _mainStatDamageIncrease;
    public GameObject ProjectilePrefab => _projectilePrefab;
    public float BuffAmount => _buffAmount;
    public float BuffDuration => _buffDuration;
    public float AnimationDuration => _animationDuration;
    public bool UseRootMotion => _useRootMotion;
    public DashDistanceMode DashDistanceMode => _dashDistanceMode;
    public float DashDistance => _dashDistance;
    public float MinDashDistance => _minDashDistance;
    public float MaxDashDistance => _maxDashDistance;
    public float DashDuration => _dashDuration;
    public float DashTiming => _dashTiming;
    public Ease DashEase => _dashEase;
    public bool CheckWallCollision => _checkWallCollision;
    public LayerMask WallLayer => _wallLayer;
    public float WallStopBuffer => _wallStopBuffer;
    public HitVfxConfig EnemyHitVfxConfig => _enemyHitVfxConfig;
    public AudioClip SkillCastSound => _skillCastSound;
    public AudioClip SkillHitSound => _skillHitSound;

    /// <summary>
    /// Hit Area 설정 목록 가져오기
    /// </summary>
    public HitAreaConfig[] GetHitAreaConfigs()
    {
        return _hitAreaConfigs;
    }

    /// <summary>
    /// Hit Area가 설정되어 있는지 확인
    /// </summary>
    public bool HasHitAreas()
    {
        return _hitAreaConfigs != null && _hitAreaConfigs.Length > 0;
    }

    /// <summary>
    /// VFX 설정 목록 가져오기
    /// </summary>
    public VfxConfig[] GetVfxConfigs()
    {
        return _vfxConfigs;
    }

    /// <summary>
    /// VFX가 설정되어 있는지 확인
    /// </summary>
    public bool HasVfx()
    {
        return _vfxConfigs != null && _vfxConfigs.Length > 0;
    }

    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_animatorTriggerName))
        {
            DebugHelper.LogWarning($"[{_skillName}] AnimatorTriggerName이 설정되지 않았습니다!");
        }

        _manaCost = Mathf.Max(0f, _manaCost);
        _cooldown = Mathf.Max(0f, _cooldown);
        _range = Mathf.Max(0f, _range);
        _buffAmount = Mathf.Max(0f, _buffAmount);
        _buffDuration = Mathf.Max(0f, _buffDuration);
        _animationDuration = Mathf.Max(0.1f, _animationDuration);
        _skillMultiplier = Mathf.Max(0f, _skillMultiplier);
        _skillBaseDamage = Mathf.Max(0f, _skillBaseDamage);
        _mainStatDamageIncrease = Mathf.Max(0f, _mainStatDamageIncrease);
        _dashDistance = Mathf.Max(0f, _dashDistance);
        _minDashDistance = Mathf.Max(0f, _minDashDistance);
        _maxDashDistance = Mathf.Max(_minDashDistance, _maxDashDistance);
        _dashDuration = Mathf.Max(0.01f, _dashDuration);
        _dashTiming = Mathf.Clamp01(_dashTiming);
        _wallStopBuffer = Mathf.Max(0.1f, _wallStopBuffer);

        if (_skillType == SkillType.Ranged && _projectilePrefab == null)
        {
            DebugHelper.LogWarning($"[{_skillName}] 원거리 스킬은 Projectile 프리팹이 필요합니다!");
        }

        if (_skillType == SkillType.Buff)
        {
            if (_buffAmount <= 0)
                DebugHelper.LogWarning($"[{_skillName}] BuffAmount > 0이어야 합니다!");
            if (_buffDuration <= 0)
                DebugHelper.LogWarning($"[{_skillName}] BuffDuration > 0이어야 합니다!");
        }

        if (_projectilePrefab != null && _skillType == SkillType.Ranged)
        {
            Projectile projectile = _projectilePrefab.GetComponent<Projectile>();
            if (projectile == null)
            {
                DebugHelper.LogError($"[{_skillName}] 투사체 프리팹에 Projectile 스크립트가 없습니다!");
            }
        }
    }
#endif
}

public enum DashDistanceMode
{
    Fixed,
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
