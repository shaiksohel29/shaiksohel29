// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdFailureRecovery.cs — Exponential backoff retry system for ad loads
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Manages retry logic with exponential backoff for failed ad loads.
    /// Prevents uncontrolled loading loops and respects maximum retry limits.
    /// Cancels all pending retries on application shutdown.
    /// </summary>
    public class AdFailureRecovery
    {
        private readonly AdConfiguration _config;
        private readonly Dictionary<AdFormat, int> _retryCounts = new Dictionary<AdFormat, int>();
        private readonly Dictionary<AdFormat, Coroutine> _pendingRetries =
            new Dictionary<AdFormat, Coroutine>();
        private MonoBehaviour _coroutineHost;
        private bool _cancelled;

        public AdFailureRecovery(AdConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            EnsureCoroutineHost();
        }

        /// <summary>
        /// Schedule a retry for the given ad format with exponential backoff.
        /// </summary>
        public void ScheduleRetry(AdFormat format, Action retryAction)
        {
            if (_cancelled) return;

            if (!_retryCounts.TryGetValue(format, out int retryCount))
                retryCount = 0;

            if (retryCount >= _config.maxRetryAttempts)
            {
                Debug.LogWarning($"[AdRetry] Max retries ({_config.maxRetryAttempts}) " +
                                 $"reached for {format}. Giving up until manual reload.");
                return;
            }

            // Calculate backoff delay: initialDelay * 2^retryCount, capped
            float delay = _config.initialRetryDelaySeconds *
                          Mathf.Pow(2f, retryCount);
            delay = Mathf.Min(delay, _config.maxRetryDelaySeconds);

            _retryCounts[format] = retryCount + 1;

            Debug.Log($"[AdRetry] Scheduling {format} retry #{retryCount + 1} " +
                      $"in {delay:F1}s.");

            // Cancel any existing pending retry for this format
            CancelRetry(format);

            if (_coroutineHost != null)
            {
                var coroutine = _coroutineHost.StartCoroutine(
                    RetryAfterDelay(format, delay, retryAction));
                _pendingRetries[format] = coroutine;
            }
        }

        /// <summary>
        /// Reset retry count for a format (call after a successful load).
        /// </summary>
        public void ResetRetryCount(AdFormat format)
        {
            _retryCounts[format] = 0;
            CancelRetry(format);
        }

        /// <summary>
        /// Cancel all pending retries. Call on application shutdown.
        /// </summary>
        public void CancelAll()
        {
            _cancelled = true;

            foreach (var kvp in _pendingRetries)
            {
                if (kvp.Value != null && _coroutineHost != null)
                {
                    _coroutineHost.StopCoroutine(kvp.Value);
                }
            }
            _pendingRetries.Clear();
            _retryCounts.Clear();

            Debug.Log("[AdRetry] All pending retries cancelled.");
        }

        /// <summary>
        /// Get current retry count for diagnostics.
        /// </summary>
        public int GetRetryCount(AdFormat format)
        {
            return _retryCounts.TryGetValue(format, out int count) ? count : 0;
        }

        // =====================================================================
        // Internals
        // =====================================================================

        private void CancelRetry(AdFormat format)
        {
            if (_pendingRetries.TryGetValue(format, out var existing))
            {
                if (existing != null && _coroutineHost != null)
                {
                    _coroutineHost.StopCoroutine(existing);
                }
                _pendingRetries.Remove(format);
            }
        }

        private IEnumerator RetryAfterDelay(AdFormat format, float delay, Action retryAction)
        {
            yield return new WaitForSecondsRealtime(delay);

            if (_cancelled) yield break;

            _pendingRetries.Remove(format);

            Debug.Log($"[AdRetry] Executing retry for {format}.");

            try
            {
                retryAction?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AdRetry] Retry action for {format} threw: {ex.Message}");
            }
        }

        private void EnsureCoroutineHost()
        {
            var hostGo = GameObject.Find("[AdRetryHost]");
            if (hostGo == null)
            {
                hostGo = new GameObject("[AdRetryHost]");
                UnityEngine.Object.DontDestroyOnLoad(hostGo);
            }

            _coroutineHost = hostGo.GetComponent<RetryHost>();
            if (_coroutineHost == null)
            {
                _coroutineHost = hostGo.AddComponent<RetryHost>();
            }
        }

        private class RetryHost : MonoBehaviour { }
    }
}
