using UnityEngine;

namespace MarbleOdyssey
{
    public class TrailEffect : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private Gradient _defaultGradient;

        private void Awake()
        {
            if (_trail == null) _trail = GetComponent<TrailRenderer>();
            if (_trail != null && _defaultGradient == null)
                _defaultGradient = _trail.colorGradient;
        }

        public void SetTrailColor(Color color)
        {
            if (_trail == null) return;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            _trail.colorGradient = gradient;
        }

        public void EnableTrail() { if (_trail != null) _trail.emitting = true; }
        public void DisableTrail() { if (_trail != null) _trail.emitting = false; }

        public void SetWidth(float width)
        {
            if (_trail == null) return;
            _trail.startWidth = width;
            _trail.endWidth = width * 0.1f;
        }

        public void ResetTrail()
        {
            if (_trail == null) return;
            _trail.Clear();
            if (_defaultGradient != null) _trail.colorGradient = _defaultGradient;
        }
    }
}
