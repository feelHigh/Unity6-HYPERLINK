using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShopUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] InventoryItemEventHandler _dragManager;
    [SerializeField] List<ShopItem> _items = new List<ShopItem>();
    [SerializeField] ItemDropTableData _dropTableData;
    [SerializeField] PlayerCharacter _player;
    [SerializeField] bool _hasOpened = false;
    [SerializeField] TextMeshProUGUI _golds;

    public void Open(PlayerCharacter player)
    {
        if (_player == null) _player = player;
        if (!_hasOpened)
        {
            Initailize();
        }
        _golds.text = $"GOLD : {_player.CurrentGold.ToString()}";
        gameObject.SetActive(true);
    }
    private void Initailize()
    {
        _hasOpened = true;
        foreach (var item in _items)
        {
            item.Initialize(this, ItemSpawner.Instance.SpawnItemData(_dropTableData));
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _dragManager.ChangeMousePos(MousePos.Shop);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _dragManager.ChangeMousePos(MousePos.None);
    }
    public void Sell(ItemData data)
    {
        _player.AddGold(data.Gold/2);
        _golds.text = $"GOLD : {_player.CurrentGold.ToString()}";
    }

    public bool Buy(ItemData data)
    {
        if (_dragManager.Buy(data, _player))
        {
            _golds.text = $"GOLD : {_player.CurrentGold.ToString()}";
            return true;
        }
        return false;
    }
}
