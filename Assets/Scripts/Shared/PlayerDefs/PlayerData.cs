using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    public float baseSpeed = 15.0f;
    public float jumpForce = 12f;
    public float gravity = -25f;
}
