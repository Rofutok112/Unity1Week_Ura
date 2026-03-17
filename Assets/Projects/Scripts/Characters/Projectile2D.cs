using UnityEngine;

namespace Projects.Scripts.Characters
{
    public sealed class Projectile2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Transform owner;
        private Collider2D ignoredCollider;
        private Vector2 direction;
        private Vector2 position;
        private float speed;
        private float damage;
        private float lifetimeRemaining;
        private float distanceRemaining;
        private LayerMask hitMask;
        private ProjectileSpawner2D despawnTarget;

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

            transform.position = position;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

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
                    ApplyDamage(hit);
                    Despawn();
                    return;
                }
            }

            position = nextPosition;
            distanceRemaining -= stepDistance;
            transform.position = position;
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
    }
}
