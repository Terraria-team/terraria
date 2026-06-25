public class ChunkUtils
{
    public const byte ChunkSize = 64; // max 255
    
    public const ushort ChunkMaxIndex = ChunkSize * ChunkSize;
    
    public static (byte x, byte y) ChunkCellCoordinates(ushort index)
    {
        byte x = (byte)(index / ChunkSize);
        byte y = (byte)(index % ChunkSize);
        return (x, y);
    }
    public static ushort ChunkCellIndex(byte x, byte y) => (ushort)(x * ChunkSize + y);
}
