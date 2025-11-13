using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Settings;

/// <summary>
/// 텔레포트 목적지 버튼
/// 
/// 기능:
/// - 목적지 이름 표시
/// - 잠금 상태 표시
/// - 클릭 시 텔레포트 실행
/// </summary>
public class TeleportDestinationButton : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Image _lockIcon;
    [SerializeField] private Image _backgroundImage;

    [Header("색상 설정")]
    [SerializeField] private Color _unlockedColor = new Color(0.2f, 0.8f, 0.3f, 0.8f);
    [SerializeField] private Color _lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

    private TeleportDestinationData _destination;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        _button.onClick.AddListener(OnButtonClicked);
    }

    /// <summary>
    /// 버튼 초기화
    /// </summary>
    public void Initialize(TeleportDestinationData destination)
    {
        _destination = destination;

        if (_destination == null)
        {
            Debug.LogError("[TeleportDestinationButton] Destination이 null입니다");
            return;
        }

        UpdateUI();
    }

    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        bool isUnlocked = _destination.IsUnlocked();

        // 이름 텍스트
        if (_nameText != null)
        {
            string name = LocalizationSettings.StringDatabase.GetLocalizedString(
                "DATA_PORTAL", "PORTAL_NAME_" + _destination.DisplayName.ToUpper());

            _nameText.text = name;
            _nameText.color = isUnlocked ? Color.white : Color.gray;
        }

        // 설명 텍스트
        if (_descriptionText != null)
        {
            if (isUnlocked)
            {
                string desc = LocalizationSettings.StringDatabase.GetLocalizedString(
                    "DATA_PORTAL", "PORTAL_DESC_" + _destination.DisplayName.ToUpper());
                _descriptionText.text = desc;
            }
            else
            {
                string text = LocalizationSettings.StringDatabase.GetLocalizedString(
                    "UI_PLAY", "PLAY_MSG_PORTAL_LOCKED");
                _descriptionText.text = text;
            }
            _descriptionText.color = isUnlocked ? new Color(0.8f, 0.8f, 0.8f) : Color.gray;
        }

        // 잠금 아이콘
        if (_lockIcon != null)
        {
            _lockIcon.gameObject.SetActive(!isUnlocked);
        }

        // 배경 색상
        if (_backgroundImage != null)
        {
            _backgroundImage.color = isUnlocked ? _unlockedColor : _lockedColor;
        }

        // 버튼 상호작용
        _button.interactable = isUnlocked;
    }

    /// <summary>
    /// 버튼 클릭 처리
    /// </summary>
    private void OnButtonClicked()
    {
        if (_destination == null)
        {
            Debug.LogError("[TeleportDestinationButton] Destination이 null입니다");
            return;
        }

        if (!_destination.IsUnlocked())
        {
            Debug.LogWarning("[TeleportDestinationButton] 잠긴 목적지입니다");
            return;
        }

        // TeleportManager를 통해 텔레포트
        if (TeleportManager.Instance != null)
        {
            TeleportManager.Instance.TeleportToDestination(_destination);

            // UI 닫기
            TeleportUIWindow window = GetComponentInParent<TeleportUIWindow>();
            if (window != null)
            {
                window.Close();
            }
        }
        else
        {
            Debug.LogError("[TeleportDestinationButton] TeleportManager를 찾을 수 없습니다");
        }
    }
}
