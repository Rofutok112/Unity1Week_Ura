using System.Collections;
using UnityEngine;

namespace Projects.Scripts.Characters
{
    public sealed class ImpactEffectPlayer : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Animation legacyAnimation;
        [SerializeField, Min(0.01f)] private float fallbackLifetime = 0.35f;

        private Coroutine despawnRoutine;
        private ParticleSystem[] particleSystems;
        private ProjectileSpawner2D despawnTarget;

        public GameObject PrefabSource { get; private set; }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (legacyAnimation == null)
            {
                legacyAnimation = GetComponentInChildren<Animation>();
            }

            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void OnDisable()
        {
            if (despawnRoutine != null)
            {
                StopCoroutine(despawnRoutine);
                despawnRoutine = null;
            }
        }

        public void SetPrefabSource(GameObject prefabSource)
        {
            PrefabSource = prefabSource;
        }

        public void Play(ProjectileSpawner2D despawnHandler)
        {
            despawnTarget = despawnHandler;
            gameObject.SetActive(true);
            RestartPlayback();

            if (despawnRoutine != null)
            {
                StopCoroutine(despawnRoutine);
            }

            despawnRoutine = StartCoroutine(ReturnToPoolAfterDelay(ResolveLifetime()));
        }

        private void RestartPlayback()
        {
            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.Play(0, 0, 0f);
            }

            if (legacyAnimation != null)
            {
                legacyAnimation.Stop();
                legacyAnimation.Play();
            }

            if (particleSystems == null)
            {
                return;
            }

            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] == null)
                {
                    continue;
                }

                particleSystems[i].Clear(true);
                particleSystems[i].Play(true);
            }
        }

        private float ResolveLifetime()
        {
            float duration = fallbackLifetime;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] == null)
                    {
                        continue;
                    }

                    duration = Mathf.Max(duration, clips[i].length);
                }
            }

            if (legacyAnimation != null)
            {
                foreach (AnimationState state in legacyAnimation)
                {
                    if (state == null)
                    {
                        continue;
                    }

                    duration = Mathf.Max(duration, state.length);
                }
            }

            if (particleSystems != null)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    ParticleSystem particleSystem = particleSystems[i];

                    if (particleSystem == null)
                    {
                        continue;
                    }

                    ParticleSystem.MainModule main = particleSystem.main;
                    float startLifetime = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                        ? main.startLifetime.constantMax
                        : main.startLifetime.constant;
                    duration = Mathf.Max(duration, main.duration + startLifetime);
                }
            }

            return duration;
        }

        private IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            despawnRoutine = null;

            if (despawnTarget != null)
            {
                despawnTarget.DespawnImpactEffect(this);
                yield break;
            }

            gameObject.SetActive(false);
        }
    }
}
