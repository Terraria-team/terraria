using Mirror;
using UnityEngine;

public class NetworkSpriteRenderer : NetworkBehaviour
{
    [Server]
    public void CmdSetSprite(Sprite sprite)
    {
        RpcSetSprite(sprite);
    }

    [ClientRpc]
    void RpcSetSprite(Sprite sprite)
    {
        GetComponent<SpriteRenderer>().sprite = sprite;
    }
}
