using UnityEngine;
using Mirror;
using Shared.DataDefinitions;

namespace Server.AI
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(NetworkIdentity))]
    public class ServerEnemyController : NetworkBehaviour
    {
        // ── Components ────────────────────────────────────────────────
        public Rigidbody2D Rb { get; private set; }
        public Collider2D Col { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────
        [Header("Config")]
        [SerializeField] public EnemyData Data;
        [SerializeField] private EnemyBehaviorType _behaviorType = EnemyBehaviorType.Fighter;
        public EnemyBehaviorType BehaviorType => _behaviorType;

        [Header("Shooter only")]
        [SerializeField] private GameObject _projectilePrefab;

        [Header("Layers")]
        // Assign the "Ground" / tilemap layer here
        [SerializeField] private LayerMask _groundLayer;
        // Assign the "Player" layer here
        [SerializeField] private LayerMask _playerLayer;
        // Tiles that block projectile line-of-sight (usually same as _groundLayer)
        [SerializeField] private LayerMask _blockingLayer;

        // ── Networking ────────────────────────────────────────────────
        // SyncVar ensures clients see the correct animation state
        [SyncVar]
        public EnemyStateType currentState = EnemyStateType.Idle;

        // ── Runtime ───────────────────────────────────────────────────
        public Transform Target { get; set; }
        public LayerMask BlockingLayer => _blockingLayer;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public void SetProjectilePrefab(GameObject prefab) => _projectilePrefab = prefab;

        private IEnemyState _currentState;

        // ── Unity Lifecycle ───────────────────────────────────────────
        void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            Col = GetComponent<Collider2D>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            DispatchInitialState();
        }

        [ServerCallback]
        void Update()
        {
            _currentState?.UpdateState();
        }

        // ── FSM ───────────────────────────────────────────────────────
        [Server]
        public void ChangeState(IEnemyState newState)
        {
            _currentState?.ExitState();
            _currentState = newState;
            _currentState?.EnterState();
        }

        [Server]
        private void DispatchInitialState()
        {
            switch (_behaviorType)
            {
                case EnemyBehaviorType.Fighter:
                    ChangeState(new FighterPatrolState(this));
                    break;
                case EnemyBehaviorType.Flyer:
                    ChangeState(new FlyerIdleState(this));
                    break;
                case EnemyBehaviorType.Slime:
                    ChangeState(new SlimeIdleState(this));
                    break;
                case EnemyBehaviorType.Shooter:
                    ChangeState(new ShooterState(this));
                    break;
            }
        }

        // ── Navigation Helpers ────────────────────────────────────────
        [Server]
        public bool IsGrounded()
        {
            if (Col == null) return false;
            Bounds bounds = Col.bounds;
            Vector2 size = new Vector2(bounds.size.x * 0.9f, 0.06f);
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y - 0.03f);

            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            Collider2D[] results = new Collider2D[4];
            int hitCount = Physics2D.OverlapBox(origin, size, 0f, filter, results);

            for (int i = 0; i < hitCount; i++)
            {
                if (results[i] != null
                    && !results[i].transform.IsChildOf(transform)
                    && results[i].gameObject != gameObject)
                    return true;
            }
            return false;
        }

        [Server]
        public bool CheckWallAhead(Vector2 direction)
        {
            if (Col == null || Data == null) return false;
            Bounds b = Col.bounds;
            Vector2 origin = new Vector2(b.center.x, b.min.y + Data.wallCheckHeight);
            return Physics2D.Raycast(origin, direction.normalized, Data.stepCheckDistance, _groundLayer);
        }

        //True = obstacle reaches above jump height
        [Server]
        public bool CheckHeadClear(Vector2 direction)
        {
            if (Col == null || Data == null) return false;
            Bounds b = Col.bounds;
            Vector2 origin = new Vector2(b.center.x, b.min.y + Data.headClearHeight);
            return Physics2D.Raycast(origin, direction.normalized, Data.stepCheckDistance, _groundLayer);
        }

        //Ray straight down slightly ahead of the foot.
        [Server]
        public bool CheckGapAhead(Vector2 direction)
        {
            if (Col == null || Data == null) return false;
            Bounds b = Col.bounds;
            float xOffset = direction.normalized.x * (b.extents.x + 0.2f);
            Vector2 origin = new Vector2(b.center.x + xOffset, b.min.y);
            return Physics2D.Raycast(origin, Vector2.down, Data.gapCheckDepth, _groundLayer);
        }

        // Finds the nearest player collider within DetectionRange using the Player layer
        [Server]
        public Transform FindNearestPlayer()
        {
            if (Data == null) return null;
            Collider2D[] hits = new Collider2D[8];
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, Data.detectionRange, hits, _playerLayer);

            Transform nearest = null;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (hits[i] == null) continue;
                float dist = Vector2.Distance(transform.position, hits[i].transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = hits[i].transform;
                }
            }
            return nearest;
        }
    }
}