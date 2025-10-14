using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스탯 롤링 범위 설정 구조체
/// 각 스탯 타입별 최소/최대값을 정의
/// </summary>
[System.Serializable]
public class DropStatsAndRange
{
    [SerializeField] private ItemStatType _type;      // 스탯 종류 (힘, 민첩 등)
    [SerializeField] private float _minStat;          // 최소값
    [SerializeField] private float _maxStat;          // 최대값

    public ItemStatType Type => _type;
    public float MinStat => _minStat;
    public float MaxStat => _maxStat;
}

/// <summary>
/// 절차적 아이템 생성 시스템 (Procedural Item Generation)
/// 
/// 핵심 기능:
/// - 적 처치 시 랜덤 아이템 드랍
/// - 아이템 등급별 스탯 수 결정 (일반=0, 마법=2, 희귀=4 등)
/// - 랜덤 스탯 롤링 (설정된 범위 내에서)
/// - 아이템 이름 자동 생성 (접두사 + 기본 이름 + 접미사)
/// - 무기/장비 구분하여 적절한 스탯 부여
/// 
/// 사용 흐름:
/// 1. Enemy.Die() → ItemSpawner.SpawnItem() 호출
/// 2. ItemDropTable로 아이템 등급 결정 (일반/마법/희귀 등)
/// 3. 아이템 타입 결정 (무기 or 장비)
/// 4. 템플릿 선택 및 런타임 복사본 생성
/// 5. 랜덤 스탯 생성 및 적용
/// 6. 이름 생성 및 적용
/// 7. 월드에 아이템 인스턴스화
/// 
/// 템플릿 시스템:
/// - 디자이너가 에디터에서 기본 아이템 템플릿 생성
/// - ItemSpawner가 템플릿을 복사해 랜덤 스탯 부여
/// - 원본 템플릿은 보존되며 재사용 가능
/// 
/// Diablo 3 시스템과의 유사점:
/// - 등급별 스탯 수 차등 (일반 < 마법 < 희귀 < 전설)
/// - 랜덤 스탯 롤링
/// - 절차적 이름 생성
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    [Header("스탯 롤링 설정")]
    [Tooltip("방어구/액세서리 스탯 범위")]
    [SerializeField] private DropStatsAndRange[] _equipment_DropRanges;

    [Tooltip("무기 스탯 범위")]
    [SerializeField] private DropStatsAndRange[] _weapon_DropRanges;

    [Header("아이템 프리팹")]
    [Tooltip("WeaponItem 컴포넌트가 있는 프리팹")]
    [SerializeField] private WeaponItem _weaponPrefab;

    [Tooltip("EquipmentItem 컴포넌트가 있는 프리팹")]
    [SerializeField] private EquipmentItem _equipmentPrefab;

    [Header("아이템 템플릿")]
    [Tooltip("무기 ItemData 템플릿 목록 (여기서 랜덤 선택)")]
    [SerializeField] private ItemData[] _weaponTemplates;

    [Tooltip("방어구/액세서리 ItemData 템플릿 목록")]
    [SerializeField] private ItemData[] _equipmentTemplates;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 아이템 생성 메인 메서드 (Public - Enemy.Die()에서 호출)
    /// 
    /// 처리 과정:
    /// 1. ItemDropTable로 아이템 등급 결정
    /// 2. 아이템 타입 랜덤 선택 (0=무기, 1=장비)
    /// 3. 아이템 인스턴스 생성 및 초기화
    /// 4. 월드 좌표에 배치
    /// 
    /// Parameters:
    ///     position: 아이템이 드랍될 월드 좌표
    ///     dropTable: 등급 확률이 정의된 드랍 테이블
    /// </summary>
    public void SpawnItem(Vector3 position, ItemDropTableData dropTable)
    {
        // 1. 드랍 테이블에서 아이템 등급 결정
        // (일반 60%, 마법 30%, 희귀 10% 같은 확률)
        ItemQuality quality = dropTable.RollItemQuality();

        // 2. 아이템 타입 결정 (50% 확률로 무기 or 장비)
        int itemTypeRoll = Random.Range(0, 2);

        // 3. 아이템 생성
        Item spawnedItem = InstantiateItem(itemTypeRoll, quality);

        // 4. 월드에 배치
        if (spawnedItem != null)
        {
            spawnedItem.transform.position = position;
        }
    }

    /// <summary>
    /// 아이템 인스턴스 생성 및 초기화
    /// 
    /// 무기 생성 (itemType == 0):
    /// 1. 무기 템플릿 중 하나 랜덤 선택
    /// 2. ItemData 런타임 복사본 생성
    /// 3. 등급 설정
    /// 4. 무기 스탯 롤링
    /// 5. 이름 생성
    /// 6. WeaponItem 인스턴스화 및 초기화
    /// 
    /// 장비 생성 (itemType == 1):
    /// 1. 장비 템플릿 중 하나 랜덤 선택
    /// 2-6. 무기와 동일 (스탯 범위만 다름)
    /// 
    /// Parameters:
    ///     itemType: 0=무기, 1=장비
    ///     quality: 아이템 등급 (일반/마법/희귀 등)
    ///     
    /// Returns:
    ///     생성된 아이템 인스턴스 (실패 시 null)
    /// </summary>
    private Item InstantiateItem(int itemType, ItemQuality quality)
    {
        // === 무기 생성 ===
        if (itemType == 0 && _weaponPrefab != null && _weaponTemplates != null && _weaponTemplates.Length > 0)
        {
            // 1. 랜덤 무기 템플릿 선택
            ItemData template = _weaponTemplates[Random.Range(0, _weaponTemplates.Length)];

            // 2. 런타임 복사본 생성 (원본 보존)
            ItemData itemData = template.CreateRuntimeCopy();
            itemData.SetQuality(quality);

            // 3. 등급에 따른 스탯 개수 결정 및 생성
            // 예: 희귀 아이템 = 4개 스탯
            List<ItemStat> stats = GenerateStats(_weapon_DropRanges, GetStatCountForQuality(quality));
            itemData.SetProceduralStats(stats);

            // 4. 절차적 이름 생성
            // 예: "강력한 검 힘" (등급 접두사 + 기본 이름 + 스탯 접미사)
            string generatedName = GenerateItemName(template.ItemName, quality, stats);
            itemData.SetName(generatedName);

            // 5. WeaponItem 인스턴스화 및 초기화
            WeaponItem weapon = Instantiate(_weaponPrefab);
            weapon.Initialize(itemData, quality, stats, generatedName);

            return weapon;
        }
        // === 장비 생성 ===
        else if (itemType == 1 && _equipmentPrefab != null && _equipmentTemplates != null && _equipmentTemplates.Length > 0)
        {
            // 무기와 동일한 프로세스, 스탯 범위만 다름
            ItemData template = _equipmentTemplates[Random.Range(0, _equipmentTemplates.Length)];
            ItemData itemData = template.CreateRuntimeCopy();
            itemData.SetQuality(quality);

            // 장비는 _equipment_DropRanges 사용 (방어 스탯 중심)
            List<ItemStat> stats = GenerateStats(_equipment_DropRanges, GetStatCountForQuality(quality));
            itemData.SetProceduralStats(stats);

            string generatedName = GenerateItemName(template.ItemName, quality, stats);
            itemData.SetName(generatedName);

            EquipmentItem equipment = Instantiate(_equipmentPrefab);
            equipment.Initialize(itemData, quality, stats, generatedName);

            return equipment;
        }

        Debug.LogWarning("아이템 생성 실패 - 프리팹 또는 템플릿 누락");
        return null;
    }

    /// <summary>
    /// 랜덤 스탯 생성
    /// 
    /// 생성 규칙:
    /// - 같은 스탯 타입 중복 불가 (예: 힘+10, 힘+20 동시 불가)
    /// - 각 스탯은 설정된 범위 내에서 랜덤 값
    /// - 필요한 개수만큼 스탯 선택
    /// 
    /// Parameters:
    ///     ranges: 스탯 타입별 최소/최대 범위 배열
    ///     count: 생성할 스탯 개수 (등급에 따라 다름)
    ///     
    /// Returns:
    ///     생성된 스탯 리스트
    ///     
    /// 예시 (희귀 무기, count=4):
    /// - 힘: 15~30 → 22 롤링
    /// - 크리티컬: 3~8% → 6% 롤링
    /// - 공격속도: 5~15% → 11% 롤링
    /// - 체력: 50~100 → 78 롤링
    /// </summary>
    private List<ItemStat> GenerateStats(DropStatsAndRange[] ranges, int count)
    {
        List<ItemStat> stats = new List<ItemStat>();
        List<ItemStatType> usedTypes = new List<ItemStatType>();  // 중복 방지용

        if (ranges == null || ranges.Length == 0)
            return stats;

        // count개의 스탯 생성
        for (int i = 0; i < count && i < ranges.Length; i++)
        {
            // 아직 사용하지 않은 스탯 타입 선택
            int attempts = 0;
            DropStatsAndRange selectedRange = null;

            do
            {
                selectedRange = ranges[Random.Range(0, ranges.Length)];
                attempts++;
            }
            while (usedTypes.Contains(selectedRange.Type) && attempts < 100);

            // 무한 루프 방지 (100번 시도 후 포기)
            if (attempts >= 100)
                break;

            // 범위 내에서 랜덤 값 생성
            float value = Random.Range(selectedRange.MinStat, selectedRange.MaxStat);
            stats.Add(new ItemStat(selectedRange.Type, value));
            usedTypes.Add(selectedRange.Type);
        }

        return stats;
    }

    /// <summary>
    /// 아이템 등급별 스탯 개수 결정
    /// 
    /// Diablo 3의 아이템 등급 시스템 기반:
    /// - Normal (일반/흰색): 0개 (기본 스탯만)
    /// - Magic (마법/파란색): 2개
    /// - Rare (희귀/노란색): 4개
    /// - Epic (영웅/보라색): 5개
    /// - Legendary (전설/주황색): 6개
    /// - Set (세트/초록색): 6개
    /// 
    /// 높은 등급일수록 더 많은 스탯 = 더 강력한 아이템
    /// 
    /// Parameters:
    ///     quality: 아이템 등급
    ///     
    /// Returns:
    ///     해당 등급의 스탯 개수
    /// </summary>
    private int GetStatCountForQuality(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.Normal: return 0;       // 일반: 추가 스탯 없음
            case ItemQuality.Magic: return 2;        // 마법: 1-2개
            case ItemQuality.Rare: return 4;         // 희귀: 3-4개
            case ItemQuality.Epic: return 5;         // 영웅: 4-5개
            case ItemQuality.Legendary: return 6;    // 전설: 5-6개
            case ItemQuality.Set: return 6;          // 세트: 5-6개
            default: return 0;
        }
    }

    /// <summary>
    /// 절차적 아이템 이름 생성
    /// 
    /// 생성 규칙:
    /// [등급 접두사] [기본 이름] [스탯 접미사]
    /// 
    /// 예시:
    /// - 일반: "검"
    /// - 마법: "강화된 검"
    /// - 희귀: "우수한 검 힘의"
    /// - 전설: "전설의 검 힘의"
    /// 
    /// 첫 번째 스탯을 기준으로 접미사 결정
    /// (더 복잡한 시스템으로 확장 가능)
    /// 
    /// Parameters:
    ///     baseName: 템플릿의 기본 이름 (예: "검")
    ///     quality: 아이템 등급
    ///     stats: 스탯 리스트
    ///     
    /// Returns:
    ///     생성된 최종 이름
    /// </summary>
    private string GenerateItemName(string baseName, ItemQuality quality, List<ItemStat> stats)
    {
        string prefix = GetQualityPrefix(quality);
        string suffix = stats.Count > 0 ? GetStatSuffix(stats[0].Type) : "";

        // Trim()으로 공백 제거
        return $"{prefix} {suffix} {baseName}".Trim();
    }

    /// <summary>
    /// 등급별 접두사 반환
    /// 
    /// 일반 아이템은 접두사 없음
    /// 높은 등급일수록 화려한 접두사
    /// </summary>
    private string GetQualityPrefix(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.Magic: return "강화된";
            case ItemQuality.Rare: return "우수한";
            case ItemQuality.Epic: return "강력한";
            case ItemQuality.Legendary: return "전설의";
            case ItemQuality.Set: return "고대의";
            default: return "";
        }
    }

    /// <summary>
    /// 스탯 타입별 접미사 반환
    /// 
    /// 첫 번째 스탯에 기반한 접미사 생성
    /// "of [특성]" 형식
    /// 
    /// 예:
    /// - 힘 → "힘의"
    /// - 민첩 → "민첩의"
    /// - 지능 → "지혜의"
    /// </summary>
    private string GetStatSuffix(ItemStatType statType)
    {
        switch (statType)
        {
            case ItemStatType.Strength: return "힘의";
            case ItemStatType.Dexterity: return "민첩의";
            case ItemStatType.Intelligence: return "지혜의";
            case ItemStatType.Vitality: return "활력의";
            case ItemStatType.PhysicsAttack: return "물리의";
            case ItemStatType.MagicAttack: return "마법의";
            default: return "";
        }
    }
}