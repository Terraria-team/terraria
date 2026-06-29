using System.Threading.Tasks;
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
    [SerializeField] private int damageFlashTime = 15;
    
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
        playerFacingLeft = newDirection;
    }
    
    [ClientRpc]
    void RpcLogChange(string message)
    {
        Debug.Log($"[Server says]: {message}");
    }

    [Command]
    public async void DamageFlash()
    {
        Color curColor = playerColor;
        playerColor = Color.red;

        // wait for damageFlashTime ms
        await Task.Delay(damageFlashTime); 

        // Safety check: Ensure the object hasn't been destroyed while we were waiting
        if (this != null && playerRenderer != null) 
        {
            playerColor= curColor;
        }
    }
    
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
