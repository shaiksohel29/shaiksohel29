// ============================================================================
// Marble Odyssey 3D — Premium 3D Marble Rolling Game
// CameraShake.cs — Perlin noise camera shake
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// Perlin-noise-based camera shake with configurable intensity,
    /// frequency, duration, and exponential decay.
    /// Attach alongside CameraController.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField] private float _defaultIntensity = 0.4f;
        [SerializeField] private float _defaultDuration = 0.35f;
        [SerializeField] private float _defaultFrequency = 25f;

        [Header("Behaviour")]
        [Tooltip("How quickly the shake decays. Higher = faster decay.")]
        [SerializeField] private float _decayRate = 4f;
        [Tooltip("Maximum positional offset in any axis.")]
        [SerializeField] private float _maxOffset = 1f;
        [Tooltip("Maximum rotational offset in degrees.")]
        [SerializeField] private float _maxRotation = 3f;

        private float _currentIntensity;
        private float _currentFrequency;
        private float _remaining;
        private float _totalDuration;
        private float _seed;

        private Vector3 _originalLocalPos;

        private void Awake()
        {
            _seed = Random.Range(0f, 1000f);
        }

        /// <summary>
        /// Trigger a camera shake.
        /// </summary>
        public void Trigger(float intensity = -1f, float duration = -1f, float frequency = -1f)
        {
            _currentIntensity = intensity > 0 ? intensity : _defaultIntensity;
            _totalDuration = duration > 0 ? duration : _defaultDuration;
            _currentFrequency = frequency > 0 ? frequency : _defaultFrequency;
            _remaining = _totalDuration;

            // Randomise the noise seed so consecutive shakes feel different
            _seed = Random.Range(0f, 1000f);
        }

        private void LateUpdate()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.deltaTime;
            float progress = 1f - (_remaining / _totalDuration);

            // Exponential decay
            float decay = Mathf.Exp(-_decayRate * progress);
            float amplitude = _currentIntensity * decay;

            float time = Time.time * _currentFrequency;

            // Perlin noise for smooth randomness (offset seed per axis)
            float offsetX = (Mathf.PerlinNoise(_seed, time) - 0.5f) * 2f;
            float offsetY = (Mathf.PerlinNoise(_seed + 100f, time) - 0.5f) * 2f;
            float offsetZ = (Mathf.PerlinNoise(_seed + 200f, time) - 0.5f) * 2f;

            Vector3 posOffset = new Vector3(offsetX, offsetY, offsetZ) * amplitude;
            posOffset = Vector3.ClampMagnitude(posOffset, _maxOffset);

            float rotX = (Mathf.PerlinNoise(_seed + 300f, time) - 0.5f) * 2f * amplitude * _maxRotation;
            float rotZ = (Mathf.PerlinNoise(_seed + 400f, time) - 0.5f) * 2f * amplitude * _maxRotation;

            // Apply as local offset (camera controller sets world position, we layer on top)
            transform.localPosition += posOffset;
            transform.localRotation *= Quaternion.Euler(rotX, 0f, rotZ);

            if (_remaining <= 0f)
            {
                // Ensure we don't accumulate drift
                _remaining = 0f;
            }
        }

        /// <summary>
        /// Stop any active shake immediately.
        /// </summary>
        public void Stop()
        {
            _remaining = 0f;
        }

        /// <summary>
        /// Whether a shake is currently active.
        /// </summary>
        public bool IsShaking => _remaining > 0f;
    }
}
