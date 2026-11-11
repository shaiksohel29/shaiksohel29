// ============================================================================
// Marble Odyssey 3D — Finish Zone (Level Completion Trigger)
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    [RequireComponent(typeof(Collider))]
    public class FinishZone : MonoBehaviour
    {
        [Header("Celebration Effects")]
        [SerializeField] private ParticleSystem[] _fireworks;
        [SerializeField] private ParticleSystem _confetti;
        [SerializeField] private AudioClip _completionFanfare;
        [SerializeField] [Range(0f, 1f)] private float _fanfareVolume = 1f;

        [Header("Visuals")]
        [SerializeField] private Renderer _portalRenderer;
        [SerializeField] private float _portalRotateSpeed = 60f;
        [SerializeField] private float _portalPulseSpeed = 2f;
        [SerializeField] private Color _portalColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private float _portalGlow = 3f;

        private bool _triggered;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Update()
        {
            // Animate portal
            if (_portalRenderer != null)
            {
                _portalRenderer.transform.Rotate(Vector3.up, _portalRotateSpeed * Time.deltaTime);

                float pulse = 1f + Mathf.Sin(Time.time * _portalPulseSpeed) * 0.2f;
                var mpb = new MaterialPropertyBlock();
                _portalRenderer.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionColor, _portalColor * _portalGlow * pulse);
                _portalRenderer.SetPropertyBlock(mpb);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player")) return;

            _triggered = true;

            // Launch celebration
            if (_fireworks != null)
            {
                foreach (var fw in _fireworks)
                {
                    if (fw != null) fw.Play();
                }
            }
            if (_confetti != null)
                _confetti.Play();

            if (_completionFanfare != null)
                AudioSource.PlayClipAtPoint(_completionFanfare, transform.position,
                    _fanfareVolume);

            // Complete the level
            LevelManager.Instance?.CompleteLevel();
        }

        /// <summary>
        /// Reset the finish zone on level restart.
        /// </summary>
        public void ResetZone()
        {
            _triggered = false;
        }
    }
}
