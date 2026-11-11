using UnityEngine;

namespace MarbleOdyssey
{
    [RequireComponent(typeof(TrailRenderer))]
    public class MarbleTrail : MonoBehaviour
    {
        [SerializeField] private float _activationSpeed = 5f;
        [SerializeField] private float _maxSpeed = 30f;
        [SerializeField] private float _minWidth = 0.05f;
        [SerializeField] private float _maxWidth = 0.4f;

        [SerializeField] private Color _slowColor = Color.white;
        [SerializeField] private Color _fastColor = new Color(0f, 0.8f, 1f);
        [SerializeField] private Color _veryFastColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private float _veryFastThreshold = 20f;

        private TrailRenderer _trail;
        private Rigidbody _rb;
        private Gradient _gradient;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
            _rb = GetComponentInParent<Rigidbody>();
            _gradient = new Gradient();
        }

        private void LateUpdate()
        {
            if (_rb == null) return;

            float speed = _rb.linearVelocity.magnitude;
            bool active = speed > _activationSpeed;
            _trail.emitting = active;

            if (!active) return;

            float t = Mathf.InverseLerp(_activationSpeed, _maxSpeed, speed);
            _trail.startWidth = Mathf.Lerp(_minWidth, _maxWidth, t);
            _trail.endWidth = _trail.startWidth * 0.2f;

            Color currentColor = speed > _veryFastThreshold
                ? Color.Lerp(_fastColor, _veryFastColor, Mathf.InverseLerp(_veryFastThreshold, _maxSpeed, speed))
                : Color.Lerp(_slowColor, _fastColor, t);

            var colorKeys = new GradientColorKey[]
            {
                new(currentColor, 0f),
                new(currentColor * 0.5f, 1f)
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new(0.8f, 0f),
                new(0f, 1f)
            };
            _gradient.SetKeys(colorKeys, alphaKeys);
            _trail.colorGradient = _gradient;
        }
    }
}
