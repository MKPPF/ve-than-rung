using System.Collections.Generic;
using TopDownGame.Combat;
using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Skills
{
    public sealed class SpinningSwordEffect2D : MonoBehaviour
    {
        private readonly Dictionary<int, int> enemyHitCounts = new Dictionary<int, int>();
        private readonly HashSet<int> hitThisTick = new HashSet<int>();

        private Transform skillCenter;
        private GameObject damageSource;
        private Transform rotatingSlash;
        private Transform counterRotatingRing;
        private SpriteRenderer slashRenderer;
        private SpriteRenderer[] sparkRenderers;
        private LayerMask enemyLayers;
        private Color effectColor;
        private float damagePerHit;
        private float attackRadius;
        private float duration;
        private float damageInterval;
        private int maximumHitsPerEnemy;
        private float elapsed;
        private float nextDamageTime;
        private Vector3 slashRestScale;
        private SwordComboVisual combo;
        private int beatIndex;
        public int DamageBeats => beatIndex;
        public void SetComboPresentation(SwordComboProfile profile)
        {
            rotatingSlash.gameObject.SetActive(false);
            counterRotatingRing.gameObject.SetActive(false);
            combo=gameObject.AddComponent<SwordComboVisual>();
            combo.Initialize(profile,skillCenter,attackRadius);
        }
        public void SetPresentation(SkillPresentationProfile profile)
        {
            if(profile==null) return;
            rotatingSlash.gameObject.SetActive(false);
            counterRotatingRing.gameObject.SetActive(false);
            SpellVfx.Spawn(profile,SpellVfxStage.Aura,transform.position,attackRadius*2.5f,duration,transform);
        }

        public void Initialize(Transform center, GameObject source, Sprite slashSprite,
            Color color, float damage, float radius, float effectDuration,
            float hitInterval, int maximumHits, LayerMask enemies)
        {
            skillCenter = center;
            damageSource = source;
            effectColor = color;
            damagePerHit = Mathf.Max(0f, damage);
            attackRadius = Mathf.Max(0.5f, radius);
            duration = Mathf.Max(0.1f, effectDuration);
            damageInterval = Mathf.Max(0.02f, hitInterval);
            maximumHitsPerEnemy = Mathf.Max(1, maximumHits);
            enemyLayers = enemies;
            transform.position = CenterPosition;
            CreateVisuals(slashSprite);
        }

        private Vector3 CenterPosition => skillCenter != null
            ? new Vector3(skillCenter.position.x, skillCenter.position.y, -0.2f)
            : transform.position;

        private void Update()
        {
            if (skillCenter == null || !skillCenter.gameObject.activeInHierarchy ||
                (skillCenter.GetComponent<TopDownGame.Player.PlayerHealth>() is var health && health!=null && !health.IsAlive))
            {
                Destroy(gameObject);
                return;
            }
            if(GameplayState.IsInputBlocked || Time.timeScale<=0) return;

            elapsed += Time.deltaTime;
            transform.position = CenterPosition;
            rotatingSlash.Rotate(0f, 0f, -900f * Time.deltaTime);
            counterRotatingRing.Rotate(0f, 0f, 560f * Time.deltaTime);
            AnimateVisuals();

            while (elapsed >= nextDamageTime && nextDamageTime < duration-0.0001f)
            {
                var combat=skillCenter.GetComponent<TopDownGame.Player.PlayerCombat>();
                combo?.Pulse(beatIndex,damageInterval,combat!=null ? combat.AimDirection : Vector2.right);
                DealAreaDamage();
                beatIndex++;
                nextDamageTime += damageInterval;
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }

        private void CreateVisuals(Sprite slashSprite)
        {
            GameObject slashObject = new GameObject("RotatingSlashVisual");
            slashObject.transform.SetParent(transform, false);
            rotatingSlash = slashObject.transform;

            slashRenderer = slashObject.AddComponent<SpriteRenderer>();
            slashRenderer.sprite = slashSprite;
            slashRenderer.color = Color.white;
            slashRenderer.sortingOrder = 27;
            ScaleSpriteToDiameter(slashRenderer, attackRadius * 2.25f);
            slashRestScale=rotatingSlash.localScale;

            GameObject counterObject = new GameObject("CounterRotatingRing");
            counterObject.transform.SetParent(transform, false);
            counterRotatingRing = counterObject.transform;

            sparkRenderers = new SpriteRenderer[10];
            for (int i = 0; i < sparkRenderers.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / sparkRenderers.Length;
                float sparkRadius = attackRadius * (0.82f + (i % 3) * 0.09f);
                GameObject spark = new GameObject("SwordSpark" + i);
                spark.transform.SetParent(counterRotatingRing, false);
                spark.transform.localPosition = new Vector3(
                    Mathf.Cos(angle), Mathf.Sin(angle), 0f) * sparkRadius;
                spark.transform.localScale = new Vector3(
                    0.045f, 0.08f + (i % 2) * 0.035f, 1f);
                SpriteRenderer renderer = spark.AddComponent<SpriteRenderer>();
                renderer.sprite = RuntimeSpriteFactory.CircleSprite;
                renderer.color = Color.Lerp(Color.white, effectColor, 0.4f);
                renderer.sortingOrder = 28;
                sparkRenderers[i] = renderer;
            }
        }

        private void ScaleSpriteToDiameter(SpriteRenderer renderer, float diameter)
        {
            if (renderer.sprite == null)
            {
                renderer.gameObject.SetActive(false);
                return;
            }

            Vector2 spriteSize = renderer.sprite.bounds.size;
            float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
            float scale = largestSide > 0f ? diameter / largestSide : 1f;
            renderer.transform.localScale = Vector3.one * scale;
        }

        private void AnimateVisuals()
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float appear = Mathf.Clamp01(progress / 0.08f);
            float disappear = Mathf.Clamp01((1f - progress) / 0.16f);
            float visibility = Mathf.Min(appear, disappear);
            float pulse = 1f + Mathf.Sin(elapsed * 17f) * 0.055f;

            rotatingSlash.localScale = slashRestScale * visibility * pulse;
            counterRotatingRing.localScale = Vector3.one * visibility *
                (1f + Mathf.Sin(elapsed * 13f) * 0.035f);

            SetAlpha(slashRenderer, visibility);

            for (int i = 0; i < sparkRenderers.Length; i++)
            {
                float twinkle = 0.25f +
                    Mathf.Abs(Mathf.Sin(elapsed * 20f + i * 0.9f)) * 0.75f;
                SetAlpha(sparkRenderers[i], visibility * twinkle);
            }
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
            {
                return;
            }

            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        private void DealAreaDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position, attackRadius, enemyLayers);
            hitThisTick.Clear();

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    continue;
                }

                IDamageable target = hit.GetComponentInParent<IDamageable>();
                int id = hit.transform.root.gameObject.GetHashCode();
                if (target == null || !target.IsAlive || !hitThisTick.Add(id))
                {
                    continue;
                }

                enemyHitCounts.TryGetValue(id, out int previousHits);
                if (previousHits >= maximumHitsPerEnemy)
                {
                    continue;
                }

                enemyHitCounts[id] = previousHits + 1;
                Vector2 direction = (Vector2)hit.transform.position -
                    (Vector2)transform.position;
                target.TakeDamage(new DamageInfo(damagePerHit, damageSource,
                    hit.ClosestPoint(transform.position), direction, 1.2f,
                    false, true));
                if(combo!=null) combo.Contact(hit.ClosestPoint(transform.position));
                else SkillEffect.SpawnBurst(hit.transform.position, effectColor, 0.42f, 0.12f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, attackRadius);
        }
    }
}
