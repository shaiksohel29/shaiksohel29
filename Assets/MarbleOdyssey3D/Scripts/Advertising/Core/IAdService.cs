// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// IAdService.cs — Core interfaces for the advertising system
// ============================================================================

using System;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Represents the current state of an ad unit.
    /// </summary>
    public enum AdState
    {
        NotLoaded,
        Loading,
        Ready,
        Showing,
        Closed,
        Failed
    }

    /// <summary>
    /// Reward transaction states for the rewarded ad state machine.
    /// </summary>
    public enum RewardTransactionState
    {
        NotLoaded,
        Loading,
        Ready,
        Showing,
        RewardPending,
        RewardGranted,
        Closed,
        Failed
    }

    /// <summary>
    /// Ad format types supported by the system.
    /// </summary>
    public enum AdFormat
    {
        Rewarded,
        Interstitial,
        Banner,
        AppOpen
    }

    /// <summary>
    /// Placement identifiers for ad insertion points.
    /// </summary>
    public enum AdPlacement
    {
        LevelComplete,
        BetweenLevels,
        WorldSelectionReturn,
        RewardBonusGems,
        RewardCosmeticCurrency,
        RewardSecondChance,
        RewardChallengeMultiplier,
        RewardDailyChallenge,
        MenuBanner,
        SettingsBanner
    }

    /// <summary>
    /// Analytics event types tracked by the ad system.
    /// </summary>
    public enum AdEventType
    {
        Request,
        LoadSuccess,
        LoadFailure,
        Impression,
        Click,
        Opened,
        Dismissed,
        RewardEarned,
        RewardGranted,
        InterstitialSkippedByPolicy,
        AdUnavailable,
        RevenueCallback
    }

    /// <summary>
    /// Core interface for ad controllers.
    /// </summary>
    public interface IAdController
    {
        AdFormat Format { get; }
        AdState State { get; }
        bool IsReady { get; }

        void Load();
        void Destroy();

        event Action<AdFormat> OnAdLoaded;
        event Action<AdFormat, string> OnAdFailedToLoad;
        event Action<AdFormat> OnAdOpened;
        event Action<AdFormat> OnAdClosed;
    }

    /// <summary>
    /// Interface for full-screen ad controllers (interstitial, rewarded).
    /// </summary>
    public interface IFullScreenAdController : IAdController
    {
        void Show();
        event Action<AdFormat> OnAdImpression;
    }

    /// <summary>
    /// Interface for rewarded ad controllers.
    /// </summary>
    public interface IRewardedAdController : IFullScreenAdController
    {
        event Action<string, double> OnRewardEarned;
    }

    /// <summary>
    /// Interface for the ad placement policy engine.
    /// </summary>
    public interface IAdPlacementPolicy
    {
        bool CanShowAd(AdFormat format, AdPlacement placement);
        void RecordImpression(AdFormat format, AdPlacement placement);
        void Reset();
    }

    /// <summary>
    /// Interface for network connectivity checking.
    /// </summary>
    public interface INetworkService
    {
        bool IsConnected { get; }
        event Action<bool> OnConnectivityChanged;
        void StartMonitoring();
        void StopMonitoring();
    }

    /// <summary>
    /// Interface for consent management.
    /// </summary>
    public interface IConsentManager
    {
        bool CanRequestAds { get; }
        bool IsConsentFormAvailable { get; }
        void RequestConsentInfoUpdate();
        void ShowConsentForm(Action<string> onComplete);
        void ShowPrivacyOptionsForm(Action<string> onComplete);
    }

    /// <summary>
    /// Interface for ad analytics tracking.
    /// </summary>
    public interface IAdAnalytics
    {
        void TrackEvent(AdEventType eventType, AdFormat format, AdPlacement placement,
            string detail = null);
        void TrackRevenue(AdFormat format, AdPlacement placement, string currencyCode,
            long valueMicros);
    }

    /// <summary>
    /// Interface for the Remove Ads purchase entitlement.
    /// </summary>
    public interface IRemoveAdsEntitlement
    {
        bool IsRemoveAdsPurchased { get; }
        event Action<bool> OnEntitlementChanged;
        void PurchaseRemoveAds();
        void RestorePurchases();
    }
}
