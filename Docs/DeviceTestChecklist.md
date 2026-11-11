# Marble Odyssey 3D — Android Device Test Checklist

## Test Environment Setup

- [ ] Physical Android device or emulator available
- [ ] Device registered as test device in AdConfiguration
- [ ] Test mode enabled in AdConfiguration
- [ ] logcat accessible for debug output
- [ ] Google Mobile Ads SDK log tag filter: `[AdInit]`, `[RewardedAd]`, `[Interstitial]`, `[Consent]`, `[Network]`, `[Policy]`, `[FreqMgr]`, `[RewardTx]`, `[Analytics]`, `[Revenue]`

---

## Test Cases

### 1. Rewarded Ad — Successful Flow
- [ ] Reward button shows "Watch Ad" when ad is loaded
- [ ] Tapping button opens rewarded video
- [ ] Game audio pauses during ad
- [ ] Watching full ad triggers reward callback
- [ ] Reward is granted exactly once
- [ ] Reward confirmation displays
- [ ] Game audio resumes after ad closes
- [ ] New rewarded ad preloads automatically

### 2. Rewarded Ad — Load Failure
- [ ] Button shows "Unavailable" when ad fails to load
- [ ] Button is non-interactable
- [ ] Retry system attempts reload with backoff
- [ ] logcat shows retry attempts with increasing delays
- [ ] After max retries, system stops retrying

### 3. Rewarded Ad — Duplicate Callback Prevention
- [ ] If SDK fires reward callback twice, only one reward is granted
- [ ] logcat shows "Duplicate reward callback detected"
- [ ] Transaction manager reports correct count

### 4. Rewarded Ad — Dismissed Without Reward
- [ ] Closing ad early does NOT grant reward
- [ ] No reward confirmation shown
- [ ] Game state returns to normal
- [ ] Audio resumes correctly

### 5. Interstitial — Successful Show
- [ ] Interstitial appears at level complete (after grace period)
- [ ] Level completion animation finishes before ad
- [ ] Progress is saved before ad shows
- [ ] Ad shows full screen
- [ ] After dismissal, correct screen resumes
- [ ] No duplicate navigation
- [ ] New interstitial preloads

### 6. Interstitial — Load Failure
- [ ] If ad fails to load, level flow continues normally
- [ ] No blank screen or stuck state
- [ ] Retry system engages
- [ ] Gameplay is never blocked by ad failure

### 7. Interstitial — No Fill Response
- [ ] If AdMob returns no fill, gameplay continues
- [ ] No error dialog shown to player
- [ ] System retries loading later

### 8. Interstitial — Frequency Cap Tests
- [ ] First 3 levels: no interstitials appear
- [ ] After impression: next 3 levels skip interstitial
- [ ] Minimum 180 seconds between interstitials
- [ ] Maximum 4 interstitials per 30 minutes
- [ ] All caps work simultaneously

### 9. Network — Disconnection
- [ ] Turn off WiFi/data during gameplay
- [ ] Connectivity overlay appears after brief hysteresis
- [ ] Game physics pause
- [ ] Level timer pauses
- [ ] Progress is preserved
- [ ] Retry button works
- [ ] Loading spinner shows during check

### 10. Network — Reconnection
- [ ] Turn WiFi/data back on
- [ ] System detects reconnection automatically
- [ ] Overlay dismisses
- [ ] Gameplay resumes
- [ ] Ads begin reloading

### 11. Slow Network
- [ ] On throttled connection (3G simulation)
- [ ] Ads may load slowly but game doesn't hang
- [ ] Timeouts handled gracefully
- [ ] Connectivity check doesn't false-positive on slow networks

### 12. App Background During Ad
- [ ] Press home button during rewarded ad
- [ ] Return to app
- [ ] Ad state is consistent
- [ ] Game doesn't crash
- [ ] Audio state is correct

### 13. App Resume After Ad
- [ ] After any ad closes, app returns to correct screen
- [ ] Input focus is restored
- [ ] No stuck overlay
- [ ] Touch input responsive

### 14. Device Rotation
- [ ] Rotate during rewarded ad
- [ ] Ad continues normally
- [ ] Game layout correct after ad
- [ ] No crashes

### 15. Audio Restoration
- [ ] Music plays before ad
- [ ] Music pauses during ad
- [ ] Music resumes at correct volume after ad
- [ ] SFX settings preserved
- [ ] Muted state preserved if player had muted

### 16. Remove Ads Purchase
- [ ] Purchase button visible in settings/shop
- [ ] Purchase flow opens Google Play billing
- [ ] After purchase: interstitials stop
- [ ] After purchase: banners stop (if enabled)
- [ ] Rewarded ads still available (optional, player-initiated)
- [ ] Entitlement persists across app restart
- [ ] Restore purchases works after reinstall

### 17. Consent Flow
- [ ] First launch shows consent form (in applicable regions)
- [ ] Consent choices are respected
- [ ] Privacy options accessible from settings
- [ ] Ads only load after consent permits
- [ ] Declining personalized ads still allows non-personalized

### 18. Ad Frequency — Multiple Rapid Presses
- [ ] Rapidly tapping reward button doesn't show multiple ads
- [ ] Button cooldown prevents double-show
- [ ] No duplicate rewards from rapid interaction

### 19. Ad Object Cleanup
- [ ] After showing 10+ ads in a session, no memory leak
- [ ] Monitor memory in profiler
- [ ] Old ad objects properly destroyed
- [ ] No growing allocation pattern

### 20. Mediation Adapter Errors (If Mediation Configured)
- [ ] If a mediation adapter fails, fallback works
- [ ] Adapter status logged at initialization
- [ ] No crashes from missing adapters
- [ ] Response info shows which network served the ad

---

## Diagnostics Panel Verification
- [ ] Panel toggles on/off
- [ ] Shows correct rewarded/interstitial state
- [ ] Fill rates calculate correctly
- [ ] Revenue callbacks tracked
- [ ] Frequency state matches expectations
- [ ] Auto-refresh updates data
- [ ] Force Load buttons trigger ad loads
