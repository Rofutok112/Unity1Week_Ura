using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.Characters
{
    public sealed class ProjectileSpawner2D : MonoBehaviour
    {
        [SerializeField, Min(1)] private int prewarmCount = 8;

        private readonly Dictionary<Projectile2D, Queue<Projectile2D>> pools = new Dictionary<Projectile2D, Queue<Projectile2D>>();
        private readonly Dictionary<GameObject, Queue<ImpactEffectPlayer>> impactPools = new Dictionary<GameObject, Queue<ImpactEffectPlayer>>();

        private void Awake()
        {
        }

        public void Spawn(
            Projectile2D projectilePrefab,
            Transform owner,
            Collider2D ignoredCollider,
            Vector2 origin,
            Vector2 direction,
            ProjectileDefinition2D definition)
        {
            if (projectilePrefab == null || definition == null)
            {
                Debug.LogWarning("Projectile spawn skipped because prefab or definition is missing.", this);
                return;
            }

            var pool = GetPool(projectilePrefab);
            var projectile = pool.Count > 0 ? pool.Dequeue() : CreateInstance(projectilePrefab);
            projectile.Initialize(this, owner, ignoredCollider, origin, direction, definition);
        }

        public void Despawn(Projectile2D projectile)
        {
            if (projectile == null)
            {
                return;
            }

            projectile.gameObject.SetActive(false);
            projectile.transform.SetParent(transform);
            if (projectile.PrefabSource == null)
            {
                return;
            }

            GetPool(projectile.PrefabSource).Enqueue(projectile);
        }

        public void SpawnImpactEffect(GameObject impactEffectPrefab, Vector2 position, Vector2 normal, Vector2 fallbackDirection)
        {
            if (impactEffectPrefab == null)
            {
                return;
            }

            var pool = GetImpactPool(impactEffectPrefab);
            var effect = pool.Count > 0 ? pool.Dequeue() : CreateImpactInstance(impactEffectPrefab);
            var facing = normal.sqrMagnitude > 0.0001f
                ? normal.normalized
                : (fallbackDirection.sqrMagnitude > 0.0001f ? fallbackDirection.normalized : Vector2.right);
            var angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            effect.transform.SetParent(transform);
            effect.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            effect.Play(this);
        }

        public void DespawnImpactEffect(ImpactEffectPlayer effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.gameObject.SetActive(false);
            effect.transform.SetParent(transform);
            if (effect.PrefabSource == null)
            {
                return;
            }

            GetImpactPool(effect.PrefabSource).Enqueue(effect);
        }

        private Queue<Projectile2D> GetPool(Projectile2D projectilePrefab)
        {
            if (pools.TryGetValue(projectilePrefab, out var pool))
            {
                return pool;
            }

            pool = new Queue<Projectile2D>();
            pools.Add(projectilePrefab, pool);

            for (var i = 0; i < prewarmCount; i++)
            {
                var projectile = CreateInstance(projectilePrefab);
                projectile.gameObject.SetActive(false);
                pool.Enqueue(projectile);
            }

            return pool;
        }

        private Queue<ImpactEffectPlayer> GetImpactPool(GameObject impactEffectPrefab)
        {
            if (impactPools.TryGetValue(impactEffectPrefab, out var pool))
            {
                return pool;
            }

            pool = new Queue<ImpactEffectPlayer>();
            impactPools.Add(impactEffectPrefab, pool);

            for (var i = 0; i < prewarmCount; i++)
            {
                var effect = CreateImpactInstance(impactEffectPrefab);
                effect.gameObject.SetActive(false);
                pool.Enqueue(effect);
            }

            return pool;
        }

        private Projectile2D CreateInstance(Projectile2D projectilePrefab)
        {
            var projectile = Instantiate(projectilePrefab, transform);
            projectile.SetPrefabSource(projectilePrefab);
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        private ImpactEffectPlayer CreateImpactInstance(GameObject impactEffectPrefab)
        {
            var effectObject = Instantiate(impactEffectPrefab, transform);
            var effect = effectObject.GetComponent<ImpactEffectPlayer>();

            if (effect == null)
            {
                effect = effectObject.AddComponent<ImpactEffectPlayer>();
            }

            effect.SetPrefabSource(impactEffectPrefab);
            effect.gameObject.SetActive(false);
            return effect;
        }
    }
}
