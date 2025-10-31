public enum TileType
{
    Empty,
    Office,
    Kitchen,
    Corridor,
    Enter,
    Exit
}

public enum WallType
{
    Empty,
    Wall,
    Door,
}
[System.Serializable]
public class MapTileData
{
    public int RoomNum = 0;
    public WallType xWall = WallType.Empty;
    public WallType yWall = WallType.Empty;
    public TileType Type = TileType.Empty;
    public bool HasObject = false;
}
