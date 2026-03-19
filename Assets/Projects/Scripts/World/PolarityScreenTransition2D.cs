using System.Collections;
using Projects.Scripts.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace Projects.Scripts.World
{
    public sealed class PolarityScreenTransition2D : MonoBehaviour
    {
        [SerializeField] private WorldPolarityService polarityService;
        [SerializeField] private RawImage targetRawImage;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform transitionCenter;
        [SerializeField] private Shader transitionShader;
        [SerializeField] private Color sourceBackgroundColor = Color.black;
        [SerializeField, Min(0.0001f)] private float edgeSoftness = 0.03f;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int EffectEnabledId = Shader.PropertyToID("_EffectEnabled");
        private static readonly int BaseInvertId = Shader.PropertyToID("_BaseInvert");
        private static readonly int TargetInvertId = Shader.PropertyToID("_TargetInvert");

        private Coroutine transitionRoutine;
        private Material runtimeMaterial;

        private void Awake()
        {
            if (polarityService == null)
            {
                polarityService = WorldPolarityService.Instance ?? FindFirstObjectByType<WorldPolarityService>();
            }

            if (targetRawImage == null)
            {
                targetRawImage = GetComponent<RawImage>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (transitionCenter == null)
            {
                var motor = FindFirstObjectByType<PlayerMotor2D>();
                transitionCenter = motor != null ? motor.transform : null;
            }

            if (transitionShader == null)
            {
                transitionShader = Shader.Find("UI/PolarityTransition");
            }

            EnsureMaterial();
            ApplyBasePolarity();
            ApplyIdleState();
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

            polarityService.TransitionStarted += HandleTransitionStarted;
            polarityService.TransitionCompleted += HandleTransitionCompleted;
            polarityService.PolarityChanged += HandlePolarityChanged;
        }

        private void OnDisable()
        {
            if (polarityService != null)
            {
                polarityService.TransitionStarted -= HandleTransitionStarted;
                polarityService.TransitionCompleted -= HandleTransitionCompleted;
                polarityService.PolarityChanged -= HandlePolarityChanged;
            }
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        private void HandleTransitionStarted(WorldPolarity fromPolarity, WorldPolarity toPolarity, float duration)
        {
            if (!EnsureMaterial())
            {
                return;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            runtimeMaterial.SetFloat(BaseInvertId, ToInvertValue(fromPolarity));
            runtimeMaterial.SetFloat(TargetInvertId, ToInvertValue(toPolarity));
            transitionRoutine = StartCoroutine(AnimateTransition(duration));
        }

        private void HandleTransitionCompleted(WorldPolarity polarity)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            ApplyBasePolarity();
            ApplyIdleState();
        }

        private void HandlePolarityChanged(WorldPolarity polarity)
        {
            ApplyBasePolarity();
        }

        private IEnumerator AnimateTransition(float duration)
        {
            runtimeMaterial.SetFloat(EffectEnabledId, 1f);
            runtimeMaterial.SetFloat(EdgeSoftnessId, edgeSoftness);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Vector2 center = ResolveViewportCenter();
                float aspect = ResolveAspect();
                float maxRadius = ResolveMaxRadius(center, aspect);
                runtimeMaterial.SetVector(CenterId, center);
                runtimeMaterial.SetFloat(AspectId, aspect);
                runtimeMaterial.SetFloat(RadiusId, duration <= 0f ? maxRadius : Mathf.Lerp(0f, maxRadius, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            Vector2 finalCenter = ResolveViewportCenter();
            float finalAspect = ResolveAspect();
            runtimeMaterial.SetVector(CenterId, finalCenter);
            runtimeMaterial.SetFloat(AspectId, finalAspect);
            runtimeMaterial.SetFloat(RadiusId, ResolveMaxRadius(finalCenter, finalAspect));
            transitionRoutine = null;
        }

        private void ApplyIdleState()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            runtimeMaterial.SetFloat(EffectEnabledId, 0f);
            runtimeMaterial.SetFloat(RadiusId, 0f);
            runtimeMaterial.SetFloat(EdgeSoftnessId, edgeSoftness);
            runtimeMaterial.SetFloat(AspectId, ResolveAspect());
            runtimeMaterial.SetVector(CenterId, ResolveViewportCenter());
            runtimeMaterial.SetFloat(TargetInvertId, runtimeMaterial.GetFloat(BaseInvertId));
        }

        private bool EnsureMaterial()
        {
            if (targetRawImage == null || transitionShader == null)
            {
                return false;
            }

            if (runtimeMaterial == null)
            {
                runtimeMaterial = new Material(transitionShader);
            }

            if (targetRawImage.texture != null)
            {
                runtimeMaterial.SetTexture(MainTexId, targetRawImage.texture);
            }

            if (targetRawImage.material != runtimeMaterial)
            {
                targetRawImage.material = runtimeMaterial;
            }

            return true;
        }

        private void ApplyBasePolarity()
        {
            if (runtimeMaterial == null || polarityService == null)
            {
                return;
            }

            runtimeMaterial.SetFloat(BaseInvertId, ToInvertValue(polarityService.CurrentPolarity));
            runtimeMaterial.SetFloat(TargetInvertId, ToInvertValue(polarityService.CurrentPolarity));

            if (worldCamera != null)
            {
                Color backgroundColor = sourceBackgroundColor;
                backgroundColor.a = 1f;
                worldCamera.backgroundColor = backgroundColor;
            }
        }

        private static float ToInvertValue(WorldPolarity polarity)
        {
            return polarity == WorldPolarity.Black ? 1f : 0f;
        }

        private Vector2 ResolveViewportCenter()
        {
            if (worldCamera == null || transitionCenter == null)
            {
                return new Vector2(0.5f, 0.5f);
            }

            Vector3 viewportPoint = worldCamera.WorldToViewportPoint(transitionCenter.position);
            return new Vector2(viewportPoint.x, viewportPoint.y);
        }

        private float ResolveAspect()
        {
            if (targetRawImage == null)
            {
                return 1f;
            }

            Rect rect = targetRawImage.rectTransform.rect;
            return rect.height <= 0.0001f ? 1f : rect.width / rect.height;
        }

        private static float ResolveMaxRadius(Vector2 center, float aspect)
        {
            Vector2[] corners =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };

            float maxRadius = 0f;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 delta = corners[i] - center;
                delta.x *= aspect;
                maxRadius = Mathf.Max(maxRadius, delta.magnitude);
            }

            return maxRadius;
        }
    }
}
