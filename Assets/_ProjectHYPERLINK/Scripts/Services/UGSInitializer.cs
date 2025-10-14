using UnityEngine;
using Unity.Services.Core;
using System.Threading.Tasks;

/// <summary>
/// Unity Gaming Services 초기화
/// 
/// UGS 서비스:
/// - Authentication: 인증
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

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await Initialize();
    }

    /// <summary>
    /// UGS 초기화 (비동기)
    /// 
    /// 다른 서비스 사용 전 필수:
    /// - AuthenticationManager
    /// - CloudSaveManager
    /// 
    /// 한 번만 실행되도록 플래그 체크
    /// </summary>
    public static async Task Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            await UnityServices.InitializeAsync();
            _isInitialized = true;
            Debug.Log("UGS 초기화 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGS 초기화 실패: {e.Message}");
            throw;
        }
    }
}