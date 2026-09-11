using System.Collections.Generic;
using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Enemies.Spawning
{
    public sealed class EnemyProjectilePool : MonoBehaviour
    {
        private const int MaximumProjectiles = 72;
        private static EnemyProjectilePool instance;
        private readonly List<EnemyProjectile> projectiles =
            new List<EnemyProjectile>(MaximumProjectiles);
        private int recycleCursor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (instance != null) return;
            GameObject root = new GameObject(nameof(EnemyProjectilePool));
            instance = root.AddComponent<EnemyProjectilePool>();
            DontDestroyOnLoad(root);
        }

        public static EnemyProjectile Acquire(EnemyProjectile prefab,
            Vector3 position, Vector2 direction, Color color, Vector3 scale)
        {
            EnsureInstance();
            EnemyProjectile projectile = null;
            for (int i = 0; i < instance.projectiles.Count; i++)
            {
                if (instance.projectiles[i] != null &&
                    !instance.projectiles[i].gameObject.activeSelf)
                {
                    projectile = instance.projectiles[i];
                    break;
                }
            }

            if (projectile == null)
            {
                if (instance.projectiles.Count >= MaximumProjectiles)
                {
                    projectile = instance.projectiles[
                        instance.recycleCursor++ % instance.projectiles.Count];
                    projectile.gameObject.SetActive(false);
                }
                else
                {
                    projectile = prefab != null
                        ? Instantiate(prefab, instance.transform)
                        : instance.CreateRuntimeProjectile();
                    projectile.SetPool(instance);
                    instance.projectiles.Add(projectile);
                }
            }

            projectile.transform.position = position;
            projectile.transform.localScale = scale;
            projectile.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = color;
            projectile.gameObject.SetActive(true);
            return projectile;
        }

        public void Release(EnemyProjectile projectile)
        {
            if (projectile != null) projectile.gameObject.SetActive(false);
        }

        private EnemyProjectile CreateRuntimeProjectile()
        {
            GameObject projectileObject = new GameObject("PooledEnemyProjectile");
            projectileObject.transform.SetParent(transform, false);
            SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.SoftGlowSprite;
            renderer.sortingOrder = 12;
            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.42f;
            Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            return projectileObject.AddComponent<EnemyProjectile>();
        }
    }
}
