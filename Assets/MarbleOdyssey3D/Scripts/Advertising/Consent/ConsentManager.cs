// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// ConsentManager.cs — Google UMP consent flow integration
// ============================================================================

using System;
using UnityEngine;

#if UNITY_ANDROID
using GoogleMobileAds.Ump.Api;
#endif

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Manages the Google User Messaging Platform (UMP) consent flow.
    /// Handles GDPR, CCPA, and other regional consent requirements.
    /// Ad requests are gated until consent status permits them.
    /// </summary>
    public class ConsentManager : IConsentManager
    {
        private readonly AdConfiguration _config;
        private bool _canRequestAds;
        private bool _consentFormAvailable;

        /// <summary>
        /// Whether ads can be requested based on current consent status.
        /// </summary>
        public bool CanRequestAds => _canRequestAds;

        /// <summary>
        /// Whether a consent form is available to show.
        /// </summary>
        public bool IsConsentFormAvailable => _consentFormAvailable;

        /// <summary>
        /// Fired when the consent process completes. Parameter is true if ads can be requested.
        /// </summary>
        public event Action<bool> OnConsentComplete;

        public ConsentManager(AdConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Request consent information update from UMP.
        /// Must be called before any ad requests.
        /// </summary>
        public void RequestConsentInfoUpdate()
        {
#if UNITY_ANDROID
            Debug.Log("[Consent] Requesting consent info update...");

            var requestParameters = new ConsentRequestParameters();

            // For testing consent in development
            if (_config.testMode)
            {
                var debugSettings = new ConsentDebugSettings
                {
                    // Set to Geography.EEA to test GDPR flow
                    DebugGeography = DebugGeography.Disabled,
                };

                if (_config.testDeviceIds != null)
                {
                    foreach (var deviceId in _config.testDeviceIds)
                    {
                        debugSettings.TestDeviceHashedIds.Add(deviceId);
                    }
                }

                requestParameters.ConsentDebugSettings = debugSettings;
            }

            requestParameters.TagForUnderAgeOfConsent = _config.tagForUnderAgeOfConsent;

            ConsentInformation.Update(requestParameters, OnConsentInfoUpdated);
#else
            // Non-Android: assume consent is granted for editor testing
            Debug.Log("[Consent] Non-Android platform — assuming consent granted for editor.");
            _canRequestAds = true;
            OnConsentComplete?.Invoke(true);
#endif
        }

#if UNITY_ANDROID
        private void OnConsentInfoUpdated(FormError error)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                if (error != null)
                {
                    Debug.LogError($"[Consent] Info update failed: {error.Message}");
                    // On failure, check if we can still request ads based on previous status
                    _canRequestAds = ConsentInformation.CanRequestAds();
                    OnConsentComplete?.Invoke(_canRequestAds);
                    return;
                }

                Debug.Log($"[Consent] Info updated. " +
                          $"Status={ConsentInformation.ConsentStatus}, " +
                          $"CanRequestAds={ConsentInformation.CanRequestAds()}, " +
                          $"FormAvailable={ConsentInformation.IsConsentFormAvailable()}");

                _canRequestAds = ConsentInformation.CanRequestAds();
                _consentFormAvailable = ConsentInformation.IsConsentFormAvailable();

                if (_consentFormAvailable)
                {
                    // Show the consent form if available and required
                    LoadAndShowConsentForm();
                }
                else
                {
                    // No form needed — proceed with current status
                    OnConsentComplete?.Invoke(_canRequestAds);
                }
            });
        }

        private void LoadAndShowConsentForm()
        {
            Debug.Log("[Consent] Loading consent form...");

            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    if (formError != null)
                    {
                        Debug.LogError($"[Consent] Form error: {formError.Message}");
                    }

                    // Re-check after form interaction
                    _canRequestAds = ConsentInformation.CanRequestAds();
                    Debug.Log($"[Consent] Post-form: CanRequestAds={_canRequestAds}");
                    OnConsentComplete?.Invoke(_canRequestAds);
                });
            });
        }
#endif

        /// <summary>
        /// Show a consent form on demand (for a settings/privacy button).
        /// </summary>
        public void ShowConsentForm(Action<string> onComplete)
        {
#if UNITY_ANDROID
            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _canRequestAds = ConsentInformation.CanRequestAds();
                    onComplete?.Invoke(formError?.Message);
                });
            });
#else
            onComplete?.Invoke(null);
#endif
        }

        /// <summary>
        /// Show the privacy options form (required by some regulations for re-consent).
        /// </summary>
        public void ShowPrivacyOptionsForm(Action<string> onComplete)
        {
#if UNITY_ANDROID
            ConsentForm.ShowPrivacyOptionsForm(formError =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    _canRequestAds = ConsentInformation.CanRequestAds();
                    onComplete?.Invoke(formError?.Message);
                });
            });
#else
            onComplete?.Invoke(null);
#endif
        }

        /// <summary>
        /// Reset consent state for testing purposes. Do not call in production.
        /// </summary>
        public void ResetConsentForTesting()
        {
#if UNITY_ANDROID
            if (_config.testMode)
            {
                ConsentInformation.Reset();
                Debug.Log("[Consent] Consent state reset (test mode).");
            }
            else
            {
                Debug.LogWarning("[Consent] Cannot reset consent outside test mode.");
            }
#endif
        }
    }
}
