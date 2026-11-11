using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey
{
    public class ParticlePoolManager : MonoBehaviour
    {
        private static ParticlePoolManager _instance;
        public static ParticlePoolManager Instance => _instance;

        [System.Serializable]
        public struct PoolEntry
        {
            public string Key;
            public ParticleSystem Prefab;
            public int PreWarm;
        }

        [SerializeField] private PoolEntry[] _pools;

        private readonly Dictionary<string, ObjectPool<ParticleSystem>> _poolMap = new();

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_pools == null) return;
            foreach (var entry in _pools)
            {
                if (entry.Prefab == null || string.IsNullOrEmpty(entry.Key)) continue;
                _poolMap[entry.Key] = new ObjectPool<ParticleSystem>(
                    entry.Prefab, entry.PreWarm > 0 ? entry.PreWarm : 5, transform);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public ParticleSystem Spawn(string key, Vector3 position, Quaternion rotation)
        {
            if (!_poolMap.TryGetValue(key, out var pool)) return null;
            var ps = pool.Get(position, rotation);
            if (ps != null)
            {
                ps.Play();
                pool.ReturnDelayed(ps, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            return ps;
        }

        public ParticleSystem Spawn(string key, Vector3 position) => Spawn(key, position, Quaternion.identity);

        public void ReturnAllPools()
        {
            foreach (var pool in _poolMap.Values) pool.ReturnAll();
        }
    }
}
