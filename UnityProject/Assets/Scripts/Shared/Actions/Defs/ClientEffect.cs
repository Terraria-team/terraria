using UnityEngine;

[CreateAssetMenu(fileName = "ClientEffect", menuName = "Scriptable Objects/ClientEffect")]
public abstract class ClientEffect : ScriptableObject
{
    public abstract void ApplyEffect(ActionContext context);
}
