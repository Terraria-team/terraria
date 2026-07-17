using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    public float baseSpeed = 10.0f;
    public float jumpForce = 8f;
    public float gravity = -25f;
}
