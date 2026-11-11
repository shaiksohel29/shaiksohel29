using UnityEngine;

namespace MarbleOdyssey
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserGate : ObstacleBase
    {
        [Header("Laser")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField] private float _laserWidth = 0.1f;

        [Header("Colors")]
        [SerializeField] private Color _activeColor = Color.red;
        [SerializeField] private Color _inactiveColor = Color.green;

        [Header("Effects")]
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] private ParticleSystem _hitParticles;

        private LineRenderer _lineRenderer;
        private Collider _triggerCollider;

        protected override void Start()
        {
            base.Start();
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.startWidth = _laserWidth;
            _lineRenderer.endWidth = _laserWidth;
            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;

            _triggerCollider = GetComponent<Collider>();
            if (_triggerCollider != null) _triggerCollider.isTrigger = true;

            UpdateLaserVisual();
        }

        protected override void ObstacleUpdate()
        {
            UpdateLaserVisual();
        }

        protected override void OnActivated()
        {
            UpdateLaserVisual();
        }

        protected override void OnDeactivated()
        {
            UpdateLaserVisual();
        }

        private void UpdateLaserVisual()
        {
            Vector3 start = _startPoint != null ? _startPoint.position : transform.position;
            Vector3 end = _endPoint != null ? _endPoint.position : transform.position + transform.forward * 5f;

            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, end);

            Color c = _isActive ? _activeColor : _inactiveColor;
            _lineRenderer.startColor = c;
            _lineRenderer.endColor = c;

            if (_triggerCollider != null)
                _triggerCollider.enabled = _isActive;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || !other.CompareTag("Player")) return;

            if (_hitParticles != null)
            {
                var ps = Instantiate(_hitParticles, other.transform.position, Quaternion.identity);
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + 1f);
            }

            if (_deathSound != null)
                AudioSource.PlayClipAtPoint(_deathSound, other.transform.position);

            CameraController.Instance?.Shake(0.3f, 0.5f);
            LevelManager.Instance?.TriggerDeath();
        }

        public override void ResetObstacle()
        {
            base.ResetObstacle();
            UpdateLaserVisual();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 start = _startPoint != null ? _startPoint.position : transform.position;
            Vector3 end = _endPoint != null ? _endPoint.position : transform.position + transform.forward * 5f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(start, end);
        }
#endif
    }
}
