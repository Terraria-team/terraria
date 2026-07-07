using System.Collections.Generic;
using Mirror;
using Shared.Components;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI healthBar;
    [SerializeField] private GameObject uiPrefab;
    
    public static Transform LocalPlayerTransform;

    public PlayerData playerData;
    private PlayerRenderer _playerRenderer;
    private HealthComponent _healthComponent;
    
    private bool _isGrounded = false;
    private float _horizontalInput = 0f; 

    private ChunkManager _chunkManager;
    
    private InventoryComponent _inventory;
    private BlockHighlight _blockHighlight;
    private Rigidbody2D _rb;
    private Collider2D _collider;
    private bool _hasSpawnedOnSurface = false;

    private float _jumpBufferCounter = 0f;
    private float _coyoteTimeCounter = 0f;
    private float _jumpCooldownTimer = 0f;

    private List<NetworkConnectionToClient> _chunkListeners = new();

    void Start()
    {
        _chunkManager = ChunkManager.Instance;
        _playerRenderer = GetComponent<PlayerRenderer>();
        _inventory = GetComponent<InventoryComponent>();
        _blockHighlight = FindObjectOfType<BlockHighlight>();
        _healthComponent = GetComponent<HealthComponent>();
       
        if (!isLocalPlayer) return;
        
        _rb = GetComponent<Rigidbody2D>();
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _collider = GetComponent<Collider2D>();

        Debug.Log($"[PlayerController] Start. isLocalPlayer={isLocalPlayer}, position={transform.position}, ChunkManagerInstance={(ChunkManager.Instance != null ? "OK" : "NULL")}");

        // Start as Kinematic to prevent falling before the map is generated/drawn.
        _rb.bodyType = RigidbodyType2D.Kinematic;

        if (!isLocalPlayer)
        {
            return;
        }
        
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0, -10);
        
        Instantiate(uiPrefab);
        
        LocalPlayerTransform = transform;
        
        SubscribeToChunks();
        _healthComponent.OnDamageFlashed += _playerRenderer.DamageFlash;
        _healthComponent.OnHealingFlashed += _playerRenderer.HealingFlash;
    }
    
    void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks
        if (_healthComponent != null)
        {
            _healthComponent.OnDamageFlashed -= _playerRenderer.DamageFlash;
            _healthComponent.OnHealingFlashed -= _playerRenderer.HealingFlash;
        }
    }

    private void RepositionToSurface()
    {
        if (_chunkManager == null || _chunkManager.playerGrid == null)
        {
            Debug.LogWarning("[PlayerController] RepositionToSurface: ChunkManager or playerGrid is null!");
            return;
        }

        Vector3 originalPos = transform.position;
        int spawnX = Mathf.Clamp(Mathf.RoundToInt(originalPos.x), 5, ChunkUtils.ChunkSize - 6);
        int spawnY = Mathf.Clamp(Mathf.RoundToInt(originalPos.y), 5, ChunkUtils.ChunkSize - 6);

        Vector3Int feetTile = new Vector3Int(spawnX, spawnY, 0);
        Vector3Int headTile = new Vector3Int(spawnX, spawnY + 1, 0);

        if (!_chunkManager.playerGrid.HasTile(feetTile) && !_chunkManager.playerGrid.HasTile(headTile))
        {
            transform.position = new Vector3(spawnX + 0.5f, originalPos.y, originalPos.z);
            Debug.Log($"[PlayerController] Spawn position {transform.position} is already empty. No relocation needed.");
            return;
        }

        for (int offset = 1; offset < ChunkUtils.ChunkSize; offset++)
        {
            int checkUpY = spawnY + offset;
            if (checkUpY < ChunkUtils.ChunkSize - 2)
            {
                if (!_chunkManager.playerGrid.HasTile(new Vector3Int(spawnX, checkUpY, 0)) &&
                    !_chunkManager.playerGrid.HasTile(new Vector3Int(spawnX, checkUpY + 1, 0)))
                {
                    transform.position = new Vector3(spawnX + 0.5f, checkUpY + 0.05f, originalPos.z);
                    Debug.Log($"[PlayerController] Spawn position occupied. Relocated UPwards to {transform.position}");
                    return;
                }
            }

            int checkDownY = spawnY - offset;
            if (checkDownY > 1)
            {
                if (!_chunkManager.playerGrid.HasTile(new Vector3Int(spawnX, checkDownY, 0)) &&
                    !_chunkManager.playerGrid.HasTile(new Vector3Int(spawnX, checkDownY + 1, 0)))
                {
                    transform.position = new Vector3(spawnX + 0.5f, checkDownY + 0.05f, originalPos.z);
                    Debug.Log($"[PlayerController] Spawn position occupied. Relocated DOWNwards to {transform.position}");
                    return;
                }
            }
        }

        transform.position = new Vector3(spawnX + 0.5f, ChunkUtils.ChunkSize - 2, originalPos.z);
        Debug.LogWarning($"[PlayerController] Failed to find any empty spawn space. Fallback to top: {transform.position}");
    }

    private bool CheckGrounded()
    {
        if (_collider == null) return false;
        
        Bounds bounds = _collider.bounds;
        
        Vector2 size = new Vector2(bounds.size.x * 0.9f, 0.06f);
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y - 0.03f);
        
        Collider2D[] results = new Collider2D[5];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        
        int hitCount = Physics2D.OverlapBox(origin, size, 0f, filter, results);
        for (int i = 0; i < hitCount; i++)
        {
            if (results[i] != null && !results[i].transform.IsChildOf(transform) && results[i].gameObject != gameObject)
            {
                return true;
            }
        }
        return false;
    }

    [Command]
    void SubscribeToChunks()
    {
        _chunkListeners.Add(connectionToClient);
    }

    void Update()
    {
        if (healthBar != null && _healthComponent != null)
        {
            healthBar.text = _healthComponent.HealthNow.ToString();
        }
        
        if (!isLocalPlayer) return;
        
        TrySwitchingInventorySlot();

        if (!_hasSpawnedOnSurface)
        {
            if (_chunkManager != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic; // Enable physics now that the map is ready
                RepositionToSurface();
                _hasSpawnedOnSurface = true;
            }
            else
            {
                return;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            _inventory.UseSelectedItem();
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            _playerRenderer.ChangeColor();
        }
        
        _horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A))
        {
            _horizontalInput += -1f;
            _playerRenderer.ChangeDirection(true);
        }
        if (Input.GetKey(KeyCode.D))
        {
            _horizontalInput += 1f;
            _playerRenderer.ChangeDirection(false);
        }
        
        _jumpCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _jumpBufferCounter = 0.15f; 
        }
        else
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer || !_hasSpawnedOnSurface) return;

        _isGrounded = CheckGrounded() && _jumpCooldownTimer <= 0f;

        //store the current Y velocity that the Unity engine calculated from its gravity
        float currentVelocityY = _rb.linearVelocity.y;

        if (_isGrounded)
        {
            _coyoteTimeCounter = 0.1f; 
        }
        else
        {
            _coyoteTimeCounter -= Time.fixedDeltaTime;
        }
        
        if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f)
        {
            currentVelocityY = playerData.jumpForce; //change y only during the jump
            _isGrounded = false;
            
            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;
            _jumpCooldownTimer = 0.15f; 
        }

        //use: movement along X from the keyboard, movement along Y - from Unity or jump
        _rb.linearVelocity = new Vector2(_horizontalInput * playerData.baseSpeed, currentVelocityY);
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
