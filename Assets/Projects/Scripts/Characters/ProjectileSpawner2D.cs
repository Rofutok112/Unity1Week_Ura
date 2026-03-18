using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.Characters
{
    public sealed class ProjectileSpawner2D : MonoBehaviour
    {
        [SerializeField, Min(1)] private int prewarmCount = 8;

        private readonly Dictionary<Projectile2D, Queue<Projectile2D>> pools = new Dictionary<Projectile2D, Queue<Projectile2D>>();

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

            Queue<Projectile2D> pool = GetPool(projectilePrefab);
            Projectile2D projectile = pool.Count > 0 ? pool.Dequeue() : CreateInstance(projectilePrefab);
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

        private Queue<Projectile2D> GetPool(Projectile2D projectilePrefab)
        {
            if (pools.TryGetValue(projectilePrefab, out Queue<Projectile2D> pool))
            {
                return pool;
            }

            pool = new Queue<Projectile2D>();
            pools.Add(projectilePrefab, pool);

            for (int i = 0; i < prewarmCount; i++)
            {
                Projectile2D projectile = CreateInstance(projectilePrefab);
                projectile.gameObject.SetActive(false);
                pool.Enqueue(projectile);
            }

            return pool;
        }

        private Projectile2D CreateInstance(Projectile2D projectilePrefab)
        {
            Projectile2D projectile = Instantiate(projectilePrefab, transform);
            projectile.SetPrefabSource(projectilePrefab);
            projectile.gameObject.SetActive(false);
            return projectile;
        }
    }
}
