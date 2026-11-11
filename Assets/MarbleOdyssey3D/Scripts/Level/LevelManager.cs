// ============================================================================
// Marble Odyssey 3D — Level Manager
// Manages active level gameplay: timer, collectibles, stars, checkpoints,
// death, completion, pause, restart.
// ============================================================================

using System;
using System.Collections;
using UnityEngine;

namespace MarbleOdyssey
{
    public enum LevelState
    {
        Loading,
        Countdown,
        Playing,
        Paused,
        Completed,
        Failed
    }

    public class LevelManager : MonoBehaviour
    {
        // =====================================================================
        // Singleton
        // =====================================================================
        private static LevelManager _instance;
        public static LevelManager Instance => _instance;

        // =====================================================================
        // Configuration
        // =====================================================================
        [Header("Level")]
        [SerializeField] private LevelData _currentLevel;

        [Header("Countdown")]
        [SerializeField] private float _countdownDuration = 3f;

        [Header("Death")]
        [SerializeField] private float _respawnDelay = 1.2f;
        [SerializeField] private float _deathCameraShakeDuration = 0.3f;
        [SerializeField] private float _deathCameraShakeIntensity = 0.4f;

        // =====================================================================
        // State
        // =====================================================================
        private LevelState _state = LevelState.Loading;
        private float _elapsedTime;
        private int _gemsCollected;
        private int _deathCount;
        private int _starsEarned;
        private Vector3 _lastCheckpointPos;
        private Quaternion _lastCheckpointRot;
        private bool _hasCheckpoint;

        // =====================================================================
        // Public Read-Only State
        // =====================================================================
        public LevelData CurrentLevel => _currentLevel;
        public LevelState State => _state;
        public float ElapsedTime => _elapsedTime;
        public int GemsCollected => _gemsCollected;
        public int TotalGems => _currentLevel != null ? _currentLevel.totalGems : 0;
        public int DeathCount => _deathCount;
        public int StarsEarned => _starsEarned;
        public float RemainingTime
        {
            get
            {
                if (_currentLevel == null || !_currentLevel.hasTimeLimit) return -1f;
                return Mathf.Max(0f, _currentLevel.timeLimitSeconds - _elapsedTime);
            }
        }
        public bool IsPaused => _state == LevelState.Paused;

        // =====================================================================
        // Events
        // =====================================================================
        public event Action<int> OnCountdownTick;       // 3, 2, 1
        public event Action OnCountdownFinished;         // GO!
        public event Action<float> OnTimerUpdated;       // elapsed seconds
        public event Action<float> OnTimeLimitWarning;   // remaining seconds (< 10)
        public event Action<int, int> OnGemCollected;    // collected, total
        public event Action<int> OnCheckpointReached;    // checkpoint index
        public event Action<int> OnDeath;                // death count
        public event Action OnRespawnStart;
        public event Action OnRespawnComplete;
        public event Action<int, float, int> OnLevelCompleted;  // stars, time, gems
        public event Action<string> OnLevelFailed;       // reason
        public event Action<bool> OnPauseChanged;        // paused
        public event Action OnLevelRestarted;

        // =====================================================================
        // Lifecycle
        // =====================================================================
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (_state != LevelState.Playing) return;

            _elapsedTime += Time.deltaTime;
            OnTimerUpdated?.Invoke(_elapsedTime);

            // Time limit check
            if (_currentLevel != null && _currentLevel.hasTimeLimit)
            {
                float remaining = _currentLevel.timeLimitSeconds - _elapsedTime;
                if (remaining <= 10f && remaining > 0f)
                {
                    OnTimeLimitWarning?.Invoke(remaining);
                }
                if (remaining <= 0f)
                {
                    FailLevel("Time's up!");
                }
            }
        }

        // =====================================================================
        // Level Flow
        // =====================================================================

        /// <summary>
        /// Initialize and start a level.
        /// </summary>
        public void StartLevel(LevelData levelData)
        {
            _currentLevel = levelData;
            ResetLevelState();
            ApplyLevelEnvironment();
            StartCoroutine(CountdownSequence());
        }

        /// <summary>
        /// Restart the current level from scratch.
        /// </summary>
        public void RestartLevel()
        {
            if (_currentLevel == null) return;

            ResetLevelState();
            SetPaused(false);

            // Respawn marble at level start
            var marble = FindFirstObjectByType<MarbleController>();
            if (marble != null)
            {
                marble.Respawn(_currentLevel.spawnPosition,
                    Quaternion.Euler(_currentLevel.spawnRotation));
            }

            OnLevelRestarted?.Invoke();
            StartCoroutine(CountdownSequence());
        }

        private void ResetLevelState()
        {
            _state = LevelState.Loading;
            _elapsedTime = 0f;
            _gemsCollected = 0;
            _deathCount = 0;
            _starsEarned = 0;
            _hasCheckpoint = false;
            _lastCheckpointPos = _currentLevel.spawnPosition;
            _lastCheckpointRot = Quaternion.Euler(_currentLevel.spawnRotation);
        }

        private void ApplyLevelEnvironment()
        {
            if (_currentLevel == null) return;

            // Skybox
            if (_currentLevel.skyboxMaterial != null)
                RenderSettings.skybox = _currentLevel.skyboxMaterial;

            // World fog
            if (_currentLevel.world != null)
            {
                RenderSettings.fog = _currentLevel.world.enableFog;
                RenderSettings.fogColor = _currentLevel.world.fogColor;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = _currentLevel.world.fogStart;
                RenderSettings.fogEndDistance = _currentLevel.world.fogEnd;
                RenderSettings.ambientLight = _currentLevel.world.ambientLightColor;
            }

            // Music
            if (_currentLevel.musicTrack != null)
            {
                var audio = AudioManager.Instance;
                if (audio != null)
                    audio.PlayMusic(_currentLevel.musicTrack);
            }
        }

        // =====================================================================
        // Countdown
        // =====================================================================
        private IEnumerator CountdownSequence()
        {
            _state = LevelState.Countdown;

            // Freeze marble
            var marble = FindFirstObjectByType<MarbleController>();
            if (marble != null) marble.SetFrozen(true);

            for (int i = (int)_countdownDuration; i > 0; i--)
            {
                OnCountdownTick?.Invoke(i);
                yield return new WaitForSecondsRealtime(1f);
            }

            OnCountdownFinished?.Invoke();

            // Unfreeze marble
            if (marble != null) marble.SetFrozen(false);

            _state = LevelState.Playing;
        }

        // =====================================================================
        // Collectibles
        // =====================================================================

        /// <summary>
        /// Call when a gem is collected.
        /// </summary>
        public void CollectGem(int value = 1)
        {
            if (_state != LevelState.Playing) return;
            _gemsCollected += value;
            OnGemCollected?.Invoke(_gemsCollected, TotalGems);
        }

        // =====================================================================
        // Checkpoints
        // =====================================================================

        /// <summary>
        /// Register a checkpoint position.
        /// </summary>
        public void SetCheckpoint(Vector3 position, Quaternion rotation, int index = -1)
        {
            _lastCheckpointPos = position;
            _lastCheckpointRot = rotation;
            _hasCheckpoint = true;
            OnCheckpointReached?.Invoke(index);
            Debug.Log($"[Level] Checkpoint set at {position}");
        }

        /// <summary>
        /// Get the current respawn point.
        /// </summary>
        public (Vector3 position, Quaternion rotation) GetRespawnPoint()
        {
            if (_hasCheckpoint)
                return (_lastCheckpointPos, _lastCheckpointRot);

            if (_currentLevel != null)
                return (_currentLevel.spawnPosition,
                    Quaternion.Euler(_currentLevel.spawnRotation));

            return (Vector3.up * 2f, Quaternion.identity);
        }

        // =====================================================================
        // Death / Respawn
        // =====================================================================

        /// <summary>
        /// Trigger marble death. Handles camera shake, particles, respawn delay.
        /// </summary>
        public void TriggerDeath()
        {
            if (_state != LevelState.Playing) return;

            _deathCount++;
            OnDeath?.Invoke(_deathCount);
            StartCoroutine(RespawnSequence());
        }

        private IEnumerator RespawnSequence()
        {
            OnRespawnStart?.Invoke();

            // Camera shake
            var cam = CameraController.Instance;
            if (cam != null)
                cam.Shake(_deathCameraShakeDuration, _deathCameraShakeIntensity);

            // Freeze marble during respawn
            var marble = FindFirstObjectByType<MarbleController>();
            if (marble != null) marble.SetFrozen(true);

            yield return new WaitForSeconds(_respawnDelay);

            // Respawn
            var (pos, rot) = GetRespawnPoint();
            if (marble != null)
            {
                marble.Respawn(pos, rot);
                marble.SetFrozen(false);
            }

            OnRespawnComplete?.Invoke();
        }

        // =====================================================================
        // Completion
        // =====================================================================

        /// <summary>
        /// Complete the level. Calculates stars, saves progress, fires events.
        /// </summary>
        public void CompleteLevel()
        {
            if (_state != LevelState.Playing) return;

            _state = LevelState.Completed;

            // Freeze marble
            var marble = FindFirstObjectByType<MarbleController>();
            if (marble != null) marble.SetFrozen(true);

            // Calculate stars
            _starsEarned = _currentLevel != null
                ? _currentLevel.CalculateStars(_elapsedTime)
                : 1;

            Debug.Log($"[Level] Completed! Time: {_elapsedTime:F2}s, " +
                      $"Stars: {_starsEarned}, Gems: {_gemsCollected}/{TotalGems}, " +
                      $"Deaths: {_deathCount}");

            // Save progress
            SaveProgress();

            OnLevelCompleted?.Invoke(_starsEarned, _elapsedTime, _gemsCollected);
        }

        // =====================================================================
        // Failure
        // =====================================================================

        /// <summary>
        /// Fail the level with a reason.
        /// </summary>
        public void FailLevel(string reason)
        {
            if (_state != LevelState.Playing) return;

            _state = LevelState.Failed;

            var marble = FindFirstObjectByType<MarbleController>();
            if (marble != null) marble.SetFrozen(true);

            Debug.Log($"[Level] Failed: {reason}");
            OnLevelFailed?.Invoke(reason);
        }

        // =====================================================================
        // Pause
        // =====================================================================

        /// <summary>
        /// Toggle pause state.
        /// </summary>
        public void TogglePause()
        {
            SetPaused(_state != LevelState.Paused);
        }

        /// <summary>
        /// Set pause state explicitly.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (paused && _state == LevelState.Playing)
            {
                _state = LevelState.Paused;
                Time.timeScale = 0f;
                OnPauseChanged?.Invoke(true);
            }
            else if (!paused && _state == LevelState.Paused)
            {
                _state = LevelState.Playing;
                Time.timeScale = 1f;
                OnPauseChanged?.Invoke(false);
            }
        }

        // =====================================================================
        // Save
        // =====================================================================
        private void SaveProgress()
        {
            if (_currentLevel == null) return;

            string key = $"Level_{_currentLevel.worldName()}_{_currentLevel.levelIndex}";

            int prevStars = PlayerPrefs.GetInt(key + "_Stars", 0);
            float prevBest = PlayerPrefs.GetFloat(key + "_BestTime", float.MaxValue);
            int prevGems = PlayerPrefs.GetInt(key + "_Gems", 0);

            // Save best values
            if (_starsEarned > prevStars)
                PlayerPrefs.SetInt(key + "_Stars", _starsEarned);

            if (_elapsedTime < prevBest)
                PlayerPrefs.SetFloat(key + "_BestTime", _elapsedTime);

            if (_gemsCollected > prevGems)
                PlayerPrefs.SetInt(key + "_Gems", _gemsCollected);

            PlayerPrefs.SetInt(key + "_Completed", 1);
            PlayerPrefs.Save();
        }
    }

    // Helper extension so LevelData doesn't need a back-reference field
    public static class LevelDataExtensions
    {
        public static string worldName(this LevelData level)
        {
            return level.world != null ? level.world.worldName : "Unknown";
        }
    }
}
