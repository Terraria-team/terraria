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
            
            if (_enemy.BehaviorType == EnemyBehaviorType.Flyer)
            {
                _enemy.Rb.gravityScale = 0f;
                _enemy.Rb.linearVelocity = Vector2.zero;
            }
            else
            {
                _enemy.Rb.linearVelocity = new Vector2(0f, _enemy.Rb.linearVelocity.y);
            }
            
            _cooldownTimer = 0f;
        }

        public void UpdateState()
        {
            if (_enemy.Target == null)
            {
                if (_enemy.BehaviorType == EnemyBehaviorType.Flyer)
                    _enemy.ChangeState(new FlyerIdleState(_enemy));
                else
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
                    Debug.Log($"[Enemy] Melee hit! Damage: {_enemy.Data.attackDamage} (HP decrease not implemented yet)");
                    _cooldownTimer = _enemy.Data.attackCooldown;
                }
                else
                {
                    if (_enemy.BehaviorType == EnemyBehaviorType.Flyer)
                        _enemy.ChangeState(new FlyerChaseState(_enemy));
                    else
                        _enemy.ChangeState(new FighterChaseState(_enemy));
                }
            }
        }

        public void ExitState() { }
    }
}
