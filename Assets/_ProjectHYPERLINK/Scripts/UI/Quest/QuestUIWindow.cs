using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 퀘스트 UI 윈도우
/// 
/// 역할:
/// - 활성 퀘스트 목록 표시
/// - QuestManager 이벤트 구독
/// - 퀘스트 진행 상황 실시간 업데이트
/// - QuestUIItem 풀링 관리
/// 
/// 통합:
/// - GameCanvas의 자식으로 배치
/// - CharacterUIController에서 Q 키로 토글
/// - CanvasGroup으로 가시성 제어
/// 
/// 작동 흐름:
/// 1. QuestManager의 이벤트 구독
/// 2. 활성 퀘스트 목록 가져오기
/// 3. 각 퀘스트별 QuestUIItem 생성
/// 4. 진행 상황 업데이트 시 UI 갱신
/// 5. 완료 시 목록에서 제거
/// 
/// UI 계층 구조:
/// - QuestUIWindow (Panel)
///   - Header (타이틀: "활성 퀘스트")
///   - ScrollView
///     - Viewport
///       - Content (QuestUIItem들이 추가되는 곳)
///   - EmptyMessage ("진행 중인 퀘스트가 없습니다")
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

    #region Unity Lifecycle

    private void Awake()
    {
        // 빈 메시지 설정
        if (_emptyMessageText != null)
        {
            _emptyMessageText.text = "진행 중인 퀘스트가 없습니다";
        }

        // 초기 상태: 빈 메시지 표시
        ShowEmptyMessage(true);
    }

    private void OnEnable()
    {
        Debug.LogWarning("Enable");
        SubscribeToQuestEvents();
        RefreshQuestList();
    }

    private void OnDisable()
    {
        UnsubscribeFromQuestEvents();
    }

    private void Update()
    {
        // 주기적으로 진행 상황 업데이트 (성능 최적화)
        if (Time.time - _lastUpdateTime >= _updateInterval)
        {
            UpdateAllQuestProgress();
            _lastUpdateTime = Time.time;
        }
    }

    #endregion

    #region 이벤트 구독

    /// <summary>
    /// QuestManager 이벤트 구독
    /// </summary>
    private void SubscribeToQuestEvents()
    {
        if (QuestManager.Instance == null)
        {
            LogWarning("QuestManager를 찾을 수 없습니다!");
            return;
        }
        Debug.LogWarning("구독");
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
        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.OnQuestStarted -= OnQuestStarted;
        QuestManager.Instance.OnQuestProgressUpdated -= OnQuestProgressUpdated;
        QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
    }

    #endregion

    #region 퀘스트 이벤트 핸들러

    /// <summary>
    /// 퀘스트 시작 이벤트
    /// 
    /// 호출: QuestManager.StartQuest()
    /// 
    /// 처리:
    /// 1. UI 아이템 풀에서 가져오기 또는 생성
    /// 2. 퀘스트 데이터로 초기화
    /// 3. 활성 목록에 추가
    /// 4. 빈 메시지 숨김
    /// </summary>
    private void OnQuestStarted(QuestData questData, QuestProgress questProgress)
    {
        Log($"퀘스트 시작: {questData.QuestName}");

        // 이미 UI에 있는지 확인
        if (_activeQuestItems.ContainsKey(questData.QuestID))
        {
            LogWarning($"이미 표시 중인 퀘스트: {questData.QuestID}");
            return;
        }

        
        // UI 아이템 생성 또는 풀에서 가져오기
        QuestUIItem questItem = GetOrCreateQuestItem();
        questItem.Initialize(questData, questProgress);

        // 활성 목록에 추가
        _activeQuestItems[questData.QuestID] = questItem;

        // 빈 메시지 숨김
        ShowEmptyMessage(false);
    }

    /// <summary>
    /// 퀘스트 진행 업데이트 이벤트
    /// 
    /// 호출: QuestManager.OnEnemyKilled()
    /// 
    /// 처리:
    /// 1. 해당 퀘스트 UI 아이템 찾기
    /// 2. UpdateProgress() 호출
    /// </summary>
    private void OnQuestProgressUpdated(QuestData questData, QuestProgress questProgress)
    {
        if (_activeQuestItems.TryGetValue(questData.QuestID, out QuestUIItem questItem))
        {
            questItem.UpdateProgress();
            Log($"퀘스트 진행: {questData.QuestName}");
        }
    }

    /// <summary>
    /// 퀘스트 완료 이벤트
    /// 
    /// 호출: QuestManager.CompleteQuest()
    /// 
    /// 처리:
    /// 1. UI 아이템 제거
    /// 2. 풀로 반환
    /// 3. 활성 목록에서 삭제
    /// 4. 모든 퀘스트 완료 시 빈 메시지 표시
    /// </summary>
    private void OnQuestCompleted(QuestData questData)
    {
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
    /// 호출:
    /// - OnEnable() (UI 활성화 시)
    /// - 수동 호출 (F5 등)
    /// 
    /// 처리:
    /// 1. 기존 UI 전부 제거
    /// 2. QuestManager에서 활성 퀘스트 목록 가져오기
    /// 3. 각 퀘스트별 UI 생성
    /// </summary>
    public void RefreshQuestList()
    {
        Log("퀘스트 목록 새로고침");

        // 기존 UI 제거
        ClearAllQuestItems();

        // QuestManager 확인
        if (QuestManager.Instance == null)
        {
            LogWarning("QuestManager를 찾을 수 없습니다!");
            ShowEmptyMessage(true);
            return;
        }

        // 활성 퀘스트 목록 가져오기
        List<QuestData> activeQuests = QuestManager.Instance.GetActiveQuests();

        if (activeQuests.Count == 0)
        {
            ShowEmptyMessage(true);
            return;
        }

        // 각 퀘스트별 UI 생성
        foreach (QuestData questData in activeQuests)
        {
            QuestProgress progress = QuestManager.Instance.GetQuestProgress(questData.QuestID);
            if (progress != null)
            {
                OnQuestStarted(questData, progress);
            }
        }

        ShowEmptyMessage(false);
    }

    /// <summary>
    /// 모든 퀘스트 진행 상황 업데이트
    /// 
    /// 호출: Update() (주기적)
    /// 
    /// 목적: 
    /// - 실시간 업데이트 보장
    /// - 이벤트 누락 방지
    /// </summary>
    private void UpdateAllQuestProgress()
    {
        foreach (var kvp in _activeQuestItems)
        {
            kvp.Value.UpdateProgress();
        }
    }

    /// <summary>
    /// 모든 퀘스트 UI 제거
    /// </summary>
    private void ClearAllQuestItems()
    {
        foreach (var kvp in _activeQuestItems)
        {
            ReturnItemToPool(kvp.Value);
        }
        _activeQuestItems.Clear();
    }

    #endregion

    #region UI 아이템 풀링

    /// <summary>
    /// UI 아이템 가져오기 또는 생성
    /// 
    /// 풀링 패턴:
    /// - 재사용 가능한 아이템이 있으면 풀에서 가져오기
    /// - 없으면 새로 생성
    /// 
    /// 목적: 성능 최적화 (Instantiate/Destroy 비용 감소)
    /// </summary>
    private QuestUIItem GetOrCreateQuestItem()
    {
        // 풀에서 사용 가능한 아이템 찾기
        foreach (QuestUIItem item in _itemPool)
        {
            if (!item.gameObject.activeSelf)
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
        _itemPool.Add(newItem);

        Log($"새 QuestUIItem 생성 (풀 크기: {_itemPool.Count})");

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
}
