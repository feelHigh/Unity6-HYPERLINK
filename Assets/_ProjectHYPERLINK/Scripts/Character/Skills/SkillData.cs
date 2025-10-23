using UnityEngine;

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

    [Tooltip("데미지 적용 타이밍 (0.0 ~ 1.0, 애니메이션 진행률)\n예: 0.5 = 애니메이션 50% 지점, 0.8 = 80% 지점")]
    [Range(0f, 1f)]
    [SerializeField] private float _damagePointTiming = 0.7f;

    [Tooltip("AOE 생성 위치 오프셋 (플레이어 로컬 좌표)\n예: (0, 0, 2) = 전방 2유닛")]
    [SerializeField] private Vector3 _aoeOffset = Vector3.zero;

    [Tooltip("Box 형태일 때만 사용 - 박스 크기\n예: (3, 2, 4) = 가로3 x 높이2 x 깊이4")]
    [SerializeField] private Vector3 _boxSize = new Vector3(3f, 2f, 4f);

    [Header("애니메이션 설정")]
    [Tooltip("애니메이션 전체 재생 시간 (초)\n애니메이션 클립 길이와 일치해야 정확함")]
    [SerializeField] private float _animationDuration = 1.0f;

    [Tooltip("Root Motion 사용 여부")]
    [SerializeField] private bool _useRootMotion = true;

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

    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 기존 검증
        _manaCost = Mathf.Max(0f, _manaCost);
        _cooldown = Mathf.Max(0f, _cooldown);
        _damage = Mathf.Max(0f, _damage);
        _range = Mathf.Max(0f, _range);
        _buffAmount = Mathf.Max(0f, _buffAmount);
        _buffDuration = Mathf.Max(0f, _buffDuration);
        _animationDuration = Mathf.Max(0.1f, _animationDuration);

        // 타이밍은 0~1 사이로 자동 제한됨 (Range 속성)
        _damagePointTiming = Mathf.Clamp01(_damagePointTiming);

        // 타입별 경고
        if (_skillType == SkillType.Ranged && _projectilePrefab == null)
        {
            Debug.LogWarning($"[{_skillName}] 원거리 스킬은 Projectile 프리팹이 필요합니다!", this);
        }

        if (_skillType == SkillType.Buff)
        {
            if (_buffAmount <= 0)
                Debug.LogWarning($"[{_skillName}] 버프 스킬은 BuffAmount > 0이어야 합니다!", this);
            if (_buffDuration <= 0)
                Debug.LogWarning($"[{_skillName}] 버프 스킬은 BuffDuration > 0이어야 합니다!", this);
        }

        if (_skillType == SkillType.AreaOfEffect)
        {
            if (_range <= 0)
                Debug.LogWarning($"[{_skillName}] AOE 스킬은 Range > 0이어야 합니다!", this);

            // Box 형태일 때 크기 검증
            if (_aoeShape == AOEShape.Box)
            {
                _boxSize.x = Mathf.Max(0.1f, _boxSize.x);
                _boxSize.y = Mathf.Max(0.1f, _boxSize.y);
                _boxSize.z = Mathf.Max(0.1f, _boxSize.z);
            }
        }

        // 투사체 프리팹 검증
        if (_projectilePrefab != null && _skillType == SkillType.Ranged)
        {
            Projectile projectile = _projectilePrefab.GetComponent<Projectile>();
            if (projectile == null)
            {
                Debug.LogError($"[{_skillName}] 투사체 프리팹에 Projectile 스크립트가 없습니다!", this);
            }
        }
    }
#endif
}

/// <summary>
/// 스킬 타입 열거형
/// </summary>
public enum SkillType
{
    Melee,           // 근거리 공격
    Ranged,          // 원거리 공격
    AreaOfEffect,    // 광역 공격
    Buff,            // 버프
    Heal             // 회복
}

/// <summary>
/// AOE 형태 열거형
/// 
/// Sphere:
/// - 원형 범위 공격
/// - Physics.OverlapSphere 사용
/// - 모든 방향으로 균등한 범위
/// - 사용 예: Judgement (심판)
/// 
/// Box:
/// - 직사각형 범위 공격
/// - Physics.OverlapBox 사용
/// - 전방 집중 공격에 유리
/// - 사용 예: SwiftSlash (빠른 베기)
/// </summary>
public enum AOEShape
{
    Sphere,  // 구형 범위
    Box      // 박스형 범위
}
