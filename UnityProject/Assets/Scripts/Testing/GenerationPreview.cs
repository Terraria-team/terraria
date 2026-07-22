using System.Collections.Generic;
using Core.WorldGeneration;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class GenerationPreview : MonoBehaviour
{
    [SerializeField] WorldGenerationConfig worldConfig;
    [SerializeField] Tilemap playerGrid;
    
    public void Generate()
    {
        playerGrid.ClearAllTiles();
        
        MapGenerator generator = new MapGenerator();
        Dictionary<Vector2Int, ChunkData> chunks = generator.GenerateMapChunks();

        foreach (var (coord, data) in chunks)
        {
            UpdateTileVisualBulk(coord, data);
        }
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
    
    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (playerGrid == null)
            return;

        DataManager.Initialize();
        
        for (int x = 0; x < worldConfig.Width; x++)
        {
            for (int y = 0; y < worldConfig.Height; y++)
            {
                Vector2Int chunkCoord = new Vector2Int(x, y);
                
                Vector3Int cell = playerGrid.WorldToCell(transform.position);
                Vector3 world = playerGrid.CellToWorld(cell);

                Gizmos.color = MapGenerator.GetBiomeTypeAt(chunkCoord).GenerationData().associatedColor;
                
                Gizmos.DrawWireCube(
                    ChunkUtils.WorldPositionOfChunkCenter(chunkCoord),
                    new Vector3(ChunkUtils.ChunkSize, ChunkUtils.ChunkSize, ChunkUtils.ChunkSize)
                );
                
                Gizmos.color = Color.white;
            }
        }

        Vector3Int cellPos = playerGrid.WorldToCell(transform.position);

        Vector3 worldPos = playerGrid.CellToWorld(cellPos);

        Handles.Label(
            worldPos + Vector3.up,
            $"Cell: {cellPos}"
        );
#endif
    }
}
