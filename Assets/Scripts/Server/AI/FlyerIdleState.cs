using UnityEngine;
using Shared.DataDefinitions;

namespace Server.AI
{
    // Flyer strategy - idle/hover phase.
    // Disables gravity, holds position, scans for players.
    // Switches to FlyerChaseState on detection.
    public class FlyerIdleState : IEnemyState
    {
        private readonly ServerEnemyController _enemy;

        public FlyerIdleState(ServerEnemyController controller)
        {
            _enemy = controller;
        }

        public void EnterState()
        {
            _enemy.currentState = EnemyStateType.Idle;
            _enemy.Rb.gravityScale = 0f;
            _enemy.Rb.linearVelocity = Vector2.zero;
        }

        public void UpdateState()
        {
            Transform player = _enemy.FindNearestPlayer();
            if (player != null)
            {
                _enemy.Target = player;
                _enemy.ChangeState(new FlyerChaseState(_enemy));
            }
        }

        public void ExitState()
        {}
    }
}
