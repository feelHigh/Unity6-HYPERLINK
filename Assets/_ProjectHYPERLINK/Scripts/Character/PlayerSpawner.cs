using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 스폰 / 텔레포트 시스템
/// 
/// 변경사항:
/// - Start()에서 자동 스폰 제거
/// - MapGenerator 완료 후 스폰하도록 변경
/// - GameInitializer가 SpawnPlayerAtCorrectLocation() 호출
/// 
/// 초기화 흐름:
/// 1. Awake: Instance 설정
/// 2. Start: 아무것도 안 함 (대기)
/// 3. GameInitializer: 맵 생성 완료 후 SpawnPlayerAtCorrectLocation() 호출
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [Header("Player Prefabs - 직업별")]
    [SerializeField] private GameObject _warriorPrefab;
    [SerializeField] private GameObject _magePrefab;
    [SerializeField] private GameObject _archerPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform _defaultSpawnPoint;

    [Header("Teleport Points")]
    public List<TeleportPoint> _teleportPoints = new List<TeleportPoint>();

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

    // Start()에서 자동 스폰 제거 - GameInitializer가 명시적으로 호출
    private void Start()
    {
        // 아무것도 안 함 - GameInitializer가 SpawnPlayerAtCorrectLocation() 호출할 때까지 대기
        Log("PlayerSpawner 준비 완료. 스폰 대기 중...");
    }

    /// <summary>
    /// 올바른 위치에 플레이어 스폰 (GameInitializer에서 호출)
    /// Portal을 통해 들어온 경우 지정된 위치에 스폰
    /// </summary>
    public void SpawnPlayerAtCorrectLocation()
    {
        // Portal에서 지정한 스폰 포인트 확인
        if (PlayerPrefs.HasKey("TargetSpawnPoint"))
        {
            string targetSpawn = PlayerPrefs.GetString("TargetSpawnPoint");
            PlayerPrefs.DeleteKey("TargetSpawnPoint");

            Log($"Portal 지정 스폰 포인트: {targetSpawn}");

            TeleportPoint point = FindTeleportPoint(targetSpawn);

            if (point != null)
            {
                SpawnPlayer(point.Position, point.Rotation);
                Log($"스폰 완료: {targetSpawn}");
                return;
            }
            else
            {
                LogWarning($"스폰 포인트 '{targetSpawn}'를 찾을 수 없습니다. 기본 위치에 스폰합니다.");
            }
        }

        // 디폴트 지점에 스폰
        SpawnPlayerAtDefault();
    }

    /// <summary>
    /// TeleportPoint 찾기
    /// </summary>
    private TeleportPoint FindTeleportPoint(string locationName)
    {
        foreach (var point in _teleportPoints)
        {
            if (point.LocationName == locationName)
            {
                return point;
            }
        }
        return null;
    }

    /// <summary>
    /// 디폴트 지점에 플레이어 스폰
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
    /// 리스폰: 기존 플레이어를 유지하면서 위치만 이동
    /// 
    /// 사용처:
    /// - PlayerDeathManager.RespawnPlayer()에서 호출
    /// - 플레이어 사망 후 리스폰 시 사용
    /// 
    /// 차이점:
    /// - SpawnPlayer(): 기존 플레이어 삭제 후 새로 생성
    /// - TeleportToSpawnPoint(): 기존 플레이어 유지하고 이동만
    /// </summary>
    public void TeleportToSpawnPoint()
    {
        if (_currentPlayer == null)
        {
            LogError("현재 플레이어가 없습니다!");
            return;
        }

        if (_defaultSpawnPoint == null)
        {
            LogError("Default spawn point not set!");
            return;
        }

        // 위치 및 회전 설정
        _currentPlayer.transform.position = _defaultSpawnPoint.position;
        _currentPlayer.transform.rotation = _defaultSpawnPoint.rotation;

        // NavMeshAgent가 있다면 워프
        UnityEngine.AI.NavMeshAgent agent = _currentPlayer.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(_defaultSpawnPoint.position);
        }

        Log($"플레이어를 스폰 포인트로 이동: {_defaultSpawnPoint.position}");
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
        TeleportPoint point = FindTeleportPoint(locationName);

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

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[PlayerSpawner] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[PlayerSpawner] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
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

    public void PortalToPoint(Portal portal)
    {
        _locationName = portal.PortalLocation;
        _spawnTransform = portal.transform;
    }
}
