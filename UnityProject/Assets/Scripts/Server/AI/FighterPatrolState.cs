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

            if (!groundAhead)
            {
                _moveDir *= -1f;
            }
            else if (wallAhead && !headClear)
            {
                if (_enemy.IsGrounded())
                {
                    var v = _enemy.Rb.linearVelocity;
                    _enemy.Rb.linearVelocity = new Vector2(v.x, _enemy.Data.jumpForce);
                }
            }
            else if (wallAhead && headClear)
            {
                _moveDir *= -1f;
            }

            float currentY = _enemy.Rb.linearVelocity.y;
            _enemy.Rb.linearVelocity = new Vector2(_moveDir * _enemy.Data.moveSpeed, currentY);
        }

        public void ExitState() { }
    }
}