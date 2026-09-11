using TopDownGame.Combat;
using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Player
{
    /// <summary>A swept, single-hit energy shot cannot tunnel through thin walls or hit its owner.</summary>
    public sealed class PlayerShot : MonoBehaviour
    {
        private GameObject owner;
        private Vector2 direction;
        private float speed, remaining, damage, knockback;
        private bool critical;
        private LayerMask enemies;
        public Vector2 Direction => direction;

        public static PlayerShot Spawn(GameObject source, Vector2 aim, float velocity,
            float range, float amount, float force, bool isCritical, LayerMask enemyLayers)
        {
            GameObject go = new GameObject("PlayerShot");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, source.scene);
            go.transform.position = source.transform.position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            var shot = go.AddComponent<PlayerShot>();
            shot.owner = source;
            shot.direction = aim.normalized;
            shot.speed = velocity;
            shot.remaining = range;
            shot.damage = amount;
            shot.knockback = force;
            shot.critical = isCritical;
            shot.enemies = enemyLayers;
            AddVisual(go.transform, "Glow", new Vector3(0.6f, 0.3f, 1f),
                new Color(0.2f, 0.85f, 1f, 0.7f), RuntimeSpriteFactory.SoftGlowSprite);
            AddVisual(go.transform, "Core", new Vector3(0.3f, 0.1f, 1f),
                new Color(0.8f, 1f, 1f), RuntimeSpriteFactory.CircleSprite);
            return shot;
        }

        private static void AddVisual(Transform parent, string label, Vector3 scale, Color tint, Sprite sprite)
        {
            var child = new GameObject(label);
            child.transform.SetParent(parent, false);
            child.transform.localScale = scale;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = 20;
        }

        private void FixedUpdate()
        {
            if (GameplayState.IsInputBlocked || Time.timeScale <= 0f) return;
            float step = Mathf.Min(speed * Time.fixedDeltaTime, remaining);
            // CircleCastAll is distance ordered, so a wall in front wins over an enemy behind it.
            foreach (RaycastHit2D hit in Physics2D.CircleCastAll(transform.position, 0.07f, direction, step))
            {
                Collider2D other = hit.collider;
                if (owner != null && other.transform.IsChildOf(owner.transform)) continue;
                if (other.GetComponentInParent<PlayerHealth>() != null) continue;
                IDamageable target = other.GetComponentInParent<IDamageable>();
                bool enemy = target != null && target.IsAlive &&
                    (enemies.value & (1 << other.gameObject.layer)) != 0;
                if (!enemy && other.isTrigger) continue;
                if (enemy) target.TakeDamage(new DamageInfo(damage, owner, hit.point,
                    direction, knockback, critical));
                Destroy(gameObject);
                return;
            }
            transform.position += (Vector3)(direction * step);
            remaining -= step;
            if (remaining <= 0f) Destroy(gameObject);
        }
    }
}
