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
    [SerializeField] int _tileNoneValue =1;
    [SerializeField] int _tile2X2Value =2;
    [SerializeField] int _tile1X1Value = 3;
    Dictionary<int, RoomData> _rooms = new Dictionary<int, RoomData>();

    private void Start()
    {
        Initialize();
        GenerateMap();
        Bake();
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
                        
                        continue;
                    }
                    if (y > 0 && _mapTiles[x, y-1].yWall == WallType.Door)
                    {
                        
                        continue;
                    }
                    int rnd = Random.Range(0, _tileNoneValue+_tile2X2Value+_tile1X1Value);
                    TileType type = _mapTiles[x, y].Type;

                    if((rnd -= _tileNoneValue)<= 0)
                    {
                        
                    }
                    else if ((rnd -= _tile2X2Value)<=0)
                    {
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
                            if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate2X2(pos, _cellSize, _map.RuntimeParent);
                        }
                        else
                        {
                            if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate1X1(pos, _cellSize, _map.RuntimeParent);
                        }
                    }
                    else if((rnd -=_tile1X1Value)<=0)
                    {
                        if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate1X1(pos, _cellSize, _map.RuntimeParent);
                    }
                        //switch (rnd)
                        //{
                        //    case < _tileNoneValue:
                        //        break;

                        //    case < _tile2X2Value:
                        //        if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate1X1(pos, _cellSize, _map.RuntimeParent);
                        //        break;

                        //    case < 6:
                        //        if (CheckCanPlace(x, y))
                        //        {
                        //            for (int i = x; i <= x + 1; i++)
                        //            {
                        //                for (int j = y; j <= y + 1; j++)
                        //                {
                        //                    _mapTiles[i, j].HasObject = true;
                        //                }
                        //            }
                        //            pos.x += _cellSize / 2;
                        //            pos.z += _cellSize / 2;
                        //            if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate2X2(pos, _cellSize, _map.RuntimeParent);
                        //        }
                        //        else
                        //        {
                        //            if (_generatorDic.ContainsKey(type)) _generatorDic[type].Generate1X1(pos, _cellSize, _map.RuntimeParent);
                        //        }
                        //        break;
                        //    default:
                        //        break;
                        //}

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

    public void Bake()
    {
        _map.Bake();
    }

}
