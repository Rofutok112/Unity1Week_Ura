using UnityEngine;

namespace Projects.Scripts.Characters
{
    [CreateAssetMenu(fileName = "WeaponDefinition2D", menuName = "Game/Combat/Weapon Definition 2D")]
    public sealed class WeaponDefinition2D : ScriptableObject
    {
        [SerializeField] private ProjectileDefinition2D projectileDefinition;
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private Vector2 muzzleLocalOffset;
        [SerializeField] private AimConstraintMode aimConstraintMode;
        [SerializeField, Range(0f, 180f)] private float maxAimAngle = 45f;
        [SerializeField, Range(1f, 180f)] private float snapStepAngle = 45f;
        [SerializeField] private WeaponLaunchDirectionMode launchDirectionMode;
        [SerializeField] private Vector2 launchDirectionLocal = Vector2.right;
        [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.FullAuto;
        [SerializeField, Min(0.01f)] private float fireInterval = 0.12f;
        [SerializeField, Min(1)] private int pelletsPerShot = 1;
        [SerializeField, Min(0f)] private float spreadAngle = 2f;
        [SerializeField, Range(0f, 1f)] private float accuracy = 0.9f;
        [SerializeField, Min(1)] private int burstCount = 3;
        [SerializeField, Min(0.01f)] private float burstInterval = 0.06f;
        [SerializeField] private AudioClip fireAudioClip;
        [SerializeField, Range(0f, 1f)] private float fireAudioVolume = 1f;

        public ProjectileDefinition2D ProjectileDefinition => projectileDefinition;
        public Projectile2D ProjectilePrefab => projectilePrefab;
        public Vector2 MuzzleLocalOffset => muzzleLocalOffset;
        public AimConstraintMode AimConstraintMode => aimConstraintMode;
        public float MaxAimAngle => maxAimAngle;
        public float SnapStepAngle => snapStepAngle;
        public WeaponLaunchDirectionMode LaunchDirectionMode => launchDirectionMode;
        public Vector2 LaunchDirectionLocal => launchDirectionLocal;
        public WeaponFireMode FireMode => fireMode;
        public float FireInterval => fireInterval;
        public int PelletsPerShot => pelletsPerShot;
        public float SpreadAngle => spreadAngle;
        public float Accuracy => accuracy;
        public int BurstCount => burstCount;
        public float BurstInterval => burstInterval;
        public AudioClip FireAudioClip => fireAudioClip;
        public float FireAudioVolume => fireAudioVolume;
    }
}
