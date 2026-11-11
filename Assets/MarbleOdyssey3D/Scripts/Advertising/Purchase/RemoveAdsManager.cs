// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// RemoveAdsManager.cs — Remove Ads IAP entitlement integration
// ============================================================================

using System;
using UnityEngine;

#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Manages the "Remove Ads" in-app purchase entitlement via Google Play Billing.
    ///
    /// When active:
    /// - Disables interstitial ads
    /// - Disables banner ads
    /// - Disables app-open ads
    /// - Rewarded ads remain optionally available (player-initiated, clearly stated)
    ///
    /// Persists entitlement and supports purchase restoration.
    ///
    /// IMPORTANT: The product description must clearly state that "Remove Ads"
    /// removes forced advertising only. Optional rewarded ads may still be available
    /// if the player chooses to watch them for bonus rewards.
    /// </summary>
    public class RemoveAdsManager : IRemoveAdsEntitlement
#if UNITY_PURCHASING
        , IDetailedStoreListener
#endif
    {
        // Product ID — must match Google Play Console configuration
        public const string RemoveAdsProductId = "com.marbleodyssey3d.removeads";

        // Persistence key
        private const string EntitlementKey = "MarbleOdyssey_RemoveAdsPurchased";

        private bool _isRemoveAdsPurchased;

#if UNITY_PURCHASING
        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;
#endif

        /// <summary>Whether the Remove Ads entitlement is active.</summary>
        public bool IsRemoveAdsPurchased => _isRemoveAdsPurchased;

        /// <summary>Fired when the entitlement changes.</summary>
        public event Action<bool> OnEntitlementChanged;

        public RemoveAdsManager()
        {
            // Load persisted entitlement
            _isRemoveAdsPurchased = PlayerPrefs.GetInt(EntitlementKey, 0) == 1;

            if (_isRemoveAdsPurchased)
            {
                Debug.Log("[RemoveAds] Entitlement loaded: ACTIVE");
            }

            InitializePurchasing();
        }

        // =====================================================================
        // Initialization
        // =====================================================================

        private void InitializePurchasing()
        {
#if UNITY_PURCHASING
            if (_storeController != null)
            {
                Debug.Log("[RemoveAds] Already initialized.");
                return;
            }

            var builder = ConfigurationBuilder.Instance(
                StandardPurchasingModule.Instance());

            // Register the Remove Ads product as a non-consumable
            builder.AddProduct(RemoveAdsProductId, ProductType.NonConsumable);

            UnityPurchasing.Initialize(this, builder);

            Debug.Log("[RemoveAds] Initializing Unity Purchasing...");
#else
            Debug.LogWarning("[RemoveAds] Unity Purchasing not available. " +
                             "Install the Unity IAP package to enable Remove Ads purchase. " +
                             "Entitlement will use persisted state only.");
#endif
        }

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Initiate the Remove Ads purchase flow.
        /// </summary>
        public void PurchaseRemoveAds()
        {
#if UNITY_PURCHASING
            if (_storeController == null)
            {
                Debug.LogError("[RemoveAds] Store not initialized.");
                return;
            }

            if (_isRemoveAdsPurchased)
            {
                Debug.Log("[RemoveAds] Already purchased.");
                return;
            }

            var product = _storeController.products.WithID(RemoveAdsProductId);
            if (product != null && product.availableToPurchase)
            {
                Debug.Log("[RemoveAds] Initiating purchase...");
                _storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogError("[RemoveAds] Product not available for purchase.");
            }
#else
            Debug.LogWarning("[RemoveAds] Unity Purchasing not available.");
#endif
        }

        /// <summary>
        /// Restore previous purchases (for reinstalls).
        /// </summary>
        public void RestorePurchases()
        {
#if UNITY_PURCHASING
            if (_extensionProvider == null)
            {
                Debug.LogError("[RemoveAds] Extensions not available.");
                return;
            }

            Debug.Log("[RemoveAds] Restoring purchases...");

            var googleExtension = _extensionProvider.GetExtension<IGooglePlayStoreExtensions>();
            googleExtension?.RestoreTransactions(success =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log($"[RemoveAds] Restore result: {(success ? "Success" : "Failed")}");
                });
            });
#else
            Debug.LogWarning("[RemoveAds] Unity Purchasing not available.");
#endif
        }

        // =====================================================================
        // Unity Purchasing Callbacks
        // =====================================================================

#if UNITY_PURCHASING
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensionProvider = extensions;

            Debug.Log("[RemoveAds] Unity Purchasing initialized.");

            // Check if user already owns the product (purchase restoration)
            var product = controller.products.WithID(RemoveAdsProductId);
            if (product != null && product.hasReceipt)
            {
                GrantEntitlement();
            }
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[RemoveAds] Purchasing init failed: {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[RemoveAds] Purchasing init failed: {error} — {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            if (args.purchasedProduct.definition.id == RemoveAdsProductId)
            {
                Debug.Log("[RemoveAds] Purchase successful!");
                GrantEntitlement();
                return PurchaseProcessingResult.Complete;
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogError($"[RemoveAds] Purchase failed: {failureReason}");
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.LogError($"[RemoveAds] Purchase failed: " +
                           $"{failureDescription.reason} — {failureDescription.message}");
        }
#endif

        // =====================================================================
        // Entitlement Management
        // =====================================================================

        private void GrantEntitlement()
        {
            if (_isRemoveAdsPurchased) return;

            _isRemoveAdsPurchased = true;
            PlayerPrefs.SetInt(EntitlementKey, 1);
            PlayerPrefs.Save();

            Debug.Log("[RemoveAds] Entitlement GRANTED.");

            // Notify the ad system
            MainThreadDispatcher.Enqueue(() =>
            {
                OnEntitlementChanged?.Invoke(true);

                // Immediately hide banners if showing
                var mgr = AdMobManager.Instance;
                if (mgr != null)
                {
                    mgr.BannerAds?.Hide();
                    mgr.BannerAds?.Destroy();
                }
            });
        }

        /// <summary>
        /// FOR TESTING ONLY: Reset the entitlement.
        /// </summary>
        public void ResetEntitlementForTesting()
        {
            _isRemoveAdsPurchased = false;
            PlayerPrefs.SetInt(EntitlementKey, 0);
            PlayerPrefs.Save();
            OnEntitlementChanged?.Invoke(false);
            Debug.Log("[RemoveAds] Entitlement RESET (testing).");
        }
    }
}
