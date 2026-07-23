using UnityEngine;
using Mirror;
using Shared.Components;

namespace Server.AI.Bosses
{
    public class QueenBeeController : ServerEnemyController
    {
        [Header("Queen Bee Phases")]
        [SerializeField] private GameObject _hornetPrefab;
        [SerializeField] private float _hornetScale = 0.5f;
        [SerializeField] private float _phase2HealthThreshold = 0.5f;
        [SerializeField] private float _chargeDuration = 5f;
        [SerializeField] private float _shootDuration = 4f;
        
        [Header("Phase 2 Adjustments")]
        [SerializeField] private float _phase2MoveSpeedMult = 1.5f;
        [SerializeField] private float _phase2StateDurationMult = 0.7f;
        [SerializeField] private float _hornetSpawnInterval = 10f;

        private HealthComponent _healthComponent;
        private bool _inPhase2 = false;
        private float _stateTimer = 0f;
        private float _hornetSpawnTimer = 0f;
        private bool _isCharging = true;

        protected override void Awake()
        {
            base.Awake();
            _healthComponent = GetComponent<HealthComponent>();
        }

        [Server]
        protected override void DispatchInitialState()
        {
            _isCharging = true;
            _stateTimer = _chargeDuration;
            ChangeState(new FlyerChaseState(this));
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
                _hornetSpawnTimer -= Time.deltaTime;
                if (_hornetSpawnTimer <= 0f)
                {
                    _hornetSpawnTimer = _hornetSpawnInterval;
                    SpawnHornet();
                }
            }

            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                ToggleState();
            }
        }

        [Server]
        private void ToggleState()
        {
            _isCharging = !_isCharging;

            float duration = _isCharging ? _chargeDuration : _shootDuration;
            if (_inPhase2) duration *= _phase2StateDurationMult;
            
            _stateTimer = duration;

            if (_isCharging)
            {
                ChangeState(new FlyerChaseState(this));
            }
            else
            {
                ChangeState(new ShooterState(this));
            }
        }

        [Server]
        private void EnterPhase2()
        {
            _inPhase2 = true;
            
            if (Data != null)
            {
                Data.moveSpeed *= _phase2MoveSpeedMult;
            }
            
            _hornetSpawnTimer = _hornetSpawnInterval;
            SpawnHornet();
        }

        [Server]
        private void SpawnHornet()
        {
            if (_hornetPrefab != null)
            {
                LayerMask checkMask = BlockingLayer | (1 << gameObject.layer) | LayerMask.GetMask("enemies", "Enemies");
                for (int i = 0; i < 10; i++)
                {
                    Vector2 offset = Random.insideUnitCircle;
                    if (offset == Vector2.zero) offset = Vector2.up;
                    Vector2 spawnPos = (Vector2)transform.position + offset.normalized * Random.Range(2.5f, 4.5f);
                    
                    if (Physics2D.OverlapCircle(spawnPos, 0.5f, checkMask) == null)
                    {
                        GameObject minion = Instantiate(_hornetPrefab, spawnPos, Quaternion.identity);
                        minion.transform.localScale = Vector3.one * _hornetScale;
                        NetworkServer.Spawn(minion);
                        break;
                    }
                }
            }
        }
    }
}
