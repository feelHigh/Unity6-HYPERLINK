using System;
using System.Collections.Generic;

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
        public int redSoda;  // ← NEW: Red Soda count
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

    private static CharacterStatsData CreateInitialStats(CharacterClass characterClass)
    {
        var stats = new CharacterStatsData
        {
            currentHealth = 100,
            currentMana = 50,
            redSoda = 3,  // ← NEW: Start with 3 Red Sodas
            baseStats = new CharacterStatsData.BaseStats(),
            secondaryStats = new CharacterStatsData.SecondaryStats
            {
                criticalChance = 5f,
                criticalDamage = 50f,
                attackSpeed = 1.0f
            },
            maxHealth = 100,
            maxMana = 50
        };

        switch (characterClass)
        {
            case CharacterClass.Laon:
                stats.baseStats.strength = 15;
                stats.baseStats.dexterity = 8;
                stats.baseStats.intelligence = 5;
                stats.baseStats.vitality = 12;
                break;

            case CharacterClass.Sian:
                stats.baseStats.strength = 5;
                stats.baseStats.dexterity = 8;
                stats.baseStats.intelligence = 15;
                stats.baseStats.vitality = 10;
                break;

            case CharacterClass.Yujin:
                stats.baseStats.strength = 8;
                stats.baseStats.dexterity = 15;
                stats.baseStats.intelligence = 7;
                stats.baseStats.vitality = 10;
                break;
        }

        return stats;
    }
}
