// ============================================================================
// Marble Odyssey 3D — Core Game Systems
// ObjectPool.cs — Generic, pre-warmed object pool for particles & gameplay objects
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// Generic object pool that pre-instantiates GameObjects and recycles them.
    /// Use for particles, collectible VFX, obstacle projectiles, etc.
    ///
    /// Usage:
    ///   var pool = new ObjectPool(prefab, 20, poolParent);
    ///   GameObject obj = pool.Get(position, rotation);
    ///   pool.Return(obj);
    /// </summary>
    public class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Queue<GameObject> _available = new Queue<GameObject>();
        private readonly HashSet<GameObject> _active = new HashSet<GameObject>();
        private readonly int _expandStep;
        private readonly int _maxSize;

        /// <summary>How many objects are currently checked out.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>How many objects are waiting in the pool.</summary>
        public int AvailableCount => _available.Count;

        /// <summary>
        /// Create a new pool.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="preWarmCount">How many to create immediately.</param>
        /// <param name="parent">Optional parent transform for pooled objects.</param>
        /// <param name="expandStep">How many to create when the pool is empty (0 = 1 at a time).</param>
        /// <param name="maxSize">Hard cap on total pooled objects (0 = unlimited).</param>
        public ObjectPool(GameObject prefab, int preWarmCount = 10,
            Transform parent = null, int expandStep = 5, int maxSize = 0)
        {
            _prefab = prefab ? prefab : throw new ArgumentNullException(nameof(prefab));
            _parent = parent;
            _expandStep = Mathf.Max(1, expandStep);
            _maxSize = maxSize;

            PreWarm(preWarmCount);
        }

        /// <summary>Get an object from the pool, activating and positioning it.</summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            if (_available.Count == 0)
                Expand(_expandStep);

            if (_available.Count == 0)
            {
                Debug.LogWarning($"[ObjectPool] Pool for '{_prefab.name}' exhausted (max {_maxSize}).");
                return null;
            }

            var obj = _available.Dequeue();

            // Guard against destroyed objects (scene unload, etc.)
            if (obj == null)
            {
                // Try next available
                return Get(position, rotation);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            _active.Add(obj);
            return obj;
        }

        /// <summary>Get with just a position (identity rotation).</summary>
        public GameObject Get(Vector3 position) => Get(position, Quaternion.identity);

        /// <summary>Return an object to the pool. Deactivates it.</summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;
            if (!_active.Remove(obj)) return; // wasn't ours

            obj.SetActive(false);
            if (_parent != null) obj.transform.SetParent(_parent);
            _available.Enqueue(obj);
        }

        /// <summary>Return an object after a delay (useful for particle lifetimes).</summary>
        public void ReturnDelayed(GameObject obj, float delay)
        {
            if (obj == null) return;
            var returner = obj.GetComponent<PooledAutoReturn>();
            if (returner == null)
                returner = obj.AddComponent<PooledAutoReturn>();
            returner.Setup(this, delay);
        }

        /// <summary>Return all active objects to the pool.</summary>
        public void ReturnAll()
        {
            // Copy to avoid mutation during iteration
            var list = new List<GameObject>(_active);
            foreach (var obj in list)
                Return(obj);
        }

        /// <summary>Destroy all pooled objects (active and available).</summary>
        public void DestroyAll()
        {
            foreach (var obj in _active)
            {
                if (obj != null) UnityEngine.Object.Destroy(obj);
            }
            foreach (var obj in _available)
            {
                if (obj != null) UnityEngine.Object.Destroy(obj);
            }
            _active.Clear();
            _available.Clear();
        }

        // =====================================================================
        // Internals
        // =====================================================================

        private void PreWarm(int count)
        {
            for (int i = 0; i < count; i++)
                CreateOne();
        }

        private void Expand(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_maxSize > 0 && (_available.Count + _active.Count) >= _maxSize)
                    break;
                CreateOne();
            }
        }

        private void CreateOne()
        {
            var obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            obj.SetActive(false);
            obj.name = $"{_prefab.name}_pooled";
            _available.Enqueue(obj);
        }
    }

    // =========================================================================
    // Typed pool wrapper (convenience for common types)
    // =========================================================================

    /// <summary>
    /// A pool that returns a specific component type instead of bare GameObjects.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly ObjectPool _inner;

        public ObjectPool(T prefab, int preWarm = 10, Transform parent = null,
            int expandStep = 5, int maxSize = 0)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            _inner = new ObjectPool(prefab.gameObject, preWarm, parent, expandStep, maxSize);
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            var go = _inner.Get(position, rotation);
            return go != null ? go.GetComponent<T>() : null;
        }

        public T Get(Vector3 position) => Get(position, Quaternion.identity);

        public void Return(T obj)
        {
            if (obj != null) _inner.Return(obj.gameObject);
        }

        public void ReturnDelayed(T obj, float delay)
        {
            if (obj != null) _inner.ReturnDelayed(obj.gameObject, delay);
        }

        public void ReturnAll() => _inner.ReturnAll();
        public void DestroyAll() => _inner.DestroyAll();
        public int ActiveCount => _inner.ActiveCount;
        public int AvailableCount => _inner.AvailableCount;
    }

    // =========================================================================
    // Auto-return helper MonoBehaviour
    // =========================================================================

    /// <summary>
    /// Attached to pooled objects to auto-return them after a delay.
    /// </summary>
    public class PooledAutoReturn : MonoBehaviour
    {
        private ObjectPool _pool;
        private float _delay;
        private float _timer;
        private bool _active;

        public void Setup(ObjectPool pool, float delay)
        {
            _pool = pool;
            _delay = delay;
            _timer = 0f;
            _active = true;
        }

        private void Update()
        {
            if (!_active) return;
            _timer += Time.deltaTime;
            if (_timer >= _delay)
            {
                _active = false;
                _pool?.Return(gameObject);
            }
        }

        private void OnDisable()
        {
            _active = false;
        }
    }
}
