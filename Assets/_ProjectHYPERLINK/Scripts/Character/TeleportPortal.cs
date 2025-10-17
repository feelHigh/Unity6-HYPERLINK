using UnityEngine;

/// <summary>
/// 상호작용 가능한 텔레포트 포탈
/// </summary>
[RequireComponent(typeof(Collider))]
public class TeleportPortal : MonoBehaviour
{
    [SerializeField] private string _destinationName;
    [SerializeField] private bool _requireInteraction = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_requireInteraction && other.CompareTag("Player"))
        {
            PlayerSpawner.Instance.TeleportToLocation(_destinationName);
        }
    }

    // UI 버튼 또는 E키 눌러서 작동
    public void Activate()
    {
        if (_requireInteraction)
        {
            PlayerSpawner.Instance.TeleportToLocation(_destinationName);
        }
    }
}
