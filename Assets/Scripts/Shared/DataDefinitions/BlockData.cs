using UnityEngine;

[CreateAssetMenu(fileName = "BlockData", menuName = "Scriptable Objects/BlockData")]
public class BlockData : ScriptableObject
{
    public ushort id;
    public string name;
    public Sprite sprite;
}
