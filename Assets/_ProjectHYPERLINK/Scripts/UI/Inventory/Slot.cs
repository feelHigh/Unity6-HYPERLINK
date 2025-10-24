using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬롯 기본 클래스
/// 
/// 역할:
/// - 모든 슬롯 타입의 기본이 되는 추상 클래스
/// - 아이템 보유 상태 관리
/// - 아이템 데이터 저장
/// - UI 아이콘 참조
/// 
/// 상속 클래스:
/// - InventorySlot: 인벤토리 그리드 슬롯 (10x6)
/// 
/// 디자인 패턴:
/// - 템플릿 메서드 패턴 (GetData는 하위 클래스에서 구현)
/// 
/// 사용 예시:
/// - 인벤토리 슬롯
/// - 장비 슬롯 (추후 확장)
/// - 퀵슬롯 (추후 확장)
/// </summary>
public abstract class Slot : MonoBehaviour
{
    [SerializeField] protected bool _hasItem = false;
    [SerializeField] protected ItemData _data;
    [SerializeField] protected Image _icon;

    /// <summary>
    /// 슬롯에 아이템이 있는지 여부
    /// </summary>
    public bool HasItem => _hasItem;

    /// <summary>
    /// 슬롯의 아이템 데이터
    /// null이면 빈 슬롯
    /// </summary>
    public ItemData Data => _data;

    /// <summary>
    /// 슬롯 아이콘 이미지
    /// UI 표시용
    /// </summary>
    public Image Icon => _icon;

    /// <summary>
    /// 슬롯에 아이템 데이터 할당
    /// 하위 클래스에서 구체적 구현 필요
    /// </summary>
    public abstract void GetData(ItemData data);

    public void RemoveData()
    {
        if (_data != null) _data = null;
    }

    /// <summary>
    /// 슬롯에서 아이템 제거하고 데이터 반환
    /// 
    /// 사용 케이스:
    /// - 드래그 앤 드롭으로 아이템 이동
    /// - 아이템 장착
    /// - 아이템 판매/버리기
    /// </summary>
    public ItemData ReturnDataAndRemove()
    {
        ItemData data = _data;
        _data = null;
        return data;
    }
}
