using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Cinemachine Camera 추적 대상 자동 지정
/// 
/// 목적:
/// - PlayerSpawner가 플레이어 생성되기 전까지 대기
/// - 플레이어를 추적 대상으로 설정
/// 
/// 사용처:
/// - CinemachineCamera 오브젝트에 추가
/// - Cinemachine이 자동으로 플레이어 추적
/// </summary>
public class CinemachineTargetSetter : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _retryInterval = 0.5f;
    [SerializeField] private int _maxRetries = 20; // 10 seconds max

    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogs = true;

    private CinemachineCamera _cinemachineCamera;
    private int _retryCount = 0;

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

    private void Start()
    {
        // Player 검색
        InvokeRepeating(nameof(TrySetPlayerTarget), 0.5f, _retryInterval);
    }

    /// <summary>
    /// Player 태그를 가진 대상 검색
    /// </summary>
    private void TrySetPlayerTarget()
    {
        _retryCount++;

        // Find player by tag
        GameObject player = GameObject.FindGameObjectWithTag(_playerTag);

        if (player != null)
        {
            SetTarget(player.transform);
            CancelInvoke(nameof(TrySetPlayerTarget));
            Log($"Player target set successfully after {_retryCount} attempts");
        }
        else if (_retryCount >= _maxRetries)
        {
            LogError($"Failed to find player after {_maxRetries} attempts");
            CancelInvoke(nameof(TrySetPlayerTarget));
        }
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
    /// can be called by other scripts
    /// </summary>
    public void SetTargetManually(Transform target)
    {
        if (target != null)
        {
            SetTarget(target);
            CancelInvoke(nameof(TrySetPlayerTarget));
        }
    }

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
