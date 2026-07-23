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
        public event Action OnDeath;
        
        [Header("Health Stats")]
        [SerializeField][SyncVar(hook = "OnHealthChange")]
        private int CurrentHealth;
        
        [SerializeField][SyncVar]
        private int MaxHealth = 100;

        [Header("Invulnerability")]
        [SerializeField]
        private float invulnerabilityDuration = 0.5f;
        
        [SyncVar]
        private bool _isDead;

        private float _lastDamageTime = -Mathf.Infinity;

        public int HealthNow => CurrentHealth;
        public int HealthMax => MaxHealth;
        public bool IsDead => _isDead;
        
        void OnHealthChange(int oldHealth, int newHealth)
        {
            if (newHealth <= 0 && oldHealth > 0)
            {
                OnDeath?.Invoke();
            }
        }

        [Server]
        public void ApplyDamageServer(int amount)
        {
            if (_isDead) return;
            
            if (amount <= 0 || amount > MaxHealth)
            {
                Debug.LogWarning("Rejected damage " + amount); 
                return;
            }

            // Invulnerability frame check
            if (Time.time - _lastDamageTime < invulnerabilityDuration)
                return;

            _lastDamageTime = Time.time;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            
            OnDamageTakenServer?.Invoke(amount);
            
            if (CurrentHealth <= 0)
            {
                _isDead = true;
                OnDeath?.Invoke();
                RpcTriggerDeath();
            }
            else
            {
                RpcTriggerDamageFlash();
            }
        }
        
        [Command(requiresAuthority = false)]
        public void ApplyDamageServerRpc(int amount)
        {
            ApplyDamageServer(amount);
        }

        [ClientRpc]
        private void RpcTriggerDeath()
        {
            OnDeath?.Invoke();
        }

        [ClientRpc]
        private void RpcTriggerDamageFlash()
        {
            OnDamageFlashed?.Invoke();
        }
        
        [Command(requiresAuthority = false)]
        public void ApplyHealingServerRpc(int amount)
        {
            if (_isDead) return;
            
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
        
        /// <summary>
        /// Resets health to max and clears the dead state. Called by server during respawn.
        /// </summary>
        [Server]
        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
            _isDead = false;
            _lastDamageTime = -Mathf.Infinity;
        }
        
        public void Awake() 
        {
            CurrentHealth = MaxHealth;
        }
    }
}