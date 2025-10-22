using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 아이템 프리팹
/// 
/// 역할:
/// - 인벤토리 그리드에 표시되는 아이템 UI
/// - 유저 입력 감지 (호버, 드래그)
/// - 아이템 크기에 맞춰 동적 크기 조정
/// 
/// Unity 이벤트 인터페이스:
/// - IPointerEnterHandler: 마우스 호버 시작
/// - IPointerExitHandler: 마우스 호버 종료
/// - IBeginDragHandler: 드래그 시작
/// - IDragHandler: 드래그 중
/// - IEndDragHandler: 드래그 종료
/// 
/// 생성 시점:
/// - ItemInventory.GetItem()
/// - ItemInventory.LoadItemToSlot()
/// 
/// 부모: ItemVisualizeField
/// 생명주기: Instantiate → Spawn → (드래그) → Destroy
/// </summary>
public class InventoryItemPrefab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform _rect;
    [SerializeField] private ItemData _data;
    [SerializeField] private ItemVisualizeField _visualizeField;
    [SerializeField] private Slot _ownerSlot;
    [SerializeField] private Image _image;
    private float _slotSize;

    public RectTransform Rect => _rect;
    public Slot OwnerSlot => _ownerSlot;
    public ItemData Data => _data;
    public Image Icon => _image;

    /// <summary>
    /// 아이템 프리팹 초기화
    /// 
    /// 호출: ItemInventory.GetItem()
    /// 
    /// 처리:
    /// 1. ItemData 저장
    /// 2. 첫 슬롯과 마지막 슬롯 위치로 중앙 계산
    /// 3. 아이템 크기에 맞춰 RectTransform 크기 조정
    /// 4. 위치 설정
    /// 
    /// 크기 계산:
    /// - 1x1 아이템: 1 슬롯 크기
    /// - 1x2 아이템: 2 슬롯 크기
    /// - 2x2 아이템: 4 슬롯 크기
    /// </summary>
    public void Spawn(ItemData data, Slot firstSlot, Slot lastSlot, float slotSize)
    {
        _slotSize = slotSize;
        _data = data;

        // 첫 슬롯과 마지막 슬롯의 중간 위치
        Vector2 firstPos = firstSlot.transform.position;
        Vector2 lastPos = lastSlot.transform.position;
        Vector2 newPos = (firstPos + lastPos) / 2f;

        _ownerSlot = firstSlot;

        // 아이템 크기 설정
        float xsize = _data.GridSize.x * _slotSize;
        float ysize = _data.GridSize.y * _slotSize;
        _rect.sizeDelta = new Vector2(xsize, ysize);

        transform.position = newPos;
    }

    /// <summary>
    /// 드래그 후 새 위치로 이동
    /// 
    /// 호출: ItemInventory.OnDrop()
    /// 
    /// Spawn과 동일한 로직이지만 ItemData는 변경 안 함
    /// </summary>
    public void ChangePos(Slot firstSlot, Slot lastSlot)
    {
        _ownerSlot = firstSlot;
        Vector2 firstPos = firstSlot.transform.position;
        Vector2 lastPos = lastSlot.transform.position;
        Vector2 newPos = (firstPos + lastPos) / 2f;
        transform.position = newPos;
    }

    /// <summary>
    /// ItemVisualizeField 참조 설정
    /// 이벤트 전달용
    /// </summary>
    public void SetVisualField(ItemVisualizeField visual)
    {
        _visualizeField = visual;
    }

    #region Unity 이벤트 인터페이스

    /// <summary>
    /// 마우스 호버 시작
    /// TODO: 아이템 툴팁 표시
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("설명 시작");
    }

    /// <summary>
    /// 마우스 호버 종료
    /// TODO: 아이템 툴팁 숨김
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("설명 끝");
    }

    /// <summary>
    /// 드래그 시작
    /// 원본 이미지 숨기고 DraggingVisualizeItem 활성화
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("시작");
        _visualizeField.OnBeginDrag(this, OwnerSlot);
        _image.enabled = false;
    }

    /// <summary>
    /// 드래그 중
    /// 매 프레임 호출
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        _visualizeField.OnDrag(eventData);
    }

    /// <summary>
    /// 드래그 종료
    /// 원본 이미지 다시 표시
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("끝");
        _visualizeField.OnEndDrag(eventData, this);
        _image.enabled = true;
    }

    #endregion
}
