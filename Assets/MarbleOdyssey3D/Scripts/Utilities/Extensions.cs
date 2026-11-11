using UnityEngine;

namespace MarbleOdyssey
{
    public static class Extensions
    {
        public static Vector3 Flat(this Vector3 v) => new(v.x, 0f, v.z);

        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return Mathf.Lerp(toMin, toMax, t);
        }

        public static void SetAlpha(this CanvasGroup cg, float alpha)
        {
            if (cg != null) cg.alpha = alpha;
        }

        public static void DestroyChildren(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Object.Destroy(t.GetChild(i).gameObject);
        }

        public static string FormatTime(this float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int ms = Mathf.FloorToInt((seconds % 1f) * 100f);
            return $"{mins:00}:{secs:00}.{ms:00}";
        }

        public static Color WithAlpha(this Color c, float a) => new(c.r, c.g, c.b, a);
    }
}
