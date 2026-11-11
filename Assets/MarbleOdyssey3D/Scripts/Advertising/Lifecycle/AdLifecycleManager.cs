// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdLifecycleManager.cs — App lifecycle and ad state coordination
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Handles application lifecycle events (pause, resume, background, foreground)
    /// in relation to ad display. Ensures ads don't break game state during
    /// Android focus loss and activity transitions.
    /// </summary>
    public class AdLifecycleManager
    {
        private readonly AdMobManager _manager;
        private bool _wasShowingAd;
        private bool _applicationPaused;

        public bool IsApplicationPaused => _applicationPaused;

        public AdLifecycleManager(AdMobManager manager)
        {
            _manager = manager;

            // Subscribe to Unity application events
            Application.focusChanged += OnApplicationFocus;
        }

        /// <summary>
        /// Call from MonoBehaviour.OnApplicationPause.
        /// </summary>
        public void OnApplicationPause(bool pauseStatus)
        {
            _applicationPaused = pauseStatus;

            if (pauseStatus)
            {
                HandleBackgrounded();
            }
            else
            {
                HandleForegrounded();
            }
        }

        /// <summary>
        /// Called when application focus changes (Android activity transitions).
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Lost focus — might be because an ad opened an overlay
                Debug.Log("[Lifecycle] Application lost focus.");
            }
            else
            {
                // Regained focus — might be returning from an ad
                Debug.Log("[Lifecycle] Application regained focus.");
            }
        }

        private void HandleBackgrounded()
        {
            Debug.Log("[Lifecycle] Application backgrounded.");

            // Track if an ad was showing when we went to background
            bool rewardedShowing = _manager.RewardedAds?.State == AdState.Showing;
            bool interstitialShowing = _manager.InterstitialAds?.State == AdState.Showing;
            _wasShowingAd = rewardedShowing || interstitialShowing;

            // Hide banner if visible (it'll be re-shown on resume if appropriate)
            if (_manager.BannerAds != null && _manager.BannerAds.IsVisible)
            {
                _manager.BannerAds.Hide();
            }
        }

        private void HandleForegrounded()
        {
            Debug.Log("[Lifecycle] Application foregrounded.");

            // If we were showing an ad when backgrounded, the SDK handles its lifecycle.
            // We just ensure our state is consistent.
            if (_wasShowingAd)
            {
                _wasShowingAd = false;
                Debug.Log("[Lifecycle] Returning from ad-related background.");
            }

            // Don't auto-show App Open ads — they're disabled by default
            // and would surprise the player during active gameplay
        }

        /// <summary>
        /// Cleanup — unsubscribe from events.
        /// </summary>
        public void Dispose()
        {
            Application.focusChanged -= OnApplicationFocus;
        }
    }
}
