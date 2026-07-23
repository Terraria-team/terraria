using System.Threading.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerRenderer : NetworkBehaviour
{
    // 1. REPLICATION (SyncVar): Automatically syncs from the Server to all Clients.
    // The "hook" function runs on clients whenever the server changes this value.
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    [SyncVar(hook = nameof(OnRotationChanged))]
    public bool playerFacingLeft = true;

    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private int damageFlashTime = 15;
    [SerializeField] private int healingFlashTime = 15;
    
    public PlayerData playerData;
    
    void Start()
    {
        if (playerRenderer != null) 
            playerRenderer.color = playerColor;
    }

    void Update()
    {
        
    }

    public void ChangeDirection(bool left)
    {
        if (left != playerFacingLeft)
        {
            CmdChangeDirection(left);
        }
    }
    
    [Command]
    void CmdChangeDirection(bool newDirection)
    {
        playerFacingLeft = newDirection;
    }

    [Command]
    public async void DamageFlash()
    {
        Color curColor = playerColor;
        playerColor = Color.red;

        // wait for damageFlashTime ms
        await Task.Delay(damageFlashTime); 

        // Safety check: Ensure the object hasn't been destroyed while we were waiting
        if (this != null && playerRenderer != null) 
        {
            playerColor= curColor;
        }
    }

    [Command]
    public async void HealingFlash()
    {
        Color curColor = playerColor;
        playerColor = Color.limeGreen;

        // wait for healingFlashTime ms
        await Task.Delay(healingFlashTime); 

        // Safety check: Ensure the object hasn't been destroyed while we were waiting
        if (this != null && playerRenderer != null) 
        {
            playerColor= curColor;
        }
    }
    
    void OnColorChanged(Color oldColor, Color newColor)
    {
        playerRenderer.color = newColor;
    }
    
    void OnRotationChanged(bool oldRotation, bool newRotation)
    {
        if (playerRenderer != null)
        {
            playerRenderer.flipX = !newRotation;
        }
    }
}
