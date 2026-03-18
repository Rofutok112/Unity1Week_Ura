using UnityEngine;

namespace Projects.Scripts.Characters
{
    [CreateAssetMenu(fileName = "ProjectileDefinition2D", menuName = "Game/Combat/Projectile Definition 2D")]
    public sealed class ProjectileDefinition2D : ScriptableObject
    {
        [SerializeField] private ProjectileMovementType movementType;
        [SerializeField] private GameObject impactEffectPrefab;
        [SerializeField, Min(0.01f)] private float speed = 18f;
        [SerializeField, Min(0.01f)] private float lifetime = 1.5f;
        [SerializeField, Min(0.01f)] private float maxDistance = 30f;
        [SerializeField, Min(0f)] private float damage = 1f;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private DelayedHomingSettings delayedHoming = new DelayedHomingSettings();

        public ProjectileMovementType MovementType => movementType;
        public GameObject ImpactEffectPrefab => impactEffectPrefab;
        public float Speed => speed;
        public float Lifetime => lifetime;
        public float MaxDistance => maxDistance;
        public float Damage => damage;
        public LayerMask HitMask => hitMask;
        public DelayedHomingSettings DelayedHoming => delayedHoming;
    }
}
