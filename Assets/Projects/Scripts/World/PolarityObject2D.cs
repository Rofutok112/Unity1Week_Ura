using UnityEngine;

namespace Projects.Scripts.World
{
    public sealed class PolarityObject2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldPolarityService polarityService;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D[] collidersToToggle;

        [Header("Identity")]
        [SerializeField] private WorldPolarity basePolarity = WorldPolarity.White;

        [Header("Colors")]
        [SerializeField] private Color whiteColor = Color.white;
        [SerializeField] private Color blackColor = Color.black;

        [Header("Gameplay")]
        [SerializeField] private bool disableCollidersWhenBlendingIntoBackground;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
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

            polarityService.PolarityChanged += ApplyPolarity;
            ApplyPolarity(polarityService.CurrentPolarity);
        }

        private void OnDisable()
        {
            if (polarityService != null)
            {
                polarityService.PolarityChanged -= ApplyPolarity;
            }
        }

        private void ApplyPolarity(WorldPolarity worldPolarity)
        {
            WorldPolarity visiblePolarity = worldPolarity == WorldPolarity.White
                ? basePolarity
                : Invert(basePolarity);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = visiblePolarity == WorldPolarity.White ? whiteColor : blackColor;
            }

            if (!disableCollidersWhenBlendingIntoBackground || collidersToToggle == null)
            {
                return;
            }

            bool shouldEnableCollider = visiblePolarity != worldPolarity;

            for (int i = 0; i < collidersToToggle.Length; i++)
            {
                if (collidersToToggle[i] != null)
                {
                    collidersToToggle[i].enabled = shouldEnableCollider;
                }
            }
        }

        private static WorldPolarity Invert(WorldPolarity polarity)
        {
            return polarity == WorldPolarity.White ? WorldPolarity.Black : WorldPolarity.White;
        }
    }
}
