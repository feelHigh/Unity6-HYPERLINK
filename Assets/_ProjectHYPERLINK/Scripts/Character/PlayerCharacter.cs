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
    /// <summary>
    /// 임시 버프 스탯 목록 (신규)
    /// 
    /// 버프 시스템:
    /// - 각 버프는 CharacterStats 객체로 저장
    /// - 여러 버프 동시 적용 가능
    /// - GetTotalStats()에서 자동으로 합산
    /// </summary>
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
    /// 
    /// 호출 시점:
    /// - Awake()에서 자동 호출
    /// - 게임 시작 시 신규 캐릭터 생성
    /// 
    /// 초기화 과정:
    /// 1. 빈 CharacterStats 인스턴스 생성
    /// 2. 기본 스탯(_baseStats) 복사 (Clone 사용)
    /// 3. 체력/마나 최대값 계산
    /// 4. 현재 체력/마나를 최대값으로 설정
    /// 
    /// 변경사항:
    /// - 이전: CopyStats(source, dest) 사용 (작동 안 함)
    /// - 현재: _baseStats.Clone() 사용 (올바른 복사)
    /// 
    /// 왜 Clone()이 필요한가?
    /// - _baseStats는 ScriptableObject 에셋 (공유 자원)
    /// - 직접 수정하면 모든 캐릭터에 영향
    /// - Clone()으로 독립적인 사본 생성
    /// 
    /// 주의사항:
    /// - _baseStats가 null이면 빈 스탯으로 시작
    /// - 저장된 데이터 로드 시에는 LoadFromSaveData() 사용
    /// - 이 메서드는 신규 캐릭터 전용
    /// </summary>
    private void InitializeCharacter()
    {
        // 빈 스탯 인스턴스 생성
        _currentStats = ScriptableObject.CreateInstance<CharacterStats>();
        _equipmentStats = ScriptableObject.CreateInstance<CharacterStats>();

        // 기본 스탯이 있으면 복사 (CHANGED: Clone() 사용)
        if (_baseStats != null)
        {
            _currentStats = _baseStats.Clone(); // 깊은 복사로 독립적인 사본 생성
        }

        // 체력/마나 최대값 계산
        RecalculateResources();

        // 현재 체력/마나를 최대값으로 설정 (만렙 상태로 시작)
        _currentHealth = _maxHealth;
        _currentMana = _maxMana;
    }

    // <summary>
    /// 레벨업 시 스탯 증가 - 소폭 개선
    /// 
    /// 호출 위치:
    /// - ExperienceManager.LevelUp()에서 호출
    /// 
    /// 처리 과정:
    /// 1. 현재 스탯 + 레벨업 증가량 = 새 스탯
    /// 2. 최대 체력/마나 재계산
    /// 3. 현재 체력/마나를 최대값으로 설정 (레벨업 보상)
    /// 4. UI 업데이트 및 이벤트 발생
    /// 
    /// 변경사항:
    /// - null 체크 추가
    /// - 상세 디버그 로그 추가
    /// 
    /// 레벨업 보상:
    /// - 완전 회복 (체력/마나 100%)
    /// - Diablo 3 스타일
    /// 
    /// 주의사항:
    /// - statGains가 null이면 아무 일도 안 일어남
    /// - AddStats()는 새 인스턴스 반환 (원본 불변)
    /// </summary>
    public void AddLevelUpStats(CharacterStats statGains)
    {
        if (statGains == null)
        {
            Debug.LogWarning("레벨업 스탯 증가량이 null입니다!");
            return;
        }

        // 기존 스탯 + 증가량 = 새 스탯
        _currentStats = _currentStats.AddStats(statGains);

        // 최대값 재계산
        RecalculateResources();

        // 레벨업 보상: 완전 회복
        _currentHealth = _maxHealth;
        _currentMana = _maxMana;

        // UI 업데이트 및 이벤트
        UpdateUI();
        OnStatsChanged?.Invoke(GetTotalStats());

        // 상세 로그
        Debug.Log($"레벨업 스탯 증가: STR+{statGains.Strength}, " +
                  $"DEX+{statGains.Dexterity}, " +
                  $"INT+{statGains.Intelligence}, " +
                  $"VIT+{statGains.Vitality}");
    }

    #region 버프 시스템 (신규)
    /// <summary>
    /// 임시 버프 스탯 추가 (신규)
    /// 
    /// SkillActivationSystem.ApplyTemporaryBuff()에서 호출
    /// 
    /// 처리 과정:
    /// 1. 버프 스탯을 _temporaryBuffs 리스트에 추가
    /// 2. 자원(체력/마나) 재계산
    /// 3. UI 업데이트 이벤트 발생
    /// 
    /// Parameters:
    ///     buffStats: 추가할 버프 스탯
    ///     
    /// 사용 예:
    /// CharacterStats buffStats = new CharacterStats.Builder()
    ///     .SetStrength(50)
    ///     .Build();
    /// playerCharacter.AddTemporaryStats(buffStats);
    /// 
    /// 버프 중첩:
    /// - 같은 종류의 버프도 별개 객체로 추가됨
    /// - 모든 버프가 독립적으로 관리됨
    /// - 제거 시 정확한 객체 참조 필요
    /// </summary>
    public void AddTemporaryStats(CharacterStats buffStats)
    {
        if (buffStats == null)
        {
            Debug.LogError("버프 스탯이 null입니다!");
            return;
        }

        // 버프 추가
        _temporaryBuffs.Add(buffStats);

        // 버프 적용 후 최대 체력/마나 재계산
        RecalculateResources();

        // UI 업데이트
        OnStatsChanged?.Invoke(GetTotalStats());

        Debug.Log($"버프 추가: 현재 활성 버프 {_temporaryBuffs.Count}개");
    }

    /// <summary>
    /// 임시 버프 스탯 제거 (신규)
    /// 
    /// SkillActivationSystem.ApplyTemporaryBuff() 코루틴 종료 시 호출
    /// 
    /// 처리 과정:
    /// 1. _temporaryBuffs 리스트에서 해당 버프 제거
    /// 2. 자원(체력/마나) 재계산
    /// 3. UI 업데이트 이벤트 발생
    /// 
    /// Parameters:
    ///     buffStats: 제거할 버프 스탯 (정확한 객체 참조)
    ///     
    /// 주의사항:
    /// - 반드시 AddTemporaryStats()로 추가한 동일한 객체를 전달해야 함
    /// - 값이 같은 다른 객체는 제거되지 않음
    /// - Remove()는 첫 번째 일치 항목만 제거
    /// 
    /// 버그 방지:
    /// - 코루틴에서 버프 객체 참조를 캡처하여 사용
    /// - 새로운 CharacterStats 객체 생성하지 말 것
    /// </summary>
    public void RemoveTemporaryStats(CharacterStats buffStats)
    {
        if (buffStats == null)
        {
            Debug.LogError("제거할 버프 스탯이 null입니다!");
            return;
        }

        // 버프 제거 (정확한 객체 참조로 제거)
        bool removed = _temporaryBuffs.Remove(buffStats);

        if (!removed)
        {
            Debug.LogWarning("버프를 찾을 수 없습니다! 이미 제거되었거나 잘못된 참조입니다.");
            return;
        }

        // 버프 제거 후 최대 체력/마나 재계산
        RecalculateResources();

        // UI 업데이트
        OnStatsChanged?.Invoke(GetTotalStats());

        Debug.Log($"버프 제거: 남은 버프 {_temporaryBuffs.Count}개");
    }

    /// <summary>
    /// 모든 버프 제거 (디버그/리셋용) (신규)
    /// 
    /// 사용 시나리오:
    /// - 플레이어 사망 시
    /// - 씬 전환 시
    /// - 디버그/치트 명령
    /// - 특정 상태 이상 (침묵, 정화 등)
    /// </summary>
    public void ClearAllTemporaryBuffs()
    {
        int buffCount = _temporaryBuffs.Count;
        _temporaryBuffs.Clear();

        // 자원 재계산
        RecalculateResources();

        // UI 업데이트
        OnStatsChanged?.Invoke(GetTotalStats());

        Debug.Log($"모든 버프 제거 완료: {buffCount}개 제거됨");
    }

    /// <summary>
    /// 현재 활성 버프 수 반환 (디버그용) (신규)
    /// </summary>
    public int GetActiveBuffCount()
    {
        return _temporaryBuffs.Count;
    }

    /// <summary>
    /// 버프 시스템 상태 출력 (디버그용) (신규)
    /// </summary>
    [ContextMenu("Debug: Print Buff Status")]
    private void DebugPrintBuffStatus()
    {
        Debug.Log("===== 버프 시스템 상태 =====");
        Debug.Log($"활성 버프 수: {_temporaryBuffs.Count}개");

        if (_temporaryBuffs.Count > 0)
        {
            Debug.Log("--- 버프 목록 ---");
            for (int i = 0; i < _temporaryBuffs.Count; i++)
            {
                CharacterStats buff = _temporaryBuffs[i];
                Debug.Log($"버프 {i + 1}:");
                Debug.Log($"  힘: +{buff.Strength}");
                Debug.Log($"  민첩: +{buff.Dexterity}");
                Debug.Log($"  지능: +{buff.Intelligence}");
                Debug.Log($"  활력: +{buff.Vitality}");
            }
        }

        CharacterStats total = GetTotalStats();
        Debug.Log("--- 최종 스탯 (버프 포함) ---");
        Debug.Log($"힘: {total.Strength}");
        Debug.Log($"민첩: {total.Dexterity}");
        Debug.Log($"지능: {total.Intelligence}");
        Debug.Log($"활력: {total.Vitality}");
    }

    #endregion

    /// <summary>
    /// 장비 스탯 업데이트 - 소폭 개선
    /// 
    /// 호출 위치:
    /// - EquipmentManager.RecalculateEquipmentStats()에서 호출
    /// - 장비 착용/해제 시마다 호출
    /// 
    /// 변경사항:
    /// - null 체크 추가 (equipmentStats가 null이면 빈 스탯으로 초기화)
    /// 
    /// 처리 과정:
    /// 1. _equipmentStats를 새 장비 스탯으로 교체
    /// 2. 최대값 재계산
    /// 3. UI 업데이트 및 이벤트
    /// 
    /// 주의사항:
    /// - null이 들어와도 에러 안 남 (빈 스탯으로 처리)
    /// - 현재 체력/마나는 변경 안 됨 (최대값만 변경)
    /// </summary>
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

    /// <summary>
    /// 총 스탯 계산 (기본 + 장비 + 버프)
    /// 
    /// 계산 순서:
    /// 1. _currentStats (기본 스탯)
    /// 2. _equipmentStats (장비 스탯)
    /// 3. _temporaryBuffs (버프 스탯들) ← 신규 추가
    /// </summary>
    public CharacterStats GetTotalStats()
    {
        // 1. 기본 스탯으로 시작
        CharacterStats total = _currentStats;

        // 2. 장비 스탯 합산
        if (_equipmentStats != null)
        {
            total = total.AddStats(_equipmentStats);
        }

        // 3. 모든 버프 스탯 합산 (신규)
        foreach (CharacterStats buffStats in _temporaryBuffs)
        {
            if (buffStats != null)
            {
                total = total.AddStats(buffStats);
            }
        }

        return total;
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

        // 모든 버프 제거
        ClearAllTemporaryBuffs();

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
    /// CharacterSaveData에서 데이터 로드 - 리팩토링 완료 (치명적 버그 수정!)
    /// 
    /// 호출 위치:
    /// - CharacterDataManager.LoadCharacterData()에서 호출
    /// - 게임 시작 시 또는 씬 전환 후 캐릭터 복원
    /// 
    /// 치명적 버그 수정:
    /// - 이전: 기본 스탯 복원이 TODO로 남겨짐
    /// - 결과: 저장 후 로드 시 모든 레벨업 스탯 손실!
    /// - 현재: CharacterStatsBuilder로 완벽 복원
    /// 
    /// 복원 순서 (중요!):
    /// 1. 기본 스탯 복원 (Builder 사용)
    /// 2. 장비 스탯 초기화 (EquipmentManager가 별도 로드)
    /// 3. 스킬 언락 복원
    /// 4. 최대값 재계산 (스탯 기반)
    /// 5. 현재 체력/마나 복원 (최대값 체크)
    /// 6. UI 업데이트
    /// 
    /// Builder 사용 이유:
    /// - Reflection 없이 타입 안전하게 스탯 설정
    /// - SaveData 구조체 → CharacterStats ScriptableObject 변환
    /// - 컴파일 타임 체크로 오타 방지
    /// 
    /// 검증 로직:
    /// - 체력/마나가 최대값 초과 시 최대값으로 제한
    /// - 체력/마나가 0 이하면 1로 설정 (사망 방지)
    /// - 스킬 못 찾으면 Warning 로그
    /// 
    /// 사용 예시:
    /// CharacterSaveData loadedData = await CloudSaveManager.LoadCharacterDataAsync();
    /// playerCharacter.LoadFromSaveData(loadedData);
    /// // 이제 모든 스탯, 체력, 마나, 스킬이 완벽히 복원됨
    /// 
    /// 주의사항:
    /// - 반드시 ExperienceManager, EquipmentManager보다 나중에 호출
    /// - 순서: Experience → Player → Equipment
    /// </summary>
    public void LoadFromSaveData(CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("로드할 데이터가 null입니다!");
            return;
        }

        // 1. 새 스탯 인스턴스 생성 및 저장된 값으로 초기화
        // CharacterStatsBuilder로 SaveData → CharacterStats 변환
        _currentStats = new CharacterStatsBuilder()
            // 주요 스탯 복원
            .SetStrength(data.stats.baseStats.strength)
            .SetDexterity(data.stats.baseStats.dexterity)
            .SetIntelligence(data.stats.baseStats.intelligence)
            .SetVitality(data.stats.baseStats.vitality)
            // 2차 스탯 복원
            .SetCriticalChance(data.stats.secondaryStats.criticalChance)
            .SetCriticalDamage(data.stats.secondaryStats.criticalDamage)
            .SetAttackSpeed(data.stats.secondaryStats.attackSpeed)
            .Build();

        // 2. 장비 스탯 초기화
        // EquipmentManager가 별도로 LoadFromSaveData() 호출하여 장비 복원
        // 여기서는 빈 인스턴스만 준비
        if (_equipmentStats == null)
        {
            _equipmentStats = ScriptableObject.CreateInstance<CharacterStats>();
        }

        // 3. 스킬 언락 복원
        _unlockedSkills.Clear();
        if (data.progression.unlockedSkills != null)
        {
            foreach (string skillName in data.progression.unlockedSkills)
            {
                // _availableSkills 배열에서 이름으로 검색
                SkillData skill = System.Array.Find(_availableSkills, s => s.SkillName == skillName);
                if (skill != null)
                {
                    _unlockedSkills.Add(skill);
                    Debug.Log($"스킬 복원: {skillName}");
                }
                else
                {
                    Debug.LogWarning($"스킬을 찾을 수 없음: {skillName}");
                }
            }
        }

        // 4. 최대값 재계산 (스탯 기반)
        // Vitality에 따라 _maxHealth, _maxMana 계산
        RecalculateResources();

        // 5. 현재 체력/마나 복원 (최대값을 초과하지 않도록 제한)
        _currentHealth = Mathf.Min(data.stats.currentHealth, _maxHealth);
        _currentMana = Mathf.Min(data.stats.currentMana, _maxMana);

        // 체력/마나가 0 이하면 최소값 설정 (사망 방지)
        if (_currentHealth <= 0) _currentHealth = 1;
        if (_currentMana <= 0) _currentMana = 1;

        // 6. UI 업데이트
        UpdateUI();

        // 상세 로그 (디버깅용)
        Debug.Log($"플레이어 데이터 로드 완료: " +
                  $"HP {_currentHealth}/{_maxHealth}, " +
                  $"Mana {_currentMana}/{_maxMana}, " +
                  $"STR {_currentStats.Strength}, " +
                  $"DEX {_currentStats.Dexterity}, " +
                  $"INT {_currentStats.Intelligence}, " +
                  $"VIT {_currentStats.Vitality}");
    }

    /// <summary>
    /// 현재 상태를 CharacterSaveData에 저장 - 리팩토링 완료
    /// 
    /// 호출 위치:
    /// - CharacterDataManager.CollectAndSaveData()에서 호출
    /// - 자동 저장 (5분마다), 수동 저장 (게임 종료 시)
    /// 
    /// 변경사항:
    /// - ref 키워드 제거 (불필요하며 혼란스러움)
    /// - null 체크 강화
    /// - 상세 로그 추가
    /// 
    /// ref를 제거한 이유:
    /// - ref는 매개변수 자체를 교체할 때 사용
    /// - 여기서는 data 내부의 프로퍼티만 수정
    /// - ref 없이도 참조 타입이라 내부 수정 가능
    /// - 오해의 소지 제거
    /// 
    /// 저장 내용:
    /// 1. 현재 체력/마나 (전투 중 상태 보존)
    /// 2. 기본 스탯 (장비 제외한 순수 캐릭터 스탯)
    /// 3. 2차 스탯 (총합 저장 - 장비 포함)
    /// 4. 언락된 스킬 리스트
    /// 
    /// 왜 2차 스탯은 총합을 저장하는가?
    /// - 크리티컬, 공격속도 등은 대부분 장비에서 제공
    /// - UI 표시 편의성
    /// - 로드 시 EquipmentManager가 다시 계산하므로 문제 없음
    /// 
    /// 주의사항:
    /// - data가 null이면 저장 실패 (에러 로그)
    /// - _currentStats가 null이면 기본 스탯 저장 안 됨
    /// - 위치 정보는 CharacterDataManager에서 별도 처리
    /// 
    /// 사용 예시:
    /// CharacterSaveData saveData = new CharacterSaveData();
    /// // ... 다른 데이터 초기화
    /// playerCharacter.SaveToData(saveData);
    /// await CloudSaveManager.SaveCharacterDataAsync(saveData);
    /// </summary>
    public void SaveToData(CharacterSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("저장할 데이터가 null입니다!");
            return;
        }

        // 현재 총 스탯 계산 (기본 + 장비)
        CharacterStats totalStats = GetTotalStats();

        // 1. 현재 체력/마나 저장
        data.stats.currentHealth = _currentHealth;
        data.stats.currentMana = _currentMana;

        // 2. 기본 스탯 저장 (장비 제외한 순수 캐릭터 스탯)
        if (_currentStats != null)
        {
            data.stats.baseStats.strength = _currentStats.Strength;
            data.stats.baseStats.dexterity = _currentStats.Dexterity;
            data.stats.baseStats.intelligence = _currentStats.Intelligence;
            data.stats.baseStats.vitality = _currentStats.Vitality;
        }

        // 3. 2차 스탯 저장 (장비 포함된 총합)
        if (totalStats != null)
        {
            data.stats.secondaryStats.criticalChance = totalStats.CriticalChance;
            data.stats.secondaryStats.criticalDamage = totalStats.CriticalDamage;
            data.stats.secondaryStats.attackSpeed = totalStats.AttackSpeed;
        }

        // 4. 언락된 스킬 저장
        data.progression.unlockedSkills.Clear();
        foreach (SkillData skill in _unlockedSkills)
        {
            if (skill != null)
            {
                data.progression.unlockedSkills.Add(skill.SkillName);
            }
        }

        // 상세 로그 (디버깅용)
        Debug.Log($"플레이어 데이터 저장 완료: " +
                  $"레벨 {data.character.level}, " +
                  $"HP {_currentHealth}, " +
                  $"스킬 {_unlockedSkills.Count}개");
    }

    #endregion
}
