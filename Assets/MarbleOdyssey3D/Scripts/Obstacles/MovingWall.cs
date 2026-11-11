// ============================================================================
// Marble Odyssey 3D — Moving Wall / Platform
// Moves between waypoints on a configurable path.
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    public class MovingWall : ObstacleBase
    {
        [Header("Path")]
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _pauseAtWaypoint = 0.5f;
        [SerializeField] private bool _pingPong = true;

        [Header("Physics")]
        [SerializeField] private Rigidbody _rb;

        private int _currentIndex;
        private int _direction = 1;
        private float _pauseTimer;
        private Vector3 _startPosition;

        protected override void Start()
        {
            base.Start();
            _startPosition = transform.position;

            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogWarning($"[MovingWall] {name}: No waypoints assigned.");
            }
        }

        protected override void ObstacleUpdate()
        {
            if (_waypoints == null || _waypoints.Length < 2) return;

            // Pause at waypoint
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                return;
            }

            Transform target = _waypoints[_currentIndex];
            if (target == null) return;

            float speed = _moveSpeed * _speedMultiplier;
            Vector3 targetPos = target.position;
            Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos,
                speed * Time.deltaTime);

            if (_rb != null)
                _rb.MovePosition(newPos);
            else
                transform.position = newPos;

            // Reached waypoint?
            if (Vector3.Distance(newPos, targetPos) < 0.01f)
            {
                _pauseTimer = _pauseAtWaypoint;
                AdvanceWaypoint();
            }
        }

        private void AdvanceWaypoint()
        {
            _currentIndex += _direction;

            if (_pingPong)
            {
                if (_currentIndex >= _waypoints.Length)
                {
                    _currentIndex = _waypoints.Length - 2;
                    _direction = -1;
                }
                else if (_currentIndex < 0)
                {
                    _currentIndex = 1;
                    _direction = 1;
                }
            }
            else
            {
                if (_currentIndex >= _waypoints.Length)
                    _currentIndex = 0;
            }
        }

        public override void ResetObstacle()
        {
            base.ResetObstacle();
            _currentIndex = 0;
            _direction = 1;
            _pauseTimer = 0f;
            transform.position = _startPosition;
            if (_rb != null) _rb.MovePosition(_startPosition);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_waypoints == null || _waypoints.Length < 2) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null) continue;
                Gizmos.DrawWireSphere(_waypoints[i].position, 0.3f);
                if (i < _waypoints.Length - 1 && _waypoints[i + 1] != null)
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
            }
        }
#endif
    }
}
