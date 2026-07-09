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
    
    [SerializeField] private float MaxDistance = 5.0f; 
    
    private Dictionary<Vector2Int, List<NetworkConnectionToClient>> _chunkTrackers = new();
    
    public void Mine(Vector2Int chunkCoord, byte x, byte y)
    {
        ServerApplyChunkDelta(chunkCoord, x, y, new BlockID(0));
    }

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
    void ServerApplyChunkDelta(Vector2Int chunkCoord, byte x, byte y, BlockID value, NetworkIdentity sender = null)
    {
        // TODO validate coordinates
        
        if (!IsValidChange(x, y, value, sender))
        {
            Debug.LogWarning($"Client attempted an invalid action at {x}, {y}");
            return; 
        }
        
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
        
        Debug.Log($"Applying delta: {delta.Deltas.Count}");
        
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
        
        // TODO come up with something smarter than this because current approach hurts performance quite a lot
        for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
        {
            var coords = ChunkUtils.ChunkCellCoordinates(i);
            UpdateTileVisual(chunkCoord, coords.x, coords.y, updatedChunk[i]);
        }
    }
    
    public bool IsValidChange(byte x, byte y, BlockID value, NetworkIdentity sender)
    {
        return true;
        
        if (x < 0 || x >= ChunkUtils.ChunkSize || y < 0 || y >= ChunkUtils.ChunkSize) return false;
        
        if (sender == null ) 
        {
            Debug.LogWarning("Validation failed: Sender or player identity is null.");
            return false;
        }
        
        Vector3Int playerCellPos = playerGrid.WorldToCell(sender.transform.position);
        
        Vector2 player2D = new Vector2(playerCellPos.x + 0.5f, playerCellPos.y + 0.5f);
        Vector2 block2D = new Vector2(x + 0.5f, y + 0.5f);

        if (value.Value != 0 && IsOnPlayer(block2D, sender)) return false;
        
        return (IsInRange(player2D, block2D)
                && IsVisible(x, y, player2D));
    }
    
    private bool IsInRange(Vector2 playerPos, Vector2 blockPos)
    {
        float distance = Vector2.Distance(playerPos, blockPos);
        if (distance > MaxDistance)
        {
            Debug.LogWarning($"Validation failed: Player is too far away ({distance} units).");
            return false;
        }

        return true;
    }
    
    private bool IsOnPlayer(Vector2 blockPos, NetworkIdentity sender)
    {
        Collider2D playerCollider = sender.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            Bounds blockBounds = new Bounds(new Vector3(blockPos.x, blockPos.y, 0), Vector3.one);
            if (playerCollider.bounds.Intersects(blockBounds))
            {
                Debug.LogWarning("Validation failed: Player in the block.");
                return true;
            }
        }

        return false;
    }
    
    private bool IsVisible(byte x, byte y, Vector2 playerPos)
    {
        int solidBlocksLayerMask = LayerMask.GetMask("Ground"); 
        Vector2[] targetPoints = {
            new Vector2(x + 0.5f, y + 0.5f),          // Center
            new Vector2(x + 0.1f, y + 0.1f),         // Bottom-Left
            new Vector2(x + 0.9f, y + 0.1f),         // Bottom-Right
            new Vector2(x + 0.1f, y + 0.9f),         // Top-Left
            new Vector2(x + 0.9f, y + 0.9f)          // Top-Right
        };

        foreach (Vector2 point in targetPoints)
        {
            RaycastHit2D hit = Physics2D.Linecast(playerPos, point, solidBlocksLayerMask);
            if (hit.collider == null || IsHitOnTargetBlock(hit.point, x, y))
            {
                return true;  // is visible
            }
        }

        Debug.LogWarning("Validation failed: No part of the block is visible to the player.");
        return false;  // is not visible
        
        bool IsHitOnTargetBlock(Vector2 hitPoint, byte x, byte y)
        {
            float epsilon = 0.05f; 
            return hitPoint.x >= (x - epsilon) && hitPoint.x <= (x + 1 + epsilon) &&
                   hitPoint.y >= (y - epsilon) && hitPoint.y <= (y + 1 + epsilon);
        }
    }
    
    private void UpdateTileVisual(Vector2Int chunkCoord, byte x, byte y, BlockID value)
    {
        Vector3Int tilePosition = new Vector3Int(chunkCoord.x * ChunkUtils.ChunkSize + x, chunkCoord.y * ChunkUtils.ChunkSize + y, 0);
        
        if (value.IsAir)
        {
            playerGrid.SetTile(tilePosition, null);
            return;
        }
        
        TileBase tileToSet = value.BlockData.blockTexture;
        playerGrid.SetTile(tilePosition, tileToSet);
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
        if (isClient)
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
        }
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