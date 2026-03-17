using UnityEngine;

namespace Projects.Scripts.Characters
{
    [CreateAssetMenu(fileName = "WeaponDefinition2D", menuName = "Game/Combat/Weapon Definition 2D")]
    public sealed class WeaponDefinition2D : ScriptableObject
    {
        [SerializeField] private ProjectileDefinition2D projectileDefinition;
        [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.FullAuto;
        [SerializeField, Min(0.01f)] private float fireInterval = 0.12f;
        [SerializeField, Min(1)] private int pelletsPerShot = 1;
        [SerializeField, Min(0f)] private float spreadAngle = 2f;
        [SerializeField, Range(0f, 1f)] private float accuracy = 0.9f;
        [SerializeField, Min(1)] private int burstCount = 3;
        [SerializeField, Min(0.01f)] private float burstInterval = 0.06f;

        public ProjectileDefinition2D ProjectileDefinition => projectileDefinition;
        public WeaponFireMode FireMode => fireMode;
        public float FireInterval => fireInterval;
        public int PelletsPerShot => pelletsPerShot;
        public float SpreadAngle => spreadAngle;
        public float Accuracy => accuracy;
        public int BurstCount => burstCount;
        public float BurstInterval => burstInterval;
    }
}
