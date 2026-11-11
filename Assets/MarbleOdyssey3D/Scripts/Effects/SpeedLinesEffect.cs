using UnityEngine;

namespace MarbleOdyssey
{
    public class SpeedLinesEffect : MonoBehaviour
    {
        [SerializeField] private Material _speedLinesMaterial;
        [SerializeField] private float _minSpeed = 10f;
        [SerializeField] private float _maxSpeed = 30f;
        [SerializeField] private float _maxIntensity = 1.5f;
        [SerializeField] private float _lerpSpeed = 5f;

        private Rigidbody _targetRb;
        private float _currentIntensity;
        private static readonly int IntensityID = Shader.PropertyToID("_Intensity");

        public void SetTarget(Rigidbody rb) => _targetRb = rb;

        private void Start()
        {
            if (_targetRb == null)
            {
                var marble = GameObject.FindGameObjectWithTag("Marble");
                if (marble != null) _targetRb = marble.GetComponent<Rigidbody>();
            }

            if (_speedLinesMaterial != null)
                _speedLinesMaterial.SetFloat(IntensityID, 0f);
        }

        private void LateUpdate()
        {
            if (_speedLinesMaterial == null) return;

            float targetIntensity = 0f;
            if (_targetRb != null)
            {
                float speed = _targetRb.linearVelocity.magnitude;
                float t = Mathf.InverseLerp(_minSpeed, _maxSpeed, speed);
                targetIntensity = t * _maxIntensity;
            }

            _currentIntensity = Mathf.Lerp(_currentIntensity, targetIntensity, Time.deltaTime * _lerpSpeed);
            _speedLinesMaterial.SetFloat(IntensityID, _currentIntensity);
        }

        private void OnDisable()
        {
            if (_speedLinesMaterial != null)
                _speedLinesMaterial.SetFloat(IntensityID, 0f);
        }
    }
}
