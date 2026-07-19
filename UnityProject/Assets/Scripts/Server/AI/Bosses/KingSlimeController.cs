using UnityEngine;
using Mirror;
using Shared.Components;

namespace Server.AI.Bosses
{
    public class KingSlimeController : ServerEnemyController
    {
        [Header("King Slime Minions")]
        [SerializeField] private GameObject _slimeMinionPrefab;
        [SerializeField] private float _spawnChanceOnDamage = 0.5f;

        protected override void Awake()
        {
            base.Awake();
            var health = GetComponent<HealthComponent>();
            if (health != null)
            {
                health.OnDamageTakenServer += HandleDamageTaken;
            }
        }

        private void OnDestroy()
        {
            var health = GetComponent<HealthComponent>();
            if (health != null)
            {
                health.OnDamageTakenServer -= HandleDamageTaken;
            }
        }

        private void HandleDamageTaken(int amount)
        {
            if (_slimeMinionPrefab != null && Random.value <= _spawnChanceOnDamage)
            {
                // Spawn a minion nearby
                Vector2 spawnPos = (Vector2)transform.position + new Vector2(Random.Range(-1.5f, 1.5f), 1f);
                GameObject minion = Instantiate(_slimeMinionPrefab, spawnPos, Quaternion.identity);
                NetworkServer.Spawn(minion);
            }
        }
    }
}
