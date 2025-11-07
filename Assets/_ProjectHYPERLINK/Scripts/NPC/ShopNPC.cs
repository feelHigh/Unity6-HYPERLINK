using UnityEngine;

public class ShopNPC : MonoBehaviour, IInteractable
{
    [SerializeField] ShopUI _shop;
    [SerializeField] string _name;
    [SerializeField] string _prompt;
    [SerializeField] float _range;
    [SerializeField] InteractionType _interactionType;
    public bool CanInteract(PlayerCharacter player)
    {
        return true;
    }

    public string GetInteractionName()
    {
        return _name;
    }

    public string GetInteractionPrompt()
    {
        return _prompt;
    }

    public float GetInteractionRange()
    {
        return _range;
    }

    public InteractionType GetInteractionType()
    {
        return _interactionType;
    }

    public void Interact(PlayerCharacter player)
    {
        _shop.Open(player);
    }

}
