using UnityEngine;

namespace MarbleOdyssey
{
    public static class MathUtils
    {
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return Mathf.Lerp(toMin, toMax, t);
        }

        public static float RemapUnclamped(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = (value - fromMin) / (fromMax - fromMin);
            return toMin + (toMax - toMin) * t;
        }

        public static float AngleDistance(float a, float b)
        {
            float delta = Mathf.Repeat(b - a + 180f, 360f) - 180f;
            return Mathf.Abs(delta);
        }

        public static Vector2 RandomPointOnCircle(float radius)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        public static Vector2 RandomPointInAnnulus(float innerRadius, float outerRadius)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(Random.Range(innerRadius * innerRadius, outerRadius * outerRadius));
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }

        public static float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }

        public static float EaseOutBounce(float t)
        {
            if (t < 1f / 2.75f)
                return 7.5625f * t * t;
            if (t < 2f / 2.75f)
            {
                t -= 1.5f / 2.75f;
                return 7.5625f * t * t + 0.75f;
            }
            if (t < 2.5f / 2.75f)
            {
                t -= 2.25f / 2.75f;
                return 7.5625f * t * t + 0.9375f;
            }
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int mins = (int)(seconds / 60f);
            float secs = seconds % 60f;
            return $"{mins:00}:{secs:05.2f}";
        }

        public static string FormatTimeShort(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int mins = (int)(seconds / 60f);
            int secs = (int)(seconds % 60f);
            return $"{mins}:{secs:00}";
        }
    }
}
