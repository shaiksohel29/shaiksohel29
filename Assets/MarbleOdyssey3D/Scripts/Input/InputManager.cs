// ============================================================================
// Marble Odyssey 3D — Premium 3D Marble Rolling Game
// InputManager.cs — Touch input system for Android
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// Manages all player input: virtual joystick movement, camera orbit,
    /// jump, pinch zoom, and optional gyroscope tilt controls.
    /// Optimised for landscape Android gameplay.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // =====================================================================
        // Singleton
        // =====================================================================

        private static InputManager _instance;
        public static InputManager Instance => _instance;

        // =====================================================================
        // References
        // =====================================================================

        [Header("References")]
        [SerializeField] private MarbleController _marble;
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private VirtualJoystick _joystick;

        // =====================================================================
        // Settings
        // =====================================================================

        [Header("Camera Orbit")]
        [Tooltip("Camera swipe sensitivity.")]
        [SerializeField] private float _cameraSensitivity = 0.3f;

        [Header("Zones")]
        [Tooltip("Fraction of screen width used for the joystick zone (left side).")]
        [Range(0.2f, 0.6f)]
        [SerializeField] private float _joystickZoneWidth = 0.4f;

        [Header("Jump")]
        [Tooltip("Screen region for the jump button (bottom-right fraction of screen).")]
        [SerializeField] private Rect _jumpButtonRect = new Rect(0.75f, 0.6f, 0.25f, 0.4f);

        [Header("Gyroscope")]
        [SerializeField] private bool _enableGyroscope = false;
        [SerializeField] private float _gyroSensitivity = 2f;
        [SerializeField] private float _gyroDeadZone = 0.05f;

        [Header("Pinch Zoom")]
        [SerializeField] private float _pinchZoomSpeed = 0.01f;

        [Header("Dead Zones")]
        [SerializeField] private float _cameraSwipeDeadZone = 5f;

        // =====================================================================
        // State
        // =====================================================================

        // Touch tracking
        private int _joystickTouchId = -1;
        private int _cameraTouchId = -1;
        private int _jumpTouchId = -1;

        private Vector2 _cameraSwipeStart;
        private Vector2 _cameraSwipePrev;
        private bool _cameraSwipeActive;

        // Pinch
        private float _lastPinchDistance;
        private bool _isPinching;

        // Gyro
        private Gyroscope _gyro;

        // Editor fallback
        private bool _editorJumpHeld;

        // =====================================================================
        // Properties
        // =====================================================================

        public bool GyroscopeEnabled
        {
            get => _enableGyroscope;
            set
            {
                _enableGyroscope = value;
                if (value) EnableGyroscope();
            }
        }

        public float CameraSensitivity
        {
            get => _cameraSensitivity;
            set => _cameraSensitivity = value;
        }

        public float GyroSensitivity
        {
            get => _gyroSensitivity;
            set => _gyroSensitivity = value;
        }

        // =====================================================================
        // Lifecycle
        // =====================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (_enableGyroscope) EnableGyroscope();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (_marble == null) return;

            // Decide input source
#if UNITY_EDITOR
            ProcessEditorInput();
#else
            ProcessTouchInput();
            if (_enableGyroscope) ProcessGyroInput();
#endif
        }

        // =====================================================================
        // Touch Input Processing
        // =====================================================================

        private void ProcessTouchInput()
        {
            int touchCount = Input.touchCount;

            // Detect pinch zoom (two fingers)
            if (touchCount >= 2)
            {
                ProcessPinchZoom();
            }
            else
            {
                _isPinching = false;
            }

            for (int i = 0; i < touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        HandleTouchBegan(touch);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        HandleTouchMoved(touch);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        HandleTouchEnded(touch);
                        break;
                }
            }

            // Apply joystick input
            if (_joystick != null)
            {
                _marble.SetMoveInput(_joystick.InputVector);
            }
        }

        private void HandleTouchBegan(Touch touch)
        {
            Vector2 pos = touch.position;
            float screenW = Screen.width;
            float screenH = Screen.height;

            // Check jump button region first (bottom-right)
            Rect jumpPixels = new Rect(
                _jumpButtonRect.x * screenW, _jumpButtonRect.y * screenH,
                _jumpButtonRect.width * screenW, _jumpButtonRect.height * screenH);

            if (jumpPixels.Contains(pos) && _jumpTouchId < 0)
            {
                _jumpTouchId = touch.fingerId;
                _marble.SetJumpPressed();
                return;
            }

            // Left side = joystick (handled by VirtualJoystick UI component)
            if (pos.x < screenW * _joystickZoneWidth)
            {
                _joystickTouchId = touch.fingerId;
                return;
            }

            // Right side = camera orbit
            if (_cameraTouchId < 0)
            {
                _cameraTouchId = touch.fingerId;
                _cameraSwipeStart = pos;
                _cameraSwipePrev = pos;
                _cameraSwipeActive = false;
            }
        }

        private void HandleTouchMoved(Touch touch)
        {
            // Camera orbit
            if (touch.fingerId == _cameraTouchId && _cameraController != null)
            {
                Vector2 delta = touch.position - _cameraSwipePrev;

                if (!_cameraSwipeActive)
                {
                    // Require minimum drag to avoid accidental swipes
                    if (Vector2.Distance(touch.position, _cameraSwipeStart) > _cameraSwipeDeadZone)
                        _cameraSwipeActive = true;
                    else
                        return;
                }

                _cameraController.ApplyOrbitDelta(delta * _cameraSensitivity);
                _cameraSwipePrev = touch.position;
            }
        }

        private void HandleTouchEnded(Touch touch)
        {
            if (touch.fingerId == _joystickTouchId)
            {
                _joystickTouchId = -1;
            }
            else if (touch.fingerId == _cameraTouchId)
            {
                _cameraTouchId = -1;
                _cameraSwipeActive = false;
            }
            else if (touch.fingerId == _jumpTouchId)
            {
                _jumpTouchId = -1;
                _marble.SetJumpReleased();
            }
        }

        // =====================================================================
        // Pinch Zoom
        // =====================================================================

        private void ProcessPinchZoom()
        {
            if (_cameraController == null) return;
            if (Input.touchCount < 2) return;

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float currentDist = Vector2.Distance(t0.position, t1.position);

            if (!_isPinching)
            {
                _isPinching = true;
                _lastPinchDistance = currentDist;
                return;
            }

            float delta = currentDist - _lastPinchDistance;
            _cameraController.ApplyZoom(delta * _pinchZoomSpeed);
            _lastPinchDistance = currentDist;
        }

        // =====================================================================
        // Gyroscope
        // =====================================================================

        private void EnableGyroscope()
        {
            if (SystemInfo.supportsGyroscope)
            {
                _gyro = Input.gyro;
                _gyro.enabled = true;
                Debug.Log("[Input] Gyroscope enabled.");
            }
            else
            {
                Debug.LogWarning("[Input] Device does not support gyroscope.");
                _enableGyroscope = false;
            }
        }

        private void ProcessGyroInput()
        {
            if (_gyro == null || !_gyro.enabled) return;

            // Use gravity vector for tilt
            Vector3 gravity = _gyro.gravity;

            // In landscape, gravity.x = left/right tilt, gravity.y = forward/back tilt
            float tiltX = gravity.x;
            float tiltY = gravity.y;

            // Apply dead zone
            if (Mathf.Abs(tiltX) < _gyroDeadZone) tiltX = 0f;
            if (Mathf.Abs(tiltY) < _gyroDeadZone) tiltY = 0f;

            // Blend gyro input with joystick input
            Vector2 gyroInput = new Vector2(tiltX, -tiltY) * _gyroSensitivity;
            Vector2 combined = _joystick != null
                ? _joystick.InputVector + gyroInput
                : gyroInput;

            // Clamp combined magnitude to 1
            if (combined.magnitude > 1f) combined.Normalize();

            _marble.SetMoveInput(combined);
        }

        // =====================================================================
        // Editor Input (Keyboard + Mouse fallback)
        // =====================================================================

        private void ProcessEditorInput()
        {
            // WASD / Arrow movement
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 move = new Vector2(h, v);
            if (move.magnitude > 1f) move.Normalize();
            _marble.SetMoveInput(move);

            // Space = jump
            if (Input.GetKeyDown(KeyCode.Space))
                _marble.SetJumpPressed();
            if (Input.GetKeyUp(KeyCode.Space))
                _marble.SetJumpReleased();

            // Right mouse drag = camera orbit
            if (Input.GetMouseButton(1) && _cameraController != null)
            {
                Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                _cameraController.ApplyOrbitDelta(mouseDelta * _cameraSensitivity * 5f);
            }

            // Scroll wheel = zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f && _cameraController != null)
            {
                _cameraController.ApplyZoom(scroll * 50f);
            }
        }

        // =====================================================================
        // Public Helpers
        // =====================================================================

        /// <summary>
        /// Enable or disable all input processing.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                _marble?.SetMoveInput(Vector2.zero);
                if (_joystick != null) _joystick.ResetInput();
            }
        }
    }
}
