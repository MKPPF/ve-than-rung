using System.Collections;
using System.Collections.Generic;
using TopDownGame.Combat;
using TopDownGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownGame.Skills
{
    /// <summary>
    /// Creates a telegraphed multi-impact zone whose overlapping meteors reward
    /// deliberate aiming while still giving enemies a readable warning.
    /// </summary>
    public sealed class MeteorShowerSkill : SkillBase, ITargetedSkill
    {
        [SerializeField, Min(1)] private int meteorCount = 5;
        [SerializeField, Min(0.2f)] private float impactRadius = 0.95f;
        [SerializeField, Min(0f)] private float spreadRadius = 1.55f;
        [SerializeField, Min(0.1f)] private float warningDuration = 0.62f;
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
            Vector2 target = ResolveTarget();
            Vector2 offset = target - (Vector2)transform.position;
            float range = ScaledRange();
            if (offset.magnitude > range)
            {
                target = (Vector2)transform.position + offset.normalized * range;
            }
            MonoBehaviourHelper.Instance.StartCoroutine(ShowerRoutine(target));
        }

        private IEnumerator ShowerRoutine(Vector2 center)
        {
            var impacts = new List<Vector2>();
            var indicators = new List<GameObject>();
            int count = Mathf.Max(1, meteorCount + (Level - 1) / 2);
            float rotation = Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float distance = i == 0 ? 0f : spreadRadius *
                    Mathf.Lerp(0.45f, 1f, (i % 3) / 2f);
                float angle = (rotation + i * 137.5f) * Mathf.Deg2Rad;
                Vector2 point = center + new Vector2(Mathf.Cos(angle),
                    Mathf.Sin(angle)) * distance;
                impacts.Add(point);
                indicators.Add(CreateIndicator(point));
                SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Meteor,point,1.9f,warningDuration);
            }

            yield return new WaitForSeconds(warningDuration);
            float perMeteorDamage = ScaledDamage(0.58f);
            foreach (Vector2 point in impacts)
            {
                DamageImpact(point, perMeteorDamage);
                SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Impact,point,impactRadius*2.6f,0.5f);
            }
            foreach (GameObject indicator in indicators)
            {
                if (indicator != null) Destroy(indicator);
            }
        }

        private void DamageImpact(Vector2 point, float damage)
        {
            TopDownGame.Core.CombatFeedback.WorldImpact(point,0.075f);
            var hitTargets = new HashSet<ulong>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(point,
                impactRadius, enemyLayers))
            {
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                GameObject root = hit.transform.root.gameObject;
                if (hit.CompareTag("Player") || damageable == null ||
                    !damageable.IsAlive || !hitTargets.Add(
                        EntityId.ToULong(root.GetEntityId())))
                {
                    continue;
                }
                Vector2 direction = (Vector2)hit.transform.position - point;
                damageable.TakeDamage(new DamageInfo(damage, gameObject,
                    hit.ClosestPoint(point), direction, 2.2f, false, false));
            }
        }

        private GameObject CreateIndicator(Vector2 point)
        {
            var warning=SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Warning,point,impactRadius*2,warningDuration);
            if(warning!=null) return warning.gameObject;
            GameObject indicator = new GameObject("MeteorWarning");
            indicator.transform.position = new Vector3(point.x, point.y, -0.15f);
            indicator.transform.localScale = Vector3.one * impactRadius * 2f;
            SpriteRenderer renderer = indicator.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.RingSprite;
            renderer.color = new Color(1f, 0.42f, 0.12f, 0.30f);
            renderer.sortingOrder = -1;
            return indicator;
        }

        private Vector2 ResolveTarget()
        {
            if (hasQueuedTarget)
            {
                hasQueuedTarget = false;
                return queuedTarget;
            }
            Camera camera = Camera.main;
            return camera != null && Mouse.current != null
                ? (Vector2)camera.ScreenToWorldPoint(Mouse.current.position.ReadValue())
                : (Vector2)transform.position;
        }
    }
}
