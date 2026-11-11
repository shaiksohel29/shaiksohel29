using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MarbleOdyssey
{
    public class StarRatingDisplay : MonoBehaviour
    {
        [SerializeField] private Image[] _starImages = new Image[3];
        [SerializeField] private Sprite _starFilled;
        [SerializeField] private Sprite _starEmpty;
        [SerializeField] private Color _filledColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _emptyColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

        [Header("Animation")]
        [SerializeField] private float _fillDuration = 0.3f;
        [SerializeField] private float _bounceScale = 1.4f;

        private int _currentStars;

        public void SetStars(int count, bool animate)
        {
            count = Mathf.Clamp(count, 0, 3);
            _currentStars = count;

            if (animate)
            {
                StartCoroutine(AnimateToStars(count));
            }
            else
            {
                ApplyStarsImmediate(count);
            }
        }

        private void ApplyStarsImmediate(int count)
        {
            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] == null) continue;
                bool filled = i < count;
                _starImages[i].sprite = filled ? _starFilled : _starEmpty;
                _starImages[i].color = filled ? _filledColor : _emptyColor;
                _starImages[i].transform.localScale = Vector3.one;
            }
        }

        private IEnumerator AnimateToStars(int count)
        {
            // Reset all to empty first
            ApplyStarsImmediate(0);

            for (int i = 0; i < count && i < _starImages.Length; i++)
            {
                if (_starImages[i] == null) continue;

                yield return StartCoroutine(FillStar(i));
                yield return new WaitForSecondsRealtime(0.15f);
            }
        }

        private IEnumerator FillStar(int index)
        {
            var img = _starImages[index];
            var rt = img.transform;

            rt.localScale = Vector3.zero;
            img.sprite = _starFilled;
            img.color = _filledColor;

            float elapsed = 0f;
            while (elapsed < _fillDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _fillDuration;
                float scale = t < 0.6f
                    ? Mathf.Lerp(0, _bounceScale, t / 0.6f)
                    : Mathf.Lerp(_bounceScale, 1f, (t - 0.6f) / 0.4f);
                rt.localScale = Vector3.one * scale;

                float rotation = Mathf.Sin(t * Mathf.PI * 2f) * 15f * (1f - t);
                rt.localRotation = Quaternion.Euler(0, 0, rotation);

                yield return null;
            }

            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
}
