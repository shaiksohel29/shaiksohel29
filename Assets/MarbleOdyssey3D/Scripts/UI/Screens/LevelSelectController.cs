// ============================================================================
// Marble Odyssey 3D — Level Selection Grid Controller
// 10 levels per world with star ratings, times, lock status.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MarbleOdyssey
{
    public class LevelSelectController : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private GameObject _levelCardPrefab;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Info Panel")]
        [SerializeField] private Text _worldTitleText;
        [SerializeField] private Text _levelInfoText;
        [SerializeField] private Text _bestTimeText;
        [SerializeField] private StarRatingDisplay _starDisplay;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Settings")]
        [SerializeField] private int _levelsPerWorld = 10;
        [SerializeField] private Color _unlockedColor = new Color(0.2f, 0.8f, 0.4f);
        [SerializeField] private Color _lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        [SerializeField] private Color _completedColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color _selectedGlow = new Color(1f, 0.85f, 0.3f);

        private string _worldId;
        private int _worldIndex;
        private int _selectedLevel = -1;
        private List<LevelCardUI> _cards = new List<LevelCardUI>();

        private void Start()
        {
            _worldId = PlayerPrefs.GetString("SelectedWorldId", "crystal_caverns");
            _worldIndex = PlayerPrefs.GetInt("SelectedWorldIndex", 0);

            if (_worldTitleText != null)
                _worldTitleText.text = _worldId.Replace('_', ' ').ToUpper();

            _playButton?.onClick.AddListener(OnPlayClicked);
            _backButton?.onClick.AddListener(OnBackClicked);
            _playButton.interactable = false;

            BuildLevelGrid();

            if (_canvasGroup != null)
                StartCoroutine(FadeIn());
        }

        // =====================================================================
        // Grid Building
        // =====================================================================

        private void BuildLevelGrid()
        {
            for (int i = 0; i < _levelsPerWorld; i++)
            {
                int levelNum = i + 1;
                string levelKey = $"{_worldId}_level_{levelNum}";

                var cardData = new LevelCardData
                {
                    LevelNumber = levelNum,
                    Stars = SaveSystem.GetLevelStars(levelKey),
                    BestTime = SaveSystem.GetLevelBestTime(levelKey),
                    IsCompleted = SaveSystem.IsLevelCompleted(levelKey),
                    IsUnlocked = IsLevelUnlocked(i),
                };

                GameObject cardObj;
                if (_levelCardPrefab != null)
                {
                    cardObj = Instantiate(_levelCardPrefab, _gridContainer);
                }
                else
                {
                    cardObj = CreateDefaultCard(cardData);
                }

                var card = cardObj.GetComponent<LevelCardUI>();
                if (card == null) card = cardObj.AddComponent<LevelCardUI>();

                card.Initialize(cardData, this);
                _cards.Add(card);
            }

            // Stagger entrance animation
            StartCoroutine(StaggerCardEntrance());
        }

        private bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex == 0) return true; // First level always unlocked

            // Previous level must be completed
            string prevKey = $"{_worldId}_level_{levelIndex}";
            return SaveSystem.IsLevelCompleted(prevKey);
        }

        private GameObject CreateDefaultCard(LevelCardData data)
        {
            var card = new GameObject($"Level_{data.LevelNumber}",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LevelCardUI));
            card.transform.SetParent(_gridContainer, false);

            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 180);

            var img = card.GetComponent<Image>();
            img.color = data.IsUnlocked
                ? (data.IsCompleted ? _completedColor : _unlockedColor)
                : _lockedColor;

            // Level number text
            var numObj = new GameObject("Number", typeof(RectTransform), typeof(Text));
            numObj.transform.SetParent(card.transform, false);
            var numRt = numObj.GetComponent<RectTransform>();
            numRt.anchoredPosition = Vector2.zero;
            numRt.sizeDelta = new Vector2(100, 60);
            var numText = numObj.GetComponent<Text>();
            numText.text = data.IsUnlocked ? data.LevelNumber.ToString() : "🔒";
            numText.fontSize = data.IsUnlocked ? 42 : 32;
            numText.alignment = TextAnchor.MiddleCenter;
            numText.color = Color.white;
            numText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Star display
            if (data.Stars > 0)
            {
                var starObj = new GameObject("Stars", typeof(RectTransform), typeof(Text));
                starObj.transform.SetParent(card.transform, false);
                var starRt = starObj.GetComponent<RectTransform>();
                starRt.anchoredPosition = new Vector2(0, -60);
                starRt.sizeDelta = new Vector2(100, 30);
                var starText = starObj.GetComponent<Text>();
                starText.text = new string('★', data.Stars) + new string('☆', 3 - data.Stars);
                starText.fontSize = 22;
                starText.alignment = TextAnchor.MiddleCenter;
                starText.color = new Color(1f, 0.85f, 0.2f);
                starText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return card;
        }

        // =====================================================================
        // Selection
        // =====================================================================

        public void SelectLevel(int levelNumber)
        {
            _selectedLevel = levelNumber;

            string levelKey = $"{_worldId}_level_{levelNumber}";
            int stars = SaveSystem.GetLevelStars(levelKey);
            float bestTime = SaveSystem.GetLevelBestTime(levelKey);

            if (_levelInfoText != null)
                _levelInfoText.text = $"Level {levelNumber}";

            if (_bestTimeText != null)
                _bestTimeText.text = bestTime > 0 ? FormatTime(bestTime) : "—";

            if (_starDisplay != null)
                _starDisplay.SetStars(stars, false);

            if (_playButton != null)
                _playButton.interactable = true;

            // Highlight selected card
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].SetSelected(i == levelNumber - 1);
            }

            AudioManager.Instance?.PlaySFX("button_hover");
        }

        // =====================================================================
        // Actions
        // =====================================================================

        private void OnPlayClicked()
        {
            if (_selectedLevel <= 0) return;

            AudioManager.Instance?.PlaySFX("button_click");

            PlayerPrefs.SetString("CurrentWorldId", _worldId);
            PlayerPrefs.SetInt("CurrentWorldIndex", _worldIndex);
            PlayerPrefs.SetInt("CurrentLevel", _selectedLevel);

            string sceneName = $"Level_{_worldId}_{_selectedLevel}";
            // Fallback to generic gameplay scene
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
                sceneName = "Gameplay";

            SceneManager.LoadScene(sceneName);
        }

        private void OnBackClicked()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            SceneManager.LoadScene("WorldSelect");
        }

        // =====================================================================
        // Animations
        // =====================================================================

        private IEnumerator StaggerCardEntrance()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var rt = _cards[i].GetComponent<RectTransform>();
                if (rt == null) continue;

                rt.localScale = Vector3.zero;
                StartCoroutine(ScaleInCard(rt, i * 0.06f));
            }
            yield break;
        }

        private IEnumerator ScaleInCard(RectTransform rt, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            float elapsed = 0f;
            float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                // Overshoot ease
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
                if (t >= 1f) scale = 1f;
                rt.localScale = Vector3.one * Mathf.Min(scale, 1.2f) * t;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private IEnumerator FadeIn()
        {
            _canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = elapsed / 0.4f;
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private string FormatTime(float seconds)
        {
            int min = (int)(seconds / 60);
            float sec = seconds % 60;
            return $"{min:D2}:{sec:05.2f}";
        }
    }

    // =========================================================================
    // Level Card Data
    // =========================================================================

    public struct LevelCardData
    {
        public int LevelNumber;
        public int Stars;
        public float BestTime;
        public bool IsCompleted;
        public bool IsUnlocked;
    }

    // =========================================================================
    // Level Card UI Component
    // =========================================================================

    public class LevelCardUI : MonoBehaviour
    {
        private LevelCardData _data;
        private LevelSelectController _controller;
        private Image _bgImage;
        private bool _isSelected;
        private Color _baseColor;

        public void Initialize(LevelCardData data, LevelSelectController controller)
        {
            _data = data;
            _controller = controller;
            _bgImage = GetComponent<Image>();
            if (_bgImage != null) _baseColor = _bgImage.color;

            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = data.IsUnlocked;
                btn.onClick.AddListener(() =>
                {
                    if (_data.IsUnlocked)
                        _controller.SelectLevel(_data.LevelNumber);
                });
            }
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (_bgImage != null)
            {
                _bgImage.color = selected
                    ? new Color(1f, 0.85f, 0.3f)
                    : _baseColor;
            }

            transform.localScale = selected ? Vector3.one * 1.1f : Vector3.one;
        }
    }
}
