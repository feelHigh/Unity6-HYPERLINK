using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬롯의 기본이 되는 스크립트.인터페이스 보단 Class 형태가 좋아보임
/// </summary>
public abstract class Slot : MonoBehaviour
{
    [SerializeField] protected bool _hasItem = false;
    [SerializeField] protected ItemData _data;
    [SerializeField] protected Image _Icon;
    public bool HasItem => _hasItem;
    public abstract void GetData(ItemData data);
    public Image Icon => _Icon;
    public ItemData Data => _data;

    public ItemData ReturnDataAndRemove()
    {
        ItemData data = _data;
        _data = null;
        return data;
    }
}
