using UnityEngine;
using Shared.DataDefinitions;

namespace Server.AI
{
    // Waits on the ground until the jump timer expires, then leaps toward the player.
    public class SlimeIdleState : IEnemyState
    {
        private readonly ServerEnemyController _enemy;
        private float _jumpTimer;
        private const float JumpInterval = 1.5f;

        public SlimeIdleState(ServerEnemyController controller)
        {
            _enemy = controller;
        }

        public void EnterState()
        {
            _enemy.currentState = EnemyStateType.Idle;
            _jumpTimer = JumpInterval;
            // Kill any residual velocity (slime lands and stops)
            _enemy.Rb.linearVelocity = new Vector2(0f, _enemy.Rb.linearVelocity.y);
        }

        public void UpdateState()
        {
            _jumpTimer -= Time.deltaTime;

            if (_jumpTimer <= 0f && _enemy.IsGrounded())
            {
                // Refresh target before each jump
                Transform player = _enemy.FindNearestPlayer();
                if (player != null)
                    _enemy.Target = player;

                _enemy.ChangeState(new SlimeJumpState(_enemy));
            }
        }

        public void ExitState() { }
    }
}
