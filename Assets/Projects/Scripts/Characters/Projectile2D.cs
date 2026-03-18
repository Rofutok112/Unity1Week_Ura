using UnityEngine;

namespace Projects.Scripts.Characters
{
    public sealed class Projectile2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Transform owner;
        private Transform lockedTarget;
        private Collider2D ignoredCollider;
        private Vector2 direction;
        private Vector2 position;
        private float speed;
        private float damage;
        private float lifetimeRemaining;
        private float distanceRemaining;
        private float homingDelayRemaining;
        private LayerMask hitMask;
        private GameObject impactEffectPrefab;
        private ProjectileMovementType movementType;
        private DelayedHomingSettings delayedHoming;
        private ProjectileSpawner2D despawnTarget;

        public Projectile2D PrefabSource { get; private set; }

        public void SetPrefabSource(Projectile2D prefabSource)
        {
            PrefabSource = prefabSource;
        }

        public void Initialize(
            ProjectileSpawner2D despawnHandler,
            Transform projectileOwner,
            Collider2D projectileIgnoredCollider,
            Vector2 startPosition,
            Vector2 travelDirection,
            ProjectileDefinition2D definition)
        {
            despawnTarget = despawnHandler;
            owner = projectileOwner;
            ignoredCollider = projectileIgnoredCollider;
            position = startPosition;
            direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector2.right;
            speed = definition.Speed;
            damage = definition.Damage;
            lifetimeRemaining = definition.Lifetime;
            distanceRemaining = definition.MaxDistance;
            hitMask = definition.HitMask;
            impactEffectPrefab = definition.ImpactEffectPrefab;
            movementType = definition.MovementType;
            delayedHoming = definition.DelayedHoming;
            homingDelayRemaining = delayedHoming != null ? delayedHoming.HomingDelay : 0f;
            lockedTarget = movementType == ProjectileMovementType.DelayedHoming
                ? AcquireTarget()
                : null;

            transform.position = position;
            UpdateVisualRotation();

            gameObject.SetActive(true);
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            lifetimeRemaining -= deltaTime;

            if (lifetimeRemaining <= 0f || distanceRemaining <= 0f)
            {
                Despawn();
                return;
            }

            UpdateDirection(deltaTime);

            float stepDistance = Mathf.Min(speed * deltaTime, distanceRemaining);
            Vector2 nextPosition = position + direction * stepDistance;
            Vector2 castDirection = nextPosition - position;
            float castDistance = castDirection.magnitude;

            if (castDistance > 0f)
            {
                RaycastHit2D hit = Physics2D.Raycast(position, castDirection / castDistance, castDistance, hitMask);

                if (hit.collider != null && hit.collider != ignoredCollider)
                {
                    transform.position = hit.point;
                    SpawnImpactEffect(hit.point, hit.normal);
                    ApplyDamage(hit);
                    Despawn();
                    return;
                }
            }

            position = nextPosition;
            distanceRemaining -= stepDistance;
            transform.position = position;
            UpdateVisualRotation();
        }

        private void ApplyDamage(RaycastHit2D hit)
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                return;
            }

            DamageContext context = new DamageContext(owner, hit.point, direction, damage);
            damageable.ApplyDamage(context);
        }

        private void Despawn()
        {
            if (despawnTarget != null)
            {
                despawnTarget.Despawn(this);
                return;
            }

            gameObject.SetActive(false);
        }

        private void SpawnImpactEffect(Vector2 point, Vector2 normal)
        {
            if (impactEffectPrefab == null)
            {
                return;
            }

            if (despawnTarget != null)
            {
                despawnTarget.SpawnImpactEffect(impactEffectPrefab, point, normal, direction);
                return;
            }

            Vector2 facing = normal.sqrMagnitude > 0.0001f ? normal.normalized : direction;
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            Instantiate(impactEffectPrefab, point, Quaternion.Euler(0f, 0f, angle));
        }

        private void UpdateDirection(float deltaTime)
        {
            if (movementType != ProjectileMovementType.DelayedHoming || lockedTarget == null || delayedHoming == null)
            {
                return;
            }

            if (homingDelayRemaining > 0f)
            {
                homingDelayRemaining -= deltaTime;
                return;
            }

            Vector2 toTarget = (Vector2)lockedTarget.position - position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float maxTurnAngle = delayedHoming.TurnRate * deltaTime;
            float signedAngle = Vector2.SignedAngle(direction, toTarget.normalized);
            float clampedAngle = Mathf.Clamp(signedAngle, -maxTurnAngle, maxTurnAngle);
            direction = Rotate(direction, clampedAngle);
        }

        private Transform AcquireTarget()
        {
            if (delayedHoming == null || delayedHoming.LockOnRadius <= 0f)
            {
                return null;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(position, delayedHoming.LockOnRadius, delayedHoming.TargetMask);
            Transform bestTarget = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D candidate = hits[i];

                if (candidate == null || candidate == ignoredCollider)
                {
                    continue;
                }

                if (owner != null && candidate.transform.root == owner.root)
                {
                    continue;
                }

                Vector2 toCandidate = (Vector2)candidate.bounds.center - position;

                if (toCandidate.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                float angle = Vector2.Angle(direction, toCandidate);

                if (angle > delayedHoming.MaxLockAngle)
                {
                    continue;
                }

                float distance = toCandidate.magnitude;
                float score = -angle * 10f - distance;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate.transform;
                }
            }

            return bestTarget;
        }

        private void UpdateVisualRotation()
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
