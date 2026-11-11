using UnityEngine;

namespace MarbleOdyssey
{
    public class Bumper : MonoBehaviour
    {
        [SerializeField] private float _bounceForce = 20f;
        [SerializeField] private float _cooldown = 0.2f;
        [SerializeField] private Animator _animator;
        [SerializeField] private ParticleSystem _hitParticles;

        private float _lastHitTime = -10f;
        private static readonly int TriggerHash = Animator.StringToHash("Bounce");

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.collider.CompareTag("Marble")) return;
            if (Time.time - _lastHitTime < _cooldown) return;

            _lastHitTime = Time.time;

            var rb = collision.collider.attachedRigidbody;
            if (rb == null) return;

            Vector3 bounceDir = (collision.collider.transform.position - transform.position).normalized;
            bounceDir.y = Mathf.Max(bounceDir.y, 0.2f);
            bounceDir.Normalize();

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(bounceDir * _bounceForce, ForceMode.VelocityChange);

            if (_animator != null) _animator.SetTrigger(TriggerHash);
            if (_hitParticles != null) _hitParticles.Play();

            AudioManager.Instance?.PlaySFX(AudioManager.Instance.SFXLib?.BumperHit);
            ScreenShakeEffect.ShakeOnBumper();
        }
    }
}
