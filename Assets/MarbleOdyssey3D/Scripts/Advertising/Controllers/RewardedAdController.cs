// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// RewardedAdController.cs — Rewarded video ad lifecycle management
// ============================================================================

using System;
using UnityEngine;

#if UNITY_ANDROID
using GoogleMobileAds.Api;
#endif

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Manages the full lifecycle of rewarded video ads.
    /// Rewards are granted ONLY after a verified successful reward callback.
    /// Implements duplicate-reward prevention via RewardTransactionManager.
    /// </summary>
    public class RewardedAdController : IRewardedAdController
    {
        private readonly AdConfiguration _config;
        private readonly AdAnalyticsManager _analytics;
        private readonly AdRevenueTracker _revenueTracker;
        private readonly RewardTransactionManager _rewardTxManager;
        private readonly AdFailureRecovery _failureRecovery;

#if UNITY_ANDROID
        private RewardedAd _rewardedAd;
#endif

        private AdState _state = AdState.NotLoaded;
        private bool _rewardEarnedThisShow;
        private string _currentTransactionId;
        private bool _isDestroyed;

        // =====================================================================
        // IAdController
        // =====================================================================

        public AdFormat Format => AdFormat.Rewarded;
        public AdState State => _state;
        public bool IsReady => _state == AdState.Ready;

        public event Action<AdFormat> OnAdLoaded;
        public event Action<AdFormat, string> OnAdFailedToLoad;
        public event Action<AdFormat> OnAdOpened;
        public event Action<AdFormat> OnAdClosed;
        public event Action<AdFormat> OnAdImpression;
        public event Action<string, double> OnRewardEarned;

        // =====================================================================
        // Constructor
        // =====================================================================

        public RewardedAdController(
            AdConfiguration config,
            AdAnalyticsManager analytics,
            AdRevenueTracker revenueTracker,
            RewardTransactionManager rewardTxManager,
            AdFailureRecovery failureRecovery)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _analytics = analytics;
            _revenueTracker = revenueTracker;
            _rewardTxManager = rewardTxManager;
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
                Debug.Log("[RewardedAd] Already loading — skipping duplicate request.");
                return;
            }

            if (_state == AdState.Ready)
            {
                Debug.Log("[RewardedAd] Already loaded and ready.");
                return;
            }

#if UNITY_ANDROID
            _state = AdState.Loading;
            string adUnitId = _config.GetAdUnitId(AdFormat.Rewarded);

            Debug.Log($"[RewardedAd] Loading ad (unit: {(adUnitId.Length > 20 ? adUnitId[..20] + "..." : adUnitId)})");
            _analytics?.TrackEvent(AdEventType.Request, AdFormat.Rewarded, AdPlacement.RewardBonusGems);

            var adRequest = new AdRequest();

            RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                MainThreadDispatcher.Enqueue(() => HandleLoadResult(ad, error));
            });
#else
            Debug.LogWarning("[RewardedAd] Not supported on this platform.");
            _state = AdState.Failed;
#endif
        }

#if UNITY_ANDROID
        private void HandleLoadResult(RewardedAd ad, LoadAdError error)
        {
            if (_isDestroyed) return;

            if (error != null || ad == null)
            {
                string errorMsg = error?.GetMessage() ?? "Unknown error (null ad)";
                Debug.LogWarning($"[RewardedAd] Load failed: {errorMsg}");
                _state = AdState.Failed;

                _analytics?.TrackEvent(AdEventType.LoadFailure, AdFormat.Rewarded,
                    AdPlacement.RewardBonusGems, errorMsg);
                OnAdFailedToLoad?.Invoke(AdFormat.Rewarded, errorMsg);

                // Schedule retry
                _failureRecovery?.ScheduleRetry(AdFormat.Rewarded, () => Load());
                return;
            }

            _rewardedAd = ad;
            _state = AdState.Ready;
            _failureRecovery?.ResetRetryCount(AdFormat.Rewarded);

            Debug.Log("[RewardedAd] Loaded successfully.");
            _analytics?.TrackEvent(AdEventType.LoadSuccess, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems);

            // Register callbacks
            RegisterCallbacks(ad);

            OnAdLoaded?.Invoke(AdFormat.Rewarded);
        }

        private void RegisterCallbacks(RewardedAd ad)
        {
            // Paid event — impression-level revenue
            ad.OnAdPaid += (AdValue adValue) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log($"[RewardedAd] Paid: {adValue.Value} {adValue.CurrencyCode} " +
                              $"(Precision: {adValue.Precision})");
                    _revenueTracker?.RecordRevenue(AdFormat.Rewarded, AdPlacement.RewardBonusGems,
                        adValue.CurrencyCode, adValue.Value);
                    _analytics?.TrackRevenue(AdFormat.Rewarded, AdPlacement.RewardBonusGems,
                        adValue.CurrencyCode, adValue.Value);
                });
            };

            // Impression
            ad.OnAdImpressionRecorded += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log("[RewardedAd] Impression recorded.");
                    _analytics?.TrackEvent(AdEventType.Impression, AdFormat.Rewarded,
                        AdPlacement.RewardBonusGems);
                    OnAdImpression?.Invoke(AdFormat.Rewarded);
                });
            };

            // Click
            ad.OnAdClicked += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log("[RewardedAd] Clicked.");
                    _analytics?.TrackEvent(AdEventType.Click, AdFormat.Rewarded,
                        AdPlacement.RewardBonusGems);
                });
            };

            // Full screen opened
            ad.OnAdFullScreenContentOpened += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log("[RewardedAd] Opened full screen.");
                    _state = AdState.Showing;
                    _analytics?.TrackEvent(AdEventType.Opened, AdFormat.Rewarded,
                        AdPlacement.RewardBonusGems);
                    OnAdOpened?.Invoke(AdFormat.Rewarded);
                });
            };

            // Full screen closed
            ad.OnAdFullScreenContentClosed += () =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log("[RewardedAd] Closed.");
                    _state = AdState.Closed;

                    _analytics?.TrackEvent(AdEventType.Dismissed, AdFormat.Rewarded,
                        AdPlacement.RewardBonusGems);

                    // Clean up old ad — never reuse consumed ad objects
                    DestroyCurrentAd();

                    OnAdClosed?.Invoke(AdFormat.Rewarded);

                    // Auto-reload for next opportunity
                    Load();
                });
            };

            // Full screen content failed to show
            ad.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.LogWarning($"[RewardedAd] Failed to show: {adError.GetMessage()}");
                    _state = AdState.Failed;

                    DestroyCurrentAd();

                    _analytics?.TrackEvent(AdEventType.LoadFailure, AdFormat.Rewarded,
                        AdPlacement.RewardBonusGems, adError.GetMessage());
                    OnAdFailedToLoad?.Invoke(AdFormat.Rewarded, adError.GetMessage());

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
                Debug.LogWarning("[RewardedAd] Cannot show — not ready.");
                return;
            }

#if UNITY_ANDROID
            if (_rewardedAd == null)
            {
                Debug.LogError("[RewardedAd] Ad object is null despite Ready state.");
                _state = AdState.Failed;
                Load();
                return;
            }

            _rewardEarnedThisShow = false;
            _currentTransactionId = _rewardTxManager.BeginTransaction();

            Debug.Log($"[RewardedAd] Showing ad (tx: {_currentTransactionId})");
            _analytics?.TrackEvent(AdEventType.Opened, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems);

            _rewardedAd.Show((Reward reward) =>
            {
                MainThreadDispatcher.Enqueue(() => HandleRewardCallback(reward));
            });
#endif
        }

#if UNITY_ANDROID
        private void HandleRewardCallback(Reward reward)
        {
            if (_rewardEarnedThisShow)
            {
                Debug.LogWarning("[RewardedAd] Duplicate reward callback detected — ignoring.");
                return;
            }

            if (string.IsNullOrEmpty(_currentTransactionId))
            {
                Debug.LogError("[RewardedAd] No active transaction for reward — ignoring.");
                return;
            }

            // Check for duplicate transaction
            if (!_rewardTxManager.CanGrantReward(_currentTransactionId))
            {
                Debug.LogWarning($"[RewardedAd] Transaction {_currentTransactionId} " +
                                 "already processed — preventing duplicate reward.");
                return;
            }

            _rewardEarnedThisShow = true;

            string rewardType = reward.Type;
            double rewardAmount = reward.Amount;

            Debug.Log($"[RewardedAd] Reward earned: {rewardAmount} {rewardType} " +
                      $"(tx: {_currentTransactionId})");

            // Mark transaction as granted
            _rewardTxManager.GrantReward(_currentTransactionId, rewardType, rewardAmount);

            _analytics?.TrackEvent(AdEventType.RewardEarned, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems, $"{rewardAmount} {rewardType}");
            _analytics?.TrackEvent(AdEventType.RewardGranted, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems, $"tx:{_currentTransactionId}");

            OnRewardEarned?.Invoke(rewardType, rewardAmount);
        }
#endif

        // =====================================================================
        // Cleanup
        // =====================================================================

        private void DestroyCurrentAd()
        {
#if UNITY_ANDROID
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
#endif
            _currentTransactionId = null;
        }

        public void Destroy()
        {
            _isDestroyed = true;
            DestroyCurrentAd();
            _state = AdState.NotLoaded;
        }
    }
}
