using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

/// <summary>
/// 엠블렘 시스템 기반 절차적 아이템 생성
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

    [Header("아이템 프리팹")]
    [SerializeField] private Item _itemPrefab;

    [Header("아이템 템플릿")]
    [SerializeField] private ItemData[] _weaponTemplates;
    [SerializeField] private ItemData[] _equipmentTemplates;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnItem(Vector3 position, ItemDropTableData dropTable)
    {
        ItemQuality quality = dropTable.RollItemQuality();
        int itemTypeRoll = Random.Range(0, 2);
        Item spawnedItem = InstantiateItem(itemTypeRoll, quality);

        if (spawnedItem != null)
        {
            spawnedItem.transform.position = position;
        }
    }

    private Item InstantiateItem(int itemType, ItemQuality quality)
    {
        ItemData itemData =new ItemData();
        List <ItemStat> stats = new List < ItemStat >();
        if (itemType == 0 && _itemPrefab != null && _weaponTemplates != null && _weaponTemplates.Length > 0)
        {
            itemData = _weaponTemplates[Random.Range(0, _weaponTemplates.Length)].CreateRuntimeCopy();
            itemData.SetQuality(quality);

            stats = GenerateStats(_weapon_DropRanges, GetStatCountForQuality(quality));
            
        }
        else if (itemType == 1 && _itemPrefab != null && _equipmentTemplates != null && _equipmentTemplates.Length > 0)
        {
            itemData = _equipmentTemplates[Random.Range(0, _equipmentTemplates.Length)].CreateRuntimeCopy();
            itemData.SetQuality(quality);

            stats = GenerateStats(_equipment_DropRanges, GetStatCountForQuality(quality));
        }
        else
        {
            Debug.LogWarning("아이템 생성 실패 - 프리팹 또는 템플릿 누락");
            return null;
        }
        itemData.SetProceduralStats(stats);

        string generatedName = GenerateItemName(itemData.ItemName, quality, stats);
        itemData.SetName(generatedName);

        Item item = Instantiate(_itemPrefab);
        item.Initialize(itemData, quality, stats, generatedName);

        return item;

        
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
