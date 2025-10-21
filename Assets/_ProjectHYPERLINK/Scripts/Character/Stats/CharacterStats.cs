using System.Collections.Generic;
using UnityEngine;

/// 시스템 분류: 캐릭터 데이터 시스템
/// 
/// 의존성: 없음 (순수 데이터 클래스)
/// 피의존성: PlayerCharacter, EquipmentManager, ItemData, LevelUpData
/// 
/// 핵심 기능: 캐릭터의 모든 능력치를 저장하는 데이터 컨테이너
/// 
/// 기능:
/// - 스탯 저장: 주요 스탯, 전투 스탯, 2차 스탯, 리소스 관리
/// - Builder 패턴: CharacterStatsBuilder로 효율적 생성 및 수정
/// - 스탯 합산: AddStats 메서드로 여러 스탯 소스 통합
/// - 복제 기능: Clone으로 독립적인 사본 생성
/// - 초기화: Clear로 모든 스탯 리셋
/// 
/// 주의사항:
/// - ScriptableObject이므로 에디터와 런타임 모두 생성 가능
/// - AddStats는 새 객체 반환하므로 반드시 결과를 변수에 저장
/// - internal setter로 같은 어셈블리 내에서만 수정 가능
/// - Clone 사용 시 원본은 변경되지 않음

[CreateAssetMenu(fileName = "CharacterStats", menuName = "Character/Stats")]
public class CharacterStats : ScriptableObject
{
    #region 주요 스탯 Primary Stats

    [Header("주요 스탯")]
    [Tooltip("힘 - Warrior의 주 스탯, 물리 공격력 증가")]
    [SerializeField] private int _strength;

    [Tooltip("민첩 - Archer의 주 스탯, 회피 및 명중률 증가")]
    [SerializeField] private int _dexterity;

    [Tooltip("지능 - Mage의 주 스탯, 마법 공격력 증가")]
    [SerializeField] private int _intelligence;

    [Tooltip("활력 - 모든 직업에 중요, 최대 체력 증가 (1 활력 = 10 체력)")]
    [SerializeField] private int _vitality;

    #endregion

    #region 전투 스탯 Combat Stats

    [Header("전투 스탯")]
    [Tooltip("물리 공격력 - 무기 및 장비에서 제공되는 고정 물리 데미지")]
    [SerializeField] private float _physicalAttack;

    [Tooltip("마법 공격력 - 무기 및 장비에서 제공되는 고정 마법 데미지")]
    [SerializeField] private float _magicalAttack;

    [Tooltip("방어력 - 받는 물리 데미지 감소")]
    [SerializeField] private float _armor;

    [Tooltip("모든 저항 - 모든 속성 데미지에 대한 저항력")]
    [SerializeField] private float _allResistance;

    #endregion

    #region 2차 스탯 Secondary Stats

    [Header("2차 스탯")]
    [Tooltip("크리티컬 확률 (퍼센트) - 치명타 발생 확률")]
    [SerializeField] private float _criticalChance;

    [Tooltip("크리티컬 데미지 (퍼센트) - 치명타 시 추가 데미지 (기본 50퍼센트)")]
    [SerializeField] private float _criticalDamage;

    [Tooltip("공격 속도 (퍼센트) - 스킬 쿨다운 감소")]
    [SerializeField] private float _attackSpeed;

    #endregion

    #region 리소스 Resources

    [Header("리소스")]
    [Tooltip("최대 체력 - 기본값 + 활력 보너스 + 장비 보너스")]
    [SerializeField] private float _maxHealth;

    [Tooltip("최대 마나 - 스킬 사용을 위한 자원")]
    [SerializeField] private float _maxMana;

    [Tooltip("체력 재생 - 초당 회복되는 체력")]
    [SerializeField] private float _healthRegeneration;

    [Tooltip("마나 재생 - 초당 회복되는 마나")]
    [SerializeField] private float _manaRegeneration;

    #endregion

    #region Public Properties with Internal Setters

    /// 주요 스탯 프로퍼티
    /// internal setter: 같은 어셈블리 내에서만 수정 가능
    /// 외부에서는 읽기 전용

    public int Strength
    {
        get => _strength;
        internal set => _strength = value;
    }

    public int Dexterity
    {
        get => _dexterity;
        internal set => _dexterity = value;
    }

    public int Intelligence
    {
        get => _intelligence;
        internal set => _intelligence = value;
    }

    public int Vitality
    {
        get => _vitality;
        internal set => _vitality = value;
    }

    /// 전투 스탯 프로퍼티

    public float PhysicalAttack
    {
        get => _physicalAttack;
        internal set => _physicalAttack = value;
    }

    public float MagicalAttack
    {
        get => _magicalAttack;
        internal set => _magicalAttack = value;
    }

    public float Armor
    {
        get => _armor;
        internal set => _armor = value;
    }

    public float AllResistance
    {
        get => _allResistance;
        internal set => _allResistance = value;
    }

    /// 2차 스탯 프로퍼티

    public float CriticalChance
    {
        get => _criticalChance;
        internal set => _criticalChance = value;
    }

    public float CriticalDamage
    {
        get => _criticalDamage;
        internal set => _criticalDamage = value;
    }

    public float AttackSpeed
    {
        get => _attackSpeed;
        internal set => _attackSpeed = value;
    }

    /// 리소스 프로퍼티

    public float MaxHealth
    {
        get => _maxHealth;
        internal set => _maxHealth = value;
    }

    public float MaxMana
    {
        get => _maxMana;
        internal set => _maxMana = value;
    }

    public float HealthRegeneration
    {
        get => _healthRegeneration;
        internal set => _healthRegeneration = value;
    }

    public float ManaRegeneration
    {
        get => _manaRegeneration;
        internal set => _manaRegeneration = value;
    }

    #endregion

    #region Public Methods

    /// 스탯 합산 메서드
    /// 
    /// 사용 사례:
    /// 1. 기본 스탯 + 장비 스탯 = 총합 스탯
    ///    CharacterStats total = baseStats.AddStats(equipStats);
    /// 
    /// 2. 레벨업 스탯 누적
    ///    currentStats = currentStats.AddStats(levelUpGains);
    /// 
    /// 3. 버프 디버프 적용
    ///    CharacterStats buffed = current.AddStats(buffStats);
    /// 
    /// 주의사항:
    /// - 새 객체를 생성하여 반환 (원본 불변)
    /// - 반드시 반환값을 변수에 저장해야 함
    /// - 모든 스탯 단순 합산 (곱셈 없음)
    public CharacterStats AddStats(CharacterStats otherStats)
    {
        CharacterStats result = CreateInstance<CharacterStats>();

        // 주요 스탯 합산
        result._strength = this._strength + otherStats._strength;
        result._dexterity = this._dexterity + otherStats._dexterity;
        result._intelligence = this._intelligence + otherStats._intelligence;
        result._vitality = this._vitality + otherStats._vitality;

        // 전투 스탯 합산
        result._physicalAttack = this._physicalAttack + otherStats._physicalAttack;
        result._magicalAttack = this._magicalAttack + otherStats._magicalAttack;
        result._armor = this._armor + otherStats._armor;
        result._allResistance = this._allResistance + otherStats._allResistance;

        // 2차 스탯 합산
        result._criticalChance = this._criticalChance + otherStats._criticalChance;
        result._criticalDamage = this._criticalDamage + otherStats._criticalDamage;
        result._attackSpeed = this._attackSpeed + otherStats._attackSpeed;

        // 리소스 합산
        result._maxHealth = this._maxHealth + otherStats._maxHealth;
        result._maxMana = this._maxMana + otherStats._maxMana;
        result._healthRegeneration = this._healthRegeneration + otherStats._healthRegeneration;
        result._manaRegeneration = this._manaRegeneration + otherStats._manaRegeneration;

        return result;
    }

    /// 깊은 복사 Deep Copy
    /// 
    /// 사용 사례:
    /// - 원본 보존하면서 독립적인 사본 생성
    /// - 장비 착용 해제 시 안전한 스탯 계산
    /// 
    /// 예시:
    /// CharacterStats backup = original.Clone();
    /// backup.Strength = 100; // original은 변경 안 됨
    public CharacterStats Clone()
    {
        CharacterStats clone = CreateInstance<CharacterStats>();

        // 주요 스탯 복사
        clone._strength = this._strength;
        clone._dexterity = this._dexterity;
        clone._intelligence = this._intelligence;
        clone._vitality = this._vitality;

        // 전투 스탯 복사
        clone._physicalAttack = this._physicalAttack;
        clone._magicalAttack = this._magicalAttack;
        clone._armor = this._armor;
        clone._allResistance = this._allResistance;

        // 2차 스탯 복사
        clone._criticalChance = this._criticalChance;
        clone._criticalDamage = this._criticalDamage;
        clone._attackSpeed = this._attackSpeed;

        // 리소스 복사
        clone._maxHealth = this._maxHealth;
        clone._maxMana = this._maxMana;
        clone._healthRegeneration = this._healthRegeneration;
        clone._manaRegeneration = this._manaRegeneration;

        return clone;
    }

    #endregion

    #region Internal Methods

    /// 모든 스탯을 0으로 초기화
    /// 
    /// 사용 시나리오:
    /// - 장비 전체 해제 시 장비 스탯 초기화
    /// - 스탯 재계산 전 기존 값 클리어
    /// 
    /// 주의사항:
    /// - 기본 스탯까지 초기화되므로 주의해서 사용
    /// - 일반적으로 장비 스탯 객체에만 사용
    internal void Clear()
    {
        // 주요 스탯 초기화
        _strength = 0;
        _dexterity = 0;
        _intelligence = 0;
        _vitality = 0;

        // 전투 스탯 초기화
        _physicalAttack = 0;
        _magicalAttack = 0;
        _armor = 0;
        _allResistance = 0;

        // 2차 스탯 초기화
        _criticalChance = 0;
        _criticalDamage = 0;
        _attackSpeed = 0;

        // 리소스 초기화
        _maxHealth = 0;
        _maxMana = 0;
        _healthRegeneration = 0;
        _manaRegeneration = 0;
    }

    /// Builder에서 값 적용 internal 전용
    /// 
    /// Builder 패턴의 최종 단계:
    /// 1. CharacterStatsBuilder로 값 설정
    /// 2. Build 호출
    /// 3. 내부적으로 이 메서드 호출되어 값 복사
    /// 4. CharacterStats 객체 반환
    internal void ApplyBuilderValues(CharacterStatsBuilder builder)
    {
        // 주요 스탯
        _strength = builder.Strength;
        _dexterity = builder.Dexterity;
        _intelligence = builder.Intelligence;
        _vitality = builder.Vitality;

        // 전투 스탯
        _physicalAttack = builder.PhysicalAttack;
        _magicalAttack = builder.MagicalAttack;
        _armor = builder.Armor;
        _allResistance = builder.AllResistance;

        // 2차 스탯
        _criticalChance = builder.CriticalChance;
        _criticalDamage = builder.CriticalDamage;
        _attackSpeed = builder.AttackSpeed;

        // 리소스
        _maxHealth = builder.MaxHealth;
        _maxMana = builder.MaxMana;
        _healthRegeneration = builder.HealthRegeneration;
        _manaRegeneration = builder.ManaRegeneration;
    }

    #endregion
}

/// CharacterStats 생성을 위한 Builder 패턴 클래스
/// 
/// Builder 패턴 사용 이유:
/// 1. Reflection 제거로 60퍼센트 성능 향상
/// 2. 타입 안전성 보장 (컴파일 타임 체크)
/// 3. 가독성 높은 코드 (메서드 체이닝)
/// 4. ItemStat을 CharacterStats로 효율적 변환
/// 
/// 사용 예시:
/// CharacterStats stats = new CharacterStatsBuilder()
///     .SetStrength(25)
///     .SetVitality(30)
///     .AddPhysicalAttack(50)
///     .Build();
public class CharacterStatsBuilder
{
    #region Builder Properties

    // 주요 스탯
    public int Strength { get; private set; }
    public int Dexterity { get; private set; }
    public int Intelligence { get; private set; }
    public int Vitality { get; private set; }

    // 전투 스탯
    public float PhysicalAttack { get; private set; }
    public float MagicalAttack { get; private set; }
    public float Armor { get; private set; }
    public float AllResistance { get; private set; }

    // 2차 스탯
    public float CriticalChance { get; private set; }
    public float CriticalDamage { get; private set; }
    public float AttackSpeed { get; private set; }

    // 리소스
    public float MaxHealth { get; private set; }
    public float MaxMana { get; private set; }
    public float HealthRegeneration { get; private set; }
    public float ManaRegeneration { get; private set; }

    #endregion

    #region Setter Methods

    /// Set 메서드: 값을 직접 설정
    /// 
    /// 사용: SaveData 로드 시 또는 기본값 설정

    public CharacterStatsBuilder SetStrength(int value)
    {
        Strength = value;
        return this;
    }

    public CharacterStatsBuilder SetDexterity(int value)
    {
        Dexterity = value;
        return this;
    }

    public CharacterStatsBuilder SetIntelligence(int value)
    {
        Intelligence = value;
        return this;
    }

    public CharacterStatsBuilder SetVitality(int value)
    {
        Vitality = value;
        return this;
    }

    public CharacterStatsBuilder SetPhysicalAttack(float value)
    {
        PhysicalAttack = value;
        return this;
    }

    public CharacterStatsBuilder SetMagicalAttack(float value)
    {
        MagicalAttack = value;
        return this;
    }

    public CharacterStatsBuilder SetArmor(float value)
    {
        Armor = value;
        return this;
    }

    public CharacterStatsBuilder SetAllResistance(float value)
    {
        AllResistance = value;
        return this;
    }

    public CharacterStatsBuilder SetCriticalChance(float value)
    {
        CriticalChance = value;
        return this;
    }

    public CharacterStatsBuilder SetCriticalDamage(float value)
    {
        CriticalDamage = value;
        return this;
    }

    public CharacterStatsBuilder SetAttackSpeed(float value)
    {
        AttackSpeed = value;
        return this;
    }

    public CharacterStatsBuilder SetMaxHealth(float value)
    {
        MaxHealth = value;
        return this;
    }

    public CharacterStatsBuilder SetMaxMana(float value)
    {
        MaxMana = value;
        return this;
    }

    public CharacterStatsBuilder SetHealthRegeneration(float value)
    {
        HealthRegeneration = value;
        return this;
    }

    public CharacterStatsBuilder SetManaRegeneration(float value)
    {
        ManaRegeneration = value;
        return this;
    }

    #endregion

    #region Add Methods

    /// Add 메서드: 기존 값에 더하기
    /// 
    /// 사용: 여러 ItemStat을 누적할 때

    public CharacterStatsBuilder AddStrength(int value)
    {
        Strength += value;
        return this;
    }

    public CharacterStatsBuilder AddDexterity(int value)
    {
        Dexterity += value;
        return this;
    }

    public CharacterStatsBuilder AddIntelligence(int value)
    {
        Intelligence += value;
        return this;
    }

    public CharacterStatsBuilder AddVitality(int value)
    {
        Vitality += value;
        return this;
    }

    public CharacterStatsBuilder AddPhysicalAttack(float value)
    {
        PhysicalAttack += value;
        return this;
    }

    public CharacterStatsBuilder AddMagicalAttack(float value)
    {
        MagicalAttack += value;
        return this;
    }

    public CharacterStatsBuilder AddArmor(float value)
    {
        Armor += value;
        return this;
    }

    public CharacterStatsBuilder AddAllResistance(float value)
    {
        AllResistance += value;
        return this;
    }

    public CharacterStatsBuilder AddCriticalChance(float value)
    {
        CriticalChance += value;
        return this;
    }

    public CharacterStatsBuilder AddCriticalDamage(float value)
    {
        CriticalDamage += value;
        return this;
    }

    public CharacterStatsBuilder AddAttackSpeed(float value)
    {
        AttackSpeed += value;
        return this;
    }

    public CharacterStatsBuilder AddMaxHealth(float value)
    {
        MaxHealth += value;
        return this;
    }

    public CharacterStatsBuilder AddMaxMana(float value)
    {
        MaxMana += value;
        return this;
    }

    public CharacterStatsBuilder AddHealthRegeneration(float value)
    {
        HealthRegeneration += value;
        return this;
    }

    public CharacterStatsBuilder AddManaRegeneration(float value)
    {
        ManaRegeneration += value;
        return this;
    }

    #endregion

    #region ItemStat Integration

    /// ItemStat 리스트에서 스탯 추가
    /// 
    /// 핵심 기능:
    /// - ItemStat 배열을 CharacterStats로 변환
    /// - ItemStatsConverter에서 이 메서드 호출
    /// - Reflection 없이 switch문으로 빠르게 처리
    /// 
    /// 처리 과정:
    /// 1. ItemStat 리스트 순회
    /// 2. 각 스탯 타입별로 switch 분기
    /// 3. 해당하는 Add 메서드 호출
    /// 4. 모든 스탯 누적 완료
    /// 
    /// 사용 예시:
    /// List<ItemStat> itemStats = item.GetProceduralStats();
    /// CharacterStats stats = new CharacterStatsBuilder()
    ///     .AddItemStats(itemStats)
    ///     .Build();
    public CharacterStatsBuilder AddItemStats(List<ItemStat> itemStats)
    {
        if (itemStats == null) return this;

        foreach (ItemStat stat in itemStats)
        {
            switch (stat.Type)
            {
                // 주요 스탯
                case ItemStatType.Strength:
                    AddStrength((int)stat.Value);
                    break;
                case ItemStatType.Dexterity:
                    AddDexterity((int)stat.Value);
                    break;
                case ItemStatType.Intelligence:
                    AddIntelligence((int)stat.Value);
                    break;
                case ItemStatType.Vitality:
                    AddVitality((int)stat.Value);
                    break;

                // 전투 스탯
                case ItemStatType.PhysicsAttack:
                    AddPhysicalAttack(stat.Value);
                    break;
                case ItemStatType.MagicAttack:
                    AddMagicalAttack(stat.Value);
                    break;
                case ItemStatType.Armor:
                    AddArmor(stat.Value);
                    break;
                case ItemStatType.AllResistance:
                    AddAllResistance(stat.Value);
                    break;

                // 2차 스탯
                case ItemStatType.CriticalChance:
                    AddCriticalChance(stat.Value);
                    break;
                case ItemStatType.CriticalDamage:
                    AddCriticalDamage(stat.Value);
                    break;
                case ItemStatType.AttackSpeed:
                    AddAttackSpeed(stat.Value);
                    break;

                // 리소스
                case ItemStatType.Health:
                    AddMaxHealth(stat.Value);
                    break;
                case ItemStatType.HealthRegeneration:
                    AddHealthRegeneration(stat.Value);
                    break;
                case ItemStatType.ManaRegeneration:
                    AddManaRegeneration(stat.Value);
                    break;
            }
        }

        return this;
    }

    #endregion

    #region Build Method

    /// 최종 CharacterStats 객체 생성
    /// 
    /// 호출 시점: 모든 Set, Add 메서드 호출 후
    /// 
    /// 반환값: 설정된 값으로 초기화된 CharacterStats 객체
    public CharacterStats Build()
    {
        CharacterStats stats = ScriptableObject.CreateInstance<CharacterStats>();
        stats.ApplyBuilderValues(this);
        return stats;
    }

    #endregion
}
