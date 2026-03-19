using UnityEngine;

namespace Projects.Scripts.Characters
{
    public sealed class HomingTarget2D : MonoBehaviour
    {
        [SerializeField] private Transform aimPoint;
        [SerializeField] private bool canBeTargeted = true;
        [SerializeField] private int priority;

        public Transform AimPoint => aimPoint != null ? aimPoint : transform;
        public bool CanBeTargeted => canBeTargeted;
        public int Priority => priority;
    }
}
