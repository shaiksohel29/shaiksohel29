// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// InterstitialAdController.cs — Interstitial ad lifecycle management
// ============================================================================

using System;
using UnityEngine;

#if UNITY_ANDROID
using GoogleMobileAds.Api;
#endif

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Manages interstitial ad loading, showing, and cleanup.
    /// Display decisions are made by AdPlacementPolicy — this controller
    /// only handles the ad object lifecycle.
    /// </summary>
    public class InterstitialAdController : IFullScreenAdController
    {
        private readonly AdConfiguration _config;
        private readonly AdAnalyticsManager _analytics;
        private readonly AdRevenueTracker _revenueTracker;
        private readonly AdFailureRecovery _failureRecovery;

#if UNITY_ANDROID
        private InterstitialAd _interstitialAd;
#endif

        private AdState _state = AdState.NotLoaded;
        private bool _isDestroyed;

        // =====================================================================
        // IAdController
        // =====================================================================

        public AdFormat Format => AdFormat.Interstitial;
        public AdState State => _state;
        public bool IsReady => _state == AdState.Ready;

        public event Action<AdFormat> OnAdLoaded;
        public event Action<AdFormat, string> OnAdFailedToLoad;
        public event Action<AdFormat> OnAdOpened;
        public event Action<AdFormat> OnAdClosed;
        public event Action<AdFormat> OnAdImpression;

        // =====================================================================
        // Constructor
        // =====================================================================

        public InterstitialAdController(
            AdConfiguration config,
            AdAnalyticsManager analytics,
            AdRevenueTracker revenueTracker,
            AdFailureRecovery failureRecovery)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _analytics = analytics;
            _revenueTracker = revenueTracker;
            _failureRecovery = failureRecovery;
        }

        // =====================================================================
        // Load
        // =====================================================================

        public void Load()
        {
            if (_isDestroyed) return;

            if (_state == AdState.Loading)
            {
                Debug.Log("[Interstitial] Already loading — skipping.");
                return;
            }

            if (_state == AdState.Ready)
            {
                Debug.Log("[Interstitial] Already loaded and ready.");
                return;
            }

#if UNITY_ANDROID
            _state = AdState.Loading;
            string adUnitId = _config.GetAdUnitId(AdFormat.Interstitial);

            Debug.Log("[Interstitial] Loading ad...");
            _analytics?.TrackEvent(AdEventType.Request, AdFormat.Interstitial,
                AdPlacement.LevelComplete);

            var adRequest = new AdRequest();

            InterstitialAd.Load(adUnitId, adRequest,
                (InterstitialAd ad, LoadAdError error) =>
                {
                    MainThreadDispatcher.Enqueue(() => HandleLoadResult(ad, error));
                });
#else
            _state = AdState.Failed;
#endif
        }

#if UNITY_ANDROID
        private void HandleLoadResult(InterstitialAd ad, LoadAdError error)
        {
            if (_isDestroyed) return;

            if (error != null || ad == null)
            {
                string errorMsg = error?.GetMessage() ?? "Unknown error";
                Debug.LogWarning($"[Interstitial] Load failed: {errorMsg}");
                _state = AdState.Failed;

                _analytics?.TrackEvent(AdEventType.LoadFailure, AdFormat.Interstitial,
                    AdPlacement.LevelComplete, errorMsg);
                OnAdFailedToLoad?.Invoke(AdFormat.Interstitial, errorMsg);

                _failureRecovery?.ScheduleRetry(AdFormat.Interstitial, () => Load());
                return;
            }

            _interstitialAd = ad;
            _state = AdState.Ready;
            _failureRecovery?.ResetRetryCount(AdFormat.Interstitial);

            Debug.Log("[Interstitial] Loaded successfully.");
            _analytics?.TrackEvent(AdEventType.LoadSuccess, AdFormat.Interstitial,
                AdPlacement.LevelComplete);

            RegisterCallbacks(ad);

            OnAdLoaded?.Invoke(AdFormat.Interstitial);
        }

        private void RegisterCallbacks(InterstitialAd ad)
        {
            ad.OnAdPaid += (AdValue adValue) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _revenueTracker?.RecordRevenue(AdFormat.Interstitial,
                        AdPlacement.LevelComplete, adValue.CurrencyCode, adValue.Value);
                    _analytics?.TrackRevenue(AdFormat.Interstitial,
                        AdPlacement.LevelComplete, adValue.CurrencyCode, adValue.Value);
                });
            };

            ad.OnAdImpressionRecorded += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _analytics?.TrackEvent(AdEventType.Impression, AdFormat.Interstitial,
                        AdPlacement.LevelComplete);
                    OnAdImpression?.Invoke(AdFormat.Interstitial);
                });
            };

            ad.OnAdClicked += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _analytics?.TrackEvent(AdEventType.Click, AdFormat.Interstitial,
                        AdPlacement.LevelComplete);
                });
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _state = AdState.Showing;
                    _analytics?.TrackEvent(AdEventType.Opened, AdFormat.Interstitial,
                        AdPlacement.LevelComplete);
                    OnAdOpened?.Invoke(AdFormat.Interstitial);
                });
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _state = AdState.Closed;
                    _analytics?.TrackEvent(AdEventType.Dismissed, AdFormat.Interstitial,
                        AdPlacement.LevelComplete);

                    DestroyCurrentAd();
                    OnAdClosed?.Invoke(AdFormat.Interstitial);

                    // Auto-reload
                    Load();
                });
            };

            ad.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.LogWarning($"[Interstitial] Failed to show: {adError.GetMessage()}");
                    _state = AdState.Failed;
                    DestroyCurrentAd();
                    OnAdFailedToLoad?.Invoke(AdFormat.Interstitial, adError.GetMessage());
                    Load();
                });
            };
        }
#endif

        // =====================================================================
        // Show
        // =====================================================================

        public void Show()
        {
            if (_state != AdState.Ready)
            {
                Debug.LogWarning("[Interstitial] Cannot show — not ready.");
                return;
            }

#if UNITY_ANDROID
            if (_interstitialAd == null)
            {
                Debug.LogError("[Interstitial] Ad object is null despite Ready state.");
                _state = AdState.Failed;
                Load();
                return;
            }

            Debug.Log("[Interstitial] Showing ad.");
            _interstitialAd.Show();
#endif
        }

        // =====================================================================
        // Cleanup
        // =====================================================================

        private void DestroyCurrentAd()
        {
#if UNITY_ANDROID
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
#endif
        }

        public void Destroy()
        {
            _isDestroyed = true;
            DestroyCurrentAd();
            _state = AdState.NotLoaded;
        }
    }
}
