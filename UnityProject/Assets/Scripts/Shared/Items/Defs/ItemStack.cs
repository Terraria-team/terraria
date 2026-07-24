using UnityEngine;

public struct ItemStack
{
    public int Count;
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
    
    public ItemStack Incremented()
    {
        Count++;
        return this;
    }

    public ItemStack Decremented()
    {
        Count--;
        return this;
    }
    
    public bool IsFilled => Count == ItemID.ItemData.stackSize;
}

public struct NullableItemStack
{
    public readonly bool HasValue;
    public readonly ItemStack ItemStack;
    
    public NullableItemStack(ItemStack itemStack)
    {
        ItemStack = itemStack;
        HasValue = true;
    }
    
    public static implicit operator NullableItemStack(ItemStack itemStack)
    {
        return new NullableItemStack(itemStack);
    }
}