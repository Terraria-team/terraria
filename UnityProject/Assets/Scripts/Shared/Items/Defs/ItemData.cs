using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public int id;
    public Sprite icon;
    public string unlocalizedName;

    public ActionType primaryAction;

    [Space] 
    [Header("Specialized data")] 
    public ushort blockToPlace;
    public int projectileToSpawn;
    public ActionSwingData swingData;
}
