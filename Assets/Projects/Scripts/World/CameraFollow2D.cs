using Projects.Scripts.Characters;
using UnityEngine;

namespace Projects.Scripts.World
{
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset;
        [SerializeField, Min(0f)] private float followSmoothTime = 0.12f;

        private Vector3 currentVelocity;

        private void Awake()
        {
            if (target == null)
            {
                var playerMotor = FindFirstObjectByType<PlayerMotor2D>();

                if (playerMotor != null)
                {
                    target = playerMotor.transform;
                }
            }
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desiredPosition = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z);

            if (followSmoothTime <= 0f)
            {
                transform.position = desiredPosition;
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref currentVelocity,
                followSmoothTime);
        }
    }
}
