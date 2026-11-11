using UnityEngine;

namespace MarbleOdyssey
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new();
        private static bool _quitting;

        public static T Instance
        {
            get
            {
                if (_quitting) return null;
                lock (_lock)
                {
                    if (_instance != null) return _instance;
                    _instance = FindAnyObjectByType<T>();
                    if (_instance != null) return _instance;

                    var go = new GameObject($"[{typeof(T).Name}]");
                    _instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this as T)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit() => _quitting = true;

        protected virtual void OnDestroy()
        {
            if (_instance == this as T) _instance = null;
        }
    }
}
