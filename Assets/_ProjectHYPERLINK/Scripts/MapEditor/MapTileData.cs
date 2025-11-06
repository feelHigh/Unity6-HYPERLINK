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

public enum ObjectType
{
    None,
    Potion,
    Teleport,
}

[System.Serializable]
public class MapTileData
{
    public int RoomNum = 0;
    public WallType xWall = WallType.Empty;
    public WallType yWall = WallType.Empty;
    public TileType Type = TileType.Empty;
    public bool HasObject = false;
    public ObjectType OwnObject = ObjectType.None;
}
