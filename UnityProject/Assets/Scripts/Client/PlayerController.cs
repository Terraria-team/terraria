using System;
using System.Collections;
using Core.WorldGeneration;
using Mirror;
using Server.AI;
using Shared.Components;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private TextMeshProUGUI healthBar;
    [SerializeField] private GameObject uiPrefab;

    public static Transform LocalPlayerTransform;

    public PlayerData playerData;
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string playerName = "";

    private TextMeshPro _nameText;
    private PlayerRenderer _playerRenderer;
    private HealthComponent _healthComponent;
    private PlayerMovement _movementComponent;

    private ChunkManager _chunkManager;
    
    private InventoryComponent _inventoryComponent;
    private BlockHighlight _blockHighlight;
    private Rigidbody2D _rb;
    private bool _hasSpawnedOnSurface = false;
    private bool _isDead = false;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private Vector3 spawnPosition;

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

        GameObject nameObj = new GameObject("Name_Text");
        nameObj.transform.SetParent(transform);
        nameObj.transform.localPosition = new Vector3(0, 2.2f, 0); // Above HP
        _nameText = nameObj.AddComponent<TextMeshPro>();
        _nameText.alignment = TextAlignmentOptions.Center;
        _nameText.fontSize = 3;
        _nameText.color = Color.white;
        _nameText.sortingOrder = 10;
        _nameText.text = playerName;

        if (!isLocalPlayer)
            return;
            
        backgroundRenderer = GameObject.FindWithTag("Background").GetComponent<SpriteRenderer>();
        
        string myName = Client.Auth.AuthService.Instance.CurrentNickname;
        if (string.IsNullOrEmpty(myName)) myName = "Player";
        CmdSetName(myName);
        
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0, -10);

        Instantiate(uiPrefab);

        LocalPlayerTransform = transform;

        _movementComponent = GetComponent<PlayerMovement>();
        _movementComponent.Initialize(playerData, _playerRenderer);

        _healthComponent.OnDamageFlashed += _playerRenderer.DamageFlash;
        _healthComponent.OnHealingFlashed += _playerRenderer.HealingFlash;
        _healthComponent.OnDeath += HandleLocalPlayerDeath;
        _movementComponent.OnFallDamage += HandleFallDamage;
    }
    
    void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks
        if (_healthComponent != null)
        {
            _healthComponent.OnDamageFlashed -= _playerRenderer.DamageFlash;
            _healthComponent.OnHealingFlashed -= _playerRenderer.HealingFlash;
            _healthComponent.OnDeath -= HandleLocalPlayerDeath;
        }
        if (_movementComponent != null)
        {
            _movementComponent.OnFallDamage -= HandleFallDamage;
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

    [SerializeField] private float cooldown = 30f;
    private float lastSpawnTime = -Mathf.Infinity;

    [Server]
    void TrySpawningAround()
    {
        const float visionRange = 96;

        if (Time.time - lastSpawnTime < cooldown)
            return;

        {
            LayerMask mask = LayerMask.GetMask("Enemies");
            Collider2D[] results = Physics2D.OverlapCircleAll(
                transform.position,
                visionRange,
                mask);
        
            if (results.Length > 10)
                return;
        }

        for (int i = 0; i < 100; i++)
        {
            float spawnAngle = Random.Range(0, 6.283f);
            Vector3 pos = transform.position + visionRange * new Vector3((float)Math.Cos(spawnAngle), (float)Math.Sin(spawnAngle) * 0.2f, 0);
        
            var type = MapGenerator.GetBiomeTypeAt(ChunkUtils.ChunkCoordsAtWorldPosition(pos));
            var data = DataManager.Biomes[type];

            if (data == null || data.allowedEnemies.Length == 0)
                continue;

            int randomIndex = Random.Range(0, data.allowedEnemies.Length);
            var randomEnemy = data.allowedEnemies[randomIndex];
            
            Bounds bounds = randomEnemy.Prefab.GetComponent<Collider2D>().bounds;
            Collider2D[] results = new Collider2D[1];
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;

            int hitCount = Physics2D.OverlapBox(pos, bounds.size, 0f, filter, results);
            
            if (hitCount != 0)
                continue;
        
            var spawned = Instantiate(randomEnemy.Prefab,
                pos,
                Quaternion.identity
            );
            NetworkServer.Spawn(spawned);
            lastSpawnTime = Time.time;
            ServerEnemyController.EnemyCounter++;
            break;
        }
    }
    
    void Update()
    {
        if (isServer)
        {
            TrySpawningAround();    
        }
        
        if (_healthComponent != null)
        {
            healthBar.text = _healthComponent.HealthNow.ToString();
        }
        
        if (!isLocalPlayer || _isDead) return;
        
        var type = MapGenerator.GetBiomeTypeAt(ChunkUtils.ChunkCoordsAtWorldPosition(transform.position));
        var data = DataManager.Biomes[type];
        
        if (data.background != null && backgroundRenderer != null)
            backgroundRenderer.sprite = data.background;

        for (int x = 0; x < ChunkManager.Instance.WorldSize.x; x++)
        {
            for (int y = 0; y < ChunkManager.Instance.WorldSize.y; y++)
            {
                Vector2Int chunkCoords = new Vector2Int(x, y);
                
                if (ChunkManager.IsChunkRelevantFor(chunkCoords, transform.position))
                    _chunkManager.CmdSubscribeToChunk(new Vector2Int(x, y));
            }
        }
        
        TrySwitchingInventorySlot();

        if (!_hasSpawnedOnSurface)
        {
            if (_chunkManager != null && _chunkManager.clientHasFinishedApplying)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic; // Enable physics now that the map is ready
                //RepositionToSurface();
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
    
    void OnPlayerNameChanged(string oldName, string newName)
    {
        if (_nameText != null)
        {
            _nameText.text = newName;
        }
    }

    [Command]
    public void CmdSetName(string newName)
    {
        playerName = newName;
    }

    // ── Fall Damage ──────────────────────────────────────────────
    private void HandleFallDamage(int damage)
    {
        if (_healthComponent != null && !_healthComponent.IsDead)
        {
            CmdApplyFallDamage(damage);
        }
    }

    [Command]
    private void CmdApplyFallDamage(int damage)
    {
        if (_healthComponent != null)
        {
            _healthComponent.ApplyDamageServerRpc(damage);
        }
    }

    // ── Death / Respawn ──────────────────────────────────────────
    private void HandleLocalPlayerDeath()
    {
        _isDead = true;
        // Disable input
        if (_movementComponent != null)
            _movementComponent.enabled = false;
        if (_blockHighlight != null)
            _blockHighlight.enabled = false;

        // Immediately hide the player sprite
        if (_playerRenderer != null)
            _playerRenderer.SetVisible(false);

        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        CmdRequestRespawn();
    }

    [Command]
    private void CmdRequestRespawn()
    {
        if (_healthComponent == null) return;

        // Reset health on server (SyncVar propagates to clients)
        _healthComponent.ResetHealth();

        // Teleport to the developer-defined spawn position
        transform.position = spawnPosition;

        // Notify the client to re-enable controls
        RpcCompleteRespawn();
    }

    [ClientRpc]
    private void RpcCompleteRespawn()
    {
        _isDead = false;

        // Show the player sprite again
        if (_playerRenderer != null)
            _playerRenderer.SetVisible(true);

        if (!isLocalPlayer) return;

        // Re-enable input
        if (_movementComponent != null)
            _movementComponent.enabled = true;
        if (_blockHighlight != null)
        {
            _blockHighlight.enabled = true;
            _blockHighlight.InitializeHighlight();
        }

        // Reset physics so the player doesn't keep falling
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }
}
