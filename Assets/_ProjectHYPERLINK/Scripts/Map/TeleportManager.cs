using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 텔레포트 시스템 전역 관리자
/// 
/// 기능:
/// - 발견 시스템 (방문한 목적지만 표시)
/// - 목적지 잠금 상태 관리
/// - UI와 Portal 간 중개
/// - 씬 전환/텔레포트 실행
/// </summary>
public class TeleportManager : MonoBehaviour
{
    public static TeleportManager Instance { get; private set; }

    [Header("목적지 데이터")]
    [Tooltip("게임 내 모든 텔레포트 목적지")]
    [SerializeField] private List<TeleportDestinationData> _allDestinations = new List<TeleportDestinationData>();

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;
    [SerializeField] private bool _unlockAllForTesting = false;

    private const string DISCOVERY_PREFIX = "DiscoveredDestination_";

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region 발견 시스템

    /// <summary>
    /// 목적지 발견 처리
    /// Portal이나 씬 로드 시 호출
    /// </summary>
    public void DiscoverDestination(string sceneName, string spawnPointName)
    {
        string key = GetDiscoveryKey(sceneName, spawnPointName);

        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            Log($"새 목적지 발견: {sceneName} - {spawnPointName}");
        }
    }

    /// <summary>
    /// 목적지 발견 여부 확인
    /// </summary>
    public bool IsDestinationDiscovered(string sceneName, string spawnPointName)
    {
        if (_unlockAllForTesting) return true;

        string key = GetDiscoveryKey(sceneName, spawnPointName);
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// 현재 위치를 자동으로 발견 처리
    /// PlayerSpawner나 Portal에서 호출
    /// </summary>
    public void DiscoverCurrentLocation()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // PlayerPrefs에서 현재 스폰 포인트 확인
        if (PlayerPrefs.HasKey("TargetSpawnPoint"))
        {
            string spawnPoint = PlayerPrefs.GetString("TargetSpawnPoint");
            DiscoverDestination(currentScene, spawnPoint);
        }
    }

    /// <summary>
    /// 발견 키 생성
    /// </summary>
    private string GetDiscoveryKey(string sceneName, string spawnPointName)
    {
        return $"{DISCOVERY_PREFIX}{sceneName}_{spawnPointName}";
    }

    /// <summary>
    /// 모든 발견 데이터 초기화 (디버그용)
    /// </summary>
    public void ResetAllDiscoveries()
    {
        foreach (var dest in _allDestinations)
        {
            string key = GetDiscoveryKey(dest.SceneName, dest.SpawnPointName);
            PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        Log("모든 발견 데이터 초기화");
    }

    #endregion

    #region 목적지 조회

    /// <summary>
    /// 발견되고 잠금 해제된 목적지만 반환
    /// </summary>
    public List<TeleportDestinationData> GetAvailableDestinations()
    {
        var result = new List<TeleportDestinationData>();
        for (int i = 0; i < _allDestinations.Count; i++)
        {
            var d = _allDestinations[i];
            if (IsDestinationDiscovered(d.SceneName, d.SpawnPointName) && d.IsUnlocked())
                result.Add(d);
        }
        return result;
    }

    /// <summary>
    /// 발견되었지만 잠긴 목적지 포함
    /// </summary>
    public List<TeleportDestinationData> GetDiscoveredDestinations()
    {
        var result = new List<TeleportDestinationData>();
        for (int i = 0; i < _allDestinations.Count; i++)
        {
            var d = _allDestinations[i];
            if (IsDestinationDiscovered(d.SceneName, d.SpawnPointName))
                result.Add(d);
        }
        return result;
    }

    /// <summary>
    /// 씬별로 그룹화된 목적지 반환
    /// </summary>
    public Dictionary<string, List<TeleportDestinationData>> GetDestinationsGroupedByScene()
    {
        var discovered = GetDiscoveredDestinations();
        var grouped = new Dictionary<string, List<TeleportDestinationData>>();
        for (int i = 0; i < discovered.Count; i++)
        {
            var d = discovered[i];
            if (!grouped.TryGetValue(d.SceneName, out var list))
            {
                list = new List<TeleportDestinationData>();
                grouped[d.SceneName] = list;
            }
            list.Add(d);
        }
        return grouped;
    }

    #endregion

    #region 텔레포트 실행

    /// <summary>
    /// 목적지로 텔레포트 실행
    /// </summary>
    public async void TeleportToDestination(TeleportDestinationData destination)
    {
        if (destination == null || !destination.IsValid())
        {
            LogError("유효하지 않은 목적지입니다");
            return;
        }

        if (!destination.IsUnlocked())
        {
            LogWarning("잠긴 목적지입니다");
            return;
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string targetScene = destination.SceneName;
        string targetSpawnPoint = destination.SpawnPointName;

        Log($"텔레포트 시작: {targetScene} -> {targetSpawnPoint}");

        // 같은 씬 내 텔레포트
        if (currentScene == targetScene)
        {
            if (PlayerSpawner.Instance != null)
            {
                PlayerSpawner.Instance.TeleportToLocation(targetSpawnPoint);
                Log("같은 씬 내 텔레포트 완료");
            }
            else
            {
                LogError("PlayerSpawner를 찾을 수 없습니다");
            }
        }
        // 다른 씬으로 전환
        else
        {
            await PerformSceneTransition(targetScene, targetSpawnPoint);
        }
    }

    /// <summary>
    /// 씬 전환 (Portal의 로직과 동일)
    /// </summary>
    private async System.Threading.Tasks.Task PerformSceneTransition(string targetScene, string spawnPoint)
    {
        Log($"씬 전환 준비: {targetScene} -> {spawnPoint}");

        // 1. 스폰 포인트 저장
        if (!string.IsNullOrEmpty(spawnPoint))
        {
            PlayerPrefs.SetString("TargetSpawnPoint", spawnPoint);
        }
        else
        {
            PlayerPrefs.DeleteKey("TargetSpawnPoint");
        }

        PlayerPrefs.SetString("TargetScene", targetScene);
        PlayerPrefs.Save();

        // 2. 캐릭터 데이터 저장
        if (CharacterDataManager.Instance != null)
        {
            Log("캐릭터 데이터 저장 중...");
            bool saveSuccess = await CharacterDataManager.Instance.CollectAndSaveData();

            if (!saveSuccess)
            {
                LogError("데이터 저장 실패! 씬 전환을 중단합니다.");
                return;
            }

            Log("캐릭터 데이터 저장 완료");
        }

        // 3. 퀘스트 데이터 저장
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SaveQuestProgress();
        }

        // 4. 씬 전환
        Log($"씬 전환 시작: {targetScene}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
    }

    #endregion

    #region 디버그

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            DebugHelper.Log($"[TeleportManager] {message}");
        }
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            DebugHelper.LogWarning($"[TeleportManager] {message}");
        }
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void LogError(string message)
    {
        DebugHelper.LogError($"[TeleportManager] {message}");
    }

    #endregion
}
