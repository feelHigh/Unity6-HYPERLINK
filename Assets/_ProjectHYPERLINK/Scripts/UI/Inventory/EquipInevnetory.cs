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

    public EquipSlot CurrentSlot => _currentSlot;

    void Start()
    {
        _equipmentManager = GameObject.FindGameObjectWithTag("Player").GetComponent<EquipmentManager>();
        Initialize();
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
        foreach(var slot in _slots)
        {
            if(slot.EquipmentType == item.Data.EquipmentType)
            {
                _equipmentManager.UnequipItem(item.Data.EquipmentType);
                slot.RemoveData();
            }
        }
    }

    /// <summary>
    /// 아이템을 받아와 착용시도,
    /// 실패시 False, 성공시 True
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool EquipItem(InventoryItemPrefab item)
    {
        if (_currentSlot.HasItem)
        {
            if (!_inventory.GetEquipItem(_currentSlot))
            {
                return false;
            }
        }
        _currentSlot.GetItemPrefab(item);
        item.transform.position = _currentSlot.transform.position;
        item.gameObject.SetActive(false);
        _equipmentManager.EquipItem(item.Data);
        return true;
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
        EquipmentType type = item.Data.EquipmentType;
        foreach(var slot in _slots)
        {
            if(item.Data.EquipmentType == slot.EquipmentType)
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
        if(_currentSlot == null || _currentSlot.EquipmentType != data.EquipmentType) return false;
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
        _itemEventHandler.OnBeginDrag(item,ownerSlot);
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
