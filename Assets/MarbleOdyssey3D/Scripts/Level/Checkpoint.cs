// ============================================================================
// Marble Odyssey 3D — Checkpoint
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int _checkpointIndex;
        [SerializeField] private Transform _respawnPoint;

        [Header("Visuals")]
        [SerializeField] private Renderer _flagRenderer;
        [SerializeField] private Color _inactiveColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _activeColor = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private float _glowIntensity = 2f;
        [SerializeField] private ParticleSystem _activationParticles;

        [Header("Audio")]
        [SerializeField] private AudioClip _activationSound;
        [SerializeField] [Range(0f, 1f)] private float _volume = 0.8f;

        private bool _activated;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
            SetVisualState(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_activated) return;
            if (!other.CompareTag("Player")) return;

            _activated = true;

            // Determine respawn position
            Vector3 pos = _respawnPoint != null
                ? _respawnPoint.position
                : transform.position + Vector3.up * 0.5f;
            Quaternion rot = _respawnPoint != null
                ? _respawnPoint.rotation
                : Quaternion.identity;

            LevelManager.Instance?.SetCheckpoint(pos, rot, _checkpointIndex);

            // Visual feedback
            SetVisualState(true);

            // Particles
            if (_activationParticles != null)
                _activationParticles.Play();

            // Sound
            if (_activationSound != null)
                AudioSource.PlayClipAtPoint(_activationSound, transform.position, _volume);
        }

        private void SetVisualState(bool active)
        {
            if (_flagRenderer == null) return;

            var mpb = new MaterialPropertyBlock();
            _flagRenderer.GetPropertyBlock(mpb);

            Color col = active ? _activeColor : _inactiveColor;
            mpb.SetColor("_BaseColor", col);

            if (active)
                mpb.SetColor(EmissionColor, _activeColor * _glowIntensity);
            else
                mpb.SetColor(EmissionColor, Color.black);

            _flagRenderer.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// Reset checkpoint (on level restart).
        /// </summary>
        public void ResetCheckpoint()
        {
            _activated = false;
            SetVisualState(false);
        }
    }
}
