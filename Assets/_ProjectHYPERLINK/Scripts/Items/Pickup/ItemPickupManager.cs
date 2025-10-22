using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 아이템 자동 줍기 시스템
/// 
/// 핵심 기능:
/// - 플레이어 주변 범위 내 아이템 감지
/// - 가장 가까운 아이템 자동 줍기
/// - EquipmentManager 연동 (장착 시도)
/// - ItemInventory 연동 (장착 실패 시 인벤토리 추가)
/// </summary>
public class ItemPickupManager : MonoBehaviour
{
    public static ItemPickupManager Instance { get; private set; }

    [Header("줍기 설정")]
    [SerializeField] private float _pickupRange = 2.0f;
    [SerializeField] private LayerMask _itemLayer = ~0;

    [Header("최적화")]
    [SerializeField] private int _maxColliderResults = 20;

    private Transform _playerTransform;
    private EquipmentManager _equipmentManager;
    private Collider[] _colliderBuffer;
    private HashSet<Item> _itemsInWorld = new HashSet<Item>();

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
        }

        _equipmentManager = FindFirstObjectByType<EquipmentManager>();

        ValidateLayerSetup();
    }

    private void ValidateLayerSetup()
    {
        if (_itemLayer == ~0)
        {
            Debug.LogWarning(
                "[ItemPickupManager] LayerMask가 'Everything'으로 설정되어 있습니다!\n" +
                "성능 향상을 위해 'Item' 전용 레이어 사용 권장!\n" +
                "설정 방법: Edit → Project Settings → Tags and Layers → Layer 8 = 'Item'"
            );
        }
    }

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

    private void Update()
    {
        if (_playerTransform == null || _equipmentManager == null)
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
    /// 가장 가까운 아이템 찾기 (최적화 버전)
    /// Physics.OverlapSphereNonAlloc 사용
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
    /// 아이템 줍기
    /// 
    /// 처리 순서:
    /// 1. 유효성 검사
    /// 2. EquipmentManager로 자동 장착 시도
    /// 3. 성공 시 월드에서 제거
    /// 4. 실패 시 ItemInventory에 추가
    /// </summary>
    private void PickupItem(Item item)
    {
        if (item == null || item.ItemData == null)
            return;

        Debug.Log($"줍는 중: {item.ItemName} ({item.Quality})");

        // 1. 장비 자동 장착 시도
        bool equipped = _equipmentManager.EquipItem(item.ItemData);

        if (equipped)
        {
            Debug.Log($"장착 성공: {item.ItemName}");

            UnregisterItem(item);
            Destroy(item.gameObject);
        }
        else
        {
            // 2. 장착 실패: 인벤토리에 추가
            Debug.Log($"인벤토리에 추가 시도: {item.ItemName}");

            if (ItemInventory.Instance != null)
            {
                bool added = ItemInventory.Instance.GetItem(item.ItemData);

                if (added)
                {
                    Debug.Log($"인벤토리 추가 성공: {item.ItemName}");
                    UnregisterItem(item);
                    Destroy(item.gameObject);
                }
                else
                {
                    Debug.LogWarning($"인벤토리 가득 참! 바닥에 유지: {item.ItemName}");
                    // 아이템을 바닥에 그대로 둠
                }
            }
            else
            {
                Debug.LogError("[ItemPickupManager] ItemInventory 인스턴스를 찾을 수 없습니다!");
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_playerTransform == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_playerTransform.position, _pickupRange);

        if (!Application.isPlaying)
            return;

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
        Debug.Log($"플레이어 위치: {(_playerTransform != null ? _playerTransform.position.ToString() : "없음")}");
        Debug.Log($"줍기 범위: {_pickupRange}");
        Debug.Log($"월드 아이템 수: {_itemsInWorld.Count}");
        Debug.Log($"EquipmentManager: {(_equipmentManager != null ? "연결됨" : "없음")}");
        Debug.Log($"ItemInventory: {(ItemInventory.Instance != null ? "연결됨" : "없음")}");
    }
}
