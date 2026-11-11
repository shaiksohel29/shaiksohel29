using System;
using UnityEngine;

namespace MarbleOdyssey
{
    [CreateAssetMenu(fileName = "MusicLibrary", menuName = "Marble Odyssey/Audio/Music Library")]
    public class MusicLibrary : ScriptableObject
    {
        [Serializable]
        public struct WorldMusic
        {
            public string WorldName;
            public AudioClip Track;
        }

        [Header("Menu")]
        public AudioClip MainMenuTrack;
        public AudioClip WorldSelectTrack;
        public AudioClip DailyChallengeTrack;

        [Header("Worlds")]
        public WorldMusic[] WorldTracks;

        public AudioClip GetWorldTrack(int worldIndex)
        {
            if (WorldTracks == null || worldIndex < 0 || worldIndex >= WorldTracks.Length)
                return null;
            return WorldTracks[worldIndex].Track;
        }
    }
}
