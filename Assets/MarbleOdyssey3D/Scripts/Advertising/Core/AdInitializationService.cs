// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdInitializationService.cs — SDK initialization with consent gating
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANDROID
using GoogleMobileAds.Api;
#endif

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Handles Google Mobile Ads SDK initialization.
    /// Initialization is gated behind consent status and occurs only once.
    /// </summary>
    public class AdInitializationService
    {
        private bool _isInitialized;
        private bool _isInitializing;
        private readonly AdConfiguration _config;
        private readonly List<Action<bool>> _pendingCallbacks = new List<Action<bool>>();

        public bool IsInitialized => _isInitialized;

        public AdInitializationService(AdConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Initialize the Google Mobile Ads SDK.
        /// Call only after consent has been obtained or determined unnecessary.
        /// </summary>
        /// <param name="onComplete">Callback with true if initialization succeeded.</param>
        public void Initialize(Action<bool> onComplete = null)
        {
            if (_isInitialized)
            {
                Debug.Log("[AdInit] Already initialized.");
                onComplete?.Invoke(true);
                return;
            }

            if (_isInitializing)
            {
                Debug.Log("[AdInit] Initialization already in progress, queuing callback.");
                if (onComplete != null)
                    _pendingCallbacks.Add(onComplete);
                return;
            }

            _isInitializing = true;
            if (onComplete != null)
                _pendingCallbacks.Add(onComplete);

#if UNITY_ANDROID
            try
            {
                // Configure request settings before initialization
                ConfigureRequestSettings();

                Debug.Log("[AdInit] Initializing Google Mobile Ads SDK...");

                MobileAds.Initialize(initStatus =>
                {
                    // This callback may arrive on a background thread.
                    // Use MainThreadDispatcher to safely process on main thread.
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        ProcessInitResult(initStatus);
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AdInit] SDK initialization threw exception: {ex.Message}");
                _isInitializing = false;
                NotifyCallbacks(false);
            }
#else
            Debug.LogWarning("[AdInit] Google Mobile Ads is only supported on Android in this project.");
            _isInitializing = false;
            NotifyCallbacks(false);
#endif
        }

#if UNITY_ANDROID
        private void ConfigureRequestSettings()
        {
            // Register test devices
            if (_config.testMode && _config.testDeviceIds != null)
            {
                var testDevices = new List<string>(_config.testDeviceIds);
                // The emulator is always a test device
                testDevices.Add(AdRequest.TestDeviceSimulator);

                var requestConfig = new RequestConfiguration
                {
                    TestDeviceIds = testDevices,
                };

                if (_config.tagForChildDirectedTreatment)
                {
                    requestConfig.TagForChildDirectedTreatment = TagForChildDirectedTreatment.True;
                }

                if (_config.tagForUnderAgeOfConsent)
                {
                    requestConfig.TagForUnderAgeOfConsent = TagForUnderAgeOfConsent.True;
                }

                MobileAds.SetRequestConfiguration(requestConfig);
                Debug.Log($"[AdInit] Configured {testDevices.Count} test device(s).");
            }
        }

        private void ProcessInitResult(InitializationStatus initStatus)
        {
            if (initStatus == null)
            {
                Debug.LogError("[AdInit] Initialization returned null status.");
                _isInitializing = false;
                NotifyCallbacks(false);
                return;
            }

            // Log adapter statuses for diagnostics
            var adapterMap = initStatus.getAdapterStatusMap();
            foreach (var entry in adapterMap)
            {
                var status = entry.Value;
                Debug.Log($"[AdInit] Adapter '{entry.Key}': " +
                          $"State={status.InitializationState}, " +
                          $"Description='{status.Description}'");
            }

            _isInitialized = true;
            _isInitializing = false;
            Debug.Log("[AdInit] Google Mobile Ads SDK initialized successfully.");
            NotifyCallbacks(true);
        }
#endif

        private void NotifyCallbacks(bool success)
        {
            var callbacks = new List<Action<bool>>(_pendingCallbacks);
            _pendingCallbacks.Clear();

            foreach (var cb in callbacks)
            {
                try
                {
                    cb.Invoke(success);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AdInit] Callback threw exception: {ex.Message}");
                }
            }
        }
    }
}
