// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdFrequencyManager.cs — Configurable interstitial frequency capping
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Tracks interstitial ad frequency across multiple dimensions:
    /// - Time since last impression
    /// - Level completions since last impression
    /// - Rolling 30-minute impression cap
    ///
    /// All thresholds are configurable via AdConfiguration.
    /// </summary>
    public class AdFrequencyManager
    {
        private readonly AdConfiguration _config;

        // Tracking state
        private float _lastInterstitialTime = float.NegativeInfinity;
        private int _levelsSinceLastInterstitial;
        private readonly List<float> _recentImpressionTimestamps = new List<float>();

        // Rolling window size in seconds (30 minutes)
        private const float RollingWindowSeconds = 1800f;

        public AdFrequencyManager(AdConfiguration config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Returns true if an interstitial can be shown right now,
        /// based on all frequency cap rules.
        /// </summary>
        public bool CanShowInterstitial()
        {
            float now = Time.realtimeSinceStartup;

            // Rule 1: Minimum time since last interstitial
            float timeSinceLast = now - _lastInterstitialTime;
            if (timeSinceLast < _config.interstitialMinIntervalSeconds)
            {
                Debug.Log($"[FreqMgr] Time cap: {timeSinceLast:F0}s < " +
                          $"{_config.interstitialMinIntervalSeconds}s minimum.");
                return false;
            }

            // Rule 2: Minimum levels completed since last interstitial
            if (_levelsSinceLastInterstitial < _config.interstitialLevelInterval)
            {
                Debug.Log($"[FreqMgr] Level cap: {_levelsSinceLastInterstitial} < " +
                          $"{_config.interstitialLevelInterval} levels.");
                return false;
            }

            // Rule 3: Rolling 30-minute impression cap
            PruneOldTimestamps(now);
            if (_recentImpressionTimestamps.Count >= _config.interstitialMaxPer30Min)
            {
                Debug.Log($"[FreqMgr] 30-min cap: {_recentImpressionTimestamps.Count} >= " +
                          $"{_config.interstitialMaxPer30Min} max.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Record that an interstitial was shown. Call after a confirmed impression.
        /// </summary>
        public void RecordInterstitialImpression()
        {
            float now = Time.realtimeSinceStartup;
            _lastInterstitialTime = now;
            _levelsSinceLastInterstitial = 0;
            _recentImpressionTimestamps.Add(now);

            PruneOldTimestamps(now);

            Debug.Log($"[FreqMgr] Recorded interstitial impression. " +
                      $"Recent count: {_recentImpressionTimestamps.Count}");
        }

        /// <summary>
        /// Notify that a level was completed. Increments the level counter.
        /// </summary>
        public void RecordLevelCompletion()
        {
            _levelsSinceLastInterstitial++;
            Debug.Log($"[FreqMgr] Level completed. Since last interstitial: " +
                      $"{_levelsSinceLastInterstitial}");
        }

        /// <summary>
        /// Get current frequency state for diagnostics.
        /// </summary>
        public FrequencyState GetState()
        {
            PruneOldTimestamps(Time.realtimeSinceStartup);

            return new FrequencyState
            {
                TimeSinceLastInterstitial = Time.realtimeSinceStartup - _lastInterstitialTime,
                LevelsSinceLastInterstitial = _levelsSinceLastInterstitial,
                ImpressionsInWindow = _recentImpressionTimestamps.Count,
                MaxImpressionsInWindow = _config.interstitialMaxPer30Min,
                MinIntervalSeconds = _config.interstitialMinIntervalSeconds,
                MinLevelInterval = _config.interstitialLevelInterval
            };
        }

        public void Reset()
        {
            _lastInterstitialTime = float.NegativeInfinity;
            _levelsSinceLastInterstitial = 0;
            _recentImpressionTimestamps.Clear();
        }

        private void PruneOldTimestamps(float now)
        {
            _recentImpressionTimestamps.RemoveAll(t => (now - t) > RollingWindowSeconds);
        }

        // =====================================================================
        // Diagnostic Data
        // =====================================================================

        public struct FrequencyState
        {
            public float TimeSinceLastInterstitial;
            public int LevelsSinceLastInterstitial;
            public int ImpressionsInWindow;
            public int MaxImpressionsInWindow;
            public float MinIntervalSeconds;
            public int MinLevelInterval;
        }
    }
}
