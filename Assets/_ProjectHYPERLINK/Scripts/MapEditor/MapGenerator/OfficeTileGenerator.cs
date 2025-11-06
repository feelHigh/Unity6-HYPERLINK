    using UnityEngine;

public class OfficeTileGenerator : TileGenerator
{
    [SerializeField] GameObject[] _object1X1;
    [SerializeField] GameObject[] _object2X2;

    public override void Generate1X1(Vector3 pos, float cellSize, Transform parent)
    {
        if (_object1X1.Length == 0) return;
        float xdistance = Random.Range(-cellSize / 3, cellSize / 3);
        float ydistance = Random.Range(-cellSize / 3, cellSize / 3);
        pos.x += xdistance;
        pos.z += ydistance;
        int num = Random.Range(0, _object1X1.Length);
        GameObject ob = Instantiate(_object1X1[num], pos, Quaternion.identity, parent);
        float rot = Random.Range(0, 360);
        ob.transform.Rotate(new Vector3(0, rot, 0));
    }

    public override void Generate2X2(Vector3 pos, float cellSize, Transform parent)
    {
        if (_object2X2.Length == 0) return;
        int num = Random.Range(0, _object2X2.Length);
        GameObject ob = Instantiate(_object2X2[num], pos, Quaternion.identity, parent);
        int rot = Random.Range(0, 4);
        ob.transform.Rotate(new Vector3(0, 90*rot, 0));
    }
}
