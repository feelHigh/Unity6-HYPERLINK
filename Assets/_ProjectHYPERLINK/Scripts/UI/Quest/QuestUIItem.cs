using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 퀘스트 UI 아이템 (개별 퀘스트 표시)
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
/// UI 구조:
/// - Panel (배경)
///   - QuestNameText (퀘스트 이름)
///   - QuestDescriptionText (퀘스트 설명)
///   - ObjectivesContainer (목표 컨테이너)
///     - ObjectiveText (각 목표별 텍스트)
///   - CompletionIndicator (완료 표시)
/// </summary>
public class QuestUIItem : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI _questNameText;
    [SerializeField] private TextMeshProUGUI _questDescriptionText;
    [SerializeField] private Transform _objectivesContainer;
    [SerializeField] private TextMeshProUGUI _objectiveTextPrefab;
    [SerializeField] private GameObject _completionIndicator;

    [Header("색상 설정")]
    [SerializeField] private Color _activeQuestColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private Color _completedObjectiveColor = new Color(0f, 1f, 0f, 0.8f);
    [SerializeField] private Color _incompleteObjectiveColor = new Color(1f, 1f, 1f, 0.8f);

    private QuestData _questData;
    private QuestProgress _questProgress;
    private System.Collections.Generic.List<TextMeshProUGUI> _objectiveTexts = new System.Collections.Generic.List<TextMeshProUGUI>();

    /// <summary>
    /// 퀘스트 UI 초기화
    /// 
    /// 호출: QuestUIWindow.AddQuestItem()
    /// 
    /// 처리:
    /// 1. QuestData와 QuestProgress 저장
    /// 2. 퀘스트 이름/설명 표시
    /// 3. 목표별 텍스트 생성
    /// 4. 초기 진행 상황 표시
    /// </summary>
    public void Initialize(QuestData questData, QuestProgress questProgress)
    {
        _questData = questData;
        _questProgress = questProgress;

        // 기본 정보 설정
        if (_questNameText != null)
        {
            _questNameText.text = questData.QuestName;
        }

        if (_questDescriptionText != null)
        {
            _questDescriptionText.text = questData.QuestDescription;
        }

        // 완료 표시 숨김
        if (_completionIndicator != null)
        {
            _completionIndicator.SetActive(false);
        }

        // 기존 목표 텍스트 제거
        ClearObjectives();

        // 목표별 텍스트 생성
        CreateObjectiveTexts();

        // 진행 상황 업데이트
        UpdateProgress();
    }

    /// <summary>
    /// 목표 텍스트 생성
    /// 
    /// 각 QuestObjective마다 하나의 TextMeshProUGUI 생성
    /// </summary>
    private void CreateObjectiveTexts()
    {
        if (_objectivesContainer == null || _objectiveTextPrefab == null)
        {
            Debug.LogWarning("[QuestUIItem] ObjectivesContainer 또는 ObjectiveTextPrefab이 없습니다!");
            return;
        }

        foreach (var objective in _questData.Objectives)
        {
            TextMeshProUGUI objectiveText = Instantiate(_objectiveTextPrefab, _objectivesContainer);
            objectiveText.gameObject.SetActive(true);
            _objectiveTexts.Add(objectiveText);
        }
    }

    /// <summary>
    /// 진행 상황 업데이트
    /// 
    /// 호출:
    /// - Initialize() (초기화 시)
    /// - QuestUIWindow.OnQuestProgressUpdated() (진행 시)
    /// 
    /// 처리:
    /// 1. 각 목표의 현재 진행 상황 가져오기
    /// 2. "씬이름: 적타입 현재/필요" 형식으로 표시
    /// 3. 완료된 목표는 녹색, 미완료는 흰색
    /// 4. 모든 목표 완료 시 완료 표시
    /// 
    /// 예시:
    /// "TutorialScene: Normal 5/10" (미완료 - 흰색)
    /// "TutorialScene: Elite 3/3" (완료 - 녹색)
    /// </summary>
    public void UpdateProgress()
    {
        if (_questData == null || _questProgress == null)
            return;

        // 각 목표별 진행 상황 표시
        for (int i = 0; i < _questData.Objectives.Count; i++)
        {
            if (i >= _objectiveTexts.Count)
                break;

            QuestObjective objective = _questData.Objectives[i];
            string key = QuestData.GetObjectiveKey(objective);
            int currentProgress = _questProgress.GetObjectiveProgress(key);
            int requiredCount = objective.RequiredCount;

            // 텍스트 형식: "씬이름: 적타입 현재/필요"
            string objectiveText = $"{objective.SceneName}: {objective.EnemyType} {currentProgress}/{requiredCount}";
            _objectiveTexts[i].text = objectiveText;

            // 완료 여부에 따라 색상 변경
            bool isCompleted = currentProgress >= requiredCount;
            _objectiveTexts[i].color = isCompleted ? _completedObjectiveColor : _incompleteObjectiveColor;
        }

        // 퀘스트 전체 완료 체크
        if (_questData.IsObjectiveComplete(_questProgress))
        {
            if (_completionIndicator != null)
            {
                _completionIndicator.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 기존 목표 텍스트 제거
    /// 
    /// 재사용 시 호출 (풀링)
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
    }

    private void OnDestroy()
    {
        ClearObjectives();
    }
}
