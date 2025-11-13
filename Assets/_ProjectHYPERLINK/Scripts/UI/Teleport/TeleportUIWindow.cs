using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 텔레포트 UI 윈도우 (CanvasGroup 기반)
/// 
/// 기능:
/// - 씬별로 그룹화된 목적지 표시
/// - 발견된 목적지만 표시
/// - 잠금 상태에 따라 버튼 활성화/비활성화
/// - CanvasGroup으로 가시성 제어
/// 
/// GameCanvas 하위에 배치
/// </summary>
public class TeleportUIWindow : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("윈도우 패널의 CanvasGroup (필수)")]
    [SerializeField] private CanvasGroup _windowCanvasGroup;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Transform _contentContainer;
    [SerializeField] private TextMeshProUGUI _titleText;

    [Header("프리팹")]
    [SerializeField] private GameObject _sceneGroupPrefab;

    [Header("설정")]
    [SerializeField] private bool _closeOnTeleport = true;
    [Tooltip("UI 열릴 때 시간 정지")]
    [SerializeField] private bool _pauseTimeWhenOpen = true;

    [Header("키 바인딩")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.T;
    [Tooltip("ESC 키로 닫기 활성화")]
    [SerializeField] private bool _enableEscapeClose = true;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private List<TeleportSceneGroup> _spawnedGroups = new List<TeleportSceneGroup>();
    private bool _isOpen = false;

    #region Unity Lifecycle

    private void Awake()
    {
        // CanvasGroup 검증
        if (!CanvasGroupHelper.Validate(_windowCanvasGroup, "TeleportUIWindow"))
        {
            LogError("CanvasGroup이 할당되지 않았습니다! Inspector에서 설정하세요.");
        }

        // Close 버튼 이벤트
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }

        // 시작 시 닫힌 상태
        CanvasGroupHelper.SetVisible(_windowCanvasGroup, false);
        _isOpen = false;
    }

    /*  인스펙터에서 설정
    private void Start()
    {
        // 타이틀 설정
        if (_titleText != null)
        {
            _titleText.text = "텔레포트";
        }
    }
    */

    private void Update()
    {
        HandleInput();
    }

    private void OnDestroy()
    {
        ClearGroups();
    }

    #endregion

    #region 입력 처리

    /// <summary>
    /// 키 입력 처리
    /// </summary>
    private void HandleInput()
    {
        // 토글 키
        if (Input.GetKeyDown(_toggleKey))
        {
            Toggle();
        }

        // ESC 키로 닫기
        if (_enableEscapeClose && _isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// UI 열기
    /// </summary>
    public void Open()
    {
        if (_isOpen)
        {
            Log("이미 열려 있습니다");
            return;
        }

        if (TeleportManager.Instance == null)
        {
            LogError("TeleportManager를 찾을 수 없습니다");
            return;
        }

        _isOpen = true;

        // CanvasGroup으로 표시
        CanvasGroupHelper.SetVisible(_windowCanvasGroup, true);

        // 목적지 목록 갱신
        RefreshDestinations();

        // 시간 정지 (선택)
        if (_pauseTimeWhenOpen)
        {
            Time.timeScale = 0f;
        }

        Log("UI 열림");
    }

    /// <summary>
    /// UI 닫기
    /// </summary>
    public void Close()
    {
        if (!_isOpen)
        {
            Log("이미 닫혀 있습니다");
            return;
        }

        _isOpen = false;

        // CanvasGroup으로 숨김
        CanvasGroupHelper.SetVisible(_windowCanvasGroup, false);

        // 시간 재개
        if (_pauseTimeWhenOpen)
        {
            Time.timeScale = 1f;
        }

        Log("UI 닫힘");
    }

    /// <summary>
    /// UI 토글 (열기/닫기)
    /// </summary>
    public void Toggle()
    {
        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    /// <summary>
    /// UI 열림 여부 확인
    /// </summary>
    public bool IsOpen()
    {
        return _isOpen;
    }

    #endregion

    #region 내부 메서드

    /// <summary>
    /// 목적지 목록 새로고침
    /// </summary>
    private void RefreshDestinations()
    {
        // 기존 그룹들 제거
        ClearGroups();

        // TeleportManager에서 씬별로 그룹화된 목적지 가져오기
        var groupedDestinations = TeleportManager.Instance.GetDestinationsGroupedByScene();

        if (groupedDestinations.Count == 0)
        {
            LogWarning("발견된 목적지가 없습니다");
            return;
        }

        Log($"목적지 그룹 {groupedDestinations.Count}개 로드");

        // 각 씬별로 그룹 생성
        foreach (var kvp in groupedDestinations)
        {
            string sceneName = kvp.Key;
            List<TeleportDestinationData> destinations = kvp.Value;

            CreateSceneGroup(sceneName, destinations);
        }
    }

    /// <summary>
    /// 씬 그룹 생성
    /// </summary>
    private void CreateSceneGroup(string sceneName, List<TeleportDestinationData> destinations)
    {
        if (_sceneGroupPrefab == null)
        {
            LogError("SceneGroupPrefab이 설정되지 않았습니다");
            return;
        }

        if (_contentContainer == null)
        {
            LogError("ContentContainer가 설정되지 않았습니다");
            return;
        }

        GameObject groupObj = Instantiate(_sceneGroupPrefab, _contentContainer);
        TeleportSceneGroup group = groupObj.GetComponent<TeleportSceneGroup>();

        if (group != null)
        {
            group.Initialize(sceneName, destinations);
            _spawnedGroups.Add(group);
            Log($"씬 그룹 생성: {sceneName} ({destinations.Count}개 목적지)");
        }
        else
        {
            LogError("SceneGroupPrefab에 TeleportSceneGroup 컴포넌트가 없습니다");
            Destroy(groupObj);
        }
    }

    /// <summary>
    /// 모든 그룹 제거
    /// </summary>
    private void ClearGroups()
    {
        foreach (var group in _spawnedGroups)
        {
            if (group != null)
            {
                Destroy(group.gameObject);
            }
        }
        _spawnedGroups.Clear();
        Log("모든 그룹 제거");
    }

    #endregion

    #region 로깅

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[TeleportUIWindow] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[TeleportUIWindow] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[TeleportUIWindow] {message}");
    }

    #endregion
}
