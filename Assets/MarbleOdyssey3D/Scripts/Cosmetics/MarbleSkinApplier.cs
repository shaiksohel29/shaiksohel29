using UnityEngine;

namespace MarbleOdyssey
{
    public class MarbleSkinApplier : MonoBehaviour
    {
        [SerializeField] private Renderer _marbleRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;

        private void OnEnable()
        {
            if (CosmeticsManager.Instance != null)
                CosmeticsManager.Instance.OnSkinChanged += ApplySkin;

            ApplyCurrentSkin();
        }

        private void OnDisable()
        {
            if (CosmeticsManager.Instance != null)
                CosmeticsManager.Instance.OnSkinChanged -= ApplySkin;
        }

        public void ApplyCurrentSkin()
        {
            if (CosmeticsManager.Instance == null) return;
            var skin = CosmeticsManager.Instance.GetEquippedSkin();
            if (skin != null) ApplySkin(skin);
        }

        private void ApplySkin(MarbleSkin skin)
        {
            if (skin.MaterialOverride != null && _marbleRenderer != null)
                _marbleRenderer.material = skin.MaterialOverride;

            if (skin.TrailColor != null && _trailRenderer != null)
                _trailRenderer.colorGradient = skin.TrailColor;
        }
    }
}
