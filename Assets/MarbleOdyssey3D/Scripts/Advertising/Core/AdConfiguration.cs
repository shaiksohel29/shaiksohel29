// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdConfiguration.cs — Centralized ScriptableObject configuration asset
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Centralized ad configuration asset. Create via Assets > Create > Marble Odyssey > Ad Configuration.
    /// Never hardcode live Ad Unit IDs in source files — use this asset with environment-specific overrides.
    /// </summary>
    [CreateAssetMenu(fileName = "AdConfiguration", menuName = "Marble Odyssey/Ad Configuration")]
    public class AdConfiguration : ScriptableObject
    {
        // =====================================================================
        // Environment Settings
        // =====================================================================

        [Header("Environment")]
        [Tooltip("Enable test mode. MUST be true during development. Production builds must set this to false.")]
        public bool testMode = true;

        [Tooltip("Additional test device IDs for real-device testing (hashed device IDs from logcat).")]
        public string[] testDeviceIds = new string[0];

        // =====================================================================
        // Ad Unit IDs — Use placeholders until real IDs are provided.
        // In test mode, official Google test ad unit IDs are used automatically.
        // =====================================================================

        [Header("Android Ad Unit IDs (Production)")]
        [Tooltip("AdMob Android App ID. Must match AndroidManifest.xml.")]
        public string androidAppId = "ca-app-pub-XXXXXXXXXXXXXXXX~XXXXXXXXXX";

        [Tooltip("Rewarded ad unit ID for production.")]
        public string rewardedAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX";

        [Tooltip("Interstitial ad unit ID for production.")]
        public string interstitialAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX";

        [Tooltip("Banner ad unit ID for production. Leave empty to disable banners.")]
        public string bannerAdUnitId = "";

        [Tooltip("App Open ad unit ID for production. Leave empty — not enabled by default.")]
        public string appOpenAdUnitId = "";

        // =====================================================================
        // Official Google Test Ad Unit IDs (Android)
        // https://developers.google.com/admob/android/test-ads
        // =====================================================================

        public static class TestAdUnits
        {
            public const string Rewarded = "ca-app-pub-3940256099942544/5224354917";
            public const string Interstitial = "ca-app-pub-3940256099942544/1033173712";
            public const string Banner = "ca-app-pub-3940256099942544/6300978111";
            public const string AppOpen = "ca-app-pub-3940256099942544/9257395921";
        }

        // =====================================================================
        // Frequency Cap Settings
        // =====================================================================

        [Header("Interstitial Frequency Caps")]
        [Tooltip("Number of initial level completions before interstitials are eligible.")]
        [Min(0)]
        public int interstitialGraceLevels = 3;

        [Tooltip("Minimum seconds between interstitial impressions.")]
        [Min(30)]
        public float interstitialMinIntervalSeconds = 180f;

        [Tooltip("Minimum eligible level completions between interstitials.")]
        [Min(1)]
        public int interstitialLevelInterval = 3;

        [Tooltip("Maximum interstitial impressions per rolling 30-minute window.")]
        [Min(1)]
        public int interstitialMaxPer30Min = 4;

        [Tooltip("Minimum session duration (seconds) before first interstitial.")]
        [Min(0)]
        public float interstitialMinSessionAge = 60f;

        // =====================================================================
        // Reward Configuration
        // =====================================================================

        [Header("Reward Values")]
        [Min(1)]
        public int rewardBonusGems = 50;

        [Min(1)]
        public int rewardCosmeticCurrency = 25;

        [Min(1)]
        public int rewardChallengeMultiplier = 2;

        [Min(1)]
        public int rewardDailyChallengeBonus = 100;

        // =====================================================================
        // Retry / Backoff Settings
        // =====================================================================

        [Header("Ad Loading Retry")]
        [Tooltip("Maximum number of retry attempts after a load failure.")]
        [Range(1, 10)]
        public int maxRetryAttempts = 5;

        [Tooltip("Initial retry delay in seconds (doubles each attempt).")]
        [Min(1f)]
        public float initialRetryDelaySeconds = 2f;

        [Tooltip("Maximum retry delay cap in seconds.")]
        [Min(5f)]
        public float maxRetryDelaySeconds = 120f;

        // =====================================================================
        // Placement Toggles
        // =====================================================================

        [Header("Placement Toggles")]
        public bool enableInterstitials = true;
        public bool enableRewardedAds = true;
        public bool enableBannerAds = false;
        public bool enableAppOpenAds = false;

        // =====================================================================
        // Consent / Privacy
        // =====================================================================

        [Header("Privacy")]
        [Tooltip("Tag for under-age-of-consent treatment (COPPA/GDPR child).")]
        public bool tagForUnderAgeOfConsent = false;

        [Tooltip("Tag for child-directed treatment (COPPA).")]
        public bool tagForChildDirectedTreatment = false;

        // =====================================================================
        // Helper Methods
        // =====================================================================

        /// <summary>
        /// Returns the correct ad unit ID based on the current environment.
        /// In test mode, returns official Google test IDs.
        /// </summary>
        public string GetAdUnitId(AdFormat format)
        {
            if (testMode)
            {
                return format switch
                {
                    AdFormat.Rewarded => TestAdUnits.Rewarded,
                    AdFormat.Interstitial => TestAdUnits.Interstitial,
                    AdFormat.Banner => TestAdUnits.Banner,
                    AdFormat.AppOpen => TestAdUnits.AppOpen,
                    _ => ""
                };
            }

            return format switch
            {
                AdFormat.Rewarded => rewardedAdUnitId,
                AdFormat.Interstitial => interstitialAdUnitId,
                AdFormat.Banner => bannerAdUnitId,
                AdFormat.AppOpen => appOpenAdUnitId,
                _ => ""
            };
        }

        /// <summary>
        /// Returns true if the format is enabled in configuration.
        /// </summary>
        public bool IsFormatEnabled(AdFormat format)
        {
            return format switch
            {
                AdFormat.Rewarded => enableRewardedAds,
                AdFormat.Interstitial => enableInterstitials,
                AdFormat.Banner => enableBannerAds,
                AdFormat.AppOpen => enableAppOpenAds,
                _ => false
            };
        }

        /// <summary>
        /// Validates that production IDs are not placeholders.
        /// Returns true if all enabled formats have non-placeholder IDs.
        /// </summary>
        public bool ValidateProductionIds()
        {
            if (testMode) return true;

            bool valid = true;

            if (enableRewardedAds && IsPlaceholder(rewardedAdUnitId))
            {
                Debug.LogError("[AdConfig] Rewarded ad unit ID is still a placeholder!");
                valid = false;
            }
            if (enableInterstitials && IsPlaceholder(interstitialAdUnitId))
            {
                Debug.LogError("[AdConfig] Interstitial ad unit ID is still a placeholder!");
                valid = false;
            }
            if (enableBannerAds && IsPlaceholder(bannerAdUnitId))
            {
                Debug.LogError("[AdConfig] Banner ad unit ID is still a placeholder!");
                valid = false;
            }
            if (IsPlaceholder(androidAppId))
            {
                Debug.LogError("[AdConfig] Android App ID is still a placeholder!");
                valid = false;
            }

            return valid;
        }

        private static bool IsPlaceholder(string id)
        {
            return string.IsNullOrEmpty(id) || id.Contains("XXXXX");
        }
    }
}
