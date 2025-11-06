using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 퀘스트 시스템 관리자
/// 
/// 역할:
/// - 활성 퀘스트 관리
/// - 적 처치 이벤트 구독
/// - 퀘스트 진행 상황 업데이트
/// - 완료 체크 및 보상 지급
/// 
/// 사용 흐름:
/// 1. GameInitializer에서 초기화
/// 2. EnemyController.Die()에서 OnEnemyKilled 호출
/// 3. Portal.CanInteract()에서 퀘스트 완료 체크
/// </summary>
public class QuestManager : MonoBehaviour
{
    private static QuestManager _instance;
    public static QuestManager Instance => _instance;

    [Header("게임 내 모든 퀘스트")]
    [Tooltip("게임에서 사용할 모든 QuestData 목록")]
    [SerializeField] private List<QuestData> _allQuests = new List<QuestData>();

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    // 내부 상태
    private Dictionary<string, QuestProgress> _activeQuests = new Dictionary<string, QuestProgress>();
    private List<string> _completedQuestIDs = new List<string>();
    private string _currentSceneName;

    // 이벤트
    public event Action<QuestData, QuestProgress> OnQuestStarted;
    public event Action<QuestData, QuestProgress> OnQuestProgressUpdated;
    public event Action<QuestData> OnQuestCompleted;

    #region Unity Lifecycle

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _currentSceneName = SceneManager.GetActiveScene().name;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 저장된 데이터로 퀘스트 시스템 초기화
    /// GameInitializer에서 호출
    /// </summary>
    public void Initialize(CharacterSaveData saveData)
    {
        if (saveData == null)
        {
            LogWarning("SaveData가 null입니다. 퀘스트 초기화 실패");
            return;
        }

        // 저장된 완료 퀘스트 로드
        _completedQuestIDs.Clear();
        if (saveData.gameplay.questsCompleted != null)
        {
            _completedQuestIDs.AddRange(saveData.gameplay.questsCompleted);
        }

        // 저장된 진행 중 퀘스트 로드
        _activeQuests.Clear();
        if (saveData.gameplay.questProgress != null)
        {
            foreach (var progress in saveData.gameplay.questProgress)
            {
                if (!progress.IsCompleted)
                {
                    _activeQuests[progress.QuestID] = progress;
                }
            }
        }

        Log($"퀘스트 시스템 초기화 완료: 완료 {_completedQuestIDs.Count}개, 진행중 {_activeQuests.Count}개");
    }

    #endregion

    #region 퀘스트 시작/완료

    /// <summary>
    /// 퀘스트 시작
    /// </summary>
    public void StartQuest(string questID)
    {
        QuestData questData = GetQuestData(questID);
        if (questData == null)
        {
            LogError($"퀘스트를 찾을 수 없습니다: {questID}");
            return;
        }

        if (_activeQuests.ContainsKey(questID))
        {
            LogWarning($"이미 진행 중인 퀘스트입니다: {questID}");
            return;
        }

        if (_completedQuestIDs.Contains(questID))
        {
            LogWarning($"이미 완료한 퀘스트입니다: {questID}");
            return;
        }

        // 선행 퀘스트 체크
        if (!CanStartQuest(questData))
        {
            LogWarning($"선행 퀘스트를 완료하지 않았습니다: {questID}");
            return;
        }

        // 새 진행 상황 생성
        QuestProgress progress = new QuestProgress(questID);
        progress.IsStarted = true;

        // 모든 목표 초기화
        foreach (var objective in questData.Objectives)
        {
            string key = QuestData.GetObjectiveKey(objective);
            progress.ObjectiveProgress[key] = 0;
        }

        _activeQuests[questID] = progress;

        Log($"퀘스트 시작: {questData.QuestName}");
        OnQuestStarted?.Invoke(questData, progress);

        SaveQuestProgress();
    }

    /// <summary>
    /// 퀘스트 시작 가능 여부 체크 (선행 퀘스트)
    /// </summary>
    private bool CanStartQuest(QuestData questData)
    {
        foreach (var prerequisiteID in questData.PrerequisiteQuestIDs)
        {
            if (!_completedQuestIDs.Contains(prerequisiteID))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 퀘스트 완료 처리
    /// </summary>
    private void CompleteQuest(string questID)
    {
        QuestData questData = GetQuestData(questID);
        if (questData == null) return;

        QuestProgress progress = _activeQuests[questID];
        progress.IsCompleted = true;

        // 완료 목록에 추가
        _completedQuestIDs.Add(questID);

        // 활성 목록에서 제거
        _activeQuests.Remove(questID);

        // 보상 지급
        GiveQuestRewards(questData);

        Log($"퀘스트 완료: {questData.QuestName}");
        OnQuestCompleted?.Invoke(questData);

        SaveQuestProgress();
    }

    /// <summary>
    /// 보상 지급
    /// [NEW] 골드 보상 활성화
    /// </summary>
    private void GiveQuestRewards(QuestData questData)
    {
        // 경험치 보상
        if (questData.ExperienceReward > 0)
        {
            ExperienceManager expManager = FindFirstObjectByType<ExperienceManager>();
            if (expManager != null)
            {
                expManager.GainExperience(questData.ExperienceReward);
                Log($"경험치 보상: {questData.ExperienceReward}");
            }
        }

        // [NEW] 골드 보상
        if (questData.GoldReward > 0)
        {
            PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
            if (player != null)
            {
                player.AddGold(questData.GoldReward);
                Log($"골드 보상: {questData.GoldReward}");
            }
            else
            {
                LogWarning("PlayerCharacter를 찾을 수 없어 골드 보상을 지급할 수 없습니다");
            }
        }
    }

    #endregion

    #region 적 처치 추적

    /// <summary>
    /// 적 처치 이벤트 처리
    /// EnemyController.Die()에서 호출
    /// </summary>
    public void OnEnemyKilled(EnemyType enemyType, string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = _currentSceneName;
        }

        Log($"적 처치: {enemyType} in {sceneName}");

        // 모든 활성 퀘스트를 순회하며 진행 상황 업데이트
        List<string> questsToComplete = new List<string>();

        foreach (var kvp in _activeQuests)
        {
            string questID = kvp.Key;
            QuestProgress progress = kvp.Value;
            QuestData questData = GetQuestData(questID);

            if (questData == null) continue;

            // 이 퀘스트의 목표 중 현재 씬/적 타입과 일치하는 것이 있는지 확인
            bool progressMade = false;

            foreach (var objective in questData.Objectives)
            {
                if (objective.SceneName == sceneName && objective.EnemyType == enemyType)
                {
                    string key = QuestData.GetObjectiveKey(objective);
                    progress.IncrementObjective(key);

                    int current = progress.GetObjectiveProgress(key);
                    Log($"  퀘스트 진행: {questData.QuestName} - {objective.EnemyType} {current}/{objective.RequiredCount}");

                    progressMade = true;
                }
            }

            if (progressMade)
            {
                OnQuestProgressUpdated?.Invoke(questData, progress);

                // 퀘스트 완료 체크
                if (questData.IsObjectiveComplete(progress))
                {
                    questsToComplete.Add(questID);
                }
            }
        }

        // 완료된 퀘스트 처리
        foreach (var questID in questsToComplete)
        {
            CompleteQuest(questID);
        }

        if (questsToComplete.Count > 0 || _activeQuests.Count > 0)
        {
            SaveQuestProgress();
        }
    }

    #endregion

    #region 퀘스트 상태 조회

    /// <summary>
    /// 특정 퀘스트가 완료되었는지 확인
    /// Portal.CanInteract()에서 호출
    /// </summary>
    public bool IsQuestCompleted(string questID)
    {
        return _completedQuestIDs.Contains(questID);
    }

    /// <summary>
    /// 특정 퀘스트가 진행 중인지 확인
    /// </summary>
    public bool IsQuestActive(string questID)
    {
        return _activeQuests.ContainsKey(questID);
    }

    /// <summary>
    /// 퀘스트 진행 상황 가져오기
    /// </summary>
    public QuestProgress GetQuestProgress(string questID)
    {
        return _activeQuests.ContainsKey(questID) ? _activeQuests[questID] : null;
    }

    /// <summary>
    /// QuestData 가져오기
    /// </summary>
    private QuestData GetQuestData(string questID)
    {
        return _allQuests.FirstOrDefault(q => q.QuestID == questID);
    }

    /// <summary>
    /// 모든 활성 퀘스트 가져오기
    /// </summary>
    public List<QuestData> GetActiveQuests()
    {
        List<QuestData> result = new List<QuestData>();
        foreach (var questID in _activeQuests.Keys)
        {
            QuestData data = GetQuestData(questID);
            if (data != null)
            {
                result.Add(data);
            }
        }
        return result;
    }

    #endregion

    #region 저장/로드

    /// <summary>
    /// 퀘스트 진행 상황 저장
    /// </summary>
    private void SaveQuestProgress()
    {
        if (GameSessionManager.Instance == null) return;

        CharacterSaveData saveData = GameSessionManager.Instance.CurrentCharacterData;
        if (saveData == null) return;

        // 완료된 퀘스트 저장
        saveData.gameplay.questsCompleted = new List<string>(_completedQuestIDs);

        // 진행 중인 퀘스트 저장
        saveData.gameplay.questProgress = new List<QuestProgress>(_activeQuests.Values);

        // 자동 저장 트리거
        GameSessionManager.Instance.SaveCurrentCharacter();
    }

    #endregion

    #region 씬 이벤트

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _currentSceneName = scene.name;
        Log($"씬 로드: {_currentSceneName}");
    }

    #endregion

    #region 디버그

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[QuestManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[QuestManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[QuestManager] {message}");
    }

    /// <summary>
    /// 현재 상태 출력 (디버그용)
    /// </summary>
    [ContextMenu("Print Quest Status")]
    public void PrintQuestStatus()
    {
        Debug.Log("===== 퀘스트 시스템 상태 =====");
        Debug.Log($"완료된 퀘스트: {_completedQuestIDs.Count}개");
        foreach (var id in _completedQuestIDs)
        {
            Debug.Log($"  - {id}");
        }

        Debug.Log($"진행 중인 퀘스트: {_activeQuests.Count}개");
        foreach (var kvp in _activeQuests)
        {
            QuestData data = GetQuestData(kvp.Key);
            QuestProgress progress = kvp.Value;

            Debug.Log($"  - {data.QuestName} ({kvp.Key})");
            foreach (var objective in data.Objectives)
            {
                string key = QuestData.GetObjectiveKey(objective);
                int current = progress.GetObjectiveProgress(key);
                Debug.Log($"    {objective.SceneName}: {objective.EnemyType} {current}/{objective.RequiredCount}");
            }
        }
    }

    #endregion
}
