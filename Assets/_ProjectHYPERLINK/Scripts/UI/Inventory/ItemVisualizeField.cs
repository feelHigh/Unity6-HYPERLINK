using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemVisualizeField : MonoBehaviour
{
    [SerializeField] List<InventoryItemPrefab> _items;
    [SerializeField] InventoryItemEventHandler _itemEventHandler;
    
    public void AddItem(InventoryItemPrefab item)
    {
        _items.Add(item);
        item.SetVisualField(this);
    }

    public void OnBeginDrag(InventoryItemPrefab item,Slot ownerSlot)
    {
        _itemEventHandler.OnBeginDrag(item, ownerSlot);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _itemEventHandler.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData, InventoryItemPrefab item)
    {
        _itemEventHandler.OnEndDrag(eventData, item);
    }
}
