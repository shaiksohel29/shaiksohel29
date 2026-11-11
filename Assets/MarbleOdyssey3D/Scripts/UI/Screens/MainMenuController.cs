// ============================================================================
// Marble Odyssey 3D — Premium Main Menu Controller
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MarbleOdyssey
{
    /// <summary>
    /// Premium main menu with animated title, pulsing play button,
    /// gem/star counters, and background marble rolling loop.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        // =====================================================================
        // UI References
        // =====================================================================

        [Header("Title")]
        [SerializeField] private RectTransform _titleTransform;
        [SerializeField] private CanvasGroup _titleCanvasGroup;
        [SerializeField] private float _titleBobAmplitude = 8f;
        [SerializeField] private float _titleBobSpeed = 1.2f;

        [Header("Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _dailyChallengeButton;
        [SerializeField] private Button _cosmeticsButton;
        [SerializeField] private Button _creditsButton;
        [SerializeField] private RectTransform _playButtonRect;

        [Header("Currency Display")]
        [SerializeField] private Text _gemCountText;
        [SerializeField] private Text _starCountText;
        [SerializeField] private RectTransform _gemIconRect;
        [SerializeField] private RectTransform _starIconRect;

        [Header("Background")]
        [SerializeField] private Transform _bgMarble;
        [SerializeField] private Transform _bgMarblePath;
        [SerializeField] private float _bgMarbleSpeed = 2f;

        [Header("Panels")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _creditsPanel;
        [SerializeField] private GameObject _dailyChallengePanel;
        [SerializeField] private GameObject _cosmeticsPanel;

        [Header("Animation Settings")]
        [SerializeField] private AnimationCurve _pulseCurve = AnimationCurve.EaseInOut(0, 0.95f, 1, 1.08f);
        [SerializeField] private float _pulseSpeed = 1.5f;
        [SerializeField] private float _menuFadeInDuration = 0.8f;

        // =====================================================================
        // State
        // =====================================================================

        private CanvasGroup _mainCanvasGroup;
        private Vector3 _titleStartPos;
        private bool _isTransitioning;

        // =====================================================================
        // Lifecycle
        // =====================================================================

        private void Awake()
        {
            _mainCanvasGroup = GetComponent<CanvasGroup>();
            if (_mainCanvasGroup == null)
                _mainCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            BindButtons();
            UpdateCurrencyDisplay();

            if (_titleTransform != null)
                _titleStartPos = _titleTransform.anchoredPosition;

            // Intro animation
            StartCoroutine(FadeInMenu());
        }

        private void Update()
        {
            AnimateTitle();
            AnimatePlayButton();
            AnimateBackgroundMarble();
            AnimateCurrencyIcons();
        }

        // =====================================================================
        // Button Bindings
        // =====================================================================

        private void BindButtons()
        {
            _playButton?.onClick.AddListener(OnPlayClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _dailyChallengeButton?.onClick.AddListener(OnDailyChallengeClicked);
            _cosmeticsButton?.onClick.AddListener(OnCosmeticsClicked);
            _creditsButton?.onClick.AddListener(OnCreditsClicked);
        }

        private void OnPlayClicked()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            AudioManager.Instance?.PlaySFX("button_click");
            StartCoroutine(TransitionToWorldSelect());
        }

        private void OnSettingsClicked()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            _settingsPanel?.SetActive(true);
        }

        private void OnDailyChallengeClicked()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            _dailyChallengePanel?.SetActive(true);
        }

        private void OnCosmeticsClicked()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            _cosmeticsPanel?.SetActive(true);
        }

        private void OnCreditsClicked()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            _creditsPanel?.SetActive(true);
        }

        // =====================================================================
        // Currency Display
        // =====================================================================

        public void UpdateCurrencyDisplay()
        {
            if (_gemCountText != null)
            {
                int gems = SaveSystem.GetGems();
                _gemCountText.text = FormatNumber(gems);
            }

            if (_starCountText != null)
            {
                int stars = SaveSystem.GetTotalStars();
                _starCountText.text = FormatNumber(stars);
            }
        }

        private string FormatNumber(int value)
        {
            if (value >= 1000000) return $"{value / 1000000f:F1}M";
            if (value >= 1000) return $"{value / 1000f:F1}K";
            return value.ToString();
        }

        // =====================================================================
        // Animations
        // =====================================================================

        private void AnimateTitle()
        {
            if (_titleTransform == null) return;

            float bob = Mathf.Sin(Time.unscaledTime * _titleBobSpeed) * _titleBobAmplitude;
            _titleTransform.anchoredPosition = _titleStartPos + Vector3.up * bob;
        }

        private void AnimatePlayButton()
        {
            if (_playButtonRect == null) return;

            float t = Mathf.PingPong(Time.unscaledTime * _pulseSpeed, 1f);
            float scale = _pulseCurve.Evaluate(t);
            _playButtonRect.localScale = Vector3.one * scale;
        }

        private void AnimateBackgroundMarble()
        {
            if (_bgMarble == null) return;

            _bgMarble.Rotate(Vector3.right * (_bgMarbleSpeed * 60f * Time.unscaledDeltaTime), Space.World);
            _bgMarble.Rotate(Vector3.forward * (_bgMarbleSpeed * 25f * Time.unscaledDeltaTime), Space.World);
        }

        private void AnimateCurrencyIcons()
        {
            float bounce = 1f + Mathf.Sin(Time.unscaledTime * 2f) * 0.03f;

            if (_gemIconRect != null)
                _gemIconRect.localScale = Vector3.one * bounce;

            if (_starIconRect != null)
            {
                float starBounce = 1f + Mathf.Sin(Time.unscaledTime * 2f + 1f) * 0.03f;
                _starIconRect.localScale = Vector3.one * starBounce;
            }
        }

        // =====================================================================
        // Transitions
        // =====================================================================

        private IEnumerator FadeInMenu()
        {
            if (_mainCanvasGroup != null)
                _mainCanvasGroup.alpha = 0f;

            if (_titleCanvasGroup != null)
                _titleCanvasGroup.alpha = 0f;

            // Fade in main canvas
            float elapsed = 0f;
            while (elapsed < _menuFadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _menuFadeInDuration;
                float smooth = t * t * (3f - 2f * t); // Smoothstep

                if (_mainCanvasGroup != null)
                    _mainCanvasGroup.alpha = smooth;

                yield return null;
            }

            if (_mainCanvasGroup != null)
                _mainCanvasGroup.alpha = 1f;

            // Title reveal
            elapsed = 0f;
            float titleDuration = 0.5f;
            while (elapsed < titleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / titleDuration);

                if (_titleCanvasGroup != null)
                    _titleCanvasGroup.alpha = t;

                if (_titleTransform != null)
                {
                    float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
                    _titleTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, t) * overshoot;
                }

                yield return null;
            }

            if (_titleCanvasGroup != null)
                _titleCanvasGroup.alpha = 1f;
            if (_titleTransform != null)
                _titleTransform.localScale = Vector3.one;
        }

        private IEnumerator TransitionToWorldSelect()
        {
            AudioManager.Instance?.PlaySFX("screen_transition");

            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                if (_mainCanvasGroup != null)
                    _mainCanvasGroup.alpha = 1f - t;

                yield return null;
            }

            SceneManager.LoadScene("WorldSelect");
        }
    }
}
