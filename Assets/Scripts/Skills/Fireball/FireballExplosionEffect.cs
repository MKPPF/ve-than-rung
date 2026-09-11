using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Skills
{
    public sealed class FireballExplosionEffect : MonoBehaviour
    {
        private SpriteRenderer glow;
        private SpriteRenderer core;
        private SpriteRenderer ring;
        private float radius;
        private float duration;
        private float elapsed;

        public static void Spawn(Vector3 position, float explosionRadius)
        {
            GameObject root = new GameObject("FireballExplosion");
            root.transform.position = new Vector3(position.x, position.y, -0.2f);
            FireballExplosionEffect effect = root.AddComponent<FireballExplosionEffect>();
            effect.radius = Mathf.Max(0.25f, explosionRadius);
            effect.duration = 0.38f;
            effect.glow = CreateLayer(root.transform, "Glow", RuntimeSpriteFactory.SoftGlowSprite,
                new Color(1f, 0.18f, 0.02f, 0.8f), 31);
            effect.core = CreateLayer(root.transform, "Core", RuntimeSpriteFactory.CircleSprite,
                new Color(1f, 0.86f, 0.22f, 0.95f), 32);
            effect.ring = CreateLayer(root.transform, "Shockwave", RuntimeSpriteFactory.RingSprite,
                new Color(1f, 0.35f, 0.03f, 0.95f), 33);
        }

        private static SpriteRenderer CreateLayer(Transform parent, string layerName,
            Sprite sprite, Color color, int sortingOrder)
        {
            GameObject layer = new GameObject(layerName);
            layer.transform.SetParent(parent, false);
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);

            SetLayer(glow, Mathf.Lerp(0.15f, radius * 2.5f, eased), 0.8f * (1f - t));
            SetLayer(core, Mathf.Lerp(0.1f, radius * 1.25f, eased), 0.95f * (1f - t));
            SetLayer(ring, Mathf.Lerp(0.2f, radius * 2.2f, eased), 0.95f * (1f - t));

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private static void SetLayer(SpriteRenderer renderer, float scale, float alpha)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.localScale = Vector3.one * scale;
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
