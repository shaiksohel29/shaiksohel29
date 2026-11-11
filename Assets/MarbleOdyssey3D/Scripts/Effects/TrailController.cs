using UnityEngine;

namespace MarbleOdyssey
{
    public class TrailController : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private ParticleSystem _speedParticles;

        [Header("Speed Thresholds")]
        [SerializeField] private float _trailActivationSpeed = 5f;
        [SerializeField] private float _maxTrailSpeed = 20f;
        [SerializeField] private float _particleActivationSpeed = 12f;

        [Header("Trail Appearance")]
        [SerializeField] private float _minWidth = 0.05f;
        [SerializeField] private float _maxWidth = 0.3f;
        [SerializeField] private Gradient _speedGradient;

        private Rigidbody _rb;
        private ParticleSystem.EmissionModule _emission;
        private bool _hasParticles;

        private void Start()
        {
            _rb = GetComponentInParent<Rigidbody>();

            if (_trail != null)
            {
                _trail.emitting = false;
                _trail.widthMultiplier = _minWidth;
            }

            if (_speedParticles != null)
            {
                _emission = _speedParticles.emission;
                _emission.enabled = false;
                _hasParticles = true;
            }
        }

        private void Update()
        {
            if (_rb == null) return;

            float speed = _rb.linearVelocity.magnitude;
            float normalizedSpeed = Mathf.InverseLerp(_trailActivationSpeed, _maxTrailSpeed, speed);

            // Trail
            if (_trail != null)
            {
                bool showTrail = speed >= _trailActivationSpeed;
                _trail.emitting = showTrail;

                if (showTrail)
                {
                    _trail.widthMultiplier = Mathf.Lerp(_minWidth, _maxWidth, normalizedSpeed);
                    if (_speedGradient != null)
                    {
                        var color = _speedGradient.Evaluate(normalizedSpeed);
                        _trail.startColor = color;
                        _trail.endColor = new Color(color.r, color.g, color.b, 0f);
                    }
                }
            }

            // Speed particles
            if (_hasParticles)
            {
                bool showParticles = speed >= _particleActivationSpeed;
                _emission.enabled = showParticles;

                if (showParticles)
                {
                    var rate = _emission.rateOverTime;
                    rate.constant = Mathf.Lerp(5f, 30f, normalizedSpeed);
                    _emission.rateOverTime = rate;
                }
            }
        }

        public void SetTrailColor(Color color)
        {
            if (_trail != null)
            {
                _trail.startColor = color;
                _trail.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }

        public void ClearTrail()
        {
            if (_trail != null) _trail.Clear();
        }
    }
}
