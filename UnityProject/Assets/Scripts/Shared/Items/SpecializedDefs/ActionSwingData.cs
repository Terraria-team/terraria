using UnityEngine;

[CreateAssetMenu(fileName = "ActionSwingData", menuName = "Scriptable Objects/ActionSwingData")]
public class ActionSwingData : ScriptableObject
{
    public float swingDamage;
    public float swingSize;
    public Sprite[] swingSprites;
}
