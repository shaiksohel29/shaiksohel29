// ============================================================================
// Marble Odyssey 3D — Core Game Systems
// SceneLoader.cs — Async scene loading with progress, fading, and additive support
// ============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MarbleOdyssey
{
    /// <summary>
    /// Manages async scene loading with a full-screen fade and an optional
    /// loading-screen overlay.  Supports single and additive loads.
    /// Attach to the same persistent GameObject as GameManager.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        // =====================================================================
        // Singleton
        // =====================================================================
        private static SceneLoader _instance;
        public static SceneLoader Instance => _instance;

        // =====================================================================
        // Inspector
        // =====================================================================
        [Header("Fade")]
        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private float _fadeDuration = 0.4f;

        [Header("Loading Screen (optional)")]
        [SerializeField] private GameObject _loadingScreenRoot;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Text _progressText;
        [SerializeField] private Text _tipText;

        [Header("Tips")]
        [SerializeField] private string[] _loadingTips = new[]
        {
            "Tilt your device gently for precise control.",
            "Collect all 3 stars for bonus gems!",
            "Speed pads give a burst of momentum.",
            "Watch for moving platforms — timing is everything.",
            "Complete daily challenges for streak bonuses.",
            "Checkpoints save your position mid-level."
        };

        // =====================================================================
        // State
        // =====================================================================
        private bool _isLoading;
        public bool IsLoading => _isLoading;

        /// <summary>0..1 progress of the current scene load.</summary>
        public float Progress { get; private set; }

        /// <summary>Fires with 0..1 progress during a load.</summary>
        public event Action<float> OnLoadProgress;

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
            DontDestroyOnLoad(gameObject);

            // Start fully transparent
            if (_fadeCanvasGroup != null)
            {
                _fadeCanvasGroup.alpha = 0f;
                _fadeCanvasGroup.blocksRaycasts = false;
            }

            if (_loadingScreenRoot != null)
                _loadingScreenRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Load a scene by name with fade-out → loading screen → fade-in.
        /// </summary>
        /// <param name="sceneName">Scene to load (must be in Build Settings).</param>
        /// <param name="onComplete">Called after the scene is active and fade-in is done.</param>
        public void LoadScene(string sceneName, Action onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning("[SceneLoader] Already loading — ignoring duplicate request.");
                return;
            }

            StartCoroutine(LoadSequence(sceneName, LoadSceneMode.Single, onComplete));
        }

        /// <summary>
        /// Load a scene additively (e.g., a UI overlay scene).
        /// </summary>
        public void LoadSceneAdditive(string sceneName, Action onComplete = null)
        {
            if (_isLoading)
            {
                Debug.LogWarning("[SceneLoader] Already loading — ignoring duplicate request.");
                return;
            }

            StartCoroutine(LoadSequence(sceneName, LoadSceneMode.Additive, onComplete));
        }

        /// <summary>Unload an additive scene.</summary>
        public void UnloadScene(string sceneName, Action onComplete = null)
        {
            StartCoroutine(UnloadSequence(sceneName, onComplete));
        }

        // =====================================================================
        // Coroutines
        // =====================================================================

        private IEnumerator LoadSequence(string sceneName, LoadSceneMode mode, Action onComplete)
        {
            _isLoading = true;
            Progress = 0f;

            // 1. Fade out
            yield return StartCoroutine(Fade(1f));

            // 2. Show loading screen
            ShowLoadingScreen();

            // 3. Async load
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' not found in Build Settings!");
                HideLoadingScreen();
                yield return StartCoroutine(Fade(0f));
                _isLoading = false;
                yield break;
            }

            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                // Unity reports 0 to 0.9 during load, then waits for allowSceneActivation
                Progress = Mathf.Clamp01(operation.progress / 0.9f);
                UpdateLoadingUI(Progress);
                OnLoadProgress?.Invoke(Progress);
                yield return null;
            }

            // Loading done — activate
            Progress = 1f;
            UpdateLoadingUI(1f);
            OnLoadProgress?.Invoke(1f);

            // Brief pause so the player sees 100%
            yield return new WaitForSecondsRealtime(0.3f);

            operation.allowSceneActivation = true;

            // Wait for activation
            while (!operation.isDone)
                yield return null;

            // 4. Hide loading screen
            HideLoadingScreen();

            // 5. Fade in
            yield return StartCoroutine(Fade(0f));

            _isLoading = false;
            onComplete?.Invoke();
        }

        private IEnumerator UnloadSequence(string sceneName, Action onComplete)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation != null)
            {
                while (!operation.isDone)
                    yield return null;
            }
            onComplete?.Invoke();
        }

        // =====================================================================
        // Fade
        // =====================================================================

        private IEnumerator Fade(float targetAlpha)
        {
            if (_fadeCanvasGroup == null) yield break;

            _fadeCanvasGroup.blocksRaycasts = true;
            float start = _fadeCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeCanvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / _fadeDuration);
                yield return null;
            }

            _fadeCanvasGroup.alpha = targetAlpha;
            _fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.5f;
        }

        /// <summary>Public helper to fade to black and call an action, then fade back.</summary>
        public void FadeAndExecute(Action midFadeAction)
        {
            StartCoroutine(FadeExecuteSequence(midFadeAction));
        }

        private IEnumerator FadeExecuteSequence(Action midFadeAction)
        {
            yield return StartCoroutine(Fade(1f));
            midFadeAction?.Invoke();
            yield return StartCoroutine(Fade(0f));
        }

        // =====================================================================
        // Loading Screen UI
        // =====================================================================

        private void ShowLoadingScreen()
        {
            if (_loadingScreenRoot != null)
                _loadingScreenRoot.SetActive(true);

            if (_tipText != null && _loadingTips.Length > 0)
                _tipText.text = _loadingTips[UnityEngine.Random.Range(0, _loadingTips.Length)];

            UpdateLoadingUI(0f);
        }

        private void HideLoadingScreen()
        {
            if (_loadingScreenRoot != null)
                _loadingScreenRoot.SetActive(false);
        }

        private void UpdateLoadingUI(float progress)
        {
            if (_progressBar != null)
                _progressBar.value = progress;

            if (_progressText != null)
                _progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
    }
}
