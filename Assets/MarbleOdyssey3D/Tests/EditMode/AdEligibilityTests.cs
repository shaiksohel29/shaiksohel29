// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdEligibilityTests.cs — Unit tests for ad eligibility decisions
// ============================================================================

using NUnit.Framework;
using UnityEngine;

namespace MarbleOdyssey.Advertising.Tests
{
    [TestFixture]
    public class AdEligibilityTests
    {
        private AdConfiguration _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<AdConfiguration>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // =====================================================================
        // AdConfiguration Tests
        // =====================================================================

        [Test]
        public void GetAdUnitId_TestMode_ReturnsTestIds()
        {
            _config.testMode = true;

            Assert.AreEqual(AdConfiguration.TestAdUnits.Rewarded,
                _config.GetAdUnitId(AdFormat.Rewarded));
            Assert.AreEqual(AdConfiguration.TestAdUnits.Interstitial,
                _config.GetAdUnitId(AdFormat.Interstitial));
            Assert.AreEqual(AdConfiguration.TestAdUnits.Banner,
                _config.GetAdUnitId(AdFormat.Banner));
        }

        [Test]
        public void GetAdUnitId_ProductionMode_ReturnsConfiguredIds()
        {
            _config.testMode = false;
            _config.rewardedAdUnitId = "ca-app-pub-test/rewarded";
            _config.interstitialAdUnitId = "ca-app-pub-test/interstitial";

            Assert.AreEqual("ca-app-pub-test/rewarded",
                _config.GetAdUnitId(AdFormat.Rewarded));
            Assert.AreEqual("ca-app-pub-test/interstitial",
                _config.GetAdUnitId(AdFormat.Interstitial));
        }

        [Test]
        public void IsFormatEnabled_RespectConfiguration()
        {
            _config.enableRewardedAds = true;
            _config.enableInterstitials = false;
            _config.enableBannerAds = false;
            _config.enableAppOpenAds = false;

            Assert.IsTrue(_config.IsFormatEnabled(AdFormat.Rewarded));
            Assert.IsFalse(_config.IsFormatEnabled(AdFormat.Interstitial));
            Assert.IsFalse(_config.IsFormatEnabled(AdFormat.Banner));
            Assert.IsFalse(_config.IsFormatEnabled(AdFormat.AppOpen));
        }

        [Test]
        public void ValidateProductionIds_TestMode_AlwaysTrue()
        {
            _config.testMode = true;
            // Even with placeholder IDs, test mode passes validation
            Assert.IsTrue(_config.ValidateProductionIds());
        }

        [Test]
        public void ValidateProductionIds_WithPlaceholders_ReturnsFalse()
        {
            _config.testMode = false;
            _config.enableRewardedAds = true;
            _config.rewardedAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX";

            Assert.IsFalse(_config.ValidateProductionIds());
        }

        [Test]
        public void ValidateProductionIds_WithRealIds_ReturnsTrue()
        {
            _config.testMode = false;
            _config.androidAppId = "ca-app-pub-1234567890123456~1234567890";
            _config.enableRewardedAds = true;
            _config.rewardedAdUnitId = "ca-app-pub-1234567890123456/1234567890";
            _config.enableInterstitials = true;
            _config.interstitialAdUnitId = "ca-app-pub-1234567890123456/0987654321";
            _config.enableBannerAds = false;
            _config.enableAppOpenAds = false;

            Assert.IsTrue(_config.ValidateProductionIds());
        }

        [Test]
        public void ValidateProductionIds_EmptyString_ReturnsFalse()
        {
            _config.testMode = false;
            _config.enableRewardedAds = true;
            _config.rewardedAdUnitId = "";

            Assert.IsFalse(_config.ValidateProductionIds());
        }

        // =====================================================================
        // Analytics Tests
        // =====================================================================

        [Test]
        public void Analytics_TrackEvent_IncrementsCounter()
        {
            var analytics = new AdAnalyticsManager();

            analytics.TrackEvent(AdEventType.Impression, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems);
            analytics.TrackEvent(AdEventType.Impression, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems);

            Assert.AreEqual(2, analytics.GetEventCount(AdEventType.Impression));
        }

        [Test]
        public void Analytics_FillRate_CalculatedCorrectly()
        {
            var analytics = new AdAnalyticsManager();

            // 3 requests, 2 successes = 66.7% fill rate
            analytics.TrackEvent(AdEventType.Request, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems);
            analytics.TrackEvent(AdEventType.Request, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems);
            analytics.TrackEvent(AdEventType.Request, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems);

            analytics.TrackEvent(AdEventType.LoadSuccess, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems);
            analytics.TrackEvent(AdEventType.LoadSuccess, AdFormat.Rewarded,
                AdPlacement.RewardBonusGems);

            float fillRate = analytics.GetFillRate(AdFormat.Rewarded);
            Assert.AreEqual(2f / 3f, fillRate, 0.01f);
        }

        [Test]
        public void Analytics_FillRate_NoRequests_ReturnsNegative()
        {
            var analytics = new AdAnalyticsManager();

            float fillRate = analytics.GetFillRate(AdFormat.Rewarded);
            Assert.Less(fillRate, 0f, "Fill rate with no requests should be -1.");
        }

        [Test]
        public void Analytics_RewardedCompletion_CalculatedCorrectly()
        {
            var analytics = new AdAnalyticsManager();

            // 4 impressions, 3 rewards = 75%
            for (int i = 0; i < 4; i++)
                analytics.TrackEvent(AdEventType.Impression, AdFormat.Rewarded,
                    AdPlacement.RewardBonusGems);

            for (int i = 0; i < 3; i++)
                analytics.TrackEvent(AdEventType.RewardEarned, AdFormat.Rewarded,
                    AdPlacement.RewardBonusGems);

            float rate = analytics.GetRewardedCompletionRate();
            Assert.AreEqual(0.75f, rate, 0.01f);
        }

        // =====================================================================
        // Revenue Tracker Tests
        // =====================================================================

        [Test]
        public void Revenue_TracksAccumulation()
        {
            var analytics = new AdAnalyticsManager();
            var tracker = new AdRevenueTracker(analytics);

            tracker.RecordRevenue(AdFormat.Rewarded, AdPlacement.RewardBonusGems,
                "USD", 15000); // $0.015
            tracker.RecordRevenue(AdFormat.Interstitial, AdPlacement.LevelComplete,
                "USD", 8000);  // $0.008

            Assert.AreEqual(23000, tracker.GetSessionRevenueMicros());
            Assert.AreEqual(2, tracker.GetRevenueEventCount());
        }

        [Test]
        public void Revenue_DecimalConversion()
        {
            var analytics = new AdAnalyticsManager();
            var tracker = new AdRevenueTracker(analytics);

            tracker.RecordRevenue(AdFormat.Rewarded, AdPlacement.RewardBonusGems,
                "USD", 1_000_000); // $1.00

            Assert.AreEqual(1.0m, tracker.GetSessionRevenueDecimal());
        }
    }
}
