using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BlockHighlight : MonoBehaviour
{
    private ChunkManager _chunkManager;
    
    private Tilemap mainTilemap;
    private Tilemap highlightTileMap;
    [SerializeField] private TileBase yellowBlock;
    [SerializeField] private TileBase redBlock;
    [SerializeField] private TileBase greenBlock;

    private Vector3Int highlightedTilePos;
    private bool hints = false;
    private ushort blockId = 0;
    
    public void InitializeHighlight()
    {
        _chunkManager = ChunkManager.Instance;
        
        GameObject fg = GameObject.Find("Foreground");
        if (fg == null) return;
    
        mainTilemap = fg.GetComponent<Tilemap>();
        
        Grid mainGrid = fg.GetComponentInParent<Grid>();
        GameObject tilemapGo = new GameObject("LocalHighlightTilemap");
        tilemapGo.transform.SetParent(mainGrid.transform);
        tilemapGo.transform.localPosition = mainTilemap.transform.localPosition; 
    
        highlightTileMap = tilemapGo.AddComponent<Tilemap>();
        TilemapRenderer tr = tilemapGo.AddComponent<TilemapRenderer>();
        tr.sortingLayerName = "Highlight";
        tr.sortingOrder = 0;
    }
    
    private void Update()
    {
        HighlightTile();
    }
    
    public void ChangeMode()
    {
        hints = !hints;
    }
    
    public void SetBlockId(ushort id)
    {
        blockId = id;
    }

    private Vector3Int GetMouseOnGridPosition()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int mouseCellPos = mainTilemap.WorldToCell(mousePos);
        mouseCellPos.z = 0;

        return mouseCellPos;

    }

    private void HighlightTile()
    {
        Vector3Int mouseCellPos = GetMouseOnGridPosition();
        
        if(highlightedTilePos != mouseCellPos)
        {
            highlightTileMap.SetTile(highlightedTilePos, null);

            if (hints)
            {
                if (!NetworkClient.active || NetworkClient.localPlayer == null) return;
                
                if (_chunkManager.IsValidChange((byte)mouseCellPos.x, (byte)mouseCellPos.y, new BlockID(blockId), NetworkClient.localPlayer))
                {
                    highlightTileMap.SetTile(mouseCellPos, greenBlock);
                }
                else
                {
                    highlightTileMap.SetTile(mouseCellPos, redBlock);
                }
            }
            else
            {
                highlightTileMap.SetTile(mouseCellPos, yellowBlock);
            }
            
            highlightedTilePos = mouseCellPos;
        }
    }

    private void OnDestroy()
    {
        if (highlightTileMap != null && gameObject.scene.isLoaded)
        {
            Destroy(highlightTileMap.gameObject);
        }
    }
}
