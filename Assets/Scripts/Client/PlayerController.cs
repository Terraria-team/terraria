using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : NetworkBehaviour
{
    // 1. REPLICATION (SyncVar): Automatically syncs from the Server to all Clients.
    // The "hook" function runs on clients whenever the server changes this value.
    
    public PlayerData playerData;
    
    private PlayerRenderer playerRenderer;


    void Start()
    {
        playerRenderer = GetComponent<PlayerRenderer>();
    }

    void Update()
    {
        // Safety check: We only want the player who OWNS this object to send inputs.
        if (!isLocalPlayer) return;

        float control = 0f;

        if (Input.GetKeyDown(KeyCode.C))
        {
            playerRenderer.ChangeColor();
        }
        
        if (Input.GetKey(KeyCode.A))
        {
            control += -1f;
            playerRenderer.ChangeDirection(true);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            playerRenderer.ChangeDirection(true);
        }
        if (Input.GetKey(KeyCode.D))
        {
            control += 1f;
            playerRenderer.ChangeDirection(false);
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            playerRenderer.ChangeDirection(false);
        }
        
        transform.position += Vector3.right * (control * playerData.baseSpeed * Time.deltaTime);
        
    }
    
    // 2. COMMAND: Called by a Client, but executed ONLY on the Server.
    // Method names must start with "Cmd".
    
    // 3. CLIENT RPC: Called by the Server, but executed on ALL Clients.
    // Method names must start with "Rpc".
    [ClientRpc]
    void RpcLogChange(string message)
    {
        Debug.Log($"[Server says]: {message}");
    }

    // 4. THE HOOK: The local function triggered by the SyncVar changing.
   
}
