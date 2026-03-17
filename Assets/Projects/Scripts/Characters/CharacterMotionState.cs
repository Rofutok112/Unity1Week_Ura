using UnityEngine;

namespace Projects.Scripts.Characters
{
    public readonly struct CharacterMotionState
    {
        public CharacterMotionState(
            Vector2 velocity,
            Vector2 moveInput,
            bool isGrounded,
            bool isRunning,
            bool isCrouching,
            bool isJumping,
            bool isFalling,
            float facingDirection)
        {
            Velocity = velocity;
            MoveInput = moveInput;
            IsGrounded = isGrounded;
            IsRunning = isRunning;
            IsCrouching = isCrouching;
            IsJumping = isJumping;
            IsFalling = isFalling;
            FacingDirection = facingDirection;
        }

        public Vector2 Velocity { get; }
        public Vector2 MoveInput { get; }
        public bool IsGrounded { get; }
        public bool IsRunning { get; }
        public bool IsCrouching { get; }
        public bool IsJumping { get; }
        public bool IsFalling { get; }
        public float FacingDirection { get; }
        public float HorizontalSpeed => Mathf.Abs(Velocity.x);
    }
}
