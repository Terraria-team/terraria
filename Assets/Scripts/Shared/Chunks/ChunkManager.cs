using System.Collections.Generic;
using Core.WorldGeneration;
using Mirror;
using Server.SODefinitions;
using UnityEngine;

public class ChunkManager : NetworkBehaviour
{
    public static ChunkManager Instance;
    
    private Dictionary<Vector2Int, ChunkData> _initialChunks = new();
    private Dictionary<Vector2Int, ChunkData> _visibleChunks = new();
    
    [SerializeField] private WorldGenerationConfig worldConfig; 
    [SerializeField] private BiomeGenerationConfig forestConfig;  

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
            RpcChunkDeltaReceived(delta, new Vector2Int(0, 0));
    }
    
    [ClientRpc]
    void RpcChunkDeltaReceived(SparseChunkDelta delta, Vector2Int chunkCoordinates)
    {
        // TODO handle known/unknown
        
        _visibleChunks[chunkCoordinates] = delta.Apply(_initialChunks[chunkCoordinates]);
    }
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Debug.LogError("Multiple instances of ChunkManager detected");

        GenerateAndInjectWorldToChunkManager();
    }
    
    private void GenerateAndInjectWorldToChunkManager()
    {
        var biomeConfigs = new Dictionary<BiomeType, IBiomeGenerationConfig>
        {
            { BiomeType.Forest, forestConfig }
        };
        
        MapGenerator generator = new MapGenerator(worldConfig, biomeConfigs);
        BlockType[,] rawMap = generator.Generate();
        
        _initialChunks = SliceMapIntoChunks(rawMap);
        _visibleChunks = _initialChunks;
    }

    public ChunkData GetChunkAt(Vector2Int chunkCoordinates)
    {
        return _visibleChunks[chunkCoordinates];
    }
    
    private Dictionary<Vector2Int, ChunkData> SliceMapIntoChunks(BlockType[,] rawMap)
    {
        var result = new Dictionary<Vector2Int, ChunkData>();

        int worldHeightInChunks = worldConfig.Height / ChunkUtils.ChunkSize;
        int worldWidthInChunks = worldConfig.Width / ChunkUtils.ChunkSize;

        for (int chunkX = 0; chunkX < worldWidthInChunks; chunkX++)
        {
            for (int chunkY = 0; chunkY < worldHeightInChunks; chunkY++)
            {
                ChunkData newChunk = new ChunkData(new BlockID(0), false);
                
                for (byte localX = 0; localX < 64; localX++)
                {
                    for (byte localY = 0; localY < 64; localY++)
                    {
                        int globalX = chunkX * 64 + localX;
                        int globalY = chunkY * 64 + localY;
                        
                        ushort blockValue = (ushort)rawMap[globalX, globalY];
                        newChunk.Set(localX, localY, new BlockID(blockValue));
                    }
                }
                result.Add(new Vector2Int(chunkX, chunkY), newChunk);
            }
        }
        return result;
    }
}
