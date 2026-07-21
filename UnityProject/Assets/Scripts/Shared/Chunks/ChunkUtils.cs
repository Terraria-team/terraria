using UnityEngine;

public class ChunkUtils
{
    public const byte ChunkSize = 64; // max 255
    
    public const ushort ChunkMaxIndex = ChunkSize * ChunkSize;
    
    public static (byte x, byte y) ChunkCellCoordinates(ushort index)
    {
        byte x = (byte)(index % ChunkSize);
        byte y = (byte)(index / ChunkSize);
        return (x, y);
    }
    public static ushort ChunkCellIndex(byte x, byte y) => (ushort)(y * ChunkSize + x);

    public static Vector2 WorldPositionOfBlock(Vector2Int chunkCoordinates, byte x, byte y)
    {
        Vector2 chunkCoordinatesWorldPosition = new Vector2(chunkCoordinates.x, chunkCoordinates.y) * ChunkSize;

        Vector2 blockWorldOffset = new Vector2(x + 0.5f, y + 0.5f);
        
        return chunkCoordinatesWorldPosition + blockWorldOffset;
    }
}
