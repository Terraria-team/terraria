using System;
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
                
                ChunkManager.Instance.Place(
                    context.blockPositionX, 
                    context.blockPositionY, 
                    new BlockID(0)
                );
                break;
            }

            case ActionType.PlaceBlock:
            {
                // TODO select chunk
                
                ChunkManager.Instance.Place(
                    context.blockPositionX, 
                    context.blockPositionY, 
                    new BlockID(1)
                );
                break;
            }

            case ActionType.SpawnProjectile:
            {
                /*GameObject.Instantiate(
                    
                );*/
                
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
                swingObject.GetComponent<SpriteAnimator>().Play(swingData.swingSprites);
                
                break;
            }
        }
    }
}
