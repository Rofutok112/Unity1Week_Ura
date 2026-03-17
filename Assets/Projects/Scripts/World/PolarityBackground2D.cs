using UnityEngine;

namespace Projects.Scripts.World
{
    public sealed class PolarityBackground2D : MonoBehaviour
    {
        [SerializeField] private WorldPolarityService polarityService;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Color whiteBackgroundColor = Color.white;
        [SerializeField] private Color blackBackgroundColor = Color.black;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (polarityService == null)
            {
                polarityService = WorldPolarityService.Instance;
            }
        }

        private void OnEnable()
        {
            if (polarityService == null)
            {
                polarityService = WorldPolarityService.Instance ?? FindFirstObjectByType<WorldPolarityService>();
            }

            if (polarityService == null)
            {
                return;
            }

            polarityService.PolarityChanged += ApplyBackground;
            ApplyBackground(polarityService.CurrentPolarity);
        }

        private void OnDisable()
        {
            if (polarityService != null)
            {
                polarityService.PolarityChanged -= ApplyBackground;
            }
        }

        private void ApplyBackground(WorldPolarity polarity)
        {
            if (targetCamera == null)
            {
                return;
            }

            targetCamera.backgroundColor = polarity == WorldPolarity.White
                ? whiteBackgroundColor
                : blackBackgroundColor;
        }
    }
}
