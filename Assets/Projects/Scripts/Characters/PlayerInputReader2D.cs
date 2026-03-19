using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Projects.Scripts.Characters
{
    public sealed class PlayerInputReader2D : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset actionsAsset;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string crouchActionName = "Crouch";
        [SerializeField] private string attackActionName = "Attack";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string togglePolarityActionName = "TogglePolarity";

        private InputActionMap playerActionMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction attackAction;
        private InputAction interactAction;
        private InputAction togglePolarityAction;

        public CharacterInputFrame CurrentFrame { get; private set; }

        public event Action AttackPressed;
        public event Action InteractPressed;
        public event Action TogglePolarityPressed;

        private void Awake()
        {
            ResolveActions();
        }

        private void OnEnable()
        {
            ResolveActions();
            playerActionMap?.Enable();
        }

        private void OnDisable()
        {
            playerActionMap?.Disable();
            CurrentFrame = default;
        }

        private void Update()
        {
            if (playerActionMap == null)
            {
                CurrentFrame = default;
                return;
            }

            var attackTriggered = attackAction != null && attackAction.WasPressedThisFrame();
            var attackHeld = attackAction != null && attackAction.IsPressed();
            var interactTriggered = interactAction != null && interactAction.WasPressedThisFrame();
            var togglePolarityTriggered = IsTogglePolarityPressedThisFrame();

            CurrentFrame = new CharacterInputFrame(
                moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero,
                lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero,
                jumpAction != null && jumpAction.WasPressedThisFrame(),
                jumpAction != null && jumpAction.IsPressed(),
                sprintAction != null && sprintAction.IsPressed(),
                crouchAction != null && crouchAction.IsPressed(),
                attackTriggered,
                attackHeld,
                interactTriggered,
                togglePolarityTriggered);

            if (attackTriggered)
            {
                AttackPressed?.Invoke();
            }

            if (interactTriggered)
            {
                InteractPressed?.Invoke();
            }

            if (togglePolarityTriggered)
            {
                TogglePolarityPressed?.Invoke();
            }
        }

        private void ResolveActions()
        {
            moveAction = null;
            lookAction = null;
            jumpAction = null;
            sprintAction = null;
            crouchAction = null;
            attackAction = null;
            interactAction = null;
            togglePolarityAction = null;

            if (actionsAsset == null)
            {
                playerActionMap = null;
                return;
            }

            playerActionMap = actionsAsset.FindActionMap(actionMapName, false);

            if (playerActionMap == null)
            {
                Debug.LogError($"Action map '{actionMapName}' was not found.", this);
                return;
            }

            moveAction = playerActionMap.FindAction(moveActionName, false);
            lookAction = playerActionMap.FindAction(lookActionName, false);
            jumpAction = playerActionMap.FindAction(jumpActionName, false);
            sprintAction = playerActionMap.FindAction(sprintActionName, false);
            crouchAction = playerActionMap.FindAction(crouchActionName, false);
            attackAction = playerActionMap.FindAction(attackActionName, false);
            interactAction = playerActionMap.FindAction(interactActionName, false);
            togglePolarityAction = playerActionMap.FindAction(togglePolarityActionName, false);
        }

        private bool IsTogglePolarityPressedThisFrame()
        {
            if (togglePolarityAction != null)
            {
                return togglePolarityAction.WasPressedThisFrame();
            }

            return Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
        }
    }
}
