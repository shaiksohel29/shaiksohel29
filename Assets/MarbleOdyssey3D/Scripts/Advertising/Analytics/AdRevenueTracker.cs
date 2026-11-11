// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdRevenueTracker.cs — Impression-level revenue callback tracking
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Tracks ad revenue from SDK impression-level revenue callbacks.
    /// Revenue values come directly from AdMob's OnAdPaid callbacks.
    ///
    /// NOTE: These are SDK-reported values and represent estimated revenue.
    /// Do not fabricate CPM values or estimate revenue from impression counts.
    /// For actual revenue figures, consult the AdMob dashboard.
    /// </summary>
    public class AdRevenueTracker
    {
        private readonly AdAnalyticsManager _analytics;

        // Revenue tracking per format and placement
        private readonly Dictionary<string, RevenueEntry> _revenueEntries =
            new Dictionary<string, RevenueEntry>();

        // Session totals
        private long _sessionTotalMicros;
        private string _sessionCurrency = "USD";
        private int _totalRevenueEvents;

        public AdRevenueTracker(AdAnalyticsManager analytics)
        {
            _analytics = analytics;
        }

        /// <summary>
        /// Record a revenue event from an SDK paid callback.
        /// </summary>
        public void RecordRevenue(AdFormat format, AdPlacement placement,
            string currencyCode, long valueMicros)
        {
            _sessionTotalMicros += valueMicros;
            _sessionCurrency = currencyCode;
            _totalRevenueEvents++;

            string key = $"{format}_{placement}";
            if (!_revenueEntries.TryGetValue(key, out var entry))
            {
                entry = new RevenueEntry
                {
                    Format = format,
                    Placement = placement,
                    Currency = currencyCode
                };
            }

            entry.TotalMicros += valueMicros;
            entry.ImpressionCount++;
            _revenueEntries[key] = entry;

            Debug.Log($"[Revenue] Recorded {valueMicros} micros {currencyCode} " +
                      $"for {format}/{placement}. Session total: {_sessionTotalMicros} micros.");
        }

        /// <summary>
        /// Get total session revenue in micros (as reported by SDK).
        /// </summary>
        public long GetSessionRevenueMicros() => _sessionTotalMicros;

        /// <summary>
        /// Get session revenue as a decimal amount.
        /// </summary>
        public decimal GetSessionRevenueDecimal() => _sessionTotalMicros / 1_000_000m;

        /// <summary>
        /// Get the currency code from SDK revenue callbacks.
        /// </summary>
        public string GetCurrencyCode() => _sessionCurrency;

        /// <summary>
        /// Get the number of revenue-bearing impressions this session.
        /// </summary>
        public int GetRevenueEventCount() => _totalRevenueEvents;

        /// <summary>
        /// Get per-placement revenue breakdown.
        /// </summary>
        public IReadOnlyDictionary<string, RevenueEntry> GetRevenueBreakdown()
        {
            return _revenueEntries;
        }

        /// <summary>
        /// Get a diagnostic summary string.
        /// </summary>
        public string GetSummaryText()
        {
            decimal amount = GetSessionRevenueDecimal();
            return $"Session Revenue: {amount:F6} {_sessionCurrency} " +
                   $"({_totalRevenueEvents} revenue events)";
        }

        // =====================================================================
        // Data Types
        // =====================================================================

        public struct RevenueEntry
        {
            public AdFormat Format;
            public AdPlacement Placement;
            public string Currency;
            public long TotalMicros;
            public int ImpressionCount;
        }
    }
}
