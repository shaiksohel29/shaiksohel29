// ============================================================================
// Marble Odyssey 3D — Settings Panel (auto-saves on change)
// ============================================================================

using UnityEngine;
using UnityEngine.UI;

namespace MarbleOdyssey
{
    public class SettingsController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Text _musicValueText;
        [SerializeField] private Text _sfxValueText;

        [Header("Controls")]
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private Text _sensitivityValueText;
        [SerializeField] private Toggle _vibrationToggle;
        [SerializeField] private Toggle _gyroToggle;

        [Header("Graphics")]
        [SerializeField] private Button _qualityLowButton;
        [SerializeField] private Button _qualityMedButton;
        [SerializeField] private Button _qualityHighButton;
        [SerializeField] private Image _lowHighlight;
        [SerializeField] private Image _medHighlight;
        [SerializeField] private Image _highHighlight;

        [Header("Privacy")]
        [SerializeField] private Button _privacyButton;

        [Header("Navigation")]
        [SerializeField] private Button _closeButton;

        [Header("Visual")]
        [SerializeField] private Color _selectedQualityColor = new Color(0.3f, 0.7f, 1f);
        [SerializeField] private Color _unselectedQualityColor = new Color(0.3f, 0.3f, 0.3f);

        // Keys
        private const string MusicVolKey = "Settings_MusicVolume";
        private const string SfxVolKey = "Settings_SFXVolume";
        private const string SensitivityKey = "Settings_Sensitivity";
        private const string VibrationKey = "Settings_Vibration";
        private const string GyroKey = "Settings_Gyro";
        private const string QualityKey = "Settings_Quality";

        // Current values
        public static float MusicVolume { get; private set; } = 0.8f;
        public static float SFXVolume { get; private set; } = 1f;
        public static float Sensitivity { get; private set; } = 1f;
        public static bool VibrationEnabled { get; private set; } = true;
        public static bool GyroEnabled { get; private set; } = false;
        public static int QualityLevel { get; private set; } = 1; // 0=Low, 1=Med, 2=High

        /// <summary>Load settings from PlayerPrefs (call at app startup).</summary>
        public static void LoadSettings()
        {
            MusicVolume = PlayerPrefs.GetFloat(MusicVolKey, 0.8f);
            SFXVolume = PlayerPrefs.GetFloat(SfxVolKey, 1f);
            Sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);
            VibrationEnabled = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            GyroEnabled = PlayerPrefs.GetInt(GyroKey, 0) == 1;
            QualityLevel = PlayerPrefs.GetInt(QualityKey, 1);

            QualitySettings.SetQualityLevel(QualityLevel, true);
        }

        private void OnEnable()
        {
            LoadSettings();
            PopulateUI();
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void PopulateUI()
        {
            if (_musicVolumeSlider != null) _musicVolumeSlider.value = MusicVolume;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = SFXVolume;
            if (_sensitivitySlider != null) _sensitivitySlider.value = Sensitivity;
            if (_vibrationToggle != null) _vibrationToggle.isOn = VibrationEnabled;
            if (_gyroToggle != null) _gyroToggle.isOn = GyroEnabled;

            UpdateVolumeLabels();
            UpdateQualityButtons();
        }

        private void BindEvents()
        {
            _musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
            _sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
            _sensitivitySlider?.onValueChanged.AddListener(OnSensitivityChanged);
            _vibrationToggle?.onValueChanged.AddListener(OnVibrationChanged);
            _gyroToggle?.onValueChanged.AddListener(OnGyroChanged);
            _qualityLowButton?.onClick.AddListener(() => SetQuality(0));
            _qualityMedButton?.onClick.AddListener(() => SetQuality(1));
            _qualityHighButton?.onClick.AddListener(() => SetQuality(2));
            _privacyButton?.onClick.AddListener(OnPrivacyClicked);
            _closeButton?.onClick.AddListener(OnClose);
        }

        private void UnbindEvents()
        {
            _musicVolumeSlider?.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            _sfxVolumeSlider?.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            _sensitivitySlider?.onValueChanged.RemoveListener(OnSensitivityChanged);
            _vibrationToggle?.onValueChanged.RemoveListener(OnVibrationChanged);
            _gyroToggle?.onValueChanged.RemoveListener(OnGyroChanged);
        }

        // =====================================================================
        // Handlers
        // =====================================================================

        private void OnMusicVolumeChanged(float value)
        {
            MusicVolume = value;
            PlayerPrefs.SetFloat(MusicVolKey, value);
            AudioManager.Instance?.SetMusicVolume(value);
            UpdateVolumeLabels();
        }

        private void OnSFXVolumeChanged(float value)
        {
            SFXVolume = value;
            PlayerPrefs.SetFloat(SfxVolKey, value);
            AudioManager.Instance?.SetSFXVolume(value);
            UpdateVolumeLabels();
        }

        private void OnSensitivityChanged(float value)
        {
            Sensitivity = value;
            PlayerPrefs.SetFloat(SensitivityKey, value);
            if (_sensitivityValueText != null)
                _sensitivityValueText.text = $"{value:F1}x";
        }

        private void OnVibrationChanged(bool enabled)
        {
            VibrationEnabled = enabled;
            PlayerPrefs.SetInt(VibrationKey, enabled ? 1 : 0);
            if (enabled) Handheld.Vibrate(); // Feedback
        }

        private void OnGyroChanged(bool enabled)
        {
            GyroEnabled = enabled;
            PlayerPrefs.SetInt(GyroKey, enabled ? 1 : 0);
            Input.gyro.enabled = enabled;
        }

        private void SetQuality(int level)
        {
            QualityLevel = level;
            PlayerPrefs.SetInt(QualityKey, level);
            QualitySettings.SetQualityLevel(level, true);
            UpdateQualityButtons();
            AudioManager.Instance?.PlaySFX("button_click");
        }

        private void OnPrivacyClicked()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            // Open consent form when ad system is ready
            // Advertising.AdMobManager.Instance?.Consent?.ShowPrivacyOptionsForm(err => { });
        }

        private void OnClose()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            PlayerPrefs.Save();
            gameObject.SetActive(false);
        }

        // =====================================================================
        // UI Updates
        // =====================================================================

        private void UpdateVolumeLabels()
        {
            if (_musicValueText != null)
                _musicValueText.text = $"{Mathf.RoundToInt(MusicVolume * 100)}%";
            if (_sfxValueText != null)
                _sfxValueText.text = $"{Mathf.RoundToInt(SFXVolume * 100)}%";
        }

        private void UpdateQualityButtons()
        {
            SetButtonHighlight(_lowHighlight, QualityLevel == 0);
            SetButtonHighlight(_medHighlight, QualityLevel == 1);
            SetButtonHighlight(_highHighlight, QualityLevel == 2);
        }

        private void SetButtonHighlight(Image img, bool selected)
        {
            if (img != null)
                img.color = selected ? _selectedQualityColor : _unselectedQualityColor;
        }
    }
}
