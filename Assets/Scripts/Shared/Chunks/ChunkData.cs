using System;
using UnityEngine;
using Random = System.Random;

public readonly struct ChunkData
{
    public ChunkData(BlockID initialValue, bool randomize)
    {
        _matrix = new BlockID[ChunkUtils.ChunkMaxIndex];
        
        Array.Fill(_matrix, initialValue);
        
        if (!randomize)
            return;
        
        for (int i = 0; i < ChunkUtils.ChunkMaxIndex / 2; i++)
        {
            Random rand = new Random();
            int index = rand.Next(0, ChunkUtils.ChunkMaxIndex);
            _matrix[index] = new BlockID(0);
        }
    }
    
    private readonly BlockID[] _matrix;
    
    public BlockID Get(byte x, byte y) => _matrix[ChunkUtils.ChunkCellIndex(x, y)];
    public void Set(byte x, byte y, BlockID value) => _matrix[ChunkUtils.ChunkCellIndex(x, y)] = value;
    
    public BlockID this[ushort index]
    {
        get => _matrix[index];
        set => _matrix[index] = value;
    }
}
