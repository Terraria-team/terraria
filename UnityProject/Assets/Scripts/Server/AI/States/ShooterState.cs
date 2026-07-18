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
    //   - FlyerShooter (Л2): ignores gravity, uses 2D steering to orbit player
    // Requires:
    //   - ServerEnemyController._projectilePrefab assigned (NetworkManager spawnable list)
    //   - ServerEnemyController._blockingLayer set to the ground/tile layer
    public class ShooterState : IEnemyState
    {
        private readonly ServerEnemyController _enemy;
        private float _shootCooldown;
        private bool _isFlying;

        public ShooterState(ServerEnemyController controller)
        {
            _enemy = controller;
        }

        public void EnterState()
        {
            _enemy.currentState = EnemyStateType.Shoot;
            _shootCooldown = 0f;
            _isFlying = _enemy.BehaviorType == EnemyBehaviorType.FlyerShooter;

            if (_isFlying)
            {
                _enemy.Rb.gravityScale = 0f;
            }
        }

        public void UpdateState()
        {
            // Re-acquire target 
            if (_enemy.Target == null)
                _enemy.Target = _enemy.FindNearestPlayer();

            if (_enemy.Target == null)
            {
                // FlyerShooter returns to hover idle; ground shooters just wait
                if (_isFlying)
                    _enemy.ChangeState(new FlyerIdleState(_enemy));
                return;
            }

            Vector2 toTarget = (Vector2)(_enemy.Target.position - _enemy.transform.position);
            float dist = toTarget.magnitude;
            float preferred = _enemy.Data.preferredShootDistance;

            if (_isFlying)
            {
                UpdateFlying(toTarget, dist, preferred);
            }
            else
            {
                UpdateGround(toTarget, dist, preferred);
            }

            // Shoot cooldown + LoS check
            _shootCooldown -= Time.deltaTime;
            if (_shootCooldown <= 0f && HasLineOfSight())
            {
                Shoot(toTarget.normalized);
                _shootCooldown = _enemy.Data.attackCooldown;
            }
        }

        // ── Flying shooter (Л2) ──────────────────────────────────────
        // Uses 2D steering: orbits above the player at preferred distance.
        private void UpdateFlying(Vector2 toTarget, float dist, float preferred)
        {
            // Target a point slightly above the player
            Vector2 targetPos = (Vector2)_enemy.Target.position + Vector2.up * 2f;
            Vector2 toTargetAbove = targetPos - (Vector2)_enemy.transform.position;

            Vector2 velocity;
            if (dist < preferred * 0.6f)
            {
                // Too close — retreat directly away
                velocity = -toTargetAbove.normalized * _enemy.Data.moveSpeed;
            }
            else if (dist > preferred * 1.4f)
            {
                // Too far — approach
                velocity = toTargetAbove.normalized * _enemy.Data.moveSpeed * 0.7f;
            }
            else
            {
                // In sweet spot — slow orbit / hover
                velocity = toTargetAbove.normalized * _enemy.Data.moveSpeed * 0.15f;
            }

            _enemy.Rb.linearVelocity = velocity;
        }

        // ── Ground shooter (П2, П3) ─────────────────────────────────
        // Original horizontal-only movement with terrain navigation.
        private void UpdateGround(Vector2 toTarget, float dist, float preferred)
        {
            float currentY = _enemy.Rb.linearVelocity.y;
            float moveDir = 0f;
            float speedMult = 1f;

            if (dist < preferred * 0.6f)
            {
                // Too close - retreat
                moveDir = -Mathf.Sign(toTarget.x);
                speedMult = 1f;
            }
            else if (dist > preferred * 1.4f)
            {
                // Too far - approach
                moveDir = Mathf.Sign(toTarget.x);
                speedMult = 0.5f;
            }

            if (moveDir != 0f)
            {
                Vector2 dir = new Vector2(moveDir, 0f);
                bool wallAhead   = _enemy.CheckWallAhead(dir);
                bool headClear   = _enemy.CheckHeadClear(dir);
                bool groundAhead = _enemy.CheckGapAhead(dir);
                bool grounded    = _enemy.IsGrounded();

                if (!groundAhead || (wallAhead && !headClear))
                {
                    if (grounded)
                    {
                        currentY = _enemy.Data.jumpForce;
                    }
                }
                else if (wallAhead && headClear)
                {
                    // Wall is too high to jump over, stop moving horizontally
                    moveDir = 0f;
                }
            }

            _enemy.Rb.linearVelocity = new Vector2(moveDir * _enemy.Data.moveSpeed * speedMult, currentY);
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
