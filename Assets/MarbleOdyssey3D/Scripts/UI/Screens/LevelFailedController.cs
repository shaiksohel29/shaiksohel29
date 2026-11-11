// ============================================================================
// Marble Odyssey 3D — Level Failed / Game Over Screen
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MarbleOdyssey
{
    public class LevelFailedController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _contentPanel;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _deathCountText;
        [SerializeField] private Text _encourageText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _secondChanceButton;
        [SerializeField] private Text _secondChanceLabel;
        [SerializeField] private Button _menuButton;

        [Header("Encouraging Messages")]
        [SerializeField] private string[] _encouragements = new[]
        {
            "Almost there! Try again!",
            "You're getting closer!",
            "Don't give up — you've got this!",
            "Every champion falls before they fly.",
            "One more try... that's all it takes!",
            "The marble believes in you!",
            "Precision comes with practice.",
            "Close! Adjust your timing slightly.",
            "Greatness is just one roll away.",
            "The finish line is waiting for you!"
        };

        [Header("Animation")]
        [SerializeField] private float _shakeIntensity = 12f;
        [SerializeField] private float _shakeDuration = 0.3f;

        private int _deathCount;
        private System.Action _onSecondChance;

        private void Start()
        {
            _retryButton?.onClick.AddListener(OnRetry);
            _secondChanceButton?.onClick.AddListener(OnSecondChance);
            _menuButton?.onClick.AddListener(OnMenu);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Show the failure screen.
        /// </summary>
        /// <param name="deathCount">Number of deaths this attempt.</param>
        /// <param name="onSecondChance">Callback if player earns a second chance via ad.</param>
        public void Show(int deathCount, System.Action onSecondChance = null)
        {
            _deathCount = deathCount;
            _onSecondChance = onSecondChance;

            if (_titleText != null) _titleText.text = "LEVEL FAILED";

            if (_deathCountText != null)
                _deathCountText.text = $"Falls: {deathCount}";

            if (_encourageText != null)
                _encourageText.text = _encouragements[Random.Range(0, _encouragements.Length)];

            // Second chance button — integrates with ad system when ready
            bool adAvailable = false;
            // Check if the ad system's rewarded ad is loaded
            // var adMgr = Advertising.AdMobManager.Instance;
            // adAvailable = adMgr != null && adMgr.IsRewardedAdAvailable();

            if (_secondChanceButton != null)
            {
                _secondChanceButton.gameObject.SetActive(adAvailable && onSecondChance != null);
                _secondChanceButton.interactable = adAvailable;
            }

            if (_secondChanceLabel != null)
                _secondChanceLabel.text = adAvailable ? "Watch Ad for Second Chance" : "Unavailable";

            gameObject.SetActive(true);
            StartCoroutine(PlayEntrance());
        }

        // =====================================================================
        // Animation
        // =====================================================================

        private IEnumerator PlayEntrance()
        {
            Time.timeScale = 0f;

            if (_canvasGroup != null) _canvasGroup.alpha = 0f;

            AudioManager.Instance?.PlaySFX("level_fail");

            // Screen shake
            if (_contentPanel != null)
            {
                Vector2 originalPos = _contentPanel.anchoredPosition;
                float elapsed = 0f;
                while (elapsed < _shakeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float decay = 1f - (elapsed / _shakeDuration);
                    float x = Random.Range(-1f, 1f) * _shakeIntensity * decay;
                    float y = Random.Range(-1f, 1f) * _shakeIntensity * decay;
                    _contentPanel.anchoredPosition = originalPos + new Vector2(x, y);

                    if (_canvasGroup != null)
                        _canvasGroup.alpha = Mathf.Min(1f, elapsed / (_shakeDuration * 0.5f));

                    yield return null;
                }
                _contentPanel.anchoredPosition = originalPos;
            }

            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }

        // =====================================================================
        // Button Handlers
        // =====================================================================

        private void OnRetry()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnSecondChance()
        {
            AudioManager.Instance?.PlaySFX("button_click");

            // Show rewarded ad, grant second chance on reward
            // When ad system is integrated:
            // Advertising.AdMobManager.Instance?.TryShowRewarded(
            //     Advertising.AdPlacement.RewardSecondChance,
            //     (type, amount) => { _onSecondChance?.Invoke(); },
            //     () => { /* Ad closed */ });

            // For now, direct callback
            if (_onSecondChance != null)
            {
                Time.timeScale = 1f;
                gameObject.SetActive(false);
                _onSecondChance.Invoke();
            }
        }

        private void OnMenu()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            Time.timeScale = 1f;
            SceneManager.LoadScene("LevelSelect");
        }
    }
}
