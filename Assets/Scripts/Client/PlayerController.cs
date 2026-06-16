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
    private float _velocityY = 0f;
    private bool _isGrounded = false;

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

        // When the local player presses C, ask the server to change the color.
        if (Input.GetKey(KeyCode.C))
        {
            CmdChangeColor();
        }
        
        float control = 0f;
        if (Input.GetKey(KeyCode.A)) control += -1f;
        if (Input.GetKey(KeyCode.D)) control += 1f;
        
        bool wantsToJump = Input.GetKeyDown(KeyCode.Space);

        if (_isGrounded)
        {
            //on the ground -> no gravity
            _velocityY = 0f;
            //jump -> impulse up
            if (wantsToJump)
            {
                _velocityY = playerData.jumpForce;
                _isGrounded = false;
            }
        }
        else
        {
            //gravity pulls down
            _velocityY += playerData.gravity * Time.deltaTime;
        }
        
        Vector3 movement = new Vector3(control * playerData.baseSpeed, _velocityY, 0f);
        
        GetComponent<Transform>().position += movement  * Time.deltaTime;
        
        //tmp floor
        if (transform.position.y <= 0f)
        {
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
            _isGrounded = true; 
        }
        
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
