using UnityEngine;
using Mirror;
using Shared.Components;

namespace Server.AI.Bosses
{
    public class EyeOfCthulhuController : ServerEnemyController
    {
        [Header("Eye of Cthulhu Phase 2")]
        [SerializeField] private GameObject _servantPrefab;
        [SerializeField] private float _phase2HealthThreshold = 0.5f;
        [SerializeField] private float _phase2MoveSpeed = 8f;
        [SerializeField] private float _phase2AttackCooldown = 0.4f;
        [SerializeField] private float _servantSpawnInterval = 8f;

        private HealthComponent _healthComponent;
        private bool _inPhase2 = false;
        private float _servantSpawnTimer = 0f;

        protected override void Awake()
        {
            base.Awake();
            _healthComponent = GetComponent<HealthComponent>();
        }

        [ServerCallback]
        protected override void Update()
        {
            base.Update();

            if (_healthComponent == null) return;

            if (!_inPhase2)
            {
                float healthPercent = (float)_healthComponent.HealthNow / _healthComponent.HealthMax;
                if (healthPercent <= _phase2HealthThreshold)
                {
                    EnterPhase2();
                }
            }
            else
            {
                // Phase 2 logic: spawn servants occasionally
                _servantSpawnTimer -= Time.deltaTime;
                if (_servantSpawnTimer <= 0f)
                {
                    _servantSpawnTimer = _servantSpawnInterval;
                    SpawnServant();
                }
            }
        }

        [Server]
        private void EnterPhase2()
        {
            _inPhase2 = true;
            
            // Adjust stats
            if (Data != null)
            {
                Data.moveSpeed = _phase2MoveSpeed;
                Data.attackCooldown = _phase2AttackCooldown;
            }

            // Spawn a few servants immediately upon entering phase 2
            SpawnServant();
            SpawnServant();
        }

        [Server]
        private void SpawnServant()
        {
            if (_servantPrefab != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 spawnPos = (Vector2)transform.position + new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
                    if (Physics2D.OverlapPoint(spawnPos, BlockingLayer) == null)
                    {
                        GameObject minion = Instantiate(_servantPrefab, spawnPos, Quaternion.identity);
                        NetworkServer.Spawn(minion);
                        break;
                    }
                }
            }
        }
    }
}
