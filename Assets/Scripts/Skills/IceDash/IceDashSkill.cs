using System.Collections;
using System.Collections.Generic;
using TopDownGame.Combat;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Skills
{
    public sealed class IceDashSkill : SkillBase
    {
        [SerializeField, Min(0.1f)] private float dashDistance = 3.5f;
        [SerializeField, Min(0.05f)] private float dashDuration = 0.2f;
        [SerializeField, Min(0.1f)] private float hitRadius = 0.75f;
        [SerializeField] private LayerMask enemyLayers = ~0;

        public override void Cast()
        {
            StartCoroutine(DashRoutine());
        }

        private IEnumerator DashRoutine()
        {
            PlayerMovement movement = GetComponent<PlayerMovement>();
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            PlayerCombat combat = GetComponent<PlayerCombat>();
            // PlayerCombat owns both manual mouse aim and the current automatic
            // enemy target, so dash follows the intended combat target even
            // while click-to-move is active.
            Vector2 direction = combat != null
                ? combat.AimDirection
                : movement != null && movement.IsMoving
                    ? movement.MoveDirection
                    : Vector2.right;
            direction = direction.sqrMagnitude > Mathf.Epsilon
                ? direction.normalized
                : Vector2.right;
            Vector2 start = body.position;
            HashSet<int> hitTargets = new HashSet<int>();
            SkillEffect.SpawnBurst(start, Data.EffectColor, 0.75f, 0.14f);
            SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Impact,start,1.9f,0.4f);

            if (movement != null)
            {
                movement.enabled = false;
            }

            float elapsed = 0f;
            float dashSpeed = dashDistance / Mathf.Max(0.05f, dashDuration);
            Vector2 effectPosition = start;
            float nextEcho = 0f;
            while (elapsed < dashDuration)
            {
                yield return new WaitForFixedUpdate();
                float stepDuration = Mathf.Min(Time.fixedDeltaTime,
                    dashDuration - elapsed);
                elapsed += stepDuration;
                if (elapsed >= nextEcho)
                {
                    SpellPresentation.Echo(movement != null ? movement.VisualRenderer : GetComponentInChildren<SpriteRenderer>(), Data.EffectColor);
                    SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Impact,body.position,1.0f,0.3f);
                    nextEcho += 0.045f;
                }
                Vector2 previousPosition = body.position;
                Vector2 displacement = direction * dashSpeed * stepDuration;
                if (movement != null)
                {
                    effectPosition = movement.MoveKinematically(displacement);
                }
                else
                {
                    effectPosition = previousPosition + displacement;
                    body.MovePosition(effectPosition);
                }

                DamageAt(effectPosition, hitTargets);
                if ((effectPosition - previousPosition).sqrMagnitude <= 0.000001f)
                {
                    break;
                }
            }

            // Wait for the final MovePosition before sampling the landing point
            // and restoring normal player locomotion.
            yield return new WaitForFixedUpdate();
            effectPosition = body.position;
            SkillEffect.SpawnBurst(effectPosition, Data.EffectColor, 1.1f, 0.22f);
            if (movement != null)
            {
                movement.enabled = true;
            }
        }

        private void DamageAt(Vector2 position, HashSet<int> hitTargets)
        {
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, hitRadius, enemyLayers))
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                int id = hit.transform.root.gameObject.GetHashCode();
                if (!hit.CompareTag("Player") && target != null && target.IsAlive && hitTargets.Add(id))
                {
                    target.TakeDamage(new DamageInfo(ScaledDamage(), gameObject,
                        hit.ClosestPoint(position), hit.transform.position - transform.position, 4f));
                    TopDownGame.Enemies.EnemyMovement enemyMovement =
                        hit.GetComponentInParent<TopDownGame.Enemies.EnemyMovement>();
                    enemyMovement?.ApplySlow(0.5f, 2f + Level * 0.25f);
                }
            }
        }
    }
}
