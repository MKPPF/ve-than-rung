using TopDownGame.Core;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Enemies
{
    /// <summary>
    /// Lightweight procedural combat readability for enemies. Every dangerous
    /// action gets a ground telegraph before damage and a short impact effect.
    /// </summary>
    public sealed class EnemyCombatVfx : MonoBehaviour
    {
        private const int PoolLimit = 96;
        private static readonly List<EnemyCombatVfx> Pool =
            new List<EnemyCombatVfx>(PoolLimit);
        private static int recycleCursor;
        private enum EffectMode
        {
            Telegraph,
            Burst,
            Footstep,
            Mist
        }

        private SpriteRenderer visual;
        private EffectMode mode;
        private Color baseColor;
        private Vector3 startScale;
        private Vector3 endScale;
        private float duration;
        private float elapsed;

        public static GameObject TelegraphCircle(Vector2 position, float radius,
            float duration, Color color)
        {
            return Create("Enemy Area Warning", position,
                new Vector3(radius * 2f, radius * 2f, 1f),
                RuntimeSpriteFactory.RingSprite, color, duration,
                EffectMode.Telegraph, 12);
        }

        public static GameObject TelegraphLine(Vector2 origin, Vector2 direction,
            float length, float width, float duration, Color color)
        {
            direction = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
            GameObject effect = Create("Enemy Line Warning",
                origin + direction * (length * 0.5f),
                new Vector3(length, width, 1f), RuntimeSpriteFactory.WhiteSprite,
                color, duration, EffectMode.Telegraph, 12);
            effect.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            return effect;
        }

        public static void TelegraphCone(Vector2 origin, Vector2 direction,
            float length, float angle, float duration, Color color)
        {
            direction = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;

            // A poison breath should read as a cloud gathering in front of the
            // Slime, not as a laser sight. Three widening bands clearly show
            // the dangerous cone while keeping its silhouette organic.
            const int BandCount = 3;
            for (int band = 0; band < BandCount; band++)
            {
                int puffCount = band + 3;
                float bandT = band / (BandCount - 1f);
                float distance = length * Mathf.Lerp(0.22f, 0.78f, bandT);
                float spread = angle * Mathf.Lerp(0.28f, 0.88f, bandT);
                for (int i = 0; i < puffCount; i++)
                {
                    float slotT = puffCount == 1
                        ? 0.5f
                        : i / (puffCount - 1f);
                    float organicOffset = Mathf.Sin((band + 1) * 4.17f +
                        i * 2.31f) * 1.8f;
                    float puffAngle = Mathf.Lerp(-spread * 0.5f,
                        spread * 0.5f, slotT) + organicOffset;
                    Vector2 puffDirection = Quaternion.Euler(0f, 0f,
                        puffAngle) * direction;
                    float stagger = Mathf.Cos(band * 3.29f + i * 1.73f) *
                        length * 0.018f;
                    Vector2 position = origin + puffDirection *
                        (distance + stagger);
                    float edge = Mathf.Abs(slotT - 0.5f) * 2f;
                    Color puffColor = color;
                    puffColor.a *= Mathf.Lerp(0.62f, 0.38f, bandT) *
                        Mathf.Lerp(1f, 0.76f, edge);
                    CreatePoisonWarningPuff(position, puffDirection,
                        Mathf.Lerp(0.5f, 0.82f, bandT) *
                            Mathf.Lerp(1f, 0.82f, edge),
                        Mathf.Lerp(0.3f, 0.54f, bandT), duration, puffColor);
                }
            }
        }

        private static void CreatePoisonWarningPuff(Vector2 position,
            Vector2 direction, float length, float width, float duration,
            Color color)
        {
            GameObject effect = Create("Poison Mist Warning", position,
                new Vector3(length, width, 1f),
                RuntimeSpriteFactory.SoftGlowSprite, color, duration,
                EffectMode.Telegraph, 12);
            effect.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        public static void PoisonMistPuff(Vector2 position, Vector2 direction,
            float length, float width, Color color, float duration = 0.28f)
        {
            direction = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
            GameObject effect = Create("Poison Spray Mist", position,
                new Vector3(Mathf.Max(0.15f, length),
                    Mathf.Max(0.08f, width), 1f),
                RuntimeSpriteFactory.SoftGlowSprite, color, duration,
                EffectMode.Mist, 13);
            effect.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        public static void ForestDashFootprints(Vector2 origin,
            Vector2 direction, float length, float duration, Color color)
        {
            direction = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
            Vector2 side = new Vector2(-direction.y, direction.x);
            float angle = Mathf.Atan2(direction.y, direction.x) *
                Mathf.Rad2Deg;
            const int StepCount = 6;
            for (int i = 0; i < StepCount; i++)
            {
                float t = (i + 0.7f) / (StepCount + 0.35f);
                float footSide = (i & 1) == 0 ? -1f : 1f;
                Vector2 position = origin + direction * length * t +
                    side * footSide * Mathf.Lerp(0.12f, 0.2f, t);
                Color footprintColor = color;
                footprintColor.a *= Mathf.Lerp(0.82f, 0.42f, t);
                GameObject effect = Create("Forest Dash Footprint", position,
                    new Vector3(Mathf.Lerp(0.24f, 0.36f, t),
                        Mathf.Lerp(0.09f, 0.14f, t), 1f),
                    RuntimeSpriteFactory.SoftGlowSprite, footprintColor,
                    duration, EffectMode.Telegraph, 12);
                effect.transform.rotation = Quaternion.Euler(0f, 0f,
                    angle + footSide * 13f);
            }
        }

        public static void ForestArrowAim(Vector2 origin, Vector2 direction,
            float length, float duration, Color color)
        {
            direction = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) *
                Mathf.Rad2Deg;
            float[] distances = { 0.24f, 0.53f, 0.82f };
            for (int marker = 0; marker < distances.Length; marker++)
            {
                Vector2 center = origin + direction * length *
                    distances[marker];
                float markerScale = 0.82f + marker * 0.12f;
                for (int wing = -1; wing <= 1; wing += 2)
                {
                    Vector2 wingDirection = Quaternion.Euler(0f, 0f,
                        wing * 32f) * -direction;
                    Color markerColor = color;
                    markerColor.a *= 0.72f - marker * 0.09f;
                    GameObject effect = Create("Forest Arrow Aim Glint",
                        center + wingDirection * 0.08f * markerScale,
                        new Vector3(0.3f * markerScale,
                            0.085f * markerScale, 1f),
                        RuntimeSpriteFactory.SoftGlowSprite, markerColor,
                        duration, EffectMode.Telegraph, 12);
                    effect.transform.rotation = Quaternion.Euler(0f, 0f,
                        baseAngle + 180f + wing * 32f);
                }
            }
        }

        public static void ForestSlashArc(Vector2 position,
            Vector2 direction, float radius, Color color,
            float duration = 0.24f)
        {
            direction = direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
            const int LeafCount = 7;
            for (int i = 0; i < LeafCount; i++)
            {
                float t = i / (LeafCount - 1f);
                float arcAngle = Mathf.Lerp(-72f, 72f, t);
                Vector2 radial = Quaternion.Euler(0f, 0f, arcAngle) *
                    direction;
                Vector2 leafPosition = position + radial * radius *
                    Mathf.Lerp(0.78f, 1f, Mathf.Sin(t * Mathf.PI));
                Color leafColor = color;
                leafColor.a *= Mathf.Lerp(0.48f, 0.86f,
                    Mathf.Sin(t * Mathf.PI));
                GameObject effect = Create("Forest Leaf Slash",
                    leafPosition, new Vector3(radius * 0.34f,
                        radius * 0.105f, 1f),
                    RuntimeSpriteFactory.SoftGlowSprite, leafColor,
                    duration, EffectMode.Burst, 13);
                effect.transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(radial.y, radial.x) * Mathf.Rad2Deg + 90f);
            }
        }

        public static void ForestLeafBurst(Vector2 position, float radius,
            Color color, float duration = 0.3f)
        {
            const int PuffCount = 7;
            for (int i = 0; i < PuffCount; i++)
            {
                float angle = i * (360f / PuffCount) + (i % 2) * 11f;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) *
                    Vector2.right;
                Color puffColor = color;
                puffColor.a *= i % 2 == 0 ? 0.7f : 0.48f;
                GameObject effect = Create("Forest Leaf Impact",
                    position + direction * radius * 0.24f,
                    new Vector3(radius * 0.32f, radius * 0.14f, 1f),
                    RuntimeSpriteFactory.SoftGlowSprite, puffColor,
                    duration * Mathf.Lerp(0.82f, 1.14f, i / 6f),
                    EffectMode.Mist, 13);
                effect.transform.rotation = Quaternion.Euler(0f, 0f,
                    angle);
            }
        }

        public static void Burst(Vector2 position, float radius, Color color,
            float duration = 0.32f)
        {
            GameObject effect = Create("Enemy Skill Impact", position,
                Vector3.one * Mathf.Max(0.1f, radius * 0.35f),
                RuntimeSpriteFactory.RingSprite, color, duration,
                EffectMode.Burst, 13);
            EnemyCombatVfx vfx = effect.GetComponent<EnemyCombatVfx>();
            if (vfx != null)
            {
                vfx.endScale = Vector3.one * radius * 2f;
            }
        }

        public static void Footstep(Vector2 position, Vector2 velocity,
            bool leftFoot, float size = 1f)
        {
            Vector2 direction = velocity.sqrMagnitude > 0.001f
                ? velocity.normalized
                : Vector2.down;
            Vector2 side = new Vector2(-direction.y, direction.x);
            Vector2 stepPosition = position - direction * 0.12f +
                side * (leftFoot ? -0.09f : 0.09f) * size;
            GameObject effect = Create(leftFoot ? "Enemy Left Step" : "Enemy Right Step",
                stepPosition, new Vector3(0.14f, 0.055f, 1f) * size,
                RuntimeSpriteFactory.SoftGlowSprite,
                new Color(0.12f, 0.09f, 0.055f, 0.24f), 0.42f,
                EffectMode.Footstep, 7);
            effect.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        public static void ForestFootstep(Vector2 position, Vector2 velocity,
            bool leftFoot, float size = 1f)
        {
            Vector2 direction = velocity.sqrMagnitude > 0.001f
                ? velocity.normalized
                : Vector2.down;
            Vector2 side = new Vector2(-direction.y, direction.x);
            Vector2 stepPosition = position - direction * 0.16f +
                side * (leftFoot ? -0.11f : 0.11f) * size;
            float angle = Mathf.Atan2(direction.y, direction.x) *
                Mathf.Rad2Deg;

            GameObject footprint = Create(leftFoot
                    ? "Forest Left Foot Plant"
                    : "Forest Right Foot Plant",
                stepPosition, new Vector3(0.22f, 0.075f, 1f) * size,
                RuntimeSpriteFactory.SoftGlowSprite,
                new Color(0.11f, 0.075f, 0.035f, 0.48f), 0.58f,
                EffectMode.Footstep, 7);
            footprint.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            GameObject leafDust = Create("Forest Step Leaf Dust",
                stepPosition - direction * 0.03f,
                new Vector3(0.17f, 0.1f, 1f) * size,
                RuntimeSpriteFactory.SoftGlowSprite,
                new Color(0.45f, 0.86f, 0.16f, 0.24f), 0.34f,
                EffectMode.Footstep, 8);
            leafDust.transform.rotation = Quaternion.Euler(0f, 0f,
                angle + (leftFoot ? -18f : 18f));
        }

        private static GameObject Create(string name, Vector2 position,
            Vector3 scale, Sprite sprite, Color color, float duration,
            EffectMode mode, int sortingOrder)
        {
            EnemyCombatVfx vfx = Acquire();
            GameObject effect = vfx.gameObject;
            effect.name = name;
            effect.transform.position = position;
            effect.transform.localScale = scale;
            effect.transform.rotation = Quaternion.identity;
            SpriteRenderer renderer = vfx.visual;
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            vfx.mode = mode;
            vfx.baseColor = color;
            vfx.startScale = scale;
            vfx.endScale = mode == EffectMode.Burst ? scale * 3f : scale;
            vfx.duration = Mathf.Max(0.05f, duration);
            vfx.elapsed = 0f;
            effect.SetActive(true);
            return effect;
        }

        private static EnemyCombatVfx Acquire()
        {
            for (int i = Pool.Count - 1; i >= 0; i--)
            {
                if (Pool[i] == null) Pool.RemoveAt(i);
            }
            for (int i = 0; i < Pool.Count; i++)
            {
                if (Pool[i] != null && !Pool[i].gameObject.activeSelf)
                    return Pool[i];
            }

            if (Pool.Count >= PoolLimit)
            {
                EnemyCombatVfx recycled = Pool[recycleCursor++ % Pool.Count];
                recycled.gameObject.SetActive(false);
                return recycled;
            }

            GameObject effect = new GameObject("PooledEnemyVfx");
            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            EnemyCombatVfx vfx = effect.AddComponent<EnemyCombatVfx>();
            vfx.visual = renderer;
            Pool.Add(vfx);
            effect.SetActive(false);
            return vfx;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color color = baseColor;

            switch (mode)
            {
                case EffectMode.Telegraph:
                    float pulse = 0.72f + Mathf.Sin(elapsed * 18f) * 0.16f;
                    color.a = baseColor.a * pulse * Mathf.Lerp(0.55f, 1f, t);
                    transform.localScale = startScale * Mathf.Lerp(0.9f, 1f, t);
                    break;

                case EffectMode.Burst:
                    transform.localScale = Vector3.Lerp(startScale, endScale,
                        1f - Mathf.Pow(1f - t, 2f));
                    color.a = baseColor.a * (1f - t);
                    break;

                case EffectMode.Mist:
                    transform.localScale = new Vector3(
                        Mathf.Lerp(startScale.x * 0.72f,
                            startScale.x * 1.32f, t),
                        Mathf.Lerp(startScale.y * 0.72f,
                            startScale.y * 1.75f, t), 1f);
                    color.a = baseColor.a * (1f - t) * (1f - t);
                    break;

                default:
                    transform.localScale = Vector3.Lerp(startScale,
                        startScale * 1.28f, t);
                    color.a = baseColor.a * (1f - t);
                    break;
            }

            visual.color = color;
            if (t >= 1f)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
