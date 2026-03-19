using Projects.Scripts.Audio;
using UnityEngine;
using System.Collections.Generic;

namespace Projects.Scripts.Characters
{
    public sealed class Projectile2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Transform owner;
        private Transform lockedTarget;
        private readonly HashSet<HomingTarget2D> targetBuffer = new HashSet<HomingTarget2D>();
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
        private AudioClip impactAudioClip;
        private float impactAudioVolume;
        private ProjectileMovementType movementType;
        private DelayedHomingSettings delayedHoming;
        private ProjectileSpawner2D despawnTarget;
        private float noiseTime;
        private float noiseSeed;

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
            impactAudioClip = definition.ImpactAudioClip;
            impactAudioVolume = definition.ImpactAudioVolume;
            movementType = definition.MovementType;
            delayedHoming = definition.DelayedHoming;
            homingDelayRemaining = delayedHoming != null ? delayedHoming.HomingDelay : 0f;
            noiseTime = 0f;
            noiseSeed = Random.Range(0f, 1000f);
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
            var deltaTime = Time.deltaTime;
            lifetimeRemaining -= deltaTime;

            if (lifetimeRemaining <= 0f || distanceRemaining <= 0f)
            {
                Despawn();
                return;
            }

            UpdateDirection(deltaTime);

            var stepDistance = Mathf.Min(speed * deltaTime, distanceRemaining);
            var nextPosition = position + direction * stepDistance;
            var castDirection = nextPosition - position;
            var castDistance = castDirection.magnitude;

            if (castDistance > 0f)
            {
                var hit = Physics2D.Raycast(position, castDirection / castDistance, castDistance, hitMask);

                if (hit.collider != null && hit.collider != ignoredCollider)
                {
                    transform.position = hit.point;
                    SpawnImpactEffect(hit.point, hit.normal);
                    AudioManager.PlayOneShotAtPosition(impactAudioClip, hit.point, impactAudioVolume);
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
            var damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                return;
            }

            var context = new DamageContext(owner, hit.point, direction, damage);
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

            var facing = normal.sqrMagnitude > 0.0001f ? normal.normalized : direction;
            var angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            Instantiate(impactEffectPrefab, point, Quaternion.Euler(0f, 0f, angle));
        }

        private void UpdateDirection(float deltaTime)
        {
            if (movementType != ProjectileMovementType.DelayedHoming || delayedHoming == null)
            {
                return;
            }

            noiseTime += deltaTime;

            if (homingDelayRemaining > 0f)
            {
                homingDelayRemaining -= deltaTime;
                ApplyNoise(deltaTime);
                return;
            }

            if (lockedTarget == null)
            {
                lockedTarget = AcquireTarget();

                if (lockedTarget == null)
                {
                    ApplyNoise(deltaTime);
                    return;
                }
            }

            var toTarget = (Vector2)lockedTarget.position - position;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var maxTurnAngle = delayedHoming.TurnRate * deltaTime;
            var signedAngle = Vector2.SignedAngle(direction, toTarget.normalized);
            var clampedAngle = Mathf.Clamp(signedAngle, -maxTurnAngle, maxTurnAngle);
            direction = Rotate(direction, clampedAngle);

            ApplyNoise(deltaTime);
        }

        private void ApplyNoise(float deltaTime)
        {
            if (delayedHoming == null || delayedHoming.NoiseAmplitude <= 0f)
            {
                return;
            }

            var noise = Mathf.PerlinNoise1D(noiseSeed + noiseTime * delayedHoming.NoiseFrequency);
            var noiseAngle = (noise - 0.5f) * 2f * delayedHoming.NoiseAmplitude * deltaTime;
            direction = Rotate(direction, noiseAngle);
        }

        private Transform AcquireTarget()
        {
            if (delayedHoming == null || delayedHoming.LockOnRadius <= 0f)
            {
                return null;
            }

            var hits = Physics2D.OverlapCircleAll(position, delayedHoming.LockOnRadius, delayedHoming.TargetMask);
            targetBuffer.Clear();
            HomingTarget2D bestTarget = null;
            var bestScore = float.NegativeInfinity;

            for (var i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i];

                if (candidate == null || candidate == ignoredCollider)
                {
                    continue;
                }

                if (owner != null && candidate.transform.IsChildOf(owner))
                {
                    continue;
                }

                var homingTarget = candidate.GetComponentInParent<HomingTarget2D>();

                if (homingTarget == null || !homingTarget.CanBeTargeted || !targetBuffer.Add(homingTarget))
                {
                    continue;
                }

                if (owner != null && homingTarget.transform.IsChildOf(owner))
                {
                    continue;
                }

                var targetPosition = (Vector2)homingTarget.AimPoint.position;
                var toCandidate = targetPosition - position;

                if (toCandidate.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                var angle = Vector2.Angle(direction, toCandidate);

                if (angle > delayedHoming.MaxLockAngle)
                {
                    continue;
                }

                var distance = toCandidate.magnitude;
                var score = homingTarget.Priority * 1000f - angle * 10f - distance;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = homingTarget;
                }
            }

            return bestTarget != null ? bestTarget.AimPoint : null;
        }

        private void UpdateVisualRotation()
        {
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
