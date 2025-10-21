using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public enum ItemDragState
{
    Possible,
    Impossible,
    Same
}
public class InventoryItemEventHandler : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] ItemVisualizeField _itemVisualizeField;
    [SerializeField] ItemInventory _inventory;

    [Header("Prefab")]
    [SerializeField] DraggingVisualizeItem _dragItem;
    [SerializeField] Slot _ownerSlot;


    public void OnBeginDrag(InventoryItemPrefab item,Slot onwerSlot)
    {
        _ownerSlot = onwerSlot;
        _dragItem.gameObject.SetActive(true);
        _dragItem.Spawn(item);
        _inventory.OnBeginDrag(_ownerSlot);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _dragItem.transform.position = eventData.position;
        switch (_inventory.OnDrag(_ownerSlot.Data, _dragItem.CheckPos)
)
        {
            case ItemDragState.Possible:
                _dragItem.ChangeColor(true);
                break;
            case ItemDragState.Impossible:
                _dragItem.ChangeColor(false);
                break;
            case ItemDragState.Same:
                break;
        }
    }

    public void OnEndDrag(PointerEventData eventData, InventoryItemPrefab item)
    {
        _inventory.OnDrop(_dragItem.CheckPos,_ownerSlot, item);
        _dragItem.gameObject.SetActive(false);
    }
}
