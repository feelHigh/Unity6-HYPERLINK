using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어 캐릭터 핵심 시스템
/// BaseStats 우선 로직 + MaxHealth/MaxMana 저장 포함
/// </summary>
public class PlayerCharacter : MonoBehaviour
{
    [Header("캐릭터 설정")]
    [SerializeField] private CharacterClass _characterClass = CharacterClass.Laon;
    [SerializeField] private CharacterStats _baseStats;
    [SerializeField] private SkillData[] _availableSkills;

    [Header("현재 리소스")]
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _currentMana;

    [Header("소비 아이템")]
    [SerializeField] private int _redSoda = 3;
    [SerializeField] private float _redSodaHealAmount = 50f;

    // 이벤트
    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnManaChanged;
    public static event Action<CharacterStats> OnStatsChanged;
    public static event Action<int> OnRedSodaChanged;
    public static event Action<SkillData> OnSkillUnlocked;

    // 스탯
    private CharacterStats _currentStats;
    private CharacterStats _equipmentStats;
    private List<SkillData> _unlockedSkills = new List<SkillData>();
    private List<CharacterStats> _temporaryBuffs = new List<CharacterStats>();

    // 계산된 값
    private float _maxHealth;
    private float _maxMana;

    // 재생
    private float _healthRegenTimer = 0f;
    private float _manaRegenTimer = 0f;
    private const float REGEN_TICK_INTERVAL = 1f;

    // Public 프로퍼티
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

    private void Awake()
    {
        InitializeCharacter();
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

        if (_baseStats != null)
        {
            _currentStats = _baseStats.Clone();
        }

        RecalculateResources();
        _currentHealth = _maxHealth;
        _currentMana = _maxMana;
    }

    public void AddLevelUpStats(CharacterStats statGains)
    {
        if (statGains == null)
        {
            Debug.LogWarning("레벨업 스탯 증가량이 null입니다!");
            return;
        }

        _currentStats = _currentStats.AddStats(statGains);
        RecalculateResources();
        _currentHealth = _maxHealth;
        _currentMana = _maxMana;
        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    public void AddTemporaryStats(CharacterStats buffStats)
    {
        if (buffStats == null) return;
        _temporaryBuffs.Add(buffStats);
        RecalculateResources();
        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    public void RemoveTemporaryStats(CharacterStats buffStats)
    {
        if (buffStats == null) return;
        _temporaryBuffs.Remove(buffStats);
        RecalculateResources();
        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    private void ClearAllTemporaryBuffs()
    {
        _temporaryBuffs.Clear();
        RecalculateResources();
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

        RecalculateResources();
        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());
    }

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

        return total;
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

    private void RecalculateResources()
    {
        CharacterStats totalStats = GetTotalStats();
        _maxHealth = (totalStats.Vitality * 10f) + totalStats.MaxHealth;
        _maxMana = totalStats.MaxMana;
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        _currentMana = Mathf.Min(_currentMana, _maxMana);
    }

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

    public void UnlockSkillsForLevel(int level)
    {
        foreach (SkillData skill in _availableSkills)
        {
            if (skill.RequiredLevel == level && !_unlockedSkills.Contains(skill))
            {
                _unlockedSkills.Add(skill);
                OnSkillUnlocked?.Invoke(skill);
                Debug.Log($"스킬 언락: {skill.SkillName}");
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

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

    /// <summary>
    /// 레드 소다 사용 (Number 1 키)
    /// </summary>
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

        Debug.Log($"레드 소다 사용! 체력 {_redSodaHealAmount} 회복 (남은 개수: {_redSoda})");
        OnRedSodaChanged?.Invoke(_redSoda);
    }

    /// <summary>
    /// 레드 소다 추가 (드랍/구매)
    /// </summary>
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
    }

    private void UpdateUI()
    {
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnManaChanged?.Invoke(_currentMana, _maxMana);
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    #region Cloud Save 통합

    /// <summary>
    /// CharacterSaveData에서 데이터 로드
    /// Red Soda 로드 추가
    /// </summary>
    public void LoadFromSaveData(CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("로드할 데이터가 null입니다!");
            return;
        }

        // 신규 캐릭터 감지
        bool isNewCharacter = data.character.level == 1 && data.character.experience == 0;

        if (isNewCharacter && _baseStats != null)
        {
            // 신규 캐릭터: 프리팹 BaseStats 우선 사용
            Debug.Log("신규 캐릭터 감지 - 프리팹 BaseStats 사용");
            _currentStats = _baseStats.Clone();
            RecalculateResources();
            _currentHealth = Mathf.Min(data.stats.currentHealth, _maxHealth);
            _currentMana = Mathf.Min(data.stats.currentMana, _maxMana);
        }
        else
        {
            // 기존 캐릭터: SaveData 사용
            Debug.Log("기존 캐릭터 감지 - SaveData 사용");
            _currentStats = new CharacterStatsBuilder()
                .SetStrength(data.stats.baseStats.strength)
                .SetDexterity(data.stats.baseStats.dexterity)
                .SetIntelligence(data.stats.baseStats.intelligence)
                .SetVitality(data.stats.baseStats.vitality)
                .SetCriticalChance(data.stats.secondaryStats.criticalChance)
                .SetCriticalDamage(data.stats.secondaryStats.criticalDamage)
                .SetAttackSpeed(data.stats.secondaryStats.attackSpeed)
                .SetMaxHealth(data.stats.maxHealth)
                .SetMaxMana(data.stats.maxMana)
                .Build();

            RecalculateResources();
            _currentHealth = Mathf.Clamp(data.stats.currentHealth, 1f, _maxHealth);
            _currentMana = Mathf.Clamp(data.stats.currentMana, 0f, _maxMana);
        }

        // RED SODA 로드 추가
        _redSoda = data.stats.redSoda;
        Debug.Log($"Red Soda 로드: {_redSoda}개");
        OnRedSodaChanged?.Invoke(_redSoda);

        // 장비 스탯 초기화
        if (_equipmentStats == null)
        {
            _equipmentStats = ScriptableObject.CreateInstance<CharacterStats>();
        }

        // 스킬 언락 복원
        _unlockedSkills.Clear();
        if (data.progression.unlockedSkills != null)
        {
            foreach (string skillName in data.progression.unlockedSkills)
            {
                SkillData skill = System.Array.Find(_availableSkills, s => s.SkillName == skillName);
                if (skill != null)
                {
                    _unlockedSkills.Add(skill);
                    Debug.Log($"스킬 복원: {skillName}");
                }
            }
        }

        UpdateUI();
        Debug.Log($"=== 캐릭터 로드 완료 ===");
        Debug.Log($"HP: {_currentHealth}/{_maxHealth}, MP: {_currentMana}/{_maxMana}, Red Soda: {_redSoda}");
    }

    /// <summary>
    /// SaveData에 현재 상태 저장
    /// </summary>
    public void SaveToData(CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("저장할 데이터가 null입니다!");
            return;
        }

        CharacterStats totalStats = GetTotalStats();

        // 기본 스탯 저장
        data.stats.baseStats.strength = _currentStats.Strength;
        data.stats.baseStats.dexterity = _currentStats.Dexterity;
        data.stats.baseStats.intelligence = _currentStats.Intelligence;
        data.stats.baseStats.vitality = _currentStats.Vitality;

        // 2차 스탯 저장
        data.stats.secondaryStats.criticalChance = totalStats.CriticalChance;
        data.stats.secondaryStats.criticalDamage = totalStats.CriticalDamage;
        data.stats.secondaryStats.attackSpeed = totalStats.AttackSpeed;

        // 리소스 저장
        data.stats.maxHealth = _currentStats.MaxHealth;
        data.stats.maxMana = _currentStats.MaxMana;
        data.stats.currentHealth = _currentHealth;
        data.stats.currentMana = _currentMana;
        data.stats.redSoda = _redSoda;

        // 스킬 저장
        data.progression.unlockedSkills.Clear();
        foreach (SkillData skill in _unlockedSkills)
        {
            data.progression.unlockedSkills.Add(skill.SkillName);
        }

        Debug.Log($"캐릭터 데이터 저장 완료 - HP: {_currentHealth}/{_maxHealth}, MP: {_currentMana}/{_maxMana}, Red Soda: {_redSoda}");
    }

    #endregion
}
