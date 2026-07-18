using UnityEngine;
using Shared.DataDefinitions;

namespace Server.AI
{

    // Aggressively follows the player:
    //   - Jumpable wall (1-2 tiles)  -> jump and keep chasing
    //   - Impassable wall            -> give up (back to Patrol)
    //   - Gap ahead                  -> jump over it
    //   - Player in attack range     -> MeleeAttackState
    //   - Player out of lose range   -> back to Patrol
    public class FighterChaseState : IEnemyState
    {
        private readonly ServerEnemyController _enemy;

        public FighterChaseState(ServerEnemyController controller)
        {
            _enemy = controller;
        }

        public void EnterState()
        {
            _enemy.currentState = EnemyStateType.Chase;
        }

        public void UpdateState()
        {
            // Lose target 
            if (_enemy.Target == null ||
                Vector2.Distance(_enemy.transform.position, _enemy.Target.position) > _enemy.Data.loseTargetRange)
            {
                _enemy.Target = null;
                _enemy.ChangeState(new FighterPatrolState(_enemy));
                return;
            }

            // Attack range (contact damage)
            float dist = _enemy.DistanceToTarget();
            if (dist <= 0.1f)
            {
                _enemy.ChangeState(new MeleeAttackState(_enemy));
                return;
            }

            // Terrain probing
            float moveDir = _enemy.Target.position.x > _enemy.transform.position.x ? 1f : -1f;
            Vector2 dir = new Vector2(moveDir, 0f);

            bool wallAhead   = _enemy.CheckWallAhead(dir);
            bool headClear   = _enemy.CheckHeadClear(dir);
            bool groundAhead = _enemy.CheckGapAhead(dir);
            bool grounded    = _enemy.IsGrounded();

            if (!groundAhead)
            {
                if (grounded)
                {
                    var v = _enemy.Rb.linearVelocity;
                    _enemy.Rb.linearVelocity = new Vector2(v.x, _enemy.Data.jumpForce);
                }
            }
            else if (wallAhead && !headClear)
            {
                if (grounded)
                {
                    var v = _enemy.Rb.linearVelocity;
                    _enemy.Rb.linearVelocity = new Vector2(v.x, _enemy.Data.jumpForce);
                }
            }
            else if (wallAhead && headClear)
            {
                _enemy.Target = null;
                _enemy.ChangeState(new FighterPatrolState(_enemy));
                return;
            }

            float speedX = moveDir * _enemy.Data.moveSpeed;
            if (wallAhead && !grounded)
            {
                speedX = 0f;
            }

            float currentY = _enemy.Rb.linearVelocity.y;
            
            // Prevent clipping into tile corners when falling
            if (currentY < 0f && Mathf.Abs(_enemy.Rb.linearVelocity.x) < 0.1f)
            {
                speedX = 0f;
            }

            _enemy.Rb.linearVelocity = new Vector2(speedX, currentY);
        }

        public void ExitState() { }
    }
}
