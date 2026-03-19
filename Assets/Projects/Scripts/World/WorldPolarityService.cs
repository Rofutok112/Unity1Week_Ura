using System;
using System.Collections;
using Projects.Scripts.Characters;
using UnityEngine;

namespace Projects.Scripts.World
{
    public sealed class WorldPolarityService : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader2D inputReader;
        [SerializeField] private WorldPolarity initialPolarity = WorldPolarity.White;
        [SerializeField, Min(0f)] private float toggleTransitionDuration = 0.35f;

        public static WorldPolarityService Instance { get; private set; }

        public event Action<WorldPolarity> PolarityChanged;
        public event Action<WorldPolarity, WorldPolarity, float> TransitionStarted;
        public event Action<WorldPolarity> TransitionCompleted;

        public WorldPolarity CurrentPolarity { get; private set; }
        public bool IsTransitioning { get; private set; }

        private Coroutine transitionRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate WorldPolarityService found. Destroying the newer instance.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentPolarity = initialPolarity;

            if (inputReader == null)
            {
                inputReader = FindFirstObjectByType<PlayerInputReader2D>();
            }
        }

        private void OnEnable()
        {
            if (inputReader == null)
            {
                inputReader = FindFirstObjectByType<PlayerInputReader2D>();
            }

            if (inputReader != null)
            {
                inputReader.TogglePolarityPressed += Toggle;
            }
        }

        private void Start()
        {
            PolarityChanged?.Invoke(CurrentPolarity);
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.TogglePolarityPressed -= Toggle;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Toggle()
        {
            TransitionTo(CurrentPolarity == WorldPolarity.White ? WorldPolarity.Black : WorldPolarity.White);
        }

        public void SetPolarity(WorldPolarity polarity)
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
                IsTransitioning = false;
            }

            if (CurrentPolarity == polarity)
            {
                return;
            }

            CurrentPolarity = polarity;
            PolarityChanged?.Invoke(CurrentPolarity);
            TransitionCompleted?.Invoke(CurrentPolarity);
        }

        public void TransitionTo(WorldPolarity polarity)
        {
            if (CurrentPolarity == polarity || IsTransitioning)
            {
                return;
            }

            if (toggleTransitionDuration <= 0f)
            {
                SetPolarity(polarity);
                return;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(TransitionRoutine(polarity));
        }

        private IEnumerator TransitionRoutine(WorldPolarity nextPolarity)
        {
            IsTransitioning = true;
            TransitionStarted?.Invoke(CurrentPolarity, nextPolarity, toggleTransitionDuration);
            yield return new WaitForSeconds(toggleTransitionDuration);
            transitionRoutine = null;
            IsTransitioning = false;
            CurrentPolarity = nextPolarity;
            PolarityChanged?.Invoke(CurrentPolarity);
            TransitionCompleted?.Invoke(CurrentPolarity);
        }
    }
}
