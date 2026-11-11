using UnityEngine;

namespace MarbleOdyssey
{
    public class SpeedPad : ObstacleBase
    {
        [Header("Boost")]
        [SerializeField] private float _boostForce = 20f;
        [SerializeField] private float _cooldown = 0.5f;

        [Header("Effects")]
        [SerializeField] private AudioClip _boostSound;
        [SerializeField] private ParticleSystem _boostParticles;

        private float _lastBoostTime = -10f;

        protected override void ObstacleUpdate() { }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || !other.CompareTag("Player")) return;
            if (Time.time - _lastBoostTime < _cooldown) return;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb == null) return;

            _lastBoostTime = Time.time;
            Vector3 dir = transform.forward;
            rb.AddForce(dir * _boostForce * _speedMultiplier, ForceMode.Impulse);

            if (_boostParticles != null) _boostParticles.Play();
            if (_boostSound != null)
                AudioSource.PlayClipAtPoint(_boostSound, transform.position);
        }

        public override void ResetObstacle()
        {
            base.ResetObstacle();
            _lastBoostTime = -10f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
#endif
    }
}
