using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퀘스트 데이터
/// 
/// 기능:
/// - 퀘스트 목표 정의 (씬별/적 타입별 처치 수)
/// - 완료 조건 검증
/// - 보상 설정
/// 
/// 사용법:
/// 1. Assets > Create > ScriptableObject > Quest Data
/// 2. 퀘스트 목표 설정 (씬, 적 타입, 처치 수)
/// 3. QuestManager에서 활성 퀘스트로 등록
/// 
/// 예시:
/// - "튜토리얼 완료": TutorialScene에서 Normal 5마리 처치
/// - "보스 토벌": BossScene에서 Boss 1마리 처치
/// </summary>
[CreateAssetMenu(fileName = "New Quest", menuName = "ScriptableObject/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("퀘스트 정보")]
    [Tooltip("퀘스트 고유 ID (저장용)")]
    [SerializeField] private string _questID;

    [Tooltip("퀘스트 이름")]
    [SerializeField] private string _questName;

    [Tooltip("퀘스트 설명")]
    [TextArea(3, 5)]
    [SerializeField] private string _questDescription;

    [Header("완료 조건")]
    [Tooltip("이 퀘스트의 모든 목표 (씬별/적 타입별)")]
    [SerializeField] private List<QuestObjective> _objectives = new List<QuestObjective>();

    [Header("보상")]
    [Tooltip("완료 시 경험치 보상")]
    [SerializeField] private int _experienceReward = 0;

    [Tooltip("완료 시 골드 보상")]
    [SerializeField] private int _goldReward = 0;

    [Header("선행 퀘스트")]
    [Tooltip("이 퀘스트를 시작하기 위해 완료해야 하는 퀘스트 ID 목록")]
    [SerializeField] private List<string> _prerequisiteQuestIDs = new List<string>();

    // Properties
    public string QuestID => _questID;
    public string QuestName => _questName;
    public string QuestDescription => _questDescription;
    public List<QuestObjective> Objectives => _objectives;
    public int ExperienceReward => _experienceReward;
    public int GoldReward => _goldReward;
    public List<string> PrerequisiteQuestIDs => _prerequisiteQuestIDs;

    /// <summary>
    /// 퀘스트 목표가 완료되었는지 확인
    /// </summary>
    public bool IsObjectiveComplete(QuestProgress progress)
    {
        if (progress == null) return false;

        // 모든 목표를 달성했는지 확인
        foreach (var objective in _objectives)
        {
            string key = GetObjectiveKey(objective);

            if (!progress.ObjectiveProgress.ContainsKey(key))
                return false;

            if (progress.ObjectiveProgress[key] < objective.RequiredCount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 목표의 고유 키 생성 (씬_적타입)
    /// </summary>
    public static string GetObjectiveKey(QuestObjective objective)
    {
        return $"{objective.SceneName}_{objective.EnemyType}";
    }

    /// <summary>
    /// 유효성 검사
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(_questID))
        {
            DebugHelper.LogError($"[QuestData] QuestID가 비어있습니다: {name}");
            return false;
        }

        if (_objectives.Count == 0)
        {
            DebugHelper.LogError($"[QuestData] 목표가 없습니다: {name}");
            return false;
        }

        foreach (var obj in _objectives)
        {
            if (string.IsNullOrEmpty(obj.SceneName))
            {
                DebugHelper.LogError($"[QuestData] 목표의 씬 이름이 비어있습니다: {name}");
                return false;
            }

            if (obj.RequiredCount <= 0)
            {
                DebugHelper.LogError($"[QuestData] 목표의 필요 처치 수가 0 이하입니다: {name}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"Quest: {_questName} (ID: {_questID})\n";
        info += $"Objectives:\n";

        foreach (var obj in _objectives)
        {
            info += $"  - {obj.SceneName}: {obj.EnemyType} x{obj.RequiredCount}\n";
        }

        return info;
    }
}

/// <summary>
/// 퀘스트 목표 정의
/// 
/// 특정 씬에서 특정 적 타입을 몇 마리 처치해야 하는지 정의
/// </summary>
[Serializable]
public class QuestObjective
{
    [Tooltip("목표를 달성해야 하는 씬 이름 (예: TutorialScene, HubScene)")]
    public string SceneName;

    [Tooltip("처치해야 하는 적 타입")]
    public EnemyType EnemyType;

    [Tooltip("처치해야 하는 수")]
    public int RequiredCount;

    public QuestObjective(string sceneName, EnemyType enemyType, int requiredCount)
    {
        SceneName = sceneName;
        EnemyType = enemyType;
        RequiredCount = requiredCount;
    }
}

/// <summary>
/// 퀘스트 진행 상황 (저장용)
/// 
/// CharacterSaveData에 포함되어 저장됨
/// </summary>
[Serializable]
public class QuestProgress
{
    [Tooltip("퀘스트 ID")]
    public string QuestID;

    [Tooltip("퀘스트 시작 여부")]
    public bool IsStarted;

    [Tooltip("퀘스트 완료 여부")]
    public bool IsCompleted;

    [Tooltip("목표별 진행 상황 (Key: 씬_적타입, Value: 현재 처치 수)")]
    public Dictionary<string, int> ObjectiveProgress = new Dictionary<string, int>();

    public QuestProgress(string questID)
    {
        QuestID = questID;
        IsStarted = false;
        IsCompleted = false;
    }

    /// <summary>
    /// 목표 진행 상황 증가
    /// </summary>
    public void IncrementObjective(string objectiveKey)
    {
        if (!ObjectiveProgress.ContainsKey(objectiveKey))
        {
            ObjectiveProgress[objectiveKey] = 0;
        }

        ObjectiveProgress[objectiveKey]++;
    }

    /// <summary>
    /// 특정 목표의 현재 진행 상황 가져오기
    /// </summary>
    public int GetObjectiveProgress(string objectiveKey)
    {
        return ObjectiveProgress.ContainsKey(objectiveKey) ? ObjectiveProgress[objectiveKey] : 0;
    }
}
