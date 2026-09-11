using UnityEngine;

namespace TopDownGame.Combat
{
    public sealed class Hurtbox : MonoBehaviour, IDamageable
    {
        [SerializeField] private MonoBehaviour damageableComponent;
        private IDamageable damageable;

        public bool IsAlive => damageable != null && damageable.IsAlive;

        private void Awake()
        {
            damageable = damageableComponent as IDamageable;
            if (damageable == null)
            {
                damageable = GetComponentInParent<IDamageable>();
            }
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            damageable?.TakeDamage(damageInfo);
        }
    }
}
