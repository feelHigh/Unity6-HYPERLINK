using UnityEngine;

/// <summary>
/// 캐릭터 스탯 데이터 ScriptableObject
/// 
/// 역할:
/// - 캐릭터의 모든 스탯 정보를 저장
/// - 불변(Immutable) 데이터 구조
/// - 레벨업/장비 착용 시 새 인스턴스 생성
/// 
/// 스탯 카테고리:
/// 1. Primary Stats (주요 스탯): 힘, 민첩, 지능, 활력
/// 2. Secondary Stats (2차 스탯): 크리티컬, 공격속도
/// 3. Resources (리소스): 체력, 마나, 재생
/// 
/// 사용 패턴:
/// - 에디터에서 생성: Create > Character > Stats
/// - 런타임 생성: ScriptableObject.CreateInstance<CharacterStats>()
/// - 스탯 합산: AddStats() 메서드 사용
/// 
/// Diablo 3 스탯 시스템 기반:
/// - 주요 스탯이 데미지/방어력에 영향
/// - 크리티컬 시스템 (확률 + 데미지)
/// - 리소스 기반 전투 (체력/마나)
/// 
/// 불변성의 장점:
/// - 원본 데이터 보존
/// - 안전한 멀티스레드
/// - 명확한 데이터 흐름
/// 
/// 사용 예시:
/// ```csharp
/// // 기본 스탯
/// CharacterStats baseStats = ...;
/// 
/// // 장비 스탯
/// CharacterStats equipStats = ...;
/// 
/// // 합산 (새 인스턴스 반환)
/// CharacterStats totalStats = baseStats.AddStats(equipStats);
/// ```
/// </summary>
[CreateAssetMenu(fileName = "CharacterStats", menuName = "Character/Stats")]
public class CharacterStats : ScriptableObject
{
    #region 주요 스탯 (Primary Stats)

    [Header("주요 스탯")]
    [SerializeField] private int _strength;        // 힘: 물리 공격력 증가
    [SerializeField] private int _dexterity;       // 민첩: 회피/명중 증가
    [SerializeField] private int _intelligence;    // 지능: 마법 공격력 증가
    [SerializeField] private int _vitality;        // 활력: 최대 체력 증가

    #endregion

    #region 2차 스탯 (Secondary Stats)

    [Header("2차 스탯")]
    [SerializeField] private float _criticalChance;    // 크리티컬 확률 (%)
    [SerializeField] private float _criticalDamage;    // 크리티컬 데미지 (%)
    [SerializeField] private float _attackSpeed;       // 공격 속도 (%)

    #endregion

    #region 리소스 (Resources)

    [Header("리소스")]
    [SerializeField] private float _maxHealth;             // 최대 체력
    [SerializeField] private float _maxMana;               // 최대 마나
    [SerializeField] private float _healthRegeneration;    // 초당 체력 재생
    [SerializeField] private float _manaRegeneration;      // 초당 마나 재생

    #endregion

    #region Public 읽기 전용 프로퍼티

    /// <summary>
    /// 힘 스탯
    /// - Warrior의 주요 스탯
    /// - 물리 공격력 증가 (1 힘 = 1% 데미지 증가)
    /// </summary>
    public int Strength => _strength;

    /// <summary>
    /// 민첩 스탯
    /// - Archer의 주요 스탯
    /// - 회피율 및 명중률에 영향
    /// </summary>
    public int Dexterity => _dexterity;

    /// <summary>
    /// 지능 스탯
    /// - Mage의 주요 스탯
    /// - 마법 공격력 증가 (1 지능 = 1% 데미지 증가)
    /// </summary>
    public int Intelligence => _intelligence;

    /// <summary>
    /// 활력 스탯
    /// - 모든 직업에 중요
    /// - 최대 체력 증가 (1 활력 = 10 체력)
    /// </summary>
    public int Vitality => _vitality;

    /// <summary>
    /// 크리티컬 확률 (%)
    /// - 크리티컬 히트 발생 확률
    /// - 범위: 0~100%
    /// </summary>
    public float CriticalChance => _criticalChance;

    /// <summary>
    /// 크리티컬 데미지 (%)
    /// - 크리티컬 발생 시 추가 데미지
    /// - 예: 150% = 기본 데미지의 2.5배
    /// </summary>
    public float CriticalDamage => _criticalDamage;

    /// <summary>
    /// 공격 속도 (%)
    /// - 공격 쿨다운 감소
    /// - 예: 10% = 쿨다운 10% 감소
    /// </summary>
    public float AttackSpeed => _attackSpeed;

    /// <summary>
    /// 최대 체력
    /// - 기본 체력 + 활력 보너스
    /// </summary>
    public float MaxHealth => _maxHealth;

    /// <summary>
    /// 최대 마나
    /// - 기본 마나 + 주요 스탯 보너스
    /// </summary>
    public float MaxMana => _maxMana;

    /// <summary>
    /// 초당 체력 재생
    /// - 전투 중/비전투 중 모두 적용
    /// </summary>
    public float HealthRegeneration => _healthRegeneration;

    /// <summary>
    /// 초당 마나 재생
    /// - 전투 중/비전투 중 모두 적용
    /// </summary>
    public float ManaRegeneration => _manaRegeneration;

    #endregion

    /// <summary>
    /// 두 CharacterStats를 합산하여 새 인스턴스 반환
    /// 
    /// 불변성 패턴:
    /// - 원본 객체들은 변경되지 않음
    /// - 항상 새로운 인스턴스 생성
    /// - 안전한 스탯 계산
    /// 
    /// 사용 시나리오:
    /// 1. 기본 스탯 + 장비 스탯 = 총합 스탯
    /// 2. 레벨업 스탯 누적
    /// 3. 버프/디버프 적용
    /// 
    /// Parameters:
    ///     otherStats: 합산할 다른 스탯
    ///     
    /// Returns:
    ///     CharacterStats: 합산 결과 (새 인스턴스)
    ///     
    /// 예시:
    /// ```csharp
    /// // 기본: 힘 10, 체력 100
    /// CharacterStats base = ...;
    /// 
    /// // 장비: 힘 5, 체력 20
    /// CharacterStats equip = ...;
    /// 
    /// // 합산: 힘 15, 체력 120
    /// CharacterStats total = base.AddStats(equip);
    /// 
    /// // base와 equip은 변경 안 됨!
    /// ```
    /// 
    /// 주의사항:
    /// - 매 호출마다 새 객체 생성 (메모리 사용)
    /// - 자주 호출하면 GC 부하 가능
    /// - 결과는 캐싱하여 재사용 권장
    /// </summary>
    public CharacterStats AddStats(CharacterStats otherStats)
    {
        // 새 인스턴스 생성
        CharacterStats result = CreateInstance<CharacterStats>();

        // === 주요 스탯 합산 ===
        result._strength = this._strength + otherStats._strength;
        result._dexterity = this._dexterity + otherStats._dexterity;
        result._intelligence = this._intelligence + otherStats._intelligence;
        result._vitality = this._vitality + otherStats._vitality;

        // === 2차 스탯 합산 ===
        result._criticalChance = this._criticalChance + otherStats._criticalChance;
        result._criticalDamage = this._criticalDamage + otherStats._criticalDamage;
        result._attackSpeed = this._attackSpeed + otherStats._attackSpeed;

        // === 리소스 합산 ===
        result._maxHealth = this._maxHealth + otherStats._maxHealth;
        result._maxMana = this._maxMana + otherStats._maxMana;
        result._healthRegeneration = this._healthRegeneration + otherStats._healthRegeneration;
        result._manaRegeneration = this._manaRegeneration + otherStats._manaRegeneration;

        return result;
    }
}