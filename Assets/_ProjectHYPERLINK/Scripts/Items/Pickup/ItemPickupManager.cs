using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 거리 기반 아이템 줍기 시스템
/// 
/// 사용 목적:
/// - 플레이어에 Collider를 추가하면 NavMesh 문제 발생 가능
/// - 트리거 방식 대신 거리 체크 방식 사용
/// - 더 안정적이고 제어 가능한 아이템 획득 시스템
/// 
/// 핵심 기능:
/// - 월드의 모든 아이템 추적
/// - 플레이어와 아이템 간 거리 계산
/// - 자동/수동 줍기 모드 지원
/// - 가장 가까운 아이템 우선 줍기
/// - 아이템 자동 장착
/// 
/// 작동 방식:
/// 1. Item.OnInitialize()에서 아이템이 등록됨
/// 2. Update()에서 매 프레임 거리 체크
/// 3. 범위 내 가장 가까운 아이템 감지
/// 4. 조건 충족 시 자동으로 줍기 (또는 E키로 수동)
/// 5. EquipmentManager를 통해 자동 장착
/// 6. 아이템 GameObject 파괴
/// 
/// ItemPickupTrigger와의 차이:
/// - ItemPickupTrigger: 콜라이더 기반 (Physics 의존)
/// - ItemPickupManager: 거리 계산 기반 (더 안정적)
/// 
/// Diablo 3 스타일:
/// - 가까이 가면 자동으로 줍기
/// - 시각적 피드백 (Gizmo로 범위 표시)
/// </summary>
public class ItemPickupManager : MonoBehaviour
{
    private static ItemPickupManager _instance;
    public static ItemPickupManager Instance => _instance;

    [Header("줍기 설정")]
    [SerializeField] private float _pickupRadius = 2.5f;          // 아이템 줍기 범위 (반경)
    [SerializeField] private bool _autoPickup = true;             // 자동 줍기 활성화 여부
    [SerializeField] private KeyCode _manualPickupKey = KeyCode.E; // 수동 줍기 키

    // 월드에 존재하는 모든 아이템 추적
    private List<Item> _itemsInWorld = new List<Item>();

    // 시스템 참조
    private Transform _playerTransform;           // 플레이어 위치
    private EquipmentManager _equipmentManager;   // 아이템 자동 장착용

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        // 플레이어 찾기 및 참조 저장
        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        if (player != null)
        {
            _playerTransform = player.transform;
            _equipmentManager = player.GetComponent<EquipmentManager>();
        }
        else
        {
            Debug.LogError("ItemPickupManager: 플레이어를 찾을 수 없습니다!");
        }
    }

    private void Update()
    {
        // 플레이어나 장비 매니저가 없으면 실행 안 함
        if (_playerTransform == null || _equipmentManager == null)
            return;

        // null 참조 제거 (파괴된 아이템 정리)
        _itemsInWorld.RemoveAll(item => item == null);

        // 가장 가까운 아이템 찾기
        Item closestItem = FindClosestItem();

        if (closestItem != null)
        {
            float distance = Vector3.Distance(_playerTransform.position, closestItem.transform.position);

            // 줍기 범위 내에 있는지 확인
            if (distance <= _pickupRadius)
            {
                // 자동 줍기 or 수동 줍기 (E키)
                if (_autoPickup)
                {
                    PickupItem(closestItem);
                }
                else if (Input.GetKeyDown(_manualPickupKey))
                {
                    PickupItem(closestItem);
                }
            }
        }
    }

    /// <summary>
    /// 아이템 등록 (Public - Item.OnInitialize()에서 호출)
    /// 
    /// 아이템이 월드에 생성될 때 호출되어야 함
    /// 이 시스템에 등록되지 않은 아이템은 줍기 불가능
    /// 
    /// 호출 시점:
    /// - ItemSpawner가 아이템 생성 후
    /// - Item.OnInitialize()에서 자동 호출
    /// 
    /// Parameters:
    ///     item: 등록할 아이템 인스턴스
    /// </summary>
    public void RegisterItem(Item item)
    {
        // null 체크 및 중복 등록 방지
        if (item != null && !_itemsInWorld.Contains(item))
        {
            _itemsInWorld.Add(item);
            Debug.Log($"아이템 등록: {item.ItemName}");
        }
    }

    /// <summary>
    /// 아이템 등록 해제
    /// 
    /// 아이템이 주워지거나 파괴될 때 호출
    /// Item.OnDestroy()에서 자동으로 호출됨
    /// 
    /// Parameters:
    ///     item: 해제할 아이템 인스턴스
    /// </summary>
    public void UnregisterItem(Item item)
    {
        if (item != null && _itemsInWorld.Contains(item))
        {
            _itemsInWorld.Remove(item);
        }
    }

    /// <summary>
    /// 플레이어에게 가장 가까운 아이템 찾기
    /// 
    /// 알고리즘:
    /// 1. 모든 등록된 아이템 순회
    /// 2. 각 아이템과의 거리 계산
    /// 3. 가장 가까운 아이템 추적
    /// 4. 최종적으로 가장 가까운 아이템 반환
    /// 
    /// Returns:
    ///     가장 가까운 아이템 (없으면 null)
    ///     
    /// 최적화 고려사항:
    /// - 아이템이 매우 많을 경우 성능 문제 가능
    /// - 공간 분할(Spatial Partitioning) 또는 쿼드트리 사용 고려
    /// - 현재 구현은 소규모 게임에 적합
    /// </summary>
    private Item FindClosestItem()
    {
        Item closest = null;
        float closestDistance = float.MaxValue;  // 무한대로 시작

        foreach (Item item in _itemsInWorld)
        {
            // null 체크 (파괴된 아이템 건너뛰기)
            if (item == null)
                continue;

            // 플레이어와의 거리 계산
            float distance = Vector3.Distance(_playerTransform.position, item.transform.position);

            // 더 가까운 아이템이면 업데이트
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = item;
            }
        }

        return closest;
    }

    /// <summary>
    /// 아이템 줍기 메인 로직
    /// 
    /// 처리 과정:
    /// 1. 아이템 유효성 검사
    /// 2. EquipmentManager를 통해 자동 장착 시도
    /// 3. 장착 성공 시:
    ///    - 추적 목록에서 제거
    ///    - 월드에서 GameObject 파괴
    /// 4. 장착 실패 시:
    ///    - 경고 로그 출력
    ///    - 아이템은 월드에 남음 (나중에 인벤토리로 이동 가능)
    /// 
    /// Parameters:
    ///     item: 주울 아이템
    ///     
    /// 주의사항:
    /// - 현재는 즉시 장착만 지원
    /// - 인벤토리 시스템 추가 시 수정 필요
    /// - 장착 실패 시 아이템을 인벤토리로 보내는 로직 필요
    /// </summary>
    private void PickupItem(Item item)
    {
        // 유효성 검사
        if (item == null || item.ItemData == null)
            return;

        Debug.Log($"줍는 중: {item.ItemName} ({item.Quality})");

        // 아이템 자동 장착 시도
        bool equipped = _equipmentManager.EquipItem(item.ItemData);

        if (equipped)
        {
            Debug.Log($"장착 성공: {item.ItemName}");

            // 추적 목록에서 제거
            UnregisterItem(item);

            // 월드 아이템 제거
            Destroy(item.gameObject);
        }
        else
        {
            Debug.LogWarning($"장착 실패: {item.ItemName}");
            // TODO: 인벤토리로 이동하는 로직 추가
            // 현재는 아이템이 월드에 남아있음
        }
    }

    /// <summary>
    /// Gizmo를 사용한 디버그 시각화
    /// 
    /// Scene 뷰에서 표시되는 내용:
    /// 1. 초록색 와이어 구: 플레이어 주변 줍기 범위
    /// 2. 노란색 선: 범위 내 아이템과 플레이어 연결
    /// 
    /// 사용 방법:
    /// - Hierarchy에서 ItemPickupManager를 선택
    /// - Scene 뷰에서 시각적 피드백 확인
    /// - 범위 조정 시 실시간으로 변경 사항 확인 가능
    /// 
    /// 게임 빌드에는 포함되지 않음 (에디터 전용)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 플레이어가 없으면 그리지 않음
        if (_playerTransform != null)
        {
            // 줍기 범위 표시 (초록색 원)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_playerTransform.position, _pickupRadius);
        }

        // 범위 내 아이템에 선 그리기
        if (_playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Item item in _itemsInWorld)
            {
                if (item != null)
                {
                    float distance = Vector3.Distance(_playerTransform.position, item.transform.position);

                    // 범위 내에 있는 아이템만 선으로 연결
                    if (distance <= _pickupRadius)
                    {
                        Gizmos.DrawLine(_playerTransform.position, item.transform.position);
                    }
                }
            }
        }
    }
}