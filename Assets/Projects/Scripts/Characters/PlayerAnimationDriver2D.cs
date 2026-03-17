using UnityEngine;

namespace Projects.Scripts.Characters
{
    public sealed class PlayerAnimationDriver2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Animator Parameters")]
        [SerializeField] private string groundedParameter = "Grounded";
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string verticalSpeedParameter = "VerticalSpeed";
        [SerializeField] private string crouchingParameter = "Crouching";
        [SerializeField] private string runningParameter = "Running";
        [SerializeField] private string inputXParameter = "InputX";

        [Header("Visuals")]
        [SerializeField] private bool flipSpriteRenderer = true;

        private void Awake()
        {
            if (motor == null)
            {
                motor = GetComponentInParent<PlayerMotor2D>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void LateUpdate()
        {
            if (motor == null)
            {
                return;
            }

            CharacterMotionState state = motor.CurrentState;

            if (animator != null)
            {
                animator.SetBool(groundedParameter, state.IsGrounded);
                animator.SetFloat(moveSpeedParameter, state.HorizontalSpeed);
                animator.SetFloat(verticalSpeedParameter, state.Velocity.y);
                animator.SetBool(crouchingParameter, state.IsCrouching);
                animator.SetBool(runningParameter, state.IsRunning);
                animator.SetFloat(inputXParameter, state.MoveInput.x);
            }

            if (flipSpriteRenderer && spriteRenderer != null && Mathf.Abs(state.FacingDirection) > 0.01f)
            {
                spriteRenderer.flipX = state.FacingDirection < 0f;
            }
        }
    }
}
