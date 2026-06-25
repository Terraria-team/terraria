using System;

namespace Shared.Components
{
    using Mirror;
    using UnityEngine;
    using System;
    
    public class HealthComponent : NetworkBehaviour
    {
        public event Action OnDamageFlashed;
        
        [Header("Health Stats")]
        [SerializeField][SyncVar(hook = "OnHealthChange")]
        private int CurrentHealth;
        
        [SerializeField][SyncVar]
        private int MaxHealth = 100;

        void OnHealthChange(int oldHealth, int newHealth)
        {
            Debug.Log($"Current health: {newHealth}");
        }

        [Command(requiresAuthority = false)]
        public void ApplyDamageServerRpc(int amount)
        {
            if (amount <= 0 || amount > MaxHealth)
            {
                Debug.LogWarning("Rejected damage " + amount); 
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            
            RpcTriggerDamageFlash();
        }

        [ClientRpc]
        private void RpcTriggerDamageFlash()
        {
            OnDamageFlashed?.Invoke();
        }
        
        public override void OnStartServer() 
        {
            CurrentHealth = MaxHealth;
        }
    }
}