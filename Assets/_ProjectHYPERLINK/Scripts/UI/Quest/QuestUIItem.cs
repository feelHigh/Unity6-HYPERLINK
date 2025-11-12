using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Settings;

/// <summary>
/// 퀘스트 UI 아이템 (개별 퀘스트 표시) - 안전성 강화 버전
/// 
/// 역할:
/// - 퀘스트 이름, 설명, 진행 상황 표시
/// - 완료/진행중 상태 시각화
/// - 목표별 진행 상황 표시 (예: "일반 몬스터 5/10", "보스 0/1")
/// 
/// 생명주기:
/// - QuestUIWindow에서 풀링으로 관리
/// - Initialize()로 데이터 설정
/// - UpdateProgress()로 진행 상황 갱신
/// - 비활성화 시 풀로 반환
/// 
/// 안전성 개선:
/// - null 체크 강화
/// - Application.isPlaying 체크
/// - 방어적 프로그래밍
/// </summary>
public class QuestUIItem : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI _questNameText;
    [SerializeField] private TextMeshProUGUI _questDescriptionText;
    [SerializeField] private Transform _objectivesContainer;
    [SerializeField] private TextMeshProUGUI _objectiveTextPrefab;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private GameObject _completionIndicator;

    [Header("색상 설정")]
    [SerializeField] private Color _activeQuestColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private Color _completedObjectiveColor = new Color(0f, 1f, 0f, 0.8f);
    [SerializeField] private Color _incompleteObjectiveColor = new Color(1f, 1f, 1f, 0.8f);

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = false;

    private QuestData _questData;
    private QuestProgress _questProgress;
    private System.Collections.Generic.List<TextMeshProUGUI> _objectiveTexts = new System.Collections.Generic.List<TextMeshProUGUI>();

    #region 초기화

    /// <summary>
    /// 퀘스트 UI 초기화
    /// </summary>
    public void Initialize(QuestData questData, QuestProgress questProgress)
    {
        // null 체크
        if (questData == null)
        {
            LogError("Initialize: questData가 null입니다!");
            return;
        }

        if (questProgress == null)
        {
            LogError("Initialize: questProgress가 null입니다!");
            return;
        }

        _questData = questData;
        _questProgress = questProgress;

        // 기본 정보 설정
        SetQuestName(questData.QuestID);
        SetQuestDescription(questData.QuestDescription);
        SetBackgroundColor(_activeQuestColor);
        HideCompletionIndicator();

        // 기존 목표 텍스트 제거
        ClearObjectives();

        // 목표별 텍스트 생성
        CreateObjectiveTexts();

        // 진행 상황 업데이트
        UpdateProgress();

        Log($"퀘스트 UI 초기화: {questData.QuestName}");
    }

    /// <summary>
    /// 퀘스트 이름 설정
    /// </summary>
    private void SetQuestName(string questID)
    {
        if (_questNameText != null)
        {
            string nameText = LocalizationSettings.StringDatabase.GetLocalizedString(
                "DATA_QUEST", "");
            _questNameText.text = questID ?? "이름 없음";
        }
        else
        {
            LogWarning("QuestNameText가 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// 퀘스트 설명 설정
    /// </summary>
    private void SetQuestDescription(string description)
    {
        if (_questDescriptionText != null)
        {
            _questDescriptionText.text = description ?? "";
        }
    }

    /// <summary>
    /// 배경 색상 설정
    /// </summary>
    private void SetBackgroundColor(Color color)
    {
        if (_backgroundImage != null)
        {
            _backgroundImage.color = color;
        }
    }

    /// <summary>
    /// 완료 표시 숨김
    /// </summary>
    private void HideCompletionIndicator()
    {
        if (_completionIndicator != null)
        {
            _completionIndicator.SetActive(false);
        }
    }

    #endregion

    #region 목표 텍스트 관리

    /// <summary>
    /// 목표 텍스트 생성
    /// </summary>
    private void CreateObjectiveTexts()
    {
        if (_questData == null)
        {
            LogError("CreateObjectiveTexts: _questData가 null입니다!");
            return;
        }

        if (_objectivesContainer == null)
        {
            LogError("ObjectivesContainer가 할당되지 않았습니다!");
            return;
        }

        if (_objectiveTextPrefab == null)
        {
            LogError("ObjectiveTextPrefab이 할당되지 않았습니다!");
            return;
        }

        // 목표가 없으면 생성하지 않음
        if (_questData.Objectives == null || _questData.Objectives.Count == 0)
        {
            LogWarning($"퀘스트 {_questData.QuestName}에 목표가 없습니다!");
            return;
        }

        foreach (var objective in _questData.Objectives)
        {
            if (objective == null)
            {
                LogWarning("null 목표를 건너뜁니다.");
                continue;
            }

            try
            {
                TextMeshProUGUI objectiveText = Instantiate(_objectiveTextPrefab, _objectivesContainer);
                if (objectiveText != null)
                {
                    objectiveText.gameObject.SetActive(true);
                    _objectiveTexts.Add(objectiveText);
                }
            }
            catch (System.Exception e)
            {
                LogError($"목표 텍스트 생성 실패: {e.Message}");
            }
        }

        Log($"{_objectiveTexts.Count}개의 목표 텍스트 생성 완료");
    }

    /// <summary>
    /// 기존 목표 텍스트 제거
    /// </summary>
    private void ClearObjectives()
    {
        foreach (var text in _objectiveTexts)
        {
            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }
        _objectiveTexts.Clear();
    }

    #endregion

    #region 진행 상황 업데이트

    /// <summary>
    /// 진행 상황 업데이트
    /// </summary>
    public void UpdateProgress()
    {
        // 에디터 모드에서는 실행하지 않음
        if (!Application.isPlaying)
            return;

        // 데이터 유효성 체크
        if (_questData == null || _questProgress == null)
        {
            LogWarning("UpdateProgress: questData 또는 questProgress가 null입니다.");
            return;
        }

        if (_questData.Objectives == null)
        {
            LogWarning("UpdateProgress: Objectives가 null입니다.");
            return;
        }

        // 각 목표별 진행 상황 표시
        for (int i = 0; i < _questData.Objectives.Count; i++)
        {
            if (i >= _objectiveTexts.Count)
            {
                LogWarning($"목표 인덱스 {i}가 텍스트 목록 범위를 벗어났습니다.");
                break;
            }

            QuestObjective objective = _questData.Objectives[i];
            if (objective == null)
            {
                LogWarning($"목표 {i}가 null입니다.");
                continue;
            }

            TextMeshProUGUI objectiveText = _objectiveTexts[i];
            if (objectiveText == null)
            {
                LogWarning($"목표 텍스트 {i}가 null입니다.");
                continue;
            }

            UpdateObjectiveText(objective, objectiveText);
        }

        // 퀘스트 전체 완료 체크
        CheckQuestCompletion();
    }

    /// <summary>
    /// 개별 목표 텍스트 업데이트
    /// </summary>
    private void UpdateObjectiveText(QuestObjective objective, TextMeshProUGUI objectiveText)
    {
        string key = QuestData.GetObjectiveKey(objective);
        int currentProgress = _questProgress.GetObjectiveProgress(key);
        int requiredCount = objective.RequiredCount;

        // 텍스트 형식: "씬이름: 적타입 현재/필요"
        string text = $"• {objective.SceneName}: {objective.EnemyType} {currentProgress}/{requiredCount}";
        objectiveText.text = text;

        // 완료 여부에 따라 색상 변경
        bool isCompleted = currentProgress >= requiredCount;
        objectiveText.color = isCompleted ? _completedObjectiveColor : _incompleteObjectiveColor;
    }

    /// <summary>
    /// 퀘스트 완료 체크
    /// </summary>
    private void CheckQuestCompletion()
    {
        if (_questData.IsObjectiveComplete(_questProgress))
        {
            if (_completionIndicator != null)
            {
                _completionIndicator.SetActive(true);
            }
            Log($"퀘스트 완료: {_questData.QuestName}");
        }
    }

    #endregion

    #region 재사용 및 정리

    /// <summary>
    /// UI 아이템 재설정 (풀 반환 시)
    /// </summary>
    public void ResetItem()
    {
        _questData = null;
        _questProgress = null;
        ClearObjectives();

        if (_completionIndicator != null)
        {
            _completionIndicator.SetActive(false);
        }

        Log("QuestUIItem 리셋 완료");
    }

    private void OnDestroy()
    {
        ClearObjectives();
    }

    #endregion

    #region 로깅

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[QuestUIItem] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[QuestUIItem] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[QuestUIItem] {message}");
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

        if (_questNameText == null)
            Debug.LogWarning("[QuestUIItem] Quest Name Text가 할당되지 않았습니다!");

        if (_objectivesContainer == null)
            Debug.LogWarning("[QuestUIItem] Objectives Container가 할당되지 않았습니다!");

        if (_objectiveTextPrefab == null)
            Debug.LogWarning("[QuestUIItem] Objective Text Prefab이 할당되지 않았습니다!");
    }
#endif

    #endregion
}
