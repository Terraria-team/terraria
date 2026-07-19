using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image itemSpriteDisplay;
    [SerializeField] private TextMeshProUGUI itemCountText;
    [SerializeField] private GameObject itemSelection;
    
    public void UpdateItem(NullableItemStack itemStack)
    {
        if (itemStack.HasValue)
        {
            itemSpriteDisplay.sprite = itemStack.ItemStack.ItemID.ItemData.icon;
            itemCountText.text = itemStack.ItemStack.Count.ToString();
        }
        else
        {
            itemSpriteDisplay.sprite = null;
            itemCountText.text = "";
        }
    }

    public void UpdateSelection(bool selected)
    {
        itemSelection.SetActive(selected);
    }
}
