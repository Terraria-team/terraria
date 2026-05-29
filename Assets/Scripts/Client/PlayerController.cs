using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    // 1. REPLICATION (SyncVar): Automatically syncs from the Server to all Clients.
    // The "hook" function runs on clients whenever the server changes this value.
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    [SerializeField] private Renderer playerRenderer;

    public GameObject chunkCube;
    
    private List<GameObject> _cubes = new(); 
    private List<Material> _materials = new();
    
    public PlayerData playerData;
    
    private Material _cachedMaterial;

    private ChunkData initial = new(new BlockID(1), false);

    void Start()
    {
        _cachedMaterial = playerRenderer.material;
        // Ensure the material reflects the current synced color right when spawned
        _cachedMaterial.SetColor("_BaseColor", playerColor);
        
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
    }

    void Update()
    {
        if (!isClient) return;
        
        for (ushort i = 0; i < ChunkUtils.ChunkSize * ChunkUtils.ChunkSize; i++)
        {
            _materials[i].SetColor("_BaseColor", initial[i].GetColor());
        }
        
        // Safety check: We only want the player who OWNS this object to send inputs.
        if (!isLocalPlayer) return;

        // When the local player presses Space, ask the server to change the color.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CmdChangeColor();
            
            CmdBroadcastChunkDelta();
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
    void CmdBroadcastChunkDelta()
    {
        ChunkData updated = new (new BlockID(1), true);
        
        SparseChunkDelta delta = new(initial, updated);
        
        RpcApplyChunkDelta(delta);
    }

    [ClientRpc]
    void RpcApplyChunkDelta(SparseChunkDelta delta)
    {
        delta.Apply(initial);
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
