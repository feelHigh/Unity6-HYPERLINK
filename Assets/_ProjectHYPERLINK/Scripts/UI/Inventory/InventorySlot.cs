using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : Slot
{
    [SerializeField] int _col;
    [SerializeField] int _row;
    [SerializeField] Vector2Int _size;

    public Vector2Int Size => _size;

    public Vector2Int Pos { get
        {
            return new Vector2Int(_col, _row);
        }
    }
    public void Initialize(int col, int row)
    {
        _col = col;
        _row = row;
    }
    public override void GetData(ItemData data)
    {
        _data = data;
    }
    public void TestSet(bool on)
    {
        _Icon.color = on ? Color.green : Color.white;

        _hasItem = on;
    }

    public void SetColor(bool on)
    {
        _Icon.color = on ?  Color.green : Color.white;
    }
}
