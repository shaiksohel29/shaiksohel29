// ============================================================================
// Marble Odyssey 3D — Level Complete Screen with star animations
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MarbleOdyssey
{
    public class LevelCompleteController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Text _levelTitleText;
        [SerializeField] private Text _timeText;
        [SerializeField] private Text _gemsCollectedText;
        [SerializeField] private Text _newBestText;
        [SerializeField] private StarRatingDisplay _starDisplay;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private RectTransform _contentPanel;
        [SerializeField] private ParticleSystem _celebrationParticles;

        [Header("Star Thresholds (seconds)")]
        [SerializeField] private float _threeStarTime = 30f;
        [SerializeField] private float _twoStarTime = 60f;

        [Header("Animation")]
        [SerializeField] private float _panelSlideDistance = 600f;
        [SerializeField] private float _slideInDuration = 0.5f;
        [SerializeField] private float _starRevealDelay = 0.8f;
        [SerializeField] private float _starRevealInterval = 0.5f;

        private float _completionTime;
        private int _gemsCollected;
        private int _earnedStars;
        private string _worldId;
        private int _levelNumber;
        private bool _isNewBest;

        private void Start()
        {
            _nextLevelButton?.onClick.AddListener(OnNextLevel);
            _retryButton?.onClick.AddListener(OnRetry);
            _menuButton?.onClick.AddListener(OnMenu);

            if (_newBestText != null)
                _newBestText.gameObject.SetActive(false);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Show the level complete screen with results.
        /// Called by LevelManager when the level is completed.
        /// </summary>
        public void Show(string worldId, int levelNumber, float completionTime,
            int gemsCollected, float threeStarTime = -1, float twoStarTime = -1)
        {
            _worldId = worldId;
            _levelNumber = levelNumber;
            _completionTime = completionTime;
            _gemsCollected = gemsCollected;

            if (threeStarTime > 0) _threeStarTime = threeStarTime;
            if (twoStarTime > 0) _twoStarTime = twoStarTime;

            // Calculate stars
            if (completionTime <= _threeStarTime) _earnedStars = 3;
            else if (completionTime <= _twoStarTime) _earnedStars = 2;
            else _earnedStars = 1;

            // Save progress
            string levelKey = $"{_worldId}_level_{_levelNumber}";
            _isNewBest = SaveSystem.SaveLevelCompletion(levelKey, _earnedStars, completionTime, gemsCollected);

            // Populate UI
            if (_levelTitleText != null)
                _levelTitleText.text = $"LEVEL {_levelNumber} COMPLETE!";

            if (_timeText != null)
                _timeText.text = FormatTime(completionTime);

            if (_gemsCollectedText != null)
                _gemsCollectedText.text = $"×{gemsCollected}";

            gameObject.SetActive(true);
            StartCoroutine(PlaySequence());
        }

        // =====================================================================
        // Animation Sequence
        // =====================================================================

        private IEnumerator PlaySequence()
        {
            Time.timeScale = 0f;

            // Initial state
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            if (_contentPanel != null)
                _contentPanel.anchoredPosition = new Vector2(0, -_panelSlideDistance);
            if (_starDisplay != null) _starDisplay.SetStars(0, false);

            // Fade in backdrop
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null)
                    _canvasGroup.alpha = Mathf.SmoothStep(0, 1, elapsed / 0.3f);
                yield return null;
            }

            // Slide up panel
            elapsed = 0f;
            Vector2 startPos = new Vector2(0, -_panelSlideDistance);
            Vector2 endPos = Vector2.zero;
            while (elapsed < _slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _slideInDuration;
                // Elastic out
                float ease = Mathf.Sin(-13f * (t + 1f) * Mathf.PI * 0.5f) *
                    Mathf.Pow(2f, -10f * t) + 1f;
                if (_contentPanel != null)
                    _contentPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
                yield return null;
            }

            // Play fanfare
            AudioManager.Instance?.PlaySFX("level_complete_fanfare");

            // Celebration particles
            if (_celebrationParticles != null)
                _celebrationParticles.Play();

            // Reveal stars one by one
            yield return new WaitForSecondsRealtime(_starRevealDelay);

            if (_starDisplay != null)
            {
                for (int i = 1; i <= _earnedStars; i++)
                {
                    _starDisplay.SetStars(i, true);
                    AudioManager.Instance?.PlaySFX("star_fill");
                    yield return new WaitForSecondsRealtime(_starRevealInterval);
                }
            }

            // New best time indicator
            if (_isNewBest && _newBestText != null)
            {
                _newBestText.gameObject.SetActive(true);
                StartCoroutine(PulseNewBest());
            }
        }

        private IEnumerator PulseNewBest()
        {
            var rt = _newBestText.GetComponent<RectTransform>();
            if (rt == null) yield break;

            rt.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / 0.4f;
                float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
                rt.localScale = Vector3.one * t * overshoot;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        // =====================================================================
        // Button Handlers
        // =====================================================================

        private void OnNextLevel()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            Time.timeScale = 1f;

            int nextLevel = _levelNumber + 1;
            if (nextLevel > 10) // Back to world select if last level
            {
                SceneManager.LoadScene("WorldSelect");
            }
            else
            {
                PlayerPrefs.SetInt("CurrentLevel", nextLevel);
                SceneManager.LoadScene("Gameplay");
            }
        }

        private void OnRetry()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnMenu()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            Time.timeScale = 1f;
            SceneManager.LoadScene("LevelSelect");
        }

        private string FormatTime(float seconds)
        {
            int min = (int)(seconds / 60);
            float sec = seconds % 60;
            return $"{min:D2}:{sec:05.2f}";
        }
    }
}
