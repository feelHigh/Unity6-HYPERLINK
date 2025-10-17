using UnityEngine;

public class UIPanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject _characterPanel;
    [SerializeField] private GameObject _skillPanel;

    private void Update()
    {
        // C 키로 캐릭터 패널 ON/OFF
        if (Input.GetKeyDown(KeyCode.C))
        {
            _characterPanel.SetActive(!_characterPanel.activeSelf);
        }

        // K 키로 스킬 패널 ON/OFF
        if (Input.GetKeyDown(KeyCode.K))
        {
            _skillPanel.SetActive(!_skillPanel.activeSelf);
        }
    }
}
