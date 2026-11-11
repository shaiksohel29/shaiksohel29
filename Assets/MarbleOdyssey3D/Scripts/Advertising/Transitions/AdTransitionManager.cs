// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdTransitionManager.cs — Premium ad transition orchestration
// ============================================================================

using System;
using System.Collections;
using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Orchestrates premium, polished transitions around ad display.
    /// Ensures ads feel integrated into the game experience rather than intrusive.
    ///
    /// Before interstitials: finish animations, save progress, natural pause.
    /// After dismissal: restore audio, input, and screen flow.
    /// Never disguise advertisements as gameplay or system messages.
    /// </summary>
    public class AdTransitionManager
    {
        private readonly AdMobManager _manager;
        private MonoBehaviour _coroutineHost;
        private bool _isTransitioning;

        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Fired before an ad begins its show sequence.
        /// Gameplay systems should finalize their state.
        /// </summary>
        public event Action OnPreAdTransition;

        /// <summary>
        /// Fired after an ad is dismissed and the game should resume.
        /// </summary>
        public event Action OnPostAdTransition;

        public AdTransitionManager(AdMobManager manager)
        {
            _manager = manager;
            EnsureCoroutineHost();
        }

        /// <summary>
        /// Show an interstitial with proper transition orchestration.
        /// 1. Notify gameplay to finalize (save progress, finish animations)
        /// 2. Brief pause for natural feel
        /// 3. Show the ad
        /// 4. Restore game state after dismissal
        /// </summary>
        public void ShowInterstitialWithTransition(
            AdPlacement placement,
            Action onTransitionComplete)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[Transition] Already in transition — skipping.");
                onTransitionComplete?.Invoke();
                return;
            }

            if (_coroutineHost != null)
            {
                _coroutineHost.StartCoroutine(
                    InterstitialTransitionSequence(placement, onTransitionComplete));
            }
            else
            {
                // Fallback: show directly without transition
                _manager.TryShowInterstitial(placement, onTransitionComplete);
            }
        }

        /// <summary>
        /// Show a rewarded ad with premium presentation.
        /// 1. Show the reward selection UI (caller handles this)
        /// 2. Player taps the watch button
        /// 3. This method handles the ad show and reward flow
        /// </summary>
        public void ShowRewardedWithTransition(
            AdPlacement placement,
            Action<string, double> onRewardGranted,
            Action onComplete)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[Transition] Already in transition — skipping.");
                onComplete?.Invoke();
                return;
            }

            _isTransitioning = true;
            OnPreAdTransition?.Invoke();

            // Notify policy that an ad is showing
            _manager.PlacementPolicy?.SetAdShowing(true);

            _manager.TryShowRewarded(placement,
                (type, amount) =>
                {
                    // Reward granted callback
                    onRewardGranted?.Invoke(type, amount);
                },
                () =>
                {
                    // Ad closed callback
                    _isTransitioning = false;
                    _manager.PlacementPolicy?.SetAdShowing(false);
                    OnPostAdTransition?.Invoke();
                    onComplete?.Invoke();
                });
        }

        // =====================================================================
        // Transition Sequences
        // =====================================================================

        private IEnumerator InterstitialTransitionSequence(
            AdPlacement placement, Action onComplete)
        {
            _isTransitioning = true;

            // Step 1: Pre-transition notification (save progress, finish anims)
            OnPreAdTransition?.Invoke();
            _manager.PlacementPolicy?.SetAdShowing(true);

            // Step 2: Brief natural pause (let animations settle)
            yield return new WaitForSecondsRealtime(0.3f);

            // Step 3: Show the interstitial
            bool adShown = _manager.TryShowInterstitial(placement, () =>
            {
                // Step 4: Post-transition restoration
                _isTransitioning = false;
                _manager.PlacementPolicy?.SetAdShowing(false);
                OnPostAdTransition?.Invoke();
                onComplete?.Invoke();
            });

            // If ad wasn't shown (not ready, policy blocked, etc.)
            if (!adShown)
            {
                _isTransitioning = false;
                _manager.PlacementPolicy?.SetAdShowing(false);
                OnPostAdTransition?.Invoke();
                // onComplete already called by TryShowInterstitial
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private void EnsureCoroutineHost()
        {
            var hostGo = GameObject.Find("[AdTransitionHost]");
            if (hostGo == null)
            {
                hostGo = new GameObject("[AdTransitionHost]");
                UnityEngine.Object.DontDestroyOnLoad(hostGo);
            }

            _coroutineHost = hostGo.GetComponent<TransitionHost>();
            if (_coroutineHost == null)
            {
                _coroutineHost = hostGo.AddComponent<TransitionHost>();
            }
        }

        private class TransitionHost : MonoBehaviour { }
    }
}
