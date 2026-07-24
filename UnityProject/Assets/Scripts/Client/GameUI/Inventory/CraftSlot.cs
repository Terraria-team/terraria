using UnityEngine;
using UnityEngine.UI;

public class CraftSlot : MonoBehaviour
{
    [SerializeField] private Image itemSpriteDisplay;
    private int _craftID;
    
    public void SetItem(CraftData craftData)
    {
        _craftID = craftData.generatedID;
        itemSpriteDisplay.gameObject.SetActive(true);
        itemSpriteDisplay.sprite = craftData.result.icon;
    }

    public void OnClick()
    {
        InventoryComponent.ClientOnlyInstance.TryCrafting(_craftID);
    }
}
