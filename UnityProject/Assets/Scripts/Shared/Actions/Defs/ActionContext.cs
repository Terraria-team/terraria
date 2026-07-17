using UnityEngine;

public struct ActionContext
{
    public byte blockPositionX;
    public byte blockPositionY;
    public Vector2Int chunkPosition;
    public Vector2 mousePosition;
    public ItemID usedItemID;
    public Vector2 userPosition;
}

public static class ActionFiller
{
    public static ActionContext GetActionContext(ItemStack usedStack)
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -Camera.main.transform.position.z;

        Vector3 worldCoord = Camera.main.ScreenToWorldPoint(mousePos);
        worldCoord.z = 0f;

        Vector3Int cellPos = ChunkManager.Instance.playerGrid.WorldToCell(worldCoord);

        var context = new ActionContext
        {
            blockPositionX = (byte)(cellPos.x % ChunkUtils.ChunkSize),
            blockPositionY = (byte)(cellPos.y % ChunkUtils.ChunkSize),
            chunkPosition = new Vector2Int(
                cellPos.x / ChunkUtils.ChunkSize,
                cellPos.y / ChunkUtils.ChunkSize
            ),
            usedItemID = usedStack.ItemID,
            mousePosition = worldCoord
        };
        
        return context;
    }
}