// ============================================================================
// Marble Odyssey 3D — Level Data ScriptableObject
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Marble Odyssey/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        public string levelName = "Unnamed Level";
        [TextArea(2, 4)]
        public string description = "";
        public int levelIndex;
        public WorldData world;
        public Sprite previewImage;

        [Header("Difficulty")]
        [Range(1, 5)]
        public int difficulty = 1;

        [Header("Star Thresholds (seconds — lower is better)")]
        [Tooltip("Complete within this time for 3 stars.")]
        public float threeStarTime = 30f;
        [Tooltip("Complete within this time for 2 stars.")]
        public float twoStarTime = 60f;
        [Tooltip("Complete within this time for 1 star.")]
        public float oneStarTime = 120f;

        [Header("Collectibles")]
        public int totalGems = 10;

        [Header("Time Limit")]
        public bool hasTimeLimit;
        [Tooltip("Time limit in seconds. Only used if hasTimeLimit is true.")]
        public float timeLimitSeconds = 180f;

        [Header("Spawn")]
        public Vector3 spawnPosition = Vector3.up;
        public Vector3 spawnRotation = Vector3.zero;

        [Header("Presentation")]
        public AudioClip musicTrack;
        public Material skyboxMaterial;

        [Header("Author Data")]
        [Tooltip("Designer's expected completion time.")]
        public float parTime = 45f;
        [Tooltip("Best recorded time by the design team.")]
        public float authorBestTime = 25f;

        [Header("Unlock Requirements")]
        [Tooltip("Minimum stars from the previous level to unlock. 0 = unlocked by default.")]
        [Range(0, 3)]
        public int requiredStarsFromPrevious;

        /// <summary>
        /// Calculate stars earned for a given completion time.
        /// </summary>
        public int CalculateStars(float completionTime)
        {
            if (completionTime <= threeStarTime) return 3;
            if (completionTime <= twoStarTime) return 2;
            if (completionTime <= oneStarTime) return 1;
            return 1; // Always at least 1 star for completing
        }
    }
}
