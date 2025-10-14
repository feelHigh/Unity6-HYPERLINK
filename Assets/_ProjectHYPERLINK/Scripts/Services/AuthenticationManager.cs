using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// Unity Authentication Service 인증 관리
/// 
/// Unity Authentication 3.5.2+ 호환
/// 
/// 지원 인증 방식:
/// - 익명 로그인 (Anonymous)
/// - 사용자명/비밀번호
/// - 추후 확장 가능: Steam, Apple, Google 등
/// 
/// 이벤트:
/// - OnSignInSuccess: 로그인 성공 (Player ID)
/// - OnSignInFailed: 로그인 실패 (에러 메시지)
/// - OnSignOutSuccess: 로그아웃 성공
/// 
/// 사용 흐름:
/// 1. UGSInitializer로 Unity Services 초기화
/// 2. SignInAnonymouslyAsync() 또는 SignInWithUsernamePasswordAsync()
/// 3. CloudSaveManager 등 다른 서비스 사용
/// </summary>
public class AuthenticationManager : MonoBehaviour
{
    private static AuthenticationManager _instance;

    public static AuthenticationManager Instance => _instance;
    public static bool IsSignedIn => AuthenticationService.Instance?.IsSignedIn ?? false;
    public static string PlayerId => AuthenticationService.Instance?.PlayerId ?? string.Empty;

    public static event Action<string> OnSignInSuccess;    // 로그인 성공 (Player ID)
    public static event Action<string> OnSignInFailed;     // 로그인 실패 (에러 메시지)
    public static event Action OnSignOutSuccess;           // 로그아웃 성공

    private void Awake()
    {
        // 싱글톤
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 익명 로그인
    /// 
    /// 가장 간단한 인증 방식:
    /// - 계정 생성 불필요
    /// - 자동으로 고유 ID 생성
    /// - 디바이스에 저장
    /// 
    /// 주의사항:
    /// - 디바이스 변경 시 계정 손실
    /// - 나중에 사용자명으로 전환 가능
    /// </summary>
    public async Task<bool> SignInAnonymouslyAsync()
    {
        try
        {
            // UGS 초기화 확인
            if (!UGSInitializer.IsInitialized)
            {
                await UGSInitializer.Initialize();
            }

            // 이미 로그인됨
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("이미 로그인되어 있습니다");
                OnSignInSuccess?.Invoke(AuthenticationService.Instance.PlayerId);
                return true;
            }

            // 익명 로그인 실행
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"익명 로그인 성공: {playerId}");

            OnSignInSuccess?.Invoke(playerId);
            return true;
        }
        catch (AuthenticationException e)
        {
            Debug.LogError($"익명 로그인 실패: {e.Message}");
            OnSignInFailed?.Invoke(e.Message);
            return false;
        }
        catch (RequestFailedException e)
        {
            Debug.LogError($"요청 실패: {e.Message}");
            OnSignInFailed?.Invoke("연결 실패. 인터넷 연결을 확인하세요.");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"로그인 오류: {e.Message}");
            OnSignInFailed?.Invoke(e.Message);
            return false;
        }
    }

    /// <summary>
    /// 사용자명/비밀번호 로그인
    /// 
    /// 기존 계정 로그인:
    /// - 사용자명과 비밀번호 필요
    /// - 서버에서 검증
    /// </summary>
    public async Task<bool> SignInWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            if (!UGSInitializer.IsInitialized)
            {
                await UGSInitializer.Initialize();
            }

            // 기존 세션 종료
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"사용자명 로그인 성공: {playerId}");

            OnSignInSuccess?.Invoke(playerId);
            return true;
        }
        catch (AuthenticationException e)
        {
            Debug.LogError($"사용자명 로그인 실패: {e.Message}");
            OnSignInFailed?.Invoke("잘못된 사용자명 또는 비밀번호입니다");
            return false;
        }
        catch (RequestFailedException e)
        {
            Debug.LogError($"요청 실패: {e.Message}");
            OnSignInFailed?.Invoke("연결 실패. 인터넷 연결을 확인하세요.");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"로그인 오류: {e.Message}");
            OnSignInFailed?.Invoke(e.Message);
            return false;
        }
    }

    /// <summary>
    /// 계정 생성
    /// 
    /// 새 계정 등록:
    /// - 고유한 사용자명 필요
    /// - 비밀번호 최소 6자
    /// </summary>
    public async Task<bool> SignUpWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            if (!UGSInitializer.IsInitialized)
            {
                await UGSInitializer.Initialize();
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"계정 생성 성공: {playerId}");

            OnSignInSuccess?.Invoke(playerId);
            return true;
        }
        catch (AuthenticationException e)
        {
            Debug.LogError($"계정 생성 실패: {e.Message}");
            OnSignInFailed?.Invoke("사용자명이 이미 존재하거나 유효하지 않습니다");
            return false;
        }
        catch (RequestFailedException e)
        {
            Debug.LogError($"요청 실패: {e.Message}");
            OnSignInFailed?.Invoke("연결 실패. 인터넷 연결을 확인하세요.");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"계정 생성 오류: {e.Message}");
            OnSignInFailed?.Invoke(e.Message);
            return false;
        }
    }

    /// <summary>
    /// 로그아웃
    /// </summary>
    public void SignOut()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            Debug.Log("로그아웃 완료");
            OnSignOutSuccess?.Invoke();
        }
    }

    /// <summary>
    /// 권한 확인
    /// </summary>
    public bool IsAuthorized()
    {
        return AuthenticationService.Instance.IsSignedIn &&
               AuthenticationService.Instance.IsAuthorized;
    }
}