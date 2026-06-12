using UnityEngine;

namespace Server.AI
{
    public class FighterPatrolState : IEnemyState
    {
        private ServerEnemyController _enemy;

        public FighterPatrolState(ServerEnemyController controller)
        {
            this._enemy =  controller;
        }

        public void EnterState()
        {
            _enemy.currentState = Shared.DataDefinitions.EnemyStateType.Patrol;
        }

        public void UpdateState()
        {
            
        }

        public void ExitState()
        {
            
        }
    }
}