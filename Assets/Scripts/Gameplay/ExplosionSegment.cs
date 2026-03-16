using UnityEngine;

namespace Bomber.Gameplay
{
    public sealed class ExplosionSegment : MonoBehaviour
    {
        public void Initialize()
        {
            ApplyDamage();
            Destroy(gameObject, 0.35f);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryAffect(other);
        }

        private void ApplyDamage()
        {
            Collider[] hits = Physics.OverlapBox(transform.position, transform.localScale * 0.5f, transform.rotation);
            foreach (Collider hit in hits)
            {
                TryAffect(hit);
            }
        }

        private static void TryAffect(Collider other)
        {
            if (other.TryGetComponent(out Bomb bomb))
            {
                bomb.DetonateNow();
            }

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeHit(other.gameObject);
            }
        }
    }
}
