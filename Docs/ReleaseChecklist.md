# Marble Odyssey 3D — Release Configuration Checklist

## Pre-Release Validation

### AdMob Account Configuration
- [ ] AdMob account created and verified at [admob.google.com](https://admob.google.com)
- [ ] App registered in AdMob console
- [ ] Ad units created for each format (Rewarded, Interstitial, Banner if enabled)
- [ ] Mediation groups configured (if using mediation partners)
- [ ] Payment information verified

### Ad Unit IDs
- [ ] Android App ID configured (matches AdMob console)
- [ ] Rewarded Ad Unit ID set (not placeholder)
- [ ] Interstitial Ad Unit ID set (not placeholder)
- [ ] Banner Ad Unit ID set (if banners enabled)
- [ ] All IDs verified against AdMob dashboard

### AdConfiguration Asset
- [ ] `testMode` set to **false**
- [ ] Test Device IDs list cleared (or only QA devices remain)
- [ ] Frequency caps reviewed and confirmed
- [ ] Reward values finalized
- [ ] Format toggles confirmed (which formats are active)

### SDK Versions
- [ ] Google Mobile Ads Unity Plugin is latest stable version
- [ ] Plugin compatible with Unity 6 LTS
- [ ] Any mediation adapter SDKs are compatible versions
- [ ] minSdkVersion meets AdMob requirements (21+)
- [ ] targetSdkVersion meets Google Play requirements

### Android Manifest
- [ ] App ID in manifest matches AdConfiguration
- [ ] `com.google.android.gms.ads.APPLICATION_ID` meta-data present
- [ ] Internet permission: `android.permission.INTERNET`
- [ ] Network state permission: `android.permission.ACCESS_NETWORK_STATE`

### Consent & Privacy
- [ ] UMP consent flow tested in EEA simulation
- [ ] UMP consent flow tested in non-EEA region
- [ ] Privacy options entry point accessible from settings
- [ ] COPPA/child-directed treatment flags set appropriately
- [ ] Privacy policy URL published and accessible
- [ ] Privacy policy covers ad data collection

### Google Play Compliance
- [ ] Data Safety form completed in Play Console
- [ ] Declared data types match actual SDK behavior
- [ ] Ad-related data collection disclosed
- [ ] Device identifiers usage disclosed
- [ ] Ads content rating declared
- [ ] Target audience declared correctly
- [ ] app-ads.txt published on developer website (if applicable)

### Build Configuration
- [ ] Production build does NOT use test ad IDs (build validator confirms)
- [ ] ProGuard/R8 rules include AdMob keep rules
- [ ] Build runs Production Build Validator without errors
- [ ] No test-device-only settings remain enabled
- [ ] Debug logging reduced/disabled for release

### Functional Testing on Device
- [ ] Ads load and display correctly on physical device
- [ ] Rewarded ads grant rewards after completion
- [ ] Rewarded ads do NOT grant rewards on dismissal
- [ ] Interstitials respect frequency caps
- [ ] Interstitials don't appear during gameplay
- [ ] Banners only on non-gameplay screens (if enabled)
- [ ] Remove Ads purchase works end-to-end
- [ ] Remove Ads properly disables forced ads
- [ ] Purchase restoration works
- [ ] Consent flow completes successfully
- [ ] Internet disconnection shows overlay
- [ ] Internet reconnection resumes gameplay
- [ ] Audio pauses during ads and restores after
- [ ] No duplicate rewards on rapid interactions
- [ ] App background/resume during ad doesn't crash
- [ ] Memory stable after multiple ad cycles

### Analytics Verification
- [ ] AdMob dashboard shows test impressions
- [ ] Revenue callbacks appear in diagnostics
- [ ] Fill rates reasonable for your region
- [ ] Error rates acceptable

## Post-Release Monitoring

- [ ] AdMob dashboard shows live impressions within 24 hours
- [ ] Revenue tracking active
- [ ] No policy violation warnings in AdMob console
- [ ] No policy violation warnings in Play Console
- [ ] App review approved
- [ ] Crash rate stable (no ad-related crashes)
- [ ] User reviews checked for ad-related complaints
