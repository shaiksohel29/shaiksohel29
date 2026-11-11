using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MarbleOdyssey
{
    public static class UIAnimator
    {
        public static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, bool useUnscaledTime = true)
        {
            cg.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            cg.alpha = to;
        }

        public static IEnumerator SlideIn(RectTransform rt, Vector2 from, Vector2 to, float duration, bool elastic = false)
        {
            rt.anchoredPosition = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float ease = elastic ? ElasticOut(t) : SmoothStep(t);
                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, ease);
                yield return null;
            }
            rt.anchoredPosition = to;
        }

        public static IEnumerator ScalePunch(Transform t, float punchScale, float duration)
        {
            Vector3 original = t.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float s = 1f + (punchScale - 1f) * Mathf.Sin(progress * Mathf.PI);
                t.localScale = original * s;
                yield return null;
            }
            t.localScale = original;
        }

        public static IEnumerator ColorLerp(Graphic g, Color from, Color to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                g.color = Color.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            g.color = to;
        }

        public static IEnumerator TypewriterText(Text text, string content, float charDelay = 0.03f)
        {
            text.text = "";
            for (int i = 0; i < content.Length; i++)
            {
                text.text += content[i];
                yield return new WaitForSecondsRealtime(charDelay);
            }
        }

        public static IEnumerator ShakeTransform(Transform t, float intensity, float duration)
        {
            Vector3 origin = t.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float decay = 1f - (elapsed / duration);
                float x = UnityEngine.Random.Range(-1f, 1f) * intensity * decay;
                float y = UnityEngine.Random.Range(-1f, 1f) * intensity * decay;
                t.localPosition = origin + new Vector3(x, y, 0);
                yield return null;
            }
            t.localPosition = origin;
        }

        public static IEnumerator DelayedAction(float delay, Action action, bool useUnscaledTime = true)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(delay);
            else
                yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        // Easing functions
        private static float SmoothStep(float t) => t * t * (3f - 2f * t);

        private static float ElasticOut(float t)
        {
            return Mathf.Sin(-13f * (t + 1f) * Mathf.PI * 0.5f) * Mathf.Pow(2f, -10f * t) + 1f;
        }
    }
}
