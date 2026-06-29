using System.Collections.Generic;
using Mirror;
using Shared.Components;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public PlayerData playerData;
    private PlayerRenderer _playerRenderer;
    private HealthComponent _healthComponent;
    
    private float _velocityY = 0f;
    private bool _isGrounded = false;

    private ChunkManager _chunkManager;

    void Start()
    {
        _chunkManager = ChunkManager.Instance;
        _playerRenderer = GetComponent<PlayerRenderer>();
        _healthComponent = GetComponent<HealthComponent>();
       
        if (!isLocalPlayer) return;

        SubscribeToChunks();
        _healthComponent.OnDamageFlashed += _playerRenderer.DamageFlash;
    }
    
    void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks
        if (_healthComponent != null)
        {
            _healthComponent.OnDamageFlashed -= _playerRenderer.DamageFlash;
        }
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
            Debug.Log("You choose air!");
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            place = 1;
            Debug.Log("You choose grass!");
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
            }
        }
        
        // When the local player presses Space, ask the server to change the color.
        if (Input.GetKeyDown(KeyCode.C))
        {
            _playerRenderer.ChangeColor();
        }
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            DoDamage(10);
        }
        
        float control = 0f;
        if (Input.GetKey(KeyCode.A))
        {
            control += -1f;
            _playerRenderer.ChangeDirection(true);
        }
        if (Input.GetKey(KeyCode.D))
        {
            control += 1f;
            _playerRenderer.ChangeDirection(false);
        }
        
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
        if (transform.position.y <= 41.3f)
        {
            transform.position = new Vector3(transform.position.x, 41.3f, transform.position.z);
            _isGrounded = true; 
        }
        
        //CmdAffectPos(control);
    }
    
    [Command]
    void CmdAffectPos(float pos)
    {
        
    }

    [ClientRpc]
    void RpcLogChange(string message)
    {
        Debug.Log($"[Server says]: {message}");
    }

    void DoDamage(int amount)
    {
        HealthComponent[] players = Object.FindObjectsByType<HealthComponent>();

        foreach (var player in  players)
        {
            if (player == _healthComponent) continue;
            
            player.ApplyDamageServerRpc(amount);
        }
    }
}
