using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MarbleOdyssey
{
    public class CountdownOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Text _countdownText;
        [SerializeField] private RectTransform _textTransform;

        [Header("Settings")]
        [SerializeField] private int _countFrom = 3;
        [SerializeField] private float _digitDuration = 0.8f;
        [SerializeField] private string _goText = "GO!";
        [SerializeField] private Color _numberColor = Color.white;
        [SerializeField] private Color _goColor = new Color(0.2f, 1f, 0.4f);

        public void StartCountdown(Action onComplete)
        {
            gameObject.SetActive(true);
            StartCoroutine(CountdownSequence(onComplete));
        }

        private IEnumerator CountdownSequence(Action onComplete)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            for (int i = _countFrom; i >= 1; i--)
            {
                if (_countdownText != null)
                {
                    _countdownText.text = i.ToString();
                    _countdownText.color = _numberColor;
                }

                yield return StartCoroutine(AnimateDigit());
                AudioManager.Instance?.PlaySFX("countdown_tick");
            }

            // GO!
            if (_countdownText != null)
            {
                _countdownText.text = _goText;
                _countdownText.color = _goColor;
            }
            AudioManager.Instance?.PlaySFX("countdown_go");
            yield return StartCoroutine(AnimateDigit());

            // Fade out
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null)
                    _canvasGroup.alpha = 1f - (elapsed / 0.3f);
                yield return null;
            }

            gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        private IEnumerator AnimateDigit()
        {
            if (_textTransform == null) { yield return new WaitForSecondsRealtime(_digitDuration); yield break; }

            _textTransform.localScale = Vector3.one * 2f;
            float elapsed = 0f;

            while (elapsed < _digitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _digitDuration;
                float scale = Mathf.Lerp(2f, 0.8f, t * t);
                float alpha = t < 0.8f ? 1f : 1f - ((t - 0.8f) / 0.2f);

                _textTransform.localScale = Vector3.one * scale;
                if (_countdownText != null)
                {
                    var c = _countdownText.color;
                    c.a = alpha;
                    _countdownText.color = c;
                }
                yield return null;
            }
        }
    }
}
