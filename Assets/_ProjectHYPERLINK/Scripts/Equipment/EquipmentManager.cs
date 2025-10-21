using UnityEngine;
using System.Collections.Generic;

/// 시스템 분류: 장비 관리 시스템
/// 
/// 의존성: PlayerCharacter, ItemData, CharacterStats
/// 피의존성: CharacterDataManager, UI 시스템
/// 
/// 핵심 기능: 장비 착용 해제 및 스탯 계산
/// 
/// 기능:
/// - 장비 슬롯: 7개 슬롯 (무기, 투구, 갑옷, 장갑, 신발, 목걸이, 반지)
/// - 장비 착용 해제: 아이템 장착 및 제거
/// - 스탯 계산: 모든 장착 장비의 스탯 합산
/// - 스탯 전달: PlayerCharacter에 장비 스탯 업데이트
/// - 아이템 조회: ItemNumber로 아이템 검색
/// 
/// 주의사항:
/// - PlayerCharacter는 같은 GameObject에 필수
/// - ItemDatabase에 모든 아이템 등록 필요
/// - Cloud Save 로드 시 ItemNumber로 아이템 복원

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
        // PlayerCharacter 자동 검색
        if (_playerCharacter == null)
        {
            _playerCharacter = GetComponent<PlayerCharacter>();

            if (_playerCharacter == null)
            {
                Debug.LogError("[EquipmentManager] PlayerCharacter 컴포넌트를 찾을 수 없습니다");
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

    /// 슬롯 초기화
    /// 7개 기본 슬롯 생성
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

    /// 아이템 조회 테이블 초기화
    /// ItemNumber를 키로 빠른 검색 가능
    private void InitializeItemLookup()
    {
        _itemLookup = new Dictionary<string, ItemData>(_itemDatabase.Count);

        foreach (ItemData item in _itemDatabase)
        {
            if (item == null)
            {
                Debug.LogWarning("[EquipmentManager] ItemDatabase에 null 아이템 존재");
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

    #region 장비 착용 해제

    /// 아이템 장착
    /// 
    /// 처리 과정:
    /// 1. 아이템 타입에 맞는 슬롯 찾기
    /// 2. 기존 아이템이 있으면 해제
    /// 3. 새 아이템 장착
    /// 4. 스탯 재계산
    public bool EquipItem(ItemData item)
    {
        if (item == null) return false;

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

    /// 아이템 해제
    public bool UnequipItem(EquipmentType slotType)
    {
        EquipmentSlot slot = FindSlotByType(slotType);
        if (slot == null || slot.equippedItem == null) return false;

        ItemData unequippedItem = slot.equippedItem;
        slot.equippedItem = null;
        RecalculateEquipmentStats();

        Debug.Log($"[EquipmentManager] 해제: {unequippedItem.ItemName}");
        return true;
    }

    /// 장착된 아이템 가져오기
    public ItemData GetEquippedItem(EquipmentType slotType)
    {
        EquipmentSlot slot = FindSlotByType(slotType);
        return slot?.equippedItem;
    }

    /// 장비 스탯 가져오기
    public CharacterStats GetEquipmentStats()
    {
        return _equipmentStats;
    }

    /// 슬롯 타입으로 슬롯 찾기
    private EquipmentSlot FindSlotByType(EquipmentType type)
    {
        return _equipmentSlots.Find(slot => slot.slotType == type);
    }

    #endregion

    #region 스탯 계산

    /// 장비 스탯 재계산
    /// 
    /// 처리 과정:
    /// 1. 장비 스탯 초기화
    /// 2. 모든 슬롯 순회
    /// 3. 장착된 아이템의 스탯 합산
    /// 4. PlayerCharacter에 전달
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

    #region Cloud Save 연동

    /// CharacterSaveData에서 장비 로드
    /// 
    /// 처리 과정:
    /// 1. 모든 슬롯 초기화
    /// 2. SaveData의 ItemNumber로 아이템 조회
    /// 3. 조회된 아이템을 슬롯에 장착
    /// 4. 스탯 재계산
    public void LoadFromSaveData(CharacterSaveData data)
    {
        if (data == null || data.equipment == null)
        {
            Debug.LogError("[EquipmentManager] 로드할 장비 데이터가 없습니다");
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

    /// 특정 슬롯에 아이템 로드
    /// ItemNumber로 아이템 조회하여 장착
    private void LoadItemToSlot(EquipmentType slotType, string itemNumber)
    {
        if (string.IsNullOrEmpty(itemNumber)) return;

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

    /// 현재 장비 상태를 SaveData에 저장
    /// 각 슬롯의 ItemNumber를 문자열로 저장
    public void SaveToData(CharacterSaveData data)
    {
        if (data == null || data.equipment == null)
        {
            Debug.LogError("[EquipmentManager] 저장할 데이터가 null입니다");
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

    /// 슬롯의 ItemNumber 문자열 반환
    /// 아이템이 없으면 빈 문자열
    private string GetItemNumber(EquipmentType slotType)
    {
        ItemData item = GetEquippedItem(slotType);
        return item != null ? item.ItemNumber.ToString() : "";
    }

    #endregion
}
