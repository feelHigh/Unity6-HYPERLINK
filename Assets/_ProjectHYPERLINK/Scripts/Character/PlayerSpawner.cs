using System;
using UnityEngine;

/// <summary>
/// 플레이어 스폰 / 텔레포트 시스템 (리팩토링)
/// 
/// 변경사항:
/// - 단일 프리팹 → 직업별 프리팹 배열로 변경
/// - GameSessionManager에서 선택된 직업 정보 읽기
/// - 직업에 맞는 프리팹 자동 선택
/// 
/// 기능:
/// - 지정한 위치에 직업별 플레이어 스폰
/// - 위치 간의 텔레포트 관리
/// - 싱글톤 구조
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [Header("Player Prefabs - 직업별")]
    [Tooltip("Warrior 프리팹")]
    [SerializeField] private GameObject _warriorPrefab;
    [Tooltip("Mage 프리팹")]
    [SerializeField] private GameObject _magePrefab;
    [Tooltip("Archer 프리팹")]
    [SerializeField] private GameObject _archerPrefab;

    [Header("Spawn Settings")]
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
    /// GameSessionManager에서 직업 정보를 가져와서 올바른 프리팹 스폰
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
            Debug.LogError("선택된 직업에 해당하는 프리팹을 찾을 수 없습니다!");
            return;
        }

        // 플레이어 인스턴스화
        _currentPlayer = Instantiate(prefabToSpawn, position, rotation);

        Debug.Log($"Player spawned at {position} - Prefab: {prefabToSpawn.name}");
    }

    /// <summary>
    /// GameSessionManager의 캐릭터 데이터에서 직업에 맞는 프리팹 반환
    /// </summary>
    private GameObject GetPrefabForSelectedClass()
    {
        // GameSessionManager가 없으면 기본값 (Warrior)
        if (GameSessionManager.Instance == null)
        {
            Debug.LogWarning("GameSessionManager가 없습니다. Warrior 프리팹 사용");
            return _warriorPrefab;
        }

        // 캐릭터 데이터가 없으면 기본값
        CharacterSaveData characterData = GameSessionManager.Instance.CurrentCharacterData;
        if (characterData == null)
        {
            Debug.LogWarning("캐릭터 데이터가 없습니다. Warrior 프리팹 사용");
            return _warriorPrefab;
        }

        // 저장된 직업 문자열을 CharacterClass enum으로 변환
        string classString = characterData.character.characterClass;
        CharacterClass characterClass;

        if (!System.Enum.TryParse(classString, out characterClass))
        {
            Debug.LogError($"알 수 없는 직업: {classString}. Warrior 프리팹 사용");
            return _warriorPrefab;
        }

        // 직업에 맞는 프리팹 반환
        switch (characterClass)
        {
            case CharacterClass.Warrior:
                if (_warriorPrefab == null)
                    Debug.LogError("Warrior 프리팹이 할당되지 않았습니다!");
                return _warriorPrefab;

            case CharacterClass.Mage:
                if (_magePrefab == null)
                    Debug.LogError("Mage 프리팹이 할당되지 않았습니다!");
                return _magePrefab;

            case CharacterClass.Archer:
                if (_archerPrefab == null)
                    Debug.LogError("Archer 프리팹이 할당되지 않았습니다!");
                return _archerPrefab;

            default:
                Debug.LogWarning($"처리되지 않은 직업: {characterClass}. Warrior 프리팹 사용");
                return _warriorPrefab;
        }
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
