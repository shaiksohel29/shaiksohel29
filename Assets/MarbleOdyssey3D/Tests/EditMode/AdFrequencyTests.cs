// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdFrequencyTests.cs — Unit tests for ad frequency capping
// ============================================================================

using NUnit.Framework;
using UnityEngine;

namespace MarbleOdyssey.Advertising.Tests
{
    [TestFixture]
    public class AdFrequencyTests
    {
        private AdConfiguration _config;
        private AdFrequencyManager _frequencyManager;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<AdConfiguration>();
            _config.interstitialGraceLevels = 3;
            _config.interstitialMinIntervalSeconds = 180f;
            _config.interstitialLevelInterval = 3;
            _config.interstitialMaxPer30Min = 4;

            _frequencyManager = new AdFrequencyManager(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void CanShowInterstitial_WhenFresh_ReturnsTrue()
        {
            // Fresh state: no impressions, time at negative infinity
            // Need to record enough level completions first
            for (int i = 0; i < 3; i++)
                _frequencyManager.RecordLevelCompletion();

            // Note: This test may need adjustment because Time.realtimeSinceStartup
            // is used internally. In a fresh test, the time since "negative infinity"
            // should be sufficient.
            bool result = _frequencyManager.CanShowInterstitial();
            Assert.IsTrue(result, "Should allow interstitial when all caps are clear.");
        }

        [Test]
        public void CanShowInterstitial_BeforeLevelInterval_ReturnsFalse()
        {
            // Record an impression (resets level counter)
            _frequencyManager.RecordInterstitialImpression();

            // Only 1 level completed (need 3)
            _frequencyManager.RecordLevelCompletion();

            bool result = _frequencyManager.CanShowInterstitial();
            Assert.IsFalse(result, "Should block interstitial before level interval met.");
        }

        [Test]
        public void RecordLevelCompletion_IncrementsCounter()
        {
            _frequencyManager.RecordLevelCompletion();
            _frequencyManager.RecordLevelCompletion();

            var state = _frequencyManager.GetState();
            Assert.AreEqual(2, state.LevelsSinceLastInterstitial);
        }

        [Test]
        public void RecordImpression_ResetsLevelCounter()
        {
            _frequencyManager.RecordLevelCompletion();
            _frequencyManager.RecordLevelCompletion();
            _frequencyManager.RecordLevelCompletion();

            _frequencyManager.RecordInterstitialImpression();

            var state = _frequencyManager.GetState();
            Assert.AreEqual(0, state.LevelsSinceLastInterstitial);
        }

        [Test]
        public void RecordImpression_IncrementsWindowCount()
        {
            _frequencyManager.RecordInterstitialImpression();

            var state = _frequencyManager.GetState();
            Assert.AreEqual(1, state.ImpressionsInWindow);
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            _frequencyManager.RecordLevelCompletion();
            _frequencyManager.RecordInterstitialImpression();

            _frequencyManager.Reset();

            var state = _frequencyManager.GetState();
            Assert.AreEqual(0, state.LevelsSinceLastInterstitial);
            Assert.AreEqual(0, state.ImpressionsInWindow);
        }

        [Test]
        public void GetState_ReturnsConfiguredValues()
        {
            var state = _frequencyManager.GetState();
            Assert.AreEqual(4, state.MaxImpressionsInWindow);
            Assert.AreEqual(180f, state.MinIntervalSeconds);
            Assert.AreEqual(3, state.MinLevelInterval);
        }
    }

    [TestFixture]
    public class AdPlacementPolicyTests
    {
        private AdConfiguration _config;
        private AdFrequencyManager _frequencyManager;
        private AdPlacementPolicy _policy;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<AdConfiguration>();
            _config.interstitialGraceLevels = 3;
            _config.interstitialMinIntervalSeconds = 180f;
            _config.interstitialLevelInterval = 3;
            _config.interstitialMaxPer30Min = 4;
            _config.interstitialMinSessionAge = 60f;

            _frequencyManager = new AdFrequencyManager(_config);
            _policy = new AdPlacementPolicy(_config, _frequencyManager);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void CanShowAd_WhenAnotherAdShowing_ReturnsFalse()
        {
            _policy.SetAdShowing(true);

            bool result = _policy.CanShowAd(AdFormat.Interstitial, AdPlacement.LevelComplete);
            Assert.IsFalse(result, "Should block when another ad is showing.");
        }

        [Test]
        public void CanShowAd_DuringCinematic_ReturnsFalse()
        {
            _policy.SetCinematicActive(true);

            bool result = _policy.CanShowAd(AdFormat.Interstitial, AdPlacement.LevelComplete);
            Assert.IsFalse(result, "Should block during cinematics.");
        }

        [Test]
        public void CanShowAd_DuringPurchase_ReturnsFalse()
        {
            _policy.SetPurchaseFlowActive(true);

            bool result = _policy.CanShowAd(AdFormat.Interstitial, AdPlacement.LevelComplete);
            Assert.IsFalse(result, "Should block during purchase flow.");
        }

        [Test]
        public void CanShowAd_DuringTutorial_ReturnsFalse()
        {
            _policy.SetTutorialActive(true);

            bool result = _policy.CanShowAd(AdFormat.Rewarded, AdPlacement.RewardBonusGems);
            Assert.IsFalse(result, "Should block during tutorial.");
        }

        [Test]
        public void CanShowInterstitial_WhileMarbleMoving_ReturnsFalse()
        {
            _policy.SetLevelsCompleted(10);
            _policy.SetMarbleMoving(true);

            bool result = _policy.CanShowAd(AdFormat.Interstitial, AdPlacement.LevelComplete);
            Assert.IsFalse(result, "Should block interstitial during marble movement.");
        }

        [Test]
        public void CanShowInterstitial_DuringJump_ReturnsFalse()
        {
            _policy.SetLevelsCompleted(10);
            _policy.SetInJump(true);

            bool result = _policy.CanShowAd(AdFormat.Interstitial, AdPlacement.LevelComplete);
            Assert.IsFalse(result, "Should block interstitial during jump.");
        }

        [Test]
        public void CanShowInterstitial_BeforeGracePeriod_ReturnsFalse()
        {
            _policy.SetLevelsCompleted(2); // Grace = 3

            bool result = _policy.CanShowAd(AdFormat.Interstitial, AdPlacement.LevelComplete);
            Assert.IsFalse(result, "Should block before grace period.");
        }

        [Test]
        public void CanShowRewarded_AtValidPlacement_ReturnsTrue()
        {
            bool result = _policy.CanShowAd(AdFormat.Rewarded, AdPlacement.RewardBonusGems);
            Assert.IsTrue(result, "Should allow rewarded at valid placement.");
        }

        [Test]
        public void CanShowRewarded_AtInvalidPlacement_ReturnsFalse()
        {
            bool result = _policy.CanShowAd(AdFormat.Rewarded, AdPlacement.LevelComplete);
            Assert.IsFalse(result, "Should block rewarded at invalid placement.");
        }

        [Test]
        public void CanShowBanner_OnMenuScreen_ReturnsTrue()
        {
            bool result = _policy.CanShowAd(AdFormat.Banner, AdPlacement.MenuBanner);
            Assert.IsTrue(result, "Should allow banner on menu screen.");
        }

        [Test]
        public void CanShowBanner_OnGameplayScreen_ReturnsFalse()
        {
            bool result = _policy.CanShowAd(AdFormat.Banner, AdPlacement.LevelComplete);
            Assert.IsFalse(result, "Should block banner on gameplay-related placement.");
        }

        [Test]
        public void AppOpen_AlwaysDisabled()
        {
            bool result = _policy.CanShowAd(AdFormat.AppOpen, AdPlacement.LevelComplete);
            Assert.IsFalse(result, "App open ads should be disabled by default.");
        }

        [Test]
        public void Reset_RestoresAllDefaults()
        {
            _policy.SetLevelsCompleted(10);
            _policy.SetMarbleMoving(true);
            _policy.SetAdShowing(true);

            _policy.Reset();

            // After reset, marble moving and ad showing should be false
            // But levels completed also reset, so grace period applies
            bool result = _policy.CanShowAd(AdFormat.Interstitial, AdPlacement.LevelComplete);
            Assert.IsFalse(result, "After reset, grace period should apply again.");
        }
    }
}
