using UnityEngine;
using Mirror;
using Shared.Components;

namespace Server.AI
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ProjectileController : NetworkBehaviour
    {
        [SyncVar] public float damage   = 15f;
        [SyncVar] public float lifetime = 4f;
        [SyncVar] public bool passesThroughWalls = false;
        
        private LayerMask _blockingLayer;
        private Rigidbody2D _rb;
        private Vector2 _direction;
        private float _speed;
        private float _timer;

        public void Initialize(Vector2 direction, float speed, float damage, float lifetime, bool passesThroughWalls, LayerMask blockingLayer)
        {
            _direction    = direction.normalized;
            _speed        = speed;
            this.damage   = damage;
            this.lifetime = lifetime;
            this.passesThroughWalls = passesThroughWalls;
            _blockingLayer = blockingLayer;
            
            if (_direction != Vector2.zero)
            {
                transform.up = _direction;
            }
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;

            var col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        [ServerCallback]
        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= lifetime)
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        [ServerCallback]
        void FixedUpdate()
        {
            _rb.linearVelocity = _direction * _speed;

            if (!passesThroughWalls && _blockingLayer != 0)
            {
                float castDist = _speed * Time.fixedDeltaTime + 0.15f;
                RaycastHit2D hit = Physics2D.Raycast(
                    (Vector2)transform.position,
                    _direction,
                    castDist,
                    _blockingLayer
                );
                if (hit.collider != null)
                {
                    NetworkServer.Destroy(gameObject);
                }
            }
        }

        [ServerCallback]
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var health = other.GetComponent<HealthComponent>();
                if (health != null)
                    health.ApplyDamageServerRpc((int)damage);

                NetworkServer.Destroy(gameObject);
                return;
            }

            if (!passesThroughWalls)
            {
                if (((1 << other.gameObject.layer) & _blockingLayer) != 0)
                {
                    NetworkServer.Destroy(gameObject);
                }
            }
        }
    }
}
