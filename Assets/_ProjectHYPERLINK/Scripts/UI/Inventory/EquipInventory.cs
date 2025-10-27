using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 장비 인벤토리 관리 시스템
/// 
/// 역할:
/// - 장비 아이템 착용/해제
/// - 드래그 앤 드롭 이벤트 처리
/// - EquipmentManager와 연동
/// 
/// 초기화:
/// - Start()에서 InvokeRepeating으로 EquipmentManager 검색
/// - PlayerSpawner로 플레이어가 스폰될 때까지 대기
/// </summary>
[Serializable]
public class EquipInventory : MonoBehaviour, IPointerEnterHandler
{
    [Header("참조")]
    [SerializeField] private InventoryItemEventHandler _itemEventHandler;
    [SerializeField] private ItemInventory _inventory;

    [Header("아이템 오브젝트들이 보이는 실제 위치")]
    [SerializeField] private ItemVisualizeField _itemVisualizeField;

    [Header("프리팹")]
    [SerializeField] private InventoryItemPrefab _itemPrefab;

    [Header("슬롯")]
    [SerializeField] private EquipSlot[] _slots;
    [SerializeField] private EquipSlot _currentSlot;

    [Header("자동 검색 설정")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _retryInterval = 0.5f;
    [SerializeField] private int _maxRetries = 20;
    [SerializeField] private bool _enableDebugLogs = true;

    private EquipmentManager _equipmentManager;
    private bool _isInitialized = false;
    private int _retryCount = 0;

    public EquipSlot CurrentSlot => _currentSlot;

    #region 초기화

    private void Start()
    {
        Initialize();
        InvokeRepeating(nameof(TryFindEquipmentManager), 0.1f, _retryInterval);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(TryFindEquipmentManager));
    }

    /// <summary>
    /// PlayerSpawner로 스폰된 플레이어를 찾기 위한 재시도 로직
    /// CharacterUIController와 동일한 방식
    /// </summary>
    private void TryFindEquipmentManager()
    {
        if (_isInitialized) return;

        _retryCount++;

        GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);

        if (playerObject != null)
        {
            _equipmentManager = playerObject.GetComponent<EquipmentManager>();

            if (_equipmentManager != null)
            {
                Log($"EquipmentManager 찾음: {playerObject.name} (시도: {_retryCount}회)");
                _isInitialized = true;
                CancelInvoke(nameof(TryFindEquipmentManager));
                return;
            }
        }

        if (_retryCount >= _maxRetries)
        {
            LogError($"EquipmentManager를 {_maxRetries}회 시도 후에도 찾지 못했습니다!");
            CancelInvoke(nameof(TryFindEquipmentManager));
        }
    }

    /// <summary>
    /// 슬롯 초기화
    /// </summary>
    public void Initialize()
    {
        foreach (var slot in _slots)
        {
            slot.Initialize(this);
        }

        Log("슬롯 초기화 완료");
    }

    #endregion

    #region 장비 착용/해제

    /// <summary>
    /// 아이템을 받아와 해당 아이템을 미착용 시켜주는 함수
    /// </summary>
    public void UnEquipItem(InventoryItemPrefab item)
    {
        if (_equipmentManager == null)
        {
            LogError("UnEquipItem: EquipmentManager가 null입니다!");
            return;
        }

        foreach (var slot in _slots)
        {
            if (slot.EquipmentType == item.Data.EquipmentType)
            {
                _equipmentManager.UnequipItem(item.Data.EquipmentType);
                slot.RemoveData();

                Log($"장비 해제: {item.Data.ItemName}");
            }
        }
    }

    /// <summary>
    /// 아이템을 받아와 착용시도
    /// 실패시 False, 성공시 True
    /// </summary>
    public bool EquipItem(InventoryItemPrefab item)
    {
        // 0. _equipmentManager null 체크
        if (_equipmentManager == null)
        {
            LogError("EquipItem: EquipmentManager가 null입니다!");
            return false;
        }

        // 1. _currentSlot이 null이면 자동으로 올바른 슬롯 찾기
        if (_currentSlot == null)
        {
            _currentSlot = FindSlotByEquipmentType(item.Data.EquipmentType);

            if (_currentSlot == null)
            {
                LogWarning($"해당 장비 타입의 슬롯을 찾을 수 없습니다: {item.Data.EquipmentType}");
                return false;
            }
        }

        // 2. 슬롯 타입 확인
        if (_currentSlot.EquipmentType != item.Data.EquipmentType)
        {
            LogWarning($"슬롯 타입이 일치하지 않습니다. 슬롯: {_currentSlot.EquipmentType}, 아이템: {item.Data.EquipmentType}");
            return false;
        }

        // 3. 기존 아이템이 있으면 인벤토리로 이동
        if (_currentSlot.HasItem)
        {
            if (!_inventory.GetEquipItem(_currentSlot))
            {
                return false;
            }
        }

        // 4. 아이템 장착
        _currentSlot.GetItemPrefab(item);
        item.transform.position = _currentSlot.transform.position;
        item.gameObject.SetActive(false);
        _equipmentManager.EquipItem(item.Data);

        Log($"장비 착용: {item.Data.ItemName}");
        return true;
    }

    /// <summary>
    /// 인벤토리에서 아이템 획득 시 아이템이 알아서 착용되는 스크립트
    /// 만약 크기가 맞지 않아 인벤토리에 아이템이 못 들어갈 시에는 작동하지 않음
    /// </summary>
    public bool QuickEquipItem(InventoryItemPrefab item)
    {
        if (_equipmentManager == null)
        {
            LogError("QuickEquipItem: EquipmentManager가 null입니다!");
            return false;
        }

        EquipmentType type = item.Data.EquipmentType;

        foreach (var slot in _slots)
        {
            if (item.Data.EquipmentType == slot.EquipmentType)
            {
                // 기존 장비 있으면 인벤토리로 이동
                if (slot.HasItem)
                {
                    if (!_inventory.GetEquipItem(slot))
                    {
                        return false;
                    }
                }

                _itemVisualizeField.AddItem(item);
                item.transform.position = transform.position;
                slot.GetItemPrefab(item);
                item.gameObject.SetActive(false);
                _equipmentManager.EquipItem(item.Data);

                Log($"빠른 장착: {item.Data.ItemName}");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 인벤토리에 없는 아이템은 따로 InventoryPrefab이 없기에 만듦
    /// 
    /// 사용 예:
    /// - 적이 드롭한 아이템을 바로 장착
    /// - ItemData만 있고 프리팹이 없는 경우
    /// </summary>
    public bool QuickDropItemEquip(ItemData data)
    {
        if (_equipmentManager == null)
        {
            LogError("QuickDropItemEquip: EquipmentManager가 null입니다!");
            return false;
        }

        EquipmentType type = data.EquipmentType;

        foreach (var slot in _slots)
        {
            if (data.EquipmentType == slot.EquipmentType)
            {
                // 기존 장비 있으면 인벤토리로 이동
                if (slot.HasItem)
                {
                    if (!_inventory.GetEquipItem(slot))
                    {
                        return false;
                    }
                }

                // 새 프리팹 생성
                InventoryItemPrefab item = Instantiate(_itemPrefab, _itemVisualizeField.transform);
                item.Spawn(data, slot, slot, _inventory.ItemSize);
                _itemVisualizeField.AddItem(item);
                slot.GetItemPrefab(item);
                item.gameObject.SetActive(false);
                _equipmentManager.EquipItem(item.Data);

                Log($"드롭 아이템 장착: {data.ItemName}");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 슬롯이 지금 입고 있는 아이템을 벗어주는 함수
    /// </summary>
    public void TakeOffEquip(EquipSlot slot)
    {
        if (!_inventory.GetEquipItem(slot))
        {
            return;
        }

        if (_equipmentManager == null)
        {
            LogError("TakeOffEquip: EquipmentManager가 null입니다!");
            return;
        }

        _equipmentManager.UnequipItem(slot.EquipmentType);
        slot.RemoveData();

        Log($"장비 해제: {slot.EquipmentType}");
    }

    #endregion

    #region 슬롯 관리

    /// <summary>
    /// 현재 슬롯이 데이터가 들어 갈 수 있나 확인하는 함수
    /// </summary>
    public bool CheckCurrentSlot(ItemData data)
    {
        if (_currentSlot == null || _currentSlot.EquipmentType != data.EquipmentType)
            return false;
        else
            return true;
    }

    public void SetCurrentSlot(EquipSlot slot)
    {
        _currentSlot = slot;
    }

    /// <summary>
    /// 장비 타입에 맞는 슬롯 찾기
    /// </summary>
    private EquipSlot FindSlotByEquipmentType(EquipmentType equipmentType)
    {
        foreach (var slot in _slots)
        {
            if (slot.EquipmentType == equipmentType)
            {
                return slot;
            }
        }
        return null;
    }

    #endregion

    #region 드래그 앤 드롭 이벤트

    public void OnBeginDrag(InventoryItemPrefab item, Slot ownerSlot)
    {
        _itemEventHandler.OnBeginDrag(item, ownerSlot);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _itemEventHandler.OnDrag(eventData);
    }

    public void OnEndDrag(InventoryItemPrefab item)
    {
        _itemEventHandler.OnEndDrag(item);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _itemEventHandler.ChangeMousePos(MousePos.EquipInventory);
    }

    #endregion

    #region 로깅

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[EquipInventory] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[EquipInventory] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[EquipInventory] {message}");
    }

    #endregion
}
