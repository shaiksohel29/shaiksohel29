// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdAnalyticsManager.cs — Privacy-conscious local ad event analytics
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Tracks ad events locally for diagnostic and performance analysis.
    /// All analytics are privacy-conscious — no personal data is collected.
    ///
    /// NOTE: Local-only statistics do NOT provide global revenue dashboards.
    /// For cross-device analytics, integrate an approved analytics SDK
    /// after configuring its privacy requirements.
    /// </summary>
    public class AdAnalyticsManager : IAdAnalytics
    {
        // =====================================================================
        // Event Storage
        // =====================================================================

        private readonly List<AdEventRecord> _events = new List<AdEventRecord>();
        private readonly Dictionary<AdEventType, int> _eventCounts =
            new Dictionary<AdEventType, int>();

        // Per-format counters
        private readonly Dictionary<AdFormat, FormatStats> _formatStats =
            new Dictionary<AdFormat, FormatStats>();

        // Session metrics
        private readonly float _sessionStartTime;
        private int _totalEvents;

        // Cap to prevent memory growth
        private const int MaxStoredEvents = 2000;

        public AdAnalyticsManager()
        {
            _sessionStartTime = Time.realtimeSinceStartup;

            // Initialize format stats
            foreach (AdFormat format in Enum.GetValues(typeof(AdFormat)))
            {
                _formatStats[format] = new FormatStats();
            }
        }

        // =====================================================================
        // Event Tracking
        // =====================================================================

        /// <summary>
        /// Track an ad-related event.
        /// </summary>
        public void TrackEvent(AdEventType eventType, AdFormat format,
            AdPlacement placement, string detail = null)
        {
            _totalEvents++;

            // Increment counters
            if (!_eventCounts.ContainsKey(eventType))
                _eventCounts[eventType] = 0;
            _eventCounts[eventType]++;

            // Update format stats
            if (_formatStats.TryGetValue(format, out var stats))
            {
                UpdateFormatStats(ref stats, eventType);
                _formatStats[format] = stats;
            }

            // Store event record (capped)
            if (_events.Count < MaxStoredEvents)
            {
                _events.Add(new AdEventRecord
                {
                    Timestamp = Time.realtimeSinceStartup,
                    EventType = eventType,
                    Format = format,
                    Placement = placement,
                    Detail = detail
                });
            }

            Debug.Log($"[Analytics] {eventType} | {format} | {placement}" +
                      (detail != null ? $" | {detail}" : ""));
        }

        /// <summary>
        /// Track an ad revenue event from the SDK's paid callback.
        /// </summary>
        public void TrackRevenue(AdFormat format, AdPlacement placement,
            string currencyCode, long valueMicros)
        {
            TrackEvent(AdEventType.RevenueCallback, format, placement,
                $"{valueMicros} micros {currencyCode}");

            if (_formatStats.TryGetValue(format, out var stats))
            {
                stats.TotalRevenueMicros += valueMicros;
                stats.RevenueCurrency = currencyCode;
                _formatStats[format] = stats;
            }
        }

        // =====================================================================
        // Computed Metrics
        // =====================================================================

        /// <summary>
        /// Get fill rate for a format (loads / requests).
        /// Returns -1 if no requests have been made.
        /// </summary>
        public float GetFillRate(AdFormat format)
        {
            if (!_formatStats.TryGetValue(format, out var stats))
                return -1f;

            if (stats.Requests == 0) return -1f;
            return (float)stats.LoadSuccesses / stats.Requests;
        }

        /// <summary>
        /// Get rewarded ad completion rate (rewards earned / impressions).
        /// </summary>
        public float GetRewardedCompletionRate()
        {
            if (!_formatStats.TryGetValue(AdFormat.Rewarded, out var stats))
                return -1f;

            if (stats.Impressions == 0) return -1f;
            return (float)stats.RewardsEarned / stats.Impressions;
        }

        /// <summary>
        /// Get event count for a specific event type.
        /// </summary>
        public int GetEventCount(AdEventType eventType)
        {
            return _eventCounts.TryGetValue(eventType, out int count) ? count : 0;
        }

        /// <summary>
        /// Get per-format statistics.
        /// </summary>
        public FormatStats GetFormatStats(AdFormat format)
        {
            return _formatStats.TryGetValue(format, out var stats) ? stats : default;
        }

        /// <summary>
        /// Get a summary of all session analytics for diagnostics display.
        /// </summary>
        public AnalyticsSummary GetSummary()
        {
            return new AnalyticsSummary
            {
                SessionDuration = Time.realtimeSinceStartup - _sessionStartTime,
                TotalEvents = _totalEvents,
                RewardedStats = GetFormatStats(AdFormat.Rewarded),
                InterstitialStats = GetFormatStats(AdFormat.Interstitial),
                BannerStats = GetFormatStats(AdFormat.Banner),
                RewardedFillRate = GetFillRate(AdFormat.Rewarded),
                InterstitialFillRate = GetFillRate(AdFormat.Interstitial),
                RewardedCompletionRate = GetRewardedCompletionRate(),
                InterstitialSkippedByPolicy = GetEventCount(
                    AdEventType.InterstitialSkippedByPolicy)
            };
        }

        /// <summary>
        /// Get recent events for diagnostic display.
        /// </summary>
        public List<AdEventRecord> GetRecentEvents(int count = 50)
        {
            int start = Mathf.Max(0, _events.Count - count);
            return _events.GetRange(start, _events.Count - start);
        }

        // =====================================================================
        // Internals
        // =====================================================================

        private void UpdateFormatStats(ref FormatStats stats, AdEventType eventType)
        {
            switch (eventType)
            {
                case AdEventType.Request:
                    stats.Requests++;
                    break;
                case AdEventType.LoadSuccess:
                    stats.LoadSuccesses++;
                    break;
                case AdEventType.LoadFailure:
                    stats.LoadFailures++;
                    break;
                case AdEventType.Impression:
                    stats.Impressions++;
                    break;
                case AdEventType.Click:
                    stats.Clicks++;
                    break;
                case AdEventType.RewardEarned:
                    stats.RewardsEarned++;
                    break;
                case AdEventType.RewardGranted:
                    stats.RewardsGranted++;
                    break;
                case AdEventType.Dismissed:
                    stats.Dismissals++;
                    break;
            }
        }

        // =====================================================================
        // Data Types
        // =====================================================================

        public struct AdEventRecord
        {
            public float Timestamp;
            public AdEventType EventType;
            public AdFormat Format;
            public AdPlacement Placement;
            public string Detail;
        }

        public struct FormatStats
        {
            public int Requests;
            public int LoadSuccesses;
            public int LoadFailures;
            public int Impressions;
            public int Clicks;
            public int RewardsEarned;
            public int RewardsGranted;
            public int Dismissals;
            public long TotalRevenueMicros;
            public string RevenueCurrency;
        }

        public struct AnalyticsSummary
        {
            public float SessionDuration;
            public int TotalEvents;
            public FormatStats RewardedStats;
            public FormatStats InterstitialStats;
            public FormatStats BannerStats;
            public float RewardedFillRate;
            public float InterstitialFillRate;
            public float RewardedCompletionRate;
            public int InterstitialSkippedByPolicy;
        }
    }
}
