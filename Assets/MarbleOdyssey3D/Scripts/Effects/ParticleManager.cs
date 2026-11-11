using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey
{
    public enum EffectType
    {
        GemCollect,
        CheckpointActivate,
        LevelComplete,
        MarbleDeath,
        SpeedBoostTrail,
        JumpPadLaunch,
        LaserHit,
        MarbleLand,
        StarBurst,
        TeleportFlash,
        CrumbleDust
    }

    public class ParticleManager : MonoBehaviour
    {
        private static ParticleManager _instance;
        public static ParticleManager Instance => _instance;

        [Serializable]
        public struct EffectEntry
        {
            public EffectType Type;
            public GameObject Prefab;
            public int PoolSize;
        }

        [SerializeField] private EffectEntry[] _effects;

        private readonly Dictionary<EffectType, Queue<ParticleSystem>> _pools = new();
        private readonly Dictionary<EffectType, GameObject> _prefabMap = new();

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitPools();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitPools()
        {
            if (_effects == null) return;
            foreach (var entry in _effects)
            {
                if (entry.Prefab == null) continue;
                _prefabMap[entry.Type] = entry.Prefab;
                var queue = new Queue<ParticleSystem>();
                int size = Mathf.Max(entry.PoolSize, 2);
                for (int i = 0; i < size; i++)
                {
                    var go = Instantiate(entry.Prefab, transform);
                    go.SetActive(false);
                    var ps = go.GetComponent<ParticleSystem>();
                    if (ps != null) queue.Enqueue(ps);
                }
                _pools[entry.Type] = queue;
            }
        }

        public ParticleSystem SpawnEffect(EffectType type, Vector3 position)
        {
            return SpawnEffect(type, position, Quaternion.identity);
        }

        public ParticleSystem SpawnEffect(EffectType type, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(type, out var queue) || queue.Count == 0)
            {
                if (_prefabMap.TryGetValue(type, out var prefab))
                {
                    var go = Instantiate(prefab, position, rotation, transform);
                    var ps = go.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Play();
                        ScheduleReturn(ps, type);
                    }
                    return ps;
                }
                return null;
            }

            var pooled = queue.Dequeue();
            pooled.transform.SetPositionAndRotation(position, rotation);
            pooled.gameObject.SetActive(true);
            pooled.Play();
            ScheduleReturn(pooled, type);
            return pooled;
        }

        private void ScheduleReturn(ParticleSystem ps, EffectType type)
        {
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            StartCoroutine(ReturnAfterDelay(ps, type, duration));
        }

        private IEnumerator ReturnAfterDelay(ParticleSystem ps, EffectType type, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (ps == null) yield break;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
            if (_pools.TryGetValue(type, out var queue))
                queue.Enqueue(ps);
        }
    }
}
