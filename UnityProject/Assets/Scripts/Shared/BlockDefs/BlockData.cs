using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "BlockData", menuName = "Scriptable Objects/BlockData")]
public class BlockData : ScriptableObject
{
    public ushort id;
    public string unlocalizedName;
    public TileBase blockTexture;
    public ItemData droppedItem;
}
