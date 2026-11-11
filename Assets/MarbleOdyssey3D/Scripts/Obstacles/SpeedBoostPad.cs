using UnityEngine;

namespace MarbleOdyssey
{
    [RequireComponent(typeof(BoxCollider))]
    public class SpeedBoostPad : MonoBehaviour
    {
        [SerializeField] private float _boostForce = 25f;
        [SerializeField] private float _cooldown = 0.5f;
        [SerializeField] private ParticleSystem _boostParticles;
        [SerializeField] private Renderer[] _arrowRenderers;

        private float _lastBoostTime = -10f;
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        private void OnValidate()
        {
            var col = GetComponent<BoxCollider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Marble")) return;
            if (Time.time - _lastBoostTime < _cooldown) return;

            var rb = other.attachedRigidbody;
            if (rb == null) return;

            _lastBoostTime = Time.time;

            Vector3 boostDir = transform.forward;
            rb.AddForce(boostDir * _boostForce, ForceMode.VelocityChange);

            if (_boostParticles != null) _boostParticles.Play();

            AudioManager.Instance?.PlaySFX(AudioManager.Instance.SFXLib?.SpeedBoost);
            ScreenShakeEffect.ShakeOnBoost();

            ParticleManager.Instance?.SpawnEffect(EffectType.SpeedBoostTrail, transform.position, transform.rotation);
        }
    }
}
