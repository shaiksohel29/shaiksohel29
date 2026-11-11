// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// DiagnosticsPanel.cs — In-game ad diagnostic display
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace MarbleOdyssey.Advertising.UI
{
    /// <summary>
    /// In-game diagnostic panel for monitoring ad system state.
    /// Only visible in development/test mode.
    /// Wire this to a debug menu toggle.
    /// </summary>
    public class DiagnosticsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _diagnosticText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _forceLoadRewardedButton;
        [SerializeField] private Button _forceLoadInterstitialButton;

        [Header("Settings")]
        [SerializeField] private float _autoRefreshInterval = 2f;

        private float _lastRefreshTime;

        private void Start()
        {
            if (_panel != null)
                _panel.SetActive(false);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => _panel?.SetActive(false));

            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(RefreshDiagnostics);

            if (_forceLoadRewardedButton != null)
                _forceLoadRewardedButton.onClick.AddListener(() =>
                    AdMobManager.Instance?.RewardedAds?.Load());

            if (_forceLoadInterstitialButton != null)
                _forceLoadInterstitialButton.onClick.AddListener(() =>
                    AdMobManager.Instance?.InterstitialAds?.Load());
        }

        private void Update()
        {
            if (_panel == null || !_panel.activeSelf) return;

            if (Time.realtimeSinceStartup - _lastRefreshTime > _autoRefreshInterval)
            {
                RefreshDiagnostics();
            }
        }

        /// <summary>
        /// Toggle the diagnostics panel visibility.
        /// </summary>
        public void Toggle()
        {
            if (_panel == null) return;

            _panel.SetActive(!_panel.activeSelf);

            if (_panel.activeSelf)
            {
                RefreshDiagnostics();
            }
        }

        /// <summary>
        /// Show the diagnostics panel.
        /// </summary>
        public void Show()
        {
            if (_panel != null)
            {
                _panel.SetActive(true);
                RefreshDiagnostics();
            }
        }

        private void RefreshDiagnostics()
        {
            _lastRefreshTime = Time.realtimeSinceStartup;

            var mgr = AdMobManager.Instance;
            if (mgr?.Diagnostics == null || _diagnosticText == null) return;

            _diagnosticText.text = mgr.Diagnostics.GenerateReport();
        }
    }
}
