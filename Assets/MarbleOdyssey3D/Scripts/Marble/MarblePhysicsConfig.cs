// ============================================================================
// Marble Odyssey 3D — Premium 3D Marble Rolling Game
// MarblePhysicsConfig.cs — ScriptableObject for marble physics tuning
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// Centralized physics configuration for the marble.
    /// Create via Assets > Create > Marble Odyssey > Marble Physics Config.
    /// </summary>
    [CreateAssetMenu(fileName = "MarblePhysicsConfig", menuName = "Marble Odyssey/Marble Physics Config")]
    public class MarblePhysicsConfig : ScriptableObject
    {
        // =====================================================================
        // Core Physics
        // =====================================================================

        [Header("Core Physics")]
        [Tooltip("Rigidbody mass of the marble.")]
        [Range(0.1f, 10f)]
        public float mass = 1f;

        [Tooltip("Linear drag (air resistance).")]
        [Range(0f, 5f)]
        public float linearDrag = 0.2f;

        [Tooltip("Angular drag (rotation slowdown).")]
        [Range(0f, 5f)]
        public float angularDrag = 0.5f;

        [Tooltip("Global gravity multiplier applied to the marble.")]
        [Range(0.5f, 5f)]
        public float gravityMultiplier = 1.5f;

        // =====================================================================
        // Movement
        // =====================================================================

        [Header("Movement")]
        [Tooltip("Base movement force applied via torque.")]
        [Range(1f, 100f)]
        public float moveForce = 35f;

        [Tooltip("Maximum linear speed the marble can reach under its own power.")]
        [Range(5f, 80f)]
        public float maxSpeed = 25f;

        [Tooltip("How quickly the marble reaches full force. Evaluated by normalized speed.")]
        public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

        [Tooltip("Extra downward force applied while grounded to keep the marble planted.")]
        [Range(0f, 20f)]
        public float groundStickForce = 5f;

        [Tooltip("Multiplier applied to movement force while airborne (reduced air control).")]
        [Range(0f, 1f)]
        public float airControlMultiplier = 0.35f;

        // =====================================================================
        // Jump
        // =====================================================================

        [Header("Jump")]
        [Tooltip("Upward impulse when the player jumps.")]
        [Range(1f, 30f)]
        public float jumpForce = 12f;

        [Tooltip("Short-hop multiplier applied when the jump button is released early.")]
        [Range(0.1f, 1f)]
        public float jumpCutMultiplier = 0.4f;

        [Tooltip("Seconds after leaving a surface where a jump is still allowed (coyote time).")]
        [Range(0f, 0.3f)]
        public float coyoteTime = 0.12f;

        [Tooltip("Seconds before landing during which a jump input is buffered.")]
        [Range(0f, 0.3f)]
        public float jumpBufferTime = 0.15f;

        [Tooltip("Extra downward force when falling (snappier feel).")]
        [Range(0f, 10f)]
        public float fallGravityBoost = 3f;

        [Tooltip("Cooldown before the marble can jump again after landing.")]
        [Range(0f, 0.5f)]
        public float jumpCooldown = 0.08f;

        // =====================================================================
        // Ground Detection
        // =====================================================================

        [Header("Ground Detection")]
        [Tooltip("Radius of the SphereCast used for ground checks.")]
        [Range(0.05f, 1f)]
        public float groundCheckRadius = 0.45f;

        [Tooltip("Distance below the marble centre to cast for ground.")]
        [Range(0.01f, 0.5f)]
        public float groundCheckDistance = 0.15f;

        [Tooltip("Layers treated as ground.")]
        public LayerMask groundLayers = ~0;

        // =====================================================================
        // Surface Materials
        // =====================================================================

        [Header("Surface — Normal")]
        [Range(0f, 1f)] public float normalFriction = 0.6f;
        [Range(0f, 1f)] public float normalBounciness = 0.3f;

        [Header("Surface — Ice")]
        [Range(0f, 1f)] public float iceFriction = 0.05f;
        [Range(0f, 1f)] public float iceBounciness = 0.1f;
        [Tooltip("Movement force multiplier on ice.")]
        [Range(0.1f, 2f)] public float iceForceMultiplier = 0.4f;

        [Header("Surface — Mud")]
        [Range(0f, 1f)] public float mudFriction = 0.9f;
        [Range(0f, 1f)] public float mudBounciness = 0.05f;
        [Tooltip("Speed cap multiplier when on mud.")]
        [Range(0.1f, 1f)] public float mudSpeedMultiplier = 0.45f;

        [Header("Surface — Bounce Pad")]
        [Tooltip("Vertical impulse from a bounce pad.")]
        [Range(5f, 50f)] public float bouncePadForce = 20f;

        // =====================================================================
        // Speed Boost
        // =====================================================================

        [Header("Speed Boost")]
        [Tooltip("Force applied by a speed-boost pad.")]
        [Range(5f, 80f)]
        public float boostForce = 40f;

        [Tooltip("Duration of the speed-boost effect in seconds.")]
        [Range(0.1f, 5f)]
        public float boostDuration = 1.2f;

        [Tooltip("Max speed while boosting.")]
        [Range(10f, 120f)]
        public float boostMaxSpeed = 45f;

        // =====================================================================
        // Visual / Haptic
        // =====================================================================

        [Header("Trail / Visual")]
        [Tooltip("Speed threshold above which the trail effect activates.")]
        [Range(1f, 40f)]
        public float trailSpeedThreshold = 12f;

        [Header("Haptics")]
        [Tooltip("Enable vibration feedback on death and big impacts.")]
        public bool enableHaptics = true;

        // =====================================================================
        // Respawn
        // =====================================================================

        [Header("Respawn")]
        [Tooltip("Time in seconds for the death-fade-out.")]
        [Range(0.1f, 2f)]
        public float deathFadeDuration = 0.6f;

        [Tooltip("Time before the marble reappears at the checkpoint.")]
        [Range(0.1f, 2f)]
        public float respawnDelay = 0.8f;

        [Tooltip("Time for the respawn-fade-in.")]
        [Range(0.1f, 2f)]
        public float respawnFadeDuration = 0.5f;
    }
}
