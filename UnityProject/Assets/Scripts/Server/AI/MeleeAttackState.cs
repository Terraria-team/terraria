using UnityEngine;
using Shared.DataDefinitions;

namespace Server.AI
{
    // Shared melee attack state - used by Fighter and Flyer strategies.
    // Stops the enemy, waits for attack cooldown, deals damage.
    // Returns to Chase if target leaves attack range.
    // TODO: integrate with player health system
    public class MeleeAttackState : IEnemyState
    {
        private readonly ServerEnemyController _enemy;
        private float _cooldownTimer;

        public MeleeAttackState(ServerEnemyController controller)
        {
            _enemy = controller;
        }

        public void EnterState()
        {
            _enemy.currentState = EnemyStateType.Attack;
            _enemy.Rb.linearVelocity = new Vector2(0f, _enemy.Rb.linearVelocity.y);
            _cooldownTimer = 0f;
        }

        public void UpdateState()
        {
            if (_enemy.Target == null)
            {
                _enemy.ChangeState(new FighterPatrolState(_enemy));
                return;
            }

            float dist = Vector2.Distance(_enemy.transform.position, _enemy.Target.position);
            _cooldownTimer -= Time.deltaTime;

            if (_cooldownTimer <= 0f)
            {
                if (dist <= _enemy.Data.attackRange)
                {
                    // Deal damage
                    // TODO: call _enemy.Target.GetComponent<HealthComponent>()?.TakeDamage(...)
                    Debug.Log($"[Enemy] Melee hit! Damage: {_enemy.Data.attackDamage}");
                    _cooldownTimer = _enemy.Data.attackCooldown;
                }
                else
                {
                    _enemy.ChangeState(new FighterChaseState(_enemy));
                }
            }
        }

        public void ExitState() { }
    }
}
