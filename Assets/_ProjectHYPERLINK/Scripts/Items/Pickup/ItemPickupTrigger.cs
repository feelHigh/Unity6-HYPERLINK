using UnityEngine;

/// <summary>
/// 트리거 기반 아이템 줍기 시스템 (리팩토링 버전)
/// 
/// 새로운 플로우:
/// 1. 플레이어 트리거 진입
/// 2. 인벤토리에 추가 시도
/// 3. 성공 시 GameObject 파괴
/// 4. 실패 시 바닥에 유지
/// 
/// 사용 조건:
/// - 플레이어에 Collider 추가 가능
/// - SphereCollider (isTrigger = true) 필수
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ItemPickupTrigger : MonoBehaviour
{
    private Item _item;
    private SphereCollider _pickupCollider;

    [Header("트리거 설정")]
    [SerializeField] private float _pickupRadius = 2f;
    [SerializeField] private bool _autoPickup = true;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private void Awake()
    {
        _item = GetComponent<Item>();

        // SphereCollider 설정
        _pickupCollider = GetComponent<SphereCollider>();
        _pickupCollider.isTrigger = true;
        _pickupCollider.radius = _pickupRadius;
    }

    /// <summary>
    /// 트리거 진입 이벤트 (리팩토링 버전)
    /// 
    /// 변경사항:
    /// - 자동 장착 제거
    /// - 인벤토리 추가로 변경
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        Log($"트리거 진입: {other.gameObject.name}");

        if (other.CompareTag("Player") && _autoPickup)
        {
            PickupItem(other.gameObject);
        }
    }

    /// <summary>
    /// 아이템 줍기 처리 (리팩토링 버전)
    /// 
    /// 새로운 로직:
    /// 1. 아이템 OnPickup() 호출
    /// 2. 인벤토리에 추가 시도
    /// 3. 성공 시 GameObject 파괴
    /// 4. 실패 시 유지
    /// </summary>
    private void PickupItem(GameObject player)
    {
        if (_item == null || _item.ItemData == null)
        {
            LogError("아이템 또는 ItemData가 null입니다!");
            return;
        }

        Log($"픽업 시도: {_item.ItemName} ({_item.Quality})");

        // 아이템 OnPickup 호출
        _item.OnPickup();

        // 인벤토리에 추가 시도
        if (ItemInventory.Instance != null)
        {
            bool added = ItemInventory.Instance.GetItem(_item.ItemData);

            if (added)
            {
                Log($"✓ 인벤토리 추가 성공: {_item.ItemName}");
                Destroy(gameObject);
            }
            else
            {
                LogWarning($"✗ 인벤토리 가득 참! 바닥에 유지: {_item.ItemName}");
                // 아이템을 바닥에 그대로 둠
            }
        }
        else
        {
            LogError("ItemInventory 인스턴스를 찾을 수 없습니다!");
        }
    }

    #region 디버그

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[ItemPickupTrigger] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[ItemPickupTrigger] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ItemPickupTrigger] {message}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _pickupRadius);
    }

    #endregion
}
