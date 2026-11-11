// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdMobManager.cs — Central orchestrator for the advertising system
// ============================================================================

using System;
using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Central singleton manager that orchestrates all advertising subsystems.
    /// Attach to a persistent GameObject. Requires an AdConfiguration asset reference.
    /// </summary>
    public class AdMobManager : MonoBehaviour
    {
        // =====================================================================
        // Singleton
        // =====================================================================

        private static AdMobManager _instance;
        public static AdMobManager Instance => _instance;

        // =====================================================================
        // Configuration
        // =====================================================================

        [Header("Configuration")]
        [SerializeField] private AdConfiguration _adConfig;
        public AdConfiguration Config => _adConfig;

        // =====================================================================
        // Subsystem References (populated during initialization)
        // =====================================================================

        private AdInitializationService _initService;
        private ConsentManager _consentManager;
        private NetworkConnectivityService _networkService;
        private AdPlacementPolicy _placementPolicy;
        private AdFrequencyManager _frequencyManager;
        private RewardedAdController _rewardedController;
        private InterstitialAdController _interstitialController;
        private BannerAdController _bannerController;
        private AdAnalyticsManager _analyticsManager;
        private AdRevenueTracker _revenueTracker;
        private RewardTransactionManager _rewardTransactionManager;
        private AdFailureRecovery _failureRecovery;
        private AdLifecycleManager _lifecycleManager;
        private AdTransitionManager _transitionManager;
        private AdAudioManager _adAudioManager;
        private MonetizationDiagnostics _diagnostics;
        private RemoveAdsManager _removeAdsManager;

        // =====================================================================
        // Public Accessors
        // =====================================================================

        public AdInitializationService InitService => _initService;
        public ConsentManager Consent => _consentManager;
        public NetworkConnectivityService Network => _networkService;
        public AdPlacementPolicy PlacementPolicy => _placementPolicy;
        public AdFrequencyManager FrequencyManager => _frequencyManager;
        public RewardedAdController RewardedAds => _rewardedController;
        public InterstitialAdController InterstitialAds => _interstitialController;
        public BannerAdController BannerAds => _bannerController;
        public AdAnalyticsManager Analytics => _analyticsManager;
        public AdRevenueTracker Revenue => _revenueTracker;
        public RewardTransactionManager RewardTransactions => _rewardTransactionManager;
        public AdFailureRecovery FailureRecovery => _failureRecovery;
        public AdLifecycleManager Lifecycle => _lifecycleManager;
        public AdTransitionManager Transitions => _transitionManager;
        public AdAudioManager AudioManager => _adAudioManager;
        public MonetizationDiagnostics Diagnostics => _diagnostics;
        public RemoveAdsManager RemoveAds => _removeAdsManager;

        public bool IsInitialized => _initService != null && _initService.IsInitialized;
        public bool IsAdFreeUser => _removeAdsManager != null && _removeAdsManager.IsRemoveAdsPurchased;

        // =====================================================================
        // Events
        // =====================================================================

        /// <summary>Fired after all subsystems are initialized and ads can be loaded.</summary>
        public event Action OnSystemReady;

        // =====================================================================
        // Lifecycle
        // =====================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[AdMobManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_adConfig == null)
            {
                Debug.LogError("[AdMobManager] AdConfiguration asset is not assigned! " +
                               "Assign it in the Inspector.");
                return;
            }

            MainThreadDispatcher.EnsureExists();
        }

        private void Start()
        {
            BeginInitialization();
        }

        private void OnDestroy()
        {
            Shutdown();
            if (_instance == this) _instance = null;
        }

        // =====================================================================
        // Initialization Pipeline
        // =====================================================================

        /// <summary>
        /// Begins the full initialization pipeline:
        /// 1. Network check
        /// 2. Consent flow
        /// 3. SDK initialization
        /// 4. Subsystem startup
        /// 5. Ad preloading
        /// </summary>
        private void BeginInitialization()
        {
            Debug.Log("[AdMobManager] Starting initialization pipeline...");

            // Validate configuration
            if (!_adConfig.testMode && !_adConfig.ValidateProductionIds())
            {
                Debug.LogError("[AdMobManager] Production ID validation failed! " +
                               "Ads will not initialize.");
                return;
            }

            // Create subsystems that don't depend on SDK init
            CreatePreInitSubsystems();

            // Start network monitoring
            _networkService.StartMonitoring();

            // Step 1: Consent flow first
            _consentManager.RequestConsentInfoUpdate();
            _consentManager.OnConsentComplete += HandleConsentComplete;
        }

        private void CreatePreInitSubsystems()
        {
            _networkService = new NetworkConnectivityService();
            _consentManager = new ConsentManager(_adConfig);
            _analyticsManager = new AdAnalyticsManager();
            _revenueTracker = new AdRevenueTracker(_analyticsManager);
            _frequencyManager = new AdFrequencyManager(_adConfig);
            _placementPolicy = new AdPlacementPolicy(_adConfig, _frequencyManager);
            _rewardTransactionManager = new RewardTransactionManager();
            _diagnostics = new MonetizationDiagnostics(_analyticsManager, _revenueTracker);
            _removeAdsManager = new RemoveAdsManager();

            Debug.Log("[AdMobManager] Pre-init subsystems created.");
        }

        private void HandleConsentComplete(bool canRequestAds)
        {
            _consentManager.OnConsentComplete -= HandleConsentComplete;

            if (!canRequestAds)
            {
                Debug.LogWarning("[AdMobManager] Cannot request ads (consent not obtained). " +
                                 "Ads will remain disabled.");
                return;
            }

            // Step 2: Initialize SDK
            _initService = new AdInitializationService(_adConfig);
            _initService.Initialize(OnSdkInitialized);
        }

        private void OnSdkInitialized(bool success)
        {
            if (!success)
            {
                Debug.LogError("[AdMobManager] SDK initialization failed.");
                return;
            }

            Debug.Log("[AdMobManager] SDK initialized. Creating ad controllers...");

            // Step 3: Create post-init subsystems
            CreatePostInitSubsystems();

            // Step 4: Preload ads
            PreloadAds();

            Debug.Log("[AdMobManager] System fully initialized and ready.");
            OnSystemReady?.Invoke();
        }

        private void CreatePostInitSubsystems()
        {
            _failureRecovery = new AdFailureRecovery(_adConfig);

            if (_adConfig.IsFormatEnabled(AdFormat.Rewarded))
            {
                _rewardedController = new RewardedAdController(
                    _adConfig, _analyticsManager, _revenueTracker,
                    _rewardTransactionManager, _failureRecovery);
            }

            if (_adConfig.IsFormatEnabled(AdFormat.Interstitial))
            {
                _interstitialController = new InterstitialAdController(
                    _adConfig, _analyticsManager, _revenueTracker, _failureRecovery);
            }

            if (_adConfig.IsFormatEnabled(AdFormat.Banner))
            {
                _bannerController = new BannerAdController(
                    _adConfig, _analyticsManager, _revenueTracker);
            }

            _lifecycleManager = new AdLifecycleManager(this);
            _transitionManager = new AdTransitionManager(this);
            _adAudioManager = new AdAudioManager();

            Debug.Log("[AdMobManager] Post-init subsystems created.");
        }

        private void PreloadAds()
        {
            if (_rewardedController != null)
            {
                _rewardedController.Load();
                Debug.Log("[AdMobManager] Rewarded ad preload requested.");
            }

            if (_interstitialController != null)
            {
                _interstitialController.Load();
                Debug.Log("[AdMobManager] Interstitial ad preload requested.");
            }
        }

        // =====================================================================
        // Public API — High-Level Ad Operations
        // =====================================================================

        /// <summary>
        /// Attempt to show an interstitial at the given placement.
        /// Checks policy, frequency, consent, connectivity, and Remove Ads status.
        /// Returns true if the ad will be shown.
        /// </summary>
        public bool TryShowInterstitial(AdPlacement placement, Action onComplete = null)
        {
            if (!IsInitialized || _interstitialController == null)
            {
                onComplete?.Invoke();
                return false;
            }

            if (IsAdFreeUser)
            {
                Debug.Log("[AdMobManager] Remove Ads active — skipping interstitial.");
                _analyticsManager?.TrackEvent(AdEventType.InterstitialSkippedByPolicy,
                    AdFormat.Interstitial, placement, "remove_ads_purchased");
                onComplete?.Invoke();
                return false;
            }

            if (!_placementPolicy.CanShowAd(AdFormat.Interstitial, placement))
            {
                Debug.Log($"[AdMobManager] Interstitial blocked by placement policy at {placement}.");
                _analyticsManager?.TrackEvent(AdEventType.InterstitialSkippedByPolicy,
                    AdFormat.Interstitial, placement, "policy_blocked");
                onComplete?.Invoke();
                return false;
            }

            if (!_interstitialController.IsReady)
            {
                Debug.Log("[AdMobManager] Interstitial not ready.");
                _analyticsManager?.TrackEvent(AdEventType.AdUnavailable,
                    AdFormat.Interstitial, placement);
                onComplete?.Invoke();
                return false;
            }

            _adAudioManager?.PauseGameAudio();
            _interstitialController.OnAdClosed += HandleInterstitialClosed;

            void HandleInterstitialClosed(AdFormat _)
            {
                _interstitialController.OnAdClosed -= HandleInterstitialClosed;
                _adAudioManager?.ResumeGameAudio();
                _placementPolicy.RecordImpression(AdFormat.Interstitial, placement);
                onComplete?.Invoke();
            }

            _interstitialController.Show();
            return true;
        }

        /// <summary>
        /// Show a rewarded ad for the given placement.
        /// The onRewardGranted callback fires only after a verified reward callback.
        /// The onComplete callback fires when the ad closes (with or without reward).
        /// Returns true if the ad will be shown.
        /// </summary>
        public bool TryShowRewarded(AdPlacement placement,
            Action<string, double> onRewardGranted, Action onComplete = null)
        {
            if (!IsInitialized || _rewardedController == null)
            {
                onComplete?.Invoke();
                return false;
            }

            if (!_rewardedController.IsReady)
            {
                Debug.Log("[AdMobManager] Rewarded ad not ready.");
                _analyticsManager?.TrackEvent(AdEventType.AdUnavailable,
                    AdFormat.Rewarded, placement);
                onComplete?.Invoke();
                return false;
            }

            _adAudioManager?.PauseGameAudio();

            Action<string, double> rewardHandler = null;
            Action<AdFormat> closedHandler = null;

            rewardHandler = (type, amount) =>
            {
                _rewardedController.OnRewardEarned -= rewardHandler;
                onRewardGranted?.Invoke(type, amount);
                _placementPolicy.RecordImpression(AdFormat.Rewarded, placement);
            };

            closedHandler = (_) =>
            {
                _rewardedController.OnRewardEarned -= rewardHandler;
                _rewardedController.OnAdClosed -= closedHandler;
                _adAudioManager?.ResumeGameAudio();
                onComplete?.Invoke();
            };

            _rewardedController.OnRewardEarned += rewardHandler;
            _rewardedController.OnAdClosed += closedHandler;

            _rewardedController.Show();
            return true;
        }

        /// <summary>
        /// Check if a rewarded ad is available for the player.
        /// Use this to enable/disable reward buttons in the UI.
        /// </summary>
        public bool IsRewardedAdAvailable()
        {
            return IsInitialized &&
                   _rewardedController != null &&
                   _rewardedController.IsReady;
        }

        /// <summary>
        /// Check if an interstitial is loaded and eligible at the given placement.
        /// </summary>
        public bool IsInterstitialEligible(AdPlacement placement)
        {
            return IsInitialized &&
                   _interstitialController != null &&
                   _interstitialController.IsReady &&
                   !IsAdFreeUser &&
                   _placementPolicy.CanShowAd(AdFormat.Interstitial, placement);
        }

        // =====================================================================
        // Shutdown
        // =====================================================================

        private void Shutdown()
        {
            Debug.Log("[AdMobManager] Shutting down...");

            _networkService?.StopMonitoring();
            _rewardedController?.Destroy();
            _interstitialController?.Destroy();
            _bannerController?.Destroy();
            _failureRecovery?.CancelAll();

            Debug.Log("[AdMobManager] Shutdown complete.");
        }
    }
}
