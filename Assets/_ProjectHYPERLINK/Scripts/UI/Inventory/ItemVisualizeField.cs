using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 아이템 시각화 필드
/// 
/// 역할:
/// - 모든 InventoryItemPrefab의 부모 컨테이너
/// - 아이템 프리팹 리스트 관리
/// - InventoryItemPrefab → InventoryItemEventHandler 이벤트 중계
/// 
/// 생명주기:
/// - 씬 로드 시 생성
/// - 아이템 추가: AddItem()
/// - 드래그 이벤트 중계
/// </summary>
public class ItemVisualizeField : MonoBehaviour
{
    [SerializeField] private List<InventoryItemPrefab> _items;
    [SerializeField] private InventoryItemEventHandler _itemEventHandler;

    /// <summary>
    /// 새 아이템 프리팹 추가
    /// 
    /// 호출: ItemInventory.GetItem()
    /// 
    /// 처리:
    /// 1. 리스트에 추가
    /// 2. 이 필드를 아이템의 부모로 설정
    /// </summary>
    public void AddItem(InventoryItemPrefab item)
    {
        _items.Add(item);
        item.SetVisualField(this);
    }

    /// <summary>
    /// 드래그 시작 이벤트 중계
    /// 
    /// 호출 체인:
    /// InventoryItemPrefab.OnBeginDrag()
    /// → ItemVisualizeField.OnBeginDrag() (여기)
    /// → InventoryItemEventHandler.OnBeginDrag()
    /// </summary>
    public void OnBeginDrag(InventoryItemPrefab item, Slot ownerSlot)
    {
        _itemEventHandler.OnBeginDrag(item, ownerSlot);
    }

    /// <summary>
    /// 드래그 중 이벤트 중계
    /// 
    /// 호출 체인:
    /// InventoryItemPrefab.OnDrag()
    /// → ItemVisualizeField.OnDrag() (여기)
    /// → InventoryItemEventHandler.OnDrag()
    /// 
    /// 매 프레임 호출
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        _itemEventHandler.OnDrag(eventData);
    }

    /// <summary>
    /// 드래그 종료 이벤트 중계
    /// 
    /// 호출 체인:
    /// InventoryItemPrefab.OnEndDrag()
    /// → ItemVisualizeField.OnEndDrag() (여기)
    /// → InventoryItemEventHandler.OnEndDrag()
    /// </summary>
    public void OnEndDrag(PointerEventData eventData, InventoryItemPrefab item)
    {
        _itemEventHandler.OnEndDrag(eventData, item);
    }
}
