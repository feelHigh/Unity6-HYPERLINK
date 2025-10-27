using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 아이템 자동 줍기 시스템
/// 
/// 플로우:
/// 1. 플레이어 범위 내 아이템 감지
/// 2. 인벤토리에 추가 시도
/// 3. 성공 시 월드에서 제거
/// 4. 실패 시 바닥에 유지 (인벤토리 가득 참)
/// </summary>
public class ItemPickupManager : MonoBehaviour
{
    public static ItemPickupManager Instance { get; private set; }

    [Header("줍기 설정")]
    [SerializeField] private float _pickupRange = 2.0f;
    [SerializeField] private LayerMask _itemLayer = ~0;

    [Header("최적화")]
    [SerializeField] private int _maxColliderResults = 20;

    [Header("자동 검색 설정")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _retryInterval = 0.5f;
    [SerializeField] private int _maxRetries = 20;
    [SerializeField] private bool _enableDebugLogs = true;

    private Transform _playerTransform;
    private Collider[] _colliderBuffer;
    private HashSet<Item> _itemsInWorld = new HashSet<Item>();
    private bool _isInitialized = false;
    private int _retryCount = 0;

    #region 초기화

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _colliderBuffer = new Collider[_maxColliderResults];
    }

    private void Start()
    {
        InvokeRepeating(nameof(TryFindPlayer), 0.1f, _retryInterval);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(TryFindPlayer));
    }

    /// <summary>
    /// PlayerSpawner로 스폰된 플레이어 찾기
    /// </summary>
    private void TryFindPlayer()
    {
        if (_isInitialized) return;

        _retryCount++;

        GameObject playerObject = GameObject.FindGameObjectWithTag(_playerTag);

        if (playerObject != null)
        {
            _playerTransform = playerObject.transform;
            _isInitialized = true;
            CancelInvoke(nameof(TryFindPlayer));

            Log($"플레이어 찾음: {playerObject.name} (시도: {_retryCount}회)");
            return;
        }

        if (_retryCount >= _maxRetries)
        {
            LogError($"플레이어를 {_maxRetries}회 시도 후에도 찾지 못했습니다!");
            CancelInvoke(nameof(TryFindPlayer));
        }
    }

    #endregion

    #region 아이템 관리

    /// <summary>
    /// 아이템 등록 (Item.OnInitialize에서 호출)
    /// </summary>
    public void RegisterItem(Item item)
    {
        if (item != null)
        {
            _itemsInWorld.Add(item);
            Log($"아이템 등록: {item.ItemName}");
        }
    }

    /// <summary>
    /// 아이템 등록 해제 (Item.OnDestroy에서 호출)
    /// </summary>
    public void UnregisterItem(Item item)
    {
        if (item != null)
        {
            _itemsInWorld.Remove(item);
        }
    }

    #endregion

    #region 아이템 줍기

    private void Update()
    {
        if (!_isInitialized || _playerTransform == null)
            return;

        TryPickupNearbyItems();
    }

    /// <summary>
    /// 범위 내 아이템 줍기 시도
    /// </summary>
    private void TryPickupNearbyItems()
    {
        Item closestItem = FindClosestItemOptimized();

        if (closestItem != null && Vector3.Distance(_playerTransform.position, closestItem.transform.position) <= _pickupRange)
        {
            if (PickupItem(closestItem))
            {
                Log($"아이템 픽업 성공: {closestItem.ItemName}");
            }
        }
    }

    /// <summary>
    /// 가장 가까운 아이템 찾기
    /// </summary>
    private Item FindClosestItemOptimized()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            _playerTransform.position,
            _pickupRange,
            _colliderBuffer,
            _itemLayer
        );

        if (hitCount == 0)
            return null;

        Item closestItem = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Item item = _colliderBuffer[i].GetComponent<Item>();
            if (item == null)
                continue;

            float distance = Vector3.Distance(_playerTransform.position, item.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestItem = item;
            }
        }

        return closestItem;
    }

    /// <summary>
    /// 아이템 픽업 처리
    /// 
    /// 수정사항:
    /// - Item.IsPickedUp 제거 (불필요)
    /// - ItemInventory.LoadItemToSlot() 사용
    /// - Item.MarkAsPickedUp() 대신 Destroy 사용
    /// </summary>
    private bool PickupItem(Item item)
    {
        if (item == null || item.ItemData == null)
        {
            LogWarning("PickupItem: 아이템 또는 ItemData가 null입니다!");
            return false;
        }

        if (ItemInventory.Instance == null)
        {
            LogWarning("PickupItem: ItemInventory가 null입니다!");
            return false;
        }

        // 인벤토리에 추가 시도 (빈 슬롯 자동 검색)
        bool addedToInventory = TryAddToFirstAvailableSlot(item.ItemData);

        if (addedToInventory)
        {
            // 아이템 픽업 이벤트 발생
            item.OnPickup();

            // 월드에서 제거
            _itemsInWorld.Remove(item);
            Destroy(item.gameObject);

            Log($"아이템 픽업 완료: {item.ItemName}");
            return true;
        }
        else
        {
            LogWarning($"인벤토리 공간 부족: {item.ItemName}");
            return false;
        }
    }

    /// <summary>
    /// 첫 번째 사용 가능한 슬롯에 아이템 추가
    /// ItemInventory.LoadItemToSlot() 사용
    /// </summary>
    private bool TryAddToFirstAvailableSlot(ItemData itemData)
    {
        // 인벤토리 크기 (10x4 = 40 슬롯)
        int inventorySize = 40;

        for (int slotIndex = 0; slotIndex < inventorySize; slotIndex++)
        {
            if (ItemInventory.Instance.LoadItemToSlot(itemData, slotIndex))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region 디버그 기즈모

    private void OnDrawGizmosSelected()
    {
        if (_playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_playerTransform.position, _pickupRange);
        }
    }

    #endregion

    #region 로깅

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[ItemPickupManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[ItemPickupManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ItemPickupManager] {message}");
    }

    #endregion
}
