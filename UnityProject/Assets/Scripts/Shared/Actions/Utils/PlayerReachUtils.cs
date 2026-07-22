using UnityEngine;

public static class PlayerReachUtils
{
    private const float PlayerReachDistance = 7.0f;
    
    public static bool IsBlockChangeValid(ActionContext context)
    {
        if (context.blockPositionX >= ChunkUtils.ChunkSize || context.blockPositionY >= ChunkUtils.ChunkSize) 
            return false;
        
        Vector2 player2D = new Vector2(context.userPosition.x + 0.5f, context.userPosition.y + 0.5f);
        Vector2 block2D = ChunkUtils.WorldPositionOfBlock(context.chunkPosition, context.blockPositionX, context.blockPositionY);

        if (IsBlockObstructingAny(block2D)) 
            return false;
        
        return IsBlockInRange(player2D, block2D);
    }
    
    private static bool IsBlockInRange(Vector2 playerPos, Vector2 blockPos)
    {
        float distance = Vector2.Distance(playerPos, blockPos);
        if (distance > PlayerReachDistance)
            return false;

        return true;
    }
    
    private static bool IsBlockObstructingAny(Vector2 blockPos)
    {
        return false; // should check for all entities, not only player
    }

}
