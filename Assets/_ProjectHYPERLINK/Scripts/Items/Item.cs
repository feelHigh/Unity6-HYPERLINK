using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 월드 아이템 인스턴스의 추상 베이스 클래스
/// </summary>
public abstract class Item : MonoBehaviour
{
    [Header("아이템 데이터 참조")]
    [SerializeField] protected ItemData _itemData;

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
        : (_itemData?.Quality ?? ItemQuality.StandardEmblem);  // ← Fixed: StandardEmblem instead of Normal

    public List<ItemStat> Stats => _hasRuntimeOverrides && _runtimeStats != null
        ? _runtimeStats
        : _itemData?.ProceduralStats;

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

    public abstract void Equip();
    public abstract void Unequip();

    public virtual void OnPickup()
    {
        Debug.Log($"아이템 획득: {ItemName}");
        Equip();
        Destroy(gameObject);
    }
}
