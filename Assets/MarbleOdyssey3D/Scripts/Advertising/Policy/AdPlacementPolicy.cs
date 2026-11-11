// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdPlacementPolicy.cs — Smart ad placement decision engine
// ============================================================================

using System;
using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// The placement policy engine decides whether an ad CAN be shown at a given point.
    /// It considers game state, frequency limits, consent, tutorial status, and UX rules.
    ///
    /// Design principle: Never dynamically increase ad pressure because a player is struggling.
    /// Never use deceptive or manipulative placements.
    /// </summary>
    public class AdPlacementPolicy : IAdPlacementPolicy
    {
        private readonly AdConfiguration _config;
        private readonly AdFrequencyManager _frequencyManager;

        // Game state trackers — updated by gameplay systems
        private int _totalLevelsCompleted;
        private bool _isInTutorial;
        private bool _isInCinematic;
        private bool _isInPurchaseFlow;
        private bool _isMarbleMoving;
        private bool _isInJump;
        private bool _isAnotherAdShowing;
        private float _sessionStartTime;

        public AdPlacementPolicy(AdConfiguration config, AdFrequencyManager frequencyManager)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _frequencyManager = frequencyManager ?? throw new ArgumentNullException(nameof(frequencyManager));
            _sessionStartTime = Time.realtimeSinceStartup;
        }

        // =====================================================================
        // State Updates — Call these from gameplay systems
        // =====================================================================

        public void SetLevelsCompleted(int count) => _totalLevelsCompleted = count;
        public void SetTutorialActive(bool active) => _isInTutorial = active;
        public void SetCinematicActive(bool active) => _isInCinematic = active;
        public void SetPurchaseFlowActive(bool active) => _isInPurchaseFlow = active;
        public void SetMarbleMoving(bool moving) => _isMarbleMoving = moving;
        public void SetInJump(bool jumping) => _isInJump = jumping;
        public void SetAdShowing(bool showing) => _isAnotherAdShowing = showing;

        // =====================================================================
        // Core Decision
        // =====================================================================

        /// <summary>
        /// Determines whether an ad of the given format can be shown at the given placement.
        /// </summary>
        public bool CanShowAd(AdFormat format, AdPlacement placement)
        {
            // Universal blocks
            if (_isAnotherAdShowing)
            {
                Debug.Log("[Policy] Blocked: another ad is currently showing.");
                return false;
            }

            if (_isInCinematic)
            {
                Debug.Log("[Policy] Blocked: cinematic sequence active.");
                return false;
            }

            if (_isInPurchaseFlow)
            {
                Debug.Log("[Policy] Blocked: purchase transaction in progress.");
                return false;
            }

            if (_isInTutorial)
            {
                Debug.Log("[Policy] Blocked: player is in tutorial.");
                return false;
            }

            // Format-specific checks
            return format switch
            {
                AdFormat.Interstitial => CanShowInterstitial(placement),
                AdFormat.Rewarded => CanShowRewarded(placement),
                AdFormat.Banner => CanShowBanner(placement),
                AdFormat.AppOpen => false, // Not enabled by default
                _ => false
            };
        }

        // =====================================================================
        // Interstitial Rules
        // =====================================================================

        private bool CanShowInterstitial(AdPlacement placement)
        {
            // Never during active gameplay
            if (_isMarbleMoving)
            {
                Debug.Log("[Policy] Interstitial blocked: marble is moving.");
                return false;
            }

            if (_isInJump)
            {
                Debug.Log("[Policy] Interstitial blocked: marble in jump.");
                return false;
            }

            // Grace period: first N levels have no interstitials
            if (_totalLevelsCompleted < _config.interstitialGraceLevels)
            {
                Debug.Log($"[Policy] Interstitial blocked: grace period " +
                          $"({_totalLevelsCompleted}/{_config.interstitialGraceLevels} levels).");
                return false;
            }

            // Minimum session age
            float sessionAge = Time.realtimeSinceStartup - _sessionStartTime;
            if (sessionAge < _config.interstitialMinSessionAge)
            {
                Debug.Log($"[Policy] Interstitial blocked: session too young " +
                          $"({sessionAge:F0}s < {_config.interstitialMinSessionAge}s).");
                return false;
            }

            // Only at valid placements
            if (!IsValidInterstitialPlacement(placement))
            {
                Debug.Log($"[Policy] Interstitial blocked: invalid placement {placement}.");
                return false;
            }

            // Frequency caps (delegated)
            if (!_frequencyManager.CanShowInterstitial())
            {
                Debug.Log("[Policy] Interstitial blocked: frequency cap.");
                return false;
            }

            return true;
        }

        private bool IsValidInterstitialPlacement(AdPlacement placement)
        {
            return placement switch
            {
                AdPlacement.LevelComplete => true,
                AdPlacement.BetweenLevels => true,
                AdPlacement.WorldSelectionReturn => true,
                _ => false
            };
        }

        // =====================================================================
        // Rewarded Rules
        // =====================================================================

        private bool CanShowRewarded(AdPlacement placement)
        {
            // Rewarded ads are always optional — the player initiates them.
            // No frequency cap needed, but block during unsafe states.
            if (_isMarbleMoving || _isInJump)
            {
                Debug.Log("[Policy] Rewarded blocked: marble in motion.");
                return false;
            }

            return IsValidRewardedPlacement(placement);
        }

        private bool IsValidRewardedPlacement(AdPlacement placement)
        {
            return placement switch
            {
                AdPlacement.RewardBonusGems => true,
                AdPlacement.RewardCosmeticCurrency => true,
                AdPlacement.RewardSecondChance => true,
                AdPlacement.RewardChallengeMultiplier => true,
                AdPlacement.RewardDailyChallenge => true,
                _ => false
            };
        }

        // =====================================================================
        // Banner Rules
        // =====================================================================

        private bool CanShowBanner(AdPlacement placement)
        {
            // Only on non-gameplay screens
            return placement switch
            {
                AdPlacement.MenuBanner => true,
                AdPlacement.SettingsBanner => true,
                _ => false
            };
        }

        // =====================================================================
        // Record & Reset
        // =====================================================================

        public void RecordImpression(AdFormat format, AdPlacement placement)
        {
            if (format == AdFormat.Interstitial)
            {
                _frequencyManager.RecordInterstitialImpression();
            }

            Debug.Log($"[Policy] Recorded {format} impression at {placement}.");
        }

        public void Reset()
        {
            _totalLevelsCompleted = 0;
            _isInTutorial = false;
            _isInCinematic = false;
            _isInPurchaseFlow = false;
            _isMarbleMoving = false;
            _isInJump = false;
            _isAnotherAdShowing = false;
            _sessionStartTime = Time.realtimeSinceStartup;
            _frequencyManager.Reset();
        }
    }
}
