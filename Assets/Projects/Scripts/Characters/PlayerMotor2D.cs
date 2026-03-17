using UnityEngine;

namespace Projects.Scripts.Characters
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        private const int GroundHitBufferSize = 8;

        [Header("References")]
        [SerializeField] private PlayerInputReader2D inputReader;
        [SerializeField] private Transform groundCheckPoint;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 1.35f;
        [SerializeField, Range(0.1f, 1f)] private float crouchSpeedMultiplier = 0.45f;
        [SerializeField, Min(0f)] private float groundAcceleration = 70f;
        [SerializeField, Min(0f)] private float groundDeceleration = 80f;
        [SerializeField, Min(0f)] private float airAcceleration = 35f;
        [SerializeField, Min(0f)] private float airDeceleration = 40f;

        [Header("Dash Sprint")]
        [SerializeField, Min(0.01f)] private float doubleTapWindow = 0.25f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.12f;
        [SerializeField, Min(1f)] private float dashSpeedMultiplier = 2.4f;
        [SerializeField, Min(0f)] private float dashAcceleration = 140f;
        [SerializeField, Range(0.1f, 1f)] private float moveTapThreshold = 0.6f;
        [SerializeField, Range(0f, 0.5f)] private float moveReleaseThreshold = 0.2f;

        [Header("Jump")]
        [SerializeField, Min(0.1f)] private float jumpHeight = 3.5f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.15f;
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 2.2f;
        [SerializeField, Min(1f)] private float lowJumpGravityMultiplier = 1.8f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 18f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.18f;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);

        private Rigidbody2D rb;
        private Collider2D bodyCollider;
        private CharacterInputFrame currentInput;
        private float facingDirection = 1f;
        private float coyoteCounter;
        private float jumpBufferCounter;
        private float lastTapTime = float.NegativeInfinity;
        private float dashTimer;
        private float previousMoveInputX;
        private int lastTapDirection;
        private int sprintLatchDirection;
        private int dashDirection;
        private bool isGrounded;
        private bool wasGrounded;
        private bool jumpConsumed;
        private readonly Collider2D[] groundHits = new Collider2D[GroundHitBufferSize];

        public CharacterMotionState CurrentState { get; private set; }

        public float FacingDirection => facingDirection;
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();

            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader2D>();
            }
        }

        private void Update()
        {
            currentInput = inputReader != null ? inputReader.CurrentFrame : default;
            UpdateDashSprintState();

            if (currentInput.Move.x > 0.01f)
            {
                facingDirection = 1f;
            }
            else if (currentInput.Move.x < -0.01f)
            {
                facingDirection = -1f;
            }

            if (currentInput.JumpPressed)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            UpdateGroundState();

            if (isGrounded)
            {
                coyoteCounter = coyoteTime;

                if (!wasGrounded)
                {
                    jumpConsumed = false;
                }
            }
            else
            {
                coyoteCounter -= Time.deltaTime;
            }

            wasGrounded = isGrounded;
            RefreshMotionState();
        }

        private void FixedUpdate()
        {
            UpdateHorizontalVelocity();
            TryConsumeJump();
            ApplyExtraGravity();
            ClampFallSpeed();
            RefreshMotionState();
        }

        private void UpdateHorizontalVelocity()
        {
            float moveInputX = currentInput.Move.x;
            float targetSpeed = moveInputX * moveSpeed;
            bool sprinting = IsSprintActive();

            if (dashTimer > 0f && dashDirection != 0)
            {
                targetSpeed = moveSpeed * dashSpeedMultiplier * dashDirection;
            }
            else if (sprinting)
            {
                targetSpeed *= sprintMultiplier;
            }

            if (currentInput.CrouchHeld && isGrounded)
            {
                targetSpeed *= crouchSpeedMultiplier;
            }

            float acceleration = Mathf.Abs(targetSpeed) > 0.01f
                ? (isGrounded ? groundAcceleration : airAcceleration)
                : (isGrounded ? groundDeceleration : airDeceleration);

            if (dashTimer > 0f)
            {
                acceleration = dashAcceleration;
                dashTimer = Mathf.Max(0f, dashTimer - Time.fixedDeltaTime);
            }

            float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
        }

        private void UpdateDashSprintState()
        {
            float moveInputX = currentInput.Move.x;
            int moveDirection = Mathf.Abs(moveInputX) >= moveTapThreshold ? (moveInputX > 0f ? 1 : -1) : 0;
            bool freshTap = moveDirection != 0 && Mathf.Abs(previousMoveInputX) < moveTapThreshold;

            if (freshTap)
            {
                if (moveDirection == lastTapDirection && Time.time - lastTapTime <= doubleTapWindow)
                {
                    dashTimer = dashDuration;
                    dashDirection = moveDirection;
                    sprintLatchDirection = moveDirection;
                }

                lastTapDirection = moveDirection;
                lastTapTime = Time.time;
            }

            if (Mathf.Abs(moveInputX) <= moveReleaseThreshold)
            {
                sprintLatchDirection = 0;
            }
            else if (sprintLatchDirection != 0 && Mathf.Sign(moveInputX) != sprintLatchDirection)
            {
                sprintLatchDirection = 0;
            }

            if (dashTimer <= 0f && sprintLatchDirection == 0)
            {
                dashDirection = 0;
            }

            previousMoveInputX = moveInputX;
        }

        private bool IsSprintActive()
        {
            if (currentInput.CrouchHeld)
            {
                return false;
            }

            if (currentInput.SprintHeld)
            {
                return Mathf.Abs(currentInput.Move.x) > 0.01f;
            }

            return sprintLatchDirection != 0 && Mathf.Sign(currentInput.Move.x) == sprintLatchDirection;
        }

        private void TryConsumeJump()
        {
            if (jumpConsumed || jumpBufferCounter <= 0f || coyoteCounter <= 0f)
            {
                return;
            }

            float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
            float jumpVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            jumpConsumed = true;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            isGrounded = false;
        }

        private void ApplyExtraGravity()
        {
            float verticalVelocity = rb.linearVelocity.y;

            if (verticalVelocity < 0f)
            {
                verticalVelocity += Physics2D.gravity.y * (fallGravityMultiplier - 1f) * rb.gravityScale * Time.fixedDeltaTime;
            }
            else if (verticalVelocity > 0f && !currentInput.JumpHeld)
            {
                verticalVelocity += Physics2D.gravity.y * (lowJumpGravityMultiplier - 1f) * rb.gravityScale * Time.fixedDeltaTime;
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, verticalVelocity);
        }

        private void ClampFallSpeed()
        {
            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
            }
        }

        private void UpdateGroundState()
        {
            Vector2 checkPosition = groundCheckPoint != null
                ? groundCheckPoint.position
                : (Vector2)transform.position + groundCheckOffset;

            ContactFilter2D contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundLayers,
                useTriggers = false
            };

            int hitCount = Physics2D.OverlapCircle(checkPosition, groundCheckRadius, contactFilter, groundHits);
            isGrounded = false;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = groundHits[i];
                groundHits[i] = null;

                if (hit == null || hit == bodyCollider)
                {
                    continue;
                }

                isGrounded = true;
                break;
            }
        }

        private void RefreshMotionState()
        {
            Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
            bool running = IsSprintActive() || dashTimer > 0f;
            bool crouching = currentInput.CrouchHeld && isGrounded;

            CurrentState = new CharacterMotionState(
                velocity,
                currentInput.Move,
                isGrounded,
                running,
                crouching,
                velocity.y > 0.05f && !isGrounded,
                velocity.y < -0.05f && !isGrounded,
                facingDirection);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector2 checkPosition = groundCheckPoint != null
                ? groundCheckPoint.position
                : (Vector2)transform.position + groundCheckOffset;
            Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        }
    }
}
