# Marble Odyssey 3D — Google AdMob Setup Guide

## Prerequisites

1. **Unity 6 LTS** installed
2. **Google AdMob account** at [admob.google.com](https://admob.google.com)
3. **Google Play Console** access for your app
4. **Google Mobile Ads Unity Plugin** (latest version)

## Step 1: Install the Google Mobile Ads Unity Plugin

1. Download the latest `.unitypackage` from:
   [https://github.com/googleads/googleads-mobile-unity/releases](https://github.com/googleads/googleads-mobile-unity/releases)

2. In Unity: **Assets → Import Package → Custom Package** → select the downloaded file.

3. After import, go to **Assets → Google Mobile Ads → Settings** and enter your **Android App ID**.

4. The plugin automatically configures `AndroidManifest.xml` with your App ID.

## Step 2: Configure the AdConfiguration Asset

1. In Unity: **Assets → Create → Marble Odyssey → Ad Configuration**

2. Configure the asset:

   | Field | Development | Production |
   |-------|------------|------------|
   | Test Mode | ✅ Enabled | ❌ Disabled |
   | Android App ID | Placeholder OK | Your real App ID |
   | Rewarded Ad Unit ID | Auto (test ID) | Your real ID |
   | Interstitial Ad Unit ID | Auto (test ID) | Your real ID |

3. Assign the asset to the **AdMobManager** component on your persistent manager GameObject.

## Step 3: Set Up the Scene

1. Create an empty GameObject named `[AdMobManager]`.
2. Add the **AdMobManager** component.
3. Assign your **AdConfiguration** asset.
4. Mark it as **DontDestroyOnLoad** (the script handles this automatically).

Optional UI components:
- Add **ConnectivityOverlay** to your UI canvas.
- Add **DiagnosticsPanel** for development testing.
- Add **RewardedAdButton** components to reward UI buttons.

## Step 4: Integrate with Gameplay

Add a **GameplayAdBridge** component and call its methods from your gameplay code:

```csharp
// When a level is completed
bridge.OnLevelCompleted(levelIndex, totalCompleted);

// When gameplay starts
bridge.OnGameplayStarted();

// Check if reward is available
bool available = bridge.IsBonusGemsRewardAvailable();

// Show rewarded ad
bridge.ShowBonusGemsAd(gems => {
    playerGems += gems;
});
```

## Step 5: Test Device Setup

1. Run the app on a device/emulator.
2. Check **logcat** for the message:
   ```
   Use RequestConfiguration.Builder.setTestDeviceIds(Arrays.asList("YOUR_DEVICE_HASH"))
   ```
3. Copy the device hash and add it to **AdConfiguration → Test Device IDs**.

## Step 6: Consent Setup (GDPR/CCPA)

The system automatically handles consent using Google's UMP SDK:
- Consent is requested before any ad loads.
- Forms are shown when required by the user's region.
- Ads are only requested after consent permits them.

For testing consent flows:
1. In `ConsentManager.cs`, change `DebugGeography.Disabled` to `DebugGeography.EEA` for GDPR testing.
2. Add your test device hash to the debug settings.

## Step 7: Unity IAP for Remove Ads

1. Install **Unity IAP** via Package Manager.
2. Configure the `UNITY_PURCHASING` scripting define symbol.
3. In Google Play Console, create a non-consumable product: `com.marbleodyssey3d.removeads`.
4. The `RemoveAdsManager` handles purchase flow automatically.

## Ad Unit IDs Reference

| Format | Test ID (Auto-used in test mode) |
|--------|----------------------------------|
| Rewarded | `ca-app-pub-3940256099942544/5224354917` |
| Interstitial | `ca-app-pub-3940256099942544/1033173712` |
| Banner | `ca-app-pub-3940256099942544/6300978111` |
| App Open | `ca-app-pub-3940256099942544/9257395921` |

These are official Google test IDs and will never generate real revenue.

## Architecture Overview

```
AdMobManager (Central Orchestrator)
├── AdInitializationService (SDK init, consent gating)
├── ConsentManager (UMP consent flow)
├── NetworkConnectivityService (Internet monitoring)
├── AdPlacementPolicy (Smart placement decisions)
│   └── AdFrequencyManager (Interstitial frequency caps)
├── RewardedAdController (Rewarded video lifecycle)
├── InterstitialAdController (Interstitial lifecycle)
├── BannerAdController (Adaptive banner lifecycle)
├── AdFailureRecovery (Exponential backoff retry)
├── RewardTransactionManager (Idempotent reward grants)
├── AdAnalyticsManager (Local event tracking)
├── AdRevenueTracker (SDK revenue callback tracking)
├── AdLifecycleManager (App pause/resume handling)
├── AdTransitionManager (Premium ad transitions)
├── AdAudioManager (Audio state preservation)
├── MonetizationDiagnostics (Debug reporting)
└── RemoveAdsManager (IAP entitlement)
```

## Important Notes

- **Never hardcode live Ad Unit IDs in source files.** Always use the AdConfiguration asset.
- **Test mode must be ON during development.** The build validator warns if you ship with test mode.
- **Client-side reward validation** prevents duplicate rewards locally but cannot prevent modified-client fraud. For full fraud prevention, implement server-side reward verification.
- **AdMob ad load failure ≠ internet disconnection.** The connectivity system checks actual internet, not ad availability.
- **Revenue figures** from `AdRevenueTracker` are SDK-reported estimates. Consult the AdMob dashboard for actual revenue.
