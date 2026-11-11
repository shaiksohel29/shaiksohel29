using UnityEngine;

namespace MarbleOdyssey
{
    public class WindZone : ObstacleBase
    {
        [Header("Wind")]
        [SerializeField] private Vector3 _windDirection = Vector3.right;
        [SerializeField] private float _windForce = 15f;
        [SerializeField] private bool _isConstant = true;
        [SerializeField] private float _gustInterval = 2f;
        [SerializeField] private float _gustDuration = 0.5f;

        [Header("Visuals")]
        [SerializeField] private ParticleSystem _windParticles;

        private float _gustTimer;
        private bool _gusting;

        protected override void ObstacleUpdate()
        {
            if (_isConstant) return;

            _gustTimer += Time.deltaTime;
            if (!_gusting && _gustTimer >= _gustInterval)
            {
                _gusting = true;
                _gustTimer = 0f;
                if (_windParticles != null) _windParticles.Play();
            }
            else if (_gusting && _gustTimer >= _gustDuration)
            {
                _gusting = false;
                _gustTimer = 0f;
                if (_windParticles != null) _windParticles.Stop();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_isActive || !other.CompareTag("Player")) return;
            if (!_isConstant && !_gusting) return;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(_windDirection.normalized * _windForce * _speedMultiplier, ForceMode.Force);
        }

        public override void ResetObstacle()
        {
            base.ResetObstacle();
            _gustTimer = 0f;
            _gusting = false;
        }
    }
}
