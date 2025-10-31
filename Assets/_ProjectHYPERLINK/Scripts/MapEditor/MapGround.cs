using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;

public class MapGround : MonoBehaviour
{
    [SerializeField] NavMeshSurface _navSurface;
    [SerializeField] ProBuilderMesh _floorMesh;
    [SerializeField] List<ProBuilderMesh> _tiles;
    [SerializeField] List<ProBuilderMesh> _wallMeshes;
    [SerializeField] Transform _tileParent;
    [SerializeField] Transform _wallParent;
    [SerializeField] Transform _runtimeParent;
    [SerializeField] Vector3 _startPos;
    [SerializeField] float _cellSize;

    Renderer _renderer = new Renderer();
    [SerializeField] GameObject _door;
    [SerializeField]MapTileData[] _mapTiles;
    [SerializeField] int _xSize;
    [SerializeField]List<RoomData> _rooms = new List<RoomData>();

    public MapTileData[] MapTiles => _mapTiles;
    public List<RoomData> Rooms => _rooms;
    public int XSize => _xSize; 
    public Vector3 StartPos => _startPos;
    public float CellSize => _cellSize;
    public Transform RuntimeParent => _runtimeParent;

    public void BuildMapMesh(Vector3 pos, float width, float height , Material material)
    {
        _floorMesh = ShapeGenerator.GeneratePlane(PivotLocation.Center, width, height,1,1,Axis.Up);
        _floorMesh.transform.position = pos;
        _floorMesh.transform.SetParent(transform);
        _renderer = _floorMesh.GetComponent<Renderer>();
        _renderer.material = material;
    }

    public void BuildTileMesh(Vector3 pos, float width, float height, Material material)
    {
        _tiles.Add(ShapeGenerator.GeneratePlane(PivotLocation.Center, width, height, 1, 1, Axis.Up));
        ProBuilderMesh tile = _tiles[_tiles.Count - 1];
        tile.transform.position = pos;
        tile.transform.SetParent(_tileParent);
        tile.GetComponent<Renderer>().material = material;
        tile.gameObject.isStatic = true;
    }

    public void BuildWallMesh(Vector3 pos, Vector3 size, Material firstMaterial, Material secondMaterial ,bool isx)
    {
        _wallMeshes.Add(ShapeGenerator.GenerateCube(PivotLocation.Center, size));
        _wallMeshes.Add(ShapeGenerator.GenerateCube(PivotLocation.Center, size));
        Vector3 firstPos = pos;
        Vector3 secondPos = pos;
        if (isx)
        {
            firstPos.x -= 0.075f;
            secondPos.x += 0.075f;
        }
        else
        {
            firstPos.z -= 0.075f;
            secondPos.z += 0.075f;
        }
        for(int i = 1; i <= 2; i++)
        {
            ProBuilderMesh wallMesh = _wallMeshes[_wallMeshes.Count - i];
            wallMesh.transform.SetParent(_wallParent);
            if (i == 1)
            {
                wallMesh.transform.position = firstPos;
                wallMesh.GetComponent<Renderer>().material = firstMaterial;
            }
            else
            {
                wallMesh.transform.position = secondPos;
                wallMesh.GetComponent<Renderer>().material = secondMaterial;
            }
            wallMesh.gameObject.isStatic = true;
        }

    }

    public void BuildDoor(Vector3 pos, bool isx)
    {
        GameObject door = Instantiate(_door,pos,Quaternion.identity,_wallParent);
        if (isx)
        {
            door.transform.Rotate(0, 90, 0);
        }
    }

    public void GetData(MapTileData[,] maps, Dictionary<int, RoomData> rooms,Vector3 startPos, float cellSize)
    {
        _mapTiles = new MapTileData[maps.GetLength(0)*maps.GetLength(1)];
        _xSize = maps.GetLength(0);
        int i = 0;
        foreach(MapTileData mapTileData in maps)
        {
            _mapTiles[i] = new MapTileData();
            _mapTiles[i].RoomNum = mapTileData.RoomNum;
            _mapTiles[i].yWall = mapTileData.yWall;
            _mapTiles[i].xWall = mapTileData.xWall;
            _mapTiles[i++].Type = mapTileData.Type;
        }
        foreach(var room in rooms)
        {
            _rooms.Add(room.Value);
        }
        _startPos = startPos;
        _cellSize = cellSize;

    }

    public void RemoveObjects()
    {
        for(int i =0; i<_wallMeshes.Count; i++)
        {
            GameObject.DestroyImmediate(_wallMeshes[i].gameObject);
        }
        for(int i=0;i<_tiles.Count; i++)
        {
            GameObject.DestroyImmediate(_tiles[i].gameObject);
        }
        _wallMeshes.Clear();
        _tiles.Clear();
        
    }
    public void Hide()
    {
        _renderer.enabled = false;
    }
    public void Show()
    {
        _renderer.enabled=true;
    }
    public void Bake()
    {
        _navSurface.BuildNavMesh();
    }

    
}
