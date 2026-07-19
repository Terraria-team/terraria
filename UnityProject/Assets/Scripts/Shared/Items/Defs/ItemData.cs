using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public int id;
    public Sprite icon;
    public string unlocalizedName;

    public int stackSize = 1;
    
    public ActionType primaryAction;

    [Space] 
    [Header("Specialized data")] 
    public BlockData blockToPlace;
    public int projectileToSpawn;
    public ActionSwingData swingData;
}
