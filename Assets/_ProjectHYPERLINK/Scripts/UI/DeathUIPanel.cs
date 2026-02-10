using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 플레이어 사망 시 표시되는 "You Died" UI 패널
/// 
/// 기능:
/// - 사망 시 자동 표시
/// - 딜레이 후 자동 리스폰 or 클릭으로 리스폰
/// - 페이드 인/아웃 애니메이션
/// 
/// 설정 방법:
/// 1. GameCanvas 하위에 Panel 생성
/// 2. CanvasGroup 컴포넌트 추가
/// 3. 자식으로 "You Died" 텍스트 추가
/// 4. 자식으로 "Click to Respawn" 버튼 추가
/// 5. 이 스크립트 추가 및 참조 연결
/// </summary>
public class DeathUIPanel : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _deathText;
    [SerializeField] private Button _respawnButton;
    [SerializeField] private TextMeshProUGUI _respawnButtonText;

    [Header("설정")]
    [Tooltip("자동 리스폰 딜레이 (초) - 0이면 버튼 클릭만 허용")]
    [SerializeField] private float _autoRespawnDelay = 3f;

    [Tooltip("페이드 인 속도")]
    [SerializeField] private float _fadeInDuration = 0.5f;

    [Tooltip("페이드 아웃 속도")]
    [SerializeField] private float _fadeOutDuration = 0.3f;

    [Header("텍스트")]
    [SerializeField] private string _deathMessage = "전사했습니다";
    [SerializeField] private string _respawnMessage = "클릭하여 부활";

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private Coroutine _autoRespawnCoroutine;
    private bool _isRespawnReady = false;

    #region 초기화

    private void Awake()
    {
        // CanvasGroup 검증
        if (!CanvasGroupHelper.Validate(_canvasGroup, "DeathUIPanel"))
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 버튼 이벤트 등록
        if (_respawnButton != null)
        {
            _respawnButton.onClick.AddListener(OnRespawnButtonClicked);
        }

        // 초기 UI 설정
        if (_deathText != null)
        {
            _deathText.text = _deathMessage;
        }

        if (_respawnButtonText != null)
        {
            _respawnButtonText.text = _respawnMessage;
        }

        // 시작 시 숨김
        CanvasGroupHelper.SetVisible(_canvasGroup, false);
    }

    private void OnEnable()
    {
        PlayerCharacter.OnPlayerDead += OnPlayerDied;
    }

    private void OnDisable()
    {
        PlayerCharacter.OnPlayerDead -= OnPlayerDied;

        // 코루틴 정리
        if (_autoRespawnCoroutine != null)
        {
            StopCoroutine(_autoRespawnCoroutine);
            _autoRespawnCoroutine = null;
        }
    }

    #endregion

    #region 사망 처리

    /// <summary>
    /// 플레이어 사망 이벤트 핸들러
    /// </summary>
    private void OnPlayerDied()
    {
        Log("플레이어 사망 감지 - UI 표시");
        ShowDeathPanel();
    }

    /// <summary>
    /// 사망 패널 표시
    /// </summary>
    private void ShowDeathPanel()
    {
        StartCoroutine(ShowDeathPanelCoroutine());
    }

    private IEnumerator ShowDeathPanelCoroutine()
    {
        // 페이드 인
        yield return StartCoroutine(FadeIn());

        // 리스폰 준비
        _isRespawnReady = true;

        // 버튼 활성화
        if (_respawnButton != null)
        {
            _respawnButton.interactable = true;
        }

        // 자동 리스폰 시작
        if (_autoRespawnDelay > 0f)
        {
            _autoRespawnCoroutine = StartCoroutine(AutoRespawnCountdown());
        }
    }

    /// <summary>
    /// 페이드 인 애니메이션
    /// </summary>
    private IEnumerator FadeIn()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeInDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        Log("사망 UI 표시 완료");
    }

    /// <summary>
    /// 자동 리스폰 카운트다운
    /// </summary>
    private IEnumerator AutoRespawnCountdown()
    {
        float remaining = _autoRespawnDelay;

        while (remaining > 0f)
        {
            // 버튼 텍스트 업데이트
            if (_respawnButtonText != null)
            {
                _respawnButtonText.text = $"{_respawnMessage} ({remaining:F1}초)";
            }

            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        // 자동 리스폰
        Log("자동 리스폰 실행");
        RequestRespawn();
    }

    #endregion

    #region 리스폰 처리

    /// <summary>
    /// 리스폰 버튼 클릭 핸들러
    /// </summary>
    private void OnRespawnButtonClicked()
    {
        if (!_isRespawnReady)
        {
            LogWarning("아직 리스폰 준비가 되지 않았습니다");
            return;
        }

        Log("수동 리스폰 요청");
        RequestRespawn();
    }

    /// <summary>
    /// 리스폰 요청
    /// </summary>
    private void RequestRespawn()
    {
        if (!_isRespawnReady)
            return;

        _isRespawnReady = false;

        // 자동 리스폰 코루틴 중지
        if (_autoRespawnCoroutine != null)
        {
            StopCoroutine(_autoRespawnCoroutine);
            _autoRespawnCoroutine = null;
        }

        // 페이드 아웃 후 리스폰
        StartCoroutine(HideAndRespawn());
    }

    private IEnumerator HideAndRespawn()
    {
        // 버튼 비활성화
        if (_respawnButton != null)
        {
            _respawnButton.interactable = false;
        }

        // 페이드 아웃
        yield return StartCoroutine(FadeOut());

        // PlayerDeathManager에게 리스폰 요청
        PlayerDeathManager deathManager = FindObjectOfType<PlayerDeathManager>();
        if (deathManager != null)
        {
            deathManager.RespawnPlayer();
        }
        else
        {
            LogError("PlayerDeathManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 페이드 아웃 애니메이션
    /// </summary>
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeOutDuration);
            yield return null;
        }

        CanvasGroupHelper.SetVisible(_canvasGroup, false);
        Log("사망 UI 숨김 완료");
    }

    #endregion

    #region Public API

    /// <summary>
    /// 강제로 패널 숨기기 (리스폰 완료 후 호출)
    /// </summary>
    public void HideImmediate()
    {
        _isRespawnReady = false;

        if (_autoRespawnCoroutine != null)
        {
            StopCoroutine(_autoRespawnCoroutine);
            _autoRespawnCoroutine = null;
        }

        StopAllCoroutines();
        CanvasGroupHelper.SetVisible(_canvasGroup, false);
        Log("사망 UI 즉시 숨김");
    }

    #endregion

    #region 로깅

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[DeathUIPanel] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[DeathUIPanel] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogError(string message)
    {
        Debug.LogError($"[DeathUIPanel] {message}");
    }

    #endregion
}
