using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 아이템의 개별 스탯 구조체
/// 절차적 아이템 생성(procedural generation)에 사용됨
/// 
/// 예시: ItemStat(ItemStatType.Strength, 25) = "힘 +25"
/// </summary>
[Serializable]
public struct ItemStat
{
    public ItemStatType Type;   // 스탯 종류 (힘, 민첩, 크리티컬 등)
    public float Value;         // 스탯 수치

    public ItemStat(ItemStatType type, float value)
    {
        Type = type;
        Value = value;
    }
}

/// <summary>
/// 통합 아이템 데이터 ScriptableObject
/// 모든 장비 아이템(무기, 방어구, 액세서리)의 데이터를 정의함
/// 
/// 주요 특징:
/// - 디자이너가 에디터에서 아이템을 생성하고 수정 가능
/// - 고정 스탯(디자이너가 직접 설정)과 절차적 스탯(랜덤 생성) 모두 지원
/// - ItemSpawner에서 이 템플릿을 사용해 런타임에 아이템 생성
/// 
/// 사용 예:
/// 1. 고정 아이템: 에디터에서 만든 전설 무기 "불의 검"
/// 2. 랜덤 아이템: 템플릿 "기본 검"을 사용해 매번 다른 스탯의 검 생성
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Equipment Item")]
public class ItemData : ScriptableObject
{
    #region 기본 정보

    [Header("기본 아이템 정보")]
    [SerializeField] private int _itemNumber = 0;           // 아이템 고유 번호 (데이터베이스용)
    [SerializeField] private string _itemName;              // 아이템 이름 (예: "강철 검")
    [SerializeField] private string _description;           // 아이템 설명
    [SerializeField] private Sprite _itemIcon;              // 인벤토리 UI에 표시될 아이콘

    #endregion

    #region 분류 정보

    [Header("아이템 분류")]
    [SerializeField] private ItemQuality _quality = ItemQuality.Normal;         // 등급 (일반/마법/희귀/전설)
    [SerializeField] private EquipmentType _equipmentType = EquipmentType.None; // 장비 부위 (무기/투구/갑옷 등)
    [SerializeField] private int _requiredLevel = 1;                            // 착용 필요 레벨

    #endregion

    #region 모델/인벤토리

    [Header("모델 & 인벤토리")]
    [Tooltip("3D 모델 프리팹 - 월드에 떨어진 아이템 표시용")]
    [SerializeField] private GameObject _itemModel;

    [Tooltip("인벤토리 그리드 크기 (1x1, 1x2, 2x2 등)")]
    [SerializeField][Range(1, 3)] private int _gridSizeX = 1;
    [SerializeField][Range(1, 3)] private int _gridSizeY = 1;

    #endregion

    #region 스탯 시스템 (듀얼 지원)

    [Header("아이템 스탯")]
    [Tooltip("고정 스탯 (디자이너가 에디터에서 직접 설정)")]
    [SerializeField] private CharacterStats _baseStats;

    [Tooltip("절차적 스탯 (ItemSpawner가 랜덤 생성)")]
    [SerializeField] private List<ItemStat> _proceduralStats = new List<ItemStat>();

    [Tooltip("절차적 스탯 사용 여부 (true면 _proceduralStats 사용, false면 _baseStats 사용)")]
    [SerializeField] private bool _useProceduralStats = false;

    #endregion

    #region Public 프로퍼티

    // 읽기 전용 프로퍼티들 - 다른 시스템에서 접근용
    public int ItemNumber => _itemNumber;
    public string ItemName => _itemName;
    public string Description => _description;
    public Sprite ItemIcon => _itemIcon;
    public ItemQuality Quality => _quality;
    public EquipmentType EquipmentType => _equipmentType;
    public int RequiredLevel => _requiredLevel;
    public GameObject ItemModel => _itemModel;
    public Vector2Int GridSize => new Vector2Int(_gridSizeX, _gridSizeY);

    /// <summary>
    /// 캐릭터 스탯 시스템용 스탯 반환
    /// EquipmentManager에서 장비 착용 시 사용
    /// 
    /// 절차적 스탯 사용 중이면 ItemStat을 CharacterStats로 변환
    /// 고정 스탯 사용 중이면 _baseStats를 그대로 반환
    /// </summary>
    public CharacterStats Stats
    {
        get
        {
            if (_useProceduralStats)
                return ConvertProceduralToCharacterStats();
            return _baseStats;
        }
    }

    /// <summary>
    /// 절차적 스탯 리스트 반환
    /// ItemSpawner와 UI 시스템에서 사용
    /// </summary>
    public List<ItemStat> ProceduralStats => _proceduralStats;

    public bool UseProceduralStats => _useProceduralStats;

    #endregion

    #region 런타임 메서드

    /// <summary>
    /// 런타임에 절차적 스탯 설정
    /// ItemSpawner가 랜덤 아이템 생성 시 호출
    /// 
    /// 사용 예:
    /// ItemData swordData = swordTemplate.CreateRuntimeCopy();
    /// swordData.SetProceduralStats(randomStats); // 랜덤 스탯 부여
    /// </summary>
    public void SetProceduralStats(List<ItemStat> stats)
    {
        _proceduralStats = new List<ItemStat>(stats);
        _useProceduralStats = true;
    }

    /// <summary>
    /// 런타임에 아이템 등급 설정
    /// 드랍 시스템이 아이템 등급을 결정한 후 호출
    /// </summary>
    public void SetQuality(ItemQuality quality)
    {
        _quality = quality;
    }

    /// <summary>
    /// 런타임에 아이템 이름 설정
    /// 절차적 생성된 아이템의 고유 이름 부여용
    /// 
    /// 예: "기본 검" → "힘의 강력한 기본 검"
    /// </summary>
    public void SetName(string name)
    {
        _itemName = name;
    }

    /// <summary>
    /// 절차적 스탯을 CharacterStats로 변환
    /// 
    /// 변환 과정:
    /// 1. ItemStat 리스트를 순회
    /// 2. 각 스탯을 CharacterStats 필드에 매핑
    /// 3. 기본 스탯이 있으면 합산
    /// 
    /// 예: [힘+10, 크리티컬+5%] → CharacterStats { Strength=10, CritChance=5 }
    /// 
    /// ItemStatsConverter를 사용해 실제 변환 수행
    /// </summary>
    private CharacterStats ConvertProceduralToCharacterStats()
    {
        if (_proceduralStats == null || _proceduralStats.Count == 0)
            return _baseStats;

        // ItemStatsConverter를 사용해 변환
        CharacterStats converted = ItemStatsConverter.ConvertToCharacterStats(_proceduralStats);

        // 기본 스탯이 있으면 합산 (절차적 스탯 + 기본 스탯)
        if (_baseStats != null && converted != null)
        {
            return _baseStats.AddStats(converted);
        }

        return converted != null ? converted : _baseStats;
    }

    /// <summary>
    /// 아이템 데이터의 런타임 복사본 생성
    /// 
    /// 필요한 이유:
    /// - ScriptableObject는 모든 인스턴스가 공유됨
    /// - 각 드랍 아이템마다 독립적인 스탯을 가져야 함
    /// - 원본 템플릿을 보존하면서 변경 가능한 복사본 생성
    /// 
    /// 사용 시나리오:
    /// 1. ItemSpawner가 "기본 검" 템플릿 로드
    /// 2. CreateRuntimeCopy()로 독립적인 복사본 생성
    /// 3. 복사본에 랜덤 스탯 부여
    /// 4. 복사본을 실제 월드 아이템에 할당
    /// </summary>
    public ItemData CreateRuntimeCopy()
    {
        ItemData copy = ScriptableObject.CreateInstance<ItemData>();

        // 모든 필드 값 복사
        copy._itemNumber = this._itemNumber;
        copy._itemName = this._itemName;
        copy._description = this._description;
        copy._itemIcon = this._itemIcon;
        copy._quality = this._quality;
        copy._equipmentType = this._equipmentType;
        copy._requiredLevel = this._requiredLevel;
        copy._itemModel = this._itemModel;
        copy._gridSizeX = this._gridSizeX;
        copy._gridSizeY = this._gridSizeY;
        copy._baseStats = this._baseStats;
        copy._proceduralStats = new List<ItemStat>(this._proceduralStats);
        copy._useProceduralStats = this._useProceduralStats;

        return copy;
    }

    #endregion
}