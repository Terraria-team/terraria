using System;

public readonly struct ChunkData
{
    public ChunkData(BlockID initialValue)
    {
        _matrix = new BlockID[ChunkUtils.ChunkMaxIndex];
        
        Array.Fill(_matrix, initialValue);
    }

    public ChunkData(BlockID[] matrix)
    {
        _matrix = matrix;
    }
    
    private readonly BlockID[] _matrix;
    
    public BlockID Get(byte x, byte y) => _matrix[ChunkUtils.ChunkCellIndex(x, y)];
    public void Set(byte x, byte y, BlockID value) => _matrix[ChunkUtils.ChunkCellIndex(x, y)] = value;
    
    public BlockID this[ushort index]
    {
        get => _matrix[index];
        set => _matrix[index] = value;
    }

    public ChunkData Clone()
    {        
        return new ChunkData((BlockID[])_matrix.Clone());
    }
}
