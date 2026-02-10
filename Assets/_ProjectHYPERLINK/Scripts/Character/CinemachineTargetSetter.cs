using UnityEngine;
using Unity.Cinemachine;

public class CinemachineTargetSetter : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogs = true;

    private CinemachineCamera _cinemachineCamera;
    private Quaternion _initialRotation;
    private bool _isTargetSet = false;

    private void Awake()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();

        // 초기 회전 저장 (54.7, -45, 0)
        _initialRotation = transform.rotation;

        if (_cinemachineCamera == null)
        {
            LogError("CinemachineCamera component not found!");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        PlayerInitializationManager.OnPlayerSpawned += HandlePlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerInitializationManager.OnPlayerSpawned -= HandlePlayerSpawned;
    }

    private void HandlePlayerSpawned(GameObject player)
    {
        if (player == null)
        {
            LogError("Player is null!");
            return;
        }

        SetTarget(player.transform);
        _isTargetSet = true;
    }

    private void SetTarget(Transform target)
    {
        if (_cinemachineCamera == null) return;

        _cinemachineCamera.Target.TrackingTarget = target;

        // 회전 복원
        transform.rotation = _initialRotation;

        Log($"Target set: {target.name}, rotation preserved");
    }

    public void SetTargetManually(Transform target)
    {
        if (target != null)
        {
            SetTarget(target);
            _isTargetSet = true;
        }
    }

    public bool IsTargetSet() => _isTargetSet;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
            Debug.Log($"[CinemachineTargetSetter] {message}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogError(string message)
    {
        Debug.LogError($"[CinemachineTargetSetter] {message}");
    }
}
