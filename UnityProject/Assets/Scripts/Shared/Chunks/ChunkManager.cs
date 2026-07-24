using System;
using System.Collections.Generic;
using Core.WorldGeneration;
using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkManager : NetworkBehaviour
{
    public static ChunkManager Instance;
    
    private Dictionary<Vector2Int, ChunkData> _initialChunks = new();
    private Dictionary<Vector2Int, ChunkData> _visibleChunks = new();
    
    [SerializeField] private WorldGenerationConfig worldConfig; 
    
    public Vector2Int WorldSize => new (worldConfig.Width, worldConfig.Height);

    public Tilemap playerGrid;
    
    private readonly Dictionary<Vector2Int, List<NetworkConnectionToClient>> _chunkTrackers = new();

    public bool clientHasFinishedApplying;
    
    [Server]
    public void Place(Vector2Int chunkCoord, byte x, byte y, BlockID value)
    {
        ServerApplyChunkDelta(chunkCoord, x, y, value);
    }

    [Command(requiresAuthority = false)]
    public void CmdSubscribeToChunk(Vector2Int chunkCoord, NetworkConnectionToClient subscriber = null)
    {
        if (!IsChunkRelevantFor(chunkCoord, subscriber.identity.gameObject.transform.position))
            return;
        
        // TODO validate coordinates
        
        if (!_chunkTrackers.ContainsKey(chunkCoord))
            _chunkTrackers[chunkCoord] = new List<NetworkConnectionToClient>();

        if (!_chunkTrackers[chunkCoord].Contains(subscriber))
        {
            //Debug.Log($"Subscribed: {subscriber}");
            _chunkTrackers[chunkCoord].Add(subscriber); // TODO rewrite to set
            
            var fullDelta = new SparseChunkDelta(
                _initialChunks[chunkCoord],
                _visibleChunks[chunkCoord]
            );

            TargetReceiveChunkDelta(subscriber, chunkCoord, fullDelta);
        }
    }

    public static bool IsChunkRelevantFor(Vector2Int chunkCoords, Vector3 position)
    {
        return (ChunkUtils.WorldPositionOfChunkCenter(chunkCoords) - new Vector2(position.x, position.y)).magnitude < 64;
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
        UpdateTileVisualBulk(chunkCoord, _visibleChunks[chunkCoord]);

        var fullDelta = new SparseChunkDelta(
            _initialChunks[chunkCoord],
            _visibleChunks[chunkCoord]
        );
        
        if (!_chunkTrackers.ContainsKey(chunkCoord))
            _chunkTrackers[chunkCoord] = new List<NetworkConnectionToClient>();
        
        var trackers = _chunkTrackers[chunkCoord];
        var toRemove = new List<NetworkConnectionToClient>();

        foreach (var tracker in trackers)
        {
            if (!IsChunkRelevantFor(chunkCoord, tracker.identity.gameObject.transform.position))
            {
                toRemove.Add(tracker);
                continue;
            }

            TargetReceiveChunkDelta(tracker, chunkCoord, fullDelta);
        }

        foreach (var tracker in toRemove)
        {
            trackers.Remove(tracker);
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
        clientHasFinishedApplying = true;
        
        if (isServer && Application.isBatchMode)
        {
            GenerateServerCollisions(chunkCoord, updatedChunk);
        }
    }
    
    private Dictionary<Vector2Int, GameObject> _chunkColliders = new();

    private void GenerateServerCollisions(Vector2Int chunkCoord, ChunkData updatedChunk)
    {
        if (_chunkColliders.TryGetValue(chunkCoord, out GameObject oldColliderObj))
        {
            Destroy(oldColliderObj);
        }

        GameObject colliderObj = new GameObject($"ChunkCollider_{chunkCoord.x}_{chunkCoord.y}");
        colliderObj.transform.parent = playerGrid.transform;
        
        for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
        {
            BlockID block = updatedChunk[i];
            if (!block.IsAir)
            {
                var coords = ChunkUtils.ChunkCellCoordinates(i);
                var box = colliderObj.AddComponent<BoxCollider2D>();
                box.offset = new Vector2(
                    chunkCoord.x * ChunkUtils.ChunkSize + coords.x + 0.5f,
                    chunkCoord.y * ChunkUtils.ChunkSize + coords.y + 0.5f
                );
                box.size = Vector2.one;
            }
        }
        
        _chunkColliders[chunkCoord] = colliderObj;
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
        MapGenerator generator = new MapGenerator();
        Dictionary<Vector2Int, ChunkData> chunks = generator.GenerateMapChunks();
        
        _initialChunks = new Dictionary<Vector2Int, ChunkData>(chunks);;
        _visibleChunks = new Dictionary<Vector2Int, ChunkData>(chunks);;
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        if (NetworkServer.active)
        {
            for (int x = 0; x < WorldSize.x; x++)
            {
                for (int y = 0; y < WorldSize.y; y++)
                {
                    Vector2Int chunkCoords = new Vector2Int(x, y);
                
                    UpdateTileVisualBulk(chunkCoords, _visibleChunks[chunkCoords]);
                }
            }
        }
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

    public Vector3 GetCastPositionFromChunk(Vector2Int chunkCoordinates)
    {
        var x = (byte)(ChunkUtils.ChunkSize / 2 + 1);

        for (int yCoords = WorldSize.y - 1; yCoords >= 0; yCoords--)
        {
            chunkCoordinates.y = yCoords;
            
            for (byte y = ChunkUtils.ChunkSize - 1; y > 0; y--)
            {
                var block = _initialChunks[chunkCoordinates].Get(x, y);

                if (!block.IsAir)
                {
                    return ChunkUtils.WorldPositionOfBlock(chunkCoordinates, x, y) + Vector2.up;
                }
            }
        }
        
        chunkCoordinates.y = WorldSize.y - 1;
        Debug.LogError("No free spawn position found");
        return ChunkUtils.WorldPositionOfBlock(chunkCoordinates, x, 63);
    }

    [Server]
    public void SpawnDroppedItemDelayed(UnityEngine.GameObject prefab, Vector3 position, ItemStack itemStack)
    {
        StartCoroutine(SpawnDroppedItemCoroutine(prefab, position, itemStack));
    }

    private System.Collections.IEnumerator SpawnDroppedItemCoroutine(UnityEngine.GameObject prefab, Vector3 position, ItemStack itemStack)
    {
        yield return new WaitForFixedUpdate();
        var droppedItem = Instantiate(prefab, position, Quaternion.identity);
        NetworkServer.Spawn(droppedItem);
        droppedItem.GetComponent<DroppedItemData>().ServerSetItemStack(itemStack);
    }
}