using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
 
public class Door : MonoBehaviour, IInteractable
{
   
    [SerializeField] float _doorTime;
    [SerializeField] Ease _doorEase;

    bool _isopening = false;
    bool _doorOpen = false;


    void DoorOpen()
    {
        if (_isopening) return;
        _isopening = true;
        Vector3 rotate = transform.rotation.eulerAngles;
        _doorOpen = !_doorOpen;
        if (_doorOpen)
        {
            rotate.y += 90;
        }
        else
        {
            rotate.y -= 90;
        }
        transform.DORotate(rotate, _doorTime).SetEase(_doorEase)
            .OnComplete(() => _isopening = false
            );
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    public void Interact(PlayerCharacter player)
    {
        DoorOpen();
    }

    public string GetInteractionPrompt()
    {
        return "hi";
    }

    public bool CanInteract(PlayerCharacter player)
    {
        return true;
    }

    public float GetInteractionRange()
    {
        return 10f;
    }
}
