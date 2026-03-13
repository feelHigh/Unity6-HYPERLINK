using UnityEngine;

[System.Serializable]
public abstract class TileGenerator : MonoBehaviour
{
    [SerializeField] TileType _type;
    public TileType Type => _type;


    public abstract void Generate1X1(Vector3 pos, float cellSize, Transform parent);
    public abstract void Generate2X2(Vector3 pos, float cellSize, Transform parent);
}
