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

            // Lose target if they get too far away
            if (_enemy.Target != null)
            {
                float checkDist = Vector2.Distance(_enemy.transform.position, _enemy.Target.position);
                if (checkDist > _enemy.Data.loseTargetRange)
                {
                    _enemy.Target = null;
                }
            }

            if (_enemy.Target == null)
            {
                // FlyerShooter returns to hover idle; ground shooters return to patrol
                if (_isFlying)
                    _enemy.ChangeState(new FlyerIdleState(_enemy));
                else
                    _enemy.ChangeState(new FighterPatrolState(_enemy));
                
                return;
            }

            Vector2 toTarget = (Vector2)(_enemy.Target.position - _enemy.transform.position);
            float dist = toTarget.magnitude;
            float preferred = _enemy.Data.preferredShootDistance;
            
            bool hasLoS = HasLineOfSight();

            if (_isFlying)
            {
                UpdateFlying(toTarget, dist, preferred, hasLoS);
            }
            else
            {
                UpdateGround(toTarget, dist, preferred, hasLoS);
            }

            // Shoot cooldown + LoS check
            _shootCooldown -= Time.deltaTime;
            if (_shootCooldown <= 0f && hasLoS)
            {
                Shoot(toTarget.normalized);
                _shootCooldown = _enemy.Data.attackCooldown;
            }
        }

        // ── Flying shooter (Л2) ──────────────────────────────────────
        // Uses 2D steering: orbits above the player at preferred distance.
        private void UpdateFlying(Vector2 toTarget, float dist, float preferred, bool hasLoS)
        {
            // Target a point 2.5 units above the player
            Vector2 targetPos = (Vector2)_enemy.Target.position + Vector2.up * 2.5f;
            Vector2 toTargetAbove = targetPos - (Vector2)_enemy.transform.position;
            Vector2 dir = toTargetAbove.normalized;

            Vector2 targetVelocity;
            if (!hasLoS)
            {
                // Can't see player -> approach directly to get a line of sight
                targetVelocity = dir * _enemy.Data.moveSpeed * 0.8f;
            }
            else if (dist < preferred * 0.8f)
            {
                // Too close — retreat horizontally, but still try to reach the hover height
                float retreatX = -Mathf.Sign(toTarget.x) * _enemy.Data.moveSpeed * 0.6f;
                targetVelocity = new Vector2(retreatX, dir.y * _enemy.Data.moveSpeed * 0.6f);
            }
            else if (dist > preferred * 1.2f)
            {
                // Too far — approach directly to the point above the player
                targetVelocity = dir * _enemy.Data.moveSpeed * 0.6f;
            }
            else
            {
                // In sweet spot — stop horizontally, maintain hover height, add a slight bob
                float bob = Mathf.Sin(Time.time * 2f) * 0.3f;
                targetVelocity = new Vector2(0f, dir.y * _enemy.Data.moveSpeed * 0.4f + bob);
            }

            // Smoothly interpolate the velocity to prevent any jittering or sharp snaps
            _enemy.Rb.linearVelocity = Vector2.Lerp(_enemy.Rb.linearVelocity, targetVelocity, Time.deltaTime * 3f);
        }

        // ── Ground shooter (П2, П3) ─────────────────────────────────
        // Original horizontal-only movement with terrain navigation.
        private void UpdateGround(Vector2 toTarget, float dist, float preferred, bool hasLoS)
        {
            float currentY = _enemy.Rb.linearVelocity.y;
            float moveDir = 0f;
            float speedMult = 1f;

            if (!hasLoS)
            {
                // Can't see player -> approach to get a line of sight
                moveDir = Mathf.Sign(toTarget.x);
                speedMult = 1f;
            }
            else if (dist < preferred * 0.6f)
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
                    
                    // If we don't have LoS and wall is too high, try to jump anyway to get over it
                    // or at least try to shoot over it occasionally.
                    if (!hasLoS && grounded && Random.value < 0.02f)
                    {
                        currentY = _enemy.Data.jumpForce;
                    }
                }
            }

            float speedX = moveDir * _enemy.Data.moveSpeed * speedMult;
            if (moveDir != 0f && _enemy.CheckWallAhead(new Vector2(Mathf.Sign(moveDir), 0f)) && !_enemy.IsGrounded())
            {
                speedX = 0f;
            }

            // Prevent clipping into tile corners when falling
            if (!grounded && currentY < -0.1f && Mathf.Abs(_enemy.Rb.linearVelocity.x) < 0.1f)
            {
                speedX = 0f;
            }

            _enemy.Rb.linearVelocity = new Vector2(speedX, currentY);
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
                    _enemy.Data.projectileLifetime,
                    _enemy.Data.projectilePassesThroughWalls,
                    _enemy.BlockingLayer
                );
            }
            NetworkServer.Spawn(projObj);
        }

        public void ExitState() { }
    }
}
