using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey
{
    public class CosmeticsManager : MonoBehaviour
    {
        public static CosmeticsManager Instance { get; private set; }

        [SerializeField] private MarbleSkinData[] _allSkins;
        [SerializeField] private MarbleSkinData _defaultSkin;

        private const string EquippedSkinKey = "Cosmetics_EquippedSkin";
        private const string OwnedSkinsKey = "Cosmetics_Owned";

        private HashSet<string> _ownedSkins = new HashSet<string>();
        private MarbleSkinData _equippedSkin;

        public MarbleSkinData EquippedSkin => _equippedSkin;
        public IReadOnlyCollection<MarbleSkinData> AllSkins => _allSkins;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadOwnedSkins();
            LoadEquippedSkin();
        }

        private void LoadOwnedSkins()
        {
            _ownedSkins.Clear();
            // Default skin is always owned
            if (_defaultSkin != null)
                _ownedSkins.Add(_defaultSkin.SkinId);

            string owned = PlayerPrefs.GetString(OwnedSkinsKey, "");
            if (!string.IsNullOrEmpty(owned))
            {
                foreach (var id in owned.Split(','))
                    _ownedSkins.Add(id.Trim());
            }
        }

        private void LoadEquippedSkin()
        {
            string equippedId = PlayerPrefs.GetString(EquippedSkinKey, "");
            _equippedSkin = GetSkinById(equippedId) ?? _defaultSkin;
        }

        public bool IsSkinOwned(string skinId) => _ownedSkins.Contains(skinId);

        public bool TryPurchaseSkin(string skinId)
        {
            if (_ownedSkins.Contains(skinId)) return false;

            var skin = GetSkinById(skinId);
            if (skin == null) return false;

            int currentGems = SaveSystem.GetGems();
            if (currentGems < skin.Price) return false;

            SaveSystem.AddGems(-skin.Price);
            _ownedSkins.Add(skinId);
            SaveOwnedSkins();

            AudioManager.Instance?.PlaySFX("purchase_success");
            return true;
        }

        public bool TryEquipSkin(string skinId)
        {
            if (!_ownedSkins.Contains(skinId)) return false;

            var skin = GetSkinById(skinId);
            if (skin == null) return false;

            _equippedSkin = skin;
            PlayerPrefs.SetString(EquippedSkinKey, skinId);
            PlayerPrefs.Save();

            AudioManager.Instance?.PlaySFX("equip_skin");
            return true;
        }

        public void ApplySkinToMarble(Renderer marbleRenderer)
        {
            if (_equippedSkin == null || marbleRenderer == null) return;

            if (_equippedSkin.Material != null)
                marbleRenderer.material = _equippedSkin.Material;

            if (_equippedSkin.OverrideColor)
                marbleRenderer.material.color = _equippedSkin.BaseColor;
        }

        private MarbleSkinData GetSkinById(string id)
        {
            if (string.IsNullOrEmpty(id) || _allSkins == null) return null;
            foreach (var skin in _allSkins)
                if (skin != null && skin.SkinId == id) return skin;
            return null;
        }

        private void SaveOwnedSkins()
        {
            PlayerPrefs.SetString(OwnedSkinsKey, string.Join(",", _ownedSkins));
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
