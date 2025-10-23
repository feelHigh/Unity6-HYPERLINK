using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 아이템 자동 줍기 시스템
/// 
/// 새로운 플로우:
/// 1. 플레이어 범위 내 아이템 감지
/// 2. 인벤토리에 추가 시도
/// 3. 성공 시 월드에서 제거
/// 4. 실패 시 바닥에 유지 (인벤토리 가득 참)
/// 
/// 장착은 플레이어가 인벤토리 UI에서 수동으로 진행
/// </summary>
public class ItemPickupManager : MonoBehaviour
{
    public static ItemPickupManager Instance { get; private set; }

    [Header("줍기 설정")]
    [Tooltip("자동 줍기 범위 (미터)")]
    [SerializeField] private float _pickupRange = 2.0f;

    [Tooltip("아이템 레이어 마스크")]
    [SerializeField] private LayerMask _itemLayer = ~0;

    [Header("최적화")]
    [SerializeField] private int _maxColliderResults = 20;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private Transform _playerTransform;
    private Collider[] _colliderBuffer;
    private HashSet<Item> _itemsInWorld = new HashSet<Item>();

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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            Log("플레이어 발견");
        }
        else
        {
            LogError("플레이어를 찾을 수 없습니다!");
        }

        ValidateLayerSetup();
    }

    private void ValidateLayerSetup()
    {
        if (_itemLayer == ~0)
        {
            LogWarning(
                "LayerMask가 'Everything'으로 설정됨!\n" +
                "성능 향상을 위해 'Item' 레이어 사용 권장"
            );
        }
    }

    #endregion

    #region 아이템 등록/해제

    public void RegisterItem(Item item)
    {
        if (item != null)
        {
            _itemsInWorld.Add(item);
        }
    }

    public void UnregisterItem(Item item)
    {
        if (item != null)
        {
            _itemsInWorld.Remove(item);
        }
    }

    #endregion

    #region 아이템 감지 및 픽업

    private void Update()
    {
        if (_playerTransform == null)
            return;

        Item closestItem = FindClosestItemOptimized();

        if (closestItem != null)
        {
            float distance = Vector3.Distance(_playerTransform.position, closestItem.transform.position);

            if (distance <= _pickupRange)
            {
                PickupItem(closestItem);
            }
        }
    }

    /// <summary>
    /// 가장 가까운 아이템 찾기 (최적화)
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
    /// 아이템 줍기 (리팩토링 버전)
    /// 
    /// 새로운 플로우:
    /// 1. 유효성 검사
    /// 2. 인벤토리에 추가 시도
    /// 3. 성공 시 월드에서 제거
    /// 4. 실패 시 바닥에 유지
    /// 
    /// 자동 장착 제거됨!
    /// </summary>
    private void PickupItem(Item item)
    {
        if (item == null || item.ItemData == null)
            return;

        Log($"아이템 픽업 시도: {item.ItemName} ({item.Quality})");

        // 아이템 OnPickup 호출 (로그 등)
        item.OnPickup();

        // 인벤토리에 추가 시도
        if (ItemInventory.Instance != null)
        {
            bool added = ItemInventory.Instance.GetItem(item.ItemData);

            if (added)
            {
                Log($"✓ 인벤토리 추가 성공: {item.ItemName}");

                // 월드에서 제거
                UnregisterItem(item);
                Destroy(item.gameObject);
            }
            else
            {
                LogWarning($"✗ 인벤토리 가득 참! 바닥에 유지: {item.ItemName}");
                // 아이템을 바닥에 그대로 둠
            }
        }
        else
        {
            LogError("ItemInventory 인스턴스를 찾을 수 없습니다!");
        }
    }

    #endregion

    #region 디버그

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

    private void OnDrawGizmos()
    {
        if (_playerTransform == null)
            return;

        // 픽업 범위 시각화
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_playerTransform.position, _pickupRange);

        if (!Application.isPlaying)
            return;

        // 가장 가까운 아이템 표시
        Item closest = FindClosestItemOptimized();
        if (closest != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_playerTransform.position, closest.transform.position);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(closest.transform.position, 0.2f);
        }
    }

    [ContextMenu("Debug: Print Pickup Status")]
    private void DebugPrintStatus()
    {
        Debug.Log("===== ItemPickupManager 상태 =====");
        Debug.Log($"플레이어: {(_playerTransform != null ? _playerTransform.name : "없음")}");
        Debug.Log($"줍기 범위: {_pickupRange}m");
        Debug.Log($"월드 아이템: {_itemsInWorld.Count}개");
        Debug.Log($"ItemInventory: {(ItemInventory.Instance != null ? "연결됨" : "없음")}");
    }

    #endregion
}
