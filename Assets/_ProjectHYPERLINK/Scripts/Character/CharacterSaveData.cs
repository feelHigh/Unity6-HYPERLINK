using System;
using System.Collections.Generic;

/// <summary>
/// 클라우드 저장용 캐릭터 데이터 구조체
/// 
/// 전체 캐릭터 정보를 단일 JSON으로 직렬화
/// 
/// 구조:
/// - metadata: 메타 정보 (버전, 생성일, 플레이 시간)
/// - character: 캐릭터 기본 정보 (이름, 직업, 레벨)
/// - stats: 스탯 정보 (체력, 마나, 주요 스탯)
/// - progression: 진행도 (언락 스킬, 스킬 레벨)
/// - equipment: 장비 정보 (각 슬롯별 아이템 ID)
/// - inventory: 인벤토리 (아이템 목록, 골드)
/// - position: 위치 정보 (씬, 좌표)
/// - gameplay: 게임플레이 통계 (난이도, 사망 횟수 등)
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
        public string version = "1.0";
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
        public BaseStats baseStats;
        public SecondaryStats secondaryStats;

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

    [Serializable]
    public class ProgressionData
    {
        public List<string> unlockedSkills = new List<string>();
        public Dictionary<string, int> skillLevels = new Dictionary<string, int>();
    }

    [Serializable]
    public class EquipmentData
    {
        public string weapon;
        public string helmet;
        public string chest;
        public string gloves;
        public string boots;
        public string amulet;
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
        public string scene = "MainLevel";
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
    /// 새 캐릭터 생성
    /// 
    /// CharacterSelectionController에서 호출
    /// 직업별 초기 스탯 설정
    /// </summary>
    public static CharacterSaveData CreateNew(string characterName, CharacterClass characterClass)
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
            stats = CreateInitialStats(characterClass),
            progression = new ProgressionData(),
            equipment = new EquipmentData(),
            inventory = new InventoryData { gold = 0 },
            position = new PositionData(),
            gameplay = new GameplayData()
        };
    }

    /// <summary>
    /// 직업별 초기 스탯 생성
    /// 
    /// Warrior: 힘 중심
    /// Mage: 지능 중심
    /// Archer: 민첩 중심
    /// </summary>
    private static CharacterStatsData CreateInitialStats(CharacterClass characterClass)
    {
        var stats = new CharacterStatsData
        {
            currentHealth = 100,
            currentMana = 50,
            baseStats = new CharacterStatsData.BaseStats(),
            secondaryStats = new CharacterStatsData.SecondaryStats
            {
                criticalChance = 5f,
                criticalDamage = 50f,
                attackSpeed = 1.0f
            }
        };

        switch (characterClass)
        {
            case CharacterClass.Warrior:
                stats.baseStats.strength = 15;
                stats.baseStats.dexterity = 8;
                stats.baseStats.intelligence = 5;
                stats.baseStats.vitality = 12;
                break;

            case CharacterClass.Mage:
                stats.baseStats.strength = 5;
                stats.baseStats.dexterity = 8;
                stats.baseStats.intelligence = 15;
                stats.baseStats.vitality = 10;
                break;

            case CharacterClass.Archer:
                stats.baseStats.strength = 8;
                stats.baseStats.dexterity = 15;
                stats.baseStats.intelligence = 7;
                stats.baseStats.vitality = 10;
                break;
        }

        return stats;
    }
}