// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// AdAudioManager.cs — Audio state preservation during ad display
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Manages game audio state during full-screen ad display.
    ///
    /// When a full-screen ad opens:
    /// - Pause gameplay music
    /// - Record current audio settings
    /// - Prevent overlapping audio
    ///
    /// When it closes:
    /// - Restore audio to the player's chosen settings
    /// - Avoid duplicate restoration
    /// </summary>
    public class AdAudioManager
    {
        private bool _wasMusicPlaying;
        private float _previousMusicVolume;
        private float _previousMasterVolume;
        private bool _isPaused;

        /// <summary>Whether audio is currently paused for an ad.</summary>
        public bool IsAudioPausedForAd => _isPaused;

        /// <summary>
        /// Pause game audio before showing a full-screen ad.
        /// Preserves the current audio state for restoration.
        /// </summary>
        public void PauseGameAudio()
        {
            if (_isPaused)
            {
                Debug.LogWarning("[AdAudio] Already paused — preventing double pause.");
                return;
            }

            _isPaused = true;

            // Record current state
            _previousMasterVolume = AudioListener.volume;
            _previousMusicVolume = AudioListener.volume; // Game-specific: adapt to your audio manager

            // Mute game audio so it doesn't overlap with ad audio
            AudioListener.volume = 0f;

            // Pause AudioListener
            AudioListener.pause = true;

            Debug.Log($"[AdAudio] Game audio paused. Previous volume: {_previousMasterVolume:F2}");
        }

        /// <summary>
        /// Resume game audio after a full-screen ad closes.
        /// Restores audio to the player's chosen settings.
        /// </summary>
        public void ResumeGameAudio()
        {
            if (!_isPaused)
            {
                Debug.LogWarning("[AdAudio] Not paused — preventing invalid resume.");
                return;
            }

            _isPaused = false;

            // Restore audio state
            AudioListener.pause = false;
            AudioListener.volume = _previousMasterVolume;

            Debug.Log($"[AdAudio] Game audio resumed. Volume restored to: {_previousMasterVolume:F2}");

            // NOTE: If the game uses a custom audio manager (e.g., for music vs SFX),
            // integrate with it here:
            //
            // GameAudioManager.Instance?.SetMusicVolume(_previousMusicVolume);
            // GameAudioManager.Instance?.ResumeMusicIfWasPlaying(_wasMusicPlaying);
        }

        /// <summary>
        /// Force-restore audio state (safety net for edge cases).
        /// Call this if the game detects audio is muted unexpectedly.
        /// </summary>
        public void ForceRestore()
        {
            if (_isPaused)
            {
                Debug.LogWarning("[AdAudio] Force-restoring audio state.");
                ResumeGameAudio();
            }
        }
    }
}
