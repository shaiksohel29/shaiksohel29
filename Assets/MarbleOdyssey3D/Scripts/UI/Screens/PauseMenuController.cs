// ============================================================================
// Marble Odyssey 3D — Pause Menu with blur overlay
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MarbleOdyssey
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private CanvasGroup _pauseCanvasGroup;
        [SerializeField] private Image _blurOverlay;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private GameObject _settingsPanel;

        [Header("Animation")]
        [SerializeField] private float _fadeInDuration = 0.25f;
        [SerializeField] private Color _overlayColor = new Color(0, 0, 0, 0.65f);

        private bool _isPaused;
        private float _previousTimeScale;

        public bool IsPaused => _isPaused;

        public static PauseMenuController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            _pausePanel?.SetActive(false);
        }

        private void Start()
        {
            _resumeButton?.onClick.AddListener(Resume);
            _restartButton?.onClick.AddListener(RestartLevel);
            _settingsButton?.onClick.AddListener(OpenSettings);
            _quitButton?.onClick.AddListener(QuitToMenu);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // =====================================================================
        // Pause / Resume
        // =====================================================================

        public void TogglePause()
        {
            if (_isPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (_isPaused) return;
            _isPaused = true;

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            AudioManager.Instance?.SetMusicVolume(0.2f);

            _pausePanel?.SetActive(true);
            StartCoroutine(FadeIn());

            AudioManager.Instance?.PlaySFX("button_click");
        }

        public void Resume()
        {
            if (!_isPaused) return;

            AudioManager.Instance?.PlaySFX("button_click");
            StartCoroutine(FadeOutAndResume());
        }

        private void RestartLevel()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            Time.timeScale = 1f;
            _isPaused = false;
            AudioManager.Instance?.RestoreMusicVolume();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OpenSettings()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            _settingsPanel?.SetActive(true);
        }

        private void QuitToMenu()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            Time.timeScale = 1f;
            _isPaused = false;
            AudioManager.Instance?.RestoreMusicVolume();
            SceneManager.LoadScene("MainMenu");
        }

        // =====================================================================
        // Animations
        // =====================================================================

        private IEnumerator FadeIn()
        {
            if (_pauseCanvasGroup != null) _pauseCanvasGroup.alpha = 0f;
            if (_blurOverlay != null) _blurOverlay.color = Color.clear;

            float elapsed = 0f;
            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _fadeInDuration;
                float smooth = t * t * (3f - 2f * t);

                if (_pauseCanvasGroup != null) _pauseCanvasGroup.alpha = smooth;
                if (_blurOverlay != null) _blurOverlay.color = Color.Lerp(Color.clear, _overlayColor, smooth);

                yield return null;
            }

            if (_pauseCanvasGroup != null) _pauseCanvasGroup.alpha = 1f;
            if (_blurOverlay != null) _blurOverlay.color = _overlayColor;
        }

        private IEnumerator FadeOutAndResume()
        {
            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - (elapsed / duration);

                if (_pauseCanvasGroup != null) _pauseCanvasGroup.alpha = t;
                if (_blurOverlay != null) _blurOverlay.color = Color.Lerp(Color.clear, _overlayColor, t);

                yield return null;
            }

            _pausePanel?.SetActive(false);
            Time.timeScale = _previousTimeScale;
            _isPaused = false;
            AudioManager.Instance?.RestoreMusicVolume();
        }
    }
}
