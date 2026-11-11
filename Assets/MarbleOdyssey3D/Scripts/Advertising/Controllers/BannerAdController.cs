// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// BannerAdController.cs — Adaptive banner ad controller
// ============================================================================

using System;
using UnityEngine;

#if UNITY_ANDROID
using GoogleMobileAds.Api;
#endif

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Manages adaptive banner ads for non-gameplay screens.
    /// Banners are NEVER placed on the active gameplay screen.
    /// Uses adaptive banner sizing for optimal display.
    /// </summary>
    public class BannerAdController : IAdController
    {
        private readonly AdConfiguration _config;
        private readonly AdAnalyticsManager _analytics;
        private readonly AdRevenueTracker _revenueTracker;

#if UNITY_ANDROID
        private BannerView _bannerView;
#endif

        private AdState _state = AdState.NotLoaded;
        private bool _isDestroyed;
        private bool _isVisible;

        // =====================================================================
        // IAdController
        // =====================================================================

        public AdFormat Format => AdFormat.Banner;
        public AdState State => _state;
        public bool IsReady => _state == AdState.Ready;

        public event Action<AdFormat> OnAdLoaded;
        public event Action<AdFormat, string> OnAdFailedToLoad;
        public event Action<AdFormat> OnAdOpened;
        public event Action<AdFormat> OnAdClosed;

        // =====================================================================
        // Constructor
        // =====================================================================

        public BannerAdController(
            AdConfiguration config,
            AdAnalyticsManager analytics,
            AdRevenueTracker revenueTracker)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _analytics = analytics;
            _revenueTracker = revenueTracker;
        }

        // =====================================================================
        // Load
        // =====================================================================

        public void Load()
        {
            if (_isDestroyed) return;
            if (_state == AdState.Loading || _state == AdState.Ready) return;

#if UNITY_ANDROID
            _state = AdState.Loading;
            string adUnitId = _config.GetAdUnitId(AdFormat.Banner);

            if (string.IsNullOrEmpty(adUnitId))
            {
                Debug.LogWarning("[Banner] No ad unit ID configured.");
                _state = AdState.Failed;
                return;
            }

            // Use adaptive banner size
            var adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(
                AdSize.FullWidth);

            _bannerView = new BannerView(adUnitId, adSize, AdPosition.Bottom);

            RegisterCallbacks(_bannerView);

            _analytics?.TrackEvent(AdEventType.Request, AdFormat.Banner, AdPlacement.MenuBanner);

            var adRequest = new AdRequest();
            _bannerView.LoadAd(adRequest);

            // Start hidden — show only when explicitly requested
            _bannerView.Hide();

            Debug.Log("[Banner] Loading adaptive banner...");
#else
            _state = AdState.Failed;
#endif
        }

#if UNITY_ANDROID
        private void RegisterCallbacks(BannerView banner)
        {
            banner.OnBannerAdLoaded += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _state = AdState.Ready;
                    Debug.Log("[Banner] Loaded successfully.");
                    _analytics?.TrackEvent(AdEventType.LoadSuccess, AdFormat.Banner,
                        AdPlacement.MenuBanner);
                    OnAdLoaded?.Invoke(AdFormat.Banner);
                });
            };

            banner.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    string msg = error?.GetMessage() ?? "Unknown";
                    Debug.LogWarning($"[Banner] Load failed: {msg}");
                    _state = AdState.Failed;
                    _analytics?.TrackEvent(AdEventType.LoadFailure, AdFormat.Banner,
                        AdPlacement.MenuBanner, msg);
                    OnAdFailedToLoad?.Invoke(AdFormat.Banner, msg);
                });
            };

            banner.OnAdPaid += (AdValue adValue) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _revenueTracker?.RecordRevenue(AdFormat.Banner, AdPlacement.MenuBanner,
                        adValue.CurrencyCode, adValue.Value);
                });
            };

            banner.OnAdImpressionRecorded += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _analytics?.TrackEvent(AdEventType.Impression, AdFormat.Banner,
                        AdPlacement.MenuBanner);
                });
            };

            banner.OnAdClicked += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _analytics?.TrackEvent(AdEventType.Click, AdFormat.Banner,
                        AdPlacement.MenuBanner);
                });
            };

            banner.OnAdFullScreenContentOpened += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    OnAdOpened?.Invoke(AdFormat.Banner);
                });
            };

            banner.OnAdFullScreenContentClosed += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    OnAdClosed?.Invoke(AdFormat.Banner);
                });
            };
        }
#endif

        // =====================================================================
        // Show / Hide
        // =====================================================================

        /// <summary>
        /// Show the banner on screen. Only call on non-gameplay screens.
        /// </summary>
        public void Show()
        {
#if UNITY_ANDROID
            if (_bannerView != null && _state == AdState.Ready)
            {
                _bannerView.Show();
                _isVisible = true;
                Debug.Log("[Banner] Shown.");
            }
#endif
        }

        /// <summary>
        /// Hide the banner (e.g., when entering gameplay).
        /// </summary>
        public void Hide()
        {
#if UNITY_ANDROID
            if (_bannerView != null)
            {
                _bannerView.Hide();
                _isVisible = false;
                Debug.Log("[Banner] Hidden.");
            }
#endif
        }

        public bool IsVisible => _isVisible;

        // =====================================================================
        // Cleanup
        // =====================================================================

        public void Destroy()
        {
            _isDestroyed = true;
#if UNITY_ANDROID
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
            }
#endif
            _isVisible = false;
            _state = AdState.NotLoaded;
        }
    }
}
