using UnityEngine;
using Mirror;
using Shared.DataDefinitions;

namespace Server.AI
{
    // Behaviour:
    //   - Maintains EnemyData.preferredShootDistance from the player
    //   - Continuously checks Line of Sight via Physics2D.Linecast on BlockingLayer
    //   - When LoS is clear and cooldown expired -> spawns a ProjectileController
    //   - П3 (magic through tiles): set BlockingLayer mask to 0 in the inspector
    //     so Linecast always passes, allowing the enemy to fire through walls.
    // Requires:
    //   - ServerEnemyController._projectilePrefab assigned (NetworkManager spawnable list)
    //   - ServerEnemyController._blockingLayer set to the ground/tile layer
    public class ShooterState : IEnemyState
    {
        private readonly ServerEnemyController _enemy;
        private float _shootCooldown;

        public ShooterState(ServerEnemyController controller)
        {
            _enemy = controller;
        }

        public void EnterState()
        {
            _enemy.currentState = EnemyStateType.Shoot;
            _shootCooldown = 0f;
        }

        public void UpdateState()
        {
            // Re-acquire target 
            if (_enemy.Target == null)
                _enemy.Target = _enemy.FindNearestPlayer();

            if (_enemy.Target == null) return;

            Vector2 toTarget = (Vector2)(_enemy.Target.position - _enemy.transform.position);
            float dist = toTarget.magnitude;
            float preferred = _enemy.Data.preferredShootDistance;

            // Maintain preferred distance
            float currentY = _enemy.Rb.linearVelocity.y;
            if (dist < preferred * 0.6f)
            {
                // Too close - retreat
                float retreatDir = -Mathf.Sign(toTarget.x);
                _enemy.Rb.linearVelocity = new Vector2(retreatDir * _enemy.Data.moveSpeed, currentY);
            }
            else if (dist > preferred * 1.4f)
            {
                // Too far - approach
                float approachDir = Mathf.Sign(toTarget.x);
                _enemy.Rb.linearVelocity = new Vector2(approachDir * _enemy.Data.moveSpeed * 0.5f, currentY);
            }
            else
            {
                // In sweet spot - stop horizontal movement
                _enemy.Rb.linearVelocity = new Vector2(0f, currentY);
            }

            // Shoot cooldown + LoS check
            _shootCooldown -= Time.deltaTime;
            if (_shootCooldown <= 0f && HasLineOfSight())
            {
                Shoot(toTarget.normalized);
                _shootCooldown = _enemy.Data.attackCooldown;
            }
        }

        // True if there are no blocking tiles between the enemy and the player.
        // П3: set _blockingLayer = 0 in the inspector to ignore tiles (magic passes through walls)
        private bool HasLineOfSight()
        {
            // If blocking layer is empty (mask == 0), always return true (П3 magic)
            if (_enemy.BlockingLayer == 0) return true;

            Vector2 origin = _enemy.transform.position;
            Vector2 target = _enemy.Target.position;
            RaycastHit2D hit = Physics2D.Linecast(origin, target, _enemy.BlockingLayer);
            return !hit; // No tile between enemy and player
        }

        private void Shoot(Vector2 direction)
        {
            GameObject prefab = _enemy.ProjectilePrefab;
            if (prefab == null)
            {
                Debug.LogWarning("[ShooterState] ProjectilePrefab not assigned on ServerEnemyController!");
                return;
            }

            GameObject projObj = Object.Instantiate(prefab, _enemy.transform.position, Quaternion.identity);
            ProjectileController proj = projObj.GetComponent<ProjectileController>();
            if (proj != null)
            {
                proj.Initialize(
                    direction,
                    _enemy.Data.projectileSpeed,
                    _enemy.Data.attackDamage,
                    _enemy.Data.projectileLifetime
                );
            }
            NetworkServer.Spawn(projObj);
        }

        public void ExitState() { }
    }
}
