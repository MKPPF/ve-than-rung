using TopDownGame.Combat;
using UnityEngine;

namespace TopDownGame.Skills
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class SkillProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float lifetime = 4f;
        [SerializeField, Min(0f)] private float explosionRadius = 0f;

        private Rigidbody2D body;
        private GameObject source;
        private float damage;
        private bool fireball;
        private Color effectColor = Color.white;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Launch(Vector2 direction, float speed, float projectileDamage,
            GameObject projectileSource, float areaRadius = 0f, bool isFireball = false,
            Color projectileEffectColor = default(Color))
        {
            source = projectileSource;
            damage = projectileDamage;
            explosionRadius = areaRadius;
            fireball = isFireball;
            effectColor = projectileEffectColor == default(Color) ? Color.white : projectileEffectColor;
            body.linearVelocity = direction.normalized * speed;
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target == null || other.CompareTag("Player"))
            {
                return;
            }

            Vector2 point = other.ClosestPoint(transform.position);
            SkillEffect.SpawnBurst(point, effectColor, fireball ? explosionRadius : 0.65f);
            target.TakeDamage(new DamageInfo(damage, source, point,
                body.linearVelocity.normalized, 2f));

            if (fireball && explosionRadius > 0f)
            {
                Collider2D[] nearby = Physics2D.OverlapCircleAll(point, explosionRadius);
                foreach (Collider2D nearbyCollider in nearby)
                {
                    IDamageable nearbyTarget = nearbyCollider.GetComponentInParent<IDamageable>();
                    if (nearbyTarget != null && nearbyTarget != target &&
                        !nearbyCollider.CompareTag("Player"))
                    {
                        nearbyTarget.TakeDamage(new DamageInfo(damage * 0.5f, source,
                            nearbyCollider.ClosestPoint(point), nearbyCollider.transform.position -
                            (Vector3)point));
                    }
                }
            }

            Destroy(gameObject);
        }
    }
}
