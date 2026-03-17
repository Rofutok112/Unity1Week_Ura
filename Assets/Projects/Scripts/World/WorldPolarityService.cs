using System;
using Projects.Scripts.Characters;
using UnityEngine;

namespace Projects.Scripts.World
{
    public sealed class WorldPolarityService : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader2D inputReader;
        [SerializeField] private WorldPolarity initialPolarity = WorldPolarity.White;

        public static WorldPolarityService Instance { get; private set; }

        public event Action<WorldPolarity> PolarityChanged;

        public WorldPolarity CurrentPolarity { get; private set; }

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
            SetPolarity(CurrentPolarity == WorldPolarity.White ? WorldPolarity.Black : WorldPolarity.White);
        }

        public void SetPolarity(WorldPolarity polarity)
        {
            if (CurrentPolarity == polarity)
            {
                return;
            }

            CurrentPolarity = polarity;
            PolarityChanged?.Invoke(CurrentPolarity);
        }
    }
}
