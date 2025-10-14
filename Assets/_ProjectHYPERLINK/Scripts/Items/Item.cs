using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 월드 아이템 인스턴스의 추상 베이스 클래스
/// 
/// 역할:
/// - ItemData(데이터)와 GameObject(월드 표현)를 연결
/// - 템플릿 기반 아이템과 절차적 아이템 모두 지원
/// - 런타임 데이터 오버라이드 기능
/// - 아이템 줍기 시스템과 통합
/// - 시각적 표현 관리
/// 
/// 상속 구조:
/// - Item (추상 베이스)
///   ├─ WeaponItem (무기)
///   └─ EquipmentItem (방어구/액세서리)
/// 
/// 데이터 시스템:
/// 1. 템플릿 모드: ItemData를 그대로 사용
/// 2. 절차적 모드: ItemData + 런타임 오버라이드
/// 
/// 사용 흐름:
/// 1. ItemSpawner가 프리팹 인스턴스화
/// 2. Initialize() 호출로 데이터 및 런타임 값 설정
/// 3. OnInitialize()로 자식 클래스 초기화
/// 4. ItemPickupManager에 자동 등록
/// 5. 플레이어가 범위 내 진입
/// 6. OnPickup() 호출
/// 7. GameObject 파괴
/// 
/// 리팩토링 이력:
/// - ItemData 통합 (이전: 별도 데이터 클래스)
/// - 런타임 오버라이드 시스템 추가
/// </summary>
public abstract class Item : MonoBehaviour
{
    [Header("아이템 데이터 참조")]
    [SerializeField] protected ItemData _itemData;  // 기본 아이템 데이터 (ScriptableObject)

    // === 런타임 오버라이드 (절차적 생성용) ===
    // ItemSpawner가 랜덤 아이템 생성 시 사용
    protected string _runtimeName;                  // 생성된 이름 (예: "강력한 검 of 힘")
    protected string _runtimeDescription;           // 커스텀 설명
    protected ItemQuality _runtimeQuality;          // 생성된 등급
    protected List<ItemStat> _runtimeStats;         // 생성된 스탯

    // 런타임 오버라이드가 설정되었는지 플래그
    protected bool _hasRuntimeOverrides = false;

    /// <summary>
    /// ItemData 접근자
    /// 다른 시스템(EquipmentManager 등)에서 사용
    /// </summary>
    public ItemData ItemData
    {
        get => _itemData;
        set => _itemData = value;
    }

    // === 편의 접근자 (런타임 오버라이드 우선) ===

    /// <summary>
    /// 아이템 이름 가져오기
    /// 
    /// 우선순위:
    /// 1. 런타임 오버라이드 이름 (절차적 생성)
    /// 2. ItemData의 기본 이름 (템플릿)
    /// </summary>
    public string ItemName => _hasRuntimeOverrides && !string.IsNullOrEmpty(_runtimeName)
        ? _runtimeName
        : _itemData?.ItemName;

    /// <summary>
    /// 아이템 설명 가져오기
    /// 런타임 오버라이드 우선
    /// </summary>
    public string Description => _hasRuntimeOverrides && !string.IsNullOrEmpty(_runtimeDescription)
        ? _runtimeDescription
        : _itemData?.Description;

    /// <summary>
    /// 아이템 등급 가져오기
    /// 런타임 오버라이드 우선
    /// </summary>
    public ItemQuality Quality => _hasRuntimeOverrides
        ? _runtimeQuality
        : (_itemData?.Quality ?? ItemQuality.Normal);

    /// <summary>
    /// 아이템 스탯 가져오기
    /// 런타임 오버라이드 우선
    /// </summary>
    public List<ItemStat> Stats => _hasRuntimeOverrides && _runtimeStats != null
        ? _runtimeStats
        : _itemData?.ProceduralStats;

    /// <summary>
    /// 아이템 초기화 (메인 메서드)
    /// 
    /// ItemSpawner.InstantiateItem()에서 호출됨
    /// 
    /// 처리 과정:
    /// 1. ItemData 할당
    /// 2. 런타임 오버라이드가 있으면 설정
    /// 3. OnInitialize() 호출 (자식 클래스 초기화)
    /// 
    /// Parameters:
    ///     data: 기본 ItemData (템플릿)
    ///     quality: 런타임 등급 (옵션)
    ///     stats: 런타임 스탯 (옵션)
    ///     customName: 커스텀 이름 (옵션)
    ///     customDescription: 커스텀 설명 (옵션)
    ///     
    /// 사용 예:
    /// WeaponItem weapon = Instantiate(weaponPrefab);
    /// weapon.Initialize(swordData, ItemQuality.Rare, randomStats, "강력한 검 of 힘");
    /// </summary>
    public virtual void Initialize(
        ItemData data,
        ItemQuality? quality = null,
        List<ItemStat> stats = null,
        string customName = null,
        string customDescription = null)
    {
        _itemData = data;

        // 런타임 오버라이드가 하나라도 있으면 플래그 설정
        if (quality.HasValue || stats != null || !string.IsNullOrEmpty(customName) || !string.IsNullOrEmpty(customDescription))
        {
            _hasRuntimeOverrides = true;
            _runtimeQuality = quality ?? data.Quality;
            _runtimeStats = stats != null ? new List<ItemStat>(stats) : data.ProceduralStats;
            _runtimeName = customName ?? data.ItemName;
            _runtimeDescription = customDescription ?? data.Description;
        }

        // 자식 클래스 초기화
        OnInitialize();
    }

    /// <summary>
    /// 자식 클래스 초기화 훅
    /// 
    /// 파생 클래스에서 오버라이드하여 추가 초기화 수행
    /// 
    /// 기본 구현:
    /// - 3D 모델 업데이트
    /// - ItemPickupManager에 등록
    /// 
    /// 확장 예시:
    /// - WeaponItem: 무기별 특수 이펙트 설정
    /// - EquipmentItem: 방어구 시각 효과
    /// </summary>
    protected virtual void OnInitialize()
    {
        // 아이템 3D 모델 업데이트
        UpdateItemVisuals();

        // 거리 기반 줍기 시스템에 등록
        if (ItemPickupManager.Instance != null)
        {
            ItemPickupManager.Instance.RegisterItem(this);
        }
    }

    /// <summary>
    /// GameObject 파괴 시 자동 호출
    /// ItemPickupManager에서 등록 해제
    /// </summary>
    private void OnDestroy()
    {
        if (ItemPickupManager.Instance != null)
        {
            ItemPickupManager.Instance.UnregisterItem(this);
        }
    }

    /// <summary>
    /// 아이템 시각적 표현 업데이트
    /// 
    /// 처리 과정:
    /// 1. 기존 자식 모델 모두 제거
    /// 2. ItemData에서 새 모델 프리팹 가져오기
    /// 3. 새 모델 인스턴스화
    /// 
    /// 사용 시나리오:
    /// - 아이템 생성 시 (OnInitialize)
    /// - 아이템 데이터 변경 시
    /// - 시각 효과 업그레이드 시
    /// 
    /// 모델 구조:
    /// GameObject (Item 컴포넌트)
    ///   └─ 3D Model (ItemData.ItemModel 프리팹)
    ///       ├─ Mesh
    ///       ├─ Materials
    ///       └─ Effects
    /// </summary>
    protected virtual void UpdateItemVisuals()
    {
        if (_itemData != null && _itemData.ItemModel != null)
        {
            // 기존 자식 오브젝트 모두 제거
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // 새 모델 인스턴스화 (이 아이템의 자식으로)
            Instantiate(_itemData.ItemModel, transform);
        }
    }

    /// <summary>
    /// 아이템 줍기 처리
    /// 
    /// ItemPickupManager 또는 ItemPickupTrigger에서 호출
    /// 
    /// 기본 구현:
    /// - 로그 출력
    /// - GameObject 파괴
    /// 
    /// 주의:
    /// - 실제 장착은 ItemPickupManager.PickupItem()에서 처리
    /// - 이 메서드는 정리 작업만 수행
    /// 
    /// TODO:
    /// - 줍기 애니메이션
    /// - 줍기 사운드
    /// - 줍기 이펙트
    /// </summary>
    public virtual void OnPickup()
    {
        Debug.Log($"획득: {ItemName} ({Quality})");
        Destroy(gameObject);
    }

    // === 추상 메서드 (자식 클래스 필수 구현) ===

    /// <summary>
    /// 장비 착용 처리
    /// WeaponItem, EquipmentItem에서 각각 구현
    /// </summary>
    public abstract void Equip();

    /// <summary>
    /// 장비 해제 처리
    /// WeaponItem, EquipmentItem에서 각각 구현
    /// </summary>
    public abstract void Unequip();
}