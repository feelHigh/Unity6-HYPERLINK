using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAmInventoryTester : MonoBehaviour
{
    public ItemData data;
    public ItemDropTableData HelloIAmTestTable;
    public ItemInventory inventory;
    public void Spawn()
    {
        //for(int i=0;i<1000;i++)
        ItemSpawner.Instance.SpawnItem(Vector3.zero, HelloIAmTestTable);
    }

    public void AddInventoryItem()
    {

        inventory.GetItem(data);
    }
}
