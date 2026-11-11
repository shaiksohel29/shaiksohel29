// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// MonetizationDiagnostics.cs — In-game diagnostic stats display
// ============================================================================

using System.Text;
using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Provides diagnostic information about the advertising system's state.
    /// Useful for development and QA testing. Can be wired to an in-game debug panel.
    ///
    /// NOTE: These are local diagnostic statistics only.
    /// They do not provide a global revenue dashboard.
    /// </summary>
    public class MonetizationDiagnostics
    {
        private readonly AdAnalyticsManager _analytics;
        private readonly AdRevenueTracker _revenueTracker;

        public MonetizationDiagnostics(
            AdAnalyticsManager analytics,
            AdRevenueTracker revenueTracker)
        {
            _analytics = analytics;
            _revenueTracker = revenueTracker;
        }

        /// <summary>
        /// Generate a complete diagnostic report string.
        /// </summary>
        public string GenerateReport()
        {
            var sb = new StringBuilder();
            var summary = _analytics.GetSummary();

            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("   MARBLE ODYSSEY 3D — AD DIAGNOSTICS");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();

            // Session Info
            sb.AppendLine("── SESSION ──");
            sb.AppendLine($"  Duration:     {FormatTime(summary.SessionDuration)}");
            sb.AppendLine($"  Total Events: {summary.TotalEvents}");
            sb.AppendLine();

            // AdMob Manager State
            var mgr = AdMobManager.Instance;
            if (mgr != null)
            {
                sb.AppendLine("── SYSTEM STATE ──");
                sb.AppendLine($"  Initialized:  {mgr.IsInitialized}");
                sb.AppendLine($"  Ad-Free User: {mgr.IsAdFreeUser}");
                sb.AppendLine($"  Test Mode:    {mgr.Config?.testMode ?? false}");
                sb.AppendLine($"  Connected:    {mgr.Network?.IsConnected ?? false}");
                sb.AppendLine($"  Can Req Ads:  {mgr.Consent?.CanRequestAds ?? false}");
                sb.AppendLine();
            }

            // Rewarded Stats
            sb.AppendLine("── REWARDED ADS ──");
            sb.AppendLine($"  State:        {mgr?.RewardedAds?.State}");
            sb.AppendLine($"  Requests:     {summary.RewardedStats.Requests}");
            sb.AppendLine($"  Fill Rate:    {FormatPercent(summary.RewardedFillRate)}");
            sb.AppendLine($"  Impressions:  {summary.RewardedStats.Impressions}");
            sb.AppendLine($"  Completion:   {FormatPercent(summary.RewardedCompletionRate)}");
            sb.AppendLine($"  Rewards:      {summary.RewardedStats.RewardsGranted}");
            sb.AppendLine($"  Failures:     {summary.RewardedStats.LoadFailures}");
            sb.AppendLine();

            // Interstitial Stats
            sb.AppendLine("── INTERSTITIAL ADS ──");
            sb.AppendLine($"  State:        {mgr?.InterstitialAds?.State}");
            sb.AppendLine($"  Requests:     {summary.InterstitialStats.Requests}");
            sb.AppendLine($"  Fill Rate:    {FormatPercent(summary.InterstitialFillRate)}");
            sb.AppendLine($"  Impressions:  {summary.InterstitialStats.Impressions}");
            sb.AppendLine($"  Skipped:      {summary.InterstitialSkippedByPolicy}");
            sb.AppendLine($"  Failures:     {summary.InterstitialStats.LoadFailures}");
            sb.AppendLine();

            // Banner Stats
            if (mgr?.Config?.enableBannerAds == true)
            {
                sb.AppendLine("── BANNER ADS ──");
                sb.AppendLine($"  State:        {mgr?.BannerAds?.State}");
                sb.AppendLine($"  Visible:      {mgr?.BannerAds?.IsVisible}");
                sb.AppendLine($"  Impressions:  {summary.BannerStats.Impressions}");
                sb.AppendLine();
            }

            // Revenue
            sb.AppendLine("── REVENUE (SDK Reported) ──");
            sb.AppendLine($"  {_revenueTracker.GetSummaryText()}");
            sb.AppendLine();

            // Frequency Manager
            if (mgr?.FrequencyManager != null)
            {
                var freq = mgr.FrequencyManager.GetState();
                sb.AppendLine("── FREQUENCY STATE ──");
                sb.AppendLine($"  Time Since Last:  {FormatTime(freq.TimeSinceLastInterstitial)}");
                sb.AppendLine($"  Levels Since:     {freq.LevelsSinceLastInterstitial}/{freq.MinLevelInterval}");
                sb.AppendLine($"  30-Min Window:    {freq.ImpressionsInWindow}/{freq.MaxImpressionsInWindow}");
                sb.AppendLine();
            }

            // Retry State
            if (mgr?.FailureRecovery != null)
            {
                sb.AppendLine("── RETRY STATE ──");
                sb.AppendLine($"  Rewarded Retries:     {mgr.FailureRecovery.GetRetryCount(AdFormat.Rewarded)}");
                sb.AppendLine($"  Interstitial Retries: {mgr.FailureRecovery.GetRetryCount(AdFormat.Interstitial)}");
                sb.AppendLine();
            }

            sb.AppendLine("═══════════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Log the diagnostic report to the console.
        /// </summary>
        public void LogReport()
        {
            Debug.Log(GenerateReport());
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private string FormatTime(float seconds)
        {
            if (float.IsInfinity(seconds) || float.IsNaN(seconds) || seconds < 0)
                return "N/A";

            if (seconds < 60) return $"{seconds:F0}s";
            if (seconds < 3600) return $"{seconds / 60:F1}m";
            return $"{seconds / 3600:F1}h";
        }

        private string FormatPercent(float value)
        {
            if (value < 0) return "N/A";
            return $"{value * 100:F1}%";
        }
    }
}
