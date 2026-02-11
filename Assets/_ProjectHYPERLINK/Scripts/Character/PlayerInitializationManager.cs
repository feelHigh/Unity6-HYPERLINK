using UnityEngine;
using System;
using System.Threading.Tasks;

/// <summary>
/// Player 초기화 중앙 관리 시스템
/// 
/// 목적:
/// - Player 스폰 및 초기화 과정을 중앙에서 제어
/// - 초기화 순서 보장
/// - 의존 시스템들에게 순차적 이벤트 발생
/// 
/// 초기화 단계:
/// Phase 1: Player 스폰 완료 → OnPlayerSpawned 이벤트
/// Phase 2: PlayerCharacter 데이터 로드 완료 → OnPlayerInitialized 이벤트
/// Phase 3: 모든 시스템 준비 완료 → OnAllSystemsReady 이벤트
/// 
/// 사용 흐름:
/// 1. GameInitializer가 이 매니저의 초기화 시작
/// 2. PlayerSpawner가 Player 스폰
/// 3. 이 매니저가 초기화 진행
/// 4. 각 단계마다 이벤트 발생
/// 5. 의존 시스템들이 이벤트 구독하여 초기화
/// </summary>
public class PlayerInitializationManager : MonoBehaviour
{
    private static PlayerInitializationManager _instance;
    public static PlayerInitializationManager Instance => _instance;

    [Header("디버그 설정")]
    [SerializeField] private bool _enableDebugLogs = true;

    [Header("타임아웃 설정")]
    [SerializeField] private float _playerSpawnTimeout = 10f;  // 플레이어 스폰 대기 시간
    [SerializeField] private float _dataLoadTimeout = 5f;      // 데이터 로드 대기 시간

    // 초기화 상태
    private bool _areAllSystemsReady = false;

    // Player 참조
    private GameObject _playerObject;
    private PlayerCharacter _playerCharacter;

    #region 이벤트 정의

    /// <summary>
    /// Phase 1: Player GameObject가 스폰되었을 때 발생
    /// 의존 시스템: CinemachineTargetSetter, ItemPickupManager, SpawnerManager
    /// </summary>
    public static event Action<GameObject> OnPlayerSpawned;


    public static event Action FindPlayer;

    /// <summary>
    /// Phase 2: PlayerCharacter 컴포넌트와 데이터가 초기화되었을 때 발생
    /// 의존 시스템: CharacterUIController, EquipInventory
    /// </summary>
    public static event Action<PlayerCharacter> OnPlayerInitialized;

    /// <summary>
    /// Phase 3: 모든 시스템이 준비되었을 때 발생
    /// </summary>
    public static event Action OnAllSystemsReady;

    #endregion

    #region 초기화

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    #endregion

    #region Public API

    /// <summary>
    /// 초기화 프로세스 시작 (GameInitializer에서 호출)
    /// </summary>
    public async Task<bool> StartInitialization()
    {
        Log("=== Player 초기화 프로세스 시작 ===");

        try
        {
            // Phase 1: Player 스폰 대기
            if (!await WaitForPlayerSpawn())
            {
                LogError("Phase 1 실패: Player 스폰 타임아웃");
                return false;
            }

            // Phase 2: Player 초기화 (데이터 로드 포함)
            if (!await InitializePlayer())
            {
                LogError("Phase 2 실패: Player 초기화 실패");
                return false;
            }

            // Phase 3: 모든 시스템 준비 완료
            CompleteInitialization();

            Log("=== Player 초기화 프로세스 완료 ===");
            return true;
        }
        catch (Exception e)
        {
            LogError($"초기화 중 예외 발생: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Player GameObject 가져오기
    /// </summary>
    public GameObject GetPlayerObject() => _playerObject;

    /// <summary>
    /// PlayerCharacter 가져오기
    /// </summary>
    public PlayerCharacter GetPlayerCharacter() => _playerCharacter;

    /// <summary>
    /// 초기화 완료 여부
    /// </summary>
    public bool IsFullyInitialized() => _areAllSystemsReady;

    #endregion

    #region Phase 1: Player 스폰 대기

    /// <summary>
    /// PlayerSpawner가 Player를 스폰할 때까지 대기
    /// </summary>
    private async Task<bool> WaitForPlayerSpawn()
    {
        Log("Phase 1: Player 스폰 대기 중...");

        float elapsed = 0f;
        float checkInterval = 0.1f; // 100ms마다 체크

        while (elapsed < _playerSpawnTimeout)
        {
            // PlayerSpawner를 통해 Player 확인
            if (PlayerSpawner.Instance != null)
            {
                _playerObject = PlayerSpawner.Instance.GetPlayer();

                if (_playerObject != null)
                {
                    Log($"Phase 1 완료: Player 스폰됨 ({_playerObject.name})");

                    // Phase 1 이벤트 발생
                    OnPlayerSpawned?.Invoke(_playerObject);
                    FindPlayer?.Invoke();
                    return true;
                }
            }

            await Task.Delay((int)(checkInterval * 1000));
            elapsed += checkInterval;
        }

        LogError($"Player 스폰 타임아웃 ({_playerSpawnTimeout}초)");
        return false;
    }

    #endregion

    #region Phase 2: Player 초기화

    /// <summary>
    /// PlayerCharacter 컴포넌트 초기화 및 데이터 로드
    /// </summary>
    private async Task<bool> InitializePlayer()
    {
        Log("Phase 2: PlayerCharacter 초기화 중...");

        // PlayerCharacter 컴포넌트 가져오기
        _playerCharacter = _playerObject.GetComponent<PlayerCharacter>();

        if (_playerCharacter == null)
        {
            LogError("PlayerCharacter 컴포넌트를 찾을 수 없습니다!");
            return false;
        }

        // CharacterDataManager가 데이터를 적용할 때까지 대기
        if (!await WaitForDataLoad())
        {
            LogError("데이터 로드 타임아웃");
            return false;
        }

        Log("Phase 2 완료: PlayerCharacter 초기화됨");

        // Phase 2 이벤트 발생
        OnPlayerInitialized?.Invoke(_playerCharacter);
        return true;
    }

    /// <summary>
    /// CharacterDataManager가 데이터를 로드하고 적용할 때까지 대기
    /// </summary>
    private async Task<bool> WaitForDataLoad()
    {
        Log("CharacterDataManager 데이터 로드 대기 중...");

        float elapsed = 0f;
        float checkInterval = 0.1f;

        while (elapsed < _dataLoadTimeout)
        {
            // CharacterDataManager에서 시스템 참조 초기화 확인
            if (CharacterDataManager.Instance != null)
            {
                // ExperienceManager와 EquipmentManager가 초기화되었는지 확인
                var exp = _playerObject.GetComponent<ExperienceManager>();
                var equip = _playerObject.GetComponent<EquipmentManager>();

                if (exp != null && equip != null)
                {
                    // 레벨이 1 이상이면 데이터가 로드된 것으로 간주
                    if (exp.CurrentLevel >= 1)
                    {
                        Log("데이터 로드 완료");
                        return true;
                    }
                }
            }

            await Task.Delay((int)(checkInterval * 1000));
            elapsed += checkInterval;
        }

        // 타임아웃이지만 기본 초기화는 성공으로 간주
        Log("데이터 로드 타임아웃이지만 기본 초기화 완료");
        return true;
    }

    #endregion

    #region Phase 3: 전체 시스템 준비

    /// <summary>
    /// 모든 시스템 초기화 완료
    /// </summary>
    private void CompleteInitialization()
    {
        Log("Phase 3: 모든 시스템 준비 완료");

        _areAllSystemsReady = true;

        // Phase 3 이벤트 발생
        OnAllSystemsReady?.Invoke();
    }

    #endregion

    #region 로깅

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            DebugHelper.Log($"[PlayerInitializationManager] {message}");
        }
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void LogError(string message)
    {
        DebugHelper.LogError($"[PlayerInitializationManager] {message}");
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        // 이벤트 정리
        OnPlayerSpawned = null;
        OnPlayerInitialized = null;
        OnAllSystemsReady = null;
    }

    #endregion
}
