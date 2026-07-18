using System.Collections.Generic;

public readonly struct ChunkDeltaEntry
{
    public readonly ushort Index;
    public readonly BlockID Value;

    public ChunkDeltaEntry(ushort index, BlockID value)
    {
        Index = index;
        Value = value;
    }
}

public readonly struct SparseChunkDelta
{
    public readonly List<ChunkDeltaEntry> Deltas;
    public bool IsEmpty => Deltas.Count == 0;

    public SparseChunkDelta(ChunkData original, ChunkData updated)
    {
        Deltas = new List<ChunkDeltaEntry>();
        
        for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
        {
            if (original[i] != updated[i])
                Deltas.Add(new ChunkDeltaEntry(i, updated[i]));
        }
    }

    public SparseChunkDelta(List<ChunkDeltaEntry> deltas)
    {
        Deltas = deltas;
    }

    public ChunkData Apply(ChunkData onImmutable)
    {
        ChunkData on = onImmutable.Clone();
        
        foreach (ChunkDeltaEntry entry in Deltas)
        {
            on[entry.Index] = entry.Value;
        }

        return on;
    }
}
