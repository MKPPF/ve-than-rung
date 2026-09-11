using System.Collections.Generic;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownGame.Skills
{
    /// <summary>
    /// Rewards fighting clustered enemies by chaining through deterministic
    /// nearest targets while showing every hop as a readable electric arc.
    /// </summary>
    public sealed class ChainLightningSkill : SkillBase, ITargetedSkill
    {
        [SerializeField, Min(1)] private int maximumTargets = 3;
        [SerializeField, Min(0.5f)] private float jumpRange = 4.25f;
        [SerializeField, Range(0.1f, 1f)] private float damageFalloff = 0.75f;
        [SerializeField] private LayerMask enemyLayers = ~0;

        private bool hasQueuedTarget;
        private Vector2 queuedTarget;

        public void SetNextTarget(Vector2 targetPosition)
        {
            queuedTarget = targetPosition;
            hasQueuedTarget = true;
        }

        public override void Cast()
        {
            Vector2 aimPoint = ResolveAimPoint();
            List<EnemyHealth> targets = SelectTargets(aimPoint);
            if (targets.Count == 0)
            {
                SkillEffect.SpawnBurst(aimPoint, Data.EffectColor, 0.45f, 0.18f);
                return;
            }

            Vector3 arcStart = transform.position;
            for (int hop = 0; hop < targets.Count; hop++)
            {
                EnemyHealth target = targets[hop];
                if (target == null || !target.IsAlive) continue;
                Vector2 direction = (Vector2)target.transform.position -
                    (Vector2)arcStart;
                float damage = ScaledDamage(DamageMultiplierForHop(hop,
                    damageFalloff));
                target.TakeDamage(new DamageInfo(damage, gameObject,
                    target.transform.position, direction, 1.25f,
                    hop == 0, true));
                if(Data.Presentation!=null && Data.Presentation.HasPaintedArt)
                    PaintedLightningLink.Spawn(Data.Presentation,arcStart,target.transform.position,.3f+hop*.04f);
                else ChainLightningArc.Spawn(arcStart, target.transform.position,
                    Data.EffectColor, 0.24f + hop * 0.04f);
                SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Impact,target.transform.position,1.4f,0.35f);
                arcStart = target.transform.position;
            }
        }

        public static float DamageMultiplierForHop(int hopIndex, float falloff)
        {
            return Mathf.Pow(Mathf.Clamp01(falloff), Mathf.Max(0, hopIndex));
        }

        private Vector2 ResolveAimPoint()
        {
            if (hasQueuedTarget)
            {
                hasQueuedTarget = false;
                return queuedTarget;
            }

            Camera camera = Camera.main;
            return camera != null && Mouse.current != null
                ? (Vector2)camera.ScreenToWorldPoint(Mouse.current.position.ReadValue())
                : (Vector2)transform.position + Vector2.right * ScaledRange();
        }

        private List<EnemyHealth> SelectTargets(Vector2 aimPoint)
        {
            var available = new List<EnemyHealth>();
            var seen = new HashSet<ulong>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position,
                ScaledRange(), enemyLayers))
            {
                EnemyHealth health = hit.GetComponentInParent<EnemyHealth>();
                if (health != null && health.IsAlive &&
                    seen.Add(EntityId.ToULong(health.gameObject.GetEntityId())))
                {
                    available.Add(health);
                }
            }

            available.Sort((left, right) => CompareTargets(left, right, aimPoint));
            var result = new List<EnemyHealth>();
            if (available.Count == 0) return result;
            result.Add(available[0]);
            available.RemoveAt(0);

            while (result.Count < Mathf.Max(1, maximumTargets) && available.Count > 0)
            {
                Vector2 origin = result[result.Count - 1].transform.position;
                available.Sort((left, right) => CompareTargets(left, right, origin));
                EnemyHealth next = available[0];
                if (Vector2.Distance(origin, next.transform.position) > jumpRange)
                {
                    break;
                }
                result.Add(next);
                available.RemoveAt(0);
            }

            return result;
        }

        private static int CompareTargets(EnemyHealth left, EnemyHealth right,
            Vector2 point)
        {
            float leftDistance = ((Vector2)left.transform.position - point).sqrMagnitude;
            float rightDistance = ((Vector2)right.transform.position - point).sqrMagnitude;
            int distanceComparison = leftDistance.CompareTo(rightDistance);
            return distanceComparison != 0
                ? distanceComparison
                : EntityId.ToULong(left.gameObject.GetEntityId()).CompareTo(
                    EntityId.ToULong(right.gameObject.GetEntityId()));
        }
    }

    internal sealed class ChainLightningArc : MonoBehaviour
    {
        private LineRenderer line;
        private Material material;
        private Vector3 start;
        private Vector3 end;
        private Color color;
        private float duration;
        private float elapsed;

        public static void Spawn(Vector3 start, Vector3 end, Color color,
            float duration)
        {
            GameObject root = new GameObject("ChainLightningArc");
            ChainLightningArc effect = root.AddComponent<ChainLightningArc>();
            effect.start = start;
            effect.end = end;
            effect.color = color;
            effect.duration = Mathf.Max(0.08f, duration);
            effect.Build();
        }

        private void Build()
        {
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 9;
            line.startWidth = 0.13f;
            line.endWidth = 0.055f;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.sortingOrder = 35;
            material = new Material(Shader.Find("Sprites/Default"));
            line.material = material;
            Redraw(1f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            Redraw(alpha);
            if (elapsed >= duration) Destroy(gameObject);
        }

        private void Redraw(float alpha)
        {
            Vector3 delta = end - start;
            Vector3 perpendicular = new Vector3(-delta.y, delta.x, 0f).normalized;
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float jitter = Mathf.Sin(i * 8.71f + Time.time * 52f) *
                    0.15f * envelope;
                Vector3 point = Vector3.Lerp(start, end, t) + perpendicular * jitter;
                point.z = -0.25f;
                line.SetPosition(i, point);
            }
            Color bright = Color.Lerp(Color.white, color, 0.62f);
            bright.a = alpha;
            Color edge = color;
            edge.a = alpha * 0.25f;
            line.startColor = bright;
            line.endColor = edge;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
