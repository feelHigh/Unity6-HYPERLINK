using System.Collections.Generic;

[System.Serializable]
public class RoomData
{
    public int RoomIndex = 0;
    public List<MapTileData> Tiles = new List<MapTileData>();
}
