using System;
using UnityEngine;

namespace MarbleOdyssey
{
    public class DailyChallengeManager : MonoBehaviour
    {
        public static DailyChallengeManager Instance { get; private set; }

        [Serializable]
        public struct ChallengeTemplate
        {
            public string Description;
            public string WorldId;
            public int LevelNumber;
            public float TargetTime;
            public int GemReward;
            public int StarReward;
        }

        [SerializeField] private ChallengeTemplate[] _templates = new ChallengeTemplate[]
        {
            new ChallengeTemplate { Description = "Complete Crystal Caverns L1 in under 25s", WorldId = "crystal_caverns", LevelNumber = 1, TargetTime = 25f, GemReward = 50, StarReward = 3 },
            new ChallengeTemplate { Description = "Complete Molten Forge L3 in under 40s", WorldId = "molten_forge", LevelNumber = 3, TargetTime = 40f, GemReward = 75, StarReward = 5 },
            new ChallengeTemplate { Description = "Complete Sky Citadel L2 in under 35s", WorldId = "sky_citadel", LevelNumber = 2, TargetTime = 35f, GemReward = 60, StarReward = 4 },
            new ChallengeTemplate { Description = "Complete Shadow Realm L1 in under 30s", WorldId = "shadow_realm", LevelNumber = 1, TargetTime = 30f, GemReward = 80, StarReward = 5 },
            new ChallengeTemplate { Description = "Complete Neon Grid L5 in under 45s", WorldId = "neon_grid", LevelNumber = 5, TargetTime = 45f, GemReward = 100, StarReward = 6 },
            new ChallengeTemplate { Description = "Complete Crystal Caverns L7 in under 50s", WorldId = "crystal_caverns", LevelNumber = 7, TargetTime = 50f, GemReward = 90, StarReward = 5 },
            new ChallengeTemplate { Description = "Complete Cosmic Void L1 in under 35s", WorldId = "cosmic_void", LevelNumber = 1, TargetTime = 35f, GemReward = 120, StarReward = 8 },
        };

        private const string LastClaimDateKey = "DailyChallenge_LastClaim";
        private const string StreakKey = "DailyChallenge_Streak";

        public ChallengeTemplate TodaysChallenge { get; private set; }
        public bool IsCompletedToday { get; private set; }
        public int CurrentStreak => PlayerPrefs.GetInt(StreakKey, 0);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            SelectTodaysChallenge();
            CheckCompletionStatus();
        }

        private void SelectTodaysChallenge()
        {
            // Deterministic selection based on date
            int daysSinceEpoch = (int)(DateTime.UtcNow - new DateTime(2024, 1, 1)).TotalDays;
            int index = daysSinceEpoch % _templates.Length;
            TodaysChallenge = _templates[index];
        }

        private void CheckCompletionStatus()
        {
            string lastClaim = PlayerPrefs.GetString(LastClaimDateKey, "");
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            IsCompletedToday = lastClaim == today;
        }

        public bool TryCompleteChallenge(string worldId, int levelNumber, float completionTime)
        {
            if (IsCompletedToday) return false;

            if (worldId != TodaysChallenge.WorldId ||
                levelNumber != TodaysChallenge.LevelNumber ||
                completionTime > TodaysChallenge.TargetTime)
                return false;

            // Mark as complete
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
            string lastClaim = PlayerPrefs.GetString(LastClaimDateKey, "");

            // Update streak
            int streak = lastClaim == yesterday ? CurrentStreak + 1 : 1;
            PlayerPrefs.SetInt(StreakKey, streak);
            PlayerPrefs.SetString(LastClaimDateKey, today);

            // Award rewards with streak bonus
            int streakBonus = Mathf.Min(streak, 7); // Cap at 7x
            int totalGems = TodaysChallenge.GemReward + (streakBonus * 10);

            SaveSystem.AddGems(totalGems);
            SaveSystem.AddStars(TodaysChallenge.StarReward);

            IsCompletedToday = true;
            PlayerPrefs.Save();
            return true;
        }

        public TimeSpan TimeUntilReset()
        {
            var now = DateTime.UtcNow;
            var tomorrow = now.Date.AddDays(1);
            return tomorrow - now;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
