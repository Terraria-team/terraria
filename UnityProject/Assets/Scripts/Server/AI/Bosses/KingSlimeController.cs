using UnityEngine;
using Mirror;
using Shared.Components;

namespace Server.AI.Bosses
{
    public class KingSlimeController : ServerEnemyController
    {
        [Header("King Slime Minions")]
        [SerializeField] private GameObject _slimeMinionPrefab;
        [SerializeField] private float _slimeScale = 0.5f;
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
                LayerMask checkMask = BlockingLayer | (1 << gameObject.layer) | LayerMask.GetMask("enemies", "Enemies");
                for (int i = 0; i < 10; i++)
                {
                    Vector2 offset = Random.insideUnitCircle;
                    if (offset.y < 0f) offset.y = -offset.y;
                    if (offset == Vector2.zero) offset = Vector2.up;
                    Vector2 spawnPos = (Vector2)transform.position + offset.normalized * Random.Range(2.5f, 4.5f);
                    
                    if (Physics2D.OverlapCircle(spawnPos, 0.5f, checkMask) == null)
                    {
                        GameObject minion = Instantiate(_slimeMinionPrefab, spawnPos, Quaternion.identity);
                        minion.transform.localScale = Vector3.one * _slimeScale;
                        NetworkServer.Spawn(minion);
                        break;
                    }
                }
            }
        }
    }
}
