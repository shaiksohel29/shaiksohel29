// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// GameplayAdBridge.cs — Integration bridge between gameplay and ad systems
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Bridge component that connects gameplay events to the ad system.
    /// Attach this to your gameplay manager or a persistent game object.
    ///
    /// This component demonstrates how to integrate the ad system with
    /// your existing gameplay code. Adapt the method calls to match
    /// your actual game architecture.
    /// </summary>
    public class GameplayAdBridge : MonoBehaviour
    {
        // =====================================================================
        // Gameplay → Ad System Notifications
        // =====================================================================

        /// <summary>
        /// Call when a level is completed successfully.
        /// Notifies frequency manager and attempts interstitial if eligible.
        /// </summary>
        public void OnLevelCompleted(int levelIndex, int totalCompleted)
        {
            var mgr = AdMobManager.Instance;
            if (mgr == null) return;

            // Update frequency manager
            mgr.FrequencyManager?.RecordLevelCompletion();
            mgr.PlacementPolicy?.SetLevelsCompleted(totalCompleted);
            mgr.PlacementPolicy?.SetMarbleMoving(false);

            // Attempt an interstitial at level complete
            // The transition manager handles the full flow
            mgr.Transitions?.ShowInterstitialWithTransition(
                AdPlacement.LevelComplete,
                () =>
                {
                    Debug.Log("[Bridge] Post-level-complete transition finished.");
                    // Navigate to results screen, next level, etc.
                });
        }

        /// <summary>
        /// Call when the player enters the world selection screen.
        /// </summary>
        public void OnWorldSelectionEntered()
        {
            var mgr = AdMobManager.Instance;
            if (mgr == null) return;

            mgr.PlacementPolicy?.SetMarbleMoving(false);
            mgr.PlacementPolicy?.SetInJump(false);

            // Show banner if enabled and not ad-free
            if (mgr.Config.enableBannerAds && !mgr.IsAdFreeUser)
            {
                mgr.BannerAds?.Show();
            }
        }

        /// <summary>
        /// Call when gameplay starts (marble begins moving).
        /// </summary>
        public void OnGameplayStarted()
        {
            var mgr = AdMobManager.Instance;
            if (mgr == null) return;

            mgr.PlacementPolicy?.SetMarbleMoving(true);

            // Always hide banners during gameplay
            mgr.BannerAds?.Hide();
        }

        /// <summary>
        /// Call when the marble is in a jump.
        /// </summary>
        public void OnMarbleJump(bool isJumping)
        {
            AdMobManager.Instance?.PlacementPolicy?.SetInJump(isJumping);
        }

        /// <summary>
        /// Call when a cinematic sequence starts/ends.
        /// </summary>
        public void OnCinematic(bool isActive)
        {
            AdMobManager.Instance?.PlacementPolicy?.SetCinematicActive(isActive);
        }

        /// <summary>
        /// Call when a purchase flow starts/ends.
        /// </summary>
        public void OnPurchaseFlow(bool isActive)
        {
            AdMobManager.Instance?.PlacementPolicy?.SetPurchaseFlowActive(isActive);
        }

        /// <summary>
        /// Call when the tutorial state changes.
        /// </summary>
        public void OnTutorial(bool isActive)
        {
            AdMobManager.Instance?.PlacementPolicy?.SetTutorialActive(isActive);
        }

        // =====================================================================
        // Rewarded Ad Integration Examples
        // =====================================================================

        /// <summary>
        /// Check if a bonus gems reward ad is available.
        /// Use this to show/hide the reward button in UI.
        /// </summary>
        public bool IsBonusGemsRewardAvailable()
        {
            return AdMobManager.Instance?.IsRewardedAdAvailable() ?? false;
        }

        /// <summary>
        /// Show a rewarded ad for bonus gems after level completion.
        /// </summary>
        public void ShowBonusGemsAd(System.Action<int> onGemsReceived)
        {
            var mgr = AdMobManager.Instance;
            if (mgr == null) return;

            mgr.Transitions?.ShowRewardedWithTransition(
                AdPlacement.RewardBonusGems,
                (rewardType, rewardAmount) =>
                {
                    int gems = mgr.Config.rewardBonusGems;
                    Debug.Log($"[Bridge] Player earned {gems} bonus gems.");
                    onGemsReceived?.Invoke(gems);
                },
                () =>
                {
                    Debug.Log("[Bridge] Bonus gems ad flow complete.");
                });
        }

        /// <summary>
        /// Show a rewarded ad for a second chance after level failure.
        /// </summary>
        public void ShowSecondChanceAd(System.Action onSecondChanceGranted)
        {
            var mgr = AdMobManager.Instance;
            if (mgr == null) return;

            mgr.Transitions?.ShowRewardedWithTransition(
                AdPlacement.RewardSecondChance,
                (rewardType, rewardAmount) =>
                {
                    Debug.Log("[Bridge] Second chance granted via rewarded ad.");
                    onSecondChanceGranted?.Invoke();
                },
                () =>
                {
                    Debug.Log("[Bridge] Second chance ad flow complete.");
                });
        }

        /// <summary>
        /// Show a rewarded ad for challenge reward multiplier.
        /// </summary>
        public void ShowChallengeMultiplierAd(System.Action<int> onMultiplierGranted)
        {
            var mgr = AdMobManager.Instance;
            if (mgr == null) return;

            mgr.Transitions?.ShowRewardedWithTransition(
                AdPlacement.RewardChallengeMultiplier,
                (rewardType, rewardAmount) =>
                {
                    int multiplier = mgr.Config.rewardChallengeMultiplier;
                    Debug.Log($"[Bridge] Challenge multiplier x{multiplier} granted.");
                    onMultiplierGranted?.Invoke(multiplier);
                },
                () =>
                {
                    Debug.Log("[Bridge] Challenge multiplier ad flow complete.");
                });
        }

        // =====================================================================
        // Remove Ads Integration
        // =====================================================================

        /// <summary>
        /// Initiate the Remove Ads purchase.
        /// </summary>
        public void PurchaseRemoveAds()
        {
            var mgr = AdMobManager.Instance;
            if (mgr == null) return;

            mgr.PlacementPolicy?.SetPurchaseFlowActive(true);
            mgr.RemoveAds?.PurchaseRemoveAds();

            // Listen for completion
            void OnEntitlementChanged(bool purchased)
            {
                mgr.RemoveAds.OnEntitlementChanged -= OnEntitlementChanged;
                mgr.PlacementPolicy?.SetPurchaseFlowActive(false);

                if (purchased)
                {
                    Debug.Log("[Bridge] Remove Ads purchased successfully.");
                    // Update UI, hide ad-related buttons, show confirmation
                }
            }

            if (mgr.RemoveAds != null)
                mgr.RemoveAds.OnEntitlementChanged += OnEntitlementChanged;
        }

        /// <summary>
        /// Restore purchases (for reinstalls).
        /// </summary>
        public void RestorePurchases()
        {
            AdMobManager.Instance?.RemoveAds?.RestorePurchases();
        }
    }
}
