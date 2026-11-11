// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// ConnectivityOverlay.cs — Internet-required UI overlay
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace MarbleOdyssey.Advertising.UI
{
    /// <summary>
    /// Manages the internet connectivity overlay that appears when connection is lost.
    ///
    /// At launch: Validates internet reachability, blocks gameplay when unavailable.
    /// During gameplay: Detects connectivity loss, pauses physics/timer, shows reconnect UI.
    /// On reconnect: Resumes after validation.
    ///
    /// IMPORTANT: AdMob ad loading failure does NOT prove internet disconnection.
    /// If general internet is available but AdMob has no fill, gameplay must continue.
    /// Never force players to watch an ad to restore connectivity.
    /// </summary>
    public class ConnectivityOverlay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _overlayPanel;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _detailText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private GameObject _loadingSpinner;

        [Header("Settings")]
        [SerializeField] private bool _pauseGameOnDisconnect = true;

        private NetworkConnectivityService _networkService;
        private bool _isOverlayVisible;
        private bool _wasTimescalePaused;
        private float _previousTimeScale;

        private void Start()
        {
            if (_overlayPanel != null)
                _overlayPanel.SetActive(false);

            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetryClicked);

            // Wait for AdMobManager to initialize
            StartCoroutine(WaitForNetworkService());
        }

        private System.Collections.IEnumerator WaitForNetworkService()
        {
            // Wait for AdMobManager
            while (AdMobManager.Instance == null || AdMobManager.Instance.Network == null)
                yield return null;

            _networkService = AdMobManager.Instance.Network;
            _networkService.OnConnectivityChanged += OnConnectivityChanged;

            // Initial check
            if (!_networkService.IsConnected)
            {
                ShowOverlay(false);
            }
        }

        private void OnDestroy()
        {
            if (_networkService != null)
                _networkService.OnConnectivityChanged -= OnConnectivityChanged;
        }

        // =====================================================================
        // Connectivity Events
        // =====================================================================

        private void OnConnectivityChanged(bool isConnected)
        {
            if (isConnected)
            {
                HideOverlay();
            }
            else
            {
                ShowOverlay(false);
            }
        }

        // =====================================================================
        // Overlay Display
        // =====================================================================

        private void ShowOverlay(bool isChecking)
        {
            if (_overlayPanel == null) return;

            _isOverlayVisible = true;
            _overlayPanel.SetActive(true);

            if (isChecking)
            {
                SetUIState("Checking Connection...",
                    "Verifying internet connectivity", false, true);
            }
            else
            {
                SetUIState("Connection Required",
                    "Marble Odyssey 3D requires an active internet connection.\n" +
                    "Please check your connection and tap retry.",
                    true, false);
            }

            // Pause game if configured
            if (_pauseGameOnDisconnect && !_wasTimescalePaused)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _wasTimescalePaused = true;
                Debug.Log("[ConnOverlay] Game paused due to connectivity loss.");
            }
        }

        private void HideOverlay()
        {
            if (_overlayPanel == null) return;

            _isOverlayVisible = false;
            _overlayPanel.SetActive(false);

            // Restore game time
            if (_wasTimescalePaused)
            {
                Time.timeScale = _previousTimeScale;
                _wasTimescalePaused = false;
                Debug.Log("[ConnOverlay] Game resumed after reconnection.");
            }
        }

        private void SetUIState(string status, string detail,
            bool showRetry, bool showSpinner)
        {
            if (_statusText != null) _statusText.text = status;
            if (_detailText != null) _detailText.text = detail;
            if (_retryButton != null) _retryButton.gameObject.SetActive(showRetry);
            if (_loadingSpinner != null) _loadingSpinner.SetActive(showSpinner);
        }

        // =====================================================================
        // User Interaction
        // =====================================================================

        private void OnRetryClicked()
        {
            if (_networkService == null) return;

            ShowOverlay(true); // Show checking state

            _networkService.CheckNow(isConnected =>
            {
                if (isConnected)
                {
                    HideOverlay();
                }
                else
                {
                    ShowOverlay(false);
                }
            });
        }

        /// <summary>
        /// Whether the overlay is currently blocking the screen.
        /// </summary>
        public bool IsBlocking => _isOverlayVisible;
    }
}
