// ============================================================================
// Marble Odyssey 3D — Core Game Systems
// EventBus.cs — Lightweight, type-safe publish / subscribe event bus
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey
{
    // =========================================================================
    // Event Bus
    // =========================================================================

    /// <summary>
    /// A zero-allocation (for repeat publishes) event bus keyed on event type.
    /// Any struct or class can be an event — just publish/subscribe by type.
    /// Subscriptions are automatically safe against removal during iteration.
    /// </summary>
    public static class EventBus
    {
        /// <summary>Subscribe to events of type <typeparamref name="T"/>.</summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            EventChannel<T>.Subscribe(handler);
        }

        /// <summary>Unsubscribe a previously registered handler.</summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            EventChannel<T>.Unsubscribe(handler);
        }

        /// <summary>Publish an event to all current subscribers.</summary>
        public static void Publish<T>(T evt) where T : struct
        {
            EventChannel<T>.Publish(evt);
        }

        /// <summary>Remove ALL subscribers for every event type. Call on hard reset.</summary>
        public static void ClearAll()
        {
            _clearActions?.Invoke();
        }

        // Channels register themselves here so ClearAll can reach them.
        internal static event Action _clearActions;

        // Each event type T gets its own static generic channel — no dictionaries.
        private static class EventChannel<T> where T : struct
        {
            private static readonly List<Action<T>> _handlers = new List<Action<T>>();
            private static bool _isPublishing;
            private static readonly List<Action<T>> _pendingAdds = new List<Action<T>>();
            private static readonly List<Action<T>> _pendingRemoves = new List<Action<T>>();

            static EventChannel()
            {
                EventBus._clearActions += () =>
                {
                    _handlers.Clear();
                    _pendingAdds.Clear();
                    _pendingRemoves.Clear();
                    _isPublishing = false;
                };
            }

            public static void Subscribe(Action<T> handler)
            {
                if (handler == null) return;
                if (_isPublishing)
                    _pendingAdds.Add(handler);
                else
                    _handlers.Add(handler);
            }

            public static void Unsubscribe(Action<T> handler)
            {
                if (handler == null) return;
                if (_isPublishing)
                    _pendingRemoves.Add(handler);
                else
                    _handlers.Remove(handler);
            }

            public static void Publish(T evt)
            {
                _isPublishing = true;
                for (int i = 0; i < _handlers.Count; i++)
                {
                    try
                    {
                        _handlers[i]?.Invoke(evt);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EventBus] Handler for {typeof(T).Name} threw: {ex}");
                    }
                }
                _isPublishing = false;

                // Apply deferred mutations
                if (_pendingRemoves.Count > 0)
                {
                    foreach (var h in _pendingRemoves) _handlers.Remove(h);
                    _pendingRemoves.Clear();
                }
                if (_pendingAdds.Count > 0)
                {
                    _handlers.AddRange(_pendingAdds);
                    _pendingAdds.Clear();
                }
            }
        }
    }

    // =========================================================================
    // Event Definitions
    // =========================================================================

    public struct GameStateChangedEvent
    {
        public GameState OldState;
        public GameState NewState;
    }

    public struct LevelStartedEvent
    {
        public int WorldIndex;
        public int LevelIndex;
    }

    public struct LevelCompletedEvent
    {
        public GameManager.LevelResultData Result;
    }

    public struct LevelFailedEvent
    {
        public int WorldIndex;
        public int LevelIndex;
    }

    public struct GemCollectedEvent
    {
        public int Amount;
        public int TotalInLevel;
    }

    public struct StarCollectedEvent
    {
        public int StarIndex; // 0, 1, or 2
    }

    public struct CheckpointReachedEvent
    {
        public int CheckpointIndex;
        public Vector3 Position;
    }

    public struct MarbleDiedEvent
    {
        public Vector3 DeathPosition;
        public string Cause; // "fall", "obstacle", "outOfBounds"
    }

    public struct MarbleRespawnedEvent
    {
        public Vector3 SpawnPosition;
        public int CheckpointIndex;
    }

    public struct ScoreChangedEvent
    {
        public int NewScore;
        public int Delta;
    }

    public struct TimeUpdatedEvent
    {
        public float Elapsed;
    }

    public struct ComboEvent
    {
        public int ComboCount;
        public float ComboMultiplier;
    }

    public struct SpeedBoostEvent
    {
        public float Duration;
        public float Multiplier;
    }

    public struct ObstacleHitEvent
    {
        public string ObstacleType;
        public Vector3 HitPoint;
        public float ImpactForce;
    }

    public struct CameraShakeRequestEvent
    {
        public float Intensity;
        public float Duration;
    }

    public struct UINavigationEvent
    {
        public string ScreenName;
        public bool IsForward; // true = push, false = pop
    }

    public struct AudioPlayEvent
    {
        public string ClipName;
        public float Volume;
        public bool IsMusic;
    }

    public struct DailyChallengeCompleteEvent
    {
        public int Day;
        public float Time;
        public int Streak;
    }

    public struct CosmeticEquippedEvent
    {
        public string CosmeticId;
        public string CosmeticType; // "skin", "trail", "hat"
    }
}
