using UnityEngine;

/// <summary>
/// 트리거 기반 아이템 줍기 시스템
/// 
/// ItemPickupManager의 대안:
/// - ItemPickupManager: 거리 체크 (NavMesh 안전)
/// - ItemPickupTrigger: Collider 트리거 (전통적)
/// 
/// 사용 조건:
/// - 플레이어에 Collider 추가 가능한 경우
/// - NavMesh 문제 없는 경우
/// 
/// 장점:
/// - Physics 엔진 활용
/// - OnTriggerEnter 이벤트 자동 감지
/// 
/// 단점:
/// - 플레이어 Collider 필요
/// - NavMesh와 충돌 가능성
/// 
/// 설정:
/// - SphereCollider 컴포넌트 필수
/// - isTrigger = true
/// - 플레이어 태그 = "Player"
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ItemPickupTrigger : MonoBehaviour
{
    private Item _item;                         // 이 아이템 인스턴스
    private SphereCollider _pickupCollider;     // 줍기 트리거 콜라이더

    [SerializeField] private float _pickupRadius = 2f;     // 줍기 범위
    [SerializeField] private bool _autoPickup = true;      // 자동 줍기 여부

    private void Awake()
    {
        _item = GetComponent<Item>();

        // SphereCollider 설정
        _pickupCollider = GetComponent<SphereCollider>();
        _pickupCollider.isTrigger = true;            // 트리거로 설정 (중요!)
        _pickupCollider.radius = _pickupRadius;
    }

    /// <summary>
    /// 트리거 진입 이벤트
    /// 
    /// Unity가 자동으로 호출:
    /// - 플레이어 Collider가 트리거 범위 진입 시
    /// 
    /// 처리 과정:
    /// 1. 태그 확인 ("Player")
    /// 2. 자동 줍기 활성화면 즉시 줍기
    /// 3. EquipmentManager를 통해 장착
    /// 4. GameObject 파괴
    /// 
    /// Parameters:
    ///     other: 진입한 Collider (플레이어)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"트리거 진입: {other.gameObject.name}, 태그: {other.tag}");

        // 플레이어 태그 확인 및 자동 줍기
        if (other.CompareTag("Player") && _autoPickup)
        {
            PickupItem(other.gameObject);
        }
        else
        {
            Debug.LogWarning($"플레이어가 아닌 객체 진입: {other.tag}");
        }
    }

    /// <summary>
    /// 아이템 줍기 처리
    /// 
    /// ItemPickupManager.PickupItem()과 동일한 로직
    /// </summary>
    private void PickupItem(GameObject player)
    {
        Debug.Log($"플레이어가 획득: {_item.ItemName} ({_item.Quality})");

        EquipmentManager equipmentManager = player.GetComponent<EquipmentManager>();

        if (equipmentManager != null && _item.ItemData != null)
        {
            bool equipped = equipmentManager.EquipItem(_item.ItemData);

            if (equipped)
            {
                Debug.Log($"자동 장착: {_item.ItemName}");
            }
            else
            {
                Debug.LogWarning($"장착 실패: {_item.ItemName}");
            }
        }

        // 월드 아이템 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 줍기 범위 시각화 (Gizmo)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _pickupRadius);
    }
}