using UnityEngine;

public struct ItemStack
{
    public int Count { get; }
    public readonly ItemID ItemID;

    public ItemStack(ItemID itemID)
    {
        ItemID = itemID;
        Count = 1;
    }

    public ItemStack(ItemID itemID, int count)
    {
        ItemID = itemID;

        if (count < 1)
        {
            count = 1;
            Debug.LogError($"Stack count cannot be non positive, got: {count}");
        }
        Count = count;
    }
}
