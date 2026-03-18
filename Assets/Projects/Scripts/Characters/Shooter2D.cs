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

            CharacterInputFrame input = inputReader.CurrentFrame;
            Vector2 rawAimDirection = ResolveRawAimDirection(input);
            UpdateFacingVisuals();

            FireWeapon(primaryWeaponController, rawAimDirection, input.AttackPressed, input.AttackHeld);
            FireWeapon(secondaryWeaponController, rawAimDirection, input.InteractPressed, input.InteractPressed);
        }

        private void UpdateFacingVisuals()
        {
            if (!mirrorMuzzleWithFacing || muzzleRoot == null || motor == null)
            {
                return;
            }

            float facingSign = motor.FacingDirection >= 0f ? 1f : -1f;

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

            if (TryGetMouseAimDirection(origin, out Vector2 mouseAimDirection))
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

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 viewportPoint;

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

        private Vector2 ApplyAimConstraint(WeaponDefinition2D weaponDefinition, Vector2 aimDirection)
        {
            if (aimDirection.sqrMagnitude <= 0.0001f)
            {
                return GetForwardDirection();
            }

            Vector2 forward = GetForwardDirection();
            AimConstraintMode constraintMode = weaponDefinition != null
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

            WeaponDefinition2D weaponDefinition = weaponController.CurrentWeapon;
            Vector2 origin = ResolveMuzzleOrigin(weaponDefinition);
            Vector2 constrainedAimDirection = ApplyAimConstraint(weaponDefinition, rawAimDirection);
            Vector2 launchDirection = ResolveLaunchDirection(weaponDefinition, constrainedAimDirection);

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

            if (weaponDefinition == null)
            {
                return baseOrigin;
            }

            Vector2 forward = GetForwardDirection();
            Vector2 up = Vector2.up;
            Vector2 localOffset = weaponDefinition.MuzzleLocalOffset;
            return baseOrigin + forward * localOffset.x + up * localOffset.y;
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
            Vector2 forward = GetForwardDirection();
            Vector2 up = Vector2.up;
            Vector2 worldDirection = forward * localDirection.x + up * localDirection.y;
            return worldDirection.sqrMagnitude > 0.0001f ? worldDirection.normalized : forward;
        }

        private Vector2 ClampAimDirection(Vector2 aimDirection, Vector2 forward, float maxAngle)
        {
            float signedAngle = Vector2.SignedAngle(forward, aimDirection);
            float clampedAngle = Mathf.Clamp(signedAngle, -maxAngle, maxAngle);
            return Rotate(forward, clampedAngle);
        }

        private Vector2 SnapAimDirection(Vector2 aimDirection, Vector2 forward, float stepAngle)
        {
            float signedAngle = Vector2.SignedAngle(forward, aimDirection);
            float snappedAngle = Mathf.Round(signedAngle / stepAngle) * stepAngle;
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
