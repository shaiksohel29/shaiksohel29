using UnityEngine;

namespace MarbleOdyssey
{
    public class MusicManager : MonoBehaviour
    {
        private static MusicManager _instance;
        public static MusicManager Instance => _instance;

        [SerializeField] private MusicLibrary _musicLibrary;
        [SerializeField] private float _defaultFadeTime = 1.5f;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private bool _aIsPlaying;
        private float _musicVolume = 1f;
        private float _fadeTimer;
        private float _fadeDuration;
        private bool _isFading;
        private AudioClip _currentClip;

        public float MusicVolume => _musicVolume;
        public MusicLibrary Library => _musicLibrary;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _sourceA = gameObject.AddComponent<AudioSource>();
            _sourceB = gameObject.AddComponent<AudioSource>();
            ConfigSource(_sourceA);
            ConfigSource(_sourceB);

            _musicVolume = PlayerPrefs.GetFloat("audio_music", 0.7f);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void ConfigSource(AudioSource src)
        {
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.priority = 0;
        }

        private void Update()
        {
            if (!_isFading) return;

            _fadeTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_fadeTimer / _fadeDuration);

            var incoming = _aIsPlaying ? _sourceA : _sourceB;
            var outgoing = _aIsPlaying ? _sourceB : _sourceA;

            float masterVol = _musicVolume * (AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f);
            incoming.volume = t * masterVol;
            outgoing.volume = (1f - t) * masterVol;

            if (t >= 1f)
            {
                _isFading = false;
                outgoing.Stop();
                outgoing.clip = null;
            }
        }

        public void PlayTrack(AudioClip clip, float fadeTime = -1f)
        {
            if (clip == null || clip == _currentClip) return;
            _currentClip = clip;

            if (fadeTime < 0f) fadeTime = _defaultFadeTime;

            _aIsPlaying = !_aIsPlaying;
            var incoming = _aIsPlaying ? _sourceA : _sourceB;

            incoming.clip = clip;
            incoming.volume = 0f;
            incoming.Play();

            _fadeDuration = Mathf.Max(fadeTime, 0.01f);
            _fadeTimer = 0f;
            _isFading = true;
        }

        public void PlayWorldMusic(int worldIndex)
        {
            if (_musicLibrary == null) return;
            var clip = _musicLibrary.GetWorldTrack(worldIndex);
            if (clip != null) PlayTrack(clip);
        }

        public void PlayMenuMusic()
        {
            if (_musicLibrary != null && _musicLibrary.MainMenuTrack != null)
                PlayTrack(_musicLibrary.MainMenuTrack);
        }

        public void StopMusic(float fadeTime = -1f)
        {
            if (fadeTime < 0f) fadeTime = _defaultFadeTime;
            _currentClip = null;

            _aIsPlaying = !_aIsPlaying;
            // incoming is silent (no clip), outgoing fades out
            _fadeDuration = Mathf.Max(fadeTime, 0.01f);
            _fadeTimer = 0f;
            _isFading = true;
        }

        public void SetMusicVolume(float vol)
        {
            _musicVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("audio_music", _musicVolume);
            PlayerPrefs.Save();

            if (!_isFading)
            {
                float masterVol = _musicVolume * (AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f);
                var active = _aIsPlaying ? _sourceA : _sourceB;
                active.volume = masterVol;
            }
        }
    }
}
