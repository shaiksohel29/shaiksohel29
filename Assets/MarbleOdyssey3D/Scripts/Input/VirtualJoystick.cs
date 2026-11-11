// ============================================================================
// Marble Odyssey 3D — Premium 3D Marble Rolling Game
// VirtualJoystick.cs — Dynamic UI virtual joystick component
// ============================================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarbleOdyssey
{
    /// <summary>
    /// Premium dynamic virtual joystick for touch input.
    /// Appears where the player touches in the joystick zone (left side),
    /// features a visible outer ring and inner knob with visual feedback,
    /// smooth return to centre, configurable dead zone and max radius.
    ///
    /// Attach to a UI RectTransform. Add Image components for the ring and knob
    /// as children, assigned to the corresponding fields.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        // =====================================================================
        // Inspector
        // =====================================================================

        [Header("UI References")]
        [Tooltip("The outer ring image.")]
        [SerializeField] private RectTransform _outerRing;
        [Tooltip("The inner knob image.")]
        [SerializeField] private RectTransform _innerKnob;

        [Header("Behaviour")]
        [Tooltip("Maximum radius the knob can move from the centre (in pixels).")]
        [SerializeField] private float _maxRadius = 100f;

        [Tooltip("Dead zone as a fraction of the max radius (0–1).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _deadZone = 0.1f;

        [Tooltip("Speed at which the knob returns to centre when released.")]
        [SerializeField] private float _returnSpeed = 20f;

        [Tooltip("Whether the joystick repositions to where the player touches.")]
        [SerializeField] private bool _dynamic = true;

        [Header("Visual Feedback")]
        [Tooltip("Alpha of the outer ring when not in use.")]
        [Range(0f, 1f)]
        [SerializeField] private float _idleAlpha = 0.3f;

        [Tooltip("Alpha of the outer ring when active.")]
        [Range(0f, 1f)]
        [SerializeField] private float _activeAlpha = 0.8f;

        [Tooltip("Scale pulse on the knob when active.")]
        [SerializeField] private float _knobActiveScale = 1.15f;

        // =====================================================================
        // State
        // =====================================================================

        private Canvas _canvas;
        private RectTransform _rectTransform;
        private Vector2 _inputVector;
        private bool _isActive;
        private Vector2 _centrePosition;
        private Image _outerImage;
        private Image _knobImage;

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        /// Current input vector, normalised. X = horizontal, Y = vertical.
        /// Magnitude is 0–1.
        /// </summary>
        public Vector2 InputVector => _inputVector;

        /// <summary>
        /// Whether the joystick is currently being touched.
        /// </summary>
        public bool IsActive => _isActive;

        // =====================================================================
        // Lifecycle
        // =====================================================================

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();

            if (_outerRing != null) _outerImage = _outerRing.GetComponent<Image>();
            if (_innerKnob != null) _knobImage = _innerKnob.GetComponent<Image>();

            _centrePosition = _rectTransform.anchoredPosition;

            SetVisualState(false);
        }

        private void Update()
        {
            // Smooth return when not active
            if (!_isActive && _innerKnob != null)
            {
                _innerKnob.anchoredPosition = Vector2.Lerp(
                    _innerKnob.anchoredPosition, Vector2.zero,
                    Time.deltaTime * _returnSpeed);

                if (_innerKnob.anchoredPosition.magnitude < 0.5f)
                    _innerKnob.anchoredPosition = Vector2.zero;
            }
        }

        // =====================================================================
        // Pointer Events
        // =====================================================================

        public void OnPointerDown(PointerEventData eventData)
        {
            _isActive = true;

            if (_dynamic)
            {
                // Reposition the joystick to where the player touched
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint);

                _rectTransform.anchoredPosition = localPoint;
            }

            SetVisualState(true);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_outerRing == null || _innerKnob == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);

            // Clamp to max radius
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, _maxRadius);

            // Set knob position
            _innerKnob.anchoredPosition = clamped;

            // Calculate normalised input
            float magnitude = clamped.magnitude / _maxRadius;

            // Apply dead zone
            if (magnitude < _deadZone)
            {
                _inputVector = Vector2.zero;
            }
            else
            {
                // Remap from dead zone → 1 to 0 → 1
                float remapped = (magnitude - _deadZone) / (1f - _deadZone);
                _inputVector = clamped.normalized * remapped;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isActive = false;
            _inputVector = Vector2.zero;

            SetVisualState(false);

            // If dynamic, return to default position
            if (_dynamic)
            {
                _rectTransform.anchoredPosition = _centrePosition;
            }
        }

        // =====================================================================
        // Visual Feedback
        // =====================================================================

        private void SetVisualState(bool active)
        {
            float targetAlpha = active ? _activeAlpha : _idleAlpha;

            if (_outerImage != null)
            {
                Color c = _outerImage.color;
                c.a = targetAlpha;
                _outerImage.color = c;
            }

            if (_knobImage != null)
            {
                Color c = _knobImage.color;
                c.a = active ? 1f : targetAlpha;
                _knobImage.color = c;
            }

            if (_innerKnob != null)
            {
                float scale = active ? _knobActiveScale : 1f;
                _innerKnob.localScale = Vector3.one * scale;
            }

            // Show/hide the ring when dynamic
            if (_dynamic && _outerRing != null)
            {
                _outerRing.gameObject.SetActive(active || !_dynamic);
            }
        }

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Force-reset the joystick to centre (e.g. when input is disabled).
        /// </summary>
        public void ResetInput()
        {
            _isActive = false;
            _inputVector = Vector2.zero;
            if (_innerKnob != null) _innerKnob.anchoredPosition = Vector2.zero;
            SetVisualState(false);
        }
    }
}
