// ============================================================================
// Marble Odyssey 3D — Kill Zone (Death Trigger)
// Handles fall-off, lava, spikes — with camera shake and respawn.
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    public enum KillType
    {
        FallOff,
        Lava,
        Spikes,
        Laser,
        Void
    }

    [RequireComponent(typeof(Collider))]
    public class KillZone : MonoBehaviour
    {
        [Header("Type")]
        [SerializeField] private KillType _killType = KillType.FallOff;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _deathParticles;
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] [Range(0f, 1f)] private float _volume = 0.8f;

        [Header("Camera Shake")]
        [SerializeField] private float _shakeDuration = 0.3f;
        [SerializeField] private float _shakeIntensity = 0.5f;

        private void Start()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // Camera shake
            CameraController.Instance?.Shake(_shakeDuration, _shakeIntensity);

            // Death particles at marble position
            if (_deathParticles != null)
            {
                var ps = Instantiate(_deathParticles, other.transform.position,
                    Quaternion.identity);
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + 1f);
            }

            // Death sound
            if (_deathSound != null)
                AudioSource.PlayClipAtPoint(_deathSound, other.transform.position, _volume);

            // Haptic (Android)
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif

            // Trigger death in level manager
            LevelManager.Instance?.TriggerDeath();
        }
    }
}
