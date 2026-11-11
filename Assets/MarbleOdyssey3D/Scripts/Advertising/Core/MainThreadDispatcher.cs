// ============================================================================
// Marble Odyssey 3D — AdMob Monetization Engine
// MainThreadDispatcher.cs — Thread-safe main-thread callback execution
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey.Advertising
{
    /// <summary>
    /// Dispatches actions to Unity's main thread from background threads.
    /// All AdMob SDK callbacks that modify Unity objects must execute on the main thread.
    /// Attach this to a persistent GameObject in the scene.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static readonly object _lock = new object();
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static bool _applicationQuitting;

        /// <summary>
        /// Enqueue an action to be executed on the main thread during the next Update.
        /// Thread-safe — can be called from any thread.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;

            lock (_lock)
            {
                if (_applicationQuitting) return;
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Ensures a MainThreadDispatcher instance exists in the scene.
        /// Call from Awake or initialization code.
        /// </summary>
        public static void EnsureExists()
        {
            if (_instance != null) return;

            lock (_lock)
            {
                if (_instance != null) return;

                var existing = FindFirstObjectByType<MainThreadDispatcher>();
                if (existing != null)
                {
                    _instance = existing;
                    return;
                }

                var go = new GameObject("[MainThreadDispatcher]");
                _instance = go.AddComponent<MainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _applicationQuitting = false;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // Process all queued actions
            while (true)
            {
                Action action;
                lock (_lock)
                {
                    if (_executionQueue.Count == 0) break;
                    action = _executionQueue.Dequeue();
                }

                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainThreadDispatcher] Action threw exception: {ex}");
                }
            }
        }

        private void OnApplicationQuit()
        {
            lock (_lock)
            {
                _applicationQuitting = true;
                _executionQueue.Clear();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
