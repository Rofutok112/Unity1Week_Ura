using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Projects.Scripts.Characters
{
    public sealed class Shooter2D : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader2D inputReader;
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private WeaponController2D weaponController;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Collider2D ignoredCollider;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Camera screenCamera;
        [SerializeField] private RawImage targetRawImage;

        [Header("Aim Constraint")]
        [SerializeField] private AimConstraintMode aimConstraintMode;
        [SerializeField, Range(0f, 180f)] private float maxAimAngle = 45f;
        [SerializeField, Range(1f, 180f)] private float snapStepAngle = 45f;

        [Header("Facing Visuals")]
        [SerializeField] private bool mirrorMuzzleWithFacing = true;

        [SerializeField, Min(0.01f)] private float lookInputThreshold = 0.25f;

        private Vector3 initialMuzzleLocalPosition;
        private Vector3 initialMuzzleLocalScale;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader2D>();
            }

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor2D>();
            }

            if (weaponController == null)
            {
                weaponController = GetComponent<WeaponController2D>();
            }

            if (ignoredCollider == null)
            {
                ignoredCollider = GetComponent<Collider2D>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (screenCamera == null)
            {
                screenCamera = Camera.main;
            }

            if (muzzle != null)
            {
                initialMuzzleLocalPosition = muzzle.localPosition;
                initialMuzzleLocalScale = muzzle.localScale;
            }
        }

        private void Update()
        {
            if (inputReader == null || weaponController == null)
            {
                return;
            }

            CharacterInputFrame input = inputReader.CurrentFrame;
            Vector2 aimDirection = ResolveAimDirection(input);
            UpdateFacingVisuals();
            Vector2 origin = muzzle != null ? muzzle.position : transform.position;

            weaponController.TickFire(
                transform,
                ignoredCollider,
                origin,
                aimDirection,
                input.AttackPressed,
                input.AttackHeld);
        }

        private void UpdateFacingVisuals()
        {
            if (!mirrorMuzzleWithFacing || muzzle == null || motor == null)
            {
                return;
            }

            float facingSign = motor.FacingDirection >= 0f ? 1f : -1f;

            muzzle.localPosition = new Vector3(
                Mathf.Abs(initialMuzzleLocalPosition.x) * facingSign,
                initialMuzzleLocalPosition.y,
                initialMuzzleLocalPosition.z);

            muzzle.localScale = new Vector3(
                Mathf.Abs(initialMuzzleLocalScale.x) * facingSign,
                initialMuzzleLocalScale.y,
                initialMuzzleLocalScale.z);
        }

        private Vector2 ResolveAimDirection(CharacterInputFrame input)
        {
            Vector2 origin = muzzle != null ? muzzle.position : (Vector2)transform.position;
            Vector2 resolvedDirection;

            if (TryGetMouseAimDirection(origin, out Vector2 mouseAimDirection))
            {
                resolvedDirection = mouseAimDirection;
            }
            else if (input.Look.sqrMagnitude >= lookInputThreshold * lookInputThreshold)
            {
                resolvedDirection = input.Look.normalized;
            }
            else if (muzzle != null)
            {
                resolvedDirection = muzzle.right;
            }
            else if (motor != null)
            {
                resolvedDirection = new Vector2(motor.FacingDirection, 0f);
            }
            else
            {
                resolvedDirection = Vector2.right;
            }

            UpdateFacingFromAimDirection(resolvedDirection);
            return ApplyAimConstraint(resolvedDirection);
        }

        private bool TryGetMouseAimDirection(Vector2 origin, out Vector2 aimDirection)
        {
            aimDirection = default;

            if (worldCamera == null || Mouse.current == null)
            {
                return false;
            }

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 viewportPoint;
            
            // RawImageが指定されている場合は、その上でマウス位置をビューポート座標に変換する
            if (targetRawImage != null)
            {
                if (!TryGetViewportPointFromRawImage(mouseScreenPosition, out Vector2 rawImageViewport))
                {
                    return false;
                }

                viewportPoint = new Vector3(rawImageViewport.x, rawImageViewport.y, 0f);
            }
            else
            {
                if (screenCamera == null)
                {
                    screenCamera = Camera.main;
                }

                if (screenCamera == null)
                {
                    return false;
                }

                viewportPoint = screenCamera.ScreenToViewportPoint(mouseScreenPosition);
            }

            float depth = worldCamera.WorldToViewportPoint(origin).z;
            Vector3 mouseWorldPosition = worldCamera.ViewportToWorldPoint(new Vector3(
                viewportPoint.x,
                viewportPoint.y,
                depth));

            Vector2 directionToMouse = (Vector2)mouseWorldPosition - origin;

            if (directionToMouse.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            aimDirection = directionToMouse.normalized;
            return true;
        }

        private bool TryGetViewportPointFromRawImage(Vector2 screenPosition, out Vector2 viewportPoint)
        {
            viewportPoint = default;

            RectTransform rectTransform = targetRawImage.rectTransform;
            Canvas canvas = targetRawImage.canvas;
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, uiCamera, out Vector2 localPoint))
            {
                return false;
            }

            Rect rect = rectTransform.rect;

            if (!rect.Contains(localPoint))
            {
                return false;
            }

            viewportPoint = new Vector2(
                Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));

            return true;
        }

        private Vector2 ApplyAimConstraint(Vector2 aimDirection)
        {
            if (aimDirection.sqrMagnitude <= 0.0001f)
            {
                return GetForwardDirection();
            }

            Vector2 forward = GetForwardDirection();

            switch (aimConstraintMode)
            {
                case AimConstraintMode.ForwardOnly:
                    return forward;

                case AimConstraintMode.ClampAngle:
                    return ClampAimDirection(aimDirection.normalized, forward);

                case AimConstraintMode.SnapDirections:
                    return SnapAimDirection(aimDirection.normalized, forward);

                default:
                    return aimDirection.normalized;
            }
        }

        private void UpdateFacingFromAimDirection(Vector2 aimDirection)
        {
            if (motor == null)
            {
                return;
            }

            float horizontalOffset = aimDirection.x;

            if (horizontalOffset > 0.01f)
            {
                motor.SetFacingDirection(1f);
            }
            else if (horizontalOffset < -0.01f)
            {
                motor.SetFacingDirection(-1f);
            }
        }

        private Vector2 GetForwardDirection()
        {
            if (motor != null)
            {
                return new Vector2(motor.FacingDirection, 0f);
            }

            if (muzzle != null)
            {
                return muzzle.right.normalized;
            }

            return Vector2.right;
        }

        private Vector2 ClampAimDirection(Vector2 aimDirection, Vector2 forward)
        {
            float signedAngle = Vector2.SignedAngle(forward, aimDirection);
            float clampedAngle = Mathf.Clamp(signedAngle, -maxAimAngle, maxAimAngle);
            return Rotate(forward, clampedAngle);
        }

        private Vector2 SnapAimDirection(Vector2 aimDirection, Vector2 forward)
        {
            float signedAngle = Vector2.SignedAngle(forward, aimDirection);
            float snappedAngle = Mathf.Round(signedAngle / snapStepAngle) * snapStepAngle;
            return Rotate(forward, snappedAngle);
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos).normalized;
        }
    }
}
