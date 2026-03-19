using UnityEngine;

namespace Projects.Scripts.Characters
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        private const int CastHitBufferSize = 8;

        [Header("References")]
        [SerializeField] private PlayerInputReader2D inputReader;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 1.35f;
        [SerializeField, Range(0.1f, 1f)] private float crouchSpeedMultiplier = 0.45f;
        [SerializeField, Min(0f)] private float groundAcceleration = 70f;
        [SerializeField, Min(0f)] private float groundDeceleration = 80f;
        [SerializeField, Min(0f)] private float airAcceleration = 35f;
        [SerializeField, Min(0f)] private float airDeceleration = 40f;
        [SerializeField, Min(0f)] private float gravityScale = 3f;

        [Header("Collision")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0.001f)] private float collisionSkinWidth = 0.02f;
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.08f;
        [SerializeField, Range(0.1f, 1f)] private float groundNormalThreshold = 0.6f;

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

        private readonly RaycastHit2D[] castHits = new RaycastHit2D[CastHitBufferSize];
        private readonly Collider2D[] overlapHits = new Collider2D[CastHitBufferSize];
        private readonly Collider2D[] groundHits = new Collider2D[CastHitBufferSize];

        private Collider2D bodyCollider;
        private CharacterInputFrame currentInput;
        private ContactFilter2D collisionFilter;
        private Vector2 velocity;
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

        public CharacterMotionState CurrentState { get; private set; }

        public float FacingDirection => facingDirection;
        public bool IsGrounded => isGrounded;

        public void SetFacingDirection(float direction)
        {
            if (direction > 0.01f)
            {
                facingDirection = 1f;
            }
            else if (direction < -0.01f)
            {
                facingDirection = -1f;
            }
        }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider2D>();

            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader2D>();
            }

            collisionFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundLayers,
                useTriggers = false
            };
        }

        private void Update()
        {
            currentInput = inputReader != null ? inputReader.CurrentFrame : default;
            UpdateDashSprintState();

            if (currentInput.JumpPressed)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.deltaTime);
            }

            RefreshMotionState();
        }

        private void FixedUpdate()
        {
            Physics2D.SyncTransforms();
            UpdateGroundState();
            UpdateGroundTimers(Time.fixedDeltaTime);
            UpdateHorizontalVelocity();
            TryConsumeJump();
            ApplyExtraGravity();
            ClampFallSpeed();
            MoveCharacter(Time.fixedDeltaTime);
            ResolveOverlaps();
            UpdateGroundState();
            UpdateGroundTimers(0f);

            if (isGrounded && velocity.y <= 0f)
            {
                velocity.y = 0f;
            }

            RefreshMotionState();
        }

        private void UpdateHorizontalVelocity()
        {
            var moveInputX = currentInput.Move.x;
            var targetSpeed = moveInputX * moveSpeed;
            var sprinting = IsSprintActive();

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

            var acceleration = Mathf.Abs(targetSpeed) > 0.01f
                ? (isGrounded ? groundAcceleration : airAcceleration)
                : (isGrounded ? groundDeceleration : airDeceleration);

            if (dashTimer > 0f)
            {
                acceleration = dashAcceleration;
                dashTimer = Mathf.Max(0f, dashTimer - Time.fixedDeltaTime);
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
        }

        private void UpdateDashSprintState()
        {
            var moveInputX = currentInput.Move.x;
            var moveDirection = Mathf.Abs(moveInputX) >= moveTapThreshold ? (moveInputX > 0f ? 1 : -1) : 0;
            var freshTap = moveDirection != 0 && Mathf.Abs(previousMoveInputX) < moveTapThreshold;

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
            else if (sprintLatchDirection != 0 && !Mathf.Approximately(Mathf.Sign(moveInputX), sprintLatchDirection))
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

            return sprintLatchDirection != 0 && Mathf.Approximately(Mathf.Sign(currentInput.Move.x), sprintLatchDirection);
        }

        private void TryConsumeJump()
        {
            if (jumpConsumed || jumpBufferCounter <= 0f || coyoteCounter <= 0f)
            {
                return;
            }

            var gravity = Mathf.Abs(Physics2D.gravity.y * gravityScale);
            var jumpVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

            velocity.y = jumpVelocity;
            jumpConsumed = true;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            isGrounded = false;
        }

        private void ApplyExtraGravity()
        {
            if (isGrounded && velocity.y <= 0f)
            {
                velocity.y = 0f;
                return;
            }

            var gravityStep = Physics2D.gravity.y * gravityScale * Time.fixedDeltaTime;
            var gravityMultiplier = 1f;

            if (velocity.y < 0f)
            {
                gravityMultiplier = fallGravityMultiplier;
            }
            else if (velocity.y > 0f && !currentInput.JumpHeld)
            {
                gravityMultiplier = lowJumpGravityMultiplier;
            }

            velocity.y += gravityStep * gravityMultiplier;
        }

        private void ClampFallSpeed()
        {
            if (velocity.y < -maxFallSpeed)
            {
                velocity.y = -maxFallSpeed;
            }
        }

        private void MoveCharacter(float deltaTime)
        {
            MoveAlongAxis(Vector2.right, velocity.x * deltaTime, false);
            MoveAlongAxis(Vector2.up, velocity.y * deltaTime, true);
        }

        private void MoveAlongAxis(Vector2 axis, float distance, bool vertical)
        {
            if (Mathf.Abs(distance) <= 0.0001f)
            {
                return;
            }

            var direction = Mathf.Sign(distance) * axis;
            var castDistance = Mathf.Abs(distance) + collisionSkinWidth;
            var hitCount = bodyCollider.Cast(direction, collisionFilter, castHits, castDistance);
            var allowedDistance = Mathf.Abs(distance);

            for (var i = 0; i < hitCount; i++)
            {
                var hit = castHits[i];

                if (hit.collider == null)
                {
                    continue;
                }

                var candidateDistance = Mathf.Max(0f, hit.distance - collisionSkinWidth);
                allowedDistance = Mathf.Min(allowedDistance, candidateDistance);

                if (vertical && direction.y < 0f && hit.normal.y >= groundNormalThreshold)
                {
                    isGrounded = true;
                }
            }

            transform.position += (Vector3)(direction * allowedDistance);
            Physics2D.SyncTransforms();

            if (allowedDistance + 0.0001f < Mathf.Abs(distance))
            {
                if (vertical)
                {
                    velocity.y = 0f;
                }
                else
                {
                    velocity.x = 0f;
                }
            }
        }

        private void UpdateGroundState()
        {
            isGrounded = false;
            var hitCount = bodyCollider.Cast(Vector2.down, collisionFilter, castHits, groundProbeDistance + collisionSkinWidth);

            for (var i = 0; i < hitCount; i++)
            {
                var hit = castHits[i];

                if (hit.collider == null || hit.normal.y < groundNormalThreshold)
                {
                    continue;
                }

                isGrounded = true;
                break;
            }

            if (isGrounded)
            {
                return;
            }

            var bounds = bodyCollider.bounds;
            var checkCenter = new Vector2(bounds.center.x, bounds.min.y - (groundProbeDistance * 0.5f));
            var checkSize = new Vector2(
                Mathf.Max(0.01f, bounds.size.x - collisionSkinWidth * 2f),
                Mathf.Max(0.01f, groundProbeDistance + collisionSkinWidth));
            var groundHitCount = Physics2D.OverlapBox(checkCenter, checkSize, 0f, collisionFilter, groundHits);

            for (var i = 0; i < groundHitCount; i++)
            {
                var hit = groundHits[i];
                groundHits[i] = null;

                if (hit == null || hit == bodyCollider)
                {
                    continue;
                }

                isGrounded = true;
                break;
            }
        }

        private void ResolveOverlaps()
        {
            var hitCount = Physics2D.OverlapCollider(bodyCollider, collisionFilter, overlapHits);

            for (var i = 0; i < hitCount; i++)
            {
                var hit = overlapHits[i];
                overlapHits[i] = null;

                if (hit == null || hit == bodyCollider)
                {
                    continue;
                }

                var distance = bodyCollider.Distance(hit);

                if (distance.isOverlapped)
                {
                    transform.position += (Vector3)(distance.normal * distance.distance);
                    Physics2D.SyncTransforms();

                    if (distance.normal.y >= groundNormalThreshold)
                    {
                        isGrounded = true;

                        if (velocity.y < 0f)
                        {
                            velocity.y = 0f;
                        }
                    }
                }
            }
        }

        private void UpdateGroundTimers(float deltaTime)
        {
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
                coyoteCounter = Mathf.Max(0f, coyoteCounter - deltaTime);
            }

            wasGrounded = isGrounded;
        }

        private void RefreshMotionState()
        {
            var running = IsSprintActive() || dashTimer > 0f;
            var crouching = currentInput.CrouchHeld && isGrounded;

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

            if (bodyCollider != null && Application.isPlaying)
            {
                var bounds = bodyCollider.bounds;
                Vector2 checkCenter = new Vector2(bounds.center.x, bounds.min.y - (groundProbeDistance * 0.5f));
                Vector2 checkSize = new Vector2(
                    Mathf.Max(0.01f, bounds.size.x - collisionSkinWidth * 2f),
                    Mathf.Max(0.01f, groundProbeDistance + collisionSkinWidth));
                Gizmos.DrawWireCube(checkCenter, checkSize);
                return;
            }
        }
    }
}
