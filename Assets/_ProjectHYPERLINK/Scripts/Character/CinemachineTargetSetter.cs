using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Cinemachine Camera 추적 대상 자동 지정
/// 
/// 변경사항:
/// - InvokeRepeating 패턴 제거
/// - PlayerInitializationManager 이벤트 구독 방식으로 변경
/// - OnPlayerSpawned 이벤트 사용 → Player 스폰 시 즉시 반응
/// 
/// 목적:
/// - PlayerInitializationManager의 이벤트를 통해 Player 추적
/// - 플레이어를 추적 대상으로 설정
/// 
/// 사용처:
/// - CinemachineCamera 오브젝트에 추가
/// - PlayerInitializationManager가 반드시 씬에 있어야 함
/// </summary>
public class CinemachineTargetSetter : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogs = true;

    private CinemachineCamera _cinemachineCamera;
    private bool _isTargetSet = false;

    private void Awake()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();

        if (_cinemachineCamera == null)
        {
            LogError("CinemachineCamera component not found!");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // PlayerInitializationManager 이벤트 구독
        PlayerInitializationManager.OnPlayerSpawned += HandlePlayerSpawned;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PlayerInitializationManager.OnPlayerSpawned -= HandlePlayerSpawned;
    }

    /// <summary>
    /// Player 스폰 완료 이벤트 핸들러
    /// PlayerInitializationManager의 Phase 1에서 호출됨
    /// </summary>
    private void HandlePlayerSpawned(GameObject player)
    {
        if (player == null)
        {
            LogError("HandlePlayerSpawned: player가 null입니다!");
            return;
        }

        SetTarget(player.transform);
        _isTargetSet = true;
    }

    /// <summary>
    /// Cinemachine Camera가 추적할 대상 지정
    /// </summary>
    private void SetTarget(Transform target)
    {
        if (_cinemachineCamera == null) return;

        // Set tracking target (for Follow component)
        _cinemachineCamera.Target.TrackingTarget = target;

        // Set LookAt target (for Hard Look At component)
        _cinemachineCamera.Target.LookAtTarget = target;

        Log($"Cinemachine targets set to: {target.name}");
    }

    /// <summary>
    /// 수동으로 타겟 설정 (외부 호출용, Optional)
    /// </summary>
    public void SetTargetManually(Transform target)
    {
        if (target != null)
        {
            SetTarget(target);
            _isTargetSet = true;
        }
    }

    /// <summary>
    /// 타겟이 설정되었는지 확인
    /// </summary>
    public bool IsTargetSet() => _isTargetSet;

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[CinemachineTargetSetter] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[CinemachineTargetSetter] {message}");
    }
}
