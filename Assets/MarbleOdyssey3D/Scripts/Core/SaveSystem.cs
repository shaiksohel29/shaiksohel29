// ============================================================================
// Marble Odyssey 3D — Core Game Systems
// SaveSystem.cs — PlayerPrefs-based persistent save / load system
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// Persistent save system backed by PlayerPrefs.
    /// Sensitive values (gem count, stars) are lightly XOR-obfuscated to
    /// discourage casual hex-editing.  This is NOT a security measure against
    /// determined attackers — use server-side verification for that.
    /// </summary>
    public class SaveSystem
    {
        // =====================================================================
        // Singleton (non-MonoBehaviour — just a C# object)
        // =====================================================================
        private static SaveSystem _instance;
        public static SaveSystem Instance => _instance;

        /// <summary>Call once early (GameManager.Start) to make sure data is loaded.</summary>
        public static void EnsureLoaded()
        {
            _instance ??= new SaveSystem();
        }

        // =====================================================================
        // In-Memory Cache
        // =====================================================================
        private readonly HashSet<int> _unlockedWorlds = new HashSet<int>();
        private readonly Dictionary<int, HashSet<int>> _unlockedLevels = new Dictionary<int, HashSet<int>>();
        private readonly Dictionary<string, int> _levelStars = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _bestTimes = new Dictionary<string, float>();
        private int _totalGems;
        private int _totalStars;

        // Settings
        private float _musicVolume;
        private float _sfxVolume;
        private float _sensitivity;
        private bool _vibrationEnabled;
        private int _qualityLevel;
        private bool _useGyroscope;

        // Cosmetics
        private string _equippedSkin;
        private string _equippedTrail;
        private string _equippedHat;
        private HashSet<string> _ownedCosmetics = new HashSet<string>();

        // Daily
        private int _dailyStreak;
        private string _lastDailyDate;

        // XOR key for obfuscation (NOT cryptographic security)
        private const byte XorKey = 0xA7;
        private readonly object _lock = new object();
        private bool _dirty;

        // =====================================================================
        // Constructor — loads everything from PlayerPrefs
        // =====================================================================
        private SaveSystem()
        {
            Load();
        }

        // =====================================================================
        // Public Accessors — Progress
        // =====================================================================

        public int TotalGems
        {
            get { lock (_lock) return _totalGems; }
        }

        public int TotalStars
        {
            get { lock (_lock) return _totalStars; }
        }

        public int DailyStreak
        {
            get { lock (_lock) return _dailyStreak; }
            set { lock (_lock) { _dailyStreak = value; _dirty = true; } }
        }

        public string LastDailyDate
        {
            get { lock (_lock) return _lastDailyDate; }
            set { lock (_lock) { _lastDailyDate = value; _dirty = true; } }
        }

        public bool IsWorldUnlocked(int worldIndex)
        {
            lock (_lock)
            {
                if (worldIndex == 0) return true; // first world always unlocked
                return _unlockedWorlds.Contains(worldIndex);
            }
        }

        public bool IsLevelUnlocked(int worldIndex, int levelIndex)
        {
            lock (_lock)
            {
                if (worldIndex == 0 && levelIndex == 0) return true; // 1-1 always unlocked
                if (!_unlockedLevels.TryGetValue(worldIndex, out var levels)) return false;
                return levels.Contains(levelIndex);
            }
        }

        public int GetLevelStars(int worldIndex, int levelIndex)
        {
            lock (_lock)
            {
                string key = StarKey(worldIndex, levelIndex);
                return _levelStars.TryGetValue(key, out int s) ? s : 0;
            }
        }

        public float GetBestTime(int worldIndex, int levelIndex)
        {
            lock (_lock)
            {
                string key = TimeKey(worldIndex, levelIndex);
                return _bestTimes.TryGetValue(key, out float t) ? t : float.MaxValue;
            }
        }

        public int GetWorldStarTotal(int worldIndex)
        {
            int total = 0;
            for (int i = 0; i < GameConstants.LevelsPerWorld; i++)
                total += GetLevelStars(worldIndex, i);
            return total;
        }

        // =====================================================================
        // Public Accessors — Settings
        // =====================================================================

        public float MusicVolume   { get { lock (_lock) return _musicVolume; } }
        public float SFXVolume     { get { lock (_lock) return _sfxVolume; } }
        public float Sensitivity   { get { lock (_lock) return _sensitivity; } }
        public bool  Vibration     { get { lock (_lock) return _vibrationEnabled; } }
        public int   QualityLevel  { get { lock (_lock) return _qualityLevel; } }
        public bool  UseGyroscope  { get { lock (_lock) return _useGyroscope; } }

        // =====================================================================
        // Public Accessors — Cosmetics
        // =====================================================================

        public string EquippedSkin  { get { lock (_lock) return _equippedSkin; } set { lock (_lock) { _equippedSkin = value; _dirty = true; } } }
        public string EquippedTrail { get { lock (_lock) return _equippedTrail; } set { lock (_lock) { _equippedTrail = value; _dirty = true; } } }
        public string EquippedHat   { get { lock (_lock) return _equippedHat; } set { lock (_lock) { _equippedHat = value; _dirty = true; } } }

        public string OwnedCosmetics
        {
            get { lock (_lock) return string.Join(",", _ownedCosmetics); }
            set
            {
                lock (_lock)
                {
                    _ownedCosmetics.Clear();
                    if (!string.IsNullOrEmpty(value))
                        foreach (var s in value.Split(','))
                            if (!string.IsNullOrWhiteSpace(s)) _ownedCosmetics.Add(s.Trim());
                    _dirty = true;
                }
            }
        }

        public bool OwnsCosmetic(string id)
        {
            lock (_lock) return _ownedCosmetics.Contains(id);
        }

        // =====================================================================
        // Mutators — Progress
        // =====================================================================

        public void UnlockWorld(int worldIndex)
        {
            lock (_lock)
            {
                if (worldIndex < 0 || worldIndex >= GameConstants.TotalWorlds) return;
                _unlockedWorlds.Add(worldIndex);
                _dirty = true;
            }
        }

        public void UnlockLevel(int worldIndex, int levelIndex)
        {
            lock (_lock)
            {
                if (!_unlockedLevels.TryGetValue(worldIndex, out var levels))
                {
                    levels = new HashSet<int>();
                    _unlockedLevels[worldIndex] = levels;
                }
                levels.Add(levelIndex);
                _dirty = true;
            }
        }

        public void SetLevelStars(int worldIndex, int levelIndex, int stars)
        {
            lock (_lock)
            {
                stars = Mathf.Clamp(stars, 0, GameConstants.MaxStarsPerLevel);
                string key = StarKey(worldIndex, levelIndex);

                // Only upgrade, never downgrade
                int existing = _levelStars.TryGetValue(key, out int s) ? s : 0;
                if (stars <= existing) return;

                int delta = stars - existing;
                _levelStars[key] = stars;
                _totalStars += delta;
                _dirty = true;
            }
        }

        public void SetBestTime(int worldIndex, int levelIndex, float time)
        {
            lock (_lock)
            {
                string key = TimeKey(worldIndex, levelIndex);
                float existing = _bestTimes.TryGetValue(key, out float t) ? t : float.MaxValue;
                if (time >= existing) return; // only save improvements
                _bestTimes[key] = time;
                _dirty = true;
            }
        }

        public void AddGems(int amount)
        {
            lock (_lock)
            {
                _totalGems += amount;
                _dirty = true;
            }
        }

        public bool SpendGems(int amount)
        {
            lock (_lock)
            {
                if (_totalGems < amount) return false;
                _totalGems -= amount;
                _dirty = true;
                return true;
            }
        }

        public int GetGems()
        {
            lock (_lock) return _totalGems;
        }

        public bool IsWorldComplete(int worldIndex)
        {
            lock (_lock)
            {
                for (int i = 0; i < GameConstants.LevelsPerWorld; i++)
                {
                    string key = StarKey(worldIndex, i);
                    if (!_levelStars.TryGetValue(key, out int stars) || stars < 1)
                        return false;
                }
                return true;
            }
        }

        // =====================================================================
        // Mutators — Settings
        // =====================================================================

        public void SetMusicVolume(float v)   { lock (_lock) { _musicVolume = Mathf.Clamp01(v); _dirty = true; } }
        public void SetSFXVolume(float v)     { lock (_lock) { _sfxVolume   = Mathf.Clamp01(v); _dirty = true; } }
        public void SetSensitivity(float v)   { lock (_lock) { _sensitivity = Mathf.Clamp(v, 0.1f, 3f); _dirty = true; } }
        public void SetVibration(bool v)      { lock (_lock) { _vibrationEnabled = v; _dirty = true; } }
        public void SetQualityLevel(int v)    { lock (_lock) { _qualityLevel = Mathf.Clamp(v, 0, 3); _dirty = true; } }
        public void SetUseGyroscope(bool v)   { lock (_lock) { _useGyroscope = v; _dirty = true; } }

        // =====================================================================
        // Mutators — Cosmetics
        // =====================================================================

        public void EquipSkin(string id)   { lock (_lock) { _equippedSkin  = id; _dirty = true; } }
        public void EquipTrail(string id)  { lock (_lock) { _equippedTrail = id; _dirty = true; } }
        public void EquipHat(string id)    { lock (_lock) { _equippedHat   = id; _dirty = true; } }

        public void AddCosmetic(string id)
        {
            lock (_lock) { _ownedCosmetics.Add(id); _dirty = true; }
        }

        // =====================================================================
        // Mutators — Daily Challenge
        // =====================================================================

        public void RecordDailyChallenge()
        {
            lock (_lock)
            {
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                if (_lastDailyDate == today) return; // already recorded

                // Check streak
                if (!string.IsNullOrEmpty(_lastDailyDate) &&
                    DateTime.TryParse(_lastDailyDate, out var lastDate))
                {
                    var diff = (DateTime.UtcNow.Date - lastDate.Date).Days;
                    _dailyStreak = diff == 1 ? _dailyStreak + 1 : 1;
                }
                else
                {
                    _dailyStreak = 1;
                }

                _lastDailyDate = today;
                _dirty = true;
            }
        }

        // =====================================================================
        // Save / Load
        // =====================================================================

        /// <summary>Flush cached data to PlayerPrefs. Call after important changes.</summary>
        public void ForceSave()
        {
            lock (_lock) { _dirty = true; }
            Save();
        }

        public void Save()
        {
            lock (_lock)
            {
                if (!_dirty) return;

                try
                {
                    // Version
                    PlayerPrefs.SetInt(GameConstants.Prefs.SaveVersion, GameConstants.SaveVersion);

                    // Gems & stars (obfuscated)
                    SetObfuscatedInt(GameConstants.Prefs.TotalGems, _totalGems);
                    SetObfuscatedInt(GameConstants.Prefs.TotalStars, _totalStars);

                    // Unlocked worlds
                    PlayerPrefs.SetString(GameConstants.Prefs.UnlockedWorlds,
                        SetToString(_unlockedWorlds));

                    // Unlocked levels per world
                    foreach (var kvp in _unlockedLevels)
                    {
                        string k = string.Format(GameConstants.Prefs.UnlockedLevels, kvp.Key);
                        PlayerPrefs.SetString(k, SetToString(kvp.Value));
                    }

                    // Stars and times
                    foreach (var kvp in _levelStars)
                        SetObfuscatedInt(kvp.Key, kvp.Value);

                    foreach (var kvp in _bestTimes)
                        PlayerPrefs.SetFloat(kvp.Key, kvp.Value);

                    // Settings
                    PlayerPrefs.SetFloat(GameConstants.Prefs.MusicVolume, _musicVolume);
                    PlayerPrefs.SetFloat(GameConstants.Prefs.SFXVolume, _sfxVolume);
                    PlayerPrefs.SetFloat(GameConstants.Prefs.Sensitivity, _sensitivity);
                    PlayerPrefs.SetInt(GameConstants.Prefs.VibrationEnabled, _vibrationEnabled ? 1 : 0);
                    PlayerPrefs.SetInt(GameConstants.Prefs.QualityLevel, _qualityLevel);
                    PlayerPrefs.SetInt(GameConstants.Prefs.UseGyroscope, _useGyroscope ? 1 : 0);

                    // Cosmetics
                    PlayerPrefs.SetString(GameConstants.Prefs.EquippedSkin, _equippedSkin ?? "");
                    PlayerPrefs.SetString(GameConstants.Prefs.EquippedTrail, _equippedTrail ?? "");
                    PlayerPrefs.SetString(GameConstants.Prefs.EquippedHat, _equippedHat ?? "");
                    PlayerPrefs.SetString(GameConstants.Prefs.OwnedCosmetics,
                        string.Join(",", _ownedCosmetics));

                    // Daily
                    PlayerPrefs.SetInt(GameConstants.Prefs.DailyStreak, _dailyStreak);
                    PlayerPrefs.SetString(GameConstants.Prefs.LastDailyDate, _lastDailyDate ?? "");

                    PlayerPrefs.Save();
                    _dirty = false;

                    Debug.Log("[SaveSystem] Saved successfully.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveSystem] Save failed: {ex.Message}");
                }
            }
        }

        private void Load()
        {
            lock (_lock)
            {
                // Gems & stars
                _totalGems  = GetObfuscatedInt(GameConstants.Prefs.TotalGems, 0);
                _totalStars = GetObfuscatedInt(GameConstants.Prefs.TotalStars, 0);

                // Unlocked worlds
                _unlockedWorlds.Clear();
                _unlockedWorlds.Add(0); // world 0 always unlocked
                ParseIntoSet(PlayerPrefs.GetString(GameConstants.Prefs.UnlockedWorlds, "0"),
                    _unlockedWorlds);

                // Unlocked levels
                _unlockedLevels.Clear();
                for (int w = 0; w < GameConstants.TotalWorlds; w++)
                {
                    string k = string.Format(GameConstants.Prefs.UnlockedLevels, w);
                    string raw = PlayerPrefs.GetString(k, w == 0 ? "0" : "");
                    if (!string.IsNullOrEmpty(raw))
                    {
                        var set = new HashSet<int>();
                        ParseIntoSet(raw, set);
                        _unlockedLevels[w] = set;
                    }
                }

                // Level stars and times
                _levelStars.Clear();
                _bestTimes.Clear();
                for (int w = 0; w < GameConstants.TotalWorlds; w++)
                {
                    for (int l = 0; l < GameConstants.LevelsPerWorld; l++)
                    {
                        string sk = StarKey(w, l);
                        int stars = GetObfuscatedInt(sk, 0);
                        if (stars > 0) _levelStars[sk] = stars;

                        string tk = TimeKey(w, l);
                        if (PlayerPrefs.HasKey(tk))
                            _bestTimes[tk] = PlayerPrefs.GetFloat(tk);
                    }
                }

                // Settings
                _musicVolume      = PlayerPrefs.GetFloat(GameConstants.Prefs.MusicVolume, GameConstants.Defaults.MusicVolume);
                _sfxVolume        = PlayerPrefs.GetFloat(GameConstants.Prefs.SFXVolume, GameConstants.Defaults.SFXVolume);
                _sensitivity      = PlayerPrefs.GetFloat(GameConstants.Prefs.Sensitivity, GameConstants.Defaults.Sensitivity);
                _vibrationEnabled = PlayerPrefs.GetInt(GameConstants.Prefs.VibrationEnabled, GameConstants.Defaults.Vibration ? 1 : 0) == 1;
                _qualityLevel     = PlayerPrefs.GetInt(GameConstants.Prefs.QualityLevel, GameConstants.Defaults.QualityLevel);
                _useGyroscope     = PlayerPrefs.GetInt(GameConstants.Prefs.UseGyroscope, GameConstants.Defaults.UseGyroscope ? 1 : 0) == 1;

                // Cosmetics
                _equippedSkin  = PlayerPrefs.GetString(GameConstants.Prefs.EquippedSkin, GameConstants.Defaults.DefaultSkin);
                _equippedTrail = PlayerPrefs.GetString(GameConstants.Prefs.EquippedTrail, GameConstants.Defaults.DefaultTrail);
                _equippedHat   = PlayerPrefs.GetString(GameConstants.Prefs.EquippedHat, GameConstants.Defaults.DefaultHat);

                _ownedCosmetics.Clear();
                string ownedRaw = PlayerPrefs.GetString(GameConstants.Prefs.OwnedCosmetics, "");
                if (!string.IsNullOrEmpty(ownedRaw))
                {
                    foreach (var id in ownedRaw.Split(','))
                    {
                        string trimmed = id.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                            _ownedCosmetics.Add(trimmed);
                    }
                }
                // Defaults always owned
                _ownedCosmetics.Add(GameConstants.Defaults.DefaultSkin);

                // Daily
                _dailyStreak   = PlayerPrefs.GetInt(GameConstants.Prefs.DailyStreak, 0);
                _lastDailyDate = PlayerPrefs.GetString(GameConstants.Prefs.LastDailyDate, "");

                _dirty = false;
                Debug.Log($"[SaveSystem] Loaded — {_totalGems} gems, {_totalStars} stars, " +
                          $"{_unlockedWorlds.Count} worlds unlocked.");
            }
        }

        // =====================================================================
        // Static convenience methods — used by UI screens with string keys
        // =====================================================================

        public static int GetLevelStars(string levelKey)
        {
            return PlayerPrefs.GetInt($"quick_{levelKey}_stars", 0);
        }

        public static float GetLevelBestTime(string levelKey)
        {
            return PlayerPrefs.GetFloat($"quick_{levelKey}_time", 0f);
        }

        public static bool IsLevelCompleted(string levelKey)
        {
            return PlayerPrefs.GetInt($"quick_{levelKey}_stars", 0) > 0;
        }

        public static bool SaveLevelCompletion(string levelKey, int stars, float time, int gems)
        {
            int prevStars = PlayerPrefs.GetInt($"quick_{levelKey}_stars", 0);
            float prevTime = PlayerPrefs.GetFloat($"quick_{levelKey}_time", float.MaxValue);
            bool isNewBest = false;

            if (stars > prevStars)
            {
                PlayerPrefs.SetInt($"quick_{levelKey}_stars", stars);
                isNewBest = true;
            }

            if (time < prevTime)
            {
                PlayerPrefs.SetFloat($"quick_{levelKey}_time", time);
                isNewBest = true;
            }

            Instance?.AddGems(gems);
            Instance?.Save();
            return isNewBest;
        }

        public static int GetGems()
        {
            return Instance?.TotalGems ?? PlayerPrefs.GetInt("Player_TotalGems", 0);
        }

        public static int GetStars()
        {
            return Instance?.TotalStars ?? PlayerPrefs.GetInt("Player_TotalStars", 0);
        }

        public static void AddGems(int amount)
        {
            if (Instance != null) { Instance.AddGems(amount); Instance.Save(); }
            else
            {
                int current = PlayerPrefs.GetInt("Player_TotalGems", 0);
                PlayerPrefs.SetInt("Player_TotalGems", current + amount);
                PlayerPrefs.Save();
            }
        }

        public static void AddStars(int amount)
        {
            int current = PlayerPrefs.GetInt("Player_TotalStars", 0);
            PlayerPrefs.SetInt("Player_TotalStars", current + amount);
            PlayerPrefs.Save();
        }

        public static int GetTotalStars()
        {
            return Instance?.TotalStars ?? PlayerPrefs.GetInt("Player_TotalStars", 0);
        }

        public static bool IsWorldUnlockedByStars(int requiredStars)
        {
            return GetTotalStars() >= requiredStars;
        }

        /// <summary>Wipe everything (for testing / new game).</summary>
        public void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            _instance = new SaveSystem(); // reload clean
            Debug.Log("[SaveSystem] All data wiped.");
        }

        // =====================================================================
        // Obfuscation helpers
        // =====================================================================

        private static void SetObfuscatedInt(string key, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            for (int i = 0; i < bytes.Length; i++) bytes[i] ^= XorKey;
            PlayerPrefs.SetString(key, Convert.ToBase64String(bytes));
        }

        private static int GetObfuscatedInt(string key, int defaultValue)
        {
            string raw = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(raw)) return defaultValue;
            try
            {
                byte[] bytes = Convert.FromBase64String(raw);
                for (int i = 0; i < bytes.Length; i++) bytes[i] ^= XorKey;
                return BitConverter.ToInt32(bytes, 0);
            }
            catch
            {
                return defaultValue;
            }
        }

        // =====================================================================
        // Key / serialization helpers
        // =====================================================================

        private static string StarKey(int w, int l)
            => string.Format(GameConstants.Prefs.LevelStars, w, l);

        private static string TimeKey(int w, int l)
            => string.Format(GameConstants.Prefs.BestTime, w, l);

        private static string SetToString(HashSet<int> set)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (int v in set)
            {
                if (!first) sb.Append(',');
                sb.Append(v);
                first = false;
            }
            return sb.ToString();
        }

        private static void ParseIntoSet(string csv, HashSet<int> set)
        {
            if (string.IsNullOrEmpty(csv)) return;
            foreach (var tok in csv.Split(','))
            {
                if (int.TryParse(tok.Trim(), out int v))
                    set.Add(v);
            }
        }
    }
}
