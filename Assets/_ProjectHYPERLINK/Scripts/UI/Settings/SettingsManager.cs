using System;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// 설정 관리자 (싱글톤)
/// 
/// 역할:
/// - 설정 로드/저장 (PlayerPrefs)
/// - 설정 적용 (해상도, 오디오, 언어)
/// - AudioManager를 통한 볼륨 제어
/// </summary>
public class SettingsManager : MonoBehaviour
{
    #region Singleton

    private static SettingsManager _instance;
    public static SettingsManager Instance => _instance;

    #endregion

    #region 설정 데이터

    [Header("현재 적용된 설정")]
    [SerializeField] private SettingsData _appliedSettings;

    /// <summary>
    /// 현재 적용된 설정 (읽기 전용)
    /// </summary>
    public SettingsData AppliedSettings => _appliedSettings;

    #endregion

    #region 상수

    private const string SETTINGS_KEY = "GameSettings";

    // 지원 해상도
    private readonly ResolutionInfo[] _supportedResolutions = new ResolutionInfo[]
    {
        new ResolutionInfo(1920, 1080),
        new ResolutionInfo(1280, 720)
    };

    #endregion

    #region 이벤트

    /// <summary>
    /// 설정이 적용될 때 발생 (UI 갱신용)
    /// </summary>
    public static event Action<SettingsData> OnSettingsApplied;

    /// <summary>
    /// 언어가 변경될 때 발생 (로컬라이제이션 시스템용)
    /// </summary>
    public static event Action<Language> OnLanguageChanged;

    #endregion

    #region 초기화

    private void Awake()
    {
        // 싱글톤 설정
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 설정 로드 및 적용
        LoadSettings();
        ApplyAllSettings(_appliedSettings);

        Debug.Log("[SettingsManager] 초기화 완료");
    }

    #endregion

    #region 저장/로드

    /// <summary>
    /// 설정 로드 (PlayerPrefs)
    /// </summary>
    public void LoadSettings()
    {
        try
        {
            if (PlayerPrefs.HasKey(SETTINGS_KEY))
            {
                string json = PlayerPrefs.GetString(SETTINGS_KEY);
                _appliedSettings = JsonConvert.DeserializeObject<SettingsData>(json);
                Debug.Log("[SettingsManager] 설정 로드 완료");
            }
            else
            {
                // 기본 설정 사용
                _appliedSettings = SettingsData.GetDefault();
                SaveSettings(_appliedSettings);
                Debug.Log("[SettingsManager] 기본 설정 생성");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsManager] 설정 로드 실패: {e.Message}");
            _appliedSettings = SettingsData.GetDefault();
        }
    }

    /// <summary>
    /// 설정 저장 (PlayerPrefs)
    /// </summary>
    public void SaveSettings(SettingsData settings)
    {
        try
        {
            string json = JsonConvert.SerializeObject(settings, Formatting.None);
            PlayerPrefs.SetString(SETTINGS_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("[SettingsManager] 설정 저장 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsManager] 설정 저장 실패: {e.Message}");
        }
    }

    #endregion

    #region 설정 적용

    /// <summary>
    /// 모든 설정 적용
    /// </summary>
    public void ApplyAllSettings(SettingsData settings)
    {
        _appliedSettings = settings.Clone();

        ApplyVideoSettings(settings);
        ApplyAudioSettings(settings);
        ApplyGameSettings(settings);

        SaveSettings(_appliedSettings);
        OnSettingsApplied?.Invoke(_appliedSettings);

        Debug.Log("[SettingsManager] 모든 설정 적용 완료");
    }

    /// <summary>
    /// 비디오 설정 적용
    /// </summary>
    private void ApplyVideoSettings(SettingsData settings)
    {
        // 해상도 적용
        if (settings.resolutionIndex >= 0 && settings.resolutionIndex < _supportedResolutions.Length)
        {
            ResolutionInfo resolution = _supportedResolutions[settings.resolutionIndex];

            // 디스플레이 모드에 따라 적용
            switch ((DisplayMode)settings.displayModeIndex)
            {
                case DisplayMode.Windowed:
                    Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.Windowed);
                    Debug.Log($"[SettingsManager] 창 모드 적용: {resolution.displayName}");
                    break;

                case DisplayMode.FullscreenWindowed:
                    Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow);
                    Debug.Log($"[SettingsManager] 전체화면 창 모드 적용: {resolution.displayName}");
                    break;

                case DisplayMode.Fullscreen:
                    Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.ExclusiveFullScreen);
                    Debug.Log($"[SettingsManager] 전체화면 적용: {resolution.displayName}");
                    break;
            }
        }
    }

    /// <summary>
    /// 오디오 설정 적용 - AudioManager를 통해 모든 볼륨 제어
    /// </summary>
    private void ApplyAudioSettings(SettingsData settings)
    {
        // AudioManager를 통해 모든 볼륨 설정 적용
        // AudioManager가 Audio Mixer 또는 AudioSource를 직접 제어
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(settings.masterVolume);
            AudioManager.Instance.SetSFXVolume(settings.sfxVolume);
            AudioManager.Instance.SetBGMVolume(settings.bgmVolume);

            Debug.Log($"[SettingsManager] 오디오 설정 적용 - Master: {settings.masterVolume:F2}, SFX: {settings.sfxVolume:F2}, BGM: {settings.bgmVolume:F2}");
        }
        else
        {
            Debug.LogWarning("[SettingsManager] AudioManager를 찾을 수 없어 오디오 설정을 적용하지 못했습니다.");
        }
    }

    /// <summary>
    /// 게임 설정 적용 (언어 등)
    /// </summary>
    private void ApplyGameSettings(SettingsData settings)
    {
        Language newLanguage = (Language)settings.languageIndex;
        OnLanguageChanged?.Invoke(newLanguage);

        Debug.Log($"[SettingsManager] 언어 설정: {newLanguage}");

        // TODO: 로컬라이제이션 시스템과 연동
    }

    #endregion

    #region 헬퍼 메서드

    /// <summary>
    /// 지원 해상도 목록 반환
    /// </summary>
    public ResolutionInfo[] GetSupportedResolutions()
    {
        return _supportedResolutions;
    }

    /// <summary>
    /// 디스플레이 모드 이름 반환
    /// </summary>
    public string GetDisplayModeName(int index)
    {
        switch ((DisplayMode)index)
        {
            case DisplayMode.Windowed:
                return "창 모드";
            case DisplayMode.FullscreenWindowed:
                return "전체화면 창 모드";
            case DisplayMode.Fullscreen:
                return "전체화면";
            default:
                return "알 수 없음";
        }
    }

    /// <summary>
    /// 언어 이름 반환
    /// </summary>
    public string GetLanguageName(int index)
    {
        switch ((Language)index)
        {
            case Language.English:
                return "English";
            case Language.Korean:
                return "한국어";
            case Language.SimplifiedChinese:
                return "简体中文";
            default:
                return "Unknown";
        }
    }

    #endregion
}
