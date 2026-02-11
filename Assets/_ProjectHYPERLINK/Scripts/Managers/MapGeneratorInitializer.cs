using UnityEngine;
using System.Threading.Tasks;
using System.Collections;

/// <summary>
/// MapGenerator 초기화 관리자
/// 
/// 목적:
/// - MapGenerator 초기화를 GameInitializer와 통합
/// - 맵 생성 완료를 명시적으로 대기
/// - 초기화 순서 보장
/// 
/// 사용법:
/// 1. 씬에 이 컴포넌트를 MapGenerator GameObject에 추가
/// 2. GameInitializer에서 이 매니저의 초기화 완료를 대기
/// 3. MapGenerator가 초기화될 때까지 게임 시작 대기
/// 
/// 초기화 흐름:
/// GameInitializer → MapGeneratorInitializer → MapGenerator
/// 
/// 의존성:
/// - MapGenerator (같은 GameObject에 있어야 함)
/// - PlayerSpawner (씬에 존재해야 함)
/// </summary>
public class MapGeneratorInitializer : MonoBehaviour
{
    private static MapGeneratorInitializer _instance;
    public static MapGeneratorInitializer Instance => _instance;

    [Header("참조")]
    [SerializeField] private MapGenerator _mapGenerator;

    [Header("설정")]
    [Tooltip("맵 생성 타임아웃 (초)")]
    [SerializeField] private float _initializationTimeout = 30f;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    // 초기화 상태
    private bool _isInitialized = false;
    private bool _isInitializing = false;

    #region Singleton

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // MapGenerator 자동 찾기
        if (_mapGenerator == null)
        {
            _mapGenerator = GetComponent<MapGenerator>();
        }

        ValidateConfiguration();
    }

    #endregion

    #region Public API

    /// <summary>
    /// 맵 생성 초기화 시작 및 완료 대기
    /// GameInitializer에서 호출
    /// </summary>
    public async Task<bool> InitializeMapGenerationAsync()
    {
        if (_isInitialized)
        {
            Log("맵 생성이 이미 완료되었습니다.");
            return true;
        }

        if (_isInitializing)
        {
            Log("맵 생성이 이미 진행 중입니다. 완료 대기 중...");
            return await WaitForInitializationComplete();
        }

        _isInitializing = true;
        Log("=== 맵 생성 초기화 시작 ===");

        try
        {
            // MapGenerator 초기화 시작
            if (_mapGenerator != null)
            {
                _mapGenerator.StartInitialization();
            }
            else
            {
                LogError("MapGenerator를 찾을 수 없습니다!");
                _isInitializing = false;
                return false;
            }

            // 초기화 완료 대기
            bool success = await WaitForInitializationComplete();

            if (success)
            {
                _isInitialized = true;
                Log("=== 맵 생성 초기화 완료 ===");
            }
            else
            {
                LogError("맵 생성 초기화 실패!");
            }

            _isInitializing = false;
            return success;
        }
        catch (System.Exception e)
        {
            LogError($"맵 생성 중 예외 발생: {e.Message}");
            _isInitializing = false;
            return false;
        }
    }

    /// <summary>
    /// 초기화 완료 여부
    /// </summary>
    public bool IsInitialized() => _isInitialized;

    #endregion

    #region Private Methods

    /// <summary>
    /// MapGenerator 초기화 완료 대기
    /// </summary>
    private async Task<bool> WaitForInitializationComplete()
    {
        if (_mapGenerator == null)
        {
            LogError("MapGenerator가 없습니다!");
            return false;
        }

        float elapsed = 0f;
        float checkInterval = 0.1f;

        Log("MapGenerator 초기화 완료 대기 중...");

        while (elapsed < _initializationTimeout)
        {
            // MapGenerator가 초기화되었는지 확인
            if (_mapGenerator.IsInitialized())
            {
                Log("MapGenerator 초기화 완료 확인");
                return true;
            }

            await Task.Delay((int)(checkInterval * 1000));
            elapsed += checkInterval;
        }

        LogError($"맵 생성 타임아웃 ({_initializationTimeout}초)");
        return false;
    }

    /// <summary>
    /// 초기 설정 검증
    /// </summary>
    private void ValidateConfiguration()
    {
        if (_mapGenerator == null)
        {
            LogError("MapGenerator가 할당되지 않았습니다!");
            LogError("같은 GameObject에 MapGenerator 컴포넌트를 추가하거나 Inspector에서 할당하세요.");
        }
    }

    #endregion

    #region 로깅

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            DebugHelper.Log($"[MapGeneratorInitializer] {message}");
        }
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void LogError(string message)
    {
        DebugHelper.LogError($"[MapGeneratorInitializer] {message}");
    }

    #endregion
}
