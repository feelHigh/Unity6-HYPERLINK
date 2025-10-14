using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 레벨업 및 스킬 언락 시각 효과
/// 
/// 기능:
/// - 레벨업 파티클 이펙트
/// - 레벨업 사운드
/// - 레벨업 UI 팝업
/// - 화면 플래시 효과
/// - 스킬 언락 알림
/// 
/// 이벤트 구독:
/// - ExperienceManager.OnLevelUp
/// - PlayerCharacter.OnSkillUnlocked
/// </summary>
public class LevelUpVFX : MonoBehaviour
{
    [Header("레벨업 이펙트")]
    [SerializeField] private GameObject _levelUpParticleEffect;
    [SerializeField] private AudioClip _levelUpSound;
    [SerializeField] private Canvas _levelUpCanvas;
    [SerializeField] private TextMeshProUGUI _levelUpText;
    [SerializeField] private float _levelUpDisplayDuration = 3f;

    [Header("스킬 언락 이펙트")]
    [SerializeField] private GameObject _skillUnlockParticleEffect;
    [SerializeField] private AudioClip _skillUnlockSound;
    [SerializeField] private Canvas _skillUnlockCanvas;
    [SerializeField] private TextMeshProUGUI _skillUnlockText;
    [SerializeField] private Image _skillUnlockIcon;
    [SerializeField] private float _skillUnlockDisplayDuration = 2.5f;

    [Header("화면 효과")]
    [SerializeField] private Image _screenFlash;
    [SerializeField] private Color _flashColor = new Color(1f, 1f, 0.5f, 0.3f);
    [SerializeField] private float _flashDuration = 0.5f;

    private AudioSource _audioSource;
    private Transform _playerTransform;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (_levelUpCanvas != null)
        {
            _levelUpCanvas.gameObject.SetActive(false);
        }

        if (_skillUnlockCanvas != null)
        {
            _skillUnlockCanvas.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        ExperienceManager.OnLevelUp += HandleLevelUp;
        PlayerCharacter.OnSkillUnlocked += HandleSkillUnlock;
    }

    private void OnDisable()
    {
        ExperienceManager.OnLevelUp -= HandleLevelUp;
        PlayerCharacter.OnSkillUnlocked -= HandleSkillUnlock;
    }

    private void Start()
    {
        _playerTransform = FindFirstObjectByType<PlayerCharacter>()?.transform;
    }

    /// <summary>
    /// 레벨업 이벤트 처리
    /// </summary>
    private void HandleLevelUp(int oldLevel, int newLevel)
    {
        StartCoroutine(PlayLevelUpEffect(newLevel));
    }

    /// <summary>
    /// 스킬 언락 이벤트 처리
    /// </summary>
    private void HandleSkillUnlock(SkillData skill)
    {
        StartCoroutine(PlaySkillUnlockEffect(skill));
    }

    /// <summary>
    /// 레벨업 이펙트 재생
    /// 
    /// 순서:
    /// 1. 사운드
    /// 2. 파티클
    /// 3. 화면 플래시
    /// 4. UI 팝업 (3초)
    /// </summary>
    private IEnumerator PlayLevelUpEffect(int newLevel)
    {
        // 사운드
        if (_levelUpSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_levelUpSound);
        }

        // 파티클
        if (_levelUpParticleEffect != null && _playerTransform != null)
        {
            GameObject effect = Instantiate(
                _levelUpParticleEffect,
                _playerTransform.position,
                Quaternion.identity
            );
            Destroy(effect, 3f);
        }

        // 화면 플래시
        if (_screenFlash != null)
        {
            StartCoroutine(FlashScreen());
        }

        // UI 팝업
        if (_levelUpCanvas != null && _levelUpText != null)
        {
            _levelUpCanvas.gameObject.SetActive(true);
            _levelUpText.text = $"레벨 업!\n레벨 {newLevel}";

            yield return new WaitForSeconds(_levelUpDisplayDuration);

            _levelUpCanvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 스킬 언락 이펙트 재생
    /// </summary>
    private IEnumerator PlaySkillUnlockEffect(SkillData skill)
    {
        // 사운드
        if (_skillUnlockSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_skillUnlockSound);
        }

        // 파티클
        if (_skillUnlockParticleEffect != null && _playerTransform != null)
        {
            GameObject effect = Instantiate(
                _skillUnlockParticleEffect,
                _playerTransform.position + Vector3.up * 2f,
                Quaternion.identity
            );
            Destroy(effect, 2f);
        }

        // UI 팝업
        if (_skillUnlockCanvas != null && _skillUnlockText != null)
        {
            _skillUnlockCanvas.gameObject.SetActive(true);
            _skillUnlockText.text = $"스킬 언락!\n{skill.SkillName}";

            if (_skillUnlockIcon != null && skill.SkillIcon != null)
            {
                _skillUnlockIcon.sprite = skill.SkillIcon;
                _skillUnlockIcon.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(_skillUnlockDisplayDuration);

            _skillUnlockCanvas.gameObject.SetActive(false);

            if (_skillUnlockIcon != null)
            {
                _skillUnlockIcon.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 화면 플래시 효과
    /// Lerp로 부드럽게 페이드 아웃
    /// </summary>
    private IEnumerator FlashScreen()
    {
        if (_screenFlash != null)
        {
            _screenFlash.color = _flashColor;
            _screenFlash.gameObject.SetActive(true);

            float elapsed = 0f;
            Color startColor = _flashColor;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            while (elapsed < _flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _flashDuration;
                _screenFlash.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }

            _screenFlash.gameObject.SetActive(false);
        }
    }
}