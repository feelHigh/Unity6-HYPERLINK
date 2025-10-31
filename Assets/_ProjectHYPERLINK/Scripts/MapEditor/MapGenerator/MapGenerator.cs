using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] MapGround[] _mapBluePrint;
    [SerializeField] List<TileGenerator> _generators;
    Vector3 _startPos;
    float _cellSize;
    MapGround _map;

    Dictionary<TileType, TileGenerator> _generatorDic = new Dictionary<TileType, TileGenerator>();

    MapTileData[,] _mapTiles;
    Dictionary<int, RoomData> _rooms = new Dictionary<int, RoomData>();

    private void Start()
    {
        Initialize();
        GenerateMap();
    }
    public void Initialize()
    {

        int rnd = Random.Range(0, _mapBluePrint.Length);
        _map = Instantiate(_mapBluePrint[rnd]);
        int size = _map.XSize;
        _mapTiles = new MapTileData[size, _map.MapTiles.Length / size];

        for (int i = 0; i < _map.MapTiles.Length; i++)
        {
            _mapTiles[(i / size), (i % size)] = _map.MapTiles[i];
        }

        foreach (var room in _map.Rooms)
        {
            _rooms.Add(room.RoomIndex, room);
        }
        _startPos = _map.StartPos;
        _cellSize = _map.CellSize;
        foreach (var gen in _generators)
        {
            _generatorDic.Add(gen.Type, gen);
        }

    }

    public void GenerateMap()
    {
        foreach(var tile in _mapTiles)
        {
            if(tile.xWall == WallType.Door || tile.yWall == WallType.Door) tile.HasObject = true;
        }
        for (int x = 0; x < _mapTiles.GetLength(0); x++)
        {
            for (int y = 0; y < _mapTiles.GetLength(1); y++)
            {
                if (_mapTiles[x, y].Type != TileType.Empty)
                {
                    if (_mapTiles[x, y].HasObject) continue;
                    Vector3 pos = _startPos + new Vector3(x * _cellSize, 0, y* _cellSize);
                    _mapTiles[x, y].HasObject = true;
                    if (x > 0 && _mapTiles[x - 1, y].xWall == WallType.Door)
                    {
                        Debug.Log(x + "," + y);
                        continue;
                    }
                    if (y > 0 && _mapTiles[x, y-1].yWall == WallType.Door)
                    {
                        Debug.Log(x + "," + y);
                        continue;
                    }
                    int rnd = Random.Range(0, 3);
                    TileType type = _mapTiles[x, y].Type;
                    switch (rnd)
                    {
                        case 0:
                            break;

                        case 1:
                            if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate1X1(pos, _cellSize, _map.RuntimeParent);
                            break;

                        case 2:
                            if (CheckCanPlace(x, y))
                            {
                                for (int i = x; i <= x + 1; i++)
                                {
                                    for (int j = y; j <= y + 1; j++)
                                    {
                                        _mapTiles[i, j].HasObject = true;
                                    }
                                }
                                pos.x += _cellSize / 2;
                                pos.z += _cellSize / 2;
                                if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate2X2(pos,_cellSize, _map.RuntimeParent);
                            }
                            else
                            {
                                if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate1X1(pos, _cellSize, _map.RuntimeParent);
                            }
                            break;
                        default:
                            break;
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
                if(_mapTiles[i, j].HasObject)
                {
                   
                    return false;
                }

            }
        }
        return true;

    }

}
