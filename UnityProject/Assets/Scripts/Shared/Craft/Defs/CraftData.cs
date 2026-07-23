using UnityEngine;

[CreateAssetMenu(fileName = "CraftData", menuName = "Scriptable Objects/CraftData")]
public class CraftData : ScriptableObject
{
    public bool requiresStation;
    
    public ItemData[] ingredients;
    public int[] ingredientAmounts =
    {
        1,
        1,
        1,
        1,
        1
    };

    public ItemData result;
    public int resultAmount = 1;

    public int generatedID;
}
