using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

[System.Serializable]
public class EquipSlot : Slot, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{

    [SerializeField] EquipmentType _equipType;
    [SerializeField] InventoryItemPrefab _itemPrefab;

    EquipInventory _equipInevnetory;

    public EquipmentType EquipmentType => _equipType;
    public InventoryItemPrefab ItemPrefab => _itemPrefab;

    /// <summary>
    /// 생성 될 때, 인벤토리를 알려주는 함수
    /// </summary>
    /// <param name="equipInventory"></param>
    public void Initialize(EquipInventory equipInventory)
    {
        _equipInevnetory = equipInventory;

        if (_equipInevnetory == null)
        {
            DebugHelper.LogError($"[EquipSlot] InitializeBoss 실패: equipInventory가 null입니다. 슬롯: {_equipType}");
        }
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
    /// 부모 클래스의 RemoveData()를 의도적으로 숨김 (EquipSlot은 추가로 _itemPrefab도 제거해야 함)
    /// </summary>
    public new void RemoveData()
    {
        _hasItem = false;
        _data = null;
        _itemPrefab = null;
        _icon.gameObject.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_itemPrefab == null) return;
        if (_equipInevnetory == null)
        {
            DebugHelper.LogError("[EquipSlot] OnBeginDrag: _equipInevnetory가 null입니다!");
            return;
        }
        _equipInevnetory.OnBeginDrag(_itemPrefab, this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_itemPrefab == null) return;
        if (_equipInevnetory == null) return;
        _equipInevnetory.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_itemPrefab == null) return;
        if (_equipInevnetory == null) return;
        _equipInevnetory.OnEndDrag(_itemPrefab);
    }



    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_equipInevnetory == null)
        {
            DebugHelper.LogError($"[EquipSlot] OnPointerEnter: _equipInevnetory가 null입니다! 슬롯: {_equipType}");
            return;
        }
        _equipInevnetory.SetCurrentSlot(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_equipInevnetory == null)
        {
            DebugHelper.LogError($"[EquipSlot] OnPointerExit: _equipInevnetory가 null입니다! 슬롯: {_equipType}");
            return;
        }
        _equipInevnetory.SetCurrentSlot(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right && _itemPrefab == null) return;
        if (_equipInevnetory == null) return;
        _equipInevnetory.TakeOffEquip(this);
    }
}
