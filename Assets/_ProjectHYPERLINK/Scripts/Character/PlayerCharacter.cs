using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 플레이어 캐릭터 핵심 시스템
/// 
/// 변경사항:
/// - RecalculateResources() → RecalculateStats()
/// - CalculateDerivedStats() 추가 (주요 스탯 기반 파생 스탯 계산)
/// - GetTotalStats()에 파생 스탯 계산 로직 추가
/// - LoadFromSaveData()에서 2차 스탯 로드 제거
/// - SaveToData()에서 2차 스탯 저장 제거
/// - Dexterity에 MovementSpeed 파생 스탯 추가
/// - Dexterity → Attack Speed 비율 변경 (0.1 → 0.05)
/// 
/// 공식:
/// - Strength (1당): Physical Attack +1, All Resistance +0.1
/// - Dexterity (1당): Critical Chance +1, Attack Speed +0.05, Movement Speed +0.1
/// - Intelligence (1당): Magical Attack +1, Max Mana +10, Mana Regen +0.1
/// - Vitality (1당): Armor +1, Max Health +10
/// </summary>
public class PlayerCharacter : MonoBehaviour
{
    [Header("캐릭터 설정")]
    [SerializeField] private CharacterClass _characterClass = CharacterClass.Laon;
    [SerializeField] private SkillData[] _availableSkills;

    [Header("현재 리소스")]
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _currentMana;

    [Header("소비 아이템")]
    [SerializeField] private int _redSoda = 3;
    [SerializeField] private float _redSodaHealAmount = 50f;

    private PlayerStateController _stateController;

    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnManaChanged;
    public static event Action<CharacterStats> OnStatsChanged;
    public static event Action<int> OnRedSodaChanged;
    public static event Action<SkillData> OnSkillUnlocked;
    public static event Action<float> OnPlayerHit;
    public static event Action OnPlayerDead;

    private CharacterStats _currentStats;
    private CharacterStats _equipmentStats;
    private List<SkillData> _unlockedSkills = new List<SkillData>();
    private List<CharacterStats> _temporaryBuffs = new List<CharacterStats>();

    private float _maxHealth;
    private float _maxMana;

    private float _healthRegenTimer = 0f;
    private float _manaRegenTimer = 0f;
    private const float REGEN_TICK_INTERVAL = 1f;

    public CharacterClass CharacterClass => _characterClass;
    public CharacterStats CurrentStats => GetTotalStats();
    public CharacterStats BaseStats => _currentStats;
    public CharacterStats EquipmentStats => _equipmentStats;
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public float CurrentMana => _currentMana;
    public float MaxMana => _maxMana;
    public int RedSoda => _redSoda;
    public List<SkillData> UnlockedSkills => _unlockedSkills;
    public bool IsAlive => _currentHealth > 0;
    public float HealthPercentage => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
    public float ManaPercentage => _maxMana > 0 ? _currentMana / _maxMana : 0f;

    #region 초기화

    private void Awake()
    {
        InitializeCharacter();

        _stateController = GetComponent<PlayerStateController>();
        if (_stateController == null)
        {
            Debug.LogWarning("[PlayerCharacter] PlayerStateController가 없습니다.");
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        HandleRegeneration();
    }

    private void InitializeCharacter()
    {
        _currentStats = ScriptableObject.CreateInstance<CharacterStats>();
        _equipmentStats = ScriptableObject.CreateInstance<CharacterStats>();

        RecalculateStats();
        _currentHealth = _maxHealth;
        _currentMana = _maxMana;

        UnlockInitialSkills();
    }

    private void UnlockInitialSkills()
    {
        UnlockSkillsForLevel(1);
        Debug.Log($"[PlayerCharacter] 초기 스킬 언락 완료: {_unlockedSkills.Count}개");
    }

    #endregion

    #region 스탯 관리

    public void AddLevelUpStats(CharacterStats statGains)
    {
        if (statGains == null)
        {
            Debug.LogWarning("레벨업 스탯 증가량이 null입니다!");
            return;
        }

        _currentStats = _currentStats.AddStats(statGains);
        RecalculateStats();
        _currentHealth = _maxHealth;
        _currentMana = _maxMana;
        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    public void AddTemporaryStats(CharacterStats buffStats)
    {
        if (buffStats == null) return;
        _temporaryBuffs.Add(buffStats);
        RecalculateStats();
        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    public void RemoveTemporaryStats(CharacterStats buffStats)
    {
        if (buffStats == null) return;
        _temporaryBuffs.Remove(buffStats);
        RecalculateStats();
        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    private void ClearAllTemporaryBuffs()
    {
        _temporaryBuffs.Clear();
        RecalculateStats();
        UpdateUI();
    }

    public void UpdateEquipmentStats(CharacterStats equipmentStats)
    {
        if (equipmentStats == null)
        {
            Debug.LogWarning("장비 스탯이 null입니다!");
            _equipmentStats = ScriptableObject.CreateInstance<CharacterStats>();
        }
        else
        {
            _equipmentStats = equipmentStats;
        }

        RecalculateStats();
        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    /// <summary>
    /// 모든 스탯 합산 (기본 + 장비 + 버프 + 파생 스탯)
    /// </summary>
    public CharacterStats GetTotalStats()
    {
        CharacterStats total = _currentStats;

        if (_equipmentStats != null)
        {
            total = total.AddStats(_equipmentStats);
        }

        foreach (CharacterStats buffStats in _temporaryBuffs)
        {
            if (buffStats != null)
            {
                total = total.AddStats(buffStats);
            }
        }

        CharacterStats derivedStats = CalculateDerivedStats(total);
        total = total.AddStats(derivedStats);

        return total;
    }

    /// <summary>
    /// 주요 스탯에서 파생 스탯 자동 계산
    /// </summary>
    private CharacterStats CalculateDerivedStats(CharacterStats baseStats)
    {
        CharacterStatsBuilder builder = new CharacterStatsBuilder();

        // Strength: Physical Attack +1, All Resistance +0.1
        builder.AddPhysicalAttack(baseStats.Strength * 1f);
        builder.AddAllResistance(baseStats.Strength * 0.1f);

        // Dexterity: Critical Chance +1, Attack Speed +0.05, Movement Speed +0.1
        builder.AddCriticalChance(baseStats.Dexterity * 1f);
        builder.AddAttackSpeed(baseStats.Dexterity * 0.05f);
        builder.AddMovementSpeed(baseStats.Dexterity * 0.1f);

        // Intelligence: Magical Attack +1, Mana Regen +0.1
        builder.AddMagicalAttack(baseStats.Intelligence * 1f);
        builder.AddManaRegeneration(baseStats.Intelligence * 0.1f);

        // Vitality: Armor +1
        builder.AddArmor(baseStats.Vitality * 1f);

        return builder.Build();
    }

    public int GetMainStat()
    {
        CharacterStats totalStats = GetTotalStats();

        switch (_characterClass)
        {
            case CharacterClass.Laon:
                return totalStats.Strength;
            case CharacterClass.Sian:
                return totalStats.Intelligence;
            case CharacterClass.Yujin:
                return totalStats.Dexterity;
            default:
                return totalStats.Strength;
        }
    }

    /// <summary>
    /// 리소스 재계산
    /// </summary>
    private void RecalculateStats()
    {
        CharacterStats totalStats = GetTotalStats();

        _maxHealth = (totalStats.Vitality * 10f) + totalStats.MaxHealth;

        float baseMana = totalStats.Intelligence * 10f;
        _maxMana = baseMana + totalStats.MaxMana;

        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        _currentMana = Mathf.Min(_currentMana, _maxMana);
    }

    #endregion

    #region 리소스 재생

    private void HandleRegeneration()
    {
        if (!IsAlive) return;

        CharacterStats totalStats = GetTotalStats();

        _healthRegenTimer += Time.deltaTime;
        if (_healthRegenTimer >= REGEN_TICK_INTERVAL)
        {
            _healthRegenTimer = 0f;
            float healthRegen = totalStats.HealthRegeneration;
            if (healthRegen > 0 && _currentHealth < _maxHealth)
            {
                _currentHealth = Mathf.Min(_currentHealth + healthRegen, _maxHealth);
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            }
        }

        _manaRegenTimer += Time.deltaTime;
        if (_manaRegenTimer >= REGEN_TICK_INTERVAL)
        {
            _manaRegenTimer = 0f;
            float manaRegen = totalStats.ManaRegeneration;
            if (manaRegen > 0 && _currentMana < _maxMana)
            {
                _currentMana = Mathf.Min(_currentMana + manaRegen, _maxMana);
                OnManaChanged?.Invoke(_currentMana, _maxMana);
            }
        }
    }

    #endregion

    #region 스킬 관리

    public void UnlockSkillsForLevel(int level)
    {
        foreach (SkillData skill in _availableSkills)
        {
            if (skill.RequiredLevel == level && !_unlockedSkills.Contains(skill))
            {
                _unlockedSkills.Add(skill);
                OnSkillUnlocked?.Invoke(skill);
                Debug.Log($"[PlayerCharacter] 스킬 언락: {skill.SkillName}");
            }
        }
    }

    #endregion

    #region 전투 및 리소스 관리

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        float actualDamage = amount;
        if (_stateController != null && _stateController.IsWeakened)
        {
            float defenseMultiplier = _stateController.GetDefenseMultiplier();
            actualDamage = amount / defenseMultiplier;
            Debug.Log($"[약화] 데미지 증가: {amount:F1} → {actualDamage:F1}");
        }

        _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnPlayerHit?.Invoke(actualDamage);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public bool ConsumeMana(float amount)
    {
        if (_currentMana >= amount)
        {
            _currentMana -= amount;
            OnManaChanged?.Invoke(_currentMana, _maxMana);
            return true;
        }
        return false;
    }

    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void RestoreMana(float amount)
    {
        _currentMana = Mathf.Min(_currentMana + amount, _maxMana);
        OnManaChanged?.Invoke(_currentMana, _maxMana);
    }

    public void UseRedSoda()
    {
        if (_redSoda <= 0)
        {
            Debug.Log("레드 소다가 없습니다!");
            return;
        }

        if (_currentHealth >= _maxHealth)
        {
            Debug.Log("체력이 가득 찼습니다!");
            return;
        }

        _redSoda--;
        Heal(_redSodaHealAmount);
        Debug.Log($"레드 소다 사용! (남은 개수: {_redSoda})");
        OnRedSodaChanged?.Invoke(_redSoda);
    }

    public void AddRedSoda(int amount)
    {
        _redSoda += amount;
        Debug.Log($"레드 소다 {amount}개 획득! (총: {_redSoda})");
        OnRedSodaChanged?.Invoke(_redSoda);
    }

    private void Die()
    {
        Debug.Log("플레이어 사망!");
        ClearAllTemporaryBuffs();
        OnPlayerDead?.Invoke();
    }

    #endregion

    #region UI 업데이트

    private void UpdateUI()
    {
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnManaChanged?.Invoke(_currentMana, _maxMana);
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    #endregion

    #region Cloud Save 통합

    /// <summary>
    /// 2차 스탯 로드 제거 (파생 스탯 자동 계산)
    /// </summary>
    public void LoadFromSaveData(CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("로드할 데이터가 null입니다!");
            return;
        }

        if (data.stats?.baseStats != null)
        {
            _currentStats = new CharacterStatsBuilder()
                .SetStrength(data.stats.baseStats.strength)
                .SetDexterity(data.stats.baseStats.dexterity)
                .SetIntelligence(data.stats.baseStats.intelligence)
                .SetVitality(data.stats.baseStats.vitality)
                .Build();
        }

        RecalculateStats();
        _currentHealth = data.stats.currentHealth;
        _currentMana = data.stats.currentMana;
        _redSoda = data.stats.redSoda;

        _unlockedSkills.Clear();
        if (data.progression?.unlockedSkills != null)
        {
            foreach (SkillData skill in _availableSkills)
            {
                if (data.progression.unlockedSkills.Contains(skill.SkillName))
                {
                    _unlockedSkills.Add(skill);
                }
            }
        }

        UpdateUI();
        Debug.Log($"캐릭터 데이터 로드 완료 (레벨: {data.character.level})");
    }

    /// <summary>
    /// 2차 스탯 저장 제거 (로드 시 재계산)
    /// </summary>
    public void SaveToData(CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("저장할 데이터가 null입니다!");
            return;
        }

        if (data.stats == null)
        {
            data.stats = new CharacterSaveData.CharacterStatsData();
        }

        if (data.stats.baseStats == null)
        {
            data.stats.baseStats = new CharacterSaveData.CharacterStatsData.BaseStats();
        }

        data.stats.baseStats.strength = _currentStats.Strength;
        data.stats.baseStats.dexterity = _currentStats.Dexterity;
        data.stats.baseStats.intelligence = _currentStats.Intelligence;
        data.stats.baseStats.vitality = _currentStats.Vitality;

        data.stats.currentHealth = _currentHealth;
        data.stats.currentMana = _currentMana;
        data.stats.maxHealth = _maxHealth;
        data.stats.maxMana = _maxMana;
        data.stats.redSoda = _redSoda;

        if (data.progression == null)
        {
            data.progression = new CharacterSaveData.ProgressionData();
        }

        data.progression.unlockedSkills.Clear();
        foreach (SkillData skill in _unlockedSkills)
        {
            data.progression.unlockedSkills.Add(skill.SkillName);
        }

        Debug.Log($"캐릭터 데이터 저장 완료 (스킬: {_unlockedSkills.Count}개)");
    }

    #endregion

    #region 디버그

    [ContextMenu("Debug: Print Character Info")]
    private void DebugPrintInfo()
    {
        Debug.Log("===== PlayerCharacter 정보 =====");
        Debug.Log($"클래스: {_characterClass}");
        Debug.Log($"체력: {_currentHealth:F0}/{_maxHealth:F0}");
        Debug.Log($"마나: {_currentMana:F0}/{_maxMana:F0}");
        Debug.Log($"레드 소다: {_redSoda}개");

        CharacterStats totalStats = GetTotalStats();
        Debug.Log($"--- 주요 스탯 ---");
        Debug.Log($"  힘: {totalStats.Strength}");
        Debug.Log($"  민첩: {totalStats.Dexterity}");
        Debug.Log($"  지능: {totalStats.Intelligence}");
        Debug.Log($"  활력: {totalStats.Vitality}");

        Debug.Log($"--- 파생 스탯 ---");
        Debug.Log($"  물리 공격력: {totalStats.PhysicalAttack:F1}");
        Debug.Log($"  마법 공격력: {totalStats.MagicalAttack:F1}");
        Debug.Log($"  방어력: {totalStats.Armor:F1}");
        Debug.Log($"  모든 저항: {totalStats.AllResistance:F1}");
        Debug.Log($"  크리티컬 확률: {totalStats.CriticalChance:F1}");
        Debug.Log($"  공격 속도: {totalStats.AttackSpeed:F1}");
        Debug.Log($"  이동 속도: {totalStats.MovementSpeed:F1}");
        Debug.Log($"  마나 재생: {totalStats.ManaRegeneration:F1}");
    }

    [ContextMenu("Test: Take Damage (100)")]
    private void TestTakeDamage()
    {
        TakeDamage(100f);
    }

    [ContextMenu("Test: Heal Full")]
    private void TestHealFull()
    {
        Heal(_maxHealth);
    }

    [ContextMenu("Test: Add Strength +10")]
    private void TestAddStrength()
    {
        CharacterStats strengthBonus = new CharacterStatsBuilder()
            .SetStrength(10)
            .Build();

        AddLevelUpStats(strengthBonus);
        Debug.Log("[테스트] Strength +10 추가");
    }

    #endregion
}
