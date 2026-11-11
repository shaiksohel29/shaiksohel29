using System;
using UnityEngine;

namespace MarbleOdyssey
{
    public static class EventManager
    {
        // Level flow
        public static event Action OnLevelStart;
        public static event Action<int, int, float> OnLevelComplete; // stars, gems, time
        public static event Action OnLevelFailed;
        public static event Action<int> OnLevelRestart; // death count

        // Collectibles
        public static event Action<int> OnGemCollected; // gem value
        public static event Action<int> OnTotalGemsChanged; // new total

        // Marble
        public static event Action OnMarbleDeath;
        public static event Action<Vector3> OnCheckpointReached; // checkpoint position
        public static event Action OnMarbleRespawned;

        // Game state
        public static event Action<bool> OnPauseToggled; // isPaused
        public static event Action<int> OnWorldUnlocked; // world index
        public static event Action<string> OnSkinEquipped; // skin id

        // UI
        public static event Action OnMainMenuEntered;
        public static event Action OnGameplayStarted;
        public static event Action OnWorldSelectionEntered;

        // Timer
        public static event Action<float> OnTimerTick; // elapsed time

        // Triggers
        public static void TriggerLevelStart() => OnLevelStart?.Invoke();
        public static void TriggerLevelComplete(int stars, int gems, float time)
            => OnLevelComplete?.Invoke(stars, gems, time);
        public static void TriggerLevelFailed() => OnLevelFailed?.Invoke();
        public static void TriggerLevelRestart(int deathCount) => OnLevelRestart?.Invoke(deathCount);
        public static void TriggerGemCollected(int value) => OnGemCollected?.Invoke(value);
        public static void TriggerTotalGemsChanged(int total) => OnTotalGemsChanged?.Invoke(total);
        public static void TriggerMarbleDeath() => OnMarbleDeath?.Invoke();
        public static void TriggerCheckpointReached(Vector3 pos) => OnCheckpointReached?.Invoke(pos);
        public static void TriggerMarbleRespawned() => OnMarbleRespawned?.Invoke();
        public static void TriggerPauseToggled(bool paused) => OnPauseToggled?.Invoke(paused);
        public static void TriggerWorldUnlocked(int world) => OnWorldUnlocked?.Invoke(world);
        public static void TriggerSkinEquipped(string id) => OnSkinEquipped?.Invoke(id);
        public static void TriggerMainMenuEntered() => OnMainMenuEntered?.Invoke();
        public static void TriggerGameplayStarted() => OnGameplayStarted?.Invoke();
        public static void TriggerWorldSelectionEntered() => OnWorldSelectionEntered?.Invoke();
        public static void TriggerTimerTick(float elapsed) => OnTimerTick?.Invoke(elapsed);

        public static void ClearAll()
        {
            OnLevelStart = null;
            OnLevelComplete = null;
            OnLevelFailed = null;
            OnLevelRestart = null;
            OnGemCollected = null;
            OnTotalGemsChanged = null;
            OnMarbleDeath = null;
            OnCheckpointReached = null;
            OnMarbleRespawned = null;
            OnPauseToggled = null;
            OnWorldUnlocked = null;
            OnSkinEquipped = null;
            OnMainMenuEntered = null;
            OnGameplayStarted = null;
            OnWorldSelectionEntered = null;
            OnTimerTick = null;
        }
    }
}
