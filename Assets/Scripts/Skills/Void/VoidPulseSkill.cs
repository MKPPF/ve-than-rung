using System.Collections.Generic;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Enemies;
using UnityEngine;

namespace TopDownGame.Skills
{
    /// <summary>
    /// A close-range control spell that pulls enemies into follow-up attacks
    /// instead of behaving as another interchangeable radial damage button.
    /// </summary>
    public sealed class VoidPulseSkill : SkillBase
    {
        [SerializeField, Min(0f)] private float pullStrength = 5.5f;
        [SerializeField] private LayerMask enemyLayers = ~0;

        public override void Cast()
        {
            float radius = ScaledRange();
            var hitTargets = new HashSet<ulong>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position,
                radius, enemyLayers))
            {
                EnemyHealth health = hit.GetComponentInParent<EnemyHealth>();
                if (health == null || !health.IsAlive ||
                    !hitTargets.Add(EntityId.ToULong(
                        health.gameObject.GetEntityId())))
                {
                    continue;
                }

                Vector2 inward = (Vector2)transform.position -
                    (Vector2)health.transform.position;
                health.TakeDamage(new DamageInfo(ScaledDamage(), gameObject,
                    hit.ClosestPoint(transform.position), inward, 0f,
                    false, true));
                health.GetComponent<EnemyMovement>()?.AddImpulse(
                    inward.normalized * pullStrength);
            }

            SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Impact,transform.position,radius*2.1f,0.65f);
        }
    }

    internal sealed class VoidPulseEffect : MonoBehaviour
    {
        private SpriteRenderer inner;
        private SpriteRenderer outer;
        private float radius;
        private float elapsed;

        public static void Spawn(Vector3 position, float radius, Color color)
        {
            GameObject root = new GameObject("VoidPulseEffect");
            root.transform.position = new Vector3(position.x, position.y, -0.2f);
            VoidPulseEffect effect = root.AddComponent<VoidPulseEffect>();
            effect.radius = radius;
            effect.inner = CreateRing(root.transform, color, 34);
            Color cyan = Color.Lerp(color, Color.cyan, 0.45f);
            effect.outer = CreateRing(root.transform, cyan, 33);
        }

        private static SpriteRenderer CreateRing(Transform parent, Color color,
            int sortingOrder)
        {
            GameObject layer = new GameObject("PulseRing");
            layer.transform.SetParent(parent, false);
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.RingSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.45f);
            SetRing(inner, Mathf.Lerp(radius * 2f, 0.15f, t), 1f - t);
            SetRing(outer, Mathf.Lerp(radius * 2.35f, 0.4f, t),
                (1f - t) * 0.7f);
            if (t >= 1f) Destroy(gameObject);
        }

        private static void SetRing(SpriteRenderer renderer, float scale,
            float alpha)
        {
            renderer.transform.localScale = Vector3.one * scale;
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
