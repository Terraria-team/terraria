using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    public float baseSpeed = 15.0f;
}
