using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryBackGround : MonoBehaviour, IPointerExitHandler
{
    [Header("참조")]
    [SerializeField] InventoryItemEventHandler _itemEventHandler;


    public void OnPointerExit(PointerEventData eventData)
    {
        _itemEventHandler.ChangeMousePos(MousePos.None);
    }
}
