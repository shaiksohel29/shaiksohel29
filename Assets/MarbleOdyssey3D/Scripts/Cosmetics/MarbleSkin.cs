using UnityEngine;

namespace MarbleOdyssey
{
    public enum SkinUnlockType
    {
        Default,
        GemPurchase,
        WorldComplete,
        DailyStreak,
        Special
    }

    [CreateAssetMenu(fileName = "MarbleSkin", menuName = "Marble Odyssey/Cosmetics/Marble Skin")]
    public class MarbleSkin : ScriptableObject
    {
        public string SkinId;
        public string DisplayName;
        [TextArea] public string Description;
        public Sprite PreviewSprite;
        public Material MaterialOverride;
        public Gradient TrailColor;

        [Header("Unlock")]
        public SkinUnlockType UnlockType;
        public int GemCost;
        public int RequiredWorldIndex = -1;
        public int RequiredStreak;
    }
}
