using System.Collections;
using TopDownGame.Combat;
using TopDownGame.Enemies;
using UnityEngine;

namespace TopDownGame.World
{
    /// <summary>
    /// Turns the eclipse ruins into an active battlefield with sparse,
    /// telegraphed anomaly strikes that force repositioning between enemy waves.
    /// </summary>
    public sealed class RiftStormController : MonoBehaviour
    {
        [SerializeField] private Vector2 intervalRange = new Vector2(8f, 13f);
        [SerializeField, Min(1)] private int strikeCount = 3;
        [SerializeField, Min(0.1f)] private float warningDuration = 1.05f;
        [SerializeField, Min(0.25f)] private float strikeRadius = 1.25f;
        [SerializeField, Min(0f)] private float damage = 18f;

        private Transform player;
        private Coroutine stormRoutine;

        private void OnEnable()
        {
            stormRoutine = StartCoroutine(StormLoop());
        }

        private void OnDisable()
        {
            if (stormRoutine != null) StopCoroutine(stormRoutine);
            stormRoutine = null;
        }

        private IEnumerator StormLoop()
        {
            yield return new WaitForSeconds(5f);
            while (enabled)
            {
                float minimum = Mathf.Min(intervalRange.x, intervalRange.y);
                float maximum = Mathf.Max(intervalRange.x, intervalRange.y);
                yield return new WaitForSeconds(Random.Range(minimum, maximum));
                if (player == null)
                {
                    player = GameObject.FindGameObjectWithTag("Player")?.transform;
                }
                if (player == null) continue;

                Vector2 center = player.position;
                Vector2[] points = new Vector2[Mathf.Max(1, strikeCount)];
                for (int i = 0; i < points.Length; i++)
                {
                    float angle = (i * 137.5f + Random.Range(-18f, 18f)) *
                        Mathf.Deg2Rad;
                    float distance = i == 0 ? 0.65f : Random.Range(1.2f, 3.2f);
                    points[i] = center + new Vector2(Mathf.Cos(angle),
                        Mathf.Sin(angle)) * distance;
                    EnemyCombatVfx.TelegraphCircle(points[i], strikeRadius,
                        warningDuration, new Color(0.58f, 0.12f, 1f, 0.7f));
                }

                yield return new WaitForSeconds(warningDuration);
                foreach (Vector2 point in points)
                {
                    EnemyCombatVfx.Burst(point, strikeRadius,
                        new Color(0.5f, 0.12f, 1f, 0.86f), 0.34f);
                    DamagePlayer(point);
                }
            }
        }

        private void DamagePlayer(Vector2 point)
        {
            if (player == null || Vector2.Distance(point, player.position) >
                strikeRadius) return;
            IDamageable target = player.GetComponent<IDamageable>();
            Vector2 direction = (Vector2)player.position - point;
            target?.TakeDamage(new DamageInfo(damage, gameObject,
                player.position, direction, 2.5f, false, true));
        }
    }
}
