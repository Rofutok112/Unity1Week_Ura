using UnityEngine;

namespace Projects.Scripts.Characters
{
    public sealed class WeaponController2D : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition2D currentWeapon;
        [SerializeField] private ProjectileSpawner2D projectileSpawner;

        private float nextFireTime;
        private int burstShotsRemaining;
        private float nextBurstTime;

        public WeaponDefinition2D CurrentWeapon => currentWeapon;

        private void Awake()
        {
            if (projectileSpawner == null)
            {
                projectileSpawner = FindFirstObjectByType<ProjectileSpawner2D>();
            }
        }

        public void TickFire(Transform owner, Collider2D ignoredCollider, Vector2 origin, Vector2 aimDirection, bool firePressed, bool fireHeld)
        {
            if (currentWeapon == null || currentWeapon.ProjectileDefinition == null || projectileSpawner == null)
            {
                return;
            }

            switch (currentWeapon.FireMode)
            {
                case WeaponFireMode.SemiAuto:
                    if (firePressed)
                    {
                        TryFireNow(owner, ignoredCollider, origin, aimDirection);
                    }
                    break;

                case WeaponFireMode.FullAuto:
                    if (fireHeld)
                    {
                        TryFireNow(owner, ignoredCollider, origin, aimDirection);
                    }
                    break;

                case WeaponFireMode.Burst:
                    HandleBurst(owner, ignoredCollider, origin, aimDirection, firePressed);
                    break;
            }
        }

        public void SetWeapon(WeaponDefinition2D weaponDefinition)
        {
            currentWeapon = weaponDefinition;
            nextFireTime = 0f;
            burstShotsRemaining = 0;
            nextBurstTime = 0f;
        }

        private void HandleBurst(Transform owner, Collider2D ignoredCollider, Vector2 origin, Vector2 aimDirection, bool firePressed)
        {
            if (firePressed && Time.time >= nextFireTime)
            {
                burstShotsRemaining = currentWeapon.BurstCount;
                nextBurstTime = Time.time;
                nextFireTime = Time.time + currentWeapon.FireInterval;
            }

            if (burstShotsRemaining <= 0 || Time.time < nextBurstTime)
            {
                return;
            }

            Fire(owner, ignoredCollider, origin, aimDirection);
            burstShotsRemaining--;
            nextBurstTime = Time.time + currentWeapon.BurstInterval;
        }

        private void TryFireNow(Transform owner, Collider2D ignoredCollider, Vector2 origin, Vector2 aimDirection)
        {
            if (Time.time < nextFireTime)
            {
                return;
            }

            Fire(owner, ignoredCollider, origin, aimDirection);
            nextFireTime = Time.time + currentWeapon.FireInterval;
        }

        private void Fire(Transform owner, Collider2D ignoredCollider, Vector2 origin, Vector2 aimDirection)
        {
            Vector2 normalizedAim = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
            int pelletCount = Mathf.Max(1, currentWeapon.PelletsPerShot);

            if (pelletCount == 1)
            {
                projectileSpawner.Spawn(owner, ignoredCollider, origin, normalizedAim, currentWeapon.ProjectileDefinition);
                return;
            }

            float spread = currentWeapon.SpreadAngle;
            float step = pelletCount > 1 ? spread / (pelletCount - 1) : 0f;
            float startAngle = -spread * 0.5f;

            for (int i = 0; i < pelletCount; i++)
            {
                float angle = startAngle + step * i;
                Vector2 pelletDirection = Rotate(normalizedAim, angle);
                projectileSpawner.Spawn(owner, ignoredCollider, origin, pelletDirection, currentWeapon.ProjectileDefinition);
            }
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }
    }
}
