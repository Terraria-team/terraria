using UnityEngine;
using Shared.DataDefinitions;

namespace Server.AI
{
    // Applies a single physics impulse toward the player (or straight up if no target).
    // Waits for landing using IsGrounded, then returns to SlimeIdleState.
    // MinAirTime prevents re-detection of ground in the same frame as the jump.
    public class SlimeJumpState : IEnemyState
    {
        private readonly ServerEnemyController _enemy;
        private bool _hasJumped;
        private float _airTimer;
        private const float MinAirTime = 0.2f;

        public SlimeJumpState(ServerEnemyController controller)
        {
            _enemy = controller;
        }

        public void EnterState()
        {
            _enemy.currentState = EnemyStateType.Jump;
            _hasJumped = false;
            _airTimer = 0f;
        }

        public void UpdateState()
        {
            if (!_hasJumped)
            {
                if (_enemy.IsGrounded())
                {
                    // Calculate jump impulse toward target
                    Vector2 jumpDir;
                    if (_enemy.Target != null)
                    {
                        Vector2 toTarget = (Vector2)(_enemy.Target.position - _enemy.transform.position);
                        // Horizontal sign + upward component, then normalize
                        jumpDir = new Vector2(Mathf.Sign(toTarget.x) * 0.65f, 1f).normalized;
                    }
                    else
                    {
                        jumpDir = Vector2.up;
                    }

                    _enemy.Rb.linearVelocity = jumpDir * _enemy.Data.jumpForce;
                    _hasJumped = true;
                    _airTimer = MinAirTime;
                }
            }
            else
            {
                // Wait for landing
                _airTimer -= Time.deltaTime;
                if (_airTimer <= 0f && _enemy.IsGrounded() && _enemy.Rb.linearVelocity.y <= 0.1f)
                {
                    _enemy.ChangeState(new SlimeIdleState(_enemy));
                }
            }
        }

        public void ExitState() { }
    }
}
