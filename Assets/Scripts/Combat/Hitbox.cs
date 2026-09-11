using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Combat
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private bool hitOncePerActivation = true;

        private readonly HashSet<int> hitTargets = new HashSet<int>();
        private DamageInfo damageInfo;
        private bool armed;

        private void Awake()
        {
            Collider2D hitboxCollider = GetComponent<Collider2D>();
            hitboxCollider.isTrigger = true;
        }

        private void OnDisable()
        {
            armed = false;
            hitTargets.Clear();
        }

        public void Arm(DamageInfo info)
        {
            damageInfo = info;
            hitTargets.Clear();
            armed = true;
        }

        public void Disarm()
        {
            armed = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!armed || (targetLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive)
            {
                return;
            }

            int targetId = other.transform.root.gameObject.GetHashCode();
            if (hitOncePerActivation && !hitTargets.Add(targetId))
            {
                return;
            }

            target.TakeDamage(damageInfo);
        }
    }
}
