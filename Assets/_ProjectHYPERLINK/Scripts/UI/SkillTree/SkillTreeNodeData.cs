using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스킬 트리 노드 데이터
/// 
/// 역할:
/// - 스킬 트리의 각 노드 정보 저장
/// - 노드 타입 (액티브/패시브), 티어, 연결 구조 정의
/// - Inspector에서 스킬 트리 구조 설정 가능
/// 
/// 사용 방법:
/// 1. Assets/Create/Character/Skill Tree Node Data로 생성
/// 2. Tier 1 노드: RequiredNodeData를 비워둠 (기본 언락 가능)
/// 3. Tier 2+ 노드: RequiredNodeData에 부모 노드 할당
/// 4. 패시브 노드: IsPassive = true, PassiveStatBonuses 설정
/// </summary>
[CreateAssetMenu(fileName = "SkillTreeNode", menuName = "Character/Skill Tree Node Data")]
public class SkillTreeNodeData : ScriptableObject
{
    #region 기본 정보

    [Header("노드 기본 정보")]
    [Tooltip("노드 ID (고유 식별자)")]
    [SerializeField] private string _nodeID;

    [Tooltip("노드 이름")]
    [SerializeField] private string _nodeName;

    [Tooltip("노드 설명")]
    [TextArea(3, 5)]
    [SerializeField] private string _description;

    [Tooltip("노드 아이콘")]
    [SerializeField] private Sprite _icon;

    [Tooltip("노드 티어 (1 = 최상위)")]
    [SerializeField] private int _tier = 1;

    #endregion

    #region 노드 타입

    [Header("노드 타입")]
    [Tooltip("패시브 스킬 여부 (false = 액티브 스킬)")]
    [SerializeField] private bool _isPassive = false;

    #endregion

    #region 스킬 참조

    [Header("스킬 데이터")]
    [Tooltip("액티브 스킬인 경우: SkillData 참조")]
    [SerializeField] private SkillData _skillData;

    #endregion

    #region 패시브 스킬 설정

    [Header("패시브 스킬 설정 (IsPassive = true일 때만)")]
    [Tooltip("패시브 스탯 보너스 리스트")]
    [SerializeField] private List<PassiveStatBonus> _passiveStatBonuses = new List<PassiveStatBonus>();

    #endregion

    #region 언락 조건

    [Header("언락 조건")]
    [Tooltip("필요 스킬 포인트")]
    [SerializeField] private int _requiredSkillPoints = 1;

    [Tooltip("필요 레벨 (선택사항)")]
    [SerializeField] private int _requiredLevel = 1;

    [Tooltip("선행 노드 (이 노드가 언락되어야 함)")]
    [SerializeField] private SkillTreeNodeData _requiredNodeData;

    #endregion

    #region UI 설정

    [Header("UI 표시 설정")]
    [Tooltip("UI 그리드 위치 (행)")]
    [SerializeField] private int _gridRow = 0;

    [Tooltip("UI 그리드 위치 (열)")]
    [SerializeField] private int _gridColumn = 0;

    #endregion

    #region 프로퍼티

    public string NodeID => _nodeID;
    public string NodeName => _nodeName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public int Tier => _tier;
    public bool IsPassive => _isPassive;
    public SkillData SkillData => _skillData;
    public List<PassiveStatBonus> PassiveStatBonuses => _passiveStatBonuses;
    public int RequiredSkillPoints => _requiredSkillPoints;
    public int RequiredLevel => _requiredLevel;
    public SkillTreeNodeData RequiredNodeData => _requiredNodeData;
    public int GridRow => _gridRow;
    public int GridColumn => _gridColumn;

    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        // NodeID 자동 생성 (비어있을 경우)
        if (string.IsNullOrEmpty(_nodeID))
        {
            _nodeID = $"Node_{name}_{GetInstanceID()}";
        }

        // 티어 검증
        _tier = Mathf.Max(1, _tier);
        _requiredSkillPoints = Mathf.Max(1, _requiredSkillPoints);
        _requiredLevel = Mathf.Max(1, _requiredLevel);

        // 타입별 검증
        if (_isPassive)
        {
            if (_skillData != null)
            {
                Debug.LogWarning($"[{_nodeName}] 패시브 노드는 SkillData를 사용하지 않습니다. PassiveStatBonuses를 설정하세요.", this);
            }

            if (_passiveStatBonuses == null || _passiveStatBonuses.Count == 0)
            {
                Debug.LogWarning($"[{_nodeName}] 패시브 노드에 스탯 보너스가 설정되지 않았습니다!", this);
            }
        }
        else // 액티브 스킬
        {
            if (_skillData == null)
            {
                Debug.LogWarning($"[{_nodeName}] 액티브 노드는 SkillData가 필요합니다!", this);
            }

            if (_passiveStatBonuses != null && _passiveStatBonuses.Count > 0)
            {
                Debug.LogWarning($"[{_nodeName}] 액티브 노드는 PassiveStatBonuses를 사용하지 않습니다.", this);
            }
        }

        // 순환 참조 검증
        if (_requiredNodeData != null)
        {
            if (_requiredNodeData == this)
            {
                Debug.LogError($"[{_nodeName}] 자기 자신을 선행 노드로 설정할 수 없습니다!", this);
                _requiredNodeData = null;
            }
            else if (_requiredNodeData._tier >= _tier)
            {
                Debug.LogWarning($"[{_nodeName}] 선행 노드의 티어가 현재 노드보다 낮거나 같습니다. 티어를 확인하세요.", this);
            }
        }
    }
#endif
}

/// <summary>
/// 패시브 스킬 스탯 보너스
/// </summary>
[System.Serializable]
public class PassiveStatBonus
{
    [Tooltip("스탯 타입")]
    public PassiveStatType StatType;

    [Tooltip("증가량")]
    public float Value;
}

/// <summary>
/// 패시브 스킬이 제공할 수 있는 스탯 타입
/// </summary>
public enum PassiveStatType
{
    // 주요 스탯
    Strength,
    Dexterity,
    Intelligence,
    Vitality,

    // 전투 스탯
    PhysicalAttack,
    MagicalAttack,
    Armor,
    AllResistance,

    // 2차 스탯
    CriticalChance,
    CriticalDamage,
    AttackSpeed,
    MovementSpeed,

    // 리소스
    MaxHealth,
    MaxMana,
    HealthRegeneration,
    ManaRegeneration
}
