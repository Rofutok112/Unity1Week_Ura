using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Projects.Scripts.Characters
{
    public sealed class Shooter2D : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader2D inputReader;
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private WeaponController2D primaryWeaponController;
        [SerializeField] private WeaponController2D secondaryWeaponController;
        [SerializeField] private Transform muzzleRoot;
        [SerializeField] private Collider2D ignoredCollider;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Camera screenCamera;
        [SerializeField] private RawImage targetRawImage;

        [Header("Facing Visuals")]
        [SerializeField] private bool mirrorMuzzleWithFacing = true;

        [SerializeField, Min(0.01f)] private float lookInputThreshold = 0.25f;

        private Vector3 initialMuzzleRootLocalPosition;
        private Vector3 initialMuzzleRootLocalScale;

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

            if (primaryWeaponController == null)
            {
                primaryWeaponController = GetComponent<WeaponController2D>();
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

            if (muzzleRoot != null)
            {
                initialMuzzleRootLocalPosition = muzzleRoot.localPosition;
                initialMuzzleRootLocalScale = muzzleRoot.localScale;
            }
        }

        private void Update()
        {
            if (inputReader == null)
            {
                return;
            }

            var input = inputReader.CurrentFrame;
            var rawAimDirection = ResolveRawAimDirection(input);
            UpdateFacingVisuals();
            rawAimDirection = ResolveRawAimDirection(input);

            FireWeapon(primaryWeaponController, rawAimDirection, input.AttackPressed, input.AttackHeld);
            FireWeapon(secondaryWeaponController, rawAimDirection, input.InteractPressed, input.InteractPressed);
        }

        private void UpdateFacingVisuals()
        {
            if (!mirrorMuzzleWithFacing || muzzleRoot == null || motor == null)
            {
                return;
            }

            var facingSign = motor.FacingDirection >= 0f ? 1f : -1f;

            muzzleRoot.localPosition = new Vector3(
                Mathf.Abs(initialMuzzleRootLocalPosition.x) * facingSign,
                initialMuzzleRootLocalPosition.y,
                initialMuzzleRootLocalPosition.z);

            muzzleRoot.localScale = new Vector3(
                Mathf.Abs(initialMuzzleRootLocalScale.x) * facingSign,
                initialMuzzleRootLocalScale.y,
                initialMuzzleRootLocalScale.z);
        }

        private Vector2 ResolveRawAimDirection(CharacterInputFrame input)
        {
            Vector2 origin = muzzleRoot != null ? muzzleRoot.position : (Vector2)transform.position;
            Vector2 resolvedDirection;

            if (TryGetMouseAimDirection(origin, out var mouseAimDirection))
            {
                resolvedDirection = mouseAimDirection;
            }
            else if (input.Look.sqrMagnitude >= lookInputThreshold * lookInputThreshold)
            {
                resolvedDirection = input.Look.normalized;
            }
            else if (muzzleRoot != null)
            {
                resolvedDirection = muzzleRoot.right;
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
            return resolvedDirection.normalized;
        }

        private bool TryGetMouseAimDirection(Vector2 origin, out Vector2 aimDirection)
        {
            aimDirection = default;

            if (worldCamera == null || Mouse.current == null)
            {
                return false;
            }

            var mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 viewportPoint;

            if (targetRawImage != null)
            {
                if (!TryGetViewportPointFromRawImage(mouseScreenPosition, out var rawImageViewport))
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

            var depth = worldCamera.WorldToViewportPoint(origin).z;
            var mouseWorldPosition = worldCamera.ViewportToWorldPoint(new Vector3(
                viewportPoint.x,
                viewportPoint.y,
                depth));

            var directionToMouse = (Vector2)mouseWorldPosition - origin;

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

            var rectTransform = targetRawImage.rectTransform;
            var canvas = targetRawImage.canvas;
            var uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, uiCamera, out var localPoint))
            {
                return false;
            }

            var rect = rectTransform.rect;

            if (!rect.Contains(localPoint))
            {
                return false;
            }

            viewportPoint = new Vector2(
                Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));

            return true;
        }

        private Vector2 ApplyAimConstraint(WeaponDefinition2D weaponDefinition, Vector2 aimDirection)
        {
            if (aimDirection.sqrMagnitude <= 0.0001f)
            {
                return GetForwardDirection();
            }

            var forward = GetForwardDirection();
            var constraintMode = weaponDefinition != null
                ? weaponDefinition.AimConstraintMode
                : AimConstraintMode.Free;

            switch (constraintMode)
            {
                case AimConstraintMode.ForwardOnly:
                    return forward;

                case AimConstraintMode.ClampAngle:
                    return ClampAimDirection(aimDirection.normalized, forward, weaponDefinition != null ? weaponDefinition.MaxAimAngle : 45f);

                case AimConstraintMode.SnapDirections:
                    return SnapAimDirection(aimDirection.normalized, forward, weaponDefinition != null ? weaponDefinition.SnapStepAngle : 45f);

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

            var horizontalOffset = aimDirection.x;

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

            if (muzzleRoot != null)
            {
                return muzzleRoot.right.normalized;
            }

            return Vector2.right;
        }

        private void FireWeapon(WeaponController2D weaponController, Vector2 rawAimDirection, bool firePressed, bool fireHeld)
        {
            if (weaponController == null || weaponController.CurrentWeapon == null)
            {
                return;
            }

            var weaponDefinition = weaponController.CurrentWeapon;
            var origin = ResolveMuzzleOrigin(weaponDefinition);
            var constrainedAimDirection = ApplyAimConstraint(weaponDefinition, rawAimDirection);
            var launchDirection = ResolveLaunchDirection(weaponDefinition, constrainedAimDirection);

            weaponController.TickFire(
                transform,
                ignoredCollider,
                origin,
                launchDirection,
                firePressed,
                fireHeld);
        }

        private Vector2 ResolveMuzzleOrigin(WeaponDefinition2D weaponDefinition)
        {
            Vector2 baseOrigin = muzzleRoot != null ? muzzleRoot.position : (Vector2)transform.position;

            if (weaponDefinition == null || muzzleRoot == null)
            {
                return baseOrigin;
            }

            var localOffset = weaponDefinition.MuzzleLocalOffset;
            return muzzleRoot.TransformPoint(new Vector3(localOffset.x, localOffset.y, 0f));
        }

        private Vector2 ResolveLaunchDirection(WeaponDefinition2D weaponDefinition, Vector2 constrainedAimDirection)
        {
            if (weaponDefinition == null)
            {
                return constrainedAimDirection;
            }

            switch (weaponDefinition.LaunchDirectionMode)
            {
                case WeaponLaunchDirectionMode.FixedLocalDirection:
                    return TransformLocalDirection(weaponDefinition.LaunchDirectionLocal);

                default:
                    return constrainedAimDirection;
            }
        }

        private Vector2 TransformLocalDirection(Vector2 localDirection)
        {
            var forward = GetForwardDirection();
            var up = Vector2.up;
            var worldDirection = forward * localDirection.x + up * localDirection.y;
            return worldDirection.sqrMagnitude > 0.0001f ? worldDirection.normalized : forward;
        }

        private Vector2 ClampAimDirection(Vector2 aimDirection, Vector2 forward, float maxAngle)
        {
            var signedAngle = Vector2.SignedAngle(forward, aimDirection);
            var clampedAngle = Mathf.Clamp(signedAngle, -maxAngle, maxAngle);
            return Rotate(forward, clampedAngle);
        }

        private Vector2 SnapAimDirection(Vector2 aimDirection, Vector2 forward, float stepAngle)
        {
            var signedAngle = Vector2.SignedAngle(forward, aimDirection);
            var snappedAngle = Mathf.Round(signedAngle / stepAngle) * stepAngle;
            return Rotate(forward, snappedAngle);
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos).normalized;
        }
    }
}
