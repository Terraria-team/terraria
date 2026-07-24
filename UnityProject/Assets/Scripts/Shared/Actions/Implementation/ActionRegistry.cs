using System;
using Mirror;
using Shared.Components;
using UnityEngine;
using Object = UnityEngine.Object;

public enum ActionType
{
    None,
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
            case ActionType.None:
                return true;
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
                    var itemStack = new ItemStack(new ItemID(droppedItemData.id));
                    var pos = ChunkUtils.WorldPositionOfBlock(context.chunkPosition, context.blockPositionX, context.blockPositionY);
                    ChunkManager.Instance.SpawnDroppedItemDelayed(PrefabsSettings.droppedItemPrefab, pos, itemStack);
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
                
                float swingDamage = context.usedItemID.ItemData.swingData.swingDamage;
                float swingRange = context.usedItemID.ItemData.swingData.swingSize;

                var colliders = Physics2D.OverlapCircleAll(swingLocation, swingRange);
                foreach (var col in colliders)
                {
                    // Skip self
                    //if (col.transform.IsChildOf(context.) || col.gameObject == gameObject)
                    //    continue;

                    var health = col.GetComponent<HealthComponent>();
                    if (health != null)
                    {
                        health.ApplyDamageServerRpc(Mathf.Max(1, (int)swingDamage));
                    }
                }
                
                break;
            }
        }

        return true;
    }
}
