using UnityEngine;

[CreateAssetMenu(fileName = "BlockData", menuName = "Scriptable Objects/BlockData")]
public class BlockData : ScriptableObject
{
    public ushort id;
    public string unlocalizedName;
    public Sprite sprite;
}
