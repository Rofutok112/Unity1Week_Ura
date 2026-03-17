using UnityEngine;

namespace Projects.Scripts
{
    public sealed class ApplicationFrameRate : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 30;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }
}