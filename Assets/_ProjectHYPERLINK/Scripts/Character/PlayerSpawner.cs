using System;
using UnityEngine;

/// <summary>
/// 플레이어 스폰 / 텔레포트 시스템
/// 
/// 기능:
/// - 지정한 위치에 플레이어 스폰
/// - 위치 간의 텔레포트 관리
/// - 싱글톤 구조
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [Header("Player Settings")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform _defaultSpawnPoint;

    [Header("Teleport Points")]
    [SerializeField] private TeleportPoint[] _teleportPoints;

    private GameObject _currentPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 디폴트 지점에 자동 스폰
        SpawnPlayerAtDefault();
    }

    /// <summary>
    /// 디폴트 지점에 플레이어 스폰
    /// </summary>
    public void SpawnPlayerAtDefault()
    {
        if (_defaultSpawnPoint == null)
        {
            Debug.LogError("Default spawn point not set!");
            return;
        }

        SpawnPlayer(_defaultSpawnPoint.position, _defaultSpawnPoint.rotation);
    }

    /// <summary>
    /// 특정 위치에 플레이어 스폰
    /// </summary>
    public void SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        // Destroy existing player if any
        if (_currentPlayer != null)
        {
            Destroy(_currentPlayer);
        }

        // 플레이어 인스턴스화
        _currentPlayer = Instantiate(_playerPrefab, position, rotation);

        Debug.Log($"Player spawned at {position}");
    }

    /// <summary>
    /// 지점명에 플레이어 텔레포트
    /// </summary>
    public void TeleportToLocation(string locationName)
    {
        TeleportPoint point = System.Array.Find(_teleportPoints,
            tp => tp.LocationName == locationName);

        if (point != null && _currentPlayer != null)
        {
            _currentPlayer.transform.position = point.Position;
            _currentPlayer.transform.rotation = point.Rotation;

            Debug.Log($"Teleported to {locationName}");
        }
        else
        {
            Debug.LogWarning($"Teleport location '{locationName}' not found!");
        }
    }

    /// <summary>
    /// 현재 플레이어 인스턴스 가져오기
    /// </summary>
    public GameObject GetPlayer()
    {
        return _currentPlayer;
    }

    public void SavePlayerPosition()
    {
        if (_currentPlayer != null)
        {
            PlayerPrefs.SetFloat("LastPosX", _currentPlayer.transform.position.x);
            PlayerPrefs.SetFloat("LastPosY", _currentPlayer.transform.position.y);
            PlayerPrefs.SetFloat("LastPosZ", _currentPlayer.transform.position.z);
        }
    }

    public void SpawnAtLastPosition()
    {
        Vector3 lastPos = new Vector3(
            PlayerPrefs.GetFloat("LastPosX", 0),
            PlayerPrefs.GetFloat("LastPosY", 0),
            PlayerPrefs.GetFloat("LastPosZ", 0)
        );
        SpawnPlayer(lastPos, Quaternion.identity);
    }
}

/// <summary>
/// 텔레포트 지점 데이터 구조
/// </summary>
[Serializable]
public class TeleportPoint
{
    [SerializeField] private string _locationName;
    [SerializeField] private Transform _spawnTransform;

    public string LocationName => _locationName;
    public Vector3 Position => _spawnTransform.position;
    public Quaternion Rotation => _spawnTransform.rotation;
}
