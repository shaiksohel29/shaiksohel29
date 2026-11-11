// ============================================================================
// Marble Odyssey 3D — Premium 3D Marble Rolling Game
// CameraController.cs — Advanced follow camera with cinematic support
// ============================================================================

using System;
using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// Premium third-person follow camera for the marble.
    /// Features smooth follow, look-ahead, manual orbit, cinematic mode,
    /// collision avoidance, and per-world presets.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        private static CameraController _inst;
        public static CameraController Instance => _inst;
        // =====================================================================
        // Camera Preset
        // =====================================================================

        [Serializable]
        public class CameraPreset
        {
            public string name = "Default";
            public Vector3 offset = new Vector3(0f, 8f, -12f);
            public float followDamping = 5f;
            public float rotationDamping = 3f;
            public float lookAheadDistance = 3f;
            public float lookAheadSmoothing = 8f;
            public float minDistance = 5f;
            public float maxDistance = 20f;
            public float defaultDistance = 12f;
            public float fieldOfView = 60f;
            [Range(-30f, 80f)] public float pitchAngle = 30f;
            [Range(0f, 1f)] public float lookAtHeightOffset = 1.5f;
        }

        // =====================================================================
        // Inspector
        // =====================================================================

        [Header("Target")]
        [SerializeField] private Transform _target;

        [Header("Active Preset")]
        [SerializeField] private CameraPreset _preset = new CameraPreset();

        [Header("Orbit Settings")]
        [Tooltip("Horizontal orbit sensitivity for touch swipe.")]
        [SerializeField] private float _orbitSensitivity = 0.2f;
        [Tooltip("Vertical orbit sensitivity.")]
        [SerializeField] private float _verticalOrbitSensitivity = 0.1f;
        [Tooltip("Minimum pitch angle.")]
        [SerializeField] private float _minPitch = 5f;
        [Tooltip("Maximum pitch angle.")]
        [SerializeField] private float _maxPitch = 75f;

        [Header("Zoom")]
        [SerializeField] private float _zoomSensitivity = 0.5f;
        [SerializeField] private float _zoomDamping = 5f;

        [Header("Collision Avoidance")]
        [SerializeField] private float _collisionRadius = 0.3f;
        [SerializeField] private LayerMask _collisionLayers = ~0;

        [Header("Auto-Rotate")]
        [Tooltip("Automatically rotate the camera behind the marble's movement direction.")]
        [SerializeField] private bool _autoRotateBehind = true;
        [SerializeField] private float _autoRotateSpeed = 1.5f;
        [Tooltip("Minimum speed before auto-rotate engages.")]
        [SerializeField] private float _autoRotateSpeedThreshold = 3f;
        [Tooltip("Seconds without touch input before auto-rotate kicks in.")]
        [SerializeField] private float _autoRotateDelay = 2f;

        [Header("Cinematic")]
        [SerializeField] private float _cinematicTransitionSpeed = 2f;

        // =====================================================================
        // Private State
        // =====================================================================

        private Camera _camera;
        private float _currentYaw;
        private float _currentPitch;
        private float _currentDistance;
        private float _targetDistance;
        private Vector3 _lookAheadOffset;
        private Vector3 _smoothVelocity;
        private float _lastManualInputTime = Mathf.NegativeInfinity;

        // Cinematic
        private bool _isCinematicMode;
        private Vector3 _cinematicPosition;
        private Quaternion _cinematicRotation;
        private float _cinematicFov;
        private float _cinematicBlend;

        // Preset transition
        private CameraPreset _transitionFrom;
        private CameraPreset _transitionTo;
        private float _presetTransitionT = 1f;
        private float _presetTransitionSpeed = 2f;

        // Shake (delegated to CameraShake)
        private CameraShake _shake;

        // =====================================================================
        // Properties
        // =====================================================================

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        public bool IsCinematicMode => _isCinematicMode;

        // =====================================================================
        // Lifecycle
        // =====================================================================

        private void Awake()
        {
            if (_inst != null && _inst != this) { Destroy(gameObject); return; }
            _inst = this;

            _camera = GetComponent<Camera>();
            if (_camera == null)
                _camera = GetComponentInChildren<Camera>();

            _shake = GetComponent<CameraShake>();

            _currentDistance = _preset.defaultDistance;
            _targetDistance = _preset.defaultDistance;
            _currentPitch = _preset.pitchAngle;

            if (_camera != null)
                _camera.fieldOfView = _preset.fieldOfView;
        }

        private void Start()
        {
            if (_target != null)
            {
                // Initialize yaw to face target's forward
                Vector3 dir = _target.position - transform.position;
                _currentYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            if (_isCinematicMode)
            {
                UpdateCinematic();
            }
            else
            {
                UpdateAutoRotation();
                UpdateDistance();
                UpdateFollowCamera();
                UpdateLookAhead();
                ApplyCollisionAvoidance();
            }

            // Preset interpolation
            if (_presetTransitionT < 1f)
            {
                _presetTransitionT += Time.deltaTime * _presetTransitionSpeed;
                _presetTransitionT = Mathf.Clamp01(_presetTransitionT);
                LerpPreset(_transitionFrom, _transitionTo, _presetTransitionT);
            }
        }

        // =====================================================================
        // Follow Camera
        // =====================================================================

        private void UpdateFollowCamera()
        {
            float yawRad = _currentYaw * Mathf.Deg2Rad;
            float pitchRad = _currentPitch * Mathf.Deg2Rad;

            // Spherical to Cartesian offset
            Vector3 offset;
            offset.x = Mathf.Sin(yawRad) * Mathf.Cos(pitchRad) * _currentDistance;
            offset.y = Mathf.Sin(pitchRad) * _currentDistance;
            offset.z = Mathf.Cos(yawRad) * Mathf.Cos(pitchRad) * _currentDistance;

            Vector3 targetPos = _target.position + Vector3.up * _preset.lookAtHeightOffset;
            Vector3 desiredPos = targetPos + offset;

            // Smooth follow
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref _smoothVelocity,
                1f / _preset.followDamping);

            // Look at target with look-ahead
            Vector3 lookTarget = targetPos + _lookAheadOffset;
            Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, desiredRot,
                Time.deltaTime * _preset.rotationDamping);
        }

        // =====================================================================
        // Look-Ahead
        // =====================================================================

        private void UpdateLookAhead()
        {
            if (_target == null) return;

            var marble = _target.GetComponent<MarbleController>();
            if (marble == null) return;

            Vector3 vel = marble.Velocity;
            vel.y = 0f; // Only horizontal look-ahead

            Vector3 targetLookAhead = vel.normalized *
                Mathf.Min(vel.magnitude, _preset.lookAheadDistance);

            _lookAheadOffset = Vector3.Lerp(
                _lookAheadOffset, targetLookAhead,
                Time.deltaTime * _preset.lookAheadSmoothing);
        }

        // =====================================================================
        // Auto-Rotation
        // =====================================================================

        private void UpdateAutoRotation()
        {
            if (!_autoRotateBehind) return;

            // Don't auto-rotate if the player recently touched the camera
            if (Time.time - _lastManualInputTime < _autoRotateDelay) return;

            var marble = _target.GetComponent<MarbleController>();
            if (marble == null || marble.Speed < _autoRotateSpeedThreshold) return;

            Vector3 vel = marble.Velocity;
            vel.y = 0f;
            if (vel.sqrMagnitude < 0.5f) return;

            float targetYaw = Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg + 180f;
            _currentYaw = Mathf.LerpAngle(_currentYaw, targetYaw,
                Time.deltaTime * _autoRotateSpeed);
        }

        // =====================================================================
        // Orbit Input (called by InputManager)
        // =====================================================================

        /// <summary>
        /// Apply orbit rotation from touch swipe delta.
        /// </summary>
        public void ApplyOrbitDelta(Vector2 delta)
        {
            _currentYaw += delta.x * _orbitSensitivity;
            _currentPitch -= delta.y * _verticalOrbitSensitivity;
            _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);

            _lastManualInputTime = Time.time;
        }

        // =====================================================================
        // Zoom
        // =====================================================================

        /// <summary>
        /// Apply pinch zoom. Positive = zoom in, negative = zoom out.
        /// </summary>
        public void ApplyZoom(float delta)
        {
            _targetDistance -= delta * _zoomSensitivity;
            _targetDistance = Mathf.Clamp(_targetDistance, _preset.minDistance, _preset.maxDistance);
        }

        private void UpdateDistance()
        {
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance,
                Time.deltaTime * _zoomDamping);
        }

        // =====================================================================
        // Collision Avoidance
        // =====================================================================

        private void ApplyCollisionAvoidance()
        {
            if (_target == null) return;

            Vector3 targetPos = _target.position + Vector3.up * _preset.lookAtHeightOffset;
            Vector3 dir = transform.position - targetPos;
            float dist = dir.magnitude;

            if (Physics.SphereCast(targetPos, _collisionRadius, dir.normalized,
                out RaycastHit hit, dist, _collisionLayers, QueryTriggerInteraction.Ignore))
            {
                // Move camera in front of the obstacle
                float safeDist = hit.distance - _collisionRadius;
                safeDist = Mathf.Max(safeDist, _preset.minDistance * 0.5f);
                transform.position = targetPos + dir.normalized * safeDist;
            }
        }

        // =====================================================================
        // Cinematic Mode
        // =====================================================================

        /// <summary>
        /// Enter cinematic mode with a specified camera position, rotation, and FOV.
        /// </summary>
        public void EnterCinematic(Vector3 position, Quaternion rotation, float fov = 50f)
        {
            _isCinematicMode = true;
            _cinematicPosition = position;
            _cinematicRotation = rotation;
            _cinematicFov = fov;
            _cinematicBlend = 0f;
        }

        /// <summary>
        /// Exit cinematic mode and return to normal follow camera.
        /// </summary>
        public void ExitCinematic()
        {
            _isCinematicMode = false;
        }

        private void UpdateCinematic()
        {
            _cinematicBlend += Time.deltaTime * _cinematicTransitionSpeed;
            _cinematicBlend = Mathf.Clamp01(_cinematicBlend);

            transform.position = Vector3.Lerp(transform.position, _cinematicPosition,
                _cinematicBlend);
            transform.rotation = Quaternion.Slerp(transform.rotation, _cinematicRotation,
                _cinematicBlend);

            if (_camera != null)
            {
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _cinematicFov,
                    _cinematicBlend);
            }
        }

        // =====================================================================
        // Preset Transitions
        // =====================================================================

        /// <summary>
        /// Smoothly transition to a new camera preset.
        /// </summary>
        public void TransitionToPreset(CameraPreset newPreset, float speed = 2f)
        {
            _transitionFrom = CopyPreset(_preset);
            _transitionTo = newPreset;
            _presetTransitionT = 0f;
            _presetTransitionSpeed = speed;
        }

        /// <summary>
        /// Immediately set a preset.
        /// </summary>
        public void SetPreset(CameraPreset preset)
        {
            _preset = preset;
            _targetDistance = preset.defaultDistance;
            if (_camera != null) _camera.fieldOfView = preset.fieldOfView;
        }

        private void LerpPreset(CameraPreset a, CameraPreset b, float t)
        {
            _preset.followDamping = Mathf.Lerp(a.followDamping, b.followDamping, t);
            _preset.rotationDamping = Mathf.Lerp(a.rotationDamping, b.rotationDamping, t);
            _preset.lookAheadDistance = Mathf.Lerp(a.lookAheadDistance, b.lookAheadDistance, t);
            _preset.lookAtHeightOffset = Mathf.Lerp(a.lookAtHeightOffset, b.lookAtHeightOffset, t);
            _preset.pitchAngle = Mathf.Lerp(a.pitchAngle, b.pitchAngle, t);
            _targetDistance = Mathf.Lerp(a.defaultDistance, b.defaultDistance, t);

            if (_camera != null)
                _camera.fieldOfView = Mathf.Lerp(a.fieldOfView, b.fieldOfView, t);
        }

        private CameraPreset CopyPreset(CameraPreset src)
        {
            return new CameraPreset
            {
                name = src.name,
                offset = src.offset,
                followDamping = src.followDamping,
                rotationDamping = src.rotationDamping,
                lookAheadDistance = src.lookAheadDistance,
                lookAheadSmoothing = src.lookAheadSmoothing,
                minDistance = src.minDistance,
                maxDistance = src.maxDistance,
                defaultDistance = src.defaultDistance,
                fieldOfView = src.fieldOfView,
                pitchAngle = src.pitchAngle,
                lookAtHeightOffset = src.lookAtHeightOffset
            };
        }

        // =====================================================================
        // Shake Passthrough
        // =====================================================================

        /// <summary>
        /// Trigger camera shake (e.g. on death, impact).
        /// </summary>
        public void Shake(float intensity = 0.5f, float duration = 0.3f)
        {
            if (_shake != null)
                _shake.Trigger(intensity, duration);
        }
    }
}
