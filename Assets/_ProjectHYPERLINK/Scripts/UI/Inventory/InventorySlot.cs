using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 슬롯
/// 
/// 역할:
/// - 10x6 그리드 인벤토리의 개별 슬롯
/// - 2D 좌표 시스템 (col, row)
/// - 아이템 크기 정보 저장
/// - 슬롯 색상 변경 (드래그 시 시각 피드백)
/// 
/// 좌표 시스템:
/// - X축 (col): 0~9 (가로 10칸)
/// - Y축 (row): 0~5 (세로 6칸)
/// - 총 60개 슬롯
/// 
/// 상태 색상:
/// - White: 기본 상태
/// - Green: 드래그 중 배치 가능
/// - Red: 드래그 중 배치 불가 (ItemInventory에서 처리)
/// 
/// 부모 클래스: Slot
/// 사용처: ItemInventory
/// </summary>
public class InventorySlot : Slot
{
    [SerializeField] private int _col;
    [SerializeField] private int _row;
    [SerializeField] private Vector2Int _size;

    /// <summary>
    /// 슬롯에 배치된 아이템의 크기
    /// GridSize와 동일 (1x1, 1x2, 2x2 등)
    /// </summary>
    public Vector2Int Size => _size;

    /// <summary>
    /// 슬롯의 2D 좌표
    /// (col, row) = (x, y)
    /// </summary>
    public Vector2Int Pos
    {
        get
        {
            return new Vector2Int(_col, _row);
        }
    }

    /// <summary>
    /// 슬롯 초기화
    /// ItemInventory.StartInitilize()에서 호출
    /// 
    /// 처리 순서:
    /// 1. 60개 슬롯 배열을 순회
    /// 2. 각 슬롯에 2D 좌표 할당
    /// </summary>
    public void Initialize(int col, int row)
    {
        _col = col;
        _row = row;
    }

    /// <summary>
    /// 슬롯에 아이템 데이터 할당
    /// Slot 추상 메서드 구현
    /// </summary>
    public override void GetData(ItemData data)
    {
        _data = data;
    }
    /// <summary>
    /// 슬롯에 아이템 배치/제거 상태 변경
    /// 
    /// 호출 시점:
    /// - 아이템 추가: ItemInventory.PlaceItem()
    /// - 아이템 제거: ItemInventory.PlaceItem()
    /// - 드래그 시작: ItemInventory.OnBeginDrag()
    /// 
    /// 색상은 항상 White로 리셋
    /// </summary>
    public void IGotItem(bool on)
    {
        _icon.color = Color.white;
        _hasItem = on;
    }

    /// <summary>
    /// 드래그 중 슬롯 색상 변경
    /// 
    /// 호출 시점:
    /// - ItemInventory.OnDrag()
    /// - ItemInventory.ChangeSlotColor()
    /// 
    /// 색상:
    /// - Green: 배치 가능
    /// - White: 기본 상태
    /// </summary>
    public void SetColor(bool on)
    {
        _icon.color = on ? Color.green : Color.white;
    }
}
