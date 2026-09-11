using System.Collections;
using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Player
{
    /// <summary>
    /// Refreshable poison debuff for the player. Repeated applications extend
    /// the duration and keep the strongest dose instead of creating unlimited
    /// overlapping coroutines.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerPoisonStatus : MonoBehaviour
    {
        private const int BubbleCount = 3;
        private readonly SpriteRenderer[] bubbles =
            new SpriteRenderer[BubbleCount];

        private PlayerHealth health;
        private Coroutine poisonRoutine;
        private float damagePerTick;
        private float tickInterval;
        private float expiresAt;
        private float nextTickAt;

        public bool IsPoisoned => poisonRoutine != null &&
            Time.time < expiresAt && health != null && health.IsAlive;
        public float RemainingDuration => IsPoisoned
            ? Mathf.Max(0f, expiresAt - Time.time)
            : 0f;
        public int Applications { get; private set; }
        public int CompletedTicks { get; private set; }
        public float TotalPoisonDamage { get; private set; }

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            CreateVisuals();
        }

        private void Update()
        {
            if (!IsPoisoned)
            {
                return;
            }

            for (int i = 0; i < bubbles.Length; i++)
            {
                SpriteRenderer bubble = bubbles[i];
                if (bubble == null) continue;
                float phase = Time.time * (2.6f + i * 0.24f) + i * 2.1f;
                bubble.transform.localPosition = new Vector3(
                    Mathf.Sin(phase) * (0.22f + i * 0.055f),
                    -0.08f + Mathf.Repeat(Time.time * (0.34f + i * 0.06f) +
                        i * 0.29f, 0.72f), 0f);
                float pulse = 0.82f + Mathf.Sin(phase * 1.7f) * 0.18f;
                bubble.transform.localScale = new Vector3(
                    (0.18f + i * 0.035f) * pulse,
                    (0.12f + i * 0.026f) * pulse, 1f);
                Color color = bubble.color;
                color.a = 0.3f + Mathf.Sin(phase * 1.3f) * 0.08f;
                bubble.color = color;
            }
        }

        public void ApplyPoison(float tickDamage, float duration,
            float interval)
        {
            if (health == null || !health.IsAlive || tickDamage <= 0f ||
                duration <= 0f)
            {
                return;
            }

            Applications++;
            damagePerTick = Mathf.Max(damagePerTick, tickDamage);
            float safeInterval = Mathf.Max(0.15f, interval);
            tickInterval = poisonRoutine == null
                ? safeInterval
                : Mathf.Min(tickInterval, safeInterval);
            expiresAt = Mathf.Max(expiresAt, Time.time + duration);

            if (poisonRoutine != null)
            {
                return;
            }

            nextTickAt = Time.time + tickInterval;
            SetVisualsActive(true);
            poisonRoutine = StartCoroutine(PoisonRoutine());
        }

        public void ClearPoison()
        {
            if (poisonRoutine != null)
            {
                StopCoroutine(poisonRoutine);
            }
            ResetStatus();
        }

        private IEnumerator PoisonRoutine()
        {
            while (health != null && health.IsAlive && Time.time < expiresAt)
            {
                if (Time.time >= nextTickAt)
                {
                    TotalPoisonDamage += health.TakeStatusDamage(damagePerTick);
                    CompletedTicks++;
                    nextTickAt = Time.time + tickInterval;
                }
                yield return null;
            }

            ResetStatus();
        }

        private void CreateVisuals()
        {
            for (int i = 0; i < bubbles.Length; i++)
            {
                GameObject bubbleObject = new GameObject(
                    "Poison Status Mist " + (i + 1));
                bubbleObject.transform.SetParent(transform, false);
                SpriteRenderer renderer =
                    bubbleObject.AddComponent<SpriteRenderer>();
                renderer.sprite = RuntimeSpriteFactory.SoftGlowSprite;
                renderer.color = Color.Lerp(
                    new Color(0.22f, 1f, 0.08f, 0.34f),
                    new Color(0.72f, 1f, 0.12f, 0.3f), i / 2f);
                renderer.sortingOrder = 14;
                bubbles[i] = renderer;
                bubbleObject.SetActive(false);
            }
        }

        private void SetVisualsActive(bool active)
        {
            for (int i = 0; i < bubbles.Length; i++)
            {
                if (bubbles[i] != null)
                {
                    bubbles[i].gameObject.SetActive(active);
                }
            }
        }

        private void ResetStatus()
        {
            poisonRoutine = null;
            damagePerTick = 0f;
            tickInterval = 0f;
            expiresAt = 0f;
            nextTickAt = 0f;
            SetVisualsActive(false);
        }

        private void OnDisable()
        {
            ResetStatus();
        }
    }
}
