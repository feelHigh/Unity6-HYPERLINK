using System;
using UnityEngine;

/// <summary>
/// 플레이어 스폰 / 텔레포트 시스템
/// 
/// 변경사항:
/// - 기존 기능 유지
/// - PlayerInitializationManager와 통합 가능하도록 준비
/// - GetPlayer() 메서드를 통해 스폰 상태 확인 가능
/// 
/// 기능:
/// - 지정한 위치에 직업별 플레이어 스폰
/// - 위치 간의 텔레포트 관리
/// - 싱글톤 구조
/// 
/// 참고:
/// - PlayerInitializationManager가 이 클래스의 GetPlayer()를 폴링하여 스폰 완료 감지
/// - Start()에서 자동 스폰하므로 변경 없음
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [Header("Player Prefabs - 직업별")]
    [Tooltip("Laon 프리팹")]
    [SerializeField] private GameObject _warriorPrefab;
    [Tooltip("Sian 프리팹")]
    [SerializeField] private GameObject _magePrefab;
    [Tooltip("Yujin 프리팹")]
    [SerializeField] private GameObject _archerPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform _defaultSpawnPoint;

    [Header("Teleport Points")]
    [SerializeField] private TeleportPoint[] _teleportPoints;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

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
        // Portal에서 지정한 스폰 포인트 확인
        if (PlayerPrefs.HasKey("TargetSpawnPoint"))
        {
            string targetSpawn = PlayerPrefs.GetString("TargetSpawnPoint");
            PlayerPrefs.DeleteKey("TargetSpawnPoint");

            Log($"Portal 지정 스폰 포인트: {targetSpawn}");

            // TeleportPoint 찾기
            TeleportPoint point = System.Array.Find(_teleportPoints,
                tp => tp.LocationName == targetSpawn);

            if (point != null)
            {
                // 해당 위치에 직접 스폰
                SpawnPlayer(point.Position, point.Rotation);
                Log($"스폰 완료: {targetSpawn}");
            }
            else
            {
                LogWarning($"스폰 포인트 '{targetSpawn}'를 찾을 수 없습니다. 기본 위치에 스폰합니다.");
                SpawnPlayerAtDefault();
            }
        }
        else
        {
            // 디폴트 지점에 자동 스폰
            SpawnPlayerAtDefault();
        }
    }

    /// <summary>
    /// 디폴트 지점에 플레이어 스폰
    /// GameSessionManager에서 직업 정보를 가져와서 올바른 프리팹 스폰
    /// </summary>
    public void SpawnPlayerAtDefault()
    {
        if (_defaultSpawnPoint == null)
        {
            LogError("Default spawn point not set!");
            return;
        }

        SpawnPlayer(_defaultSpawnPoint.position, _defaultSpawnPoint.rotation);
    }

    /// <summary>
    /// 특정 위치에 플레이어 스폰
    /// GameSessionManager의 캐릭터 데이터에서 직업 읽기
    /// </summary>
    public void SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        // 기존 플레이어 제거
        if (_currentPlayer != null)
        {
            Destroy(_currentPlayer);
        }

        // GameSessionManager에서 선택된 직업 가져오기
        GameObject prefabToSpawn = GetPrefabForSelectedClass();

        if (prefabToSpawn == null)
        {
            LogError("선택된 직업에 해당하는 프리팹을 찾을 수 없습니다!");
            return;
        }

        // 플레이어 인스턴스화
        _currentPlayer = Instantiate(prefabToSpawn, position, rotation);

        Log($"Player spawned at {position} - Prefab: {prefabToSpawn.name}");
    }

    /// <summary>
    /// GameSessionManager의 캐릭터 데이터에서 직업에 맞는 프리팹 반환
    /// </summary>
    private GameObject GetPrefabForSelectedClass()
    {
        // GameSessionManager가 없으면 기본값 (Laon)
        if (GameSessionManager.Instance == null)
        {
            LogWarning("GameSessionManager가 없습니다. Laon 프리팹 사용");
            return _warriorPrefab;
        }

        // 캐릭터 데이터가 없으면 기본값
        CharacterSaveData characterData = GameSessionManager.Instance.CurrentCharacterData;
        if (characterData == null)
        {
            LogWarning("캐릭터 데이터가 없습니다. Laon 프리팹 사용");
            return _warriorPrefab;
        }

        // 저장된 직업 문자열을 CharacterClass enum으로 변환
        string classString = characterData.character.characterClass;
        CharacterClass characterClass;

        if (!System.Enum.TryParse(classString, out characterClass))
        {
            LogError($"알 수 없는 직업: {classString}. Laon 프리팹 사용");
            return _warriorPrefab;
        }

        // 직업에 맞는 프리팹 반환
        switch (characterClass)
        {
            case CharacterClass.Laon:
                if (_warriorPrefab == null)
                    LogError("Laon 프리팹이 할당되지 않았습니다!");
                return _warriorPrefab;

            case CharacterClass.Sian:
                if (_magePrefab == null)
                    LogError("Sian 프리팹이 할당되지 않았습니다!");
                return _magePrefab;

            case CharacterClass.Yujin:
                if (_archerPrefab == null)
                    LogError("Yujin 프리팹이 할당되지 않았습니다!");
                return _archerPrefab;

            default:
                LogWarning($"처리되지 않은 직업: {characterClass}. Laon 프리팹 사용");
                return _warriorPrefab;
        }
    }

    /// <summary>
    /// 지점명에 플레이어 텔레포트
    /// </summary>
    public void TeleportToLocation(string locationName)
    {
        TeleportPoint point = Array.Find(_teleportPoints,
            tp => tp.LocationName == locationName);

        if (point != null && _currentPlayer != null)
        {
            _currentPlayer.transform.position = point.Position;
            _currentPlayer.transform.rotation = point.Rotation;

            Log($"Teleported to {locationName}");

            // 텔레포트 후 위치 저장
            SavePlayerPosition();
        }
        else
        {
            LogWarning($"Teleport location '{locationName}' not found!");
        }
    }

    /// <summary>
    /// 현재 플레이어 인스턴스 가져오기
    /// PlayerInitializationManager가 이 메서드로 스폰 상태 확인
    /// </summary>
    public GameObject GetPlayer()
    {
        return _currentPlayer;
    }

    /// <summary>
    /// 플레이어 위치 저장
    /// </summary>
    public void SavePlayerPosition()
    {
        if (_currentPlayer != null)
        {
            PlayerPrefs.SetFloat("LastPosX", _currentPlayer.transform.position.x);
            PlayerPrefs.SetFloat("LastPosY", _currentPlayer.transform.position.y);
            PlayerPrefs.SetFloat("LastPosZ", _currentPlayer.transform.position.z);
            PlayerPrefs.Save();

            Log("플레이어 위치 저장 완료");
        }
    }

    /// <summary>
    /// 마지막 저장 위치에 플레이어 스폰
    /// </summary>
    public void SpawnAtLastPosition()
    {
        Vector3 lastPos = new Vector3(
            PlayerPrefs.GetFloat("LastPosX", 0),
            PlayerPrefs.GetFloat("LastPosY", 0),
            PlayerPrefs.GetFloat("LastPosZ", 0)
        );

        Log($"마지막 저장 위치에 스폰: {lastPos}");
        SpawnPlayer(lastPos, Quaternion.identity);
    }

    #region 로깅

    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[PlayerSpawner] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[PlayerSpawner] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[PlayerSpawner] {message}");
    }

    #endregion
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
