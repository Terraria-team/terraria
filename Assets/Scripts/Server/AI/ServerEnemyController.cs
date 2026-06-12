using UnityEngine;
using Mirror;
using Shared.DataDefinitions;

namespace Server.AI
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ServerEnemyController : NetworkBehaviour
    {
        public Rigidbody2D Rb { get; private set; }
        private IEnemyState _currentState;

        // SyncVar ensures that when the server changes this state, clients will update automatically
        [SyncVar] 
        public EnemyStateType currentState = EnemyStateType.Idle;

        void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            // Start the default state only on the server
            ChangeState(new FighterPatrolState(this));
        }

        [ServerCallback]
        void Update()
        {
            // Execute the logic of the current state every frame on the server
            _currentState?.UpdateState();
        }

        [Server]
        public void ChangeState(IEnemyState newState)
        {
            _currentState?.ExitState();
            _currentState = newState;
            _currentState?.EnterState();
        }
    }
}