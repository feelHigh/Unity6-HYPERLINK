using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 UI 컨트롤러
///
/// 보스 이름 및 체력 표시
/// </summary>
public class BossUIController : MonoBehaviour
{
    [Header("----- UI 요소 -----")]
    [SerializeField] GameObject _bossHealthPanel;
    [SerializeField] TextMeshProUGUI _bossNameText;
    [SerializeField] Image _bossHealthBar;

    [Header("----- 참조 -----")]
    [SerializeField] EnemyController _enemyController;

    private void Start()
    {
        if (_bossHealthPanel != null)
        {
            _bossHealthPanel.SetActive(false);
        }
    }
}
