// ============================================================================
// Marble Odyssey 3D — Obstacle Base
// Abstract base for all level obstacles.
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// Abstract base class for all dynamic obstacles.
    /// Provides activation, timing patterns, and difficulty scaling.
    /// </summary>
    public abstract class ObstacleBase : MonoBehaviour
    {
        [Header("Obstacle Base")]
        [SerializeField] protected bool _activeOnStart = true;
        [SerializeField] protected float _activationDelay;

        [Header("Timing Pattern")]
        [Tooltip("Cycle duration in seconds. 0 = always on.")]
        [SerializeField] protected float _cycleDuration;
        [Tooltip("Fraction of cycle the obstacle is active (0-1).")]
        [SerializeField] [Range(0f, 1f)] protected float _dutyCycle = 1f;
        [Tooltip("Offset into the cycle at start (0-1).")]
        [SerializeField] [Range(0f, 1f)] protected float _phaseOffset;

        [Header("Difficulty Scaling")]
        [Tooltip("Speed multiplier applied based on difficulty setting.")]
        [SerializeField] protected float _speedMultiplier = 1f;

        protected bool _isActive;
        protected float _timer;

        protected virtual void Start()
        {
            _isActive = _activeOnStart;
            _timer = -_activationDelay + _phaseOffset * _cycleDuration;
        }

        protected virtual void Update()
        {
            if (_activationDelay > 0f && _timer < 0f)
            {
                _timer += Time.deltaTime;
                if (_timer >= 0f)
                    OnActivated();
                return;
            }

            // Cycle pattern
            if (_cycleDuration > 0f)
            {
                _timer += Time.deltaTime;
                float phase = (_timer % _cycleDuration) / _cycleDuration;
                bool shouldBeActive = phase < _dutyCycle;

                if (shouldBeActive != _isActive)
                {
                    _isActive = shouldBeActive;
                    if (_isActive) OnActivated();
                    else OnDeactivated();
                }
            }

            if (_isActive)
                ObstacleUpdate();
        }

        /// <summary>Called every frame when the obstacle is in its active phase.</summary>
        protected abstract void ObstacleUpdate();

        /// <summary>Called when the obstacle transitions to active.</summary>
        protected virtual void OnActivated() { }

        /// <summary>Called when the obstacle transitions to inactive.</summary>
        protected virtual void OnDeactivated() { }

        /// <summary>Reset obstacle to starting state (for level restart).</summary>
        public virtual void ResetObstacle()
        {
            _isActive = _activeOnStart;
            _timer = -_activationDelay + _phaseOffset * _cycleDuration;
        }
    }
}
