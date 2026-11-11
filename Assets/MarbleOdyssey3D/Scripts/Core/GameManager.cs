// ============================================================================
// Marble Odyssey 3D — Core Game Systems
// GameManager.cs — Central game state machine and flow controller
// ============================================================================

using System;
using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// All possible game states. Transitions are guarded by the state machine.
    /// </summary>
    public enum GameState
    {
        Boot,
        MainMenu,
        WorldSelect,
        LevelSelect,
        Loading,
        Playing,
        Paused,
        LevelComplete,
        LevelFailed,
        DailyChallenge
    }

    /// <summary>
    /// Central singleton that owns the game-wide state machine, orchestrates
    /// scene transitions, and exposes the canonical current-world / current-level
    /// context every other system reads.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // =====================================================================
        // Singleton
        // =====================================================================
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        // =====================================================================
        // Inspector
        // =====================================================================
        [Header("Debug")]
        [SerializeField] private bool _logStateChanges = true;

        // =====================================================================
        // State
        // =====================================================================
        private GameState _currentState = GameState.Boot;
        public GameState CurrentState => _currentState;

        /// <summary>Currently selected world index (0-based).</summary>
        public int CurrentWorldIndex { get; private set; }

        /// <summary>Currently selected level index (0-based, within world).</summary>
        public int CurrentLevelIndex { get; private set; }

        /// <summary>True while a level is actively being played.</summary>
        public bool IsPlaying => _currentState == GameState.Playing;

        /// <summary>True while the game is paused (menu overlay).</summary>
        public bool IsPaused => _currentState == GameState.Paused;

        /// <summary>Elapsed play-time for the current level attempt (seconds).</summary>
        public float LevelElapsedTime { get; private set; }

        /// <summary>Gems collected in the current level attempt.</summary>
        public int LevelGemsCollected { get; private set; }

        /// <summary>Whether the current attempt has reached a checkpoint.</summary>
        public int LastCheckpointIndex { get; set; } = -1;

        // =====================================================================
        // Events
        // =====================================================================
        /// <summary>Fires just before the state changes. Args: (old, new).</summary>
        public event Action<GameState, GameState> OnStateChanging;

        /// <summary>Fires just after the state has changed. Args: (old, new).</summary>
        public event Action<GameState, GameState> OnStateChanged;

        /// <summary>Fires when a level attempt begins (after Loading → Playing).</summary>
        public event Action OnLevelStarted;

        /// <summary>Fires when a level is completed successfully.</summary>
        public event Action<LevelResultData> OnLevelCompleted;

        /// <summary>Fires when the player fails a level.</summary>
        public event Action OnLevelFailed;

        /// <summary>Fires every gameplay frame with the updated elapsed time.</summary>
        public event Action<float> OnTimerTick;

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
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Ensure core singletons exist
            SaveSystem.EnsureLoaded();
            TransitionToState(GameState.MainMenu);
        }

        private void Update()
        {
            if (_currentState == GameState.Playing)
            {
                LevelElapsedTime += Time.deltaTime;
                OnTimerTick?.Invoke(LevelElapsedTime);
                EventBus.Publish(new TimeUpdatedEvent { Elapsed = LevelElapsedTime });
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // =====================================================================
        // State Machine
        // =====================================================================

        /// <summary>
        /// Request a state transition. Invalid transitions are logged and ignored.
        /// </summary>
        public void TransitionToState(GameState newState)
        {
            if (newState == _currentState) return;

            if (!IsTransitionValid(_currentState, newState))
            {
                Debug.LogWarning($"[GameManager] Invalid transition {_currentState} → {newState}");
                return;
            }

            var old = _currentState;

            if (_logStateChanges)
                Debug.Log($"[GameManager] {old} → {newState}");

            OnStateChanging?.Invoke(old, newState);

            // Exit old state
            ExitState(old);

            _currentState = newState;

            // Enter new state
            EnterState(newState);

            OnStateChanged?.Invoke(old, newState);
            EventBus.Publish(new GameStateChangedEvent { OldState = old, NewState = newState });
        }

        private bool IsTransitionValid(GameState from, GameState to)
        {
            // Allow any transition from Boot (initial startup)
            if (from == GameState.Boot) return true;

            return (from, to) switch
            {
                // Menu navigation
                (GameState.MainMenu, GameState.WorldSelect) => true,
                (GameState.MainMenu, GameState.DailyChallenge) => true,
                (GameState.MainMenu, GameState.Loading) => true,
                (GameState.WorldSelect, GameState.MainMenu) => true,
                (GameState.WorldSelect, GameState.LevelSelect) => true,
                (GameState.LevelSelect, GameState.WorldSelect) => true,
                (GameState.LevelSelect, GameState.Loading) => true,

                // Gameplay flow
                (GameState.Loading, GameState.Playing) => true,
                (GameState.Playing, GameState.Paused) => true,
                (GameState.Playing, GameState.LevelComplete) => true,
                (GameState.Playing, GameState.LevelFailed) => true,
                (GameState.Paused, GameState.Playing) => true,
                (GameState.Paused, GameState.MainMenu) => true,
                (GameState.Paused, GameState.LevelSelect) => true,

                // Post-level flow
                (GameState.LevelComplete, GameState.LevelSelect) => true,
                (GameState.LevelComplete, GameState.Loading) => true, // next level
                (GameState.LevelComplete, GameState.MainMenu) => true,
                (GameState.LevelFailed, GameState.Loading) => true,   // retry
                (GameState.LevelFailed, GameState.LevelSelect) => true,
                (GameState.LevelFailed, GameState.MainMenu) => true,

                // Daily challenge
                (GameState.DailyChallenge, GameState.Loading) => true,
                (GameState.DailyChallenge, GameState.MainMenu) => true,
                (GameState.Loading, GameState.DailyChallenge) => true,

                _ => false
            };
        }

        private void EnterState(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.LevelComplete:
                    Time.timeScale = 1f; // allow celebration animations
                    break;
                case GameState.LevelFailed:
                    Time.timeScale = 1f;
                    break;
            }
        }

        private void ExitState(GameState state)
        {
            switch (state)
            {
                case GameState.Paused:
                    Time.timeScale = 1f;
                    break;
            }
        }

        // =====================================================================
        // High-Level Game Flow
        // =====================================================================

        /// <summary>Navigate to the main menu from anywhere.</summary>
        public void GoToMainMenu()
        {
            TransitionToState(GameState.MainMenu);
            SceneLoader.Instance?.LoadScene(GameConstants.Scenes.MainMenu);
        }

        /// <summary>Open the world selection screen.</summary>
        public void GoToWorldSelect()
        {
            TransitionToState(GameState.WorldSelect);
        }

        /// <summary>Open level selection for a specific world.</summary>
        public void GoToLevelSelect(int worldIndex)
        {
            CurrentWorldIndex = worldIndex;
            TransitionToState(GameState.LevelSelect);
        }

        /// <summary>Start loading a specific level.</summary>
        public void StartLevel(int worldIndex, int levelIndex)
        {
            CurrentWorldIndex = worldIndex;
            CurrentLevelIndex = levelIndex;
            LevelElapsedTime = 0f;
            LevelGemsCollected = 0;
            LastCheckpointIndex = -1;

            TransitionToState(GameState.Loading);

            string sceneName = $"World{worldIndex + 1}_Level{levelIndex + 1}";
            SceneLoader.Instance?.LoadScene(sceneName, () =>
            {
                TransitionToState(GameState.Playing);
                OnLevelStarted?.Invoke();
                EventBus.Publish(new LevelStartedEvent
                {
                    WorldIndex = worldIndex,
                    LevelIndex = levelIndex
                });
            });
        }

        /// <summary>Retry the current level.</summary>
        public void RetryLevel()
        {
            StartLevel(CurrentWorldIndex, CurrentLevelIndex);
        }

        /// <summary>Load the next level in the current world (or back to select).</summary>
        public void NextLevel()
        {
            int nextLevel = CurrentLevelIndex + 1;
            // If world has more levels, load next; otherwise back to select
            if (nextLevel < GameConstants.LevelsPerWorld)
            {
                StartLevel(CurrentWorldIndex, nextLevel);
            }
            else
            {
                GoToLevelSelect(CurrentWorldIndex);
            }
        }

        /// <summary>Called when the marble reaches the goal.</summary>
        public void CompleteLevelSuccess(int starsEarned)
        {
            if (_currentState != GameState.Playing) return;

            starsEarned = Mathf.Clamp(starsEarned, 1, 3);

            var result = new LevelResultData
            {
                WorldIndex = CurrentWorldIndex,
                LevelIndex = CurrentLevelIndex,
                Stars = starsEarned,
                ElapsedTime = LevelElapsedTime,
                GemsCollected = LevelGemsCollected
            };

            // Persist progress
            SaveSystem.Instance?.SetLevelStars(CurrentWorldIndex, CurrentLevelIndex, starsEarned);
            SaveSystem.Instance?.SetBestTime(CurrentWorldIndex, CurrentLevelIndex, LevelElapsedTime);
            SaveSystem.Instance?.AddGems(LevelGemsCollected);

            // Unlock next level
            int nextLevel = CurrentLevelIndex + 1;
            if (nextLevel < GameConstants.LevelsPerWorld)
            {
                SaveSystem.Instance?.UnlockLevel(CurrentWorldIndex, nextLevel);
            }
            else
            {
                // Unlock next world
                SaveSystem.Instance?.UnlockWorld(CurrentWorldIndex + 1);
            }

            SaveSystem.Instance?.Save();

            TransitionToState(GameState.LevelComplete);
            OnLevelCompleted?.Invoke(result);
            EventBus.Publish(new LevelCompletedEvent { Result = result });
        }

        /// <summary>Called when the marble falls off or time runs out.</summary>
        public void CompleteLevelFailed()
        {
            if (_currentState != GameState.Playing) return;

            TransitionToState(GameState.LevelFailed);
            OnLevelFailed?.Invoke();
            EventBus.Publish(new LevelFailedEvent
            {
                WorldIndex = CurrentWorldIndex,
                LevelIndex = CurrentLevelIndex
            });
        }

        /// <summary>Toggle pause state during gameplay.</summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
                TransitionToState(GameState.Paused);
            else if (_currentState == GameState.Paused)
                TransitionToState(GameState.Playing);
        }

        /// <summary>Add gems collected during gameplay.</summary>
        public void CollectGem(int amount = 1)
        {
            LevelGemsCollected += amount;
            EventBus.Publish(new GemCollectedEvent
            {
                Amount = amount,
                TotalInLevel = LevelGemsCollected
            });
        }

        // =====================================================================
        // Data
        // =====================================================================

        /// <summary>Result data passed when a level is completed.</summary>
        public struct LevelResultData
        {
            public int WorldIndex;
            public int LevelIndex;
            public int Stars;
            public float ElapsedTime;
            public int GemsCollected;
        }
    }
}
