using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 월드 아이템 인스턴스 기본 클래스
/// 
/// 새로운 플로우:
/// 1. 플레이어가 아이템 픽업
/// 2. 인벤토리에 추가 (ItemPickupManager가 처리)
/// 3. 플레이어가 인벤토리에서 수동 장착
/// 
/// 역할:
/// - 3D 월드에 배치된 아이템 표현
/// - 절차적 생성 지원
/// - ItemPickupManager 연동
/// </summary>
public class Item : MonoBehaviour
{
    [Header("아이템 데이터 참조")]
    [SerializeField] protected ItemData _itemData;

    // 런타임 오버라이드
    protected string _runtimeName;
    protected string _runtimeDescription;
    protected ItemQuality _runtimeQuality;
    protected List<ItemStat> _runtimeStats;
    protected bool _hasRuntimeOverrides = false;

    public ItemData ItemData
    {
        get => _itemData;
        set => _itemData = value;
    }

    public string ItemName => _hasRuntimeOverrides && !string.IsNullOrEmpty(_runtimeName)
        ? _runtimeName
        : _itemData?.ItemName;

    public string Description => _hasRuntimeOverrides && !string.IsNullOrEmpty(_runtimeDescription)
        ? _runtimeDescription
        : _itemData?.Description;

    public ItemQuality Quality => _hasRuntimeOverrides
        ? _runtimeQuality
        : (_itemData?.Quality ?? ItemQuality.StandardEmblem);

    public List<ItemStat> Stats => _hasRuntimeOverrides && _runtimeStats != null
        ? _runtimeStats
        : _itemData?.ProceduralStats;

    /// <summary>
    /// 아이템 초기화
    /// </summary>
    public virtual void Initialize(
        ItemData data,
        ItemQuality? quality = null,
        List<ItemStat> stats = null,
        string customName = null,
        string customDescription = null)
    {
        _itemData = data;

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

    protected virtual void OnInitialize()
    {
        UpdateItemVisuals();

        if (ItemPickupManager.Instance != null)
        {
            ItemPickupManager.Instance.RegisterItem(this);
        }
    }

    private void OnDestroy()
    {
        if (ItemPickupManager.Instance != null)
        {
            ItemPickupManager.Instance.UnregisterItem(this);
        }
    }

    private void UpdateItemVisuals()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if (_itemData != null && _itemData.ItemModel != null)
        {
            GameObject model = Instantiate(_itemData.ItemModel, transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// 아이템 장착
    /// 인벤토리 UI에서 호출
    /// </summary>
    //public abstract void Equip();

    /// <summary>
    /// 아이템 해제
    /// </summary>
    //public abstract void Unequip();

    /// <summary>
    /// 아이템 줍기
    /// 
    /// 기능:
    /// - 자동 장착 제거
    /// - 인벤토리 추가는 ItemPickupManager가 처리
    /// - 이 메서드는 로그만 출력 (오버라이드 가능)
    /// 
    /// 호출: ItemPickupManager.PickupItem()
    /// </summary>
    public virtual void OnPickup()
    {
        Debug.Log($"[Item] 아이템 획득: {ItemName} ({Quality})");
        // 인벤토리 추가는 ItemPickupManager에서 처리
    }
}
