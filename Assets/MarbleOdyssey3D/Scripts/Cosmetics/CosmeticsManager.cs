using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey
{
    public class CosmeticsManager : MonoBehaviour
    {
        private static CosmeticsManager _instance;
        public static CosmeticsManager Instance => _instance;

        [SerializeField] private MarbleSkin[] _allSkins;
        [SerializeField] private MarbleSkin _defaultSkin;

        public event Action<MarbleSkin> OnSkinChanged;

        private string _equippedSkinId;
        private HashSet<string> _unlockedSkins = new();

        public MarbleSkin[] AllSkins => _allSkins;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadState();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void LoadState()
        {
            _equippedSkinId = PlayerPrefs.GetString("cosmetics_equipped", "default");
            string unlocked = PlayerPrefs.GetString("cosmetics_unlocked", "default");
            _unlockedSkins.Clear();
            foreach (var id in unlocked.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                    _unlockedSkins.Add(id);
            }
        }

        private void SaveState()
        {
            PlayerPrefs.SetString("cosmetics_equipped", _equippedSkinId);
            PlayerPrefs.SetString("cosmetics_unlocked", string.Join(",", _unlockedSkins));
            PlayerPrefs.Save();
        }

        public List<MarbleSkin> GetUnlockedSkins()
        {
            var result = new List<MarbleSkin>();
            if (_allSkins == null) return result;
            foreach (var skin in _allSkins)
            {
                if (IsSkinUnlocked(skin.SkinId))
                    result.Add(skin);
            }
            return result;
        }

        public MarbleSkin GetEquippedSkin()
        {
            if (_allSkins == null) return _defaultSkin;
            foreach (var skin in _allSkins)
            {
                if (skin.SkinId == _equippedSkinId)
                    return skin;
            }
            return _defaultSkin;
        }

        public void EquipSkin(string skinId)
        {
            if (!IsSkinUnlocked(skinId)) return;
            _equippedSkinId = skinId;
            SaveState();
            var skin = FindSkin(skinId);
            if (skin != null) OnSkinChanged?.Invoke(skin);
        }

        public bool PurchaseSkin(string skinId)
        {
            var skin = FindSkin(skinId);
            if (skin == null || skin.UnlockType != SkinUnlockType.GemPurchase) return false;
            if (IsSkinUnlocked(skinId)) return false;

            int gems = SaveSystem.Instance != null ? SaveSystem.Instance.GetGems() : 0;
            if (gems < skin.GemCost) return false;

            SaveSystem.Instance?.AddGems(-skin.GemCost);
            _unlockedSkins.Add(skinId);
            SaveState();
            return true;
        }

        public bool IsSkinUnlocked(string skinId)
        {
            if (string.IsNullOrEmpty(skinId)) return false;
            if (_unlockedSkins.Contains(skinId)) return true;
            var skin = FindSkin(skinId);
            if (skin == null) return false;
            return skin.UnlockType == SkinUnlockType.Default;
        }

        public void CheckAutoUnlocks()
        {
            if (_allSkins == null) return;
            bool changed = false;
            foreach (var skin in _allSkins)
            {
                if (_unlockedSkins.Contains(skin.SkinId)) continue;
                bool shouldUnlock = skin.UnlockType switch
                {
                    SkinUnlockType.Default => true,
                    SkinUnlockType.WorldComplete =>
                        skin.RequiredWorldIndex >= 0 &&
                        SaveSystem.Instance != null &&
                        SaveSystem.Instance.IsWorldComplete(skin.RequiredWorldIndex),
                    SkinUnlockType.DailyStreak =>
                        DailyChallengeManager.Instance != null &&
                        DailyChallengeManager.Instance.GetStreak() >= skin.RequiredStreak,
                    _ => false
                };
                if (shouldUnlock)
                {
                    _unlockedSkins.Add(skin.SkinId);
                    changed = true;
                }
            }
            if (changed) SaveState();
        }

        private MarbleSkin FindSkin(string skinId)
        {
            if (_allSkins == null) return null;
            foreach (var skin in _allSkins)
            {
                if (skin.SkinId == skinId) return skin;
            }
            return null;
        }
    }
}
