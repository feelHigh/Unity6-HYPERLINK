using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

[System.Serializable]
public class EquipSlot : Slot, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    
    [SerializeField] EquipmentType _equipType;
    [SerializeField] InventoryItemPrefab _itemPrefab;

    EquipInevnetory _equipInevnetory;

    public EquipmentType EquipmentType => _equipType;
    public InventoryItemPrefab ItemPrefab => _itemPrefab;

    /// <summary>
    /// 생성 될 때, 인벤토리를 알려주는 함수
    /// </summary>
    /// <param name="equipInventory"></param>
    public void Initialize(EquipInevnetory equipInventory)
    {
        _equipInevnetory = equipInventory;
    }

    /// <summary>
    /// 데이터를 받아와, 설정 해 주는 함수
    /// </summary>
    /// <param name="data"></param>
    public override void GetData(ItemData data)
    {
        _data = data;
        _hasItem = true;
        _icon.sprite = _data.ItemIcon;
        _icon.gameObject.SetActive(true);
    }

    /// <summary>
    /// 아이템을 받아와 저장 및 데이터를 받아오는 함수
    /// </summary>
    /// <param name="prefab"></param>
    public void GetItemPrefab(InventoryItemPrefab prefab)
    {
        _itemPrefab = prefab;
        GetData(prefab.Data);
    }

    /// <summary>
    /// 데이터 제거 및 아이템 제거 함수
    /// </summary>
    public void RemoveData()
    {
        _hasItem = false;
        _data = null;
        _itemPrefab = null;
        _icon.gameObject.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_itemPrefab == null) return;
        _equipInevnetory.OnBeginDrag(_itemPrefab, this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_itemPrefab == null) return;
        _equipInevnetory.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_itemPrefab == null) return;
        _equipInevnetory.OnEndDrag(_itemPrefab);
    }

    

    public void OnPointerEnter(PointerEventData eventData)
    {
        _equipInevnetory.SetCurrentSlot(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _equipInevnetory.SetCurrentSlot(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right && _itemPrefab == null) return;
        _equipInevnetory.TakeOffEquip(this);
    }
}
