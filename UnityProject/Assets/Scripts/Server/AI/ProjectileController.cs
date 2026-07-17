using UnityEngine;
using Mirror;

namespace Server.AI
{
    // Travels in a straight line, destroys itself on player hit or lifetime expiry.
    // Synced to clients via SyncVars - clients see it move automatically.

    // Setup in Unity:
    //   - Add Rigidbody2D (Gravity Scale = 0)
    //   - Add Collider2D with IsTrigger = true
    //   - Tag the Player GameObject as "Player"
    //   - Register this prefab in the NetworkManager's Spawnable Prefabs list
    [RequireComponent(typeof(Rigidbody2D))]
    public class ProjectileController : NetworkBehaviour
    {
        [SyncVar] public float damage   = 15f;
        [SyncVar] public float lifetime = 4f;

        private Rigidbody2D _rb;
        private Vector2 _direction;
        private float _speed;
        private float _timer;

        // Called by ShooterState immediately after Instantiate, before NetworkServer.Spawn.
        public void Initialize(Vector2 direction, float speed, float damage, float lifetime)
        {
            _direction    = direction.normalized;
            _speed        = speed;
            this.damage   = damage;
            this.lifetime = lifetime;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
        }

        [ServerCallback]
        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= lifetime)
                NetworkServer.Destroy(gameObject);
        }

        [ServerCallback]
        void FixedUpdate()
        {
            _rb.linearVelocity = _direction * _speed;
        }

        [ServerCallback]
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            // TODO: call other.GetComponent<HealthComponent>()?.TakeDamage(damage)
            Debug.Log($"[Projectile] Hit player! Damage: {damage}");
            NetworkServer.Destroy(gameObject);
        }
    }
}
