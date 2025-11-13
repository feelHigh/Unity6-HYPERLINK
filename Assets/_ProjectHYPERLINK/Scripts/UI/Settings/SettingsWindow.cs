using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Localization.Settings;

/// <summary>
/// 설정 윈도우 (Prefab 호환)
/// 
/// CanvasGroup 패턴 사용
/// SettingsManager 초기화 순서 대응
/// 모든 패널 CanvasGroup으로 관리
/// </summary>
public class SettingsWindow : MonoBehaviour
{
    #region UI 참조 - Tab Buttons

    [Header("탭 버튼")]
    [SerializeField] private Toggle _videoTabButton;
    [SerializeField] private Toggle _audioTabButton;
    [SerializeField] private Toggle _gameTabButton;

    #endregion

    #region UI 참조 - Panels (CanvasGroup 필수)

    [Header("패널 (CanvasGroup)")]
    [SerializeField] private CanvasGroup _videoCanvasGroup;
    [SerializeField] private CanvasGroup _audioCanvasGroup;
    [SerializeField] private CanvasGroup _gameCanvasGroup;

    #endregion

    #region UI 참조 - Video Settings

    [Header("비디오 설정")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private TMP_Dropdown _displayModeDropdown;

    #endregion

    #region UI 참조 - Audio Settings

    [Header("오디오 설정")]
    [SerializeField] private Slider _masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI _masterVolumeText;

    [SerializeField] private Slider _sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI _sfxVolumeText;

    [SerializeField] private Slider _bgmVolumeSlider;
    [SerializeField] private TextMeshProUGUI _bgmVolumeText;

    #endregion

    #region UI 참조 - Game Settings

    [Header("게임 설정")]
    [SerializeField] private TMP_Dropdown _languageDropdown;

    #endregion

    #region UI 참조 - Bottom Buttons

    [Header("하단 버튼")]
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _acceptButton;

    #endregion

    #region 내부 상태

    private CanvasGroup _canvasGroup;
    private SettingsData _tempSettings;
    private bool _isInitialized = false;

    #endregion

    #region 초기화

    private void Awake()
    {
        if (!Application.isPlaying)
            return;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            Debug.LogError("[SettingsWindow] CanvasGroup이 없습니다!");
        }

        CanvasGroupHelper.SetVisible(_canvasGroup, false);

        // 내부 패널 CanvasGroup 검증
        ValidatePanelCanvasGroups();
    }

    private void ValidatePanelCanvasGroups()
    {
        CanvasGroupHelper.Validate(_videoCanvasGroup, "Video Panel");
        CanvasGroupHelper.Validate(_audioCanvasGroup, "Audio Panel");
        CanvasGroupHelper.Validate(_gameCanvasGroup, "Game Panel");
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        // SettingsManager 대기 후 초기화
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("[SettingsWindow] SettingsManager가 아직 준비되지 않음, 재시도 중...");
            StartCoroutine(WaitForSettingsManager());
        }
        else
        {
            InitializeUI();
            _isInitialized = true;
        }
    }

    private IEnumerator WaitForSettingsManager()
    {
        int attempts = 0;
        while (SettingsManager.Instance == null && attempts < 20)
        {
            yield return new WaitForSeconds(0.2f);
            attempts++;
        }

        if (SettingsManager.Instance == null)
        {
            Debug.LogError("[SettingsWindow] SettingsManager를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log("[SettingsWindow] SettingsManager 준비 완료");
            InitializeUI();
            _isInitialized = true;
        }
    }

    private void InitializeUI()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("[SettingsWindow] SettingsManager가 없어 초기화 불가");
            return;
        }

        // 탭 버튼 이벤트
        if (_videoTabButton != null)
        {
            _videoTabButton.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.Video); });
            _videoTabButton.isOn = true;
        }
        if (_audioTabButton != null)
            _audioTabButton.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.Audio); });
        if (_gameTabButton != null)
            _gameTabButton.onValueChanged.AddListener((isOn) => { if (isOn) ShowPanel(PanelType.Game); });

        // 해상도 드롭다운
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.ClearOptions();
            ResolutionInfo[] resolutions = SettingsManager.Instance.GetSupportedResolutions();

            var optionsList = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            foreach (var res in resolutions)
            {
                optionsList.Add(new TMP_Dropdown.OptionData(res.displayName));
            }
            _resolutionDropdown.AddOptions(optionsList);
            _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        // 디스플레이 모드 드롭다운
        if (_displayModeDropdown != null)
        {
            _displayModeDropdown.ClearOptions();
            var optionsList = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();

            string windowed = LocalizationSettings.StringDatabase.GetLocalizedString(
                "UI_SETTINGS", "SETTINGS_VIDEO_MODE_WINDOWED");
            string borderless = LocalizationSettings.StringDatabase.GetLocalizedString(
                "UI_SETTINGS", "SETTINGS_VIDEO_MODE_BORDERLESS");
            string fullScreen = LocalizationSettings.StringDatabase.GetLocalizedString(
                "UI_SETTINGS", "SETTINGS_VIDEO_MODE_FULLSCREEN");

            optionsList.Add(new TMP_Dropdown.OptionData(windowed));
            optionsList.Add(new TMP_Dropdown.OptionData(borderless));
            optionsList.Add(new TMP_Dropdown.OptionData(fullScreen));
            _displayModeDropdown.AddOptions(optionsList);
            _displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
        }

        // 언어 드롭다운
        if (_languageDropdown != null)
        {
            _languageDropdown.ClearOptions();
            var optionsList = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();

            string english = LocalizationSettings.StringDatabase.GetLocalizedString(
                "SYSTEM_COMMON", "SYSTEM_LANGUAGE_EN");
            string korean = LocalizationSettings.StringDatabase.GetLocalizedString(
                "SYSTEM_COMMON", "SYSTEM_LANGUAGE_KR");
            string chinese = LocalizationSettings.StringDatabase.GetLocalizedString(
                "SYSTEM_COMMON", "SYSTEM_LANGUAGE_ZH");

            optionsList.Add(new TMP_Dropdown.OptionData(english));
            optionsList.Add(new TMP_Dropdown.OptionData(korean));
            optionsList.Add(new TMP_Dropdown.OptionData(chinese));
            _languageDropdown.AddOptions(optionsList);
            _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        // 볼륨 슬라이더
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.minValue = 0f;
            _masterVolumeSlider.maxValue = 1f;
            _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.minValue = 0f;
            _sfxVolumeSlider.maxValue = 1f;
            _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.minValue = 0f;
            _bgmVolumeSlider.maxValue = 1f;
            _bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        // 하단 버튼
        if (_cancelButton != null)
            _cancelButton.onClick.AddListener(OnCancelClicked);
        if (_acceptButton != null)
            _acceptButton.onClick.AddListener(OnAcceptClicked);

        Debug.Log("[SettingsWindow] UI 초기화 완료");
    }

    #endregion

    #region 윈도우 열기/닫기

    public void Open()
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[SettingsWindow] 아직 초기화되지 않음");
            return;
        }

        CanvasGroupHelper.SetVisible(_canvasGroup, true);

        _tempSettings = SettingsManager.Instance.AppliedSettings.Clone();
        LoadSettingsToUI();
        ShowPanel(PanelType.Video);

        Debug.Log("[SettingsWindow] 윈도우 열림");
    }

    public void Close()
    {
        CanvasGroupHelper.SetVisible(_canvasGroup, false);
        Debug.Log("[SettingsWindow] 윈도우 닫힘");
    }

    public bool IsVisible()
    {
        return CanvasGroupHelper.IsVisible(_canvasGroup);
    }

    #endregion

    #region 패널 전환

    private enum PanelType
    {
        Video,
        Audio,
        Game
    }

    private void ShowPanel(PanelType panelType)
    {
        // 모든 패널 숨기기
        CanvasGroupHelper.SetVisible(_videoCanvasGroup, false);
        CanvasGroupHelper.SetVisible(_audioCanvasGroup, false);
        CanvasGroupHelper.SetVisible(_gameCanvasGroup, false);

        // 선택된 패널만 표시
        switch (panelType)
        {
            case PanelType.Video:
                CanvasGroupHelper.SetVisible(_videoCanvasGroup, true);
                break;
            case PanelType.Audio:
                CanvasGroupHelper.SetVisible(_audioCanvasGroup, true);
                break;
            case PanelType.Game:
                CanvasGroupHelper.SetVisible(_gameCanvasGroup, true);
                break;
        }
    }

    #endregion

    #region UI에 설정 로드

    private void LoadSettingsToUI()
    {
        if (_tempSettings == null) return;

        if (_resolutionDropdown != null)
            _resolutionDropdown.SetValueWithoutNotify(_tempSettings.resolutionIndex);

        if (_displayModeDropdown != null)
            _displayModeDropdown.SetValueWithoutNotify(_tempSettings.displayModeIndex);

        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.SetValueWithoutNotify(_tempSettings.masterVolume);
            UpdateVolumeText(_masterVolumeText, _tempSettings.masterVolume);
        }
        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.SetValueWithoutNotify(_tempSettings.sfxVolume);
            UpdateVolumeText(_sfxVolumeText, _tempSettings.sfxVolume);
        }
        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.SetValueWithoutNotify(_tempSettings.bgmVolume);
            UpdateVolumeText(_bgmVolumeText, _tempSettings.bgmVolume);
        }

        if (_languageDropdown != null)
            _languageDropdown.SetValueWithoutNotify(_tempSettings.languageIndex);
    }

    #endregion

    #region UI 이벤트 핸들러

    private void OnResolutionChanged(int index)
    {
        if (_tempSettings != null)
        {
            _tempSettings.resolutionIndex = index;
            Debug.Log($"[SettingsWindow] 해상도 변경: {index}");
        }
    }

    private void OnDisplayModeChanged(int index)
    {
        if (_tempSettings != null)
        {
            _tempSettings.displayModeIndex = index;
            Debug.Log($"[SettingsWindow] 디스플레이 모드 변경: {index}");
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (_tempSettings != null)
        {
            _tempSettings.masterVolume = value;
            UpdateVolumeText(_masterVolumeText, value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (_tempSettings != null)
        {
            _tempSettings.sfxVolume = value;
            UpdateVolumeText(_sfxVolumeText, value);
        }
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (_tempSettings != null)
        {
            _tempSettings.bgmVolume = value;
            UpdateVolumeText(_bgmVolumeText, value);
        }
    }

    private void OnLanguageChanged(int index)
    {
        if (_tempSettings != null)
        {
            _tempSettings.languageIndex = index;
            Debug.Log($"[SettingsWindow] 언어 변경: {index}");
        }
    }

    private void UpdateVolumeText(TextMeshProUGUI text, float value)
    {
        if (text != null)
        {
            text.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    #endregion

    #region 버튼 이벤트

    private void OnCancelClicked()
    {
        Debug.Log("[SettingsWindow] Cancel 클릭 - 변경사항 취소");
        Close();
    }

    private void OnAcceptClicked()
    {
        Debug.Log("[SettingsWindow] Accept 클릭 - 설정 적용");

        if (_tempSettings != null && SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplyAllSettings(_tempSettings);
        }

        Close();
    }

    #endregion

    #region 에디터 헬퍼

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        if (_videoTabButton == null)
            Debug.LogWarning("[SettingsWindow] Video Tab Button이 할당되지 않았습니다!");

        if (_videoCanvasGroup == null)
            Debug.LogWarning("[SettingsWindow] Video CanvasGroup이 할당되지 않았습니다!");

        if (_audioCanvasGroup == null)
            Debug.LogWarning("[SettingsWindow] Audio CanvasGroup이 할당되지 않았습니다!");

        if (_gameCanvasGroup == null)
            Debug.LogWarning("[SettingsWindow] Game CanvasGroup이 할당되지 않았습니다!");
    }
#endif

    #endregion
}
