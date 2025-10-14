using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ItemStat(절차적)과 CharacterStats(ScriptableObject) 간 변환 유틸리티 클래스
/// 
/// 목적:
/// - 두 개의 스탯 시스템을 원활하게 통합
/// - ItemStat: 절차적 아이템 생성에 사용 (구조체, 가변)
/// - CharacterStats: 캐릭터 시스템에 사용 (ScriptableObject, 불변)
/// 
/// 주요 기능:
/// - ItemStat → CharacterStats 변환 (아이템 착용 시)
/// - CharacterStats → ItemStat 변환 (UI 표시 시)
/// - 스탯 타입별 표시 이름 제공
/// - 스탯 값 포맷팅 (정수/실수/퍼센트)
/// 
/// 사용 시나리오:
/// 
/// 1. 아이템 착용:
///    ItemData (ItemStat[]) 
///    → ConvertToCharacterStats() 
///    → CharacterStats 
///    → PlayerCharacter 적용
/// 
/// 2. 아이템 툴팁:
///    ItemData.Stats 
///    → FormatStatValue() 
///    → UI 표시 ("힘 +25", "크리티컬 +5.0%")
/// 
/// 3. 장비 비교:
///    CharacterStats 
///    → ConvertToItemStats() 
///    → ItemStat[] 
///    → 새 아이템과 비교
/// 
/// 기술적 제약:
/// - CharacterStats의 필드가 private이라 Reflection 사용
/// - 성능이 중요한 곳에서는 사용 주의
/// - 추후 CharacterStats 리팩토링 시 개선 가능
/// </summary>
public static class ItemStatsConverter
{
    /// <summary>
    /// ItemStat 리스트를 CharacterStats ScriptableObject로 변환
    /// 
    /// 사용 위치:
    /// - ItemData.ConvertProceduralToCharacterStats()
    /// - EquipmentManager.RecalculateEquipmentStats()
    /// 
    /// 처리 과정:
    /// 1. 빈 CharacterStats 인스턴스 생성
    /// 2. 각 ItemStat을 순회
    /// 3. Reflection을 사용해 private 필드 설정
    /// 4. 타입 변환 (int/float) 처리
    /// 5. 최종 CharacterStats 반환
    /// 
    /// Parameters:
    ///     itemStats: 변환할 ItemStat 리스트
    ///     
    /// Returns:
    ///     CharacterStats: 변환된 스탯 객체 (null이면 null 반환)
    ///     
    /// 주의사항:
    /// - Reflection 사용으로 성능 저하 가능
    /// - 필드 이름이 정확해야 함 (매핑 테이블 참조)
    /// - 일부 ItemStat은 CharacterStats에 매핑 안 될 수 있음
    /// </summary>
    public static CharacterStats ConvertToCharacterStats(List<ItemStat> itemStats)
    {
        if (itemStats == null || itemStats.Count == 0)
            return null;

        CharacterStats stats = ScriptableObject.CreateInstance<CharacterStats>();

        // CharacterStats의 private 필드에 접근하기 위해
        // Reflection이 필요함 (또는 CharacterStats 리팩토링)
        // 헬퍼 메서드로 실제 변환 수행
        return CreateCharacterStatsFromItemStats(itemStats);
    }

    /// <summary>
    /// CharacterStats를 ItemStat 리스트로 변환
    /// 
    /// 사용 시나리오:
    /// - 장비 스탯을 통합 형식으로 표시
    /// - 아이템 비교 UI
    /// - 디버그 로그
    /// 
    /// 처리 과정:
    /// 1. 빈 ItemStat 리스트 생성
    /// 2. CharacterStats의 각 프로퍼티 확인
    /// 3. 0보다 큰 값만 ItemStat으로 변환
    /// 4. 리스트에 추가
    /// 
    /// Parameters:
    ///     characterStats: 변환할 CharacterStats
    ///     
    /// Returns:
    ///     List<ItemStat>: 변환된 스탯 리스트 (빈 리스트 가능)
    ///     
    /// 변환 규칙:
    /// - 0 이하 값은 제외 (의미 없는 스탯)
    /// - Primary/Secondary/Resource 스탯 모두 포함
    /// </summary>
    public static List<ItemStat> ConvertToItemStats(CharacterStats characterStats)
    {
        if (characterStats == null)
            return new List<ItemStat>();

        List<ItemStat> itemStats = new List<ItemStat>();

        // === 주요 스탯 (Primary Stats) ===
        if (characterStats.Strength > 0)
            itemStats.Add(new ItemStat(ItemStatType.Strength, characterStats.Strength));
        if (characterStats.Dexterity > 0)
            itemStats.Add(new ItemStat(ItemStatType.Dexterity, characterStats.Dexterity));
        if (characterStats.Intelligence > 0)
            itemStats.Add(new ItemStat(ItemStatType.Intelligence, characterStats.Intelligence));
        if (characterStats.Vitality > 0)
            itemStats.Add(new ItemStat(ItemStatType.Vitality, characterStats.Vitality));

        // === 2차 스탯 (Secondary Stats) ===
        if (characterStats.CriticalChance > 0)
            itemStats.Add(new ItemStat(ItemStatType.CriticalChance, characterStats.CriticalChance));
        if (characterStats.CriticalDamage > 0)
            itemStats.Add(new ItemStat(ItemStatType.CriticalDamage, characterStats.CriticalDamage));
        if (characterStats.AttackSpeed > 0)
            itemStats.Add(new ItemStat(ItemStatType.AttackSpeed, characterStats.AttackSpeed));

        // === 리소스 스탯 (Resource Stats) ===
        if (characterStats.MaxHealth > 0)
            itemStats.Add(new ItemStat(ItemStatType.Health, characterStats.MaxHealth));
        if (characterStats.HealthRegeneration > 0)
            itemStats.Add(new ItemStat(ItemStatType.HealthRegeneration, characterStats.HealthRegeneration));
        if (characterStats.ManaRegeneration > 0)
            itemStats.Add(new ItemStat(ItemStatType.ManaRegeneration, characterStats.ManaRegeneration));

        return itemStats;
    }

    /// <summary>
    /// ItemStat 리스트로부터 CharacterStats 인스턴스 생성 (헬퍼 메서드)
    /// 
    /// 기술적 구현:
    /// - Reflection을 사용해 private 필드 접근
    /// - 타입 변환 처리 (float → int/float)
    /// - 매핑되지 않는 스탯은 무시
    /// 
    /// Parameters:
    ///     itemStats: 변환할 ItemStat 리스트
    ///     
    /// Returns:
    ///     CharacterStats: 생성된 스탯 객체
    ///     
    /// TODO: CharacterStats 리팩토링 시 개선
    /// - Public setter 추가
    /// - Reflection 제거
    /// - 성능 향상
    /// </summary>
    private static CharacterStats CreateCharacterStatsFromItemStats(List<ItemStat> itemStats)
    {
        // 임시 CharacterStats 생성
        CharacterStats stats = ScriptableObject.CreateInstance<CharacterStats>();

        // Reflection으로 private 필드 접근
        var type = typeof(CharacterStats);

        foreach (ItemStat itemStat in itemStats)
        {
            // ItemStatType → CharacterStats 필드명 매핑
            string fieldName = GetCharacterStatsFieldName(itemStat.Type);

            if (!string.IsNullOrEmpty(fieldName))
            {
                // private 필드 찾기
                var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    // 타입에 맞게 변환
                    if (field.FieldType == typeof(int))
                    {
                        // float → int 변환 (반올림)
                        field.SetValue(stats, Mathf.RoundToInt(itemStat.Value));
                    }
                    else
                    {
                        // float 그대로 사용
                        field.SetValue(stats, itemStat.Value);
                    }
                }
            }
        }

        return stats;
    }

    /// <summary>
    /// ItemStatType을 CharacterStats 필드명으로 매핑
    /// 
    /// 매핑 테이블:
    /// - ItemStatType → CharacterStats private 필드명
    /// 
    /// 반환 예시:
    /// - Strength → "_strength"
    /// - CriticalChance → "_criticalChance"
    /// - Health → "_maxHealth"
    /// 
    /// Parameters:
    ///     statType: 변환할 ItemStatType
    ///     
    /// Returns:
    ///     string: 필드명 (매핑 없으면 null)
    ///     
    /// 주의:
    /// - 필드명은 CharacterStats.cs의 실제 필드명과 일치해야 함
    /// - CharacterStats 리팩토링 시 수정 필요
    /// - 일부 ItemStat은 매핑 없음 (PhysicsAttack, MagicAttack 등)
    /// </summary>
    private static string GetCharacterStatsFieldName(ItemStatType statType)
    {
        switch (statType)
        {
            // === 주요 스탯 ===
            case ItemStatType.Strength: return "_strength";
            case ItemStatType.Dexterity: return "_dexterity";
            case ItemStatType.Intelligence: return "_intelligence";
            case ItemStatType.Vitality: return "_vitality";

            // === 2차 스탯 ===
            case ItemStatType.CriticalChance: return "_criticalChance";
            case ItemStatType.CriticalDamage: return "_criticalDamage";
            case ItemStatType.AttackSpeed: return "_attackSpeed";

            // === 리소스 스탯 ===
            case ItemStatType.Health: return "_maxHealth";
            case ItemStatType.HealthRegeneration: return "_healthRegeneration";
            case ItemStatType.ManaRegeneration: return "_manaRegeneration";

            // === 매핑 없음 (CharacterStats에 해당 필드 없음) ===
            // PhysicsAttack, MagicAttack, Armor, AllResistance
            // 이들은 별도 처리 필요하거나 CharacterStats 확장 필요

            default: return null;
        }
    }

    /// <summary>
    /// ItemStatType의 사용자 친화적 표시 이름 반환
    /// 
    /// 사용 위치:
    /// - 아이템 툴팁 UI
    /// - 장비 비교 창
    /// - 캐릭터 스탯 창
    /// 
    /// Parameters:
    ///     statType: 표시 이름을 가져올 스탯 타입
    ///     
    /// Returns:
    ///     string: 한국어 표시 이름
    ///     
    /// 예시:
    /// - Strength → "힘"
    /// - CriticalChance → "크리티컬 확률"
    /// - PhysicsAttack → "물리 공격력"
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
            case ItemStatType.Health: return "체력";
            case ItemStatType.Armor: return "방어력";
            case ItemStatType.AllResistance: return "모든 저항";
            case ItemStatType.Speed: return "이동 속도";
            case ItemStatType.HealthRegeneration: return "체력 재생";
            case ItemStatType.ManaRegeneration: return "마나 재생";
            default: return statType.ToString();
        }
    }

    /// <summary>
    /// 스탯 값을 표시용으로 포맷팅
    /// 
    /// 포맷 규칙:
    /// - 퍼센트 스탯: "15.5%" (소수점 1자리)
    /// - 정수 스탯: "25" (소수점 없음)
    /// - 실수 스탯: "12.5" (소수점 1자리)
    /// 
    /// 사용 위치:
    /// - 아이템 툴팁
    /// - 장비 비교 창
    /// - 캐릭터 스탯 창
    /// 
    /// Parameters:
    ///     statType: 스탯 종류
    ///     value: 스탯 값
    ///     
    /// Returns:
    ///     string: 포맷된 문자열
    ///     
    /// 예시:
    /// - (Strength, 25) → "25"
    /// - (CriticalChance, 15.5) → "15.5%"
    /// - (HealthRegeneration, 12.3) → "12.3"
    /// </summary>
    public static string FormatStatValue(ItemStatType statType, float value)
    {
        // === 퍼센트 스탯 ===
        // 소수점 1자리 + % 기호
        if (statType == ItemStatType.CriticalChance ||
            statType == ItemStatType.CriticalDamage ||
            statType == ItemStatType.AttackSpeed)
        {
            return $"{value:F1}%";
        }

        // === 정수 스탯 ===
        // 주요 스탯, 방어력 등은 정수로 표시
        if (statType == ItemStatType.Strength ||
            statType == ItemStatType.Dexterity ||
            statType == ItemStatType.Intelligence ||
            statType == ItemStatType.Vitality ||
            statType == ItemStatType.Armor)
        {
            return Mathf.RoundToInt(value).ToString();
        }

        // === 실수 스탯 ===
        // 기타 스탯은 소수점 1자리로 표시
        return $"{value:F1}";
    }
}