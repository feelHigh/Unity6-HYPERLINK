using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 오디오 관리자 (싱글톤)
/// 
/// 역할:
/// - Master/SFX/BGM 볼륨 제어
/// - 카테고리별 사운드 재생
/// - GameSoundLibrary를 통한 사운드 에셋 접근
/// - BGM 페이드 인/아웃 및 전환
/// 
/// 사용법:
/// AudioManager.Instance.PlaySFX(AudioManager.Instance.SoundLibrary.PlayerBasicAttack);
/// AudioManager.Instance.PlayBGM(bgmClip);
/// AudioManager.Instance.SetMasterVolume(0.8f);
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region 싱글톤

    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    #endregion

    #region 설정

    [Header("사운드 라이브러리")]
    [Tooltip("모든 게임 사운드 에셋을 포함하는 ScriptableObject")]
    [SerializeField] private GameSoundLibrary _soundLibrary;

    [Header("Audio Mixer (선택사항)")]
    [Tooltip("Unity Audio Mixer를 사용하는 경우 할당")]
    [SerializeField] private AudioMixer _audioMixer;

    [Header("오디오 소스")]
    [Tooltip("SFX 재생용 AudioSource Pool 크기")]
    [SerializeField] private int _sfxSourcePoolSize = 10;

    #endregion

    #region 내부 상태

    private AudioSource _bgmSource;
    private AudioSource[] _sfxSources;
    private int _currentSfxIndex = 0;

    // 볼륨 설정 (0.0 ~ 1.0)
    private float _masterVolume = 1f;
    private float _sfxVolume = 1f;
    private float _bgmVolume = 1f;

    // Audio Mixer 파라미터 이름
    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";
    private const string BGM_VOLUME_PARAM = "BGMVolume";

    #endregion

    #region 프로퍼티

    public GameSoundLibrary SoundLibrary => _soundLibrary;
    public float MasterVolume => _masterVolume;
    public float SFXVolume => _sfxVolume;
    public float BGMVolume => _bgmVolume;

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

        InitializeAudioSources();
        LoadVolumeSettings();
    }

    /// <summary>
    /// 오디오 소스 초기화
    /// </summary>
    private void InitializeAudioSources()
    {
        // BGM용 AudioSource 생성
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume = _bgmVolume;

        // SFX용 AudioSource Pool 생성
        _sfxSources = new AudioSource[_sfxSourcePoolSize];
        for (int i = 0; i < _sfxSourcePoolSize; i++)
        {
            _sfxSources[i] = gameObject.AddComponent<AudioSource>();
            _sfxSources[i].loop = false;
            _sfxSources[i].playOnAwake = false;
            _sfxSources[i].volume = _sfxVolume;
        }

        Debug.Log($"[AudioManager] 오디오 소스 초기화 완료 - BGM: 1, SFX Pool: {_sfxSourcePoolSize}");
    }

    /// <summary>
    /// SettingsManager에서 볼륨 설정 로드
    /// </summary>
    private void LoadVolumeSettings()
    {
        if (SettingsManager.Instance != null)
        {
            var settings = SettingsManager.Instance.AppliedSettings;
            SetMasterVolume(settings.masterVolume);
            SetSFXVolume(settings.sfxVolume);
            SetBGMVolume(settings.bgmVolume);

            // SettingsManager 이벤트 구독 (static 이벤트)
            SettingsManager.OnSettingsApplied += HandleSettingsApplied;
        }
    }

    private void OnDestroy()
    {
        // static 이벤트 구독 해제
        SettingsManager.OnSettingsApplied -= HandleSettingsApplied;
    }

    /// <summary>
    /// 설정 변경 이벤트 핸들러
    /// </summary>
    private void HandleSettingsApplied(SettingsData settings)
    {
        SetMasterVolume(settings.masterVolume);
        SetSFXVolume(settings.sfxVolume);
        SetBGMVolume(settings.bgmVolume);
    }

    #endregion

    #region 볼륨 제어

    /// <summary>
    /// Master 볼륨 설정 (0.0 ~ 1.0)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);

        // Audio Mixer 사용 시
        if (_audioMixer != null)
        {
            // 데시벨로 변환: -80dB ~ 0dB
            float db = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
            _audioMixer.SetFloat(MASTER_VOLUME_PARAM, db);
        }
        else
        {
            // AudioListener 직접 제어
            AudioListener.volume = _masterVolume;
        }

        Debug.Log($"[AudioManager] Master 볼륨 설정: {_masterVolume:F2} ({_masterVolume * 100:F0}%)");
    }

    /// <summary>
    /// SFX 볼륨 설정 (0.0 ~ 1.0)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);

        // Audio Mixer 사용 시
        if (_audioMixer != null)
        {
            float db = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
            _audioMixer.SetFloat(SFX_VOLUME_PARAM, db);
        }
        else
        {
            // 모든 SFX AudioSource 볼륨 업데이트
            foreach (var source in _sfxSources)
            {
                source.volume = _sfxVolume * _masterVolume;
            }
        }

        Debug.Log($"[AudioManager] SFX 볼륨 설정: {_sfxVolume:F2} ({_sfxVolume * 100:F0}%)");
    }

    /// <summary>
    /// BGM 볼륨 설정 (0.0 ~ 1.0)
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);

        // Audio Mixer 사용 시
        if (_audioMixer != null)
        {
            float db = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
            _audioMixer.SetFloat(BGM_VOLUME_PARAM, db);
        }
        else
        {
            // BGM AudioSource 볼륨 업데이트
            if (_bgmSource != null)
            {
                _bgmSource.volume = _bgmVolume * _masterVolume;
            }
        }

        Debug.Log($"[AudioManager] BGM 볼륨 설정: {_bgmVolume:F2} ({_bgmVolume * 100:F0}%)");
    }

    #endregion

    #region SFX 재생

    /// <summary>
    /// SFX 재생
    /// </summary>
    /// <param name="clip">재생할 AudioClip</param>
    /// <param name="volumeScale">볼륨 스케일 (0.0 ~ 1.0)</param>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] 재생할 AudioClip이 null입니다.");
            return;
        }

        // 사용 가능한 AudioSource 찾기 (라운드 로빈)
        AudioSource source = _sfxSources[_currentSfxIndex];
        _currentSfxIndex = (_currentSfxIndex + 1) % _sfxSourcePoolSize;

        // 볼륨 계산
        float finalVolume = _audioMixer != null ? volumeScale : _sfxVolume * _masterVolume * volumeScale;

        source.volume = finalVolume;
        source.PlayOneShot(clip);
    }

    /// <summary>
    /// 3D 공간 위치에서 SFX 재생
    /// </summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] 재생할 AudioClip이 null입니다.");
            return;
        }

        float finalVolume = _audioMixer != null ? volumeScale : _sfxVolume * _masterVolume * volumeScale;
        AudioSource.PlayClipAtPoint(clip, position, finalVolume);
    }

    #endregion

    #region BGM 재생

    /// <summary>
    /// BGM 재생
    /// </summary>
    public void PlayBGM(AudioClip clip, bool fadeIn = true, float fadeDuration = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] 재생할 BGM이 null입니다.");
            return;
        }

        // 이미 같은 BGM이 재생 중이면 무시
        if (_bgmSource.clip == clip && _bgmSource.isPlaying)
        {
            return;
        }

        StopAllCoroutines();

        if (fadeIn)
        {
            StartCoroutine(FadeInBGM(clip, fadeDuration));
        }
        else
        {
            _bgmSource.clip = clip;
            _bgmSource.volume = _audioMixer != null ? 1f : _bgmVolume * _masterVolume;
            _bgmSource.Play();
        }

        Debug.Log($"[AudioManager] BGM 재생: {clip.name}");
    }

    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM(bool fadeOut = true, float fadeDuration = 1f)
    {
        StopAllCoroutines();

        if (fadeOut)
        {
            StartCoroutine(FadeOutBGM(fadeDuration));
        }
        else
        {
            _bgmSource.Stop();
        }
    }

    /// <summary>
    /// BGM 페이드 인
    /// </summary>
    private System.Collections.IEnumerator FadeInBGM(AudioClip clip, float duration)
    {
        _bgmSource.clip = clip;
        _bgmSource.volume = 0f;
        _bgmSource.Play();

        float targetVolume = _audioMixer != null ? 1f : _bgmVolume * _masterVolume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }

        _bgmSource.volume = targetVolume;
    }

    /// <summary>
    /// BGM 페이드 아웃
    /// </summary>
    private System.Collections.IEnumerator FadeOutBGM(float duration)
    {
        float startVolume = _bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        _bgmSource.Stop();
        _bgmSource.volume = startVolume;
    }

    #endregion
}
