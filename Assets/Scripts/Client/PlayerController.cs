using System.Collections.Generic;
using Mirror;
using Server;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class PlayerController : NetworkBehaviour
{
    // 1. REPLICATION (SyncVar): Automatically syncs from the Server to all Clients.
    // The "hook" function runs on clients whenever the server changes this value.
    
    public PlayerData playerData;
    
    private PlayerRenderer playerRenderer;
    
    private ChunkManager _chunkManager;

    void Start()
    {
        playerRenderer = GetComponent<PlayerRenderer>();
    }

    void Update()
    {
        // Safety check: We only want the player who OWNS this object to send inputs.
        if (!isLocalPlayer) return;
        if (!NetworkClient.ready) return;

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

        if (Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = FindObjectOfType<Tilemap>().WorldToCell(mouseWorldPos);
            
            // call server block break
            BlockWorldManager.Instance.CmdBreakBlock(cellPos.x, cellPos.y);
            // visualize
            // render what server said
        }
        
        if (Input.GetMouseButton(1))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = FindObjectOfType<Tilemap>().WorldToCell(mouseWorldPos);
            
            // call server block place
            BlockWorldManager.Instance.CmdPlaceBlock(cellPos.x, cellPos.y, 1);
            
            // visualize
            // render what server said
        }
        
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
