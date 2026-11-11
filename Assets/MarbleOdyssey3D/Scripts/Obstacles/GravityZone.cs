using UnityEngine;

namespace MarbleOdyssey
{
    public class GravityZone : ObstacleBase
    {
        [Header("Gravity")]
        [SerializeField] private Vector3 _gravityDirection = Vector3.up;
        [SerializeField] private float _gravityStrength = 15f;
        [SerializeField] private float _transitionSpeed = 5f;

        [Header("Visuals")]
        [SerializeField] private ParticleSystem _distortionParticles;

        private Rigidbody _affectedRb;
        private bool _marbleInside;

        protected override void ObstacleUpdate() { }

        private void FixedUpdate()
        {
            if (!_isActive || !_marbleInside || _affectedRb == null) return;

            // Cancel default gravity and apply custom
            _affectedRb.AddForce(-Physics.gravity, ForceMode.Acceleration);
            Vector3 customGravity = _gravityDirection.normalized * _gravityStrength * _speedMultiplier;
            _affectedRb.AddForce(customGravity, ForceMode.Acceleration);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _affectedRb = other.GetComponent<Rigidbody>();
            _marbleInside = true;
            if (_distortionParticles != null) _distortionParticles.Play();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _marbleInside = false;
            _affectedRb = null;
            if (_distortionParticles != null) _distortionParticles.Stop();
        }

        public override void ResetObstacle()
        {
            base.ResetObstacle();
            _marbleInside = false;
            _affectedRb = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
            Gizmos.DrawCube(transform.position, transform.localScale);
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, _gravityDirection.normalized * 2f);
        }
#endif
    }
}
