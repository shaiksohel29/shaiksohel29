using UnityEngine;

namespace MarbleOdyssey
{
    [CreateAssetMenu(fileName = "SFXLibrary", menuName = "Marble Odyssey/Audio/SFX Library")]
    public class SFXLibrary : ScriptableObject
    {
        [Header("Marble")]
        public AudioClip MarbleRoll;
        public AudioClip MarbleJump;
        public AudioClip MarbleBounce;
        public AudioClip MarbleLand;
        public AudioClip MarbleDeath;

        [Header("Collectibles")]
        public AudioClip GemCollect;
        public AudioClip CheckpointReached;
        public AudioClip StarEarned;

        [Header("Level")]
        public AudioClip LevelComplete;
        public AudioClip LevelFailed;
        public AudioClip CountdownTick;
        public AudioClip CountdownGo;

        [Header("Obstacles")]
        public AudioClip SpeedBoost;
        public AudioClip JumpPadLaunch;
        public AudioClip LaserWarning;
        public AudioClip LaserFire;
        public AudioClip CrumblePlatform;
        public AudioClip BumperHit;
        public AudioClip TeleportIn;
        public AudioClip TeleportOut;

        [Header("UI")]
        public AudioClip ButtonClick;
        public AudioClip ButtonHover;
        public AudioClip PanelOpen;
        public AudioClip PanelClose;
    }
}
