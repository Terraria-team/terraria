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

    public GameObject chunkCube;
    
    private List<Material> _materials = new();
    
    public PlayerData playerData;
    
    private Material _cachedMaterial;
    private float _velocityY = 0f;
    private bool _isGrounded = false;

    private ChunkManager _chunkManager;

    void Start()
    {
        _chunkManager = ChunkManager.Instance;
        _cachedMaterial = playerRenderer.material;
        // Ensure the material reflects the current synced color right when spawned
        _cachedMaterial.SetColor("_BaseColor", playerColor);

        if (!isLocalPlayer) return;
        
        for (int x = 0; x < ChunkUtils.ChunkSize; x++)
        {
            for (int y = 0; y < ChunkUtils.ChunkSize; y++)
            {
                float scale = 0.1f;
                
                var newOne = Instantiate(chunkCube, new Vector3(x*1*scale, y*1*scale, 0), Quaternion.identity);
                
                newOne.transform.localScale = new Vector3(scale, scale, scale);
                
                _materials.Add(newOne.GetComponent<Renderer>().material);
            }
        }

        //SubscribeToChunks();
    }

    private List<NetworkConnectionToClient> _chunkListeners = new();
    
    [Command]
    void SubscribeToChunks()
    {
        _chunkListeners.Add(connectionToClient);
    }

    void Update()
    {
        // Safety check: We only want the player who OWNS this object to send inputs.
        if (!isLocalPlayer) return;

        for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
        {
            _materials[i].SetColor("_BaseColor", _chunkManager._localView[i].GetColor());
        }

        int place = -1;
        
        if (Input.GetKey(KeyCode.Alpha1))
        {
            place = 0;
        }
        
        if (Input.GetKey(KeyCode.Alpha2))
        {
            place = 1;
        }

        if (place != -1)
        {
            var coord = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
            float scale = 0.1f;

            var x = (int)(coord.x / scale);
            var y = (int)(coord.y / scale);
            
            x = Mathf.Clamp(x, 0, ChunkUtils.ChunkSize - 1);
            y = Mathf.Clamp(y, 0, ChunkUtils.ChunkSize - 1);
            
            _chunkManager.Place((byte)x, (byte)y, new BlockID((ushort)place));
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
