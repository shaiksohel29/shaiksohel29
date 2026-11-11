// ============================================================================
// Marble Odyssey 3D — Collectibles (Gem, Star)
// Hover animation, magnetic pull, collection burst.
// ============================================================================

using UnityEngine;

namespace MarbleOdyssey
{
    /// <summary>
    /// Base class for all collectible items.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class Collectible : MonoBehaviour
    {
        [Header("Hover Animation")]
        [SerializeField] protected float bobHeight = 0.25f;
        [SerializeField] protected float bobSpeed = 2f;
        [SerializeField] protected float rotateSpeed = 90f;

        [Header("Magnetic Pull")]
        [SerializeField] protected float magnetRange = 3f;
        [SerializeField] protected float magnetForce = 8f;
        [SerializeField] protected bool enableMagnet = true;

        [Header("Collection Effects")]
        [SerializeField] protected ParticleSystem collectParticles;
        [SerializeField] protected AudioClip collectSound;
        [SerializeField] [Range(0f, 1f)] protected float soundVolume = 0.7f;

        protected Vector3 _startPos;
        protected bool _collected;
        protected Transform _marbleTransform;

        protected virtual void Start()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
            _startPos = transform.position;
        }

        protected virtual void Update()
        {
            if (_collected) return;

            // Bob up and down
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = _startPos + Vector3.up * bobOffset;

            // Rotate
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // Magnetic pull toward marble
            if (enableMagnet && _marbleTransform == null)
            {
                var marble = FindFirstObjectByType<MarbleController>();
                if (marble != null) _marbleTransform = marble.transform;
            }

            if (enableMagnet && _marbleTransform != null)
            {
                float dist = Vector3.Distance(transform.position, _marbleTransform.position);
                if (dist < magnetRange && dist > 0.1f)
                {
                    float pull = magnetForce * (1f - dist / magnetRange);
                    Vector3 dir = (_marbleTransform.position - transform.position).normalized;
                    _startPos += dir * pull * Time.deltaTime;
                }
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (_collected) return;
            if (!other.CompareTag("Player")) return;

            _collected = true;
            OnCollected();

            // Particles
            if (collectParticles != null)
            {
                var ps = Instantiate(collectParticles, transform.position, Quaternion.identity);
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + 1f);
            }

            // Sound
            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);

            // Hide and schedule destroy
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Override to handle game logic when collected.
        /// </summary>
        protected abstract void OnCollected();
    }

    // =========================================================================
    // Gem Collectible
    // =========================================================================
    public class Gem : Collectible
    {
        public enum GemTier
        {
            Bronze = 1,
            Silver = 5,
            Gold = 10,
            Diamond = 25
        }

        [Header("Gem")]
        [SerializeField] private GemTier _tier = GemTier.Bronze;

        public int Value => (int)_tier;

        protected override void OnCollected()
        {
            LevelManager.Instance?.CollectGem(Value);
            // Also add to the player's persistent gem wallet
            int current = PlayerPrefs.GetInt("TotalGems", 0);
            PlayerPrefs.SetInt("TotalGems", current + Value);
        }
    }

    // =========================================================================
    // Star Collectible — Hidden bonus, one per level
    // =========================================================================
    public class HiddenStar : Collectible
    {
        [Header("Hidden Star")]
        [SerializeField] private int _bonusGems = 50;
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField] private float _pulseScale = 0.15f;

        private Vector3 _baseScale;

        protected override void Start()
        {
            base.Start();
            _baseScale = transform.localScale;
        }

        protected override void Update()
        {
            base.Update();
            if (_collected) return;

            // Pulsing scale
            float pulse = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseScale;
            transform.localScale = _baseScale * pulse;
        }

        protected override void OnCollected()
        {
            LevelManager.Instance?.CollectGem(_bonusGems);
            int current = PlayerPrefs.GetInt("TotalGems", 0);
            PlayerPrefs.SetInt("TotalGems", current + _bonusGems);
            Debug.Log("[Collectible] Hidden star found! Bonus gems awarded.");
        }
    }
}
