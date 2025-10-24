using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 인벤토리 아이템 드래그 상태
/// </summary>
public enum ItemDragState
{
    Possible,       // 배치 가능
    Impossible,     // 배치 불가 (슬롯 초과, 중복)
    Same           // 동일 슬롯 (이동 안 함)
}
public enum MousePos
{
    None,
    ItemInventory,
    EquipInventory
}

/// <summary>
/// 인벤토리 아이템 이벤트 핸들러
/// 
/// 역할:
/// - 아이템 드래그 이벤트 중계
/// - ItemVisualizeField → ItemInventory 연결
/// - DraggingVisualizeItem 제어
/// 
/// 이벤트 흐름:
/// 1. InventoryItemPrefab: 유저 입력 감지
/// 2. ItemVisualizeField: 이벤트 전달
/// 3. InventoryItemEventHandler: 중앙 처리 (이 클래스)
/// 4. ItemInventory: 로직 처리
/// 
/// 처리 과정:
/// - OnBeginDrag: 드래그 시작, DraggingVisualizeItem 활성화
/// - OnDrag: 마우스 위치 추적, 배치 가능 여부 확인
/// - OnEndDrag: 아이템 배치/복원
/// 
/// 디자인 패턴:
/// - 중재자 패턴 (Mediator)
/// - UI와 비즈니스 로직 분리
/// </summary>
public class InventoryItemEventHandler : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private ItemVisualizeField _itemVisualizeField;
    [SerializeField] private ItemInventory _inventory;

    [Header("Prefab")]
    [SerializeField] private DraggingVisualizeItem _dragItem;
    [SerializeField] private Slot _ownerSlot;

    // 임시
    [SerializeField] private MousePos _currentMousePos;

    /// <summary>
    /// 드래그 시작
    /// 
    /// 호출: ItemVisualizeField.OnBeginDrag()
    /// 
    /// 처리:
    /// 1. 원본 슬롯 저장
    /// 2. DraggingVisualizeItem 활성화
    /// 3. 원본 아이템 정보 복사
    /// 4. ItemInventory에 드래그 시작 알림
    /// </summary>
    public void OnBeginDrag(InventoryItemPrefab item, Slot ownerSlot)
    {
        _ownerSlot = ownerSlot;
        _dragItem.gameObject.SetActive(true);
        _dragItem.Spawn(item);
        _inventory.OnBeginDrag(_ownerSlot);
    }

    /// <summary>
    /// 드래그 중
    /// 
    /// 호출: ItemVisualizeField.OnDrag()
    /// 매 프레임 호출
    /// 
    /// 처리:
    /// 1. DraggingVisualizeItem을 마우스 위치로 이동
    /// 2. ItemInventory에서 배치 가능 여부 확인
    /// 3. 결과에 따라 색상 변경
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        _dragItem.transform.position = eventData.position;

        switch (_inventory.OnDrag(_ownerSlot.Data, _dragItem.CheckPos))
        {
            case ItemDragState.Possible:
                _dragItem.ChangeColor(true);
                break;
            case ItemDragState.Impossible:
                _dragItem.ChangeColor(false);
                break;
            case ItemDragState.Same:
                // 색상 변경 없음
                break;
        }
    }

    /// <summary>
    /// 드래그 종료
    /// 
    /// 호출: ItemVisualizeField.OnEndDrag()
    /// 
    /// 처리:
    /// 1. ItemInventory에 최종 위치 전달
    /// 2. 배치 가능: 아이템 이동
    /// 3. 배치 불가: 원래 위치로 복원
    /// 4. DraggingVisualizeItem 비활성화
    /// </summary>
    public void OnEndDrag(PointerEventData eventData, InventoryItemPrefab item)
    {
        _inventory.OnDrop(_dragItem.CheckPos, _ownerSlot, item);
        _dragItem.gameObject.SetActive(false);
    }

    /// <summary>
    /// 마우스의 위치를 Update해주는 함수
    /// </summary>
    /// <param name="pos"></param>
    public void ChangeMousePos(MousePos pos)
    {
        _currentMousePos = pos;
    }
}
