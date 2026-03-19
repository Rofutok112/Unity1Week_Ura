using System;
using UnityEngine;

namespace Projects.Scripts.Characters
{
    [Serializable]
    public sealed class DelayedHomingSettings
    {
        [SerializeField, Min(0f)] private float homingDelay = 0.2f;
        [SerializeField, Min(0f)] private float lockOnRadius = 12f;
        [SerializeField, Range(0f, 180f)] private float maxLockAngle = 90f;
        [SerializeField, Min(0f)] private float turnRate = 720f;
        [SerializeField] private LayerMask targetMask = ~0;

        [Header("Noise")]
        [SerializeField, Min(0f)] private float noiseAmplitude = 15f;
        [SerializeField, Min(0.01f)] private float noiseFrequency = 3f;

        public float HomingDelay => homingDelay;
        public float LockOnRadius => lockOnRadius;
        public float MaxLockAngle => maxLockAngle;
        public float TurnRate => turnRate;
        public LayerMask TargetMask => targetMask;
        public float NoiseAmplitude => noiseAmplitude;
        public float NoiseFrequency => noiseFrequency;
    }
}
