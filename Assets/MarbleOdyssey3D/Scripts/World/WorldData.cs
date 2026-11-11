// ============================================================================
// Marble Odyssey 3D — World Data ScriptableObject
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    [CreateAssetMenu(fileName = "WorldData", menuName = "Marble Odyssey/World Data")]
    public class WorldData : ScriptableObject
    {
        [Header("Identity")]
        public string worldName = "Unnamed World";
        [TextArea(2, 4)]
        public string description = "";
        public int worldIndex;
        public Sprite worldIcon;

        [Header("Theme Colors")]
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;
        public Color accentColor = Color.cyan;

        [Header("Environment")]
        public Material skyboxMaterial;
        public Color ambientLightColor = new Color(0.5f, 0.5f, 0.6f);
        [Tooltip("Enable distance fog for this world.")]
        public bool enableFog = true;
        public Color fogColor = new Color(0.7f, 0.8f, 0.9f);
        [Tooltip("Fog start distance.")]
        public float fogStart = 30f;
        [Tooltip("Fog end distance.")]
        public float fogEnd = 120f;

        [Header("Audio")]
        public AudioClip worldMusic;
        public AudioClip ambientLoop;

        [Header("Unlock")]
        [Tooltip("Total cumulative stars needed to unlock this world. 0 = always unlocked.")]
        public int requiredTotalStars;

        [Header("Levels")]
        public LevelData[] levels = new LevelData[0];

        /// <summary>
        /// Number of levels in this world.
        /// </summary>
        public int LevelCount => levels != null ? levels.Length : 0;
    }
}
