using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]

public class EquipInevnetory : MonoBehaviour, IPointerEnterHandler
{
    [Header("참조")]
    [SerializeField] InventoryItemEventHandler _itemEventHandler;
    [SerializeField] ItemInventory _inventory;

    [Header("아이템 오브젝트들이 보이는 실제 위치")]
    [SerializeField] ItemVisualizeField _itemVisualizeField;

    [Header("프리팹")]
    [SerializeField] InventoryItemPrefab _itemPrefab;
    [Header("슬롯")]
    [SerializeField] EquipSlot[] _slots;
    [SerializeField] EquipSlot _currentSlot;

    [Header("임시")]
    [SerializeField] EquipmentManager _equipmentManager;

    [Header("자동 검색 설정")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _retryInterval = 0.5f;
    [SerializeField] private int _maxRetries = 20;

    private bool _isInitialized = false;
    private int _retryCount = 0;

    public EquipSlot CurrentSlot => _currentSlot;

    void Start()
    {
        Initialize();
        InvokeRepeating(nameof(TryFindEquipmentManager), 0.1f, _retryInterval);
    }

    void OnDestroy()
    {
        CancelInvoke(nameof(TryFindEquipmentManager));
    }

    /// <summary>
    /// PlayerSpawner로 스폰된 플레이어를 찾기 위한 재시도 로직
    /// CharacterUIController와 동일한 방식
    /// </summary>
    private void TryFindEquipmentManager()
    {
        if (_isInitialized) return;

        _retryCount++;

        GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);

        if (playerObject != null)
        {
            _equipmentManager = playerObject.GetComponent<EquipmentManager>();

            if (_equipmentManager != null)
            {
                Debug.Log($"[EquipInventory] EquipmentManager 찾음: {playerObject.name} (시도: {_retryCount}회)");
                _isInitialized = true;
                CancelInvoke(nameof(TryFindEquipmentManager));
                return;
            }
        }

        if (_retryCount >= _maxRetries)
        {
            Debug.LogError($"[EquipInventory] EquipmentManager를 {_maxRetries}회 시도 후에도 찾지 못했습니다!");
            CancelInvoke(nameof(TryFindEquipmentManager));
        }
    }

    public void Initialize()
    {
        foreach (var slot in _slots)
        {
            slot.Initialize(this);
        }
    }


    /// <summary>
    /// 아이템을 받아와 해당 아이템을 미착용 시켜주는 함수
    /// </summary>
    /// <param name="item"></param>
    public void UnEquipItem(InventoryItemPrefab item)
    {
        if (_equipmentManager == null)
        {
            Debug.LogError("[EquipInventory] UnEquipItem: EquipmentManager가 null입니다!");
            return;
        }

        foreach (var slot in _slots)
        {
            if (slot.EquipmentType == item.Data.EquipmentType)
            {
                _equipmentManager.UnequipItem(item.Data.EquipmentType);
                slot.RemoveData();
            }
        }
    }

    /// <summary>
    /// 아이템을 받아와 착용시도,
    /// 실패시 False, 성공시 True
    /// 
    /// 수정사항:
    /// - _currentSlot null 체크 추가
    /// - _equipmentManager null 체크 추가
    /// - 자동으로 올바른 슬롯 찾기 기능 추가
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool EquipItem(InventoryItemPrefab item)
    {
        // 0. _equipmentManager null 체크
        if (_equipmentManager == null)
        {
            Debug.LogError("[EquipInventory] EquipItem: EquipmentManager가 null입니다!");
            return false;
        }

        // 1. _currentSlot이 null이면 자동으로 올바른 슬롯 찾기
        if (_currentSlot == null)
        {
            _currentSlot = FindSlotByEquipmentType(item.Data.EquipmentType);

            if (_currentSlot == null)
            {
                Debug.LogWarning($"[EquipInventory] 해당 장비 타입의 슬롯을 찾을 수 없습니다: {item.Data.EquipmentType}");
                return false;
            }
        }

        // 2. 슬롯 타입 확인
        if (_currentSlot.EquipmentType != item.Data.EquipmentType)
        {
            Debug.LogWarning($"[EquipInventory] 슬롯 타입이 일치하지 않습니다. 슬롯: {_currentSlot.EquipmentType}, 아이템: {item.Data.EquipmentType}");
            return false;
        }

        // 3. 기존 아이템이 있으면 인벤토리로 이동
        if (_currentSlot.HasItem)
        {
            if (!_inventory.GetEquipItem(_currentSlot))
            {
                return false;
            }
        }

        // 4. 아이템 장착
        _currentSlot.GetItemPrefab(item);
        item.transform.position = _currentSlot.transform.position;
        item.gameObject.SetActive(false);
        _equipmentManager.EquipItem(item.Data);

        return true;
    }

    /// <summary>
    /// 장비 타입에 맞는 슬롯 찾기
    /// </summary>
    /// <param name="equipmentType"></param>
    /// <returns></returns>
    private EquipSlot FindSlotByEquipmentType(EquipmentType equipmentType)
    {
        foreach (var slot in _slots)
        {
            if (slot.EquipmentType == equipmentType)
            {
                return slot;
            }
        }
        return null;
    }

    /// <summary>
    /// 인벤토리에서 아이템 획득 시 아이템이 알아서 착용되는 스크립트.
    /// 만약 크기가 맞지 않아 인벤토리에 아이템이 못 들어갈 시에는 작동하지 않음.
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool QuickEquipItem(InventoryItemPrefab item)
    {
        if (_equipmentManager == null)
        {
            Debug.LogError("[EquipInventory] QuickEquipItem: EquipmentManager가 null입니다!");
            return false;
        }

        EquipmentType type = item.Data.EquipmentType;
        foreach (var slot in _slots)
        {
            if (item.Data.EquipmentType == slot.EquipmentType)
            {
                if (slot.HasItem)
                {
                    if (!_inventory.GetEquipItem(slot))
                    {
                        return false;
                    }
                }
                _itemVisualizeField.AddItem(item);
                item.transform.position = transform.position;
                slot.GetItemPrefab(item);
                item.gameObject.SetActive(false);
                _equipmentManager.EquipItem(item.Data);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 인벤토리에 없는 아이템은 따로 InventoryPrefab이 없기에 만듬
    /// 솔직히 필요한지 모르곘음
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool QuickDropItemEquip(ItemData data)
    {
        if (_equipmentManager == null)
        {
            Debug.LogError("[EquipInventory] QuickDropItemEquip: EquipmentManager가 null입니다!");
            return false;
        }

        EquipmentType type = data.EquipmentType;
        foreach (var slot in _slots)
        {
            if (data.EquipmentType == slot.EquipmentType)
            {
                if (slot.HasItem)
                {
                    if (!_inventory.GetEquipItem(slot))
                    {
                        return false;
                    }
                }
                InventoryItemPrefab item = Instantiate(_itemPrefab, _itemVisualizeField.transform);
                item.Spawn(data, slot, slot, _inventory.ItemSize);
                _itemVisualizeField.AddItem(item);
                slot.GetItemPrefab(item);
                item.gameObject.SetActive(false);
                _equipmentManager.EquipItem(item.Data);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 슬롯이 지금 입고 있는 아이템을 벗어주는 함수
    /// </summary>
    /// <param name="slot"></param>
    public void TakeOffEquip(EquipSlot slot)
    {
        if (!_inventory.GetEquipItem(slot))
        {
            return;
        }

        if (_equipmentManager == null)
        {
            Debug.LogError("[EquipInventory] TakeOffEquip: EquipmentManager가 null입니다!");
            return;
        }

        _equipmentManager.UnequipItem(slot.EquipmentType);
        slot.RemoveData();

    }

    /// <summary>
    /// 현재 슬롯이 데이터가 들어 갈 수 있나 확인하는 함수
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool CheckCurrentSlot(ItemData data)
    {
        if (_currentSlot == null || _currentSlot.EquipmentType != data.EquipmentType) return false;
        else
        {
            return true;
        }
    }

    public void SetCurrentSlot(EquipSlot slot)
    {
        _currentSlot = slot;
    }

    public void OnBeginDrag(InventoryItemPrefab item, Slot ownerSlot)
    {
        _itemEventHandler.OnBeginDrag(item, ownerSlot);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _itemEventHandler.OnDrag(eventData);
    }

    public void OnEndDrag(InventoryItemPrefab item)
    {
        _itemEventHandler.OnEndDrag(item);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _itemEventHandler.ChangeMousePos(MousePos.EquipInventory);
    }

}
