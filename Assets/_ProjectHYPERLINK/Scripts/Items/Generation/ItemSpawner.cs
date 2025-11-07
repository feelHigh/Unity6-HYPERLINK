using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엠블렘 시스템 기반 절차적 아이템 생성
/// 
/// 수정사항:
/// - 장비 타입별 템플릿 분리
/// - 특정 장비 타입 스폰 기능 추가
/// </summary>
[System.Serializable]
public class DropStatsAndRange
{
    [SerializeField] private ItemStatType _type;
    [SerializeField] private float _minStat;
    [SerializeField] private float _maxStat;

    public ItemStatType Type => _type;
    public float MinStat => _minStat;
    public float MaxStat => _maxStat;
}

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    [Header("스탯 롤링 설정")]
    [SerializeField] private DropStatsAndRange[] _equipment_DropRanges;
    [SerializeField] private DropStatsAndRange[] _weapon_DropRanges;
    [SerializeField] private int _minGold;
    [SerializeField] private int _maxGold;

    [Header("아이템 프리팹")]
    [SerializeField] private Item _itemPrefab;

    [Header("무기 템플릿")]
    [SerializeField] private ItemData[] _weaponTemplates;

    [Header("장비 템플릿 (타입별 분리)")]
    [SerializeField] private ItemData[] _helmetTemplates;
    [SerializeField] private ItemData[] _chestTemplates;
    [SerializeField] private ItemData[] _glovesTemplates;
    [SerializeField] private ItemData[] _bootsTemplates;
    [SerializeField] private ItemData[] _necklaceTemplates;
    [SerializeField] private ItemData[] _ringTemplates;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 기존 SpawnItem - 랜덤 장비 타입
    /// </summary>
    public void SpawnItem(Vector3 position, ItemDropTableData dropTable)
    {
        ItemQuality quality = dropTable.RollItemQuality();
        int itemTypeRoll = Random.Range(0, 2);
        Item spawnedItem = InstantiateItem(itemTypeRoll, quality, EquipmentType.None);

        if (spawnedItem != null)
        {
            spawnedItem.transform.position = position;
        }
    }

    /// <summary>
    /// 특정 장비 타입으로 스폰 (새로 추가)
    /// </summary>
    public void SpawnItemWithType(Vector3 position, ItemDropTableData dropTable, EquipmentType equipmentType)
    {
        ItemQuality quality = dropTable.RollItemQuality();
        Item spawnedItem = InstantiateEquipment(quality, equipmentType);

        if (spawnedItem != null)
        {
            spawnedItem.transform.position = position;
        }
    }

    public ItemData SpawnItemData(ItemDropTableData dropTable)
    {
        ItemData data = new ItemData();
        List<ItemStat> stats = new List<ItemStat>();
        ItemQuality quality = dropTable.RollItemQuality();
        int rnd = Random.Range(0, 2);
        if (rnd == 0)
        {
            data = _weaponTemplates[Random.Range(0, _weaponTemplates.Length)].CreateRuntimeCopy();
            data.SetQuality(quality);
            stats = GenerateStats(_weapon_DropRanges, GetStatCountForQuality(quality));
        }
        else
        {
            EquipmentType equip = GetRandomEquipmentType();
            data = GetEquipmentTemplate(equip);

            data.SetQuality(quality);
            stats = GenerateStats(_equipment_DropRanges, GetStatCountForQuality(quality));
        }
        data.SetProceduralStats(stats);

        int gold = Random.Range(_minGold, _maxGold);
        gold *= ((int)quality + 1);
        data.SetGold(gold);

        string generatedName = GenerateItemName(data.ItemName, quality, stats);
        data.SetName(generatedName);
        Debug.Log("아이템 나옴");
        return data;
    }

    /// <summary>
    /// 아이템 생성 (리팩토링)
    /// </summary>
    private Item InstantiateItem(int itemType, ItemQuality quality, EquipmentType specificType)
    {
        ItemData itemData = new ItemData();
        List<ItemStat> stats = new List<ItemStat>();
        
        if (itemType == 0 && _itemPrefab != null && _weaponTemplates != null && _weaponTemplates.Length > 0)
        {
            // 무기 생성
            itemData = _weaponTemplates[Random.Range(0, _weaponTemplates.Length)].CreateRuntimeCopy();
            itemData.SetQuality(quality);
            stats = GenerateStats(_weapon_DropRanges, GetStatCountForQuality(quality));
        }
        else if (itemType == 1 && _itemPrefab != null)
        {
            // 장비 생성 - 타입별 분리
            if (specificType == EquipmentType.None)
            {
                // 랜덤 장비 타입 선택
                specificType = GetRandomEquipmentType();
            }

            itemData = GetEquipmentTemplate(specificType);
            if (itemData == null)
            {
                Debug.LogWarning($"[ItemSpawner] 장비 템플릿을 찾을 수 없습니다: {specificType}");
                return null;
            }

            itemData = itemData.CreateRuntimeCopy();
            itemData.SetQuality(quality);
            stats = GenerateStats(_equipment_DropRanges, GetStatCountForQuality(quality));
        }
        else
        {
            Debug.LogWarning("[ItemSpawner] 아이템 생성 실패 - 프리팹 또는 템플릿 누락");
            return null;
        }
        int gold = Random.Range(_minGold, _maxGold);
        gold *= ((int)quality+1);
        itemData.SetGold(gold);
        itemData.SetProceduralStats(stats);

        string generatedName = GenerateItemName(itemData.ItemName, quality, stats);
        itemData.SetName(generatedName);

        Item item = Instantiate(_itemPrefab);
        item.Initialize(itemData, quality, stats, generatedName);

        return item;
    }

    /// <summary>
    /// 특정 타입의 장비 생성
    /// </summary>
    private Item InstantiateEquipment(ItemQuality quality, EquipmentType equipmentType)
    {
        ItemData itemData = GetEquipmentTemplate(equipmentType);
        if (itemData == null)
        {
            Debug.LogWarning($"[ItemSpawner] 장비 템플릿을 찾을 수 없습니다: {equipmentType}");
            return null;
        }

        itemData = itemData.CreateRuntimeCopy();
        itemData.SetQuality(quality);

        List<ItemStat> stats = GenerateStats(_equipment_DropRanges, GetStatCountForQuality(quality));
        itemData.SetProceduralStats(stats);

        int gold = Random.Range(_minGold, _maxGold);
        gold *= ((int)quality + 1);
        itemData.SetGold(gold);

        string generatedName = GenerateItemName(itemData.ItemName, quality, stats);
        itemData.SetName(generatedName);

        Item item = Instantiate(_itemPrefab);
        item.Initialize(itemData, quality, stats, generatedName);

        return item;
    }

    /// <summary>
    /// 장비 타입에 맞는 템플릿 가져오기
    /// </summary>
    private ItemData GetEquipmentTemplate(EquipmentType equipmentType)
    {
        ItemData[] templates = null;

        switch (equipmentType)
        {
            case EquipmentType.Helmet:
                templates = _helmetTemplates;
                break;
            case EquipmentType.Chest:
                templates = _chestTemplates;
                break;
            case EquipmentType.Gloves:
                templates = _glovesTemplates;
                break;
            case EquipmentType.Boots:
                templates = _bootsTemplates;
                break;
            case EquipmentType.Necklace:
                templates = _necklaceTemplates;
                break;
            case EquipmentType.Ring:
                templates = _ringTemplates;
                break;
            default:
                Debug.LogWarning($"[ItemSpawner] 지원하지 않는 장비 타입: {equipmentType}");
                return null;
        }

        if (templates == null || templates.Length == 0)
        {
            Debug.LogWarning($"[ItemSpawner] {equipmentType} 템플릿이 비어있습니다!");
            return null;
        }

        return templates[Random.Range(0, templates.Length)];
    }

    /// <summary>
    /// 랜덤 장비 타입 선택
    /// </summary>
    private EquipmentType GetRandomEquipmentType()
    {
        List<EquipmentType> availableTypes = new List<EquipmentType>();

        if (_helmetTemplates != null && _helmetTemplates.Length > 0) availableTypes.Add(EquipmentType.Helmet);
        if (_chestTemplates != null && _chestTemplates.Length > 0) availableTypes.Add(EquipmentType.Chest);
        if (_glovesTemplates != null && _glovesTemplates.Length > 0) availableTypes.Add(EquipmentType.Gloves);
        if (_bootsTemplates != null && _bootsTemplates.Length > 0) availableTypes.Add(EquipmentType.Boots);
        if (_necklaceTemplates != null && _necklaceTemplates.Length > 0) availableTypes.Add(EquipmentType.Necklace);
        if (_ringTemplates != null && _ringTemplates.Length > 0) availableTypes.Add(EquipmentType.Ring);

        if (availableTypes.Count == 0)
        {
            Debug.LogWarning("[ItemSpawner] 사용 가능한 장비 템플릿이 없습니다!");
            return EquipmentType.None;
        }

        return availableTypes[Random.Range(0, availableTypes.Count)];
    }

    /// <summary>
    /// 랜덤 스탯 생성
    /// </summary>
    private List<ItemStat> GenerateStats(DropStatsAndRange[] ranges, int count)
    {
        List<ItemStat> stats = new List<ItemStat>();
        List<ItemStatType> usedTypes = new List<ItemStatType>();

        if (ranges == null || ranges.Length == 0 || count <= 0)
            return stats;

        int attempts = 0;
        int maxAttempts = count * 10;

        while (stats.Count < count && attempts < maxAttempts)
        {
            attempts++;
            DropStatsAndRange range = ranges[Random.Range(0, ranges.Length)];

            if (usedTypes.Contains(range.Type))
                continue;

            float value = Random.Range(range.MinStat, range.MaxStat);
            stats.Add(new ItemStat(range.Type, value));
            usedTypes.Add(range.Type);
        }

        return stats;
    }

    /// <summary>
    /// 엠블렘 등급별 스탯 개수
    /// 
    /// - StandardEmblem: 0-1개
    /// - SilverEmblem: 2-3개
    /// - GoldEmblem: 4-5개
    /// - DiamondEmblem: 6-7개
    /// </summary>
    private int GetStatCountForQuality(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.StandardEmblem:
                return Random.Range(0, 2);  // 0-1개
            case ItemQuality.SilverEmblem:
                return Random.Range(2, 4);  // 2-3개
            case ItemQuality.GoldEmblem:
                return Random.Range(4, 6);  // 4-5개
            case ItemQuality.DiamondEmblem:
                return Random.Range(6, 8);  // 6-7개
            default:
                return 0;
        }
    }

    /// <summary>
    /// 엠블렘 기반 아이템 이름 생성
    /// </summary>
    private string GenerateItemName(string baseName, ItemQuality quality, List<ItemStat> stats)
    {
        string prefix = GetQualityPrefix(quality);
        string suffix = stats.Count > 0 ? GetStatSuffix(stats[0].Type) : "";

        return $"{prefix} {suffix} {baseName}".Trim();
    }

    /// <summary>
    /// 엠블렘 등급별 접두사
    /// </summary>
    private string GetQualityPrefix(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.StandardEmblem:
                return "";  // 접두사 없음
            case ItemQuality.SilverEmblem:
                return "은빛";
            case ItemQuality.GoldEmblem:
                return "황금";
            case ItemQuality.DiamondEmblem:
                return "찬란한";
            default:
                return "";
        }
    }

    /// <summary>
    /// 스탯 타입별 접미사
    /// </summary>
    private string GetStatSuffix(ItemStatType statType)
    {
        switch (statType)
        {
            case ItemStatType.Strength:
                return "힘의";
            case ItemStatType.Dexterity:
                return "민첩의";
            case ItemStatType.Intelligence:
                return "지혜의";
            case ItemStatType.Vitality:
                return "활력의";
            case ItemStatType.PhysicsAttack:
                return "물리의";
            case ItemStatType.MagicAttack:
                return "마법의";
            case ItemStatType.CriticalChance:
                return "치명의";
            case ItemStatType.Armor:
                return "방어의";
            default:
                return "";
        }
    }
}
