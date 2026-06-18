using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ChunkManager : NetworkBehaviour
{
    private readonly ChunkData _initial = new(new BlockID(1), false);
    
    public ChunkData _localView;
    public static ChunkManager Instance;
    
    public Dictionary<Vector2Int, ChunkData> Chunks = new Dictionary<Vector2Int, ChunkData>();
    public void InjectWorldData(Dictionary<Vector2Int, ChunkData> generatedWorld)
    {
        Chunks = generatedWorld;
    }

    public void Mine(byte x, byte y)
    {
        CmdBroadcastChunkDelta(x, y, new BlockID(0));
    }

    public void Place(byte x, byte y, BlockID value)
    {
        CmdBroadcastChunkDelta(x, y, value);
    }
    
    [Command(requiresAuthority = false)]
    void CmdBroadcastChunkDelta(byte x, byte y, BlockID value)
    {
        var list = new List<ChunkDeltaEntry> { new (ChunkUtils.ChunkCellIndex(x, y), value) };

        SparseChunkDelta delta = new(list);
        
        if (!delta.IsEmpty)
            RpcChunkDeltaReceived(delta);
    }
    
    [ClientRpc]
    void RpcChunkDeltaReceived(SparseChunkDelta delta)
    {
        delta.Apply(ref _localView);
    }
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Debug.LogError("Multiple instances of ChunkManager detected");

        _localView = _initial;
    }
}
