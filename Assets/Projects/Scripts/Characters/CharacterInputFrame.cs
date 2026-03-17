using UnityEngine;

namespace Projects.Scripts.Characters
{
    public readonly struct CharacterInputFrame
    {
        public CharacterInputFrame(
            Vector2 move,
            Vector2 look,
            bool jumpPressed,
            bool jumpHeld,
            bool sprintHeld,
            bool crouchHeld,
            bool attackPressed,
            bool attackHeld,
            bool interactPressed,
            bool togglePolarityPressed)
        {
            Move = move;
            Look = look;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
            SprintHeld = sprintHeld;
            CrouchHeld = crouchHeld;
            AttackPressed = attackPressed;
            AttackHeld = attackHeld;
            InteractPressed = interactPressed;
            TogglePolarityPressed = togglePolarityPressed;
        }

        public Vector2 Move { get; }
        public Vector2 Look { get; }
        public bool JumpPressed { get; }
        public bool JumpHeld { get; }
        public bool SprintHeld { get; }
        public bool CrouchHeld { get; }
        public bool AttackPressed { get; }
        public bool AttackHeld { get; }
        public bool InteractPressed { get; }
        public bool TogglePolarityPressed { get; }
    }
}
