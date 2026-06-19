using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkManager : NetworkBehaviour
{
    private readonly ChunkData _initial = new(new BlockID(0), false);
    
    public ChunkData _localView;
    public static ChunkManager Instance;
    
    public Tilemap playerGrid;
    [SerializeField] private List<BlockData> blockTexture = new List<BlockData>();
    
    public Dictionary<Vector2Int, ChunkData> Chunks = new Dictionary<Vector2Int, ChunkData>();
    public void InjectWorldData(Dictionary<Vector2Int, ChunkData> generatedWorld)
    {
        Chunks = generatedWorld;
    }

    public void Mine(byte x, byte y)
    {
        Debug.Log($"Trying to mine {x} {y}");
        CmdBroadcastChunkDelta(x, y, new BlockID(0));
    }

    public void Place(byte x, byte y, BlockID value)
    {
        Debug.Log($"Trying to place {x} {y}");
        CmdBroadcastChunkDelta(x, y, value);
    }
    
    [Command(requiresAuthority = false)]
    void CmdBroadcastChunkDelta(byte x, byte y, BlockID value)
    {
        var list = new List<ChunkDeltaEntry> { new (ChunkUtils.ChunkCellIndex(x, y), value) };

        SparseChunkDelta delta = new(list);
        
        // Apply to server's internal state so late-joiners can get the updated view.
        delta.Apply(ref _localView);
        
        if (!delta.IsEmpty)
            RpcChunkDeltaReceived(delta);
    }
    
    [ClientRpc]
    void RpcChunkDeltaReceived(SparseChunkDelta delta)
    {
        delta.Apply(ref _localView);
        
        foreach (var entry in delta.Deltas)
        {
            var coords = ChunkUtils.ChunkCellCoordinates(entry.Index);
            UpdateTileVisual(coords.x, coords.y, entry.Value);
        }
    }
    
    public void UpdateTileVisual(byte x, byte y, BlockID value)
    {
        Vector3Int tilePosition = new Vector3Int(x, y, 0);

        int id = value.Value;
        if (id < blockTexture.Count && blockTexture[id] != null)
        {
            TileBase tileToSet = blockTexture[id].blockTexture;
            playerGrid.SetTile(tilePosition, tileToSet);
            Debug.Log($"Set tile to {value.Value} on  position {tilePosition}");
        }
        else
        {
            playerGrid.SetTile(tilePosition, null);
            Debug.Log($"Set tile to {value.Value} on  position {tilePosition}");
        }
    }
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Debug.LogError("Multiple instances of ChunkManager detected");

        _localView = _initial;
    }
    
    // Client
    public override void OnStartClient()
    {
        // Initial population of the Tilemap
        for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
        {
            var coords = ChunkUtils.ChunkCellCoordinates(i);
            UpdateTileVisual(coords.x, coords.y, _localView[i]);
        }
    }
}
