using System;
using Mirror;
using Shared.Components;
using UnityEngine;
using Object = UnityEngine.Object;

public enum ActionType
{
    BreakBlock,
    PlaceBlock,
    SpawnProjectile,
    Swing
}

public static class ActionRegistry
{
    private static ActionsPrefabsSettings PrefabsSettings => ActionsPrefabsSettings.Instance;
    public static bool ExecuteAction(ActionType action, ActionContext context)
    {
        switch (action)
        {
            case ActionType.BreakBlock:
            {
                if (!PlayerReachUtils.IsBlockChangeValid(context))
                    return false;
                
                var currentBlock = ChunkManager.Instance.GetChunkAt(
                    context.chunkPosition    
                ).Get(context.blockPositionX, context.blockPositionY);

                // Cannot mine air
                if (currentBlock.IsAir)
                    return false;

                var droppedItemData = currentBlock.BlockData.droppedItem;
                
                ChunkManager.Instance.Place(
                    context.chunkPosition,
                    context.blockPositionX, 
                    context.blockPositionY, 
                    BlockID.Air
                );

                if (droppedItemData != null)
                {
                    var droppedItem = Object.Instantiate(
                        PrefabsSettings.droppedItemPrefab,
                        ChunkUtils.WorldPositionOfBlock(context.chunkPosition, context.blockPositionX, context.blockPositionY),
                        Quaternion.identity
                    );
                    NetworkServer.Spawn(droppedItem);
                
                    droppedItem.GetComponent<DroppedItemData>().ServerSetItemStack(new ItemStack(
                        new ItemID(droppedItemData.id)    
                    ));
                }
                
                
                break;
            }

            case ActionType.PlaceBlock:
            {
                // TODO select chunk
                if (!PlayerReachUtils.IsBlockChangeValid(context))
                    return false;
                
                var currentBlock = ChunkManager.Instance.GetChunkAt(
                    context.chunkPosition    
                ).Get(context.blockPositionX, context.blockPositionY);
                
                if (!currentBlock.IsAir)
                    return false;
                
                ChunkManager.Instance.Place(
                    context.chunkPosition,
                    context.blockPositionX, 
                    context.blockPositionY, 
                    context.placedBlockID
                );
                break;
            }

            case ActionType.SpawnProjectile:
            {
                Vector2 projectileDirection = context.mousePosition - context.userPosition;
                projectileDirection.Normalize();
                
                Vector2 projectileLocation = context.userPosition + projectileDirection * 1.5f;
                
                var projectile = Object.Instantiate(
                    PrefabsSettings.projectilePrefab,
                    projectileLocation,
                    Quaternion.identity
                );
                NetworkServer.Spawn(projectile);
                
                //swingObject.transform.localScale = new Vector3(swingData.swingSize, swingData.swingSize, swingData.swingSize);
                //swingObject.GetComponent<SpriteAnimator>().Play(swingData.swingSprites);

                projectile.GetComponent<Rigidbody2D>().AddForce(projectileDirection * 10, ForceMode2D.Impulse);
                
                break;
            }

            case ActionType.Swing:
            {
                var swingData = context.usedItemID.ItemData.swingData;
                
                Vector2 swingDirection = context.mousePosition - context.userPosition;
                
                const float MAX_SWING_DISTANCE = 50f; // TODO move to swing data
                
                float swingDistance = Math.Min(swingDirection.magnitude, MAX_SWING_DISTANCE);
                swingDirection.Normalize();
                
                Vector2 swingLocation = context.userPosition + swingDirection * swingDistance;
                
                var swingObject = Object.Instantiate(
                    PrefabsSettings.swingPrefab,
                    swingLocation,
                    Quaternion.identity
                );
                
                swingObject.transform.localScale = new Vector3(swingData.swingSize, swingData.swingSize, swingData.swingSize);
                //swingObject.GetComponent<SpriteAnimator>().Play(swingData.swingSprites);
                NetworkServer.Spawn(swingObject);
                
                break;
            }
        }

        return true;
    }
}
