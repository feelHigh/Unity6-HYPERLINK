using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 저장 데이터 구조
/// </summary>
[Serializable]
public class CharacterSaveData
{
    public MetaData metadata;
    public CharacterInfo character;
    public CharacterStatsData stats;
    public ProgressionData progression;
    public EquipmentData equipment;
    public InventoryData inventory;
    public PositionData position;
    public GameplayData gameplay;

    [Serializable]
    public class MetaData
    {
        public string version;
        public string createdAt;
        public string lastPlayed;
        public long playTimeSeconds;
    }

    [Serializable]
    public class CharacterInfo
    {
        public string characterName;
        public string characterClass;
        public int level;
        public int experience;
    }

    [Serializable]
    public class CharacterStatsData
    {
        public float currentHealth;
        public float currentMana;
        public int redSoda;
        public BaseStats baseStats;
        public SecondaryStats secondaryStats;
        public float maxHealth;
        public float maxMana;

        [Serializable]
        public class BaseStats
        {
            public int strength;
            public int dexterity;
            public int intelligence;
            public int vitality;
        }

        [Serializable]
        public class SecondaryStats
        {
            public float criticalChance;
            public float criticalDamage;
            public float attackSpeed;
        }
    }

    /// <summary>
    /// 진행 데이터 (스킬 트리 + 스킬 슬롯 포함)
    /// </summary>
    [Serializable]
    public class ProgressionData
    {
        public List<string> unlockedSkills = new List<string>();
        public Dictionary<string, int> skillLevels = new Dictionary<string, int>();

        /// <summary>
        /// 스킬 트리 저장 데이터
        /// </summary>
        public SkillTreeSaveData skillTree = new SkillTreeSaveData();

        /// <summary>
        /// 스킬 슬롯 할당 저장
        /// 
        /// Q/W/E/R 슬롯에 할당된 스킬 저장
        /// 슬롯 인덱스: 0=Q, 1=W, 2=E, 3=R
        /// </summary>
        public List<SkillSlotData> skillSlots = new List<SkillSlotData>();
    }

    [Serializable]
    public class EquipmentData
    {
        public string weapon;
        public string helmet;
        public string chest;
        public string gloves;
        public string boots;
        public string necklace;
        public string ring;
    }

    [Serializable]
    public class InventoryData
    {
        public List<InventoryItem> items = new List<InventoryItem>();
        public int gold;

        [Serializable]
        public class InventoryItem
        {
            public string itemId;
            public int quantity;
            public int slot;
        }
    }

    [Serializable]
    public class PositionData
    {
        public string scene = "TutorialScene";
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class GameplayData
    {
        public string difficulty = "Normal";
        public int deaths;
        public int enemiesKilled;
        public List<string> questsCompleted = new List<string>();
    }

    /// <summary>
    /// 새 캐릭터 데이터 생성 (Unity 에디터 스탯 사용)
    /// 
    /// 주의사항:
    /// - Unity 에디터에서 설정한 스탯을 초기값으로 사용
    /// </summary>
    /// <param name="characterName">캐릭터 이름</param>
    /// <param name="characterClass">캐릭터 직업</param>
    /// <param name="baseStats">Unity 에디터에서 설정한 CharacterStats ScriptableObject</param>
    /// <returns>새로 생성된 CharacterSaveData</returns>
    public static CharacterSaveData CreateNew(string characterName, CharacterClass characterClass, CharacterStats baseStats)
    {
        return new CharacterSaveData
        {
            metadata = new MetaData
            {
                version = "1.0",
                createdAt = DateTime.UtcNow.ToString("o"),
                lastPlayed = DateTime.UtcNow.ToString("o"),
                playTimeSeconds = 0
            },
            character = new CharacterInfo
            {
                characterName = characterName,
                characterClass = characterClass.ToString(),
                level = 1,
                experience = 0
            },
            stats = CreateInitialStatsFromScriptableObject(baseStats),
            progression = new ProgressionData(),
            equipment = new EquipmentData(),
            inventory = new InventoryData { gold = 0 },
            position = new PositionData(),
            gameplay = new GameplayData()
        };
    }

    /// <summary>
    /// Unity 에디터에서 설정한 CharacterStats를 CharacterStatsData로 변환
    /// 
    /// 핵심 메서드:
    /// - 하드코딩된 스탯 대신 Unity 에디터 값 사용
    /// - ScriptableObject → Serializable 데이터 구조 변환
    /// </summary>
    /// <param name="baseStats">Unity에서 설정한 캐릭터 기본 스탯</param>
    /// <returns>저장 가능한 CharacterStatsData</returns>
    private static CharacterStatsData CreateInitialStatsFromScriptableObject(CharacterStats baseStats)
    {
        if (baseStats == null)
        {
            Debug.LogError("[CharacterSaveData] baseStats가 null입니다! 기본값으로 초기화합니다.");

            // Fallback: baseStats가 없을 경우 기본값 반환
            return new CharacterStatsData
            {
                currentHealth = 100,
                currentMana = 150,  // Intelligence 10 * 10 + MaxMana 50 = 150
                redSoda = 3,
                baseStats = new CharacterStatsData.BaseStats
                {
                    strength = 10,
                    dexterity = 10,
                    intelligence = 10,
                    vitality = 10
                },
                secondaryStats = new CharacterStatsData.SecondaryStats
                {
                    criticalChance = 5f,
                    criticalDamage = 50f,
                    attackSpeed = 1.0f
                },
                maxHealth = 100,
                maxMana = 150  // Intelligence 10 * 10 + MaxMana 50 = 150
            };
        }

        // Unity 에디터 스탯에서 값 추출
        var stats = new CharacterStatsData
        {
            // 초기 리소스는 최대값으로 설정 (HP와 Mana 모두)
            // HP: Vitality * 10 + MaxHealth
            // Mana: Intelligence * 10 + MaxMana
            currentHealth = (baseStats.Vitality * 10f) + baseStats.MaxHealth,
            currentMana = (baseStats.Intelligence * 10f) + baseStats.MaxMana,  // 수정: Intelligence * 10 추가
            redSoda = 3,  // 시작 시 레드 소다 3개

            // 주요 스탯 (Unity 에디터 값 사용)
            baseStats = new CharacterStatsData.BaseStats
            {
                strength = baseStats.Strength,
                dexterity = baseStats.Dexterity,
                intelligence = baseStats.Intelligence,
                vitality = baseStats.Vitality
            },

            // 2차 스탯 (Unity 에디터 값 사용)
            secondaryStats = new CharacterStatsData.SecondaryStats
            {
                criticalChance = baseStats.CriticalChance,
                criticalDamage = baseStats.CriticalDamage,
                attackSpeed = baseStats.AttackSpeed
            },

            // 최대 리소스 계산
            // HP: Vitality * 10 + MaxHealth
            // Mana: Intelligence * 10 + MaxMana
            maxHealth = (baseStats.Vitality * 10f) + baseStats.MaxHealth,
            maxMana = (baseStats.Intelligence * 10f) + baseStats.MaxMana
        };

        Debug.Log($"[CharacterSaveData] {baseStats.name} 스탯으로 초기화: STR {stats.baseStats.strength}, DEX {stats.baseStats.dexterity}, INT {stats.baseStats.intelligence}, VIT {stats.baseStats.vitality}");
        Debug.Log($"[CharacterSaveData] 초기 리소스: HP {stats.currentHealth}/{stats.maxHealth}, Mana {stats.currentMana}/{stats.maxMana}");

        return stats;
    }

    #region 디버그 헬퍼

    /// <summary>
    /// 저장 데이터 요약 출력 (디버그용)
    /// </summary>
    public void PrintSummary()
    {
        Debug.Log("===== CharacterSaveData 요약 =====");
        Debug.Log($"Character: {character.characterName} ({character.characterClass})");
        Debug.Log($"Level: {character.level} (EXP: {character.experience})");
        Debug.Log($"Stats: STR {stats.baseStats.strength}, DEX {stats.baseStats.dexterity}, INT {stats.baseStats.intelligence}, VIT {stats.baseStats.vitality}");
        Debug.Log($"HP: {stats.currentHealth}/{stats.maxHealth}, Mana: {stats.currentMana}/{stats.maxMana}");
        Debug.Log($"Unlocked Skills: {progression.unlockedSkills.Count}");
        Debug.Log($"Skill Tree: SP {progression.skillTree.currentSkillPoints}, Nodes {progression.skillTree.unlockedNodeIDs.Count}");

        // [NEW] 스킬 슬롯 정보
        Debug.Log($"Skill Slots: {progression.skillSlots.Count}");
        foreach (var slotData in progression.skillSlots)
        {
            string skillName = string.IsNullOrEmpty(slotData.assignedSkillID) ?
                "Empty" : slotData.assignedSkillID;
            Debug.Log($"  Slot {slotData.slotIndex}: {skillName}");
        }

        Debug.Log($"Play Time: {metadata.playTimeSeconds} seconds");
        Debug.Log($"Gold: {inventory.gold}");
    }

    #endregion
}

/// <summary>
/// 스킬 트리 저장 데이터
/// 
/// 역할:
/// - 현재 스킬 포인트 저장
/// - 언락된 노드 ID 목록 저장
/// 
/// 사용:
/// - CharacterSaveData.ProgressionData.skillTree에 포함
/// - SkillTreeManager.SaveSkillTree()에서 생성
/// - SkillTreeManager.LoadSkillTree()에서 로드
/// </summary>
[Serializable]
public class SkillTreeSaveData
{
    [Tooltip("현재 보유 스킬 포인트")]
    public int currentSkillPoints;

    [Tooltip("언락된 노드 ID 목록")]
    public List<string> unlockedNodeIDs = new List<string>();
}

/// <summary>
/// 스킬 슬롯 저장 데이터
/// 
/// 역할:
/// - Q/W/E/R 슬롯에 할당된 스킬 저장
/// - 게임 재시작 시 슬롯 할당 복원
/// 
/// 사용:
/// - CharacterSaveData.ProgressionData.skillSlots에 포함
/// - CharacterDataManager.SaveSkillSlots()에서 생성
/// - CharacterDataManager.LoadSkillSlots()에서 로드
/// </summary>
[Serializable]
public class SkillSlotData
{
    [Tooltip("슬롯 인덱스 (0=Q, 1=W, 2=E, 3=R)")]
    public int slotIndex;

    [Tooltip("할당된 스킬 ID (SkillData.SkillName)")]
    public string assignedSkillID;

    public SkillSlotData(int index, string skillID)
    {
        slotIndex = index;
        assignedSkillID = skillID;
    }
}
