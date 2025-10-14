using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어 캐릭터 핵심 시스템 (Cloud Save 통합 + 전투 지원)
/// 
/// 주요 변경사항:
/// - Cloud Save 통합 (LoadFromSaveData/SaveToData)
/// - GetMainStat() 메서드 추가 (SkillActivationSystem 지원)
/// - TryConsumeMana() → ConsumeMana()로 변경 (bool 반환)
/// - Heal() / RestoreMana() 메서드 추가
/// 
/// 기능:
/// - 캐릭터 스탯 관리 (기본 + 장비)
/// - 체력/마나 시스템
/// - 스킬 언락 시스템
/// - 재생 시스템
/// - 전투 데미지 처리
/// </summary>
public class PlayerCharacter : MonoBehaviour
{
    [Header("캐릭터 설정")]
    [SerializeField] private CharacterClass _characterClass = CharacterClass.Warrior;
    [SerializeField] private CharacterStats _baseStats;
    [SerializeField] private SkillData[] _availableSkills;

    [Header("현재 리소스")]
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _currentMana;

    // 이벤트
    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnManaChanged;
    public static event Action<CharacterStats> OnStatsChanged;
    public static event Action<SkillData> OnSkillUnlocked;

    // 스탯
    private CharacterStats _currentStats;    // 기본 스탯
    private CharacterStats _equipmentStats;  // 장비 스탯
    private List<SkillData> _unlockedSkills = new List<SkillData>();

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

    /// <summary>
    /// 캐릭터 초기화 (신규 캐릭터용)
    /// </summary>
    private void InitializeCharacter()
    {
        _currentStats = ScriptableObject.CreateInstance<CharacterStats>();
        _equipmentStats = ScriptableObject.CreateInstance<CharacterStats>();

        if (_baseStats != null)
        {
            CopyStats(_baseStats, _currentStats);
        }

        RecalculateResources();
        _currentHealth = _maxHealth;
        _currentMana = _maxMana;
    }

    /// <summary>
    /// 레벨업 시 스탯 증가
    /// ExperienceManager에서 호출
    /// </summary>
    public void AddLevelUpStats(CharacterStats statGains)
    {
        _currentStats = _currentStats.AddStats(statGains);
        RecalculateResources();

        // 레벨업 보상: 완전 회복
        _currentHealth = _maxHealth;
        _currentMana = _maxMana;

        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    /// <summary>
    /// 장비 스탯 업데이트
    /// EquipmentManager에서 호출
    /// </summary>
    public void UpdateEquipmentStats(CharacterStats equipmentStats)
    {
        _equipmentStats = equipmentStats;
        RecalculateResources();
        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    /// <summary>
    /// 총 스탯 계산 (기본 + 장비)
    /// </summary>
    public CharacterStats GetTotalStats()
    {
        if (_equipmentStats == null)
            return _currentStats;

        return _currentStats.AddStats(_equipmentStats);
    }

    /// <summary>
    /// 주요 스탯 반환 (직업별)
    /// 
    /// SkillActivationSystem에서 데미지 계산 시 사용
    /// 
    /// 반환값:
    /// - Warrior: 힘 (Strength)
    /// - Mage: 지능 (Intelligence)
    /// - Archer: 민첩 (Dexterity)
    /// 
    /// 데미지 공식에 사용:
    /// 최종 데미지 = 기본 데미지 × (1 + MainStat / 100)
    /// </summary>
    public int GetMainStat()
    {
        CharacterStats totalStats = GetTotalStats();

        switch (_characterClass)
        {
            case CharacterClass.Warrior:
                return totalStats.Strength;
            case CharacterClass.Mage:
                return totalStats.Intelligence;
            case CharacterClass.Archer:
                return totalStats.Dexterity;
            default:
                return totalStats.Strength;
        }
    }

    /// <summary>
    /// 최대 체력/마나 재계산
    /// </summary>
    private void RecalculateResources()
    {
        CharacterStats totalStats = GetTotalStats();

        // Diablo 3 스타일: 활력 1 = 체력 10
        _maxHealth = 100f + (totalStats.Vitality * 10f) + totalStats.MaxHealth;
        _maxMana = 100f + totalStats.MaxMana;

        // 현재 값이 최대를 초과하지 않도록
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        _currentMana = Mathf.Min(_currentMana, _maxMana);
    }

    /// <summary>
    /// 체력/마나 재생
    /// </summary>
    private void HandleRegeneration()
    {
        if (!IsAlive)
            return;

        CharacterStats totalStats = GetTotalStats();

        // 체력 재생
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

        // 마나 재생
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

    /// <summary>
    /// 스킬 언락 처리
    /// ExperienceManager에서 레벨업 시 호출
    /// </summary>
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

    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive)
            return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 마나 소비 시도
    /// 
    /// SkillActivationSystem에서 호출
    /// 
    /// Returns:
    ///     true: 마나 충분, 소비 성공
    ///     false: 마나 부족, 소비 실패
    /// </summary>
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

    /// <summary>
    /// 체력 회복
    /// SkillActivationSystem의 Heal 스킬에서 호출
    /// </summary>
    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    /// <summary>
    /// 마나 회복
    /// </summary>
    public void RestoreMana(float amount)
    {
        _currentMana = Mathf.Min(_currentMana + amount, _maxMana);
        OnManaChanged?.Invoke(_currentMana, _maxMana);
    }

    private void Die()
    {
        Debug.Log("플레이어 사망!");
        // TODO: 사망 처리, 게임오버 UI
    }

    private void UpdateUI()
    {
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        OnManaChanged?.Invoke(_currentMana, _maxMana);
        OnStatsChanged?.Invoke(GetTotalStats());
    }

    private void CopyStats(CharacterStats source, CharacterStats destination)
    {
        if (source == null || destination == null)
            return;

        // Reflection 또는 CharacterStats에 Copy 메서드 필요
        // 임시로 AddStats 사용
        var emptyStats = ScriptableObject.CreateInstance<CharacterStats>();
        destination = source.AddStats(emptyStats);
    }

    #region Cloud Save 통합

    /// <summary>
    /// CharacterSaveData에서 데이터 로드
    /// CharacterDataManager에서 호출
    /// </summary>
    public void LoadFromSaveData(CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("로드할 데이터가 null입니다!");
            return;
        }

        // 1. 체력/마나 복원
        _currentHealth = data.stats.currentHealth;
        _currentMana = data.stats.currentMana;

        // 2. 기본 스탯 복원 (임시: _baseStats 사용)
        // TODO: SaveData에서 실제 스탯 값 복원

        // 3. 스킬 언락 복원
        _unlockedSkills.Clear();
        foreach (string skillName in data.progression.unlockedSkills)
        {
            SkillData skill = System.Array.Find(_availableSkills, s => s.SkillName == skillName);
            if (skill != null)
            {
                _unlockedSkills.Add(skill);
            }
        }

        // 4. 최대값 재계산
        RecalculateResources();

        // 5. UI 업데이트
        UpdateUI();

        Debug.Log($"플레이어 데이터 로드 완료: HP {_currentHealth}/{_maxHealth}, Mana {_currentMana}/{_maxMana}");
    }

    /// <summary>
    /// 현재 상태를 CharacterSaveData에 저장
    /// CharacterDataManager에서 호출
    /// </summary>
    public void SaveToData(ref CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("저장할 데이터가 null입니다!");
            return;
        }

        // 1. 체력/마나
        data.stats.currentHealth = _currentHealth;
        data.stats.currentMana = _currentMana;

        // 2. 기본 스탯
        CharacterStats totalStats = GetTotalStats();
        data.stats.baseStats.strength = totalStats.Strength;
        data.stats.baseStats.dexterity = totalStats.Dexterity;
        data.stats.baseStats.intelligence = totalStats.Intelligence;
        data.stats.baseStats.vitality = totalStats.Vitality;

        // 3. 2차 스탯
        data.stats.secondaryStats.criticalChance = totalStats.CriticalChance;
        data.stats.secondaryStats.criticalDamage = totalStats.CriticalDamage;
        data.stats.secondaryStats.attackSpeed = totalStats.AttackSpeed;

        // 4. 언락된 스킬
        data.progression.unlockedSkills.Clear();
        foreach (SkillData skill in _unlockedSkills)
        {
            data.progression.unlockedSkills.Add(skill.SkillName);
        }
    }

    #endregion
}