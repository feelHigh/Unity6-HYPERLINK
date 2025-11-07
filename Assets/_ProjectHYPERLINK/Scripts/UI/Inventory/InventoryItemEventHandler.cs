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
    EquipInventory,
    Shop,
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
    [SerializeField] private EquipInventory _equipInventory;
    [SerializeField] private ShopUI _shop;

    [Header("Prefab")]
    [SerializeField] private DraggingVisualizeItem _dragItem;
    [SerializeField] private Slot _ownerSlot;

    private MousePos _currentMousePos;
    private InventoryItemPrefab _item;


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
        _item = item;
        item.Icon.enabled = false;
        _ownerSlot = ownerSlot;
        _dragItem.gameObject.SetActive(true);
        _dragItem.Spawn(item);
        if (ownerSlot is InventorySlot slot)
            _inventory.OnBeginDrag(slot);
        _inventory.SetStartPos();
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

        switch (_currentMousePos)
        {
            case MousePos.None:
                _dragItem.ChangeColor(false);
                break;
            case MousePos.ItemInventory:
                InventoryOnDrag();
                break;
            case MousePos.EquipInventory:
                EquipOnDrag();
                break;
            case MousePos.Shop:
                _dragItem.ChangeColor(true);
                break;
        }
    }

    /// <summary>
    /// 인벤토리 위에서 드래그 중일 떄, 작동하여 못 들어가면 빨강, 들어 갈 수 있으면 하얀색으로 바꿔주는 함수
    /// </summary>
    void InventoryOnDrag()
    {
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
    /// 착용 아이템이 들어 갈 수 없으면 색을 빨강으로 바꿔주는 함수
    /// </summary>
    void EquipOnDrag()
    {
        if (_equipInventory.CurrentSlot == null || _equipInventory.CurrentSlot.EquipmentType != _ownerSlot.Data.EquipmentType)
        {
            _dragItem.ChangeColor(false);

        }
        else
        {
            _dragItem.ChangeColor(true);
        }
    }

    /// <summary>
    /// 드래그 종료
    /// 
    /// 호출: ItemVisualizeField.OnEndDrag()
    /// 
    /// 처리:
    /// 현재 마우스 위치에 따라 다른 로직 실행
    /// 
    /// 1. None
    /// 아무것도 안함
    ///
    /// 2. ItemInventory
    /// 아이템 인벤토리에서 해당 슬롯에 아이템이 들어가나 확인
    ///
    /// 3. EquipInventory
    /// 착용장비 슬롯에 해당 아이템이 들어가나 확인
    /// 
    /// 수정사항:
    /// - EquipInventory 처리 시 CheckCurrentSlot 제거 (EquipItem 내부에서 처리)
    /// </summary>
    public void OnEndDrag(InventoryItemPrefab item)
    {
        bool giveItem = false;
        switch (_currentMousePos)
        {
            case MousePos.None:
                break;

            case MousePos.ItemInventory:
                if (_inventory.CheckCurrentSlot(item.Data))
                {
                    _inventory.OnDrop(item);
                    giveItem = true;
                    if (_ownerSlot is EquipSlot eslot1)
                    {
                        Debug.Log("[InventoryItemEventHandler] 장비 해제 후 인벤토리로 이동");
                        _equipInventory.UnEquipItem(item);
                    }
                    _ownerSlot.RemoveData();
                }
                break;

            case MousePos.EquipInventory:
                // EquipItem 메서드 내부에서 null 체크 및 슬롯 찾기를 처리하므로
                // CheckCurrentSlot 호출 불필요
                if (_equipInventory.EquipItem(item))
                {
                    giveItem = true;
                    _ownerSlot.RemoveData();
                    Debug.Log($"[InventoryItemEventHandler] 아이템 장착 성공: {item.Data.ItemName}");
                }
                else
                {
                    Debug.LogWarning($"[InventoryItemEventHandler] 아이템 장착 실패: {item.Data.ItemName}");
                }
                break;
            case MousePos.Shop:

                if (_ownerSlot is EquipSlot eslot2)
                {
                    _equipInventory.UnEquipItem(item);
                }
                else if (_ownerSlot is InventorySlot islot)
                {
                    _inventory.PlaceItem(item.Data, islot, false);
                }
                giveItem = true;
                _itemVisualizeField.RemoveItem(item);
                _shop.Sell(item.Data);
                Destroy(item.gameObject);
                break;
        }

        if (!giveItem)
        {
            if (_ownerSlot is InventorySlot islot)
            {
                _inventory.ReturnItem(item, islot);
            }
        }
        _dragItem.gameObject.SetActive(false);
        item.Icon.enabled = true;
        _ownerSlot = null;
        _item = null;
    }

    /// <summary>
    /// 마우스의 위치를 Update해주는 함수
    /// </summary>
    /// <param name="pos"></param>
    public void ChangeMousePos(MousePos pos)
    {
        _currentMousePos = pos;
    }

    public void InventoryClosed()
    {
        if (_item == null) return;
        OnEndDrag(_item);
    }

    public bool Buy(ItemData item, PlayerCharacter player)
    {
        if (!player.HasGold(item.Gold))
        {
            return false;
        }
        if (!_inventory.GetItem(item))
        {
            return false;
        }
        player.RemoveGold(item.Gold);

        return true;
    }

    /// <summary>
    /// 인벤토리 아이템을 우클릭 시 착용 시켜주는 함수
    /// </summary>
    /// <param name="item"></param>
    public void OnRightClick(InventoryItemPrefab item)
    {
        if (item.OwnerSlot is InventorySlot slot)
        {
            _inventory.PlaceItem(item.Data, slot, false);
            if (!_equipInventory.QuickEquipItem(item))
            {

                _inventory.PlaceItem(item.Data, slot, true);
            }
        }
    }
}
