using UnityEngine;

namespace MarbleOdyssey
{
    public class JumpPad : ObstacleBase
    {
        [SerializeField] private float _launchForce = 12f;
        [SerializeField] private Vector3 _launchDirection = Vector3.up;
        [SerializeField] private ParticleSystem _activateEffect;
        [SerializeField] private Animator _animator;

        private static readonly int TriggerActivate = Animator.StringToHash("Activate");

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(GameConstants.Tags.Player)) return;

            var rb = other.attachedRigidbody;
            if (rb == null) return;

            Vector3 dir = transform.TransformDirection(_launchDirection.normalized);
            Vector3 vel = rb.linearVelocity;
            vel.y = 0f;
            rb.linearVelocity = vel;
            rb.AddForce(dir * _launchForce, ForceMode.Impulse);

            if (_activateEffect != null) _activateEffect.Play();
            if (_animator != null) _animator.SetTrigger(TriggerActivate);

            EventBus.Publish(new AudioPlayEvent { ClipName = "jumppad", Volume = 0.9f });
        }
    }
}
