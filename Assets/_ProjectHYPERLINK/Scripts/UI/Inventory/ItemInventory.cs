using System.Collections;
using UnityEngine;

/// <summary>
/// 아이템 인벤토리
/// </summary>
public class ItemInventory : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] InventoryItemEventHandler _itemEventHandler;

    [Header("아이템 오브젝트들이 보이는 실제 위치")]
    [SerializeField] ItemVisualizeField _itemVisualizeField;

    [Header("아이템 프리팹")]
    [SerializeField] InventoryItemPrefab _itemPrefab;

    [Header("슬롯 한 변의 길이")]
    [SerializeField] float _itemSize;

    // SerializeFiled 지울 예정
    [SerializeField] InventorySlot[] _inventory;
    [SerializeField] InventorySlot[,] _slots = new InventorySlot[10, 6];

    // 인벤토리 한 변의 길이를 저장하는 함수
    float _sideLength;

    // 아이템 이동에 필요한 인벤토리의 맨 왼쪽 윗 부분의 위치
    Vector2 _startPosition = new Vector2();

    InventorySlot _currentSlot = null;

    void Awake()
    {
        Initilize();
    }

    public void Initilize()
    {
        StartCoroutine(StartInitilize());
    }

    IEnumerator StartInitilize()
    {
        int num = 0;
        for (int i = 0; i < _slots.GetLength(0); i++)
        {

            for (int j = 0; j < _slots.GetLength(1); j++)
            {
                _slots[i, j] = _inventory[num++];

                _slots[i, j].Initialize(i, j);
            }
        }
        foreach (InventorySlot slot in _slots)
        {
            if (slot.HasItem)
            {
                Vector2Int pos = slot.Pos;
                for (int i = 0; i < slot.Size.x; i++)
                {
                    for (int j = 0; j < slot.Size.y; j++)
                    {
                        _slots[pos.x + i, pos.y + j].IGotItem(true);
                    }
                }
            }
        }
        yield return new WaitForEndOfFrame();

        Vector3[] coners = new Vector3[4];
        _inventory[0].Icon.rectTransform.GetWorldCorners(coners);
        _sideLength = coners[3].x - coners[1].x;
        _startPosition = coners[1];
    }

    public void GetItem(ItemData data)
    {
        foreach (InventorySlot slot in _slots)
        {
            if (!slot.HasItem)
            {
                if (ChekCanDrop(data, slot))
                {
                    InventoryItemPrefab item = Instantiate(_itemPrefab, _itemVisualizeField.transform);
                    _itemVisualizeField.AddItem(item);
                    Vector2Int pos = slot.Pos;
                    Vector2Int size = data.GridSize;
                    InventorySlot lastSlot = _slots[pos.x + size.x - 1, pos.y + size.y - 1];
                    item.Spawn(data, slot, lastSlot, _itemSize);
                    slot.GetData(data);
                    PlaceItem(data, slot, true);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 아이템 데이터와 아이템 슬롯을 받아, 아이템 데이터를 기반으로 bool 값에 따라 아이템 제거 또는 부여를함
    /// </summary>
    /// <param name="data">전달 되는 값 (이거 없으면 크기를 모름)</param>
    /// <param name="slot">값이 변경되는 슬롯</param>
    /// <param name="state">아이템이 들어오는지, 나가는지</param>
    void PlaceItem(ItemData data, InventorySlot slot, bool get)
    {
        if (get) slot.GetData(data);
        Vector2Int pos = slot.Pos;
        Vector2Int size = data.GridSize;

        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                _slots[pos.x + i, pos.y + j].IGotItem(get);
            }
        }
    }

    void ChangeSlotColor(ItemData data, InventorySlot slot, bool get)
    {
        Vector2Int pos = slot.Pos;
        Vector2Int size = data.GridSize;
        for (int i = pos.x; i < pos.x + size.x; i++)
        {
            if (i >= _slots.GetLength(0)) break;
            for (int j = pos.y; j < pos.y + size.y; j++)
            {
                if (j >= _slots.GetLength(1)) break;
                _slots[i, j].SetColor(get);
            }
        }
    }

    bool ChekCanDrop(ItemData data, InventorySlot slot)
    {
        Vector2Int pos = slot.Pos;
        Vector2Int size = data.GridSize;
        if (pos.y + size.y > _slots.GetLength(1) || pos.x + size.x > _slots.GetLength(0)) return false;

        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                if (_slots[pos.x + i, pos.y + j].HasItem)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void OnBeginDrag(Slot originSlot)
    {
        ItemData data = originSlot.Data;
        if (data == null) return;
        if (originSlot is InventorySlot slot)
            PlaceItem(slot.Data, slot, false);
    }

    public ItemDragState OnDrag(ItemData data, Vector2 pos)
    {
        Vector2Int size = data.GridSize;
        int x = -Mathf.RoundToInt((_startPosition.x - pos.x) / _sideLength);
        int y = Mathf.RoundToInt((_startPosition.y - pos.y) / _sideLength);
        if (x < 0 || x > _slots.GetLength(0)- size.x || y < 0 || y > _slots.GetLength(1)-size.y)
        {
            if(_currentSlot != null)
            {
                ChangeSlotColor(data, _currentSlot, false);
                _currentSlot = null;
            }

            return ItemDragState.Impossible;
        }

        if (_currentSlot == _slots[x, y])
        {
            return ItemDragState.Same;
        }
        if (_currentSlot != null)
        {
            ChangeSlotColor(data, _currentSlot, false);
        }
        _currentSlot = _slots[x, y];

        if (ChekCanDrop(data, _currentSlot))
        {
            Vector2Int slotPos = _currentSlot.Pos;
            for (int i = 0; i < size.x; i++)
            {
                for (int j = 0; j < size.y; j++)
                {
                    if (_slots[slotPos.x + i, slotPos.y + j].HasItem)
                    {
                        return ItemDragState.Impossible;
                    }
                }
            }
            ChangeSlotColor(data, _currentSlot, true);
            return ItemDragState.Possible;
        }

        return ItemDragState.Impossible;
    }

    public void OnDrop(Vector2 pos, Slot ownerSlot, InventoryItemPrefab item)
    {
        if (ownerSlot is InventorySlot slot)
        {
            if (_currentSlot != null)
            {
                if (ChekCanDrop(slot.Data, _currentSlot))
                {
                    PlaceItem(slot.Data, slot, false);
                    PlaceItem(slot.ReturnDataAndRemove(), _currentSlot, true);
                    Vector2Int posx = _currentSlot.Pos;
                    Vector2Int size = _currentSlot.Data.GridSize;
                    InventorySlot lastSlot = _slots[posx.x + size.x - 1, posx.y + size.y - 1];
                    item.ChangePos(_currentSlot, lastSlot);
                    return;
                }
            }
            PlaceItem(slot.Data, slot, true);
        }
    }
}
