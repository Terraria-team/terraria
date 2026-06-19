using System.Collections.Generic;
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

    private ChunkManager _chunkManager;

    void Start()
    {
        _chunkManager = ChunkManager.Instance;
        _cachedMaterial = playerRenderer.material;
       
        if (!isLocalPlayer) return;
        
        SubscribeToChunks();
    }

    private List<NetworkConnectionToClient> _chunkListeners = new();
    
    [Command]
    void SubscribeToChunks()
    {
        _chunkListeners.Add(connectionToClient);
    }

    private int? place = null;
    void Update()
    {
        // Safety check: We only want the player who OWNS this object to send inputs.
        if (!isLocalPlayer) return;
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            place = 0;
            Debug.Log($"Choose AIR");
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            place = 1;
            Debug.Log($"Choose Grass");
        }

        if (Input.GetMouseButton(0) && place != null)
        {
            Vector3 mousePos = Input.mousePosition;
            // Ensure ScreenToWorldPoint works correctly by providing distance from camera
            mousePos.z = -Camera.main.transform.position.z; 
            
            Vector3 worldCoord = Camera.main.ScreenToWorldPoint(mousePos);
            worldCoord.z = 0f;
            
            Vector3Int cellPos = _chunkManager.playerGrid.WorldToCell(worldCoord);
            // _chunkManager.UpdateTileVisual(cellPos.x, cellPos.y, (int)place);
            
            // Only place blocks if we are clicking INSIDE the chunk boundaries (0 to 63)
            if (cellPos.x >= 0 && cellPos.x < ChunkUtils.ChunkSize && 
                cellPos.y >= 0 && cellPos.y < ChunkUtils.ChunkSize)
            {
                // Immediate local visual feedback
                _chunkManager.UpdateTileVisual((byte)cellPos.x, (byte)cellPos.y, new BlockID((ushort)place.Value));
                
                _chunkManager.Place((byte)cellPos.x, (byte)cellPos.y, new BlockID((ushort)place.Value));
                Debug.Log($"Clicked World: {worldCoord} -> Grid Cell: {cellPos}. Sending Place({cellPos.x}, {cellPos.y}, {place.Value})");
            }
        }
        
        // When the local player presses Space, ask the server to change the color.
        if (Input.GetKeyDown(KeyCode.C))
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
