// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// NetworkConnectivityService.cs — Internet connectivity monitoring
// ============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Monitors actual internet reachability (not just local network).
    /// Uses a lightweight HTTP check with hysteresis to avoid false disconnections
    /// during temporary network switching.
    ///
    /// IMPORTANT: AdMob ad loading failure does NOT prove internet disconnection.
    /// If general internet is available but AdMob has no fill, gameplay must continue.
    /// </summary>
    public class NetworkConnectivityService : INetworkService
    {
        // =====================================================================
        // Configuration
        // =====================================================================

        /// <summary>URL to check for connectivity. Google's generate_204 is reliable.</summary>
        private const string ConnectivityCheckUrl = "https://connectivitycheck.gstatic.com/generate_204";

        /// <summary>Seconds between connectivity checks.</summary>
        private const float CheckIntervalSeconds = 10f;

        /// <summary>HTTP request timeout in seconds.</summary>
        private const int RequestTimeoutSeconds = 5;

        /// <summary>
        /// Number of consecutive failures required before declaring disconnected.
        /// This implements hysteresis to avoid false positives during network switching.
        /// </summary>
        private const int FailureThreshold = 2;

        /// <summary>
        /// Number of consecutive successes required before declaring reconnected
        /// after a confirmed disconnection.
        /// </summary>
        private const int ReconnectThreshold = 1;

        // =====================================================================
        // State
        // =====================================================================

        private bool _isConnected = true;
        private bool _isMonitoring;
        private int _consecutiveFailures;
        private int _consecutiveSuccesses;
        private Coroutine _monitorCoroutine;
        private MonoBehaviour _coroutineHost;

        /// <summary>Current connectivity status.</summary>
        public bool IsConnected => _isConnected;

        /// <summary>Fired when connectivity changes. Parameter is the new state.</summary>
        public event Action<bool> OnConnectivityChanged;

        // =====================================================================
        // Monitoring Control
        // =====================================================================

        /// <summary>
        /// Start periodic connectivity monitoring.
        /// Uses a coroutine hosted on a persistent MonoBehaviour.
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;

            // Create or find a host for our coroutine
            var hostGo = GameObject.Find("[NetworkMonitor]");
            if (hostGo == null)
            {
                hostGo = new GameObject("[NetworkMonitor]");
                UnityEngine.Object.DontDestroyOnLoad(hostGo);
            }

            _coroutineHost = hostGo.GetComponent<NetworkMonitorHost>();
            if (_coroutineHost == null)
            {
                _coroutineHost = hostGo.AddComponent<NetworkMonitorHost>();
            }

            _monitorCoroutine = _coroutineHost.StartCoroutine(MonitorLoop());
            Debug.Log("[Network] Connectivity monitoring started.");
        }

        /// <summary>Stop periodic connectivity monitoring.</summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            _isMonitoring = false;

            if (_monitorCoroutine != null && _coroutineHost != null)
            {
                _coroutineHost.StopCoroutine(_monitorCoroutine);
                _monitorCoroutine = null;
            }

            Debug.Log("[Network] Connectivity monitoring stopped.");
        }

        /// <summary>
        /// Perform a single connectivity check immediately.
        /// </summary>
        public void CheckNow(Action<bool> callback)
        {
            if (_coroutineHost != null)
            {
                _coroutineHost.StartCoroutine(SingleCheck(callback));
            }
            else
            {
                callback?.Invoke(_isConnected);
            }
        }

        // =====================================================================
        // Internals
        // =====================================================================

        private IEnumerator MonitorLoop()
        {
            // Initial check
            yield return PerformCheck();

            while (_isMonitoring)
            {
                yield return new WaitForSecondsRealtime(CheckIntervalSeconds);

                if (!_isMonitoring) yield break;

                yield return PerformCheck();
            }
        }

        private IEnumerator SingleCheck(Action<bool> callback)
        {
            yield return PerformCheck();
            callback?.Invoke(_isConnected);
        }

        private IEnumerator PerformCheck()
        {
            // Quick pre-check: if the device reports no network at all, skip the HTTP check
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                RecordFailure();
                yield break;
            }

            using (var request = UnityWebRequest.Head(ConnectivityCheckUrl))
            {
                request.timeout = RequestTimeoutSeconds;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success ||
                    request.responseCode == 204 ||
                    request.responseCode == 200)
                {
                    RecordSuccess();
                }
                else
                {
                    RecordFailure();
                }
            }
        }

        private void RecordSuccess()
        {
            _consecutiveFailures = 0;
            _consecutiveSuccesses++;

            if (!_isConnected && _consecutiveSuccesses >= ReconnectThreshold)
            {
                _isConnected = true;
                Debug.Log("[Network] Internet connectivity restored.");
                OnConnectivityChanged?.Invoke(true);
            }
            else if (!_isConnected)
            {
                // Still in hysteresis window for reconnection
            }
        }

        private void RecordFailure()
        {
            _consecutiveSuccesses = 0;
            _consecutiveFailures++;

            if (_isConnected && _consecutiveFailures >= FailureThreshold)
            {
                _isConnected = false;
                Debug.LogWarning("[Network] Internet connectivity lost.");
                OnConnectivityChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Helper MonoBehaviour to host coroutines for the network service.
        /// </summary>
        private class NetworkMonitorHost : MonoBehaviour { }
    }
}
