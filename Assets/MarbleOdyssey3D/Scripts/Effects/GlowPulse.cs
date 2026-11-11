using UnityEngine;

namespace MarbleOdyssey
{
    public class GlowPulse : MonoBehaviour
    {
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _minIntensity = 0.5f;
        [SerializeField] private float _maxIntensity = 2f;
        [SerializeField] private Color _emissionColor = new Color(0f, 1f, 0.8f);
        [SerializeField] private float _phaseOffset;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (_renderer == null) return;

            float t = (Mathf.Sin((Time.time + _phaseOffset) * _pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            float intensity = Mathf.Lerp(_minIntensity, _maxIntensity, t);

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColorID, _emissionColor * intensity);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
