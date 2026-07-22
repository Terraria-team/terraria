using UnityEngine;
using Shared.DataDefinitions;
using Shared.Components;

namespace Server.AI
{
    // Shared melee attack state - used by Fighter and Flyer strategies.
    // Stops the enemy, waits for attack cooldown, deals damage.
    // Returns to Chase if target leaves attack range.
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
            
            // 1. Deal damage immediately upon entering the attack state
            if (_enemy.Target != null)
            {
                float dist = _enemy.DistanceToTarget();
                // Slimes deal contact damage only — use a tight threshold.
                // Other melee enemies get a small leniency buffer for physics jitter.
                float maxHitDist = _enemy.BehaviorType == EnemyBehaviorType.Slime
                    ? 0.15f
                    : _enemy.Data.attackRange + 0.2f;
                if (dist <= maxHitDist)
                {
                    var health = _enemy.Target.GetComponentInParent<HealthComponent>();
                    if (health != null)
                    {
                        health.ApplyDamageServerRpc((int)_enemy.Data.attackDamage);
                    }
                }
            }

            // 2. Apply physical knockback/bounce
            if (_enemy.BehaviorType == EnemyBehaviorType.Flyer)
            {
                _enemy.Rb.gravityScale = 0f;
                if (_enemy.Target != null)
                {
                    Vector2 bounceDir = (_enemy.transform.position - _enemy.Target.position).normalized;
                    _enemy.Rb.linearVelocity = bounceDir * _enemy.Data.moveSpeed;
                }
            }
            else if (_enemy.BehaviorType == EnemyBehaviorType.Slime)
            {
                if (_enemy.Target != null)
                {
                    float dirX = Mathf.Sign(_enemy.transform.position.x - _enemy.Target.position.x);
                    _enemy.Rb.linearVelocity = new Vector2(dirX * 3f, _enemy.Data.jumpForce * 0.6f);
                }
            }
            else
            {
                _enemy.Rb.linearVelocity = new Vector2(0f, _enemy.Rb.linearVelocity.y);
            }
            
            // 3. Start cooldown timer
            _cooldownTimer = _enemy.Data.attackCooldown;
        }

        public void UpdateState()
        {
            _cooldownTimer -= Time.deltaTime;

            if (_cooldownTimer <= 0f)
            {
                if (_enemy.Target == null)
                {
                    if (_enemy.BehaviorType == EnemyBehaviorType.Flyer)
                        _enemy.ChangeState(new FlyerIdleState(_enemy));
                    else if (_enemy.BehaviorType == EnemyBehaviorType.Slime)
                        _enemy.ChangeState(new SlimeIdleState(_enemy));
                    else
                        _enemy.ChangeState(new FighterPatrolState(_enemy));
                    return;
                }

                if (_enemy.BehaviorType == EnemyBehaviorType.Flyer)
                    _enemy.ChangeState(new FlyerChaseState(_enemy));
                else if (_enemy.BehaviorType == EnemyBehaviorType.Slime)
                    _enemy.ChangeState(new SlimeIdleState(_enemy));
                else
                    _enemy.ChangeState(new FighterChaseState(_enemy));
            }
        }

        public void ExitState() { }
    }
}
