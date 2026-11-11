using System;
using UnityEngine;

namespace MarbleOdyssey
{
    public class DailyChallengeManager : MonoBehaviour
    {
        private static DailyChallengeManager _instance;
        public static DailyChallengeManager Instance => _instance;

        private SaveSystem _save;
        private int _todaySeed;
        private bool _completedToday;

        public int CurrentStreak => _save?.DailyStreak ?? 0;
        public bool CompletedToday => _completedToday;
        public int TodayWorldIndex { get; private set; }
        public int TodayLevelIndex { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _save = SaveSystem.Instance;
            GenerateDailyChallenge();
            CheckStreak();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void GenerateDailyChallenge()
        {
            string today = DateTime.UtcNow.ToString("yyyyMMdd");
            _todaySeed = today.GetHashCode();
            var rng = new System.Random(_todaySeed);
            TodayWorldIndex = rng.Next(0, GameConstants.TotalWorlds);
            TodayLevelIndex = rng.Next(0, GameConstants.LevelsPerWorld);
        }

        private void CheckStreak()
        {
            if (_save == null) return;
            string lastDate = _save.LastDailyDate;
            string today = DateTime.UtcNow.ToString("yyyyMMdd");

            _completedToday = lastDate == today;

            if (!string.IsNullOrEmpty(lastDate) && lastDate != today)
            {
                if (DateTime.TryParseExact(lastDate, "yyyyMMdd", null,
                    System.Globalization.DateTimeStyles.None, out DateTime last))
                {
                    double daysDiff = (DateTime.UtcNow.Date - last.Date).TotalDays;
                    if (daysDiff > 1)
                    {
                        _save.DailyStreak = 0;
                        _save.ForceSave();
                    }
                }
            }
        }

        public void CompleteChallenge(float completionTime)
        {
            if (_completedToday || _save == null) return;

            _completedToday = true;
            _save.DailyStreak++;
            _save.LastDailyDate = DateTime.UtcNow.ToString("yyyyMMdd");

            int bonus = Mathf.Min(
                _save.DailyStreak * GameConstants.Rewards.DailyStreakBonus,
                GameConstants.Rewards.MaxDailyStreakBonus);
            int totalReward = GameConstants.Rewards.DailyChallengeBase + bonus;

            _save.AddGems(totalReward);
            _save.ForceSave();

            EventBus.Publish(new DailyChallengeCompleteEvent
            {
                Day = int.Parse(DateTime.UtcNow.ToString("yyyyMMdd")),
                Time = completionTime,
                Streak = _save.DailyStreak
            });

            EventBus.Publish(new AudioPlayEvent { ClipName = "daily_complete", Volume = 1f });
        }

        public string GetChallengeWorldName()
        {
            if (TodayWorldIndex >= 0 && TodayWorldIndex < GameConstants.Worlds.Themes.Length)
                return GameConstants.Worlds.Themes[TodayWorldIndex].Name;
            return "Unknown";
        }

        public int GetCurrentReward()
        {
            int bonus = Mathf.Min(
                (CurrentStreak + 1) * GameConstants.Rewards.DailyStreakBonus,
                GameConstants.Rewards.MaxDailyStreakBonus);
            return GameConstants.Rewards.DailyChallengeBase + bonus;
        }
    }
}
