using UnityEngine;
using Shared.DataDefinitions;

namespace Server.AI
{
    // Moves horizontally, handles walls (jump if 1-2 tiles) and gaps (turn around).
    // Switches to FighterChaseState when a player enters detection range.
    public class FighterPatrolState : IEnemyState
    {
        private readonly ServerEnemyController _enemy;
        private float _moveDir = 1f; // 1 = right, -1 = left

        public FighterPatrolState(ServerEnemyController controller)
        {
            _enemy = controller;
        }

        public void EnterState()
        {
            _enemy.currentState = EnemyStateType.Patrol;
        }

        public void UpdateState()
        {
            Transform player = _enemy.FindNearestPlayer();
            if (player != null)
            {
                _enemy.Target = player;
                _enemy.ChangeState(new FighterChaseState(_enemy));
                return;
            }

            Vector2 dir = new Vector2(_moveDir, 0f);
            bool wallAhead  = _enemy.CheckWallAhead(dir);
            bool headClear  = _enemy.CheckHeadClear(dir);
            bool groundAhead = _enemy.CheckGapAhead(dir);
            bool grounded = _enemy.IsGrounded();

            if (grounded && !groundAhead)
            {
                _moveDir *= -1f;
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
                _moveDir *= -1f;
            }

            float speedX = _moveDir * _enemy.Data.moveSpeed;
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