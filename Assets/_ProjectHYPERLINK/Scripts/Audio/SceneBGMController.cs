using UnityEngine;

/// <summary>
/// 씬 BGM 관리자
/// 
/// 역할:
/// - 씬이 로드되면 자동으로 BGM 재생
/// - 씬마다 고유한 BGM 설정 가능
/// - AudioManager와 연동하여 Music 볼륨 적용
/// 
/// 사용법:
/// 1. 각 씬의 빈 GameObject에 컴포넌트 추가
/// 2. Inspector에서 BGM AudioClip 할당
/// 3. 씬이 로드되면 자동으로 BGM 재생됨
/// </summary>
public class SceneBGMController : MonoBehaviour
{
    [Header("씬 BGM 설정")]
    [Tooltip("이 씬에서 재생할 BGM AudioClip")]
    [SerializeField] private AudioClip _sceneBGM;

    [Tooltip("씬 로드 시 페이드 인 사용 여부")]
    [SerializeField] private bool _useFadeIn = true;

    [Tooltip("페이드 인 지속 시간 (초)")]
    [SerializeField] private float _fadeInDuration = 2f;

    [Tooltip("이전 씬의 BGM이 재생 중일 경우 정지할지 여부")]
    [SerializeField] private bool _stopPreviousBGM = true;

    private void Start()
    {
        PlaySceneBGM();
    }

    /// <summary>
    /// 씬 BGM 재생
    /// </summary>
    private void PlaySceneBGM()
    {
        if (_sceneBGM == null)
        {
            Debug.LogWarning($"[SceneBGMController] {gameObject.scene.name} 씬에 BGM이 설정되지 않았습니다.");
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogError("[SceneBGMController] AudioManager 인스턴스를 찾을 수 없습니다!");
            return;
        }

        // 이전 BGM 정지 옵션
        if (_stopPreviousBGM)
        {
            AudioManager.Instance.StopBGM(fadeOut: true, fadeDuration: 1f);
        }

        // 새 BGM 재생
        AudioManager.Instance.PlayBGM(_sceneBGM, fadeIn: _useFadeIn, fadeDuration: _fadeInDuration);

        Debug.Log($"[SceneBGMController] {gameObject.scene.name} 씬 BGM 재생: {_sceneBGM.name}");
    }

    /// <summary>
    /// BGM 변경 (런타임에서 호출 가능)
    /// </summary>
    public void ChangeBGM(AudioClip newBGM, bool fadeTransition = true)
    {
        if (newBGM == null)
        {
            Debug.LogWarning("[SceneBGMController] 변경할 BGM이 null입니다.");
            return;
        }

        _sceneBGM = newBGM;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(newBGM, fadeIn: fadeTransition, fadeDuration: _fadeInDuration);
        }
    }

    /// <summary>
    /// BGM 일시 정지
    /// </summary>
    public void PauseBGM()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM(fadeOut: false);
        }
    }

    /// <summary>
    /// BGM 재개
    /// </summary>
    public void ResumeBGM()
    {
        if (_sceneBGM != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(_sceneBGM, fadeIn: false);
        }
    }
}
