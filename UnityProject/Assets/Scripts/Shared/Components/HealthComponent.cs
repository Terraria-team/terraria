using System;

namespace Shared.Components
{
    using Mirror;
    using UnityEngine;
    using System;
    
    public class HealthComponent : NetworkBehaviour
    {
        public event Action OnDamageFlashed;
        public event Action OnHealingFlashed;
        public event Action<int> OnDamageTakenServer;
        
        [Header("Health Stats")]
        [SerializeField][SyncVar(hook = "OnHealthChange")]
        private int CurrentHealth;
        
        [SerializeField][SyncVar]
        private int MaxHealth = 100;

        public int HealthNow => CurrentHealth;
        public int HealthMax => MaxHealth;
        
        void OnHealthChange(int oldHealth, int newHealth)
        {
            
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
            
            OnDamageTakenServer?.Invoke(amount);
            
            RpcTriggerDamageFlash();
        }

        [ClientRpc]
        private void RpcTriggerDamageFlash()
        {
            OnDamageFlashed?.Invoke();
        }
        
        [Command(requiresAuthority = false)]
        public void ApplyHealingServerRpc(int amount)
        {
            if (amount <= 0 || amount > MaxHealth)
            {
                Debug.LogWarning("Rejected healing " + amount); 
                return;
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            
            RpcTriggerHealingFlash();
        }

        [ClientRpc]
        private void RpcTriggerHealingFlash()
        {
            OnHealingFlashed?.Invoke();
        }
        
        public void Awake() 
        {
            CurrentHealth = MaxHealth;
        }
    }
}