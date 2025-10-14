using UnityEngine;

/// <summary>
/// 방어구/액세서리 아이템 구체 클래스
/// 
/// Item 추상 클래스를 상속받아 장비 전용 기능 구현
/// 
/// 장비 종류:
/// - 방어구: 투구, 갑옷, 장갑, 신발
/// - 액세서리: 목걸이, 반지
/// 
/// 장비 특징:
/// - 방어력/저항 증가
/// - Armor, AllResistance 스탯 적용
/// - 착용/해제 시 EquipmentManager 통합
/// </summary>
public class EquipmentItem : Item
{
    [Header("장비 전용 속성")]
    [SerializeField] private float _defenseMultiplier = 1.0f;  // 방어력 배율

    /// <summary>
    /// 장비 착용 처리
    /// WeaponItem.Equip()과 동일한 로직
    /// </summary>
    public override void Equip()
    {
        Debug.Log($"장비 착용: {ItemName}");

        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        EquipmentManager equipmentManager = player?.GetComponent<EquipmentManager>();

        if (equipmentManager != null && _itemData != null)
        {
            equipmentManager.EquipItem(_itemData);
        }
    }

    /// <summary>
    /// 장비 해제 처리
    /// WeaponItem.Unequip()과 동일한 로직
    /// </summary>
    public override void Unequip()
    {
        Debug.Log($"장비 해제: {ItemName}");

        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        EquipmentManager equipmentManager = player?.GetComponent<EquipmentManager>();

        if (equipmentManager != null && _itemData != null)
        {
            equipmentManager.UnequipItem(_itemData.EquipmentType);
        }
    }

    /// <summary>
    /// 방어력 계산
    /// 
    /// 계산 공식:
    /// - 기본 방어력 × 배율
    /// - + 스탯에서 오는 추가 방어력
    /// 
    /// 스탯 적용:
    /// - Armor: 방어력
    /// - AllResistance: 모든 저항
    /// 
    /// Returns:
    ///     float: 최종 방어력
    ///     
    /// 예시:
    /// - 기본: 5 × 1.0 = 5
    /// - 방어력: +50
    /// - 저항: +10
    /// - 최종: 65
    /// </summary>
    public float CalculateDefense()
    {
        float baseDefense = 5f * _defenseMultiplier;

        // 런타임 스탯에서 추가 방어력
        if (_runtimeStats != null)
        {
            foreach (ItemStat stat in _runtimeStats)
            {
                if (stat.Type == ItemStatType.Armor || stat.Type == ItemStatType.AllResistance)
                {
                    baseDefense += stat.Value;
                }
            }
        }

        return baseDefense;
    }
}