using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private GameObject uiPrefab;
    
    public PlayerData playerData;
    private PlayerRenderer _playerRenderer;
    
    private float _velocityY = 0f;
    private bool _isGrounded = false;

    private ChunkManager _chunkManager;
    
    private InventoryComponent _inventory;

    void Start()
    {
        _chunkManager = ChunkManager.Instance;
        _playerRenderer = GetComponent<PlayerRenderer>();
        _inventory = GetComponent<InventoryComponent>();
       
        if (!isLocalPlayer) return;
        
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0, -10);
        
        Instantiate(uiPrefab);
        
        SubscribeToChunks();
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
        
        TrySwitchingInventorySlot();

        if (Input.GetMouseButtonDown(0))
        {
            _inventory.UseSelectedItem();
        }
        
        // When the local player presses Space, ask the server to change the color.
        if (Input.GetKeyDown(KeyCode.C))
        {
            _playerRenderer.ChangeColor();
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

    void TrySwitchingInventorySlot()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _inventory.ChangeSelection(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _inventory.ChangeSelection(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _inventory.ChangeSelection(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            _inventory.ChangeSelection(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            _inventory.ChangeSelection(4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            _inventory.ChangeSelection(5);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            _inventory.ChangeSelection(6);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            _inventory.ChangeSelection(7);
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            _inventory.ChangeSelection(8);
        }
    }
}
