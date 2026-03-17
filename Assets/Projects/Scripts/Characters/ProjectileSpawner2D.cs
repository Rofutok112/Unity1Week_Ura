using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.Characters
{
    public sealed class ProjectileSpawner2D : MonoBehaviour
    {
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField, Min(1)] private int prewarmCount = 8;

        private readonly Queue<Projectile2D> pool = new Queue<Projectile2D>();

        private void Awake()
        {
            Prewarm();
        }

        public void Spawn(
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

            Projectile2D projectile = pool.Count > 0 ? pool.Dequeue() : CreateInstance();
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
            pool.Enqueue(projectile);
        }

        private void Prewarm()
        {
            for (int i = 0; i < prewarmCount; i++)
            {
                Projectile2D projectile = CreateInstance();
                projectile.gameObject.SetActive(false);
                pool.Enqueue(projectile);
            }
        }

        private Projectile2D CreateInstance()
        {
            Projectile2D projectile = Instantiate(projectilePrefab, transform);
            projectile.gameObject.SetActive(false);
            return projectile;
        }
    }
}
