using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 장비 관리 시스템 (Cloud Save 통합)
/// 
/// 추가된 기능:
/// - LoadFromSaveData() / SaveToData() 메서드
/// - 아이템 ID 기반 저장/로드
/// - CharacterDataManager와 협업
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [System.Serializable]
    public class EquipmentSlot
    {
        public EquipmentType slotType;
        public ItemData equippedItem;
    }

    [Header("장비 슬롯")]
    [SerializeField] private List<EquipmentSlot> _equipmentSlots = new List<EquipmentSlot>();

    [Header("아이템 데이터베이스")]
    [Tooltip("게임 내 모든 아이템 템플릿 (ID로 조회용)")]
    [SerializeField] private List<ItemData> _itemDatabase = new List<ItemData>();

    private PlayerCharacter _playerCharacter;
    private CharacterStats _equipmentStats;

    public static event Action<ItemData, EquipmentType> OnItemEquipped;
    public static event Action<ItemData, EquipmentType> OnItemUnequipped;

    private void Awake()
    {
        _playerCharacter = GetComponent<PlayerCharacter>();
        InitializeEquipmentSlots();
        _equipmentStats = ScriptableObject.CreateInstance<CharacterStats>();
    }

    private void InitializeEquipmentSlots()
    {
        if (_equipmentSlots.Count == 0)
        {
            _equipmentSlots.Add(new EquipmentSlot { slotType = EquipmentType.Weapon });
            _equipmentSlots.Add(new EquipmentSlot { slotType = EquipmentType.Helmet });
            _equipmentSlots.Add(new EquipmentSlot { slotType = EquipmentType.Chest });
            _equipmentSlots.Add(new EquipmentSlot { slotType = EquipmentType.Gloves });
            _equipmentSlots.Add(new EquipmentSlot { slotType = EquipmentType.Boots });
            _equipmentSlots.Add(new EquipmentSlot { slotType = EquipmentType.Amulet });
            _equipmentSlots.Add(new EquipmentSlot { slotType = EquipmentType.Ring });
        }
    }

    public bool EquipItem(ItemData item)
    {
        if (item == null)
            return false;

        EquipmentSlot slot = FindSlotByType(item.EquipmentType);
        if (slot == null)
            return false;

        // 기존 아이템 해제
        if (slot.equippedItem != null)
        {
            UnequipItem(item.EquipmentType);
        }

        slot.equippedItem = item;
        RecalculateEquipmentStats();
        OnItemEquipped?.Invoke(item, item.EquipmentType);

        Debug.Log($"{item.ItemName} 착용");
        return true;
    }

    public bool UnequipItem(EquipmentType slotType)
    {
        EquipmentSlot slot = FindSlotByType(slotType);
        if (slot == null || slot.equippedItem == null)
            return false;

        ItemData unequippedItem = slot.equippedItem;
        slot.equippedItem = null;
        RecalculateEquipmentStats();
        OnItemUnequipped?.Invoke(unequippedItem, slotType);

        Debug.Log($"{unequippedItem.ItemName} 해제");
        return true;
    }

    public ItemData GetEquippedItem(EquipmentType slotType)
    {
        EquipmentSlot slot = FindSlotByType(slotType);
        return slot?.equippedItem;
    }

    public CharacterStats GetEquipmentStats()
    {
        return _equipmentStats;
    }

    private EquipmentSlot FindSlotByType(EquipmentType type)
    {
        return _equipmentSlots.Find(slot => slot.slotType == type);
    }

    private void RecalculateEquipmentStats()
    {
        _equipmentStats = ScriptableObject.CreateInstance<CharacterStats>();

        foreach (EquipmentSlot slot in _equipmentSlots)
        {
            if (slot.equippedItem != null && slot.equippedItem.Stats != null)
            {
                _equipmentStats = _equipmentStats.AddStats(slot.equippedItem.Stats);
            }
        }

        if (_playerCharacter != null)
        {
            _playerCharacter.UpdateEquipmentStats(_equipmentStats);
        }
    }

    #region Cloud Save 통합

    /// <summary>
    /// CharacterSaveData에서 장비 로드
    /// CharacterDataManager에서 호출
    /// 
    /// 복원 항목:
    /// - 각 슬롯에 착용된 아이템 (ID로 조회)
    /// </summary>
    public void LoadFromSaveData(CharacterSaveData data)
    {
        if (data == null || data.equipment == null)
        {
            Debug.LogError("로드할 장비 데이터가 없습니다!");
            return;
        }

        // 모든 슬롯 초기화
        foreach (var slot in _equipmentSlots)
        {
            slot.equippedItem = null;
        }

        // 각 슬롯에 아이템 로드
        LoadItemToSlot(EquipmentType.Weapon, data.equipment.weapon);
        LoadItemToSlot(EquipmentType.Helmet, data.equipment.helmet);
        LoadItemToSlot(EquipmentType.Chest, data.equipment.chest);
        LoadItemToSlot(EquipmentType.Gloves, data.equipment.gloves);
        LoadItemToSlot(EquipmentType.Boots, data.equipment.boots);
        LoadItemToSlot(EquipmentType.Amulet, data.equipment.amulet);
        LoadItemToSlot(EquipmentType.Ring, data.equipment.ring);

        // 장비 스탯 재계산
        RecalculateEquipmentStats();

        Debug.Log("장비 데이터 로드 완료");
    }

    /// <summary>
    /// 현재 장비를 CharacterSaveData에 저장
    /// CharacterDataManager에서 호출
    /// 
    /// 저장 항목:
    /// - 각 슬롯에 착용된 아이템 ID
    /// </summary>
    public void SaveToData(ref CharacterSaveData data)
    {
        if (data == null || data.equipment == null)
        {
            Debug.LogError("저장할 데이터가 null입니다!");
            return;
        }

        data.equipment.weapon = GetItemId(GetEquippedItem(EquipmentType.Weapon));
        data.equipment.helmet = GetItemId(GetEquippedItem(EquipmentType.Helmet));
        data.equipment.chest = GetItemId(GetEquippedItem(EquipmentType.Chest));
        data.equipment.gloves = GetItemId(GetEquippedItem(EquipmentType.Gloves));
        data.equipment.boots = GetItemId(GetEquippedItem(EquipmentType.Boots));
        data.equipment.amulet = GetItemId(GetEquippedItem(EquipmentType.Amulet));
        data.equipment.ring = GetItemId(GetEquippedItem(EquipmentType.Ring));
    }

    /// <summary>
    /// 아이템 ID로 슬롯에 로드
    /// </summary>
    private void LoadItemToSlot(EquipmentType slotType, string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        ItemData item = FindItemById(itemId);
        if (item != null)
        {
            EquipmentSlot slot = FindSlotByType(slotType);
            if (slot != null)
            {
                slot.equippedItem = item;
            }
        }
        else
        {
            Debug.LogWarning($"아이템 ID를 찾을 수 없습니다: {itemId}");
        }
    }

    /// <summary>
    /// 아이템 ID 반환 (null이면 빈 문자열)
    /// </summary>
    private string GetItemId(ItemData item)
    {
        if (item == null)
            return "";

        // ItemData.ItemNumber 또는 고유 ID 사용
        return item.ItemNumber.ToString();
    }

    /// <summary>
    /// 아이템 데이터베이스에서 ID로 조회
    /// 
    /// 사용:
    /// - LoadFromSaveData()에서 아이템 복원 시
    /// 
    /// TODO: 더 효율적인 Dictionary 기반 검색으로 개선
    /// </summary>
    private ItemData FindItemById(string itemId)
    {
        foreach (ItemData item in _itemDatabase)
        {
            if (item.ItemNumber.ToString() == itemId)
            {
                return item;
            }
        }
        return null;
    }

    #endregion
}