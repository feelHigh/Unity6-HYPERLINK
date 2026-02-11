using UnityEngine;
using Unity.Services.Core;
using System.Threading.Tasks;

/// <summary>
/// Unity Gaming Services 통합 초기화 시스템
/// 
/// 사용 방법:
/// 1. LoginScene(또는 첫 씬)에 빈 GameObject 생성
/// 2. 이 스크립트를 컴포넌트로 추가
/// 3. 이름을 "UGSInitializer"로 설정
/// 4. 다른 설정 불필요 - 자동으로 모든 시스템 초기화
/// 
/// UGS 서비스:
/// - Authentication: 인증 (자동 생성)
/// - Cloud Save: 클라우드 저장
/// - 기타 Unity 서비스
/// 
/// 싱글톤 + DontDestroyOnLoad:
/// - 씬 전환 시에도 유지
/// - 한 번만 초기화
/// </summary>
public class UGSInitializer : MonoBehaviour
{
    private static UGSInitializer _instance;
    private static bool _isInitialized = false;

    public static UGSInitializer Instance => _instance;
    public static bool IsInitialized => _isInitialized;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    private void Awake()
    {
        // 싱글톤 패턴
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Log("UGSInitializer Awake 완료");
    }

    private async void Start()
    {
        await Initialize();
    }

    /// <summary>
    /// UGS 통합 초기화 (비동기)
    /// 
    /// 초기화 순서:
    /// 1. Unity Services 초기화
    /// 2. AuthenticationManager 생성 및 초기화
    /// 3. CloudSaveManager 준비 완료
    /// 
    /// 다른 서비스 사용 전 필수:
    /// - AuthenticationManager (자동 생성)
    /// - CloudSaveManager
    /// 
    /// 한 번만 실행되도록 플래그 체크
    /// </summary>
    public static async Task Initialize()
    {
        if (_isInitialized)
        {
            Instance?.Log("이미 초기화됨 - 건너뜀");
            return;
        }

        try
        {
            Instance?.Log("=== UGS 초기화 시작 ===");

            // 1단계: Unity Services 초기화
            Instance?.Log("1단계: Unity Services 초기화 중...");
            await UnityServices.InitializeAsync();
            Instance?.Log("Unity Services 초기화 완료");

            // 2단계: AuthenticationManager 자동 생성 및 초기화
            Instance?.Log("2단계: AuthenticationManager 확인 중...");
            await EnsureAuthenticationManager();
            Instance?.Log("AuthenticationManager 준비 완료");

            // 3단계: CloudSaveManager 확인 (선택적)
            Instance?.Log("3단계: CloudSaveManager 확인 중...");
            EnsureCloudSaveManager();
            Instance?.Log("CloudSaveManager 준비 완료");

            // 초기화 완료
            _isInitialized = true;
            Instance?.Log("=== UGS 초기화 성공 ===");
        }
        catch (System.Exception e)
        {
            Instance?.LogError($"UGS 초기화 실패: {e.Message}");
            Instance?.LogError($"Stack Trace: {e.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// AuthenticationManager 자동 생성 및 초기화
    /// </summary>
    private static async Task EnsureAuthenticationManager()
    {
        // AuthenticationManager가 이미 존재하는지 확인
        if (AuthenticationManager.Instance != null)
        {
            Instance?.Log("AuthenticationManager 이미 존재함");
            return;
        }

        Instance?.Log("AuthenticationManager가 없음 - 자동 생성 중...");

        // 새로운 GameObject에 AuthenticationManager 컴포넌트 추가
        GameObject authManagerObj = new GameObject("AuthenticationManager");
        authManagerObj.AddComponent<AuthenticationManager>();

        // DontDestroyOnLoad 보장
        DontDestroyOnLoad(authManagerObj);

        Instance?.Log("AuthenticationManager 생성 완료");

        // AuthenticationManager의 Awake가 실행될 때까지 잠시 대기
        await Task.Yield();

        // 생성 확인
        if (AuthenticationManager.Instance == null)
        {
            Instance?.LogError("AuthenticationManager 생성 실패!");
            throw new System.Exception("AuthenticationManager 자동 생성 실패");
        }

        Instance?.Log("AuthenticationManager.Instance 확인 완료");
    }

    /// <summary>
    /// CloudSaveManager 존재 확인 (선택적)
    /// 
    /// CloudSaveManager는 CharacterSelectionScene에서 필요
    /// 없으면 경고만 출력
    /// </summary>
    private static void EnsureCloudSaveManager()
    {
        if (CloudSaveManager.Instance != null)
        {
            Instance?.Log("CloudSaveManager 이미 존재함");
            return;
        }

        Instance?.Log("CloudSaveManager가 없음 (CharacterSelectionScene에서 필요)");
    }

    #region 로깅

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            DebugHelper.Log($"[UGSInitializer] {message}");
        }
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOG")]
    private void LogError(string message)
    {
        DebugHelper.LogError($"[UGSInitializer] {message}");
    }

    #endregion

    #region 에디터 도구

#if UNITY_EDITOR
    [ContextMenu("강제 재초기화")]
    private void ForceReinitialize()
    {
        _isInitialized = false;
        DebugHelper.Log("[UGSInitializer] 초기화 플래그 리셋 완료. 게임을 재시작하세요.");
    }

    [ContextMenu("현재 상태 확인")]
    private void CheckStatus()
    {
        DebugHelper.Log("=== UGS 상태 확인 ===");
        DebugHelper.Log($"초기화 완료: {_isInitialized}");
        DebugHelper.Log($"UGSInitializer.Instance: {(Instance != null ? "존재" : "null")}");
        DebugHelper.Log($"AuthenticationManager.Instance: {(AuthenticationManager.Instance != null ? "존재" : "null")}");
        DebugHelper.Log($"CloudSaveManager.Instance: {(CloudSaveManager.Instance != null ? "존재" : "null")}");
        DebugHelper.Log($"로그인 상태: {AuthenticationManager.IsSignedIn}");
        if (AuthenticationManager.IsSignedIn)
        {
            DebugHelper.Log($"Player ID: {AuthenticationManager.PlayerId}");
        }
        DebugHelper.Log("==================");
    }
#endif

    #endregion
}
