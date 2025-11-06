using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public enum DragState
{
    None,
    Dragging,

}

public enum TileBrushType
{
    None,
    Tile,
    Wall,
    Object
}




public class MapEditor : EditorWindow
{
    private Vector2Int _mapSize = new Vector2Int();

    private const float CELL_SIZE = 5;
    public MapConfig _mapConfig;
    private MapGround _currentMap;
    DragState _dragState = DragState.None;

    bool _isMaking = false;
    bool _showGrid = false;

    Vector3 _startPos;
    Dictionary<TileType, Color> _tileByColor = new Dictionary<TileType, Color>();
    Dictionary<TileType, Material> _tileByTileMaterial = new Dictionary<TileType, Material>();
    Dictionary<TileType, Material> _tileByWallMaterial = new Dictionary<TileType, Material>();

    MapTileData[,] _mapTiles;

    string[] _brushNames;
    int _selectedBrushIndex = 1;
    TileBrushType _selectedBrushType;


    string[] _tileNames;
    int _selectedTileIndex = 1;
    TileType _selectedTile = TileType.Empty;

    string[] _wallNames;
    int _selectionWallIndex = 1;
    WallType _selectedWall = WallType.Empty;

    string[] _objectNames;
    int _selectionObjectIndex = 1;
    ObjectType _selectedObject = ObjectType.None;

    Dictionary<int, RoomData> _rooms = new Dictionary<int, RoomData>();

    public void OnEnable()
    {
        _brushNames = Enum.GetNames(typeof(TileBrushType));
        _tileNames = Enum.GetNames(typeof(TileType));
        _wallNames = Enum.GetNames(typeof(WallType));
        _objectNames = Enum.GetNames(typeof(ObjectType));
        SceneView.duringSceneGui += this.OnSceneGUI;
    }

    public void OnDisable()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;

    }

    [MenuItem("Tools/HyperMapEditor")]
    public static void Open()
    {
        GetWindow<MapEditor>();
    }


    private void OnGUI()
    {
        if (_mapConfig == null)
        {
            GUILayout.Label("Config 없음!! 추가 필요", EditorStyles.largeLabel);
            _mapConfig = (MapConfig)EditorGUILayout.ObjectField("ConfigFIle", _mapConfig, typeof(MapConfig), false);
            return;
        }

        GUILayout.Label("그라운드 생성 (Palette)", EditorStyles.boldLabel);

        _mapSize = EditorGUILayout.Vector2IntField("맵 크기", _mapSize);
        if (_currentMap != null) GUILayout.Label(_currentMap.name);
        if (GUILayout.Button("Plane 생성"))
        {
            Generate();
        }
        EditorGUILayout.Space(20);

        GUILayout.Label("타일 및 벽 선택 (Brush)", EditorStyles.boldLabel);
        GUILayout.Label("현재 브러시");
        _selectedBrushIndex = GUILayout.Toolbar(_selectedBrushIndex, _brushNames);
        if (_selectedBrushType != (TileBrushType)_selectedBrushIndex)
        {
            _selectedBrushType = (TileBrushType)_selectedBrushIndex;
        }
        EditorGUILayout.Space(5);

        if (_selectedBrushType == TileBrushType.Tile)
        {
            _selectedTileIndex = GUILayout.Toolbar(_selectedTileIndex, _tileNames);
            if (_selectedTile != (TileType)_selectedTileIndex)
            {
                _selectedTile = (TileType)_selectedTileIndex;
            }
        }
        else if (_selectedBrushType == TileBrushType.Wall)
        {
            _selectionWallIndex = GUILayout.Toolbar(_selectionWallIndex, _wallNames);
            if (_selectedWall != (WallType)_selectionWallIndex)
            {
                _selectedWall = (WallType)_selectionWallIndex;
            }
        }
        else if (_selectedBrushType == TileBrushType.Object)
        {
            _selectionObjectIndex = GUILayout.Toolbar(_selectionObjectIndex, _objectNames);
            if (_selectedObject != (ObjectType)_selectionObjectIndex)
            {
                _selectedObject = (ObjectType)_selectionObjectIndex;
            }
        }

        EditorGUILayout.Space(5);
        if (GUILayout.Button("벽 자동 생성", EditorStyles.miniButtonMid))
        {
            AutoMakeWall();
        }


        EditorGUILayout.Space(20);
        if (_isMaking && _currentMap != null && GUILayout.Button("맵 생성 (Generate)"))
        {
            GenerateMap();
        }


        if (!_isMaking && _currentMap != null)
        {
            if (GUILayout.Button("맵 재설정"))
            {
                _currentMap.RemoveObjects();
                _currentMap.Show();
                _showGrid = true;
                _isMaking = true;
            }
            if (!_showGrid && GUILayout.Button("그리드 활성화"))
            {
                _showGrid = true;
            }
            else if (_showGrid && GUILayout.Button("그리드 비활성화"))
            {
                _showGrid = false;
            }
        }

    }

    /// <summary>
    /// 바닥 생성 함수
    /// </summary>
    void Generate()
    {
        if (_currentMap != null)
        {
            UnityEngine.Object.DestroyImmediate(_currentMap.gameObject);
            _currentMap = null;
        }
        _currentMap = Instantiate(_mapConfig.Ground);
        _currentMap.BuildMapMesh(Vector3.zero, _mapSize.x * CELL_SIZE, _mapSize.y * CELL_SIZE, _mapConfig.NormalMaterial);
        _mapTiles = new MapTileData[_mapSize.x, _mapSize.y];
        for (int x = 0; x < _mapTiles.GetLength(0); x++)
        {
            for (int z = 0; z < _mapTiles.GetLength(1); z++)
            {
                _mapTiles[x, z] = new MapTileData();
            }
        }
        float startPosx = (_mapSize.x - 1) * CELL_SIZE / 2;
        float startPosy = (_mapSize.y - 1) * CELL_SIZE / 2;
        _startPos = -new Vector3(startPosx, 0, startPosy);
        _isMaking = true;
        _showGrid = true;
    }


    /// <summary>
    /// 마우스 클릭과 드래그를 인시갛는 함수
    /// </summary>
    /// <param name="sceneView"></param>
    private void OnSceneGUI(SceneView sceneView)
    {
        if (_showGrid) DrawMapPreview();
        if (!_isMaking) return;
        if (_currentMap == null) return;
        Event e = Event.current;

        if (_selectedBrushType == TileBrushType.Tile)
        {
            TileDraw(e);
        }
        else if (_selectedBrushType == TileBrushType.Wall)
        {
            WallDraw(e);
        }
        else if (_selectedBrushType == TileBrushType.Object)
        {
            ObjectDraw(e);
        }


    }

    /// <summary>
    /// 타일 브러쉬 작동 함수
    /// </summary>
    /// <param name="e"></param>
    void TileDraw(Event e)
    {
        if (e.button == 0 && e.type == EventType.MouseDown)
        {
            if (ChangeTile(e)) _dragState = DragState.Dragging;

        }

        if (_dragState == DragState.Dragging && e.type == EventType.MouseDrag)
        {
            e.Use();
            ChangeTile(e);
        }

        if ((_dragState == DragState.Dragging && e.type == EventType.MouseUp))
        {
            e.Use();
            _dragState = DragState.None;
        }
    }

    /// <summary>
    /// 벽 브러쉬 작동 함수
    /// </summary>
    /// <param name="e"></param>
    void WallDraw(Event e)
    {
        if (e.button == 0 && e.type == EventType.MouseDown)
        {
            if (DrawWall(e)) _dragState = DragState.Dragging;

        }

        if (_dragState == DragState.Dragging && e.type == EventType.MouseDrag)
        {
            e.Use();
            DrawWall(e);
        }

        if ((_dragState == DragState.Dragging && e.type == EventType.MouseUp))
        {
            e.Use();

            _dragState = DragState.None;
        }
    }

    /// <summary>
    /// 타일을 변경해 주는 함수
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    bool ChangeTile(Event e)
    {
        Ray ray = UnityEditor.HandleUtility.GUIPointToWorldRay(e.mousePosition);

        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 pos = ray.GetPoint(enter) - _startPos;
            int x = Mathf.FloorToInt((pos.x + CELL_SIZE / 2) / CELL_SIZE);
            int y = Mathf.FloorToInt((pos.z + CELL_SIZE / 2) / CELL_SIZE);
            if (_mapTiles.GetLength(0) <= x || x < 0) return false;
            if (_mapTiles.GetLength(1) <= y || y < 0) return false;
            _mapTiles[x, y].Type = _selectedTile;

            if (_mapTiles[x, y].Type != TileType.Empty && x + 1 == _mapTiles.GetLength(0) || x + 1 < _mapTiles.GetLength(0) && _mapTiles[x, y].Type != _mapTiles[x + 1, y].Type)
            {
                _mapTiles[x, y].xWall = WallType.Wall;
            }
            else
            {
                _mapTiles[x, y].xWall = WallType.Empty;
            }

            if (x - 1 >= 0 && _mapTiles[x, y].Type != _mapTiles[x - 1, y].Type)
            {
                _mapTiles[x - 1, y].xWall = WallType.Wall;
            }
            else if (x - 1 >= 0)
            {
                _mapTiles[x - 1, y].xWall = WallType.Empty;
            }

            if (_mapTiles[x, y].Type != TileType.Empty && y + 1 == _mapTiles.GetLength(1) || y + 1 < _mapTiles.GetLength(1) && _mapTiles[x, y].Type != _mapTiles[x, y + 1].Type)
            {
                _mapTiles[x, y].yWall = WallType.Wall;
            }
            else
            {
                _mapTiles[x, y].yWall = WallType.Empty;
            }

            if (y - 1 >= 0 && _mapTiles[x, y].Type != _mapTiles[x, y - 1].Type)
            {
                _mapTiles[x, y - 1].yWall = WallType.Wall;
            }
            else if (y - 1 >= 0)
            {
                _mapTiles[x, y - 1].yWall = WallType.Empty;
            }
        }
        return true;
    }

    bool DrawWall(Event e)
    {
        Ray ray = UnityEditor.HandleUtility.GUIPointToWorldRay(e.mousePosition);

        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 pos = ray.GetPoint(enter) - _startPos;
            float xDistnace = MathF.Abs(CELL_SIZE / 2 - pos.x % CELL_SIZE);
            float yDistnace = MathF.Abs(CELL_SIZE / 2 - pos.z % CELL_SIZE);
            int x = 0;
            int y = 0;

            bool xShort = (xDistnace < yDistnace) ? true : false;
            // x가 가까울 때
            if (xShort)
            {
                x = Mathf.FloorToInt((pos.x) / CELL_SIZE);
                y = Mathf.FloorToInt((pos.z + CELL_SIZE / 2) / CELL_SIZE);
            }
            // y 가 가까울 때
            else
            {
                x = Mathf.FloorToInt((pos.x + CELL_SIZE / 2) / CELL_SIZE);
                y = Mathf.FloorToInt((pos.z) / CELL_SIZE);
            }

            if (_mapTiles.GetLength(0) <= x || x < 0) return false;
            if (_mapTiles.GetLength(1) <= y || y < 0) return false;

            if (xShort)
            {
                _mapTiles[x, y].xWall = _selectedWall;
            }
            else
            {
                _mapTiles[x, y].yWall = _selectedWall;
            }

        }
        return true;
    }

    void ObjectDraw(Event e)
    {
        if (e.button == 0 && e.type == EventType.MouseDown)
        {
            e.Use();
            ChangeObject(e);
        }

    }

    void ChangeObject(Event e)
    {
        Ray ray = UnityEditor.HandleUtility.GUIPointToWorldRay(e.mousePosition);

        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 pos = ray.GetPoint(enter) - _startPos;
            int x = Mathf.FloorToInt((pos.x + CELL_SIZE / 2) / CELL_SIZE);
            int y = Mathf.FloorToInt((pos.z + CELL_SIZE / 2) / CELL_SIZE);
            if (_mapTiles.GetLength(0) <= x || x < 0) return;
            if (_mapTiles.GetLength(1) <= y || y < 0) return;
            if (_selectedObject == ObjectType.None)
            {
                _mapTiles[x, y].HasObject = false;
            }
            else
            {
                _mapTiles[x, y].HasObject = true;
            }
            _mapTiles[x, y].OwnObject = _selectedObject;
        }

    }
    void DrawMapPreview()
    {
        for (int x = 0; x < _mapTiles.GetLength(0); x++)
        {
            for (int z = 0; z < _mapTiles.GetLength(1); z++)
            {
                if (_mapTiles[x, z].Type != TileType.Empty)
                {

                    Vector3 worldPos = new Vector3(x * CELL_SIZE, 0, z * CELL_SIZE) + _startPos;

                    Color color = GetGridColor(_mapTiles[x, z].Type);

                    Handles.color = color;
                    Handles.DrawWireCube(worldPos, new Vector3(CELL_SIZE, 0.1f, CELL_SIZE));

                }
                if (_mapTiles[x, z].xWall != WallType.Empty)
                {
                    Vector3 worldPos = new Vector3(x * CELL_SIZE + CELL_SIZE / 2, 1.5f, z * CELL_SIZE) + _startPos;
                    Handles.color = Color.red;
                    Handles.DrawWireCube(worldPos, new Vector3(0.15f, 3, CELL_SIZE));
                    if (_mapTiles[x, z].xWall == WallType.Door)
                    {
                        Handles.color = Color.blue;
                        Handles.DrawWireCube(worldPos, new Vector3(0.15f, 3, 2));
                    }
                }
                if (_mapTiles[x, z].yWall != WallType.Empty)
                {
                    Vector3 worldPos = new Vector3(x * CELL_SIZE, 1.5f, z * CELL_SIZE + CELL_SIZE / 2) + _startPos;
                    Handles.color = Color.red;
                    Handles.DrawWireCube(worldPos, new Vector3(CELL_SIZE, 3, 0.15f));
                    if (_mapTiles[x, z].yWall == WallType.Door)
                    {
                        Handles.color = Color.blue;
                        Handles.DrawWireCube(worldPos, new Vector3(2, 3, 0.15f));
                    }
                }
                if (_mapTiles[x, z].HasObject)
                {
                    Vector3 worldPos = new Vector3(x * CELL_SIZE, 0.5f, z * CELL_SIZE) + _startPos;
                    if (_mapTiles[x, z].OwnObject == ObjectType.Potion) Handles.color = Color.red;
                    else if (_mapTiles[x, z].OwnObject == ObjectType.Teleport) Handles.color = Color.blue;
                    Handles.DrawWireCube(worldPos, new Vector3(CELL_SIZE, 1, CELL_SIZE));
                }

            }
        }
    }

    Color GetGridColor(TileType tile)
    {
        if (_tileByColor.Count == 0)
        {
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                _tileByColor.Add(type, _mapConfig.Color[(int)type]);
            }
        }
        Color color;
        _tileByColor.TryGetValue(tile, out color);
        return color;
    }

    void AutoMakeWall()
    {
        for (int x = 0; x < _mapTiles.GetLength(0); x++)
        {
            for (int z = 0; z < _mapTiles.GetLength(1); z++)
            {
                if (_mapTiles[x, z].Type != TileType.Empty && x + 1 == _mapTiles.GetLength(0) || x + 1 < _mapTiles.GetLength(0) && _mapTiles[x, z].Type != _mapTiles[x + 1, z].Type)
                {
                    _mapTiles[x, z].xWall = WallType.Wall;
                }
                else
                {
                    _mapTiles[x, z].xWall = WallType.Empty;
                }
                if (_mapTiles[x, z].Type != TileType.Empty && z + 1 == _mapTiles.GetLength(1) || z + 1 < _mapTiles.GetLength(1) && _mapTiles[x, z].Type != _mapTiles[x, z + 1].Type)
                {
                    _mapTiles[x, z].yWall = WallType.Wall;
                }
                else
                {
                    _mapTiles[x, z].yWall = WallType.Empty;
                }
            }
        }
    }

    /// <summary>
    /// x와 y가 0인 경우의 로직이 필요
    /// </summary>
    void GenerateMap()
    {
        _currentMap.RemoveObjects();

        _currentMap.Hide();
        _isMaking = false;
        _showGrid = false;
        MakeRooms();

        GenerateVisualMap();
        Save();
    }

    void GenerateVisualMap()
    {
        for (int i = 0; i < _mapTiles.GetLength(1); i++)
        {
            if (_mapTiles[0, i].Type != TileType.Empty)
            {
                Material material = GetWallMaterial(_mapTiles[0, i].Type);
                Vector3 pos = _startPos;
                pos.z += CELL_SIZE * i;
                pos.y += 1.5f;
                pos.x -= CELL_SIZE / 2;
                _currentMap.BuildWallMesh(pos, new Vector3(0.15f, 3, CELL_SIZE), material, material, true);
            }
        }
        for (int i = 0; i < _mapTiles.GetLength(0); i++)
        {
            if (_mapTiles[i, 0].Type != TileType.Empty)
            {
                Material material = GetWallMaterial(_mapTiles[i, 0].Type);
                Vector3 pos = _startPos;
                pos.x += CELL_SIZE * i;
                pos.y += 1.5f;
                pos.z -= CELL_SIZE / 2;
                _currentMap.BuildWallMesh(pos, new Vector3(CELL_SIZE, 3, 0.15f), material, material, false);
            }
        }
        for (int x = 0; x < _mapTiles.GetLength(0); x++)
        {
            for (int y = 0; y < _mapTiles.GetLength(1); y++)
            {
                // 타일 생성 부분
                if (_mapTiles[x, y].Type != TileType.Empty)
                {
                    Material material = GetTileMaterial(_mapTiles[x, y].Type);
                    Vector3 worldPos = new Vector3(x * CELL_SIZE, 0, y * CELL_SIZE) + _startPos;
                    _currentMap.BuildTileMesh(worldPos, CELL_SIZE, CELL_SIZE, material);
                    if (_mapTiles[x,y].Type == TileType.Exit)
                    {
                        _currentMap.BuildPortal(worldPos, _mapConfig.Exit);
                    }
                    else if(_mapTiles[x, y].Type == TileType.Enter)
                    {
                        _currentMap.BuildPortal(worldPos, _mapConfig.Enter);
                    }
                }

                // 벽 생성 부분
                if (_mapTiles[x, y].xWall != WallType.Empty)
                {
                    Vector3 worldPos = new Vector3(x * CELL_SIZE + CELL_SIZE / 2, 1.5f, y * CELL_SIZE) + _startPos;
                    Material firstMaterial = firstMaterial = GetWallMaterial(_mapTiles[x, y].Type);
                    Material secondMaterial;
                    if (x + 1 < _mapTiles.GetLength(0))
                    {
                        secondMaterial = GetWallMaterial(_mapTiles[x + 1, y].Type);
                    }
                    else
                    {
                        secondMaterial = firstMaterial = GetWallMaterial(_mapTiles[x, y].Type);
                    }
                    if (_mapTiles[x, y].xWall == WallType.Door)
                    {
                        Vector3 firstPos = worldPos;
                        Vector3 secondPos = worldPos;
                        firstPos.z -= (CELL_SIZE / 2 - (CELL_SIZE / 2 - 1) / 2);
                        secondPos.z = firstPos.z + (CELL_SIZE / 2 + 1);
                        _currentMap.BuildWallMesh(firstPos, new Vector3(0.15f, 3, ((CELL_SIZE - 2) / 2)), firstMaterial, secondMaterial, true);
                        _currentMap.BuildWallMesh(secondPos, new Vector3(0.15f, 3, ((CELL_SIZE - 2) / 2)), firstMaterial, secondMaterial, true);
                        _currentMap.BuildDoor(worldPos, true);
                    }
                    else
                    {
                        _currentMap.BuildWallMesh(worldPos, new Vector3(0.15f, 3, CELL_SIZE), firstMaterial, secondMaterial, true);
                    }
                }
                if (_mapTiles[x, y].yWall != WallType.Empty)
                {
                    Vector3 worldPos = new Vector3(x * CELL_SIZE, 1.5f, y * CELL_SIZE + CELL_SIZE / 2) + _startPos;
                    Material firstMaterial = firstMaterial = GetWallMaterial(_mapTiles[x, y].Type);
                    Material secondMaterial;
                    if (y + 1 < _mapTiles.GetLength(1))
                    {
                        secondMaterial = GetWallMaterial(_mapTiles[x, y + 1].Type);
                    }
                    else
                    {
                        secondMaterial = firstMaterial = GetWallMaterial(_mapTiles[x, y].Type);
                    }
                    if (_mapTiles[x, y].yWall == WallType.Door)
                    {
                        Vector3 firstPos = worldPos;
                        Vector3 secondPos = worldPos;
                        firstPos.x -= (CELL_SIZE / 2 - (CELL_SIZE / 2 - 1) / 2);
                        secondPos.x = firstPos.x + (CELL_SIZE / 2 + 1);
                        _currentMap.BuildWallMesh(firstPos, new Vector3(((CELL_SIZE - 2) / 2), 3, 0.15f), firstMaterial, secondMaterial, false);
                        _currentMap.BuildWallMesh(secondPos, new Vector3(((CELL_SIZE - 2) / 2), 3, 0.15f), firstMaterial, secondMaterial, false);
                        _currentMap.BuildDoor(worldPos, false);
                    }
                    else
                    {
                        _currentMap.BuildWallMesh(worldPos, new Vector3(CELL_SIZE, 3, 0.15f), firstMaterial, secondMaterial, false);
                    }
                }
                if (_mapTiles[x,y].OwnObject == ObjectType.Potion)
                {
                    Vector3 worldPos = new Vector3(x * CELL_SIZE, 0, y * CELL_SIZE) + _startPos;
                    _currentMap.BuildObject(worldPos,_mapConfig.Potion);
                }
                else if (_mapTiles[x, y].OwnObject == ObjectType.Teleport)
                {
                    Vector3 worldPos = new Vector3(x * CELL_SIZE, 0, y * CELL_SIZE) + _startPos;
                    _currentMap.BuildPortal(worldPos, _mapConfig.PortalPrefab);
                }
            }
        }
    }

    void MakeRooms()
    {
        _rooms.Clear();
        int roomIndex = 1;
        for (int x = 0; x < _mapTiles.GetLength(0); x++)
        {
            for (int y = 0; y < _mapTiles.GetLength(1); y++)
            {
                if (_mapTiles[x, y].Type != TileType.Empty && _mapTiles[x, y].RoomNum == 0)
                {
                    _rooms.Add(roomIndex, new RoomData());
                    _rooms[roomIndex].RoomIndex = roomIndex;
                    CheckRoom(x, y, roomIndex);
                    roomIndex++;
                }
            }
        }
    }

    void Save()
    {
        _currentMap.GetData(_mapTiles, _rooms, _startPos, CELL_SIZE);
        EditorUtility.SetDirty(_currentMap);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    void CheckRoom(int x, int y, int num)
    {
        _rooms[num].Tiles.Add(_mapTiles[x, y]);
        _mapTiles[x, y].RoomNum = num;
        if (x > 0 && _mapTiles[x - 1, y].xWall == WallType.Empty && _mapTiles[x, y].Type == _mapTiles[x - 1, y].Type && _mapTiles[x - 1, y].RoomNum == 0)
        {
            CheckRoom(x - 1, y, num);
        }
        if (x + 1 < _mapTiles.GetLength(0) && _mapTiles[x, y].xWall == WallType.Empty && _mapTiles[x, y].Type == _mapTiles[x + 1, y].Type && _mapTiles[x + 1, y].RoomNum == 0)
        {
            CheckRoom(x + 1, y, num);
        }
        if (y > 0 && _mapTiles[x, y - 1].yWall == WallType.Empty && _mapTiles[x, y].Type == _mapTiles[x, y - 1].Type && _mapTiles[x, y - 1].RoomNum == 0)
        {
            CheckRoom(x, y - 1, num);
        }
        if (y + 1 < _mapTiles.GetLength(1) && _mapTiles[x, y].yWall == WallType.Empty && _mapTiles[x, y].Type == _mapTiles[x, y + 1].Type && _mapTiles[x, y + 1].RoomNum == 0)
        {
            CheckRoom(x, y + 1, num);
        }
    }
    Material GetTileMaterial(TileType tile)
    {
        if (_tileByTileMaterial.Count == 0)
        {
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                _tileByTileMaterial.Add(type, _mapConfig.TileMaterials[(int)type]);
            }
        }
        Material material;
        _tileByTileMaterial.TryGetValue(tile, out material);
        return material;
    }

    Material GetWallMaterial(TileType tile)
    {
        if (_tileByWallMaterial.Count == 0)
        {
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                _tileByWallMaterial.Add(type, _mapConfig.WallMaterials[(int)type]);
            }
        }
        Material mateiral;
        _tileByWallMaterial.TryGetValue(tile, out mateiral);
        return mateiral;
    }

}


