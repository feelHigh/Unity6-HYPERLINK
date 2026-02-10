using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 생성 시스템
/// 
/// 초기화 흐름:
/// 1. Awake: 기본 변수만 초기화
/// 2. Start: 코루틴으로 비동기 초기화 시작
/// 3. InitializeAsync: 의존성 체크 → 맵 생성 → NavMesh 베이킹
/// 
/// 의존성:
/// - PlayerSpawner: 텔레포트 포인트 등록용
/// - MapConfig 또는 MapGround 프리팹
/// </summary>
public class MapGenerator : MonoBehaviour
{
    [Header("맵 블루프린트")]
    [SerializeField] MapGround[] _mapBluePrint;

    [Header("시스템 참조")]
    [SerializeField] PlayerSpawner _playerSpawner;

    [Header("타일 생성기")]
    [SerializeField] List<TileGenerator> _generators;

    [Header("포탈")]
    [SerializeField] List<Portal> _portals;

    [Header("타일 생성 가중치")]
    [SerializeField] int _tileNoneValue = 1;
    [SerializeField] int _tile2X2Value = 2;
    [SerializeField] int _tile1X1Value = 3;

    [Header("초기화 설정")]
    [Tooltip("의존성 대기 타임아웃 (초)")]
    [SerializeField] private float _dependencyTimeout = 10f;

    [Header("디버그")]
    [SerializeField] private bool _enableDebugLogs = true;

    // 내부 상태
    private Vector3 _startPos;
    private float _cellSize;
    private MapGround _map;
    private Dictionary<TileType, TileGenerator> _generatorDic = new Dictionary<TileType, TileGenerator>();
    private MapTileData[,] _mapTiles;
    private Dictionary<int, RoomData> _rooms = new Dictionary<int, RoomData>();

    // 초기화 상태 플래그
    private bool _isInitialized = false;
    private bool _isInitializing = false;

    #region Unity Lifecycle

    private void Awake()
    {
        // Awake에서는 기본 검증만 수행
        ValidateConfiguration();
    }

    private void Start()
    {
        // Start에서 비동기 초기화 시작
        if (!_isInitialized && !_isInitializing)
        {
            StartCoroutine(InitializeAsync());
        }
    }

    #endregion

    #region 초기화

    /// <summary>
    /// 비동기 초기화 프로세스
    /// 의존성 체크 → 맵 생성 → NavMesh 베이킹
    /// </summary>
    private IEnumerator InitializeAsync()
    {
        _isInitializing = true;
        Log("=== 맵 생성 초기화 시작 ===");

        // 1. 의존성 대기
        yield return StartCoroutine(WaitForDependencies());

        // 2. 맵 초기화
        Log("Phase 1: 맵 데이터 초기화");
        bool initSuccess = Initialize();

        if (!initSuccess)
        {
            LogError("맵 초기화 실패!");
            _isInitializing = false;
            yield break;
        }

        // 3. 맵 생성
        Log("Phase 2: 맵 생성 (타일 배치)");
        GenerateMap();

        // 한 프레임 대기 (생성된 오브젝트들이 씬에 등록되도록)
        yield return null;

        // 4. NavMesh 베이킹 (비동기)
        Log("Phase 3: NavMesh 베이킹");
        yield return StartCoroutine(BakeNavMeshAsync());

        _isInitialized = true;
        _isInitializing = false;
        Log("=== 맵 생성 완료 ===");
    }

    /// <summary>
    /// 의존성 대기 (PlayerSpawner 등)
    /// </summary>
    private IEnumerator WaitForDependencies()
    {
        Log("의존성 체크 중...");

        float elapsed = 0f;

        // PlayerSpawner 대기
        while (_playerSpawner == null && elapsed < _dependencyTimeout)
        {
            _playerSpawner = PlayerSpawner.Instance;

            if (_playerSpawner == null)
            {
                yield return WaitForSecondsCache.Get(0.1f);
                elapsed += 0.1f;
            }
        }

        if (_playerSpawner == null)
        {
            LogError($"PlayerSpawner를 찾을 수 없습니다! (타임아웃: {_dependencyTimeout}초)");
            LogError("씬에 PlayerSpawner GameObject가 있는지 확인하세요.");
            yield break;
        }

        Log("모든 의존성 준비 완료");
    }

    /// <summary>
    /// 맵 데이터 초기화
    /// </summary>
    private bool Initialize()
    {
        try
        {
            // 블루프린트 검증
            if (_mapBluePrint == null || _mapBluePrint.Length == 0)
            {
                LogError("MapBluePrint가 비어있습니다!");
                return false;
            }

            // 랜덤 맵 선택
            int rnd = Random.Range(0, _mapBluePrint.Length);
            _map = Instantiate(_mapBluePrint[rnd]);

            if (_map == null)
            {
                LogError("맵 인스턴스 생성 실패!");
                return false;
            }

            // 타일 배열 초기화
            int size = _map.XSize;
            _mapTiles = new MapTileData[size, _map.MapTiles.Length / size];

            for (int i = 0; i < _map.MapTiles.Length; i++)
            {
                _mapTiles[(i / size), (i % size)] = _map.MapTiles[i];
            }

            // 방 데이터 초기화
            foreach (var room in _map.Rooms)
            {
                _rooms.Add(room.RoomIndex, room);
            }

            _startPos = _map.StartPos;
            _cellSize = _map.CellSize;

            // 타일 생성기 딕셔너리 생성
            _generatorDic.Clear();
            foreach (var gen in _generators)
            {
                if (gen != null)
                {
                    _generatorDic.Add(gen.Type, gen);
                }
            }

            // 포탈 정보를 PlayerSpawner에 등록
            RegisterPortals(rnd);

            Log($"맵 초기화 완료: {_map.name}");
            return true;
        }
        catch (System.Exception e)
        {
            LogError($"초기화 중 예외 발생: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 포탈을 PlayerSpawner에 등록
    /// </summary>
    private void RegisterPortals(int blueprintIndex)
    {
        if (_playerSpawner == null)
        {
            LogWarning("PlayerSpawner가 없어서 포탈 등록을 건너뜁니다.");
            return;
        }

        _portals = _mapBluePrint[blueprintIndex].Portals;

        if (_portals == null || _portals.Count == 0)
        {
            Log("등록할 포탈이 없습니다.");
            return;
        }

        foreach (var portal in _portals)
        {
            if (portal == null) continue;

            TeleportPoint point = new TeleportPoint();
            point.PortalToPoint(portal);

            // PlayerSpawner의 텔레포트 포인트 리스트가 null이 아닌지 확인
            if (_playerSpawner._teleportPoints == null)
            {
                _playerSpawner._teleportPoints = new List<TeleportPoint>();
            }

            _playerSpawner._teleportPoints.Add(point);
        }

        Log($"{_portals.Count}개의 포탈 등록 완료");
    }

    #endregion

    #region 맵 생성

    /// <summary>
    /// 맵 생성 (타일 오브젝트 배치)
    /// </summary>
    public void GenerateMap()
    {
        foreach (var tile in _mapTiles)
        {
            if (tile.xWall == WallType.Door || tile.yWall == WallType.Door) tile.HasObject = true;
        }
        for (int x = 0; x < _mapTiles.GetLength(0); x++)
        {
            for (int y = 0; y < _mapTiles.GetLength(1); y++)
            {
                if (_mapTiles[x, y].Type != TileType.Empty)
                {
                    if (_mapTiles[x, y].HasObject) continue;
                    Vector3 pos = _startPos + new Vector3(x * _cellSize, 0, y * _cellSize);
                    _mapTiles[x, y].HasObject = true;
                    if (x > 0 && _mapTiles[x - 1, y].xWall == WallType.Door)
                    {

                        continue;
                    }
                    if (y > 0 && _mapTiles[x, y - 1].yWall == WallType.Door)
                    {

                        continue;
                    }
                    int rnd = Random.Range(0, _tileNoneValue + _tile2X2Value + _tile1X1Value);
                    TileType type = _mapTiles[x, y].Type;

                    if ((rnd -= _tileNoneValue) <= 0)
                    {

                    }
                    else if ((rnd -= _tile2X2Value) <= 0)
                    {
                        if (!CheckCanPlace(x, y)) continue;
                        bool caniMake = true;
                        _mapTiles[x, y].HasObject = true;
                        for (int i = x; i <= x + 1; i++)
                        {
                            for (int j = y; j <= y + 1; j++)
                            {
                                if (!CheckCanPlace(i, j))  caniMake = false;
                            }
                        }
                        pos.x += _cellSize / 2;
                        pos.z += _cellSize / 2;
                        if (caniMake)
                        {
                            for (int i = x; i <= x + 1; i++)
                            {
                                for (int j = y; j <= y + 1; j++)
                                {
                                    _mapTiles[i, j].HasObject = true;
                                }
                            }
                            if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate2X2(pos, _cellSize, _map.RuntimeParent);
                        }
                        else
                        {
                            if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate1X1(pos, _cellSize, _map.RuntimeParent);
                        }
                    }
                    else if ((rnd -= _tile1X1Value) <= 0)
                    {
                        if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate1X1(pos, _cellSize, _map.RuntimeParent);
                    }

                }
            }
        }
    }

    bool CheckCanPlace(int x, int y)
    {
        if (x + 1 >= _mapTiles.GetLength(0) || y + 1 >= _mapTiles.GetLength(1))
        {

            return false;
        }
        int roomNum = _mapTiles[x, y].RoomNum;
        for (int i = x; i <= x + 1; i++)
        {
            for (int j = y; j <= y + 1; j++)
            {
                if (i == x && j == y) continue;
                if (_mapTiles[i, j].RoomNum != roomNum)
                {

                    return false;
                }
                if (_mapTiles[i, j].HasObject)
                {

                    return false;
                }

            }
        }
        return true;
    }

    #endregion

    #region NavMesh 베이킹

    /// <summary>
    /// NavMesh 비동기 베이킹
    /// 프레임 블록킹을 방지하기 위해 코루틴 사용
    /// </summary>
    private IEnumerator BakeNavMeshAsync()
    {
        if (_map == null)
        {
            LogError("맵이 없어서 NavMesh 베이킹을 건너뜁니다.");
            yield break;
        }

        Log("NavMesh 베이킹 시작...");

        // 한 프레임 대기 (모든 오브젝트가 씬에 배치되도록)
        yield return null;

        // NavMesh 베이킹 실행
        _map.Bake();

        Log("NavMesh 베이킹 완료");
    }

    #endregion

    #region 검증 및 디버그

    /// <summary>
    /// 초기 설정 검증
    /// </summary>
    private void ValidateConfiguration()
    {
        if (_mapBluePrint == null || _mapBluePrint.Length == 0)
        {
            LogError("MapBluePrint가 할당되지 않았습니다!");
        }

        if (_generators == null || _generators.Count == 0)
        {
            LogWarning("TileGenerator가 없습니다. 타일이 생성되지 않을 수 있습니다.");
        }

        // PlayerSpawner는 런타임에 찾으므로 여기서는 체크 안 함
    }

    /// <summary>
    /// 초기화 완료 여부
    /// </summary>
    public bool IsInitialized() => _isInitialized;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void Log(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[MapGenerator] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogWarning(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.LogWarning($"[MapGenerator] {message}");
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogError(string message)
    {
        Debug.LogError($"[MapGenerator] {message}");
    }

    #endregion

    #region Public API (호환성)

    /// <summary>
    /// 외부에서 수동으로 초기화 시작 (필요 시)
    /// </summary>
    public void StartInitialization()
    {
        if (!_isInitialized && !_isInitializing)
        {
            StartCoroutine(InitializeAsync());
        }
    }

    #endregion
}
