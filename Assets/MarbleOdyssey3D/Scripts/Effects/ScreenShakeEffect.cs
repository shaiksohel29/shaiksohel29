using UnityEngine;

namespace MarbleOdyssey
{
    public static class ScreenShakeEffect
    {
        public static void ShakeOnDeath()
        {
            var shake = Object.FindFirstObjectByType<CameraShake>();
            if (shake != null) shake.Shake(0.4f, 0.5f);
        }

        public static void ShakeOnLand(float impactForce)
        {
            float magnitude = Mathf.Clamp(impactForce * 0.02f, 0.05f, 0.3f);
            var shake = Object.FindFirstObjectByType<CameraShake>();
            if (shake != null) shake.Shake(magnitude, 0.2f);
        }

        public static void ShakeOnBoost()
        {
            var shake = Object.FindFirstObjectByType<CameraShake>();
            if (shake != null) shake.Shake(0.15f, 0.3f);
        }

        public static void ShakeOnBumper()
        {
            var shake = Object.FindFirstObjectByType<CameraShake>();
            if (shake != null) shake.Shake(0.2f, 0.15f);
        }
    }
}
