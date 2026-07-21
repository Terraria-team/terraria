using UnityEngine;

public struct ActionContext
{
    public byte blockPositionX;
    public byte blockPositionY;
    public Vector2Int chunkPosition;
    public BlockID placedBlockID;
    public Vector2 mousePosition;
    public ItemID usedItemID;
    public Vector2 userPosition;
}

public static class ActionFiller
{
    public static ActionContext GetStrippedClientContext(ItemStack usedStack)
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
            mousePosition = worldCoord
        };
        
        return context;
    }

    public static ActionContext ExpandClientContext(ActionContext clientContext, ItemStack usedStack, Vector3 userPosition)
    {
        clientContext.userPosition = userPosition;
        clientContext.usedItemID = usedStack.ItemID;
        var blockToPlace = usedStack.ItemID.ItemData.blockToPlace;
        clientContext.placedBlockID = new BlockID(blockToPlace != null ? blockToPlace.id : (ushort)0);

        return clientContext;
    }

    public static ActionContext GetFullClientContext(ItemStack usedStack, Vector3 userPosition)
    {
        var strippedContext = GetStrippedClientContext(usedStack);
        
        return ExpandClientContext(strippedContext, usedStack, userPosition);
    }
}