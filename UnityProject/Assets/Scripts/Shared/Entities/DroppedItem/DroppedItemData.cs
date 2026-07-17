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
}
