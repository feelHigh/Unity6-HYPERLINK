using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 장비 시스템 관리자 (PlayerCharacter 자동 검색 추가)
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [System.Serializable]
    public class EquipmentSlot
    {
        public EquipmentType slotType;
        public ItemData equippedItem;
    }

    [Header("장비 슬롯 설정")]
    [SerializeField] private List<EquipmentSlot> _equipmentSlots = new List<EquipmentSlot>();

    [Header("아이템 데이터베이스")]
    [SerializeField] private List<ItemData> _itemDatabase = new List<ItemData>();

    [Header("캐릭터 참조")]
    [SerializeField] private PlayerCharacter _playerCharacter;

    private CharacterStats _equipmentStats;
    private Dictionary<string, ItemData> _itemLookup;

    #region 초기화

    private void Awake()
    {
        // ===== 수정: PlayerCharacter 자동 검색 =====
        if (_playerCharacter == null)
        {
            _playerCharacter = GetComponent<PlayerCharacter>();

            if (_playerCharacter == null)
            {
                Debug.LogError("[EquipmentManager] PlayerCharacter 컴포넌트를 찾을 수 없습니다! " +
                               "EquipmentManager는 PlayerCharacter와 같은 GameObject에 있어야 합니다.");
            }
            else
            {
                Debug.Log("[EquipmentManager] PlayerCharacter 자동 검색 성공");
            }
        }

        InitializeSlots();
        InitializeItemLookup();
        _equipmentStats = ScriptableObject.CreateInstance<CharacterStats>();
    }

    private void InitializeSlots()
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

    private void InitializeItemLookup()
    {
        _itemLookup = new Dictionary<string, ItemData>(_itemDatabase.Count);

        foreach (ItemData item in _itemDatabase)
        {
            if (item == null)
            {
                Debug.LogWarning("[EquipmentManager] ItemDatabase에 null 아이템 존재!");
                continue;
            }

            string key = item.ItemNumber.ToString();
            if (!_itemLookup.ContainsKey(key))
            {
                _itemLookup.Add(key, item);
            }
            else
            {
                Debug.LogWarning($"[EquipmentManager] 중복 ItemNumber: {key}");
            }
        }

        Debug.Log($"[EquipmentManager] 아이템 데이터베이스 로드: {_itemLookup.Count}개");
    }

    #endregion

    #region 장비 착용/해제

    public bool EquipItem(ItemData item)
    {
        if (item == null)
            return false;

        EquipmentSlot slot = FindSlotByType(item.EquipmentType);
        if (slot == null)
        {
            Debug.LogWarning($"[EquipmentManager] 해당 슬롯 없음: {item.EquipmentType}");
            return false;
        }

        // 기존 아이템 해제
        if (slot.equippedItem != null)
        {
            UnequipItem(slot.slotType);
        }

        // 새 아이템 장착
        slot.equippedItem = item;
        RecalculateEquipmentStats();

        Debug.Log($"[EquipmentManager] 장착: {item.ItemName}");
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

        Debug.Log($"[EquipmentManager] 해제: {unequippedItem.ItemName}");
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

    #endregion

    #region 스탯 계산

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
        else
        {
            Debug.LogWarning("[EquipmentManager] PlayerCharacter 참조 없음 - 스탯 업데이트 불가");
        }
    }

    #endregion

    #region Cloud Save 통합

    public void LoadFromSaveData(CharacterSaveData data)
    {
        if (data == null || data.equipment == null)
        {
            Debug.LogError("[EquipmentManager] 로드할 장비 데이터가 없습니다!");
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

        RecalculateEquipmentStats();

        Debug.Log("[EquipmentManager] 장비 데이터 로드 완료");
    }

    private void LoadItemToSlot(EquipmentType slotType, string itemNumber)
    {
        if (string.IsNullOrEmpty(itemNumber))
            return;

        if (_itemLookup.TryGetValue(itemNumber, out ItemData item))
        {
            EquipmentSlot slot = FindSlotByType(slotType);
            if (slot != null)
            {
                slot.equippedItem = item;
            }
        }
        else
        {
            Debug.LogWarning($"[EquipmentManager] 아이템을 찾을 수 없음: {itemNumber}");
        }
    }

    public void SaveToData(CharacterSaveData data)
    {
        if (data == null || data.equipment == null)
        {
            Debug.LogError("[EquipmentManager] 저장할 데이터가 null입니다!");
            return;
        }

        data.equipment.weapon = GetItemNumber(EquipmentType.Weapon);
        data.equipment.helmet = GetItemNumber(EquipmentType.Helmet);
        data.equipment.chest = GetItemNumber(EquipmentType.Chest);
        data.equipment.gloves = GetItemNumber(EquipmentType.Gloves);
        data.equipment.boots = GetItemNumber(EquipmentType.Boots);
        data.equipment.amulet = GetItemNumber(EquipmentType.Amulet);
        data.equipment.ring = GetItemNumber(EquipmentType.Ring);
    }

    private string GetItemNumber(EquipmentType slotType)
    {
        ItemData item = GetEquippedItem(slotType);
        return item != null ? item.ItemNumber.ToString() : "";
    }

    #endregion
}
