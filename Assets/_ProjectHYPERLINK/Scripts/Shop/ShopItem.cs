using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] ShopUI _shop;
    [SerializeField] ItemData _data;
    [SerializeField] int _gold;
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _price;
    [SerializeField] TextMeshProUGUI _itemName;
    bool _sled = false;
    public void Initialize(ShopUI shop, ItemData data)
    {
        _shop = shop;
        _data = data;
        _gold = data.Gold;
        _price.text = $"Price : {_gold}";
        _icon.sprite = _data.ItemIcon;
        _itemName.text = _data.ItemName;
        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!_sled ) Buy();
    }

    void Buy()
    {
        if (_shop.Buy(_data))
        {
            _sled = true;
            gameObject.SetActive(false);
        }
    }
}
