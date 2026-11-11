using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOdyssey
{
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Marble Odyssey/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public struct SoundEntry
        {
            public string Name;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume;
            [Range(0.5f, 2f)] public float PitchMin;
            [Range(0.5f, 2f)] public float PitchMax;
        }

        [SerializeField] private SoundEntry[] _sounds = new SoundEntry[0];

        private Dictionary<string, SoundEntry> _lookup;

        public AudioClip GetClip(string name)
        {
            BuildLookup();
            return _lookup.TryGetValue(name, out var entry) ? entry.Clip : null;
        }

        public SoundEntry? GetEntry(string name)
        {
            BuildLookup();
            return _lookup.TryGetValue(name, out var entry) ? entry : (SoundEntry?)null;
        }

        private void BuildLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<string, SoundEntry>(_sounds.Length);
            foreach (var s in _sounds)
            {
                if (!string.IsNullOrEmpty(s.Name) && !_lookup.ContainsKey(s.Name))
                    _lookup[s.Name] = s;
            }
        }

        private void OnValidate()
        {
            _lookup = null; // Rebuild on changes in editor
        }
    }
}
