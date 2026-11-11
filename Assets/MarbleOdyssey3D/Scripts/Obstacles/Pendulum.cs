// ============================================================================
// Marble Odyssey 3D — Pendulum Obstacle
// Swinging obstacle that knocks marble off platforms.
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    public class Pendulum : ObstacleBase
    {
        [Header("Pendulum")]
        [SerializeField] private float _maxAngle = 60f;
        [SerializeField] private float _swingSpeed = 2f;
        [SerializeField] private Vector3 _swingAxis = Vector3.forward;
        [SerializeField] private float _knockbackForce = 12f;

        [Header("Visuals")]
        [SerializeField] private Transform _armPivot;

        private Quaternion _startRotation;
        private float _phase;

        protected override void Start()
        {
            base.Start();
            Transform pivot = _armPivot != null ? _armPivot : transform;
            _startRotation = pivot.localRotation;
            _phase = _phaseOffset * Mathf.PI * 2f;
        }

        protected override void ObstacleUpdate()
        {
            _phase += Time.deltaTime * _swingSpeed * _speedMultiplier;
            float angle = Mathf.Sin(_phase) * _maxAngle;

            Transform pivot = _armPivot != null ? _armPivot : transform;
            pivot.localRotation = _startRotation * Quaternion.AngleAxis(angle,
                _swingAxis.normalized);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.collider.CompareTag("Player")) return;

            Rigidbody marbleRb = collision.collider.GetComponent<Rigidbody>();
            if (marbleRb == null) return;

            // Calculate knockback direction
            Vector3 knockDir = (collision.transform.position - transform.position).normalized;
            knockDir.y = Mathf.Max(knockDir.y, 0.3f); // Add upward component
            knockDir.Normalize();

            marbleRb.AddForce(knockDir * _knockbackForce, ForceMode.Impulse);
        }

        public override void ResetObstacle()
        {
            base.ResetObstacle();
            _phase = _phaseOffset * Mathf.PI * 2f;
            Transform pivot = _armPivot != null ? _armPivot : transform;
            pivot.localRotation = _startRotation;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
            Vector3 swingEnd = transform.position + Vector3.down * 3f;
            Gizmos.DrawLine(transform.position, swingEnd);
        }
#endif
    }
}
