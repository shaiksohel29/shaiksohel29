using System.Collections;
using UnityEngine;

namespace MarbleOdyssey
{
    public class Teleporter : ObstacleBase
    {
        [Header("Teleporter")]
        [SerializeField] private Teleporter _linkedTeleporter;
        [SerializeField] private float _teleportDelay = 0.2f;
        [SerializeField] private float _exitVelocityMultiplier = 1f;
        [SerializeField] private float _cooldown = 1f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _enterEffect;
        [SerializeField] private ParticleSystem _exitEffect;
        [SerializeField] private AudioClip _teleportSound;

        private float _lastTeleportTime = -10f;
        public bool IsOnCooldown => Time.time - _lastTeleportTime < _cooldown;

        protected override void ObstacleUpdate() { }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || !other.CompareTag("Player")) return;
            if (_linkedTeleporter == null || IsOnCooldown || _linkedTeleporter.IsOnCooldown) return;

            StartCoroutine(TeleportSequence(other.gameObject));
        }

        private IEnumerator TeleportSequence(GameObject marble)
        {
            _lastTeleportTime = Time.time;
            _linkedTeleporter._lastTeleportTime = Time.time;

            Rigidbody rb = marble.GetComponent<Rigidbody>();
            Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;

            if (_enterEffect != null) _enterEffect.Play();
            if (_teleportSound != null)
                AudioSource.PlayClipAtPoint(_teleportSound, transform.position);

            if (rb != null)
            {
                rb.isKinematic = true;
                marble.GetComponent<Collider>().enabled = false;
            }

            yield return new WaitForSeconds(_teleportDelay);

            Vector3 exitPos = _linkedTeleporter.transform.position + Vector3.up * 0.5f;
            marble.transform.position = exitPos;

            if (rb != null)
            {
                rb.isKinematic = false;
                marble.GetComponent<Collider>().enabled = true;
                rb.linearVelocity = _linkedTeleporter.transform.forward * velocity.magnitude * _exitVelocityMultiplier;
            }

            if (_linkedTeleporter._exitEffect != null)
                _linkedTeleporter._exitEffect.Play();
        }

        public override void ResetObstacle()
        {
            base.ResetObstacle();
            _lastTeleportTime = -10f;
            StopAllCoroutines();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_linkedTeleporter == null) return;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, _linkedTeleporter.transform.position);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawWireSphere(_linkedTeleporter.transform.position, 0.5f);
        }
#endif
    }
}
