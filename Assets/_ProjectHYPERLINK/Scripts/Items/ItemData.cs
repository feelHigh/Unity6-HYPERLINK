using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public struct ItemStat
{
    public ItemStatType Type;
    public float Value;

    public ItemStat(ItemStatType type, float value)
    {
        Type = type;
        Value = value;
    }
}

/// <summary>
/// 통합 아이템 데이터 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Equipment Item")]
public class ItemData : ScriptableObject
{
    #region 기본 정보

    [Header("기본 아이템 정보")]
    [SerializeField] private int _itemNumber = 0;
    [SerializeField] private string _itemName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _itemIcon;

    #endregion

    #region 분류 정보

    [Header("아이템 분류")]
    [SerializeField] private ItemQuality _quality = ItemQuality.StandardEmblem;
    [SerializeField] private EquipmentType _equipmentType = EquipmentType.None;
    [SerializeField] private int _requiredLevel = 1;

    #endregion

    #region 모델/인벤토리

    [Header("모델 & 인벤토리")]
    [SerializeField] private GameObject _itemModel;
    [SerializeField][Range(1, 3)] private int _gridSizeX = 1;
    [SerializeField][Range(1, 3)] private int _gridSizeY = 1;

    #endregion

    #region 스탯 시스템

    [Header("아이템 스탯")]
    [SerializeField] private CharacterStats _baseStats;
    [SerializeField] private List<ItemStat> _proceduralStats = new List<ItemStat>();
    [SerializeField] private bool _useProceduralStats = false;

    #endregion

    #region Public 프로퍼티

    public int ItemNumber => _itemNumber;
    public string ItemName => _itemName;
    public string Description => _description;
    public Sprite ItemIcon => _itemIcon;
    public ItemQuality Quality => _quality;
    public EquipmentType EquipmentType => _equipmentType;
    public int RequiredLevel => _requiredLevel;
    public GameObject ItemModel => _itemModel;
    public Vector2Int GridSize => new Vector2Int(_gridSizeX, _gridSizeY);

    public CharacterStats Stats
    {
        get
        {
            if (_useProceduralStats)
                return ConvertProceduralToCharacterStats();
            return _baseStats;
        }
    }

    public List<ItemStat> ProceduralStats => _proceduralStats;
    public bool UseProceduralStats => _useProceduralStats;

    #endregion

    #region 런타임 메서드

    public void SetProceduralStats(List<ItemStat> stats)
    {
        _proceduralStats = new List<ItemStat>(stats);
        _useProceduralStats = true;
    }

    public void SetQuality(ItemQuality quality)
    {
        _quality = quality;
    }

    public void SetName(string name)
    {
        _itemName = name;
    }

    /// <summary>
    /// 절차적 스탯을 CharacterStats로 변환
    /// </summary>
    private CharacterStats ConvertProceduralToCharacterStats()
    {
        CharacterStats stats = ScriptableObject.CreateInstance<CharacterStats>();
        CharacterStatsBuilder builder = new CharacterStatsBuilder();

        foreach (ItemStat stat in _proceduralStats)
        {
            switch (stat.Type)
            {
                case ItemStatType.Strength:
                    builder.AddStrength((int)stat.Value);
                    break;
                case ItemStatType.Dexterity:
                    builder.AddDexterity((int)stat.Value);
                    break;
                case ItemStatType.Intelligence:
                    builder.AddIntelligence((int)stat.Value);
                    break;
                case ItemStatType.Vitality:
                    builder.AddVitality((int)stat.Value);
                    break;
                case ItemStatType.PhysicsAttack:
                    builder.AddPhysicalAttack(stat.Value);
                    break;
                case ItemStatType.MagicAttack:
                    builder.AddMagicalAttack(stat.Value);
                    break;
                case ItemStatType.Armor:
                    builder.AddArmor(stat.Value);
                    break;
                case ItemStatType.AllResistance:
                    builder.AddAllResistance(stat.Value);
                    break;
                case ItemStatType.CriticalChance:
                    builder.AddCriticalChance(stat.Value);
                    break;
                case ItemStatType.CriticalDamage:
                    builder.AddCriticalDamage(stat.Value);
                    break;
                case ItemStatType.AttackSpeed:
                    builder.AddAttackSpeed(stat.Value);
                    break;
                case ItemStatType.Health:
                    builder.AddMaxHealth(stat.Value);
                    break;
                case ItemStatType.HealthRegeneration:
                    builder.AddHealthRegeneration(stat.Value);
                    break;
                case ItemStatType.ManaRegeneration:
                    builder.AddManaRegeneration(stat.Value);
                    break;
            }
        }

        return builder.Build();
    }

    public ItemData CreateRuntimeCopy()
    {
        ItemData copy = ScriptableObject.CreateInstance<ItemData>();

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
