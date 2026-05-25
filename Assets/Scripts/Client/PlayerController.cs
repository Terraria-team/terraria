using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : NetworkBehaviour
{
    // 1. REPLICATION (SyncVar): Automatically syncs from the Server to all Clients.
    // The "hook" function runs on clients whenever the server changes this value.
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    [SerializeField] private Renderer playerRenderer;
    
    public PlayerData playerData;
    
    private Material _cachedMaterial;

    void Start()
    {
        _cachedMaterial = playerRenderer.material;
        // Ensure the material reflects the current synced color right when spawned
        _cachedMaterial.SetColor("Color", playerColor);
    }

    void Update()
    {
        // Safety check: We only want the player who OWNS this object to send inputs.
        if (!isLocalPlayer) return;

        // When the local player presses Space, ask the server to change the color.
        if (Input.GetKey(KeyCode.Space))
        {
            CmdChangeColor();
        }

        float control = 0f;
        
        if (Input.GetKey(KeyCode.S))
        {
            control += -1f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            control += 1f;
        }
        
        GetComponent<Transform>().position += Vector3.up * (control * playerData.baseSpeed * Time.deltaTime);
        
        //CmdAffectPos(control);
    }

    [Command]
    void CmdAffectPos(float pos)
    {
        
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
        if (_cachedMaterial != null)
        {
            _cachedMaterial.SetColor("_BaseColor", playerColor);
        }
    }
}
