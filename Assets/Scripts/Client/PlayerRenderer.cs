using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerRenderer : NetworkBehaviour
{
    // 1. REPLICATION (SyncVar): Automatically syncs from the Server to all Clients.
    // The "hook" function runs on clients whenever the server changes this value.
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    [SyncVar(hook = nameof(OnRotationChanged))]
    public bool playerFacingLeft = false;
    
    [SerializeField] private SpriteRenderer playerRenderer;
    
    public PlayerData playerData;
    
    void Start()
    {
        if (playerRenderer != null) 
            playerRenderer.color = playerColor;
    }

    void Update()
    {
        
    }

    public void ChangeDirection(bool left)
    {
        if (left != playerFacingLeft)
        {
            CmdChangeDirection(left);
        }
    }
    
    public void ChangeColor()
    {
        CmdChangeColor();
    }
    
    // 2. COMMAND: Called by a Client, but executed ONLY on the Server.
    // Method names must start with "Cmd".
    [Command]
    void CmdChangeColor()
    {
        // The server generates a random color and updates the SyncVar.
        // Because it's a SyncVar, this automatically pushes the new color to all clients.
        playerColor = new Color(Random.value, Random.value, Random.value);

        // The server also triggers an RPC to send a message to everyone.
        RpcLogChange("A player changed their color!");
    }
    
    [Command]
    void CmdChangeDirection(bool newDirection)
    {
        // The server generates a random color and updates the SyncVar.
        // Because it's a SyncVar, this automatically pushes the new color to all clients.
        playerFacingLeft = newDirection;

        // The server also triggers an RPC to send a message to everyone.
        RpcLogChange("A player changed their direction!");
    }

    // 3. CLIENT RPC: Called by the Server, but executed on ALL Clients.
    // Method names must start with "Rpc".
    [ClientRpc]
    void RpcLogChange(string message)
    {
        Debug.Log($"[Server says]: {message}");
    }

    // 4. THE HOOK: The local function triggered by the SyncVar changing.
    void OnColorChanged(Color oldColor, Color newColor)
    {
        playerRenderer.color = newColor;
    }
    
    void OnRotationChanged(bool oldRotation, bool newRotation)
    {
        if (playerRenderer != null)
        {
            playerRenderer.flipY = newRotation;
        }
    }
}
