using System.Collections.Generic;
using TopDownGame.Combat;
using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Skills
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class FireballProjectile2D : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float lifeTime = 4f;

        private Rigidbody2D body;
        private Vector2 moveDirection;
        private float damage;
        private float explosionRadius;
        private LayerMask enemyLayers;
        private LayerMask collisionLayers;
        private GameObject owner;
        private Material runtimeMaterial;
        private bool hasExploded;
        private SkillPresentationProfile presentation;
        public void SetPresentation(SkillPresentationProfile profile) => presentation=profile;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Initialize(Vector2 direction, float speed, float fireballDamage,
            float radius, LayerMask enemies, LayerMask collisions, GameObject skillOwner)
        {
            moveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            damage = Mathf.Max(0f, fireballDamage);
            explosionRadius = Mathf.Max(0.1f, radius);
            enemyLayers = enemies;
            collisionLayers = collisions;
            owner = skillOwner;
            body.linearVelocity = moveDirection * Mathf.Max(0f, speed);
            Destroy(gameObject, lifeTime);
        }

        public void OwnRuntimeMaterial(Material material)
        {
            runtimeMaterial = material;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasExploded || IsOwner(other.transform))
            {
                return;
            }

            IDamageable target = other.GetComponentInParent<IDamageable>();
            bool isEnemy = target != null && target.IsAlive &&
                !other.CompareTag("Player") && IsLayerInMask(other.gameObject.layer, enemyLayers);
            bool isCollision = !other.isTrigger &&
                IsLayerInMask(other.gameObject.layer, collisionLayers);

            if (isEnemy || isCollision)
            {
                Explode();
            }
        }

        private bool IsOwner(Transform target)
        {
            return owner != null && (target.gameObject == owner || target.IsChildOf(owner.transform));
        }

        private void Explode()
        {
            if (hasExploded)
            {
                return;
            }

            hasExploded = true;
            body.linearVelocity = Vector2.zero;
            DamageEnemiesInRadius();
            SpellVfx.Spawn(presentation,SpellVfxStage.Impact,transform.position,explosionRadius*2.5f,0.5f);
            Destroy(gameObject);
        }

        private void DamageEnemiesInRadius()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position, explosionRadius, enemyLayers);
            HashSet<int> damagedTargets = new HashSet<int>();

            foreach (Collider2D hit in hits)
            {
                if (IsOwner(hit.transform) || hit.CompareTag("Player"))
                {
                    continue;
                }

                IDamageable target = hit.GetComponentInParent<IDamageable>();
                int id = hit.transform.root.gameObject.GetHashCode();
                if (target == null || !target.IsAlive || !damagedTargets.Add(id))
                {
                    continue;
                }

                Vector2 hitDirection = (Vector2)hit.transform.position - (Vector2)transform.position;
                target.TakeDamage(new DamageInfo(damage, owner,
                    hit.ClosestPoint(transform.position), hitDirection, 2f));
                SpawnBurningVisual(hit.transform.root);
            }
        }

        private static void SpawnBurningVisual(Transform target)
        {
            if (target == null)
            {
                return;
            }

            GameObject burning = new GameObject("BurningEffect");
            burning.transform.SetParent(target, false);
            burning.transform.localPosition = Vector3.up * 0.15f;
            burning.transform.localScale = Vector3.one * 0.65f;
            SpriteRenderer renderer = burning.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.SoftGlowSprite;
            renderer.color = new Color(1f, 0.2f, 0.02f, 0.72f);
            renderer.sortingOrder = 28;
            burning.AddComponent<FireballVisualEffect>();
            Destroy(burning, 1.5f);
        }

        private static bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.05f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }
    }
}
