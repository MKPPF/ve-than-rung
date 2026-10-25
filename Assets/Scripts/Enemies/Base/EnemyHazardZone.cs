using System.Collections.Generic;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Enemies.Progression;
using UnityEngine;

namespace TopDownGame.Enemies
{
    /// <summary>
    /// Pooled lingering danger left by signature skills. The persistent ring
    /// gives the player a reason to reposition after dodging the first impact.
    /// </summary>
    public sealed class EnemyHazardZone : MonoBehaviour
    {
        private const int PoolLimit = 32;
        private static readonly List<EnemyHazardZone> Pool =
            new List<EnemyHazardZone>(PoolLimit);
        private static int recycleCursor;

        private SpriteRenderer visual;
        private GameObject source;
        private float radius;
        private float damagePerPulse;
        private float pulseInterval;
        private float duration;
        private float elapsed;
        private float nextPulseTime;
        private Color baseColor;

        public float Radius => radius;
        public float RemainingDuration => Mathf.Max(0f, duration - elapsed);
        public static int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (EnemyHazardZone zone in Pool)
                {
                    if (zone != null && zone.gameObject.activeSelf) count++;
                }
                return count;
            }
        }

        public static EnemyHazardZone Spawn(Vector2 position, float radius,
            float duration, float pulseInterval, float damagePerPulse,
            GameObject source, Color color)
        {
            EnemyHazardZone zone = Acquire();
            zone.transform.position = position;
            zone.transform.localScale = Vector3.one *
                Mathf.Max(0.2f, radius * 2f);
            zone.radius = Mathf.Max(0.1f, radius);
            zone.duration = Mathf.Max(0.2f, duration);
            zone.pulseInterval = Mathf.Max(0.2f, pulseInterval);
            zone.damagePerPulse = Mathf.Max(0f, damagePerPulse);
            zone.source = source;
            zone.elapsed = 0f;
            zone.nextPulseTime = Time.time + 0.12f;
            zone.baseColor = color;
            zone.visual.color = color;
            zone.visual.sprite = RuntimeSpriteFactory.RingSprite;
            zone.visual.sortingOrder = 9;
            zone.gameObject.name = "Enemy Lingering Hazard";
            zone.gameObject.SetActive(true);
            return zone;
        }

        private static EnemyHazardZone Acquire()
        {
            for (int i = Pool.Count - 1; i >= 0; i--)
            {
                if (Pool[i] == null) Pool.RemoveAt(i);
            }

            foreach (EnemyHazardZone zone in Pool)
            {
                if (zone != null && !zone.gameObject.activeSelf) return zone;
            }

            if (Pool.Count >= PoolLimit)
            {
                EnemyHazardZone recycled = Pool[recycleCursor++ % Pool.Count];
                recycled.gameObject.SetActive(false);
                return recycled;
            }

            GameObject hazardObject = new GameObject("Pooled Enemy Hazard");
            SpriteRenderer renderer = hazardObject.AddComponent<SpriteRenderer>();
            EnemyHazardZone hazard = hazardObject.AddComponent<EnemyHazardZone>();
            hazard.visual = renderer;
            Pool.Add(hazard);
            hazardObject.SetActive(false);
            return hazard;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float pulse = 0.92f + Mathf.Sin(elapsed * 7.5f) * 0.08f;
            transform.localScale = Vector3.one * radius * 2f * pulse;
            Color color = baseColor;
            color.a *= Mathf.Lerp(0.72f, 0f,
                Mathf.InverseLerp(0.72f, 1f, progress));
            visual.color = color;

            if (Time.time >= nextPulseTime)
            {
                nextPulseTime = Time.time + pulseInterval;
                PulseDamage();
                EnemyCombatVfx.Burst(transform.position, radius * 0.92f,
                    new Color(baseColor.r, baseColor.g, baseColor.b,
                        baseColor.a * 0.42f), 0.24f);
            }

            if (elapsed >= duration)
            {
                source = null;
                gameObject.SetActive(false);
            }
        }

        private void PulseDamage()
        {
            if (damagePerPulse <= 0f)
            {
                return;
            }

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            GameObject player = null;
            float closestDistance = float.PositiveInfinity;
            foreach (GameObject candidate in players)
            {
                float distance = Vector2.Distance(transform.position,
                    candidate.transform.position);
                if (distance <= radius && distance < closestDistance)
                {
                    player = candidate;
                    closestDistance = distance;
                }
            }

            if (player == null)
            {
                return;
            }

            IDamageable target = player.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive)
            {
                return;
            }

            Vector2 direction = (Vector2)player.transform.position -
                (Vector2)transform.position;
            target.TakeDamage(new DamageInfo(damagePerPulse, source,
                player.transform.position, direction, 0.4f));
            if (source != null) source.GetComponent<EnemyRuntimeProfile>()?.ApplyLifeSteal(damagePerPulse);
        }
    }
}
