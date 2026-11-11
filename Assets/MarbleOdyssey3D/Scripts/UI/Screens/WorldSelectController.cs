// ============================================================================
// Marble Odyssey 3D — World Selection Controller
// 6 worlds as 3D orbs with horizontal snap-scroll, lock/unlock, stars.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MarbleOdyssey
{
    public class WorldSelectController : MonoBehaviour
    {
        // =====================================================================
        // World Definition
        // =====================================================================

        [System.Serializable]
        public class WorldEntry
        {
            public string worldId;
            public string worldName;
            public string description;
            public int starsToUnlock;
            public Color themeColor = Color.white;
            public Sprite worldIcon;
            [Tooltip("3D orb prefab for this world")]
            public GameObject orbPrefab;
        }

        // =====================================================================
        // Inspector
        // =====================================================================

        [Header("World Data")]
        [SerializeField] private List<WorldEntry> _worlds = new List<WorldEntry>
        {
            new() { worldId = "crystal_caverns", worldName = "Crystal Caverns", starsToUnlock = 0, themeColor = new Color(0.3f, 0.7f, 1f) },
            new() { worldId = "molten_forge", worldName = "Molten Forge", starsToUnlock = 8, themeColor = new Color(1f, 0.4f, 0.1f) },
            new() { worldId = "sky_citadel", worldName = "Sky Citadel", starsToUnlock = 20, themeColor = new Color(0.5f, 0.8f, 1f) },
            new() { worldId = "shadow_realm", worldName = "Shadow Realm", starsToUnlock = 35, themeColor = new Color(0.4f, 0.2f, 0.6f) },
            new() { worldId = "neon_grid", worldName = "Neon Grid", starsToUnlock = 50, themeColor = new Color(0f, 1f, 0.7f) },
            new() { worldId = "cosmic_void", worldName = "Cosmic Void", starsToUnlock = 70, themeColor = new Color(0.1f, 0.1f, 0.3f) },
        };

        [Header("UI References")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentContainer;
        [SerializeField] private Text _worldNameText;
        [SerializeField] private Text _worldDescriptionText;
        [SerializeField] private Text _starsText;
        [SerializeField] private Text _lockInfoText;
        [SerializeField] private Button _enterButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private GameObject _lockOverlay;
        [SerializeField] private Image _backgroundGradient;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Card Prefab")]
        [SerializeField] private GameObject _worldCardPrefab;

        [Header("Scroll Settings")]
        [SerializeField] private float _snapSpeed = 12f;
        [SerializeField] private float _cardSpacing = 400f;
        [SerializeField] private float _scaleSelected = 1.15f;
        [SerializeField] private float _scaleUnselected = 0.8f;
        [SerializeField] private float _alphaUnselected = 0.55f;

        // =====================================================================
        // State
        // =====================================================================

        private int _selectedIndex;
        private List<RectTransform> _cards = new List<RectTransform>();
        private List<CanvasGroup> _cardGroups = new List<CanvasGroup>();
        private float _targetScrollPos;
        private bool _isDragging;
        private bool _initialized;
        private int _totalStars;

        // =====================================================================
        // Lifecycle
        // =====================================================================

        private void Start()
        {
            _totalStars = SaveSystem.GetTotalStars();
            _backButton?.onClick.AddListener(OnBackClicked);
            _enterButton?.onClick.AddListener(OnEnterClicked);

            BuildWorldCards();
            SelectWorld(0, false);

            if (_canvasGroup != null)
                StartCoroutine(FadeIn());

            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;

            HandleSnapScroll();
            UpdateCardVisuals();
        }

        // =====================================================================
        // Card Building
        // =====================================================================

        private void BuildWorldCards()
        {
            if (_contentContainer == null) return;

            for (int i = 0; i < _worlds.Count; i++)
            {
                GameObject card;
                if (_worldCardPrefab != null)
                {
                    card = Instantiate(_worldCardPrefab, _contentContainer);
                }
                else
                {
                    card = CreateDefaultCard(i);
                }

                var rt = card.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(i * _cardSpacing, 0);
                _cards.Add(rt);

                var cg = card.GetComponent<CanvasGroup>();
                if (cg == null) cg = card.AddComponent<CanvasGroup>();
                _cardGroups.Add(cg);

                // Card click handler
                int index = i;
                var btn = card.GetComponent<Button>();
                if (btn == null) btn = card.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectWorld(index, true));

                // Populate card text
                var nameLabel = card.GetComponentInChildren<Text>();
                if (nameLabel != null)
                    nameLabel.text = _worlds[i].worldName;
            }

            // Set content width
            float totalWidth = (_worlds.Count - 1) * _cardSpacing;
            _contentContainer.sizeDelta = new Vector2(totalWidth + 800f, _contentContainer.sizeDelta.y);
        }

        private GameObject CreateDefaultCard(int index)
        {
            var card = new GameObject($"WorldCard_{index}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_contentContainer, false);

            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 350);

            var img = card.GetComponent<Image>();
            img.color = _worlds[index].themeColor;
            img.sprite = null;

            // World name text
            var textObj = new GameObject("WorldName", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(card.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchoredPosition = new Vector2(0, -130);
            textRt.sizeDelta = new Vector2(280, 50);
            var text = textObj.GetComponent<Text>();
            text.text = _worlds[index].worldName;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 28;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return card;
        }

        // =====================================================================
        // Selection & Scrolling
        // =====================================================================

        public void SelectWorld(int index, bool animate)
        {
            _selectedIndex = Mathf.Clamp(index, 0, _worlds.Count - 1);
            _targetScrollPos = (float)_selectedIndex / Mathf.Max(1, _worlds.Count - 1);

            var world = _worlds[_selectedIndex];
            bool unlocked = _totalStars >= world.starsToUnlock;

            // Update info panel
            if (_worldNameText != null) _worldNameText.text = world.worldName;
            if (_worldDescriptionText != null) _worldDescriptionText.text = world.description;

            int worldStars = SaveSystem.GetWorldStars(world.worldId);
            int maxStars = 30; // 10 levels × 3 stars
            if (_starsText != null) _starsText.text = $"{worldStars} / {maxStars}";

            if (_lockOverlay != null) _lockOverlay.SetActive(!unlocked);
            if (_lockInfoText != null)
                _lockInfoText.text = unlocked ? "" : $"Requires {world.starsToUnlock} Stars ({_totalStars}/{world.starsToUnlock})";

            if (_enterButton != null) _enterButton.interactable = unlocked;

            // Animate background color
            if (_backgroundGradient != null && animate)
                StartCoroutine(LerpBackgroundColor(world.themeColor));

            if (animate)
                AudioManager.Instance?.PlaySFX("button_hover");
        }

        private void HandleSnapScroll()
        {
            if (_scrollRect == null) return;

            // Detect user drag
            if (Input.GetMouseButton(0) || Input.touchCount > 0)
            {
                _isDragging = true;
                return;
            }

            if (_isDragging)
            {
                _isDragging = false;
                // Snap to nearest card
                float currentPos = _scrollRect.horizontalNormalizedPosition;
                int nearest = Mathf.RoundToInt(currentPos * (_worlds.Count - 1));
                SelectWorld(nearest, true);
            }

            // Smooth snap
            float current = _scrollRect.horizontalNormalizedPosition;
            _scrollRect.horizontalNormalizedPosition =
                Mathf.Lerp(current, _targetScrollPos, Time.unscaledDeltaTime * _snapSpeed);
        }

        private void UpdateCardVisuals()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                bool isSelected = i == _selectedIndex;
                float targetScale = isSelected ? _scaleSelected : _scaleUnselected;
                float targetAlpha = isSelected ? 1f : _alphaUnselected;

                // Smooth scale
                _cards[i].localScale = Vector3.Lerp(
                    _cards[i].localScale,
                    Vector3.one * targetScale,
                    Time.unscaledDeltaTime * 8f);

                // Smooth alpha
                if (_cardGroups[i] != null)
                    _cardGroups[i].alpha = Mathf.Lerp(
                        _cardGroups[i].alpha, targetAlpha,
                        Time.unscaledDeltaTime * 8f);

                // Subtle rotation on unselected cards
                if (!isSelected)
                {
                    float tilt = (i < _selectedIndex ? 5f : -5f);
                    var targetRot = Quaternion.Euler(0, tilt, 0);
                    _cards[i].localRotation = Quaternion.Slerp(
                        _cards[i].localRotation, targetRot,
                        Time.unscaledDeltaTime * 6f);
                }
                else
                {
                    _cards[i].localRotation = Quaternion.Slerp(
                        _cards[i].localRotation, Quaternion.identity,
                        Time.unscaledDeltaTime * 6f);
                }
            }
        }

        // =====================================================================
        // Button Handlers
        // =====================================================================

        private void OnEnterClicked()
        {
            var world = _worlds[_selectedIndex];
            if (_totalStars < world.starsToUnlock) return;

            AudioManager.Instance?.PlaySFX("button_click");
            // Store selected world for level select
            PlayerPrefs.SetString("SelectedWorldId", world.worldId);
            PlayerPrefs.SetInt("SelectedWorldIndex", _selectedIndex);
            SceneManager.LoadScene("LevelSelect");
        }

        private void OnBackClicked()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            SceneManager.LoadScene("MainMenu");
        }

        // =====================================================================
        // Animations
        // =====================================================================

        private IEnumerator FadeIn()
        {
            _canvasGroup.alpha = 0f;
            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.SmoothStep(0, 1, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private IEnumerator LerpBackgroundColor(Color target)
        {
            Color start = _backgroundGradient.color;
            float elapsed = 0f;
            float duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _backgroundGradient.color = Color.Lerp(start, target * 0.3f, elapsed / duration);
                yield return null;
            }
        }
    }
}
