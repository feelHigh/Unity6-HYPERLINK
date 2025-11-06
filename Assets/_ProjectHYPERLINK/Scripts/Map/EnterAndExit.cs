using UnityEngine;

public class EnterAndExit : MonoBehaviour, IInteractable
{
    [SerializeField] private string _direction;
    public string Direction => _direction;

    public bool CanInteract(PlayerCharacter player)
    {
        throw new System.NotImplementedException();
    }

    public string GetInteractionName()
    {
        throw new System.NotImplementedException();
    }

    public string GetInteractionPrompt()
    {
        throw new System.NotImplementedException();
    }

    public float GetInteractionRange()
    {
        throw new System.NotImplementedException();
    }

    public InteractionType GetInteractionType()
    {
        throw new System.NotImplementedException();
    }

    public void Interact(PlayerCharacter player)
    {
        throw new System.NotImplementedException();
    }
}
