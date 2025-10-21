using DG.Tweening;
using UnityEngine;

/// <summary>
/// 문 상호작용 구현
/// 
/// ⭐ Task 3 업데이트:
/// - GetInteractionType() 구현
/// - GetInteractionName() 구현
/// </summary>
public class Door : MonoBehaviour, IInteractable
{
    [Header("문 설정")]
    [SerializeField] private string _doorName = "문";
    [SerializeField] private float _doorTime = 0.5f;
    [SerializeField] private Ease _doorEase = Ease.OutQuad;
    [SerializeField] private float _interactionRange = 3.0f;

    private bool _isOpening = false;
    private bool _doorOpen = false;

    #region IInteractable 구현

    public void Interact(PlayerCharacter player)
    {
        ToggleDoor();
    }

    public string GetInteractionPrompt()
    {
        // 호환성 유지용 (더 이상 사용 안 함)
        return _doorOpen ? "문 닫기" : "문 열기";
    }

    public bool CanInteract(PlayerCharacter player)
    {
        return !_isOpening;
    }

    public float GetInteractionRange()
    {
        return _interactionRange;
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.Door;
    }

    public string GetInteractionName()
    {
        return _doorName;
    }

    #endregion

    #region 문 동작

    private void ToggleDoor()
    {
        if (_isOpening) return;

        _isOpening = true;
        _doorOpen = !_doorOpen;

        Vector3 rotation = transform.rotation.eulerAngles;
        rotation.y += _doorOpen ? 90 : -90;

        transform.DORotate(rotation, _doorTime)
            .SetEase(_doorEase)
            .OnComplete(() => _isOpening = false);
    }

    #endregion

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
