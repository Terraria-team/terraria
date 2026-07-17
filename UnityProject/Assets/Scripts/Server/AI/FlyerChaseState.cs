using UnityEngine;
using Shared.DataDefinitions;

namespace Server.AI
{
    // Ignores gravity. Uses steering (direct vector) toward the player.
    // Optional sinusoidal oscillation perpendicular to movement direction
    // creates a realistic "flight wobble" - enable via EnemyData.useSinusoidalFlight.
    // Л1: sinusoidal = true (erratic weak flyer)
    // Л2: sinusoidal = false (precise aggressive flyer)
    public class FlyerChaseState : IEnemyState
    {
        private readonly ServerEnemyController _enemy;
        private float _timeElapsed;

        public FlyerChaseState(ServerEnemyController controller)
        {
            _enemy = controller;
        }

        public void EnterState()
        {
            _enemy.currentState = EnemyStateType.Fly;
            _enemy.Rb.gravityScale = 0f;
            _timeElapsed = 0f;
        }

        public void UpdateState()
        {
            // Re-acquire if target gone
            if (_enemy.Target == null)
                _enemy.Target = _enemy.FindNearestPlayer();

            if (_enemy.Target == null)
            {
                _enemy.ChangeState(new FlyerIdleState(_enemy));
                return;
            }

            _timeElapsed += Time.deltaTime;

            Vector2 toTarget = (Vector2)(_enemy.Target.position - _enemy.transform.position);

            // Attack range
            if (toTarget.magnitude <= _enemy.Data.attackRange)
            {
                _enemy.ChangeState(new MeleeAttackState(_enemy));
                return;
            }

            // Steering direction
            Vector2 direction = toTarget.normalized;

            if (_enemy.Data.useSinusoidalFlight)
            {
                // Add sinusoidal offset perpendicular to movement (flight wobble)
                Vector2 perp = new Vector2(-direction.y, direction.x);
                float wave = Mathf.Sin(_timeElapsed * _enemy.Data.sinFrequency) * _enemy.Data.sinAmplitude;
                direction = (direction + perp * wave * Time.deltaTime).normalized;
            }

            _enemy.Rb.linearVelocity = direction * _enemy.Data.moveSpeed;
        }

        public void ExitState()
        {
            _enemy.Rb.gravityScale = 1f;
        }
    }
}
