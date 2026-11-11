using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private SoundLibrary _soundLibrary;
        [SerializeField] private int _sfxPoolSize = 12;

        [Header("Sources")]
        [SerializeField] private AudioSource _musicSourceA;
        [SerializeField] private AudioSource _musicSourceB;

        private AudioSource[] _sfxPool;
        private int _sfxPoolIndex;
        private float _musicVolume = 0.8f;
        private float _sfxVolume = 1f;
        private float _savedMusicVolume;
        private AudioSource _activeMusic;
        private Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();
        private Coroutine _crossfadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitSFXPool();
            _activeMusic = _musicSourceA;

            _musicVolume = SettingsController.MusicVolume;
            _sfxVolume = SettingsController.SFXVolume;
        }

        private void InitSFXPool()
        {
            _sfxPool = new AudioSource[_sfxPoolSize];
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var go = new GameObject($"SFX_{i}");
                go.transform.SetParent(transform);
                _sfxPool[i] = go.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
            }
        }

        public void PlaySFX(string clipName, float volumeScale = 1f)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;

            var source = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Length;

            source.clip = clip;
            source.volume = _sfxVolume * volumeScale;
            source.pitch = 1f + Random.Range(-0.05f, 0.05f);
            source.Play();
        }

        public void PlaySFXAtPosition(string clipName, Vector3 position, float volumeScale = 1f)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;

            var source = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Length;

            source.transform.position = position;
            source.spatialBlend = 1f;
            source.clip = clip;
            source.volume = _sfxVolume * volumeScale;
            source.Play();
        }

        public void PlayMusic(string clipName, bool crossfade = true)
        {
            var clip = GetClip(clipName);
            if (clip == null) return;

            if (_activeMusic != null && _activeMusic.clip == clip && _activeMusic.isPlaying)
                return;

            if (crossfade && _activeMusic != null && _activeMusic.isPlaying)
            {
                if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
                _crossfadeRoutine = StartCoroutine(CrossfadeMusic(clip));
            }
            else
            {
                if (_activeMusic == null) _activeMusic = _musicSourceA;
                _activeMusic.clip = clip;
                _activeMusic.volume = _musicVolume;
                _activeMusic.loop = true;
                _activeMusic.Play();
            }
        }

        public void StopMusic(float fadeDuration = 1f)
        {
            if (_activeMusic != null && _activeMusic.isPlaying)
                StartCoroutine(FadeOutMusic(_activeMusic, fadeDuration));
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = volume;
            if (_activeMusic != null) _activeMusic.volume = volume;
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = volume;
        }

        public void ReduceMusicVolume(float factor)
        {
            _savedMusicVolume = _musicVolume;
            if (_activeMusic != null) _activeMusic.volume = _musicVolume * factor;
        }

        public void RestoreMusicVolume()
        {
            if (_savedMusicVolume > 0)
                SetMusicVolume(_savedMusicVolume);
            else if (_activeMusic != null)
                _activeMusic.volume = _musicVolume;
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            var fadeOut = _activeMusic;
            var fadeIn = (_activeMusic == _musicSourceA) ? _musicSourceB : _musicSourceA;

            fadeIn.clip = newClip;
            fadeIn.volume = 0f;
            fadeIn.loop = true;
            fadeIn.Play();

            float duration = 1.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                if (fadeOut != null) fadeOut.volume = _musicVolume * (1f - t);
                fadeIn.volume = _musicVolume * t;
                yield return null;
            }

            fadeOut?.Stop();
            fadeIn.volume = _musicVolume;
            _activeMusic = fadeIn;
        }

        private IEnumerator FadeOutMusic(AudioSource source, float duration)
        {
            float startVol = source.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVol, 0, elapsed / duration);
                yield return null;
            }
            source.Stop();
            source.volume = startVol;
        }

        private AudioClip GetClip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_clipCache.TryGetValue(name, out var cached))
                return cached;

            if (_soundLibrary != null)
            {
                var clip = _soundLibrary.GetClip(name);
                if (clip != null) { _clipCache[name] = clip; return clip; }
            }

            var loaded = Resources.Load<AudioClip>($"Audio/{name}");
            if (loaded != null) _clipCache[name] = loaded;
            return loaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
