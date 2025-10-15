using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DraggingVisualizeItem : MonoBehaviour
{
    [SerializeField] Image _image;
    [SerializeField] RectTransform _rect;
    public Vector2 CheckPos { 
        get{
            Vector3[] coners = new Vector3[4];
            _rect.GetWorldCorners(coners);
            return coners[1];
        } 
    }
    public void Spawn(InventoryItemPrefab item)
    {
        _image.sprite = item.Icon.sprite;
        _image.rectTransform.sizeDelta = item.Icon.rectTransform.sizeDelta;
    }

   
}
