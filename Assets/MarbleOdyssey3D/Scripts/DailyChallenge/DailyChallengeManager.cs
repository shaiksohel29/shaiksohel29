using System;
using UnityEngine;

namespace MarbleOdyssey
{
    [Serializable]
    public struct DailyChallengeData
    {
        public string DateKey;
        public int WorldIndex;
        public int LevelIndex;
        public float SpeedMultiplier;
        public float GravityMultiplier;
        public float TimeLimit;
        public int GemTarget;
    }

    public class DailyChallengeManager : MonoBehaviour
    {
        private static DailyChallengeManager _instance;
        public static DailyChallengeManager Instance => _instance;

        [SerializeField] private ChallengeModifiers _modifiers;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public DailyChallengeData GetTodaysChallenge()
        {
            return GetChallengeForDate(DateTime.UtcNow);
        }

        public DailyChallengeData GetChallengeForDate(DateTime date)
        {
            string dateKey = date.ToString("yyyy-MM-dd");
            int seed = dateKey.GetHashCode();
            var rng = new System.Random(seed);

            int maxWorlds = _modifiers != null ? _modifiers.MaxUnlockedWorlds : GameConstants.TotalWorlds;
            int worldIndex = rng.Next(0, Mathf.Min(maxWorlds, GameConstants.TotalWorlds));
            int levelIndex = rng.Next(0, GameConstants.LevelsPerWorld);

            float speedMult = 1f;
            float gravMult = 1f;
            float timeLimit = 120f;
            int gemTarget = 5;

            if (_modifiers != null)
            {
                speedMult = Lerp(rng, _modifiers.MinSpeedMultiplier, _modifiers.MaxSpeedMultiplier);
                gravMult = Lerp(rng, _modifiers.MinGravityMultiplier, _modifiers.MaxGravityMultiplier);
                timeLimit = Lerp(rng, _modifiers.MinTimeLimit, _modifiers.MaxTimeLimit);
                gemTarget = rng.Next(_modifiers.MinGemTarget, _modifiers.MaxGemTarget + 1);
            }

            return new DailyChallengeData
            {
                DateKey = dateKey,
                WorldIndex = worldIndex,
                LevelIndex = levelIndex,
                SpeedMultiplier = speedMult,
                GravityMultiplier = gravMult,
                TimeLimit = timeLimit,
                GemTarget = gemTarget
            };
        }

        public bool HasCompletedToday()
        {
            string dateKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return PlayerPrefs.GetString("daily_last_completed", "") == dateKey;
        }

        public void CompleteChallenge(int score)
        {
            string dateKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string lastCompleted = PlayerPrefs.GetString("daily_last_completed", "");

            string yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
            int streak = PlayerPrefs.GetInt("daily_streak", 0);

            if (lastCompleted == yesterday)
                streak++;
            else if (lastCompleted != dateKey)
                streak = 1;

            PlayerPrefs.SetString("daily_last_completed", dateKey);
            PlayerPrefs.SetInt("daily_streak", streak);

            int bestScore = PlayerPrefs.GetInt("daily_best_" + dateKey, 0);
            if (score > bestScore)
                PlayerPrefs.SetInt("daily_best_" + dateKey, score);

            int totalCompleted = PlayerPrefs.GetInt("daily_total", 0) + 1;
            PlayerPrefs.SetInt("daily_total", totalCompleted);
            PlayerPrefs.Save();

            EventBus.Publish(new DailyChallengeCompletedEvent
            {
                Score = score,
                Streak = streak
            });
        }

        public int GetStreak() => PlayerPrefs.GetInt("daily_streak", 0);
        public int GetTotalCompleted() => PlayerPrefs.GetInt("daily_total", 0);

        private static float Lerp(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }

    public struct DailyChallengeCompletedEvent
    {
        public int Score;
        public int Streak;
    }
}
