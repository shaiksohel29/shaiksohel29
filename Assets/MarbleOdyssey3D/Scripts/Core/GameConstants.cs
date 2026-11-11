// ============================================================================
// Marble Odyssey 3D — Core Game Systems
// GameConstants.cs — Centralised game-wide constants
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// Single source of truth for every magic number, layer name, tag,
    /// scene name, and default value used across the project.
    /// </summary>
    public static class GameConstants
    {
        // =====================================================================
        // World / Level Counts
        // =====================================================================
        public const int TotalWorlds       = 6;
        public const int LevelsPerWorld    = 10;
        public const int TotalLevels       = TotalWorlds * LevelsPerWorld;
        public const int MaxStarsPerLevel  = 3;

        // =====================================================================
        // Physics
        // =====================================================================
        public static class Physics
        {
            public const float MarbleMass            = 1f;
            public const float MarbleRadius          = 0.5f;
            public const float MarbleDrag            = 0.3f;
            public const float MarbleAngularDrag     = 0.5f;
            public const float MoveForce             = 15f;
            public const float MaxSpeed              = 20f;
            public const float JumpForce             = 8f;
            public const float GroundCheckDistance    = 0.6f;
            public const float FallDeathY            = -30f;
            public const float SpeedBoostMultiplier  = 1.8f;
            public const float SpeedBoostDuration    = 3f;
            public const float BounceForce           = 6f;
            public const float GravityMultiplier     = 2f;
            public const float PhysicsMaterial_Bounce         = 0.4f;
            public const float PhysicsMaterial_Friction       = 0.6f;
            public const float PhysicsMaterial_DynamicFriction = 0.5f;
        }

        // =====================================================================
        // Camera
        // =====================================================================
        public static class Camera
        {
            public const float FollowDistance     = 8f;
            public const float FollowHeight       = 5f;
            public const float FollowSmoothSpeed  = 8f;
            public const float LookAheadDistance   = 3f;
            public const float RotationSmoothSpeed = 5f;
            public const float ShakeDecay         = 1.5f;
            public const float MaxShakeIntensity  = 0.5f;
            public const float FOV                = 60f;
            public const float NearClip           = 0.1f;
            public const float FarClip            = 500f;
        }

        // =====================================================================
        // Layers  (set these in Unity Tag Manager)
        // =====================================================================
        public static class Layers
        {
            public const string Marble         = "Marble";
            public const string Ground         = "Ground";
            public const string Obstacle       = "Obstacle";
            public const string Collectible    = "Collectible";
            public const string KillZone       = "KillZone";
            public const string Checkpoint     = "Checkpoint";
            public const string GoalZone       = "GoalZone";
            public const string UI             = "UI";
            public const string IgnoreRaycast  = "Ignore Raycast";

            // Computed masks (call Init once after project loads layers)
            public static int GroundMask      => LayerMask.GetMask(Ground);
            public static int ObstacleMask    => LayerMask.GetMask(Obstacle);
            public static int InteractMask    => LayerMask.GetMask(Ground, Obstacle, KillZone, GoalZone);
        }

        // =====================================================================
        // Tags
        // =====================================================================
        public static class Tags
        {
            public const string Player     = "Player";
            public const string Gem        = "Gem";
            public const string Star       = "Star";
            public const string Checkpoint = "Checkpoint";
            public const string Goal       = "Goal";
            public const string SpeedBoost = "SpeedBoost";
            public const string JumpPad    = "JumpPad";
            public const string KillZone   = "KillZone";
            public const string MovingPlatform = "MovingPlatform";
        }

        // =====================================================================
        // Scene Names
        // =====================================================================
        public static class Scenes
        {
            public const string Boot        = "Boot";
            public const string MainMenu    = "MainMenu";
            public const string UIOverlay   = "UIOverlay";

            /// <summary>Returns "World{w}_Level{l}" (1-based).</summary>
            public static string Level(int worldIndex, int levelIndex)
                => $"World{worldIndex + 1}_Level{levelIndex + 1}";
        }

        // =====================================================================
        // PlayerPrefs Keys
        // =====================================================================
        public static class Prefs
        {
            // Progress
            public const string UnlockedWorlds   = "MO_UnlockedWorlds";   // comma-separated ints
            public const string UnlockedLevels   = "MO_UnlockedLvls_{0}"; // per world
            public const string LevelStars       = "MO_Stars_{0}_{1}";    // world, level
            public const string BestTime         = "MO_BestTime_{0}_{1}";
            public const string TotalGems        = "MO_TotalGems";
            public const string TotalStars       = "MO_TotalStars";

            // Settings
            public const string MusicVolume      = "MO_MusicVol";
            public const string SFXVolume        = "MO_SFXVol";
            public const string Sensitivity      = "MO_Sens";
            public const string VibrationEnabled = "MO_Vibration";
            public const string QualityLevel     = "MO_Quality";
            public const string UseGyroscope     = "MO_Gyro";

            // Cosmetics
            public const string EquippedSkin     = "MO_Skin";
            public const string EquippedTrail    = "MO_Trail";
            public const string EquippedHat      = "MO_Hat";
            public const string OwnedCosmetics   = "MO_OwnedCos";

            // Daily challenge
            public const string DailyStreak      = "MO_DailyStreak";
            public const string LastDailyDate    = "MO_LastDaily";

            // Meta
            public const string SaveVersion      = "MO_SaveVer";
            public const string FirstLaunchDone  = "MO_FirstLaunch";
        }

        // =====================================================================
        // Default Settings
        // =====================================================================
        public static class Defaults
        {
            public const float MusicVolume   = 0.8f;
            public const float SFXVolume     = 1.0f;
            public const float Sensitivity   = 1.0f;
            public const bool  Vibration     = true;
            public const int   QualityLevel  = 2;       // Medium
            public const bool  UseGyroscope  = false;
            public const string DefaultSkin  = "classic";
            public const string DefaultTrail = "none";
            public const string DefaultHat   = "none";
        }

        // =====================================================================
        // Economy / Rewards
        // =====================================================================
        public static class Rewards
        {
            public const int GemsPerStar              = 10;
            public const int GemsForLevelComplete     = 25;
            public const int GemsFor3Stars            = 50;
            public const int DailyChallengeBase       = 30;
            public const int DailyStreakBonus         = 10;  // per streak day
            public const int MaxDailyStreakBonus      = 100;
            public const int WorldUnlockStarThreshold = 15;  // stars needed to unlock next world
        }

        // =====================================================================
        // Scoring / Stars
        // =====================================================================
        public static class Scoring
        {
            /// <summary>Time thresholds (seconds) for 3 / 2 / 1 star, indexed [0]=gold.</summary>
            public static readonly float[] DefaultStarTimes = { 30f, 60f, 120f };

            public const float PerfectTimeBonus = 1.5f; // multiplier
            public const int GemScoreValue      = 100;
        }

        // =====================================================================
        // World Themes  (colour, name)
        // =====================================================================
        public static class Worlds
        {
            public static readonly WorldTheme[] Themes =
            {
                new WorldTheme("Crystal Caverns",   new Color(0.3f, 0.7f, 1f)),
                new WorldTheme("Volcanic Forge",    new Color(1f, 0.35f, 0.1f)),
                new WorldTheme("Floating Gardens",  new Color(0.3f, 0.9f, 0.4f)),
                new WorldTheme("Frozen Peaks",      new Color(0.7f, 0.85f, 1f)),
                new WorldTheme("Ancient Ruins",     new Color(0.85f, 0.75f, 0.5f)),
                new WorldTheme("Neon Nexus",        new Color(0.9f, 0.2f, 0.9f)),
            };

            public struct WorldTheme
            {
                public string Name;
                public Color AccentColor;
                public WorldTheme(string name, Color color)
                {
                    Name = name;
                    AccentColor = color;
                }
            }
        }

        // =====================================================================
        // Animation Hashes (pre-computed for Animator.SetTrigger / SetBool)
        // =====================================================================
        public static class Anim
        {
            public static readonly int IsRolling   = Animator.StringToHash("IsRolling");
            public static readonly int Jump        = Animator.StringToHash("Jump");
            public static readonly int Land        = Animator.StringToHash("Land");
            public static readonly int Die         = Animator.StringToHash("Die");
            public static readonly int Respawn     = Animator.StringToHash("Respawn");
            public static readonly int Celebrate   = Animator.StringToHash("Celebrate");
            public static readonly int SpeedBoost  = Animator.StringToHash("SpeedBoost");
            public static readonly int UISlideIn   = Animator.StringToHash("SlideIn");
            public static readonly int UISlideOut  = Animator.StringToHash("SlideOut");
            public static readonly int UIPopIn     = Animator.StringToHash("PopIn");
        }

        // =====================================================================
        // Misc
        // =====================================================================
        public const int TargetFrameRate  = 60;
        public const int SaveVersion      = 1;
        public const string GameVersion   = "1.0.0";
    }
}
