using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그 중인 아이템 시각화
/// 
/// 역할:
/// - 마우스 커서를 따라다니는 아이템 이미지
/// - 배치 가능 여부 시각 피드백 (색상 변경)
/// - 드래그 중 임시 표시용
/// 
/// 동작 과정:
/// 1. OnBeginDrag: 원본 아이템 정보 복사, 활성화
/// 2. OnDrag: 마우스 위치 추적, 색상 변경
/// 3. OnEndDrag: 비활성화
/// 
/// 색상 피드백:
/// - White: 배치 가능
/// - Red: 배치 불가 (슬롯 초과, 중복 등)
/// 
/// 생명주기:
/// - 씬에 항상 존재 (비활성 상태)
/// - 드래그 시에만 활성화
/// - 드래그 종료 시 비활성화
/// 
/// 사용처: InventoryItemEventHandler
/// </summary>
public class DraggingVisualizeItem : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private RectTransform _rect;

    /// <summary>
    /// 드래그 중인 아이템의 좌측 하단 위치
    /// 
    /// 사용 목적:
    /// - ItemInventory.OnDrag()에서 슬롯 인덱스 계산
    /// - 그리드 좌표 변환용
    /// 
    /// 계산 방법:
    /// - RectTransform의 WorldCorners[1] (좌측 하단)
    /// </summary>
    public Vector2 CheckPos
    {
        get
        {
            Vector3[] coners = new Vector3[4];
            _rect.GetWorldCorners(coners);
            return coners[1]; // 좌측 하단
        }
    }

    /// <summary>
    /// 드래그 시작 시 호출
    /// 원본 아이템의 스프라이트와 크기 복사
    /// 
    /// 호출: InventoryItemEventHandler.OnBeginDrag()
    /// </summary>
    public void Spawn(InventoryItemPrefab item)
    {
        _image.sprite = item.Icon.sprite;
        _image.rectTransform.sizeDelta = item.Icon.rectTransform.sizeDelta;
    }

    /// <summary>
    /// 배치 가능 여부에 따라 색상 변경
    /// 
    /// 호출: InventoryItemEventHandler.OnDrag()
    /// 
    /// 색상:
    /// - true: White (배치 가능)
    /// - false: Red (배치 불가)
    /// </summary>
    public void ChangeColor(bool on)
    {
        _image.color = on ? Color.white : Color.red;
    }
}
