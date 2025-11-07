using UnityEngine;

public class HelloImTester : MonoBehaviour
{
    [SerializeField] ItemInventory _inventory;
    [SerializeField] ItemData _data;
    void Start()
    {
        
    }

    public void Get()
    {
        _inventory.GetItem(_data);
    }
}
