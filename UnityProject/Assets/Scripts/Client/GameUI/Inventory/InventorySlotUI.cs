using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image itemSpriteDisplay;
    [SerializeField] private TextMeshProUGUI itemCountText;
    [SerializeField] private GameObject itemSelection;
    
    public void UpdateItem(ItemStack? itemStack)
    {
        if (itemStack is { } stack)
        {
            itemSpriteDisplay.sprite = stack.ItemID.ItemData.icon;
            itemCountText.text = stack.Count.ToString();
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
