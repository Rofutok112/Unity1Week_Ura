using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Projects.Scripts.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        private readonly Dictionary<string, AudioClip> clips = new();
        private readonly Dictionary<string, AudioSource> sources = new();
        private readonly Queue<AudioSource> oneShotPool = new();

        public static void Register(string key, AudioClip clip)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("AudioManager.Register failed: key is null or empty.");
                return;
            }

            if (clip == null)
            {
                Debug.LogWarning($"AudioManager.Register failed: clip is null. key={key}");
                return;
            }

            EnsureInstance().clips[key] = clip;
        }

        public static bool Unregister(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var instance = EnsureInstance();
            instance.StopInternal(key);

            if (instance.sources.Remove(key, out var source) && source != null)
            {
                Destroy(source);
            }

            return instance.clips.Remove(key);
        }

        public static bool IsRegistered(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && EnsureInstance().clips.ContainsKey(key);
        }

        public static void Play(string key, float volume = 1f, bool loop = false)
        {
            var instance = EnsureInstance();
            if (!instance.TryGetClip(key, out var clip))
            {
                return;
            }

            var source = instance.GetOrCreateSource(key);
            source.Stop();
            source.clip = clip;
            source.volume = volume;
            source.loop = loop;
            source.Play();
        }

        public static void PlayOneShot(string key, float volume = 1f)
        {
            var instance = EnsureInstance();
            if (!instance.TryGetClip(key, out var clip))
            {
                return;
            }

            var source = instance.GetOrCreateSource(key);
            source.clip = clip;
            source.volume = volume;
            source.loop = false;
            source.Play();
        }

        public static void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                return;
            }

            EnsureInstance().PlayOneShotInternal(clip, volume);
        }

        public static void PlayOneShotAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null)
            {
                return;
            }

            EnsureInstance().PlayOneShotInternal(clip, volume, position, true);
        }

        public static void PlayOneShotAtPosition(string key, Vector3 position, float volume = 1f)
        {
            var instance = EnsureInstance();
            if (!instance.TryGetClip(key, out var clip))
            {
                return;
            }

            instance.PlayOneShotInternal(clip, volume, position, true);
        }

        public static void StopAll()
        {
            if (_instance == null)
            {
                return;
            }

            foreach (var source in _instance.sources.Values)
            {
                if (source == null)
                {
                    continue;
                }

                source.Stop();
                source.clip = null;
                source.loop = false;
            }
        }

        public static void Stop(string key)
        {
            if (_instance == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _instance.StopInternal(key);
        }

        private void PlayOneShotInternal(AudioClip clip, float volume)
        {
            PlayOneShotInternal(clip, volume, Vector3.zero, false);
        }

        private void PlayOneShotInternal(AudioClip clip, float volume, Vector3 position, bool useWorldPosition)
        {
            var source = oneShotPool.Count > 0
                ? oneShotPool.Dequeue()
                : CreateOneShotSource();

            source.transform.position = position;
            source.clip = clip;
            source.volume = volume;
            source.loop = false;
            source.spatialBlend = useWorldPosition ? 1f : 0f;
            source.minDistance = 1f;
            source.maxDistance = 12f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
            StartCoroutine(ReturnOneShotAfterDelay(source, clip.length));
        }

        private IEnumerator ReturnOneShotAfterDelay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            source.Stop();
            source.clip = null;
            oneShotPool.Enqueue(source);
        }

        private AudioSource CreateOneShotSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        private static AudioManager EnsureInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindFirstObjectByType<AudioManager>();
            if (_instance != null)
            {
                return _instance;
            }

            var managerObject = new GameObject(nameof(AudioManager));
            DontDestroyOnLoad(managerObject);
            _instance = managerObject.AddComponent<AudioManager>();
            return _instance;
        }

        private bool TryGetClip(string key, out AudioClip clip)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                clip = null;
                Debug.LogWarning("AudioManager failed: key is null or empty.");
                return false;
            }

            if (clips.TryGetValue(key, out clip))
            {
                return true;
            }

            Debug.LogWarning($"AudioManager failed: no clip registered for key={key}");
            return false;
        }

        private AudioSource GetOrCreateSource(string key)
        {
            if (sources.TryGetValue(key, out var source) && source != null)
            {
                return source;
            }

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            sources[key] = source;
            return source;
        }

        private void StopInternal(string key)
        {
            if (!sources.TryGetValue(key, out var source) || source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.loop = false;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
