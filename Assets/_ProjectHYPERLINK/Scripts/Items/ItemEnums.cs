/// <summary>
/// 통합 장비 타입 시스템
/// 
/// 이전: EquipmentType, EquipType 중복 정의
/// 현재: ItemEnums.cs에 통합
/// 
/// 슬롯 종류:
/// - None: 장비 아님
/// - Weapon: 무기 (검, 활, 지팡이 등)
/// - Helmet: 투구
/// - Chest: 갑옷
/// - Gloves: 장갑
/// - Boots: 신발
/// - Amulet: 목걸이
/// - Ring: 반지
/// 
/// 사용 위치:
/// - EquipmentManager: 슬롯 관리
/// - ItemData: 아이템 분류
/// </summary>
public enum EquipmentType
{
    None,        // 장비 아님

    // 무기
    Weapon,      // 일반 무기 슬롯
    Sword,       // 검 (특화 타입)
    Bow,         // 활
    Staff,       // 지팡이

    // 방어구
    Helmet,      // 투구
    Chest,       // 갑옷
    Gloves,      // 장갑
    Boots,       // 신발

    // 액세서리
    Amulet,      // 목걸이
    Ring         // 반지
}

/// <summary>
/// 통합 아이템 등급/희귀도 시스템
/// 
/// 이전: ItemQuality, Rarity 중복 정의
/// 현재: ItemEnums.cs에 통합
/// 
/// Diablo 3 등급 시스템 기반:
/// - Normal (흰색): 기본 아이템, 추가 스탯 없음
/// - Magic (파란색): 1-2개 추가 스탯
/// - Rare (노란색): 3-4개 추가 스탯
/// - Epic (보라색): 4-5개 추가 스탯 (Diablo에는 없음)
/// - Legendary (주황색): 5-6개 스탯 + 고유 능력
/// - Set (초록색): 세트 보너스 (여러 개 착용 시)
/// 
/// 드랍 확률 예시:
/// - Normal: 60%
/// - Magic: 30%
/// - Rare: 9%
/// - Epic: 0.7%
/// - Legendary: 0.29%
/// - Set: 0.01%
/// </summary>
public enum ItemQuality
{
    Normal,      // 일반 (흰색)
    Magic,       // 마법 (파란색)
    Rare,        // 희귀 (노란색)
    Epic,        // 영웅 (보라색)
    Legendary,   // 전설 (주황색)
    Set          // 세트 (초록색)
}

/// <summary>
/// 아이템 스탯 타입
/// 
/// 절차적 아이템 생성에 사용
/// ItemStat 구조체의 Type 필드로 사용
/// 
/// 카테고리:
/// - Primary: 주요 스탯 (힘, 민첩, 지능, 활력)
/// - Offensive: 공격 스탯
/// - Defensive: 방어 스탯
/// - Utility: 유틸리티 스탯
/// </summary>
public enum ItemStatType
{
    // === 주요 스탯 ===
    Strength,           // 힘 (Warrior 주요 스탯)
    Dexterity,          // 민첩 (Archer 주요 스탯)
    Intelligence,       // 지능 (Mage 주요 스탯)
    Vitality,           // 활력 (체력 증가)

    // === 공격 스탯 ===
    PhysicsAttack,      // 물리 공격력
    MagicAttack,        // 마법 공격력
    CriticalChance,     // 크리티컬 확률 (%)
    CriticalDamage,     // 크리티컬 데미지 (%)
    AttackSpeed,        // 공격 속도 (%)

    // === 방어 스탯 ===
    Health,             // 최대 체력
    Armor,              // 방어력
    AllResistance,      // 모든 저항

    // === 유틸리티 스탯 ===
    Speed,              // 이동 속도
    HealthRegeneration, // 체력 재생
    ManaRegeneration    // 마나 재생
}

/// <summary>
/// 드랍 타입 분류
/// 
/// 적 종류별 드랍 테이블 구분용
/// 
/// - Class1: 일반 적 (낮은 등급)
/// - Class2: 엘리트 적 (중간 등급)
/// - Class3: 보스 적 (높은 등급)
/// - Universal: 모든 적 (균일 확률)
/// </summary>
public enum DropType
{
    Class1,      // 일반
    Class2,      // 엘리트
    Class3,      // 보스
    Universal    // 범용
}