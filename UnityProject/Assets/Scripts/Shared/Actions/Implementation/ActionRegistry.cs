using System;
using System.Collections.Generic;
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
    private static ActionsSettings Settings => ActionsSettings.Instance;
    public static void ExecuteAction(ActionType action, ActionContext context)
    {
        switch (action)
        {
            case ActionType.BreakBlock:
            {
                // TODO select chunk
                
                var currentBlock = ChunkManager.Instance.GetChunkAt(
                    context.chunkPosition    
                ).Get(context.blockPositionX, context.blockPositionY);

                // Cannot mine air
                if (currentBlock.IsAir)
                    break;

                var droppedItemData = currentBlock.BlockData.droppedItem;
                
                ChunkManager.Instance.Place(
                    context.chunkPosition,
                    context.blockPositionX, 
                    context.blockPositionY, 
                    new BlockID(0)
                );

                if (droppedItemData != null)
                {
                    var droppedItem = Object.Instantiate(
                        Settings.DroppedItemPrefab,
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
                
                ChunkManager.Instance.Place(
                    context.chunkPosition,
                    context.blockPositionX, 
                    context.blockPositionY, 
                    new BlockID(1)
                );
                break;
            }

            case ActionType.SpawnProjectile:
            {
                Vector2 projectileDirection = context.mousePosition - context.userPosition;
                projectileDirection.Normalize();
                
                Vector2 projectileLocation = context.userPosition + projectileDirection * 1.5f;
                
                var projectile = Object.Instantiate(
                    Settings.ProjectilePrefab,
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
                    Settings.SwingPrefab,
                    swingLocation,
                    Quaternion.identity
                );
                
                swingObject.transform.localScale = new Vector3(swingData.swingSize, swingData.swingSize, swingData.swingSize);
                //swingObject.GetComponent<SpriteAnimator>().Play(swingData.swingSprites);
                NetworkServer.Spawn(swingObject);
                
                // Scan for damage
                var colliders = Physics2D.OverlapCircleAll(swingLocation, 1);

                Debug.Log(swingLocation);
                
                foreach (var collider in colliders)
                {
                    Debug.Log(collider.gameObject.name);
                    if (collider.TryGetComponent<HealthComponent>(out var component))
                    {
                        component.ApplyDamageServerRpc(10); 
                    }
                }
                
                break;
            }
        }
    }
}
