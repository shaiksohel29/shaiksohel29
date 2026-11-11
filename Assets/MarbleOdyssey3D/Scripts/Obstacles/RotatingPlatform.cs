// ============================================================================
// Marble Odyssey 3D — Rotating Platform
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    public class RotatingPlatform : ObstacleBase
    {
        [Header("Rotation")]
        [SerializeField] private Vector3 _rotationAxis = Vector3.up;
        [SerializeField] private float _degreesPerSecond = 45f;
        [SerializeField] private bool _reverseDirection;

        [Header("Platform Physics")]
        [Tooltip("Rigidbody for kinematic rotation — lets marble ride on top via friction.")]
        [SerializeField] private Rigidbody _rb;

        private Quaternion _startRotation;

        protected override void Start()
        {
            base.Start();
            _startRotation = transform.rotation;

            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        protected override void ObstacleUpdate()
        {
            float speed = _degreesPerSecond * _speedMultiplier;
            if (_reverseDirection) speed = -speed;

            if (_rb != null)
            {
                Quaternion delta = Quaternion.Euler(_rotationAxis.normalized * speed * Time.fixedDeltaTime);
                _rb.MoveRotation(_rb.rotation * delta);
            }
            else
            {
                transform.Rotate(_rotationAxis.normalized, speed * Time.deltaTime, Space.Self);
            }
        }

        private void FixedUpdate()
        {
            // Use FixedUpdate for Rigidbody rotation so marble friction works properly
            if (_rb != null && _isActive)
            {
                float speed = _degreesPerSecond * _speedMultiplier;
                if (_reverseDirection) speed = -speed;
                Quaternion delta = Quaternion.Euler(_rotationAxis.normalized * speed * Time.fixedDeltaTime);
                _rb.MoveRotation(_rb.rotation * delta);
            }
        }

        public override void ResetObstacle()
        {
            base.ResetObstacle();
            transform.rotation = _startRotation;
            if (_rb != null) _rb.MoveRotation(_startRotation);
        }
    }
}
