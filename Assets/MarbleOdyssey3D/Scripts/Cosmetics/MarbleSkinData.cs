using UnityEngine;

namespace MarbleOdyssey
{
    [CreateAssetMenu(fileName = "MarbleSkin", menuName = "Marble Odyssey/Marble Skin")]
    public class MarbleSkinData : ScriptableObject
    {
        [Header("Identity")]
        public string SkinId;
        public string DisplayName;
        [TextArea(1, 2)] public string Description;
        public Sprite Thumbnail;

        [Header("Appearance")]
        public Material Material;
        public bool OverrideColor;
        public Color BaseColor = Color.white;
        public Color EmissionColor = Color.black;
        [Range(0f, 2f)] public float EmissionIntensity;

        [Header("Trail")]
        public Gradient TrailGradient;
        [Range(0.05f, 0.5f)] public float TrailWidth = 0.15f;

        [Header("Particles")]
        public Color ParticleTint = Color.white;

        [Header("Economy")]
        public int Price;
        public bool IsPremium;

        [Header("Unlock")]
        public UnlockMethod Unlock;
        public int RequiredStars;
        public string RequiredWorldId;
    }

    public enum UnlockMethod
    {
        Default,
        GemPurchase,
        StarThreshold,
        WorldCompletion,
        DailyReward,
        Special
    }
}
