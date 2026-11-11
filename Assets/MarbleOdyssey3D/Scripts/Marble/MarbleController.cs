// ============================================================================
// Marble Odyssey 3D — Premium 3D Marble Rolling Game
// MarbleController.cs — Core marble physics, state machine & gameplay
// ============================================================================

using System;
using System.Collections;
using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// Main marble controller driving all physics-based movement, jumping,
    /// surface interactions, death/respawn, and visual state.
    /// Attach to the marble GameObject (requires Rigidbody + SphereCollider).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class MarbleController : MonoBehaviour
    {
        // =====================================================================
        // Types
        // =====================================================================

        public enum MarbleState
        {
            Idle,
            Rolling,
            Jumping,
            InAir,
            Boosting,
            Dead,
            Respawning,
            Finished
        }

        public enum SurfaceType
        {
            Normal,
            Ice,
            Mud,
            BouncePad,
            SpeedBoost,
            DeathZone
        }

        // =====================================================================
        // Inspector
        // =====================================================================

        [Header("Configuration")]
        [SerializeField] private MarblePhysicsConfig _config;

        [Header("References")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private ParticleSystem _deathParticles;
        [SerializeField] private ParticleSystem _respawnParticles;
        [SerializeField] private ParticleSystem _boostParticles;
        [SerializeField] private MeshRenderer _meshRenderer;

        [Header("Runtime (read-only)")]
        [SerializeField] private MarbleState _state = MarbleState.Idle;

        // =====================================================================
        // Components
        // =====================================================================

        private Rigidbody _rb;
        private SphereCollider _collider;

        // =====================================================================
        // State
        // =====================================================================

        // Ground
        private bool _isGrounded;
        private Vector3 _groundNormal = Vector3.up;
        private float _lastGroundedTime = Mathf.NegativeInfinity;
        private SurfaceType _currentSurface = SurfaceType.Normal;

        // Jump
        private float _lastJumpRequestTime = Mathf.NegativeInfinity;
        private float _lastJumpTime = Mathf.NegativeInfinity;
        private bool _jumpHeld;
        private bool _hasJumped;

        // Boost
        private float _boostEndTime;
        private Vector3 _boostDirection;

        // Input (set externally by InputManager)
        private Vector2 _moveInput;
        private bool _jumpPressed;
        private bool _jumpReleased;

        // Respawn
        private Vector3 _checkpointPosition;
        private Quaternion _checkpointRotation;
        private Vector3 _spawnPosition;

        // Gravity override
        private Vector3 _gravityDirection = Vector3.down;
        private float _gravityStrength;

        // Cosmetic
        private Material _currentSkin;

        // =====================================================================
        // Properties
        // =====================================================================

        public MarbleState State => _state;
        public bool IsGrounded => _isGrounded;
        public float Speed => _rb != null ? _rb.linearVelocity.magnitude : 0f;
        public float NormalizedSpeed => _config != null && _config.maxSpeed > 0
            ? Mathf.Clamp01(Speed / _config.maxSpeed) : 0f;
        public Vector3 Velocity => _rb != null ? _rb.linearVelocity : Vector3.zero;
        public SurfaceType CurrentSurface => _currentSurface;
        public MarblePhysicsConfig Config => _config;

        // =====================================================================
        // Events
        // =====================================================================

        public event Action OnJumped;
        public event Action OnLanded;
        public event Action OnDeath;
        public event Action OnRespawned;
        public event Action OnFinished;
        public event Action<SurfaceType> OnSurfaceChanged;
        public event Action OnBoostStarted;
        public event Action OnBoostEnded;

        // =====================================================================
        // Lifecycle
        // =====================================================================

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();

            if (_config == null)
            {
                Debug.LogError("[Marble] MarblePhysicsConfig is not assigned!");
                return;
            }

            ApplyPhysicsConfig();

            _spawnPosition = transform.position;
            _checkpointPosition = _spawnPosition;
            _checkpointRotation = transform.rotation;

            _gravityStrength = Physics.gravity.magnitude * _config.gravityMultiplier;
        }

        private void OnEnable()
        {
            if (_trailRenderer != null) _trailRenderer.emitting = false;
        }

        private void Update()
        {
            if (_state == MarbleState.Dead || _state == MarbleState.Respawning ||
                _state == MarbleState.Finished)
                return;

            UpdateGroundCheck();
            UpdateTrailEffect();
        }

        private void FixedUpdate()
        {
            if (_config == null) return;

            if (_state == MarbleState.Dead || _state == MarbleState.Respawning ||
                _state == MarbleState.Finished)
                return;

            ApplyCustomGravity();
            ProcessMovement();
            ProcessJump();
            ProcessBoost();
            ClampSpeed();
            UpdateState();
        }

        // =====================================================================
        // Physics Config
        // =====================================================================

        private void ApplyPhysicsConfig()
        {
            _rb.mass = _config.mass;
            _rb.linearDamping = _config.linearDrag;
            _rb.angularDamping = _config.angularDrag;
            _rb.useGravity = false; // We apply custom gravity
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        // =====================================================================
        // Input Interface (called by InputManager)
        // =====================================================================

        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        public void SetJumpPressed()
        {
            _jumpPressed = true;
            _jumpHeld = true;
            _lastJumpRequestTime = Time.time;
        }

        public void SetJumpReleased()
        {
            _jumpReleased = true;
            _jumpHeld = false;
        }

        // =====================================================================
        // Ground Detection
        // =====================================================================

        private void UpdateGroundCheck()
        {
            bool wasGrounded = _isGrounded;

            Vector3 origin = transform.position;
            float radius = _config.groundCheckRadius;
            float dist = _config.groundCheckDistance;

            if (Physics.SphereCast(origin, radius, -transform.up, out RaycastHit hit,
                dist + radius, _config.groundLayers, QueryTriggerInteraction.Ignore))
            {
                _isGrounded = true;
                _groundNormal = hit.normal;
                _lastGroundedTime = Time.time;

                // Detect surface
                DetectSurface(hit.collider);
            }
            else
            {
                _isGrounded = false;
            }

            // Landing event
            if (_isGrounded && !wasGrounded)
            {
                _hasJumped = false;
                OnLanded?.Invoke();
            }
        }

        private void DetectSurface(Collider col)
        {
            SurfaceType prev = _currentSurface;

            if (col.CompareTag("Ice"))
                _currentSurface = SurfaceType.Ice;
            else if (col.CompareTag("Mud"))
                _currentSurface = SurfaceType.Mud;
            else if (col.CompareTag("BouncePad"))
                _currentSurface = SurfaceType.BouncePad;
            else if (col.CompareTag("SpeedBoost"))
                _currentSurface = SurfaceType.SpeedBoost;
            else if (col.CompareTag("DeathZone"))
                _currentSurface = SurfaceType.DeathZone;
            else
                _currentSurface = SurfaceType.Normal;

            if (_currentSurface != prev)
                OnSurfaceChanged?.Invoke(_currentSurface);
        }

        // =====================================================================
        // Movement
        // =====================================================================

        private void ProcessMovement()
        {
            if (_moveInput.sqrMagnitude < 0.01f) return;

            // World-space direction relative to camera
            Vector3 camForward = _cameraTransform != null
                ? Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized
                : Vector3.forward;
            Vector3 camRight = _cameraTransform != null
                ? Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized
                : Vector3.right;

            Vector3 moveDir = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

            // Surface-specific modifiers
            float forceMultiplier = 1f;
            if (_currentSurface == SurfaceType.Ice)
                forceMultiplier = _config.iceForceMultiplier;
            else if (_currentSurface == SurfaceType.Mud)
                forceMultiplier = 0.7f;

            // Air control reduction
            if (!_isGrounded)
                forceMultiplier *= _config.airControlMultiplier;

            // Acceleration curve
            float speedFactor = _config.accelerationCurve.Evaluate(NormalizedSpeed);

            float force = _config.moveForce * forceMultiplier * speedFactor * _moveInput.magnitude;

            // Apply as torque for realistic rolling
            Vector3 torqueAxis = Vector3.Cross(Vector3.up, moveDir);
            _rb.AddTorque(torqueAxis * force, ForceMode.Force);

            // Also apply a small direct force for responsiveness
            _rb.AddForce(moveDir * (force * 0.3f), ForceMode.Force);

            // Grounded stick force
            if (_isGrounded)
            {
                _rb.AddForce(-_groundNormal * _config.groundStickForce, ForceMode.Force);
            }
        }

        // =====================================================================
        // Jump
        // =====================================================================

        private void ProcessJump()
        {
            // Jump buffer: accept input within buffer window
            bool jumpRequested = _jumpPressed ||
                                 (Time.time - _lastJumpRequestTime < _config.jumpBufferTime);

            // Coyote time: allow jump shortly after leaving ground
            bool canCoyote = (Time.time - _lastGroundedTime < _config.coyoteTime) && !_hasJumped;
            bool canJump = (_isGrounded || canCoyote) &&
                           (Time.time - _lastJumpTime > _config.jumpCooldown);

            if (jumpRequested && canJump)
            {
                ExecuteJump();
            }

            // Variable jump height: cut upward velocity on release
            if (_jumpReleased && _rb.linearVelocity.y > 0f && _hasJumped)
            {
                Vector3 vel = _rb.linearVelocity;
                vel.y *= _config.jumpCutMultiplier;
                _rb.linearVelocity = vel;
            }

            // Fall gravity boost for snappy feel
            if (!_isGrounded && _rb.linearVelocity.y < 0f)
            {
                _rb.AddForce(Vector3.down * _config.fallGravityBoost, ForceMode.Acceleration);
            }

            _jumpPressed = false;
            _jumpReleased = false;
        }

        private void ExecuteJump()
        {
            // Zero out existing vertical velocity
            Vector3 vel = _rb.linearVelocity;
            vel.y = 0f;
            _rb.linearVelocity = vel;

            _rb.AddForce(Vector3.up * _config.jumpForce, ForceMode.Impulse);

            _hasJumped = true;
            _lastJumpTime = Time.time;
            _lastJumpRequestTime = Mathf.NegativeInfinity;
            _isGrounded = false;

            _state = MarbleState.Jumping;
            OnJumped?.Invoke();
        }

        // =====================================================================
        // Custom Gravity
        // =====================================================================

        private void ApplyCustomGravity()
        {
            Vector3 gravity = _gravityDirection * _gravityStrength;
            _rb.AddForce(gravity, ForceMode.Acceleration);
        }

        /// <summary>
        /// Set a custom gravity direction (for gravity zones).
        /// </summary>
        public void SetGravityOverride(Vector3 direction, float strength = -1f)
        {
            _gravityDirection = direction.normalized;
            if (strength > 0f) _gravityStrength = strength;
        }

        /// <summary>
        /// Reset gravity to default down.
        /// </summary>
        public void ResetGravity()
        {
            _gravityDirection = Vector3.down;
            _gravityStrength = Physics.gravity.magnitude * _config.gravityMultiplier;
        }

        // =====================================================================
        // Speed Boost
        // =====================================================================

        public void ApplyBoost(Vector3 direction)
        {
            _boostDirection = direction.normalized;
            _boostEndTime = Time.time + _config.boostDuration;
            _state = MarbleState.Boosting;

            _rb.AddForce(_boostDirection * _config.boostForce, ForceMode.Impulse);

            if (_boostParticles != null) _boostParticles.Play();
            OnBoostStarted?.Invoke();
        }

        private void ProcessBoost()
        {
            if (_state == MarbleState.Boosting && Time.time >= _boostEndTime)
            {
                _state = _isGrounded ? MarbleState.Rolling : MarbleState.InAir;
                if (_boostParticles != null) _boostParticles.Stop();
                OnBoostEnded?.Invoke();
            }
        }

        // =====================================================================
        // Speed Clamping
        // =====================================================================

        private void ClampSpeed()
        {
            float maxSpd = _state == MarbleState.Boosting
                ? _config.boostMaxSpeed
                : _config.maxSpeed;

            if (_currentSurface == SurfaceType.Mud)
                maxSpd *= _config.mudSpeedMultiplier;

            // Only clamp horizontal speed, allow vertical freedom
            Vector3 vel = _rb.linearVelocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);

            if (horizontal.magnitude > maxSpd)
            {
                horizontal = horizontal.normalized * maxSpd;
                _rb.linearVelocity = new Vector3(horizontal.x, vel.y, horizontal.z);
            }
        }

        // =====================================================================
        // State Machine
        // =====================================================================

        private void UpdateState()
        {
            if (_state == MarbleState.Boosting || _state == MarbleState.Dead ||
                _state == MarbleState.Respawning || _state == MarbleState.Finished)
                return;

            if (!_isGrounded)
            {
                if (_state != MarbleState.Jumping)
                    _state = MarbleState.InAir;
            }
            else if (_moveInput.sqrMagnitude > 0.01f || Speed > 0.5f)
            {
                _state = MarbleState.Rolling;
            }
            else
            {
                _state = MarbleState.Idle;
            }
        }

        // =====================================================================
        // Trail Effect
        // =====================================================================

        private void UpdateTrailEffect()
        {
            if (_trailRenderer == null) return;
            _trailRenderer.emitting = Speed > _config.trailSpeedThreshold &&
                                      _state != MarbleState.Dead;
        }

        // =====================================================================
        // Collision Events
        // =====================================================================

        private void OnCollisionEnter(Collision collision)
        {
            if (_state == MarbleState.Dead) return;

            // Bounce pad
            if (collision.collider.CompareTag("BouncePad"))
            {
                Vector3 bounceDir = collision.contacts[0].normal;
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                _rb.AddForce(bounceDir * _config.bouncePadForce, ForceMode.Impulse);
                return;
            }

            // Speed boost trigger
            if (collision.collider.CompareTag("SpeedBoost"))
            {
                Vector3 boostDir = collision.collider.transform.forward;
                ApplyBoost(boostDir);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_state == MarbleState.Dead || _state == MarbleState.Finished) return;

            if (other.CompareTag("DeathZone"))
            {
                Die();
            }
            else if (other.CompareTag("Checkpoint"))
            {
                SetCheckpoint(other.transform.position + Vector3.up * 0.5f, other.transform.rotation);
            }
            else if (other.CompareTag("Finish"))
            {
                Finish();
            }
            else if (other.CompareTag("SpeedBoost"))
            {
                ApplyBoost(other.transform.forward);
            }
            else if (other.CompareTag("GravityZone"))
            {
                var zone = other.GetComponent<GravityZone>();
                if (zone != null)
                    SetGravityOverride(zone.GravityDirection, zone.GravityStrength);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("GravityZone"))
            {
                ResetGravity();
            }
        }

        // =====================================================================
        // Checkpoint
        // =====================================================================

        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            _checkpointPosition = position;
            _checkpointRotation = rotation;
        }

        // =====================================================================
        // Death & Respawn
        // =====================================================================

        public void Die()
        {
            if (_state == MarbleState.Dead || _state == MarbleState.Respawning) return;

            _state = MarbleState.Dead;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;

            if (_config.enableHaptics)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                Handheld.Vibrate();
#endif
            }

            if (_deathParticles != null)
                _deathParticles.Play();

            if (_trailRenderer != null)
                _trailRenderer.emitting = false;

            OnDeath?.Invoke();

            StartCoroutine(RespawnSequence());
        }

        private IEnumerator RespawnSequence()
        {
            // Fade out
            yield return FadeMesh(1f, 0f, _config.deathFadeDuration);

            _state = MarbleState.Respawning;

            // Wait
            yield return new WaitForSeconds(_config.respawnDelay);

            // Teleport to checkpoint
            transform.position = _checkpointPosition;
            transform.rotation = _checkpointRotation;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = false;
            ResetGravity();

            if (_respawnParticles != null)
                _respawnParticles.Play();

            // Fade in
            yield return FadeMesh(0f, 1f, _config.respawnFadeDuration);

            _state = MarbleState.Idle;
            _hasJumped = false;
            OnRespawned?.Invoke();
        }

        private IEnumerator FadeMesh(float from, float to, float duration)
        {
            if (_meshRenderer == null) yield break;

            Material mat = _meshRenderer.material;
            Color col = mat.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                col.a = Mathf.Lerp(from, to, t);
                mat.color = col;
                yield return null;
            }

            col.a = to;
            mat.color = col;
        }

        // =====================================================================
        // Finish
        // =====================================================================

        public void Finish()
        {
            if (_state == MarbleState.Finished) return;

            _state = MarbleState.Finished;
            _rb.linearVelocity *= 0.3f;
            _rb.angularVelocity *= 0.3f;

            OnFinished?.Invoke();
        }

        // =====================================================================
        // Cosmetic Skin
        // =====================================================================

        /// <summary>
        /// Apply a cosmetic skin material to the marble.
        /// </summary>
        public void ApplySkin(Material skin)
        {
            if (skin == null || _meshRenderer == null) return;
            _currentSkin = skin;
            _meshRenderer.material = skin;
        }

        /// <summary>
        /// Get the currently applied skin.
        /// </summary>
        public Material GetCurrentSkin() => _currentSkin;

        // =====================================================================
        // Reset
        // =====================================================================

        /// <summary>
        /// Full reset to spawn state.
        /// </summary>
        public void ResetToSpawn()
        {
            StopAllCoroutines();

            transform.position = _spawnPosition;
            transform.rotation = Quaternion.identity;
            _checkpointPosition = _spawnPosition;

            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            ResetGravity();

            _state = MarbleState.Idle;
            _hasJumped = false;
            _isGrounded = false;
            _currentSurface = SurfaceType.Normal;

            if (_meshRenderer != null)
            {
                Color c = _meshRenderer.material.color;
                c.a = 1f;
                _meshRenderer.material.color = c;
            }

            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.emitting = false;
            }
        }

        /// <summary>
        /// Freeze the marble in place (for cutscenes, menus).
        /// </summary>
        public void Freeze()
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Unfreeze the marble.
        /// </summary>
        public void Unfreeze()
        {
            _rb.isKinematic = false;
        }
    }

}
