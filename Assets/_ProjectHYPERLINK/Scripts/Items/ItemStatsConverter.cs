using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CharacterStats ↔ ItemStat 변환 유틸리티
/// </summary>
public static class ItemStatsConverter
{
    /// <summary>
    /// CharacterStats를 ItemStat 리스트로 변환
    /// </summary>
    public static List<ItemStat> CharacterStatsToItemStats(CharacterStats characterStats)
    {
        List<ItemStat> itemStats = new List<ItemStat>();

        if (characterStats == null)
            return itemStats;

        // 주요 스탯
        if (characterStats.Strength > 0)
            itemStats.Add(new ItemStat(ItemStatType.Strength, characterStats.Strength));
        if (characterStats.Dexterity > 0)
            itemStats.Add(new ItemStat(ItemStatType.Dexterity, characterStats.Dexterity));
        if (characterStats.Intelligence > 0)
            itemStats.Add(new ItemStat(ItemStatType.Intelligence, characterStats.Intelligence));
        if (characterStats.Vitality > 0)
            itemStats.Add(new ItemStat(ItemStatType.Vitality, characterStats.Vitality));

        // 전투 스탯
        if (characterStats.PhysicalAttack > 0)
            itemStats.Add(new ItemStat(ItemStatType.PhysicsAttack, characterStats.PhysicalAttack));
        if (characterStats.MagicalAttack > 0)
            itemStats.Add(new ItemStat(ItemStatType.MagicAttack, characterStats.MagicalAttack));
        if (characterStats.Armor > 0)
            itemStats.Add(new ItemStat(ItemStatType.Armor, characterStats.Armor));
        if (characterStats.AllResistance > 0)
            itemStats.Add(new ItemStat(ItemStatType.AllResistance, characterStats.AllResistance));

        // 2차 스탯
        if (characterStats.CriticalChance > 0)
            itemStats.Add(new ItemStat(ItemStatType.CriticalChance, characterStats.CriticalChance));
        if (characterStats.CriticalDamage > 0)
            itemStats.Add(new ItemStat(ItemStatType.CriticalDamage, characterStats.CriticalDamage));
        if (characterStats.AttackSpeed > 0)
            itemStats.Add(new ItemStat(ItemStatType.AttackSpeed, characterStats.AttackSpeed));

        // 리소스 스탯
        if (characterStats.MaxHealth > 0)
            itemStats.Add(new ItemStat(ItemStatType.Health, characterStats.MaxHealth));
        if (characterStats.HealthRegeneration > 0)
            itemStats.Add(new ItemStat(ItemStatType.HealthRegeneration, characterStats.HealthRegeneration));
        if (characterStats.ManaRegeneration > 0)
            itemStats.Add(new ItemStat(ItemStatType.ManaRegeneration, characterStats.ManaRegeneration));

        return itemStats;
    }

    /// <summary>
    /// ItemStatType의 표시 이름 반환
    /// </summary>
    public static string GetStatDisplayName(ItemStatType statType)
    {
        switch (statType)
        {
            case ItemStatType.Strength: return "힘";
            case ItemStatType.Dexterity: return "민첩";
            case ItemStatType.Intelligence: return "지능";
            case ItemStatType.Vitality: return "활력";
            case ItemStatType.PhysicsAttack: return "물리 공격력";
            case ItemStatType.MagicAttack: return "마법 공격력";
            case ItemStatType.CriticalChance: return "크리티컬 확률";
            case ItemStatType.CriticalDamage: return "크리티컬 데미지";
            case ItemStatType.AttackSpeed: return "공격 속도";
            case ItemStatType.Health: return "최대 체력";
            case ItemStatType.Armor: return "방어력";
            case ItemStatType.AllResistance: return "모든 저항";
            case ItemStatType.Speed: return "이동 속도";
            case ItemStatType.HealthRegeneration: return "체력 재생";
            case ItemStatType.ManaRegeneration: return "마나 재생";
            default: return statType.ToString();
        }
    }

    /// <summary>
    /// ItemQuality의 표시 이름 반환 (엠블렘)
    /// </summary>
    public static string GetQualityDisplayName(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.StandardEmblem: return "표준 엠블렘";
            case ItemQuality.SilverEmblem: return "실버 엠블렘";
            case ItemQuality.GoldEmblem: return "골드 엠블렘";
            case ItemQuality.DiamondEmblem: return "다이아몬드 엠블렘";
            default: return quality.ToString();
        }
    }

    /// <summary>
    /// ItemQuality의 UI 색상 반환 (엠블렘)
    /// </summary>
    public static Color GetQualityColor(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.StandardEmblem:
                return Color.white;  // 흰색
            case ItemQuality.SilverEmblem:
                return new Color(0.75f, 0.75f, 0.75f);  // 은색
            case ItemQuality.GoldEmblem:
                return new Color(1f, 0.84f, 0f);  // 금색
            case ItemQuality.DiamondEmblem:
                return new Color(0f, 0.8f, 0.8f);  // 청록색
            default:
                return Color.white;
        }
    }
}
