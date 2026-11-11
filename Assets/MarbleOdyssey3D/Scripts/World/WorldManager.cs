// ============================================================================
// Marble Odyssey 3D — World Manager
// 6 themed worlds, 60 levels, star-based unlock progression.
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey
{
    public class WorldManager : MonoBehaviour
    {
        // =====================================================================
        // Singleton
        // =====================================================================
        private static WorldManager _instance;
        public static WorldManager Instance => _instance;

        // =====================================================================
        // Configuration
        // =====================================================================
        [Header("Worlds (6 worlds × 10 levels = 60 total)")]
        [SerializeField] private WorldData[] _worlds;

        public WorldData[] Worlds => _worlds;

        // Default star requirements per world if not set in data
        private static readonly int[] DefaultStarRequirements =
            { 0, 10, 30, 60, 100, 140 };

        // =====================================================================
        // Events
        // =====================================================================
        public event Action<WorldData> OnWorldSelected;
        public event Action<WorldData> OnWorldUnlocked;
        public event Action<LevelData> OnLevelSelected;

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

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // =====================================================================
        // World Queries
        // =====================================================================

        /// <summary>
        /// Check if a world is unlocked by cumulative star count.
        /// World 0 (Crystal Caverns) is always unlocked.
        /// </summary>
        public bool IsWorldUnlocked(int worldIndex)
        {
            if (worldIndex <= 0) return true;
            if (_worlds == null || worldIndex >= _worlds.Length) return false;

            int required = _worlds[worldIndex].requiredTotalStars;
            if (required <= 0 && worldIndex < DefaultStarRequirements.Length)
                required = DefaultStarRequirements[worldIndex];

            return GetTotalStars() >= required;
        }

        /// <summary>
        /// Check if a world is unlocked.
        /// </summary>
        public bool IsWorldUnlocked(WorldData world)
        {
            if (world == null || _worlds == null) return false;
            int idx = Array.IndexOf(_worlds, world);
            return IsWorldUnlocked(idx);
        }

        /// <summary>
        /// Get world data by index.
        /// </summary>
        public WorldData GetWorld(int index)
        {
            if (_worlds == null || index < 0 || index >= _worlds.Length) return null;
            return _worlds[index];
        }

        /// <summary>
        /// Get the number of unlocked worlds.
        /// </summary>
        public int GetUnlockedWorldCount()
        {
            int count = 0;
            if (_worlds == null) return 0;
            for (int i = 0; i < _worlds.Length; i++)
            {
                if (IsWorldUnlocked(i)) count++;
            }
            return count;
        }

        // =====================================================================
        // Level Queries
        // =====================================================================

        /// <summary>
        /// Check if a specific level is unlocked.
        /// Level 0 in each unlocked world is always available.
        /// Subsequent levels require the previous level to be completed.
        /// </summary>
        public bool IsLevelUnlocked(WorldData world, int levelIndex)
        {
            if (!IsWorldUnlocked(world)) return false;
            if (levelIndex <= 0) return true;
            if (world.levels == null || levelIndex >= world.levels.Length) return false;

            // Check if previous level was completed
            var prevLevel = world.levels[levelIndex - 1];
            string key = $"Level_{world.worldName}_{prevLevel.levelIndex}_Completed";
            bool prevCompleted = PlayerPrefs.GetInt(key, 0) == 1;

            // Also check star requirement if set
            var currentLevel = world.levels[levelIndex];
            if (currentLevel.requiredStarsFromPrevious > 0)
            {
                string starKey = $"Level_{world.worldName}_{prevLevel.levelIndex}_Stars";
                int prevStars = PlayerPrefs.GetInt(starKey, 0);
                return prevCompleted && prevStars >= currentLevel.requiredStarsFromPrevious;
            }

            return prevCompleted;
        }

        /// <summary>
        /// Get the best star count for a level.
        /// </summary>
        public int GetLevelStars(WorldData world, LevelData level)
        {
            string key = $"Level_{world.worldName}_{level.levelIndex}_Stars";
            return PlayerPrefs.GetInt(key, 0);
        }

        /// <summary>
        /// Get best time for a level.
        /// </summary>
        public float GetLevelBestTime(WorldData world, LevelData level)
        {
            string key = $"Level_{world.worldName}_{level.levelIndex}_BestTime";
            return PlayerPrefs.GetFloat(key, -1f);
        }

        /// <summary>
        /// Check if a level has been completed at all.
        /// </summary>
        public bool IsLevelCompleted(WorldData world, LevelData level)
        {
            string key = $"Level_{world.worldName}_{level.levelIndex}_Completed";
            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        // =====================================================================
        // Star Counting
        // =====================================================================

        /// <summary>
        /// Total stars earned across all worlds and levels.
        /// </summary>
        public int GetTotalStars()
        {
            int total = 0;
            if (_worlds == null) return 0;

            foreach (var world in _worlds)
            {
                total += GetWorldStars(world);
            }
            return total;
        }

        /// <summary>
        /// Stars earned in a specific world.
        /// </summary>
        public int GetWorldStars(WorldData world)
        {
            if (world == null || world.levels == null) return 0;
            int stars = 0;
            foreach (var level in world.levels)
            {
                if (level == null) continue;
                stars += GetLevelStars(world, level);
            }
            return stars;
        }

        /// <summary>
        /// Maximum possible stars across all worlds (3 per level × 60 = 180).
        /// </summary>
        public int GetMaxPossibleStars()
        {
            int max = 0;
            if (_worlds == null) return 0;
            foreach (var world in _worlds)
            {
                if (world != null && world.levels != null)
                    max += world.levels.Length * 3;
            }
            return max;
        }

        /// <summary>
        /// Percentage of all stars collected.
        /// </summary>
        public float GetCompletionPercentage()
        {
            int max = GetMaxPossibleStars();
            if (max == 0) return 0f;
            return (float)GetTotalStars() / max;
        }

        // =====================================================================
        // Selection
        // =====================================================================

        /// <summary>
        /// Select a world to browse its levels.
        /// </summary>
        public void SelectWorld(WorldData world)
        {
            if (world == null) return;
            if (!IsWorldUnlocked(world))
            {
                Debug.LogWarning($"[World] {world.worldName} is locked. " +
                    $"Need {world.requiredTotalStars} stars, have {GetTotalStars()}.");
                return;
            }
            OnWorldSelected?.Invoke(world);
        }

        /// <summary>
        /// Select a level to play.
        /// </summary>
        public void SelectLevel(WorldData world, int levelIndex)
        {
            if (!IsLevelUnlocked(world, levelIndex))
            {
                Debug.LogWarning($"[World] Level {levelIndex} in {world.worldName} is locked.");
                return;
            }

            var level = world.levels[levelIndex];
            OnLevelSelected?.Invoke(level);
        }

        // =====================================================================
        // World Info (static theme data for reference)
        // =====================================================================

        /// <summary>
        /// Returns default world definitions for editor tooling / initial creation.
        /// Actual data lives in WorldData ScriptableObjects.
        /// </summary>
        public static WorldThemeInfo[] GetDefaultWorldThemes()
        {
            return new[]
            {
                new WorldThemeInfo
                {
                    Name = "Crystal Caverns",
                    Description = "Gleaming underground caves filled with crystal formations.",
                    Primary = new Color(0.4f, 0.6f, 1f),
                    Secondary = new Color(0.2f, 0.3f, 0.6f),
                    Accent = new Color(0.7f, 0.9f, 1f),
                    RequiredStars = 0
                },
                new WorldThemeInfo
                {
                    Name = "Sky Gardens",
                    Description = "Floating islands high above the clouds with lush greenery.",
                    Primary = new Color(0.4f, 0.9f, 0.5f),
                    Secondary = new Color(0.2f, 0.6f, 0.8f),
                    Accent = new Color(1f, 0.95f, 0.6f),
                    RequiredStars = 10
                },
                new WorldThemeInfo
                {
                    Name = "Volcanic Depths",
                    Description = "Treacherous paths over rivers of molten lava.",
                    Primary = new Color(1f, 0.4f, 0.1f),
                    Secondary = new Color(0.3f, 0.1f, 0.05f),
                    Accent = new Color(1f, 0.8f, 0.2f),
                    RequiredStars = 30
                },
                new WorldThemeInfo
                {
                    Name = "Frozen Peaks",
                    Description = "Icy mountains where slippery surfaces test your control.",
                    Primary = new Color(0.8f, 0.9f, 1f),
                    Secondary = new Color(0.5f, 0.7f, 0.9f),
                    Accent = new Color(0.3f, 0.8f, 1f),
                    RequiredStars = 60
                },
                new WorldThemeInfo
                {
                    Name = "Neon City",
                    Description = "A cyberpunk metropolis with glowing neon tracks.",
                    Primary = new Color(1f, 0f, 0.8f),
                    Secondary = new Color(0.1f, 0f, 0.2f),
                    Accent = new Color(0f, 1f, 0.8f),
                    RequiredStars = 100
                },
                new WorldThemeInfo
                {
                    Name = "Cosmic Void",
                    Description = "The final frontier — marble through space and black holes.",
                    Primary = new Color(0.3f, 0f, 0.6f),
                    Secondary = new Color(0.05f, 0f, 0.1f),
                    Accent = new Color(1f, 0.6f, 0f),
                    RequiredStars = 140
                }
            };
        }

        [Serializable]
        public struct WorldThemeInfo
        {
            public string Name;
            public string Description;
            public Color Primary;
            public Color Secondary;
            public Color Accent;
            public int RequiredStars;
        }
    }
}
