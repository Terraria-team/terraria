using System;
using System.Collections.Generic;
using Core.WorldGeneration;
using Mirror;
using Server.SODefinitions;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkManager : NetworkBehaviour
{
    public static ChunkManager Instance;
    
    private Dictionary<Vector2Int, ChunkData> _initialChunks = new();
    private Dictionary<Vector2Int, ChunkData> _visibleChunks = new();
    
    [SerializeField] private WorldGenerationConfig worldConfig; 
    [SerializeField] private BiomeGenerationConfig forestConfig;  

    public Tilemap playerGrid;
    
    private readonly Dictionary<Vector2Int, List<NetworkConnectionToClient>> _chunkTrackers = new();
    
    [Server]
    public void Place(Vector2Int chunkCoord, byte x, byte y, BlockID value)
    {
        ServerApplyChunkDelta(chunkCoord, x, y, value);
    }

    [Command(requiresAuthority = false)]
    public void CmdSubscribeToChunk(Vector2Int chunkCoord, NetworkConnectionToClient subscriber = null)
    {
        // TODO check whether subscription is valid
        // TODO validate coordinates
        
        if (!_chunkTrackers.ContainsKey(chunkCoord))
            _chunkTrackers[chunkCoord] = new List<NetworkConnectionToClient>();

        if (!_chunkTrackers[chunkCoord].Contains(subscriber))
        {
            Debug.Log($"Subscribed: {subscriber}");
            _chunkTrackers[chunkCoord].Add(subscriber); // TODO rewrite to set
            
            var fullDelta = new SparseChunkDelta(
                _initialChunks[chunkCoord],
                _visibleChunks[chunkCoord]
            );

            TargetReceiveChunkDelta(subscriber, chunkCoord, fullDelta);
        }
    }
    
    [Server]
    void ServerApplyChunkDelta(Vector2Int chunkCoord, byte x, byte y, BlockID value)
    {
        // TODO validate coordinates
        
        // Build delta
        var list = new List<ChunkDeltaEntry> { new (ChunkUtils.ChunkCellIndex(x, y), value) };
        SparseChunkDelta delta = new(list);
        
        if (delta.IsEmpty)
            Debug.LogError("Applying empty delta. This should have been prevented");
        
        // Apply to server's vision of world
        if (!_visibleChunks.ContainsKey(chunkCoord))
        {
            _visibleChunks[chunkCoord] = new ChunkData();
        }
        
        _visibleChunks[chunkCoord] = delta.Apply(_visibleChunks[chunkCoord]);

        var fullDelta = new SparseChunkDelta(
            _initialChunks[chunkCoord],
            _visibleChunks[chunkCoord]
        );
        
        if (!_chunkTrackers.ContainsKey(chunkCoord))
            _chunkTrackers[chunkCoord] = new List<NetworkConnectionToClient>();
        
        foreach (var tracker in _chunkTrackers[chunkCoord])
        {
            // TODO check whether subscription is still valid and remove if out of range
            
            TargetReceiveChunkDelta(tracker, chunkCoord, fullDelta);
        }
    }

    [TargetRpc]
    void TargetReceiveChunkDelta(NetworkConnectionToClient target, Vector2Int chunkCoord, SparseChunkDelta delta)
    {
        _visibleChunks[chunkCoord] = delta.Apply(_initialChunks[chunkCoord]);
        
        var updatedChunk = _visibleChunks[chunkCoord];
        
        UpdateTileVisualBulk(chunkCoord, updatedChunk);
    }
    
    private void UpdateTileVisualBulk(Vector2Int chunkCoord, ChunkData updatedChunk)
    {
        TileBase[] tiles = new TileBase[ChunkUtils.ChunkMaxIndex];

        for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
        {
            BlockID block = updatedChunk[i];

            tiles[i] = block.IsAir
                ? null
                : block.BlockData.blockTexture;
        }

        BoundsInt bounds = new BoundsInt(
            chunkCoord.x * ChunkUtils.ChunkSize,
            chunkCoord.y * ChunkUtils.ChunkSize,
            0,
            ChunkUtils.ChunkSize,
            ChunkUtils.ChunkSize,
            1
        );

        playerGrid.SetTilesBlock(bounds, tiles);
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
        _visibleChunks = SliceMapIntoChunks(rawMap);
    }
    
    private void Start()
    {
        // TODO revisit. Might not be needed as server handles this on connection
        /*if (isClient)
        {
            foreach (var (chunkCoord, chunkData) in _visibleChunks)
            {
                // Initial population of the Tilemap
                for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
                {
                    var coords = ChunkUtils.ChunkCellCoordinates(i);
                    
                    if (chunkData[i].Value > 1)
                        continue;
                    
                    UpdateTileVisual(chunkCoord, coords.x, coords.y, chunkData[i]);
                }
            }
        }*/
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
                ChunkData newChunk = new ChunkData(new BlockID(0));
                
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