using System.Collections.Generic;
using UnityEngine;

public class OfficeTileGenerator : TileGenerator
{
    [SerializeField] GameObject[] _object1X1;
    [SerializeField] GameObject[] _object2X2;

    public override void Generate1X1(Vector3 pos, float cellSize, Transform parent)
    {
        float xdistance = Random.Range(-cellSize/3, cellSize/3);
        float ydistance = Random.Range(-cellSize/3, cellSize/3);
        pos.x += xdistance;
        pos.z += ydistance;
        Instantiate(_object1X1[0],pos,Quaternion.identity, parent);
    }

    public override void Generate2X2(Vector3 pos, float cellSize, Transform parent)
    {
        Instantiate(_object2X2[0], pos, Quaternion.identity, parent);
    }
}
