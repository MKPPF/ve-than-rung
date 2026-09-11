using TopDownGame.Combat;
using TopDownGame.Enemies.Progression;
using TopDownGame.Enemies.Spawning;
using UnityEngine;

namespace TopDownGame.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float lifetime = 5f;
        [SerializeField, Min(0f)] private float knockback = 1.5f;

        private Rigidbody2D body;
        private float damage;
        private float expiresAt;
        private GameObject source;
        private EnemyProjectilePool owner;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Update()
        {
            if (Time.time >= expiresAt) Release();
        }

        public void SetPool(EnemyProjectilePool projectilePool)
        {
            owner = projectilePool;
        }

        public void Launch(Vector2 direction, float speed, float projectileDamage,
            GameObject projectileSource, float customLifetime = -1f)
        {
            damage = projectileDamage;
            source = projectileSource;
            expiresAt = Time.time + (customLifetime > 0f
                ? customLifetime
                : lifetime);
            body.simulated = true;
            body.linearVelocity = direction.normalized * speed;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            IDamageable target = other.GetComponentInParent<IDamageable>();
            Vector2 direction = body.linearVelocity.normalized;
            target?.TakeDamage(new DamageInfo(damage, source,
                other.ClosestPoint(transform.position), direction, knockback));
            source?.GetComponent<EnemyRuntimeProfile>()?.ApplyLifeSteal(damage);
            Release();
        }

        private void Release()
        {
            if (!gameObject.activeSelf) return;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            source = null;
            if (owner != null) owner.Release(this);
            else Destroy(gameObject);
        }
    }
}
