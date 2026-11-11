using UnityEngine;

namespace MarbleOdyssey
{
    public class LaserObstacle : ObstacleBase
    {
        [Header("Laser")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private float _beamWidth = 0.15f;
        [SerializeField] private float _warningDuration = 0.5f;
        [SerializeField] private LayerMask _hitMask = ~0;

        [Header("Visual")]
        [SerializeField] private Color _activeColor = Color.red;
        [SerializeField] private Color _warningColor = new Color(1f, 0.3f, 0f, 0.4f);

        private float _activationTimer;
        private bool _isWarning;

        protected override void Start()
        {
            base.Start();
            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = 2;
                _lineRenderer.startWidth = _beamWidth;
                _lineRenderer.endWidth = _beamWidth;
                _lineRenderer.enabled = false;
            }
        }

        protected override void OnActivated()
        {
            _activationTimer = 0f;
            _isWarning = true;
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = true;
                _lineRenderer.startColor = _warningColor;
                _lineRenderer.endColor = _warningColor;
                _lineRenderer.startWidth = _beamWidth * 0.3f;
                _lineRenderer.endWidth = _beamWidth * 0.3f;
            }
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.SFXLib?.LaserWarning);
        }

        protected override void OnDeactivated()
        {
            _isWarning = false;
            if (_lineRenderer != null) _lineRenderer.enabled = false;
        }

        protected override void ObstacleUpdate()
        {
            if (_startPoint == null || _endPoint == null) return;

            if (_isWarning)
            {
                _activationTimer += Time.deltaTime;
                if (_activationTimer >= _warningDuration)
                {
                    _isWarning = false;
                    ActivateBeam();
                }
                else
                {
                    float flash = Mathf.PingPong(_activationTimer * 10f, 1f);
                    if (_lineRenderer != null)
                    {
                        Color c = Color.Lerp(_warningColor, _activeColor, flash);
                        _lineRenderer.startColor = c;
                        _lineRenderer.endColor = c;
                    }
                }
            }

            UpdateBeamPositions();

            if (!_isWarning)
                CheckHit();
        }

        private void ActivateBeam()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.startColor = _activeColor;
                _lineRenderer.endColor = _activeColor;
                _lineRenderer.startWidth = _beamWidth;
                _lineRenderer.endWidth = _beamWidth;
            }
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.SFXLib?.LaserFire);
        }

        private void UpdateBeamPositions()
        {
            if (_lineRenderer == null) return;
            _lineRenderer.SetPosition(0, _startPoint.position);
            _lineRenderer.SetPosition(1, _endPoint.position);
        }

        private void CheckHit()
        {
            Vector3 dir = _endPoint.position - _startPoint.position;
            float dist = dir.magnitude;

            if (Physics.SphereCast(_startPoint.position, _beamWidth * 0.5f, dir.normalized, out var hit, dist, _hitMask))
            {
                if (hit.collider.CompareTag("Marble"))
                {
                    ParticleManager.Instance?.SpawnEffect(EffectType.LaserHit, hit.point);
                    GameManager.Instance?.CompleteLevelFailed();
                }
            }
        }

        public override void ResetObstacle()
        {
            base.ResetObstacle();
            _isWarning = false;
            if (_lineRenderer != null) _lineRenderer.enabled = false;
        }
    }
}
