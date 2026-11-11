using UnityEngine;

namespace MarbleOdyssey
{
    [CreateAssetMenu(fileName = "ChallengeModifiers", menuName = "Marble Odyssey/Daily Challenge/Modifiers")]
    public class ChallengeModifiers : ScriptableObject
    {
        [Header("World Selection")]
        [Tooltip("How many worlds the daily challenge can draw from")]
        public int MaxUnlockedWorlds = 3;

        [Header("Speed")]
        public float MinSpeedMultiplier = 0.8f;
        public float MaxSpeedMultiplier = 1.5f;

        [Header("Gravity")]
        public float MinGravityMultiplier = 0.8f;
        public float MaxGravityMultiplier = 1.3f;

        [Header("Time")]
        public float MinTimeLimit = 60f;
        public float MaxTimeLimit = 180f;

        [Header("Gems")]
        public int MinGemTarget = 3;
        public int MaxGemTarget = 10;
    }
}
