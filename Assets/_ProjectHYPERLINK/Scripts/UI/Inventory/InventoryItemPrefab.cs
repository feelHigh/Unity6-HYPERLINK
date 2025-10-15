using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemPrefab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] RectTransform _rect;
    [SerializeField] ItemData _data;
    [SerializeField] ItemVisualizeField _visualizeField;
    [SerializeField] Slot _ownerSlot;
    [SerializeField] Image _image;
    float _slotSize;
    
    public RectTransform Rect => _rect;
    public Slot OwnerSlot => _ownerSlot; 
    public ItemData Data => _data;
    public Image Icon => _image;

    public void Spawn(ItemData data,Slot firstSlot, Slot lastSlot, float slotSize)
    {
        _slotSize = slotSize;
        _data = data;
        Vector2 firstPos = firstSlot.transform.position;
        Vector2 lastPos = lastSlot.transform.position;
        Vector2 newPos = (firstPos +lastPos) /2f;
        _ownerSlot = firstSlot;
        float xsize = _data.GridSize.x * _slotSize;
        float ysize = _data.GridSize.y * _slotSize;
        _rect.sizeDelta = new Vector2(xsize,ysize);
        transform.position = newPos;
    }

    public void ChangePos(Slot firstSlot, Slot lastSlot)
    {
        _ownerSlot = firstSlot;
        Vector2 firstPos = firstSlot.transform.position;
        Vector2 lastPos = lastSlot.transform.position;
        Vector2 newPos = (firstPos + lastPos) / 2f;
        transform.position = newPos;
    }
    public void SetVisualField(ItemVisualizeField visuual)
    {
        _visualizeField = visuual;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("설명 시작");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("설명 끝");
        
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("시작");
        _visualizeField.OnBeginDrag(this, OwnerSlot);
        _image.enabled = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _visualizeField.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("끝");
        _visualizeField.OnEndDrag(eventData, this);
        _image.enabled = true;
    }
}
