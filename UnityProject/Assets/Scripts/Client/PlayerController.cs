using Mirror;
using Shared.Components;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI healthBar;
    [SerializeField] private GameObject uiPrefab;

    public static Transform LocalPlayerTransform;

    public PlayerData playerData;
    private PlayerRenderer _playerRenderer;
    private HealthComponent _healthComponent;
    private PlayerMovement _movementComponent;

    private ChunkManager _chunkManager;
    
    private InventoryComponent _inventoryComponent;
    private BlockHighlight _blockHighlight;
    private Rigidbody2D _rb;
    private bool _hasSpawnedOnSurface = false;

    void Start()
    {
        _chunkManager = ChunkManager.Instance;
        _playerRenderer = GetComponent<PlayerRenderer>();
        _inventoryComponent = GetComponent<InventoryComponent>();
        _blockHighlight = GetComponent<BlockHighlight>();
        _healthComponent = GetComponent<HealthComponent>();
       
        _blockHighlight.enabled = true;
        _blockHighlight.InitializeHighlight();
        
        _rb = GetComponent<Rigidbody2D>();
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.bodyType = RigidbodyType2D.Kinematic;

        if (!isLocalPlayer)
            return;
        
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0, -10);

        Instantiate(uiPrefab);

        LocalPlayerTransform = transform;

        _movementComponent = GetComponent<PlayerMovement>();
        _movementComponent.Initialize(playerData, _playerRenderer);

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
    
    void Update()
    {
        if (_healthComponent != null)
        {
            healthBar.text = _healthComponent.HealthNow.ToString();
        }
        
        if (!isLocalPlayer) return;
        
        // TODO rewrite to actually scan chunks around player and only request ones within range.
        _chunkManager.CmdSubscribeToChunk(new Vector2Int(0, 0));
        
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
        
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            _blockHighlight.ChangeMode();
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            _inventoryComponent.UseSelectedItem();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            _playerRenderer.ChangeColor();
        }
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
            _inventoryComponent.ChangeSelection(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _inventoryComponent.ChangeSelection(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _inventoryComponent.ChangeSelection(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            _inventoryComponent.ChangeSelection(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            _inventoryComponent.ChangeSelection(4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            _inventoryComponent.ChangeSelection(5);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            _inventoryComponent.ChangeSelection(6);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            _inventoryComponent.ChangeSelection(7);
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            _inventoryComponent.ChangeSelection(8);
        }
    }
    
}
