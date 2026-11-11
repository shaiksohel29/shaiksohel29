using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_URP
using UnityEngine.Rendering.Universal;
#endif

namespace MarbleOdyssey
{
    public class PostProcessController : MonoBehaviour
    {
        public static PostProcessController Instance { get; private set; }

        [SerializeField] private Volume _globalVolume;

#if UNITY_URP
        private Vignette _vignette;
        private Bloom _bloom;
        private ChromaticAberration _chromaticAberration;
        private ColorAdjustments _colorAdjustments;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

#if UNITY_URP
            if (_globalVolume != null && _globalVolume.profile != null)
            {
                _globalVolume.profile.TryGet(out _vignette);
                _globalVolume.profile.TryGet(out _bloom);
                _globalVolume.profile.TryGet(out _chromaticAberration);
                _globalVolume.profile.TryGet(out _colorAdjustments);
            }
#endif
        }

        public void SetVignette(float intensity)
        {
#if UNITY_URP
            if (_vignette != null)
            {
                _vignette.active = intensity > 0.01f;
                _vignette.intensity.value = Mathf.Clamp01(intensity);
            }
#endif
        }

        public void SetBloom(float intensity)
        {
#if UNITY_URP
            if (_bloom != null)
            {
                _bloom.active = intensity > 0.01f;
                _bloom.intensity.value = intensity;
            }
#endif
        }

        public void SetChromaticAberration(float intensity)
        {
#if UNITY_URP
            if (_chromaticAberration != null)
            {
                _chromaticAberration.active = intensity > 0.01f;
                _chromaticAberration.intensity.value = Mathf.Clamp01(intensity);
            }
#endif
        }

        public void ApplyDamageEffect()
        {
            SetVignette(0.6f);
            SetChromaticAberration(0.8f);
        }

        public void ApplySpeedEffect()
        {
            SetChromaticAberration(0.3f);
            SetBloom(1.5f);
        }

        public void ResetEffects()
        {
            SetVignette(0f);
            SetBloom(0.5f);
            SetChromaticAberration(0f);
        }
    }
}
