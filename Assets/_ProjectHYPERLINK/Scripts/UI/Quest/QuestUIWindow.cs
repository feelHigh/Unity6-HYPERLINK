using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

/// <summary>
/// 퀘스트 UI 윈도우 (Prefab 호환)
/// 
/// 수정 사항:
/// - QuestManager 초기화 완료 이벤트 구독
/// - 초기화 완료 후에만 UI 갱신
/// - 타이밍 이슈 해결
/// 
/// 역할:
/// - 활성 퀘스트 목록 표시
/// - QuestManager 이벤트 구독
/// - 퀘스트 진행 상황 실시간 업데이트
/// - QuestUIItem 풀링 관리
/// </summary>
public class QuestUIWindow : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Transform _questItemContainer;
    [SerializeField] private QuestUIItem _questItemPrefab;
    [SerializeField] private GameObject _emptyMessageObject;
    [SerializeField] private TextMeshProUGUI _emptyMessageText;

    [Header("설정")]
    [SerializeField] private bool _enableDebugLogs = true;
    [SerializeField] private float _updateInterval = 0.5f;

    // 내부 상태
    private Dictionary<string, QuestUIItem> _activeQuestItems = new Dictionary<string, QuestUIItem>();
    private List<QuestUIItem> _itemPool = new List<QuestUIItem>();
    private float _lastUpdateTime;
    private bool _isInitialized = false;
    private CanvasGroup _canvasGroup;

    #region Unity Lifecycle

    private void Awake()
    {
        if (!Application.isPlaying)
            return;

        // CanvasGroup 찾기 (CharacterUIController가 제어)
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            Debug.LogWarning("[QuestUIWindow] CanvasGroup이 없습니다! CharacterUIController에서 제어하려면 필요합니다.");
        }

        InitializeUI();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        // [수정] QuestManager가 준비되면 구독
        if (_isInitialized && QuestManager.Instance != null)
        {
            SubscribeToQuestEvents();
            RefreshQuestList();
        }
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        UnsubscribeFromQuestEvents();
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        // Start에서 한 번 더 초기화 보장
        if (!_isInitialized)
        {
            InitializeUI();
        }

        // QuestManager 연결 시도
        if (QuestManager.Instance != null)
        {
            // [수정] QuestManager가 이미 초기화되었는지 확인
            if (QuestManager.Instance.IsInitialized)
            {
                // 즉시 구독 및 새로고침
                SubscribeToQuestEvents();
                RefreshQuestList();
                _isInitialized = true;
            }
            else
            {
                // 초기화 이벤트 대기
                StartCoroutine(WaitForQuestManagerInitialization());
            }
        }
        else
        {
            // QuestManager 인스턴스 대기
            StartCoroutine(WaitForQuestManager());
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!_isInitialized)
            return;

        // CanvasGroup으로 가시성 확인
        if (_canvasGroup != null && !CanvasGroupHelper.IsVisible(_canvasGroup))
            return;

        // 주기적으로 진행 상황 업데이트
        if (Time.time - _lastUpdateTime >= _updateInterval)
        {
            UpdateAllQuestProgress();
            _lastUpdateTime = Time.time;
        }
    }

    #endregion

    #region 초기화

    /// <summary>
    /// UI 기본 설정 초기화
    /// QuestManager와 무관한 UI 요소만 설정
    /// </summary>
    private void InitializeUI()
    {
        // 빈 메시지 설정
        if (_emptyMessageText != null)
        {
            string emptyText = LocalizationSettings.StringDatabase.GetLocalizedString(
                "UI_PLAY", "PLAY_MSG_NO_ACTIVE_QUEST");
            _emptyMessageText.text = emptyText;
        }

        // 초기 상태: 빈 메시지 표시
        ShowEmptyMessage(true);

        Log("QuestUIWindow UI 초기화 완료");
    }

    /// <summary>
    /// [NEW] QuestManager 초기화 완료 대기
    /// 
    /// QuestManager.Initialize()가 호출된 후에만 UI 갱신
    /// </summary>
    private IEnumerator WaitForQuestManagerInitialization()
    {
        Log("QuestManager 초기화 대기 중...");

        // QuestManager 초기화 이벤트 구독
        QuestManager.Instance.OnQuestSystemInitialized += OnQuestManagerInitialized;

        yield return null; // 이벤트가 이미 발생했을 수도 있으므로 한 프레임 대기

        // 만약 이미 초기화되었다면
        if (QuestManager.Instance.IsInitialized && !_isInitialized)
        {
            OnQuestManagerInitialized();
        }
    }

    /// <summary>
    /// [NEW] QuestManager 초기화 완료 콜백
    /// </summary>
    private void OnQuestManagerInitialized()
    {
        // 이벤트 구독 해제 (중복 호출 방지)
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestSystemInitialized -= OnQuestManagerInitialized;
        }

        Log("QuestManager 초기화 완료 감지! UI 갱신 시작");

        SubscribeToQuestEvents();
        RefreshQuestList();
        _isInitialized = true;
    }

    /// <summary>
    /// QuestManager를 기다리는 코루틴
    /// 
    /// 이유:
    /// - QuestManager가 DontDestroyOnLoad이므로 씬 로드 시 약간의 지연이 있을 수 있음
    /// - 최대 5초 동안 0.2초 간격으로 재시도
    /// </summary>
    private IEnumerator WaitForQuestManager()
    {
        float waitTime = 0f;
        const float maxWaitTime = 5f;
        const float retryInterval = 0.2f;

        while (waitTime < maxWaitTime)
        {
            if (QuestManager.Instance != null)
            {
                Log("QuestManager 발견!");

                // [수정] 초기화 완료 대기
                if (QuestManager.Instance.IsInitialized)
                {
                    SubscribeToQuestEvents();
                    RefreshQuestList();
                    _isInitialized = true;
                }
                else
                {
                    StartCoroutine(WaitForQuestManagerInitialization());
                }

                yield break;
            }

            yield return new WaitForSeconds(retryInterval);
            waitTime += retryInterval;
        }

        LogError($"QuestManager를 {maxWaitTime}초 동안 찾을 수 없습니다!");
    }

    #endregion

    #region 이벤트 구독

    /// <summary>
    /// QuestManager 이벤트 구독
    /// </summary>
    private void SubscribeToQuestEvents()
    {
        if (!Application.isPlaying || QuestManager.Instance == null)
        {
            if (Application.isPlaying)
                LogWarning("QuestManager를 찾을 수 없어 이벤트 구독을 건너뜁니다.");
            return;
        }

        // 중복 구독 방지
        UnsubscribeFromQuestEvents();

        QuestManager.Instance.OnQuestStarted += OnQuestStarted;
        QuestManager.Instance.OnQuestProgressUpdated += OnQuestProgressUpdated;
        QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;

        Log("QuestManager 이벤트 구독 완료");
    }

    /// <summary>
    /// QuestManager 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromQuestEvents()
    {
        if (!Application.isPlaying || QuestManager.Instance == null)
            return;

        QuestManager.Instance.OnQuestStarted -= OnQuestStarted;
        QuestManager.Instance.OnQuestProgressUpdated -= OnQuestProgressUpdated;
        QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;

        // [NEW] 초기화 이벤트도 구독 해제
        QuestManager.Instance.OnQuestSystemInitialized -= OnQuestManagerInitialized;
    }

    #endregion

    #region 퀘스트 이벤트 핸들러

    /// <summary>
    /// 퀘스트 시작 이벤트
    /// </summary>
    private void OnQuestStarted(QuestData questData, QuestProgress questProgress)
    {
        if (questData == null || questProgress == null)
        {
            LogWarning("OnQuestStarted: questData 또는 questProgress가 null입니다.");
            return;
        }

        Log($"퀘스트 시작: {questData.QuestName}");

        // 이미 UI에 있는지 확인
        if (_activeQuestItems.ContainsKey(questData.QuestID))
        {
            LogWarning($"이미 표시 중인 퀘스트: {questData.QuestID}");
            return;
        }

        // UI 아이템 생성 또는 풀에서 가져오기
        QuestUIItem questItem = GetOrCreateQuestItem();
        if (questItem == null)
        {
            LogError("QuestUIItem을 생성할 수 없습니다!");
            return;
        }

        questItem.Initialize(questData, questProgress);

        // 활성 목록에 추가
        _activeQuestItems[questData.QuestID] = questItem;

        // 빈 메시지 숨김
        ShowEmptyMessage(false);
    }

    /// <summary>
    /// 퀘스트 진행 업데이트 이벤트
    /// </summary>
    private void OnQuestProgressUpdated(QuestData questData, QuestProgress questProgress)
    {
        if (questData == null)
            return;

        if (_activeQuestItems.TryGetValue(questData.QuestID, out QuestUIItem questItem))
        {
            questItem.UpdateProgress();
            Log($"퀘스트 진행: {questData.QuestName}");
        }
    }

    /// <summary>
    /// 퀘스트 완료 이벤트
    /// </summary>
    private void OnQuestCompleted(QuestData questData)
    {
        if (questData == null)
            return;

        Log($"퀘스트 완료: {questData.QuestName}");

        if (_activeQuestItems.TryGetValue(questData.QuestID, out QuestUIItem questItem))
        {
            // 풀로 반환
            ReturnItemToPool(questItem);

            // 활성 목록에서 제거
            _activeQuestItems.Remove(questData.QuestID);
        }

        // 모든 퀘스트 완료 시 빈 메시지 표시
        if (_activeQuestItems.Count == 0)
        {
            ShowEmptyMessage(true);
        }
    }

    #endregion

    #region 퀘스트 목록 관리

    /// <summary>
    /// 전체 퀘스트 목록 새로고침
    /// 
    /// [수정] 초기화되지 않았으면 실행하지 않음
    /// </summary>
    public void RefreshQuestList()
    {
        Log("퀘스트 목록 새로고침");

        // 기존 UI 제거
        ClearAllQuestItems();

        // QuestManager 확인
        if (!Application.isPlaying || QuestManager.Instance == null)
        {
            if (Application.isPlaying)
                LogWarning("QuestManager를 찾을 수 없습니다!");
            ShowEmptyMessage(true);
            return;
        }

        // [수정] QuestManager 초기화 확인
        if (!QuestManager.Instance.IsInitialized)
        {
            Log("QuestManager가 아직 초기화되지 않았습니다. 대기 중...");
            ShowEmptyMessage(true);
            return;
        }

        // 활성 퀘스트 목록 가져오기
        List<QuestData> activeQuests = QuestManager.Instance.GetActiveQuests();

        if (activeQuests == null || activeQuests.Count == 0)
        {
            ShowEmptyMessage(true);
            return;
        }

        // 각 퀘스트별 UI 생성
        foreach (QuestData questData in activeQuests)
        {
            if (questData == null)
                continue;

            QuestProgress progress = QuestManager.Instance.GetQuestProgress(questData.QuestID);
            if (progress != null)
            {
                OnQuestStarted(questData, progress);
            }
        }

        ShowEmptyMessage(_activeQuestItems.Count == 0);
    }

    /// <summary>
    /// 모든 퀘스트 진행 상황 업데이트
    /// </summary>
    private void UpdateAllQuestProgress()
    {
        // QuestManager가 없으면 업데이트 불가
        if (!Application.isPlaying || QuestManager.Instance == null)
            return;

        foreach (var kvp in _activeQuestItems)
        {
            if (kvp.Value != null)
            {
                kvp.Value.UpdateProgress();
            }
        }
    }

    /// <summary>
    /// 모든 퀘스트 UI 제거
    /// </summary>
    private void ClearAllQuestItems()
    {
        foreach (var kvp in _activeQuestItems)
        {
            if (kvp.Value != null)
            {
                ReturnItemToPool(kvp.Value);
            }
        }
        _activeQuestItems.Clear();
    }

    #endregion

    #region UI 아이템 풀링

    /// <summary>
    /// UI 아이템 가져오기 또는 생성
    /// </summary>
    private QuestUIItem GetOrCreateQuestItem()
    {
        // 컨테이너 확인
        if (_questItemContainer == null)
        {
            LogError("Quest Item Container가 할당되지 않았습니다!");
            return null;
        }

        // 풀에서 사용 가능한 아이템 찾기
        foreach (QuestUIItem item in _itemPool)
        {
            if (item != null && !item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                return item;
            }
        }

        // 풀에 없으면 새로 생성
        if (_questItemPrefab == null)
        {
            LogError("QuestItemPrefab이 할당되지 않았습니다!");
            return null;
        }

        QuestUIItem newItem = Instantiate(_questItemPrefab, _questItemContainer);
        if (newItem != null)
        {
            _itemPool.Add(newItem);
            Log($"새 QuestUIItem 생성 (풀 크기: {_itemPool.Count})");
        }

        return newItem;
    }

    /// <summary>
    /// UI 아이템을 풀로 반환
    /// </summary>
    private void ReturnItemToPool(QuestUIItem item)
    {
        if (item == null)
            return;

        item.ResetItem();
        item.gameObject.SetActive(false);
    }

    #endregion

    #region UI 헬퍼

    /// <summary>
    /// 빈 메시지 표시/숨김
    /// </summary>
    private void ShowEmptyMessage(bool show)
    {
        if (_emptyMessageObject != null)
        {
            _emptyMessageObject.SetActive(show);
        }
    }

    #endregion

    #region 로깅

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[QuestUIWindow] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[QuestUIWindow] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[QuestUIWindow] {message}");
    }

    #endregion

    #region 에디터 헬퍼

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 컴포넌트 검증
    /// </summary>
    private void OnValidate()
    {
        // Prefab 모드에서는 검증하지 않음
        if (!Application.isPlaying)
            return;

        if (_questItemContainer == null)
        {
            Debug.LogWarning("[QuestUIWindow] Quest Item Container가 할당되지 않았습니다!");
        }

        if (_questItemPrefab == null)
        {
            Debug.LogWarning("[QuestUIWindow] Quest Item Prefab이 할당되지 않았습니다!");
        }
    }
#endif

    #endregion
}
