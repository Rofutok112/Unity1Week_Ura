using UnityEngine;

namespace Projects.Scripts.Characters
{
    public readonly struct DamageContext
    {
        public DamageContext(Transform instigator, Vector2 point, Vector2 direction, float damage)
        {
            Instigator = instigator;
            Point = point;
            Direction = direction;
            Damage = damage;
        }

        public Transform Instigator { get; }
        public Vector2 Point { get; }
        public Vector2 Direction { get; }
        public float Damage { get; }
    }
}
