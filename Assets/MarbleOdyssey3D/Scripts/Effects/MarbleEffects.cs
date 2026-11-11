using UnityEngine;

namespace MarbleOdyssey
{
    [RequireComponent(typeof(MarbleController))]
    public class MarbleEffects : MonoBehaviour
    {
        [Header("Trail")]
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private float _trailMinSpeed = 3f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem _rollDust;
        [SerializeField] private ParticleSystem _jumpBurst;
        [SerializeField] private ParticleSystem _landBurst;
        [SerializeField] private ParticleSystem _deathExplosion;
        [SerializeField] private ParticleSystem _respawnGlow;
        [SerializeField] private ParticleSystem _speedLines;
        [SerializeField] private ParticleSystem _gemPickup;

        [Header("Squash & Stretch")]
        [SerializeField] private Transform _meshTransform;
        [SerializeField] private float _squashAmount = 0.15f;
        [SerializeField] private float _stretchAmount = 0.1f;
        [SerializeField] private float _squashRecoverySpeed = 8f;

        private MarbleController _marble;
        private Vector3 _defaultScale;
        private Vector3 _targetScale;
        private bool _wasGrounded;
        private bool _wasAlive = true;

        private void Awake()
        {
            _marble = GetComponent<MarbleController>();
            if (_meshTransform != null)
                _defaultScale = _meshTransform.localScale;
            _targetScale = _defaultScale;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<MarbleDiedEvent>(OnDied);
            EventBus.Subscribe<MarbleRespawnedEvent>(OnRespawned);
            EventBus.Subscribe<GemCollectedEvent>(OnGemCollected);
            EventBus.Subscribe<SpeedBoostEvent>(OnSpeedBoost);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MarbleDiedEvent>(OnDied);
            EventBus.Unsubscribe<MarbleRespawnedEvent>(OnRespawned);
            EventBus.Unsubscribe<GemCollectedEvent>(OnGemCollected);
            EventBus.Unsubscribe<SpeedBoostEvent>(OnSpeedBoost);
        }

        private void Update()
        {
            if (_marble == null) return;

            float speed = _marble.CurrentSpeed;
            bool grounded = _marble.IsGrounded;

            UpdateTrail(speed);
            UpdateRollDust(speed, grounded);
            UpdateSpeedLines(speed);
            DetectLanding(grounded);
            DetectAirborne(grounded);
            UpdateSquashStretch();

            _wasGrounded = grounded;
        }

        private void UpdateTrail(float speed)
        {
            if (_trail == null) return;
            _trail.emitting = speed > _trailMinSpeed;
        }

        private void UpdateRollDust(float speed, bool grounded)
        {
            if (_rollDust == null) return;
            var emission = _rollDust.emission;
            emission.rateOverTime = grounded && speed > 1f ? speed * 3f : 0f;
            if (grounded && speed > 1f && !_rollDust.isPlaying)
                _rollDust.Play();
            else if ((!grounded || speed <= 1f) && _rollDust.isPlaying)
                _rollDust.Stop();
        }

        private void UpdateSpeedLines(float speed)
        {
            if (_speedLines == null) return;
            float threshold = GameConstants.Physics.MaxSpeed * 0.7f;
            var emission = _speedLines.emission;
            if (speed > threshold)
            {
                emission.rateOverTime = (speed - threshold) * 5f;
                if (!_speedLines.isPlaying) _speedLines.Play();
            }
            else
            {
                emission.rateOverTime = 0f;
                if (_speedLines.isPlaying) _speedLines.Stop();
            }
        }

        private void DetectLanding(bool grounded)
        {
            if (grounded && !_wasGrounded)
            {
                if (_landBurst != null) _landBurst.Play();
                _targetScale = new Vector3(
                    _defaultScale.x * (1f + _squashAmount),
                    _defaultScale.y * (1f - _squashAmount),
                    _defaultScale.z * (1f + _squashAmount));
            }
        }

        private void DetectAirborne(bool grounded)
        {
            if (!grounded && _wasGrounded)
            {
                if (_jumpBurst != null) _jumpBurst.Play();
                _targetScale = new Vector3(
                    _defaultScale.x * (1f - _stretchAmount),
                    _defaultScale.y * (1f + _stretchAmount),
                    _defaultScale.z * (1f - _stretchAmount));
            }
        }

        private void UpdateSquashStretch()
        {
            if (_meshTransform == null) return;
            _targetScale = Vector3.Lerp(_targetScale, _defaultScale, Time.deltaTime * _squashRecoverySpeed);
            _meshTransform.localScale = Vector3.Lerp(_meshTransform.localScale, _targetScale, Time.deltaTime * _squashRecoverySpeed);
        }

        private void OnDied(MarbleDiedEvent evt)
        {
            if (_deathExplosion != null)
            {
                _deathExplosion.transform.position = evt.DeathPosition;
                _deathExplosion.Play();
            }
            if (_trail != null) _trail.Clear();
            _wasAlive = false;
        }

        private void OnRespawned(MarbleRespawnedEvent evt)
        {
            if (_respawnGlow != null)
            {
                _respawnGlow.transform.position = evt.SpawnPosition;
                _respawnGlow.Play();
            }
            if (_trail != null) _trail.Clear();
            _wasAlive = true;
        }

        private void OnGemCollected(GemCollectedEvent evt)
        {
            if (_gemPickup != null)
            {
                _gemPickup.transform.position = transform.position;
                _gemPickup.Play();
            }
        }

        private void OnSpeedBoost(SpeedBoostEvent evt)
        {
            if (_speedLines != null) _speedLines.Play();
        }
    }
}
