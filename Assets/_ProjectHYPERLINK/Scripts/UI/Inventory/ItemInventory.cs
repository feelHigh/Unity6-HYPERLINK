using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 아이템 인벤토리 (싱글톤 패턴)
/// GameCanvas에 통합됨
/// </summary>
public class ItemInventory : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region 싱글톤
    public static ItemInventory Instance { get; private set; }
    #endregion

    [Header("참조")]
    [SerializeField] InventoryItemEventHandler _itemEventHandler;

    [Header("아이템 오브젝트들이 보이는 실제 위치")]
    [SerializeField] ItemVisualizeField _itemVisualizeField;

    [Header("아이템 프리팹")]
    [SerializeField] InventoryItemPrefab _itemPrefab;

    [Header("슬롯 한 변의 길이")]
    [SerializeField] float _itemSize;

    [SerializeField] InventorySlot[] _inventory;
    [SerializeField] InventorySlot[,] _slots = new InventorySlot[10, 4];

    float _sideLength;
    Vector2 _startPosition = new Vector2();
    InventorySlot _currentSlot = null;

    public float ItemSize => _itemSize;
    void Awake()
    {
        // 싱글톤 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Initilize();
    }

    void Start()
    {
        // 자동 참조 검색
        if (_itemEventHandler == null)
        {
            _itemEventHandler = GetComponentInChildren<InventoryItemEventHandler>();
        }

        if (_itemVisualizeField == null)
        {
            _itemVisualizeField = GetComponentInChildren<ItemVisualizeField>();
        }

        // 슬롯 배열 검증
        if (_inventory == null || _inventory.Length != 40)
        {
            Debug.LogError("[ItemInventory] InventorySlot 배열이 60개여야 합니다!");
        }
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

    /// <summary>
    /// 아이템을 인벤토리에 추가
    /// ItemPickupManager에서 호출
    /// </summary>
    public bool GetItem(ItemData data)
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

                    Debug.Log($"[ItemInventory] 아이템 추가 성공: {data.ItemName}");
                    return true;
                }
            }
        }

        Debug.LogWarning($"[ItemInventory] 인벤토리 가득 찬 상태 - {data.ItemName} 추가 실패");
        return false;
    }

    /// <summary>
    /// 착용 장비를 받아와 인벤토리에 넣어보는 함수
    /// 실패시 False, 성공시 True 반환
    /// </summary>
    /// <param name="equipSlot"></param>
    /// <returns></returns>
    public bool GetEquipItem(EquipSlot equipSlot)
    {
        foreach (InventorySlot slot in _slots)
        {
            if (!slot.HasItem)
            {
                if (ChekCanDrop(equipSlot.Data, slot))
                {
                    Vector2Int pos = slot.Pos;
                    Vector2Int size = equipSlot.Data.GridSize;
                    InventorySlot lastSlot = _slots[pos.x + size.x - 1, pos.y + size.y - 1];
                    InventoryItemPrefab item = equipSlot.ItemPrefab;
                    item.ChangePos(slot, lastSlot);
                    item.gameObject.SetActive(true);
                    PlaceItem(equipSlot.Data, slot, true);
                    equipSlot.RemoveData();
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 저장용: 모든 아이템 데이터 반환
    /// CharacterDataManager에서 호출
    /// </summary>
    public List<(ItemData data, int slotIndex)> GetAllItems()
    {
        List<(ItemData, int)> items = new List<(ItemData, int)>();

        for (int i = 0; i < _inventory.Length; i++)
        {
            if (_inventory[i].HasItem && _inventory[i].Data != null)
            {
                items.Add((_inventory[i].Data, i));
            }
        }

        return items;
    }

    /// <summary>
    /// 로드용: 특정 슬롯에 아이템 직접 배치
    /// CharacterDataManager에서 호출
    /// </summary>
    public bool LoadItemToSlot(ItemData data, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _inventory.Length)
        {
            Debug.LogError($"[ItemInventory] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }

        // 슬롯을 2D 좌표로 변환 (10x6 그리드)
        int x = slotIndex % 10;
        int y = slotIndex / 10;

        if (x >= _slots.GetLength(0) || y >= _slots.GetLength(1))
        {
            Debug.LogError($"[ItemInventory] 슬롯 좌표 범위 초과: ({x}, {y})");
            return false;
        }

        InventorySlot slot = _slots[x, y];

        // 아이템 배치 가능 확인
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

            Debug.Log($"[ItemInventory] 로드 성공: {data.ItemName} → 슬롯 {slotIndex}");
            return true;
        }

        Debug.LogWarning($"[ItemInventory] 로드 실패: {data.ItemName} → 슬롯 {slotIndex} (공간 부족)");
        return false;
    }

    /// <summary>
    /// 인벤토리 초기화 (로드 전 호출)
    /// </summary>
    public void ClearInventory()
    {
        // 모든 아이템 프리팹 제거
        if (_itemVisualizeField != null)
        {
            foreach (Transform child in _itemVisualizeField.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // 모든 슬롯 초기화
        foreach (InventorySlot slot in _inventory)
        {
            slot.GetData(null);
            slot.IGotItem(false);
        }

        Debug.Log("[ItemInventory] 인벤토리 초기화 완료");
    }

    public void PlaceItem(ItemData data, InventorySlot slot, bool get)
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

    public bool CheckCurrentSlot(ItemData data)
    {
        if (_currentSlot == null) return false;
        return ChekCanDrop(data, _currentSlot);
    }
    public void OnBeginDrag(InventorySlot slot)
    {
        PlaceItem(slot.Data, slot, false);
    }

    /// <summary>
    /// 드래그 중, 슬롯에서 해당 부분에 아이템을 넣을 수 있나 확인하는 함수
    ///
    /// </summary>
    /// <param name="data"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public ItemDragState OnDrag(ItemData data, Vector2 pos)
    {
        Vector2Int size = data.GridSize;
        int x = -Mathf.RoundToInt((_startPosition.x - pos.x) / _sideLength);
        int y = Mathf.RoundToInt((_startPosition.y - pos.y) / _sideLength);

        if (x < 0 || x > _slots.GetLength(0) - size.x || y < 0 || y > _slots.GetLength(1) - size.y)
        {
            if (_currentSlot != null)
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

    public void OnDrop(InventoryItemPrefab item)
    {
        PlaceItem(item.Data, _currentSlot, true);
        Vector2Int pos = _currentSlot.Pos;
        Vector2Int size = _currentSlot.Data.GridSize;
        InventorySlot lastSlot = _slots[pos.x + size.x - 1, pos.y + size.y - 1];
        item.gameObject.SetActive(true);
        item.ChangePos(_currentSlot, lastSlot);
    }

    public void ReturnItem(InventoryItemPrefab item, InventorySlot slot)
    {
        PlaceItem(item.Data, slot, true);
        Vector2Int pos = slot.Pos;
        Vector2Int size = slot.Data.GridSize;
        InventorySlot lastSlot = _slots[pos.x + size.x - 1, pos.y + size.y - 1];
        item.ChangePos(slot, lastSlot);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        _itemEventHandler.ChangeMousePos(MousePos.ItemInventory);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //_itemEventHandler.ChangeMousePos(MousePos.None);
    }
}
