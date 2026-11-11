// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// RewardedAdButton.cs — Premium rewarded ad UI button component
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace MarbleOdyssey.Advertising.UI
{
    /// <summary>
    /// Attach to a UI button to create a premium rewarded ad interaction point.
    /// Automatically enables/disables based on ad availability.
    /// Shows the reward amount and clearly identifies the ad.
    ///
    /// Design: Never disguise this as a gameplay button.
    /// Clearly identify it as "Watch Ad for Reward".
    /// </summary>
    public class RewardedAdButton : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AdPlacement _placement = AdPlacement.RewardBonusGems;

        [Header("UI References")]
        [SerializeField] private Button _button;
        [SerializeField] private Text _rewardText;
        [SerializeField] private Text _labelText;
        [SerializeField] private GameObject _adIcon;
        [SerializeField] private GameObject _unavailableOverlay;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Reward Display")]
        [SerializeField] private string _rewardFormat = "+{0} Gems";

        [Header("Cooldown")]
        [SerializeField] private float _buttonCooldownSeconds = 1f;

        private float _lastClickTime;
        private bool _isProcessing;

        private void Start()
        {
            if (_button != null)
                _button.onClick.AddListener(OnButtonClicked);

            UpdateAvailability();
        }

        private void OnEnable()
        {
            // Check availability whenever the button becomes visible
            InvokeRepeating(nameof(UpdateAvailability), 0.5f, 2f);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(UpdateAvailability));
        }

        /// <summary>
        /// Update button state based on ad availability.
        /// </summary>
        private void UpdateAvailability()
        {
            var mgr = AdMobManager.Instance;
            bool available = mgr != null && mgr.IsRewardedAdAvailable() && !_isProcessing;

            if (_button != null)
                _button.interactable = available;

            if (_unavailableOverlay != null)
                _unavailableOverlay.SetActive(!available);

            if (_canvasGroup != null)
                _canvasGroup.alpha = available ? 1f : 0.5f;

            // Update reward text
            if (_rewardText != null && mgr?.Config != null)
            {
                int rewardAmount = GetRewardAmount(mgr.Config);
                _rewardText.text = string.Format(_rewardFormat, rewardAmount);
            }

            if (_labelText != null)
            {
                _labelText.text = available ? "Watch Ad" : "Unavailable";
            }
        }

        private void OnButtonClicked()
        {
            // Prevent rapid clicks
            if (Time.realtimeSinceStartup - _lastClickTime < _buttonCooldownSeconds)
                return;

            _lastClickTime = Time.realtimeSinceStartup;

            var mgr = AdMobManager.Instance;
            if (mgr == null || !mgr.IsRewardedAdAvailable()) return;

            _isProcessing = true;
            UpdateAvailability();

            mgr.Transitions?.ShowRewardedWithTransition(
                _placement,
                (rewardType, rewardAmount) =>
                {
                    // Reward granted — integrate with your game's economy
                    Debug.Log($"[RewardButton] Player earned: {rewardAmount} {rewardType}");

                    // TODO: Grant the reward through your game's currency system
                    // Example: GameEconomy.Instance.AddGems((int)rewardAmount);
                },
                () =>
                {
                    // Ad completed (with or without reward)
                    _isProcessing = false;
                    UpdateAvailability();
                });
        }

        private int GetRewardAmount(AdConfiguration config)
        {
            return _placement switch
            {
                AdPlacement.RewardBonusGems => config.rewardBonusGems,
                AdPlacement.RewardCosmeticCurrency => config.rewardCosmeticCurrency,
                AdPlacement.RewardChallengeMultiplier => config.rewardChallengeMultiplier,
                AdPlacement.RewardDailyChallenge => config.rewardDailyChallengeBonus,
                _ => 0
            };
        }
    }
}
