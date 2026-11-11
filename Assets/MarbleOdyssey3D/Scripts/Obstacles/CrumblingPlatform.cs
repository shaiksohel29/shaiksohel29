using System.Collections;
using UnityEngine;

namespace MarbleOdyssey
{
    public class CrumblingPlatform : ObstacleBase
    {
        [Header("Crumble")]
        [SerializeField] private float _crumbleDelay = 1.5f;
        [SerializeField] private float _respawnTime = 5f;
        [SerializeField] private float _shakeAmount = 0.05f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _crumbleParticles;
        [SerializeField] private AudioClip _crumbleSound;
        [SerializeField] private AudioClip _warningSound;

        private Renderer _renderer;
        private Collider _collider;
        private Vector3 _originalPosition;
        private bool _isCrumbling;
        private Coroutine _crumbleRoutine;

        protected override void Start()
        {
            base.Start();
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider>();
            _originalPosition = transform.localPosition;
        }

        protected override void ObstacleUpdate() { }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isActive || _isCrumbling || !collision.collider.CompareTag("Player")) return;
            _crumbleRoutine = StartCoroutine(CrumbleSequence());
        }

        private IEnumerator CrumbleSequence()
        {
            _isCrumbling = true;

            if (_warningSound != null)
                AudioSource.PlayClipAtPoint(_warningSound, transform.position, 0.6f);

            float elapsed = 0f;
            while (elapsed < _crumbleDelay)
            {
                float intensity = elapsed / _crumbleDelay;
                Vector3 shake = Random.insideUnitSphere * _shakeAmount * intensity;
                transform.localPosition = _originalPosition + shake;
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_crumbleParticles != null) _crumbleParticles.Play();
            if (_crumbleSound != null)
                AudioSource.PlayClipAtPoint(_crumbleSound, transform.position);

            if (_renderer != null) _renderer.enabled = false;
            if (_collider != null) _collider.enabled = false;

            yield return new WaitForSeconds(_respawnTime);

            transform.localPosition = _originalPosition;
            if (_renderer != null) _renderer.enabled = true;
            if (_collider != null) _collider.enabled = true;
            _isCrumbling = false;
        }

        public override void ResetObstacle()
        {
            base.ResetObstacle();
            if (_crumbleRoutine != null) StopCoroutine(_crumbleRoutine);
            _isCrumbling = false;
            transform.localPosition = _originalPosition;
            if (_renderer != null) _renderer.enabled = true;
            if (_collider != null) _collider.enabled = true;
        }
    }
}
