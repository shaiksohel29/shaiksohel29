using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MarbleOdyssey
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Timer")]
        [SerializeField] private Text _timerText;
        [SerializeField] private Image _timerIcon;

        [Header("Gems")]
        [SerializeField] private Text _gemCountText;
        [SerializeField] private RectTransform _gemIcon;

        [Header("Level Info")]
        [SerializeField] private Text _levelNameText;
        [SerializeField] private Text _worldNameText;

        [Header("Controls")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _restartButton;

        [Header("Checkpoint")]
        [SerializeField] private CanvasGroup _checkpointNotification;
        [SerializeField] private Text _checkpointText;

        [Header("Speed Indicator")]
        [SerializeField] private Image _speedFill;
        [SerializeField] private Gradient _speedGradient;

        private float _elapsedTime;
        private int _gemCount;
        private bool _isRunning;
        private Coroutine _checkpointRoutine;

        public float ElapsedTime => _elapsedTime;
        public int GemCount => _gemCount;

        private void Start()
        {
            _pauseButton?.onClick.AddListener(() =>
            {
                PauseMenuController.Instance?.TogglePause();
            });

            _restartButton?.onClick.AddListener(() =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });

            if (_checkpointNotification != null)
                _checkpointNotification.alpha = 0f;
        }

        private void Update()
        {
            if (!_isRunning) return;

            _elapsedTime += Time.deltaTime;

            if (_timerText != null)
            {
                int min = (int)(_elapsedTime / 60);
                float sec = _elapsedTime % 60;
                _timerText.text = $"{min:D2}:{sec:05.2f}";
            }
        }

        public void StartTimer() => _isRunning = true;
        public void StopTimer() => _isRunning = false;

        public void ResetTimer()
        {
            _elapsedTime = 0f;
            if (_timerText != null) _timerText.text = "00:00.00";
        }

        public void SetLevelInfo(string worldName, int levelNumber)
        {
            if (_worldNameText != null) _worldNameText.text = worldName;
            if (_levelNameText != null) _levelNameText.text = $"Level {levelNumber}";
        }

        public void AddGem(int amount = 1)
        {
            _gemCount += amount;
            if (_gemCountText != null) _gemCountText.text = $"×{_gemCount}";

            if (_gemIcon != null)
                StartCoroutine(PunchScale(_gemIcon, 1.3f, 0.2f));

            AudioManager.Instance?.PlaySFX("gem_collect");
        }

        public void UpdateSpeedIndicator(float normalizedSpeed)
        {
            if (_speedFill != null)
            {
                _speedFill.fillAmount = normalizedSpeed;
                if (_speedGradient != null)
                    _speedFill.color = _speedGradient.Evaluate(normalizedSpeed);
            }
        }

        public void ShowCheckpointReached()
        {
            if (_checkpointRoutine != null) StopCoroutine(_checkpointRoutine);
            _checkpointRoutine = StartCoroutine(CheckpointAnimation());
        }

        private IEnumerator CheckpointAnimation()
        {
            if (_checkpointNotification == null) yield break;
            if (_checkpointText != null) _checkpointText.text = "CHECKPOINT!";

            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                _checkpointNotification.alpha = elapsed / 0.3f;
                yield return null;
            }
            _checkpointNotification.alpha = 1f;

            yield return new WaitForSeconds(1.5f);

            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                _checkpointNotification.alpha = 1f - (elapsed / 0.5f);
                yield return null;
            }
            _checkpointNotification.alpha = 0f;
        }

        private IEnumerator PunchScale(RectTransform rt, float scale, float duration)
        {
            float elapsed = 0f;
            Vector3 original = Vector3.one;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float s = 1f + (scale - 1f) * Mathf.Sin(t * Mathf.PI);
                rt.localScale = original * s;
                yield return null;
            }
            rt.localScale = original;
        }
    }
}
