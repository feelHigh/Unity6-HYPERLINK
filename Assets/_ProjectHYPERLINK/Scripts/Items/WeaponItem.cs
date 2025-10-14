using UnityEngine;

/// <summary>
/// 무기 아이템 구체 클래스
/// 
/// Item 추상 클래스를 상속받아 무기 전용 기능 구현
/// 
/// 무기 특징:
/// - 공격 데미지 증폭
/// - 물리/마법 공격력 스탯 적용
/// - 착용/해제 시 EquipmentManager 통합
/// 
/// 사용 흐름:
/// 1. ItemSpawner가 무기 생성
/// 2. Initialize()로 데이터 설정
/// 3. 플레이어가 줍기
/// 4. Equip() 호출로 EquipmentManager에 장착
/// 5. CalculateDamage()로 무기 데미지 계산
/// </summary>
public class WeaponItem : Item
{
    [Header("무기 전용 속성")]
    [SerializeField] private float _attackDamageMultiplier = 1.0f;  // 공격 데미지 배율

    /// <summary>
    /// 무기 장착 처리
    /// 
    /// 호출 위치:
    /// - ItemPickupManager (자동 장착)
    /// - 인벤토리 UI (수동 장착)
    /// 
    /// 처리 과정:
    /// 1. PlayerCharacter 찾기
    /// 2. EquipmentManager 가져오기
    /// 3. EquipItem() 호출로 무기 슬롯에 장착
    /// </summary>
    public override void Equip()
    {
        Debug.Log($"무기 장착: {ItemName}");

        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        EquipmentManager equipmentManager = player?.GetComponent<EquipmentManager>();

        if (equipmentManager != null && _itemData != null)
        {
            equipmentManager.EquipItem(_itemData);
        }
    }

    /// <summary>
    /// 무기 해제 처리
    /// 
    /// 호출 위치:
    /// - 장비 창에서 우클릭
    /// - 다른 무기 착용 시 (자동 해제)
    /// </summary>
    public override void Unequip()
    {
        Debug.Log($"무기 해제: {ItemName}");

        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        EquipmentManager equipmentManager = player?.GetComponent<EquipmentManager>();

        if (equipmentManager != null && _itemData != null)
        {
            equipmentManager.UnequipItem(_itemData.EquipmentType);
        }
    }

    /// <summary>
    /// 무기 데미지 계산
    /// 
    /// 계산 공식:
    /// - 기본 무기 데미지 × 배율
    /// - + 스탯에서 오는 추가 데미지
    /// 
    /// 스탯 적용:
    /// - PhysicsAttack: 물리 공격력
    /// - MagicAttack: 마법 공격력
    /// 
    /// Returns:
    ///     float: 최종 무기 데미지
    ///     
    /// 예시:
    /// - 기본: 10 × 1.5 = 15
    /// - 물리 공격: +25
    /// - 최종: 40
    /// </summary>
    public float CalculateDamage()
    {
        float baseDamage = 10f * _attackDamageMultiplier;

        // 런타임 스탯에서 추가 데미지
        if (_runtimeStats != null)
        {
            foreach (ItemStat stat in _runtimeStats)
            {
                if (stat.Type == ItemStatType.PhysicsAttack || stat.Type == ItemStatType.MagicAttack)
                {
                    baseDamage += stat.Value;
                }
            }
        }

        return baseDamage;
    }
}