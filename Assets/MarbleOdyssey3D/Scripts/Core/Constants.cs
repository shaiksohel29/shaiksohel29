using UnityEngine;

namespace MarbleOdyssey
{
    public static class Constants
    {
        // Worlds & Levels
        public const int MaxWorlds = 5;
        public const int LevelsPerWorld = 10;
        public const int TotalLevels = MaxWorlds * LevelsPerWorld;

        // Tags
        public const string TagIce = "Ice";
        public const string TagMud = "Mud";
        public const string TagBouncePad = "BouncePad";
        public const string TagSpeedBoost = "SpeedBoost";
        public const string TagDeathZone = "DeathZone";
        public const string TagCheckpoint = "Checkpoint";
        public const string TagCollectible = "Collectible";
        public const string TagFinishLine = "FinishLine";
        public const string TagGravityZone = "GravityZone";
        public const string TagTeleporter = "Teleporter";
        public const string TagMovingPlatform = "MovingPlatform";

        // Layers
        public const string LayerMarble = "Marble";
        public const string LayerGround = "Ground";
        public const string LayerObstacle = "Obstacle";
        public const string LayerUI = "UI";
        public const string LayerCollectible = "Collectible";

        // Scenes
        public const string SceneMainMenu = "MainMenu";
        public const string SceneWorldSelect = "WorldSelect";
        public const string SceneLoading = "Loading";
        public const string SceneGameplay = "Gameplay";
        public const string SceneHUD = "HUD";

        public static string GetLevelSceneName(int world, int level)
            => $"World{world}_Level{level}";

        // Star Rating Thresholds (percentage of par time)
        public const float Star3Threshold = 0.7f;
        public const float Star2Threshold = 0.9f;
        public const float Star1Threshold = 1.2f;

        // Gems
        public const int GemBaseValue = 10;
        public const int BonusGemValue = 50;

        // Cosmetics
        public const string DefaultSkinId = "marble_default";

        // Save Keys (PlayerPrefs)
        public static class SaveKeys
        {
            public const string CurrentWorld = "save_currentWorld";
            public const string CurrentLevel = "save_currentLevel";
            public const string TotalGems = "save_totalGems";
            public const string EquippedSkin = "save_equippedSkin";
            public const string DailyStreak = "save_dailyStreak";
            public const string LastDailyDate = "save_lastDailyDate";
            public const string MusicVolume = "settings_musicVol";
            public const string SfxVolume = "settings_sfxVol";
            public const string CameraSensitivity = "settings_camSens";
            public const string VibrationEnabled = "settings_vibration";
            public const string GyroEnabled = "settings_gyro";
            public const string QualityLevel = "settings_quality";

            public static string UnlockedLevel(int world, int level)
                => $"save_unlocked_w{world}_l{level}";
            public static string LevelStars(int world, int level)
                => $"save_stars_w{world}_l{level}";
            public static string LevelBestTime(int world, int level)
                => $"save_bestTime_w{world}_l{level}";
            public static string LevelGems(int world, int level)
                => $"save_gems_w{world}_l{level}";
            public static string SkinUnlocked(string skinId)
                => $"save_skin_{skinId}";
        }
    }
}
