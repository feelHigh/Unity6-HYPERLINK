using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 월드 아이템 인스턴스 기본 클래스
/// 
/// 역할:
/// - 3D 월드에 배치된 아이템 (바닥에 떨어진 아이템)
/// - ScriptableObject(ItemData) → GameObject 인스턴스
/// - 절차적 생성 지원 (랜덤 스탯, 품질)
/// - ItemPickupManager 연동
/// 
/// 상속 클래스:
/// - WeaponItem: 무기 아이템
/// - EquipmentItem: 방어구 아이템
/// 
/// 생명주기:
/// 1. ItemSpawner.InstantiateItem() → 생성
/// 2. Initialize() → 데이터 설정
/// 3. OnInitialize() → ItemPickupManager 등록
/// 4. 플레이어 접근 → ItemPickupManager.PickupItem()
/// 5. OnDestroy() → ItemPickupManager 해제
/// 
/// 런타임 오버라이드:
/// - ItemData는 ScriptableObject(공유)
/// - 런타임 변경이 필요한 경우 _runtime 필드 사용
/// - 절차적 생성 시 품질/스탯/이름 오버라이드
/// 
/// 디자인 패턴:
/// - 템플릿 메서드 패턴
/// - Equip/Unequip은 하위 클래스에서 구현
/// </summary>
public abstract class Item : MonoBehaviour
{
    [Header("아이템 데이터 참조")]
    [SerializeField] protected ItemData _itemData;

    // 런타임 오버라이드 (절차적 생성 시 사용)
    protected string _runtimeName;
    protected string _runtimeDescription;
    protected ItemQuality _runtimeQuality;
    protected List<ItemStat> _runtimeStats;
    protected bool _hasRuntimeOverrides = false;

    /// <summary>
    /// ItemData ScriptableObject 참조
    /// </summary>
    public ItemData ItemData
    {
        get => _itemData;
        set => _itemData = value;
    }

    /// <summary>
    /// 아이템 이름
    /// 런타임 오버라이드가 있으면 런타임 이름 사용
    /// </summary>
    public string ItemName => _hasRuntimeOverrides && !string.IsNullOrEmpty(_runtimeName)
        ? _runtimeName
        : _itemData?.ItemName;

    /// <summary>
    /// 아이템 설명
    /// 런타임 오버라이드가 있으면 런타임 설명 사용
    /// </summary>
    public string Description => _hasRuntimeOverrides && !string.IsNullOrEmpty(_runtimeDescription)
        ? _runtimeDescription
        : _itemData?.Description;

    /// <summary>
    /// 아이템 품질
    /// 런타임 오버라이드가 있으면 런타임 품질 사용
    /// </summary>
    public ItemQuality Quality => _hasRuntimeOverrides
        ? _runtimeQuality
        : (_itemData?.Quality ?? ItemQuality.StandardEmblem);

    /// <summary>
    /// 아이템 스탯
    /// 런타임 오버라이드가 있으면 런타임 스탯 사용
    /// </summary>
    public List<ItemStat> Stats => _hasRuntimeOverrides && _runtimeStats != null
        ? _runtimeStats
        : _itemData?.ProceduralStats;

    /// <summary>
    /// 아이템 초기화
    /// 
    /// 호출: ItemSpawner.InstantiateItem()
    /// 
    /// Parameters:
    /// - data: ItemData ScriptableObject (필수)
    /// - quality: 절차적 품질 (선택, 랜덤 생성 시)
    /// - stats: 절차적 스탯 (선택, 랜덤 생성 시)
    /// - customName: 절차적 이름 (선택, 예: "화염의 검")
    /// - customDescription: 절차적 설명 (선택)
    /// 
    /// 처리:
    /// 1. ItemData 할당
    /// 2. 오버라이드 값 있으면 런타임 필드에 저장
    /// 3. OnInitialize() 호출 (하위 클래스 훅)
    /// </summary>
    public virtual void Initialize(
        ItemData data,
        ItemQuality? quality = null,
        List<ItemStat> stats = null,
        string customName = null,
        string customDescription = null)
    {
        _itemData = data;

        // 오버라이드 값이 하나라도 있으면 런타임 모드 활성화
        if (quality.HasValue || stats != null || !string.IsNullOrEmpty(customName) || !string.IsNullOrEmpty(customDescription))
        {
            _hasRuntimeOverrides = true;
            _runtimeQuality = quality ?? data.Quality;
            _runtimeStats = stats != null ? new List<ItemStat>(stats) : data.ProceduralStats;
            _runtimeName = customName ?? data.ItemName;
            _runtimeDescription = customDescription ?? data.Description;
        }

        OnInitialize();
    }

    /// <summary>
    /// 초기화 후 처리 (하위 클래스 훅)
    /// 
    /// 처리:
    /// 1. 3D 모델 생성
    /// 2. ItemPickupManager 등록
    /// </summary>
    protected virtual void OnInitialize()
    {
        UpdateItemVisuals();

        if (ItemPickupManager.Instance != null)
        {
            ItemPickupManager.Instance.RegisterItem(this);
        }
    }

    /// <summary>
    /// 파괴 시 ItemPickupManager에서 제거
    /// </summary>
    private void OnDestroy()
    {
        if (ItemPickupManager.Instance != null)
        {
            ItemPickupManager.Instance.UnregisterItem(this);
        }
    }

    /// <summary>
    /// 3D 모델 생성
    /// 
    /// 처리:
    /// 1. 기존 자식 오브젝트 제거
    /// 2. ItemData.ItemModel 인스턴스 생성
    /// 3. 자식으로 추가
    /// </summary>
    private void UpdateItemVisuals()
    {
        // 기존 모델 제거
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // 새 모델 생성
        if (_itemData != null && _itemData.ItemModel != null)
        {
            GameObject model = Instantiate(_itemData.ItemModel, transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// 아이템 장착
    /// 하위 클래스에서 구현 필요
    /// 
    /// WeaponItem: EquipmentManager.EquipItem()
    /// EquipmentItem: EquipmentManager.EquipItem()
    /// </summary>
    public abstract void Equip();

    /// <summary>
    /// 아이템 해제
    /// 하위 클래스에서 구현 필요
    /// </summary>
    public abstract void Unequip();

    /// <summary>
    /// 아이템 줍기
    /// ItemPickupManager.PickupItem()에서 호출
    /// 
    /// 기본 동작:
    /// 1. 로그 출력
    /// 2. 장착 시도
    /// 3. 월드에서 제거
    /// 
    /// 하위 클래스에서 오버라이드 가능
    /// </summary>
    public virtual void OnPickup()
    {
        Debug.Log($"아이템 획득: {ItemName}");
        Equip();
        Destroy(gameObject);
    }
}
