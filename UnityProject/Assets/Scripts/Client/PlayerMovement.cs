using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    private PlayerData _playerData;
    private PlayerRenderer _playerRenderer;
    private Rigidbody2D _rb;
    private Collider2D _collider;
    private Animator _animator;

    private bool _enabled = false; // controlled by PlayerController

    private bool _isGrounded = false;
    private float _horizontalInput = 0f;

    private float _jumpBufferCounter = 0f;
    private float _coyoteTimeCounter = 0f;
    private float _jumpCooldownTimer = 0f;

    // ── Fall Damage ──────────────────────────────────────────
    /// <summary>Fired when the player lands with enough velocity to take damage. Parameter is the damage amount.</summary>
    public event Action<int> OnFallDamage;

    [SerializeField]
    private const float FallDamageVelocityThreshold = -15f; // safe landing speed
    [SerializeField]
    private const float FallDamageMultiplier = 2f;           // damage per unit of excess velocity
    private bool _wasGroundedLastFrame = true;
    private float _velocityYBeforeLanding;

    public void Initialize(PlayerData playerData, PlayerRenderer playerRenderer)
    {
        _playerData = playerData;
        _playerRenderer = playerRenderer;
        _rb = GetComponent<Rigidbody2D>();
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _collider = GetComponent<Collider2D>();
        _animator = GetComponent<Animator>();
        _enabled = true;
    }

    void Update()
    {
        if (!_enabled) return;

        ReadHorizontalInput();
        ReadJumpInput();

        if (_animator != null)
        {
            _animator.SetFloat("Speed", Mathf.Abs(_horizontalInput));
            _animator.SetBool("IsGrounded", _isGrounded);
        }
    }

    void FixedUpdate()
    {
        if (!_enabled) return;

        // Capture velocity BEFORE we check ground so we know impact speed.
        _velocityYBeforeLanding = _rb.linearVelocity.y;

        bool wasGrounded = _isGrounded;
        _isGrounded = CheckGrounded() && _jumpCooldownTimer <= 0f;

        // ── Fall Damage Detection ────────────────────────────
        if (_isGrounded && !_wasGroundedLastFrame)
        {
            if (_velocityYBeforeLanding < FallDamageVelocityThreshold)
            {
                float excessSpeed = Mathf.Abs(_velocityYBeforeLanding) - Mathf.Abs(FallDamageVelocityThreshold);
                int damage = Mathf.CeilToInt(excessSpeed * FallDamageMultiplier);
                if (damage > 0)
                {
                    OnFallDamage?.Invoke(damage);
                }
            }
        }
        _wasGroundedLastFrame = _isGrounded;

        // Store the current Y velocity that Unity's gravity engine calculated.
        float currentVelocityY = _rb.linearVelocity.y;

        // Update coyote time.
        if (_isGrounded)
            _coyoteTimeCounter = 0.1f;
        else
            _coyoteTimeCounter -= Time.fixedDeltaTime;

        // Jump: consume buffer + coyote window.
        if (_jumpBufferCounter > 0f && _coyoteTimeCounter > 0f)
        {
            currentVelocityY = _playerData.jumpForce;
            _isGrounded = false;

            _jumpBufferCounter = 0f;
            _coyoteTimeCounter = 0f;
            _jumpCooldownTimer = 0.15f;
        }

        // Apply: X from keyboard, Y from Unity gravity or jump.
        _rb.linearVelocity = new Vector2(_horizontalInput * _playerData.baseSpeed, currentVelocityY);
    }
    
    private void ReadHorizontalInput()
    {
        _horizontalInput = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            _horizontalInput -= 1f;
            _playerRenderer.ChangeDirection(true);
        }

        if (Input.GetKey(KeyCode.D))
        {
            _horizontalInput += 1f;
            _playerRenderer.ChangeDirection(false);
        }
    }

    private void ReadJumpInput()
    {
        _jumpCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space))
            _jumpBufferCounter = 0.15f;
        else
            _jumpBufferCounter -= Time.deltaTime;
    }

    private bool CheckGrounded()
    {
        if (_collider == null) return false;

        Bounds bounds = _collider.bounds;
        Vector2 size = new Vector2(bounds.size.x * 0.9f, 0.06f);
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y - 0.03f);

        Collider2D[] results = new Collider2D[5];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;

        int hitCount = Physics2D.OverlapBox(origin, size, 0f, filter, results);
        for (int i = 0; i < hitCount; i++)
        {
            if (results[i] != null
                && !results[i].transform.IsChildOf(transform)
                && results[i].gameObject != gameObject)
            {
                return true;
            }
        }

        return false;
    }
}
