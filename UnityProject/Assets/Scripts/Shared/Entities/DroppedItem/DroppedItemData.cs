using System;
using Mirror;
using UnityEngine;

public class DroppedItemData : NetworkBehaviour
{
    public ItemStack ItemStack { get; private set; }

    [Server]
    public void ServerSetItemStack(ItemStack itemStack)
    {
        ItemStack = itemStack;
        
        RpcSetItemStack(itemStack);
    }

    [ClientRpc]
    public void RpcSetItemStack(ItemStack itemStack)
    {
        GetComponent<SpriteRenderer>().sprite = itemStack.ItemID.ItemData.icon;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isServer)
        {
            var inventoryComponent = other.gameObject.GetComponentInParent<InventoryComponent>();

            if (inventoryComponent == null)
                return;
                
            while (ItemStack.Count > 0)
            {
                int canAdd = inventoryComponent.HowMuchCanAddOf(ItemStack);
                
                if (canAdd == 0)
                    return;

                ItemStack = ItemStack.Decremented();
                inventoryComponent.AddItem(ItemStack.ItemID);
            }
            
            Destroy(gameObject);
        }
    }
}
