using System.Collections;
using TopDownGame.Combat;
using TopDownGame.Enemies.Spawning;
using UnityEngine;

namespace TopDownGame.Enemies
{
    public enum EnemySignatureSkill
    {
        ToxicPounce,
        TricksterDash,
        BackstepVolley,
        ArcanePhase,
        SiegeCharge,
        CrystalBurrow,
        RiftCross,
        AncientQuake
    }

    /// <summary>
    /// Gives every enemy family a readable signature action without replacing
    /// the authored attack patterns in its main combat controller.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMovement))]
    public sealed class EnemySignatureAbility : MonoBehaviour
    {
        private EnemyBase owner;
        private EnemyMovement movement;
        private EnemyHealth health;
        private EnemyCharacterAnimator characterAnimator;
        private Transform target;
        private float nextReadyTime;

        public bool IsPerforming { get; private set; }
        public EnemySignatureSkill Skill => ResolveSkill(Archetype);
        public string DisplayName => GetDisplayName(Skill);
        public float Cooldown => ResolveCooldown(Archetype);

        private EnemyArchetype Archetype
        {
            get
            {
                EnemyBase source = owner != null
                    ? owner
                    : GetComponent<EnemyBase>();
                return source != null && source.Data != null
                    ? source.Data.Archetype
                    : EnemyArchetype.Slime;
            }
        }

        public void Configure(EnemyBase enemy)
        {
            owner = enemy;
            movement = GetComponent<EnemyMovement>();
            health = GetComponent<EnemyHealth>();
            characterAnimator = GetComponent<EnemyCharacterAnimator>();
            if (nextReadyTime <= 0f)
            {
                ScheduleNext(0.58f);
            }
        }

        public void ResetForSpawn()
        {
            StopAllCoroutines();
            IsPerforming = false;
            target = null;
            ScheduleNext(Random.Range(0.52f, 0.76f));
        }

        public bool HandleCombatTick(Transform combatTarget,
            float targetDistance, bool localActionBusy)
        {
            if (IsPerforming)
            {
                return true;
            }

            if (localActionBusy || owner == null || owner.Data == null ||
                combatTarget == null || health == null || !health.IsAlive ||
                targetDistance > owner.DetectionRange ||
                !CanUseAtDistance(targetDistance) ||
                Time.time < nextReadyTime)
            {
                return false;
            }

            target = combatTarget;
            StartCoroutine(PerformRoutine());
            return true;
        }

        public static EnemySignatureSkill ResolveSkill(
            EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Goblin:
                    return EnemySignatureSkill.TricksterDash;
                case EnemyArchetype.Archer:
                    return EnemySignatureSkill.BackstepVolley;
                case EnemyArchetype.Mage:
                    return EnemySignatureSkill.ArcanePhase;
                case EnemyArchetype.Tank:
                    return EnemySignatureSkill.SiegeCharge;
                case EnemyArchetype.CrystalScorpion:
                    return EnemySignatureSkill.CrystalBurrow;
                case EnemyArchetype.VoidReaver:
                    return EnemySignatureSkill.RiftCross;
                case EnemyArchetype.Boss:
                    return EnemySignatureSkill.AncientQuake;
                default:
                    return EnemySignatureSkill.ToxicPounce;
            }
        }

        public static string GetDisplayName(EnemySignatureSkill skill)
        {
            switch (skill)
            {
                case EnemySignatureSkill.ToxicPounce:
                    return "Độc Dịch Bùng Nổ";
                case EnemySignatureSkill.TricksterDash:
                    return "Ảnh Bộ Song Kích";
                case EnemySignatureSkill.BackstepVolley:
                    return "Thoái Bộ Tam Tiễn";
                case EnemySignatureSkill.ArcanePhase:
                    return "Dịch Chuyển Ma Pháp";
                case EnemySignatureSkill.SiegeCharge:
                    return "Xung Phong Phá Thành";
                case EnemySignatureSkill.CrystalBurrow:
                    return "Độn Tinh Thể";
                case EnemySignatureSkill.RiftCross:
                    return "Thập Tự Hư Không";
                default:
                    return "Địa Chấn Cổ Thụ";
            }
        }

        private IEnumerator PerformRoutine()
        {
            IsPerforming = true;
            movement.StopImmediately();
            switch (Skill)
            {
                case EnemySignatureSkill.ToxicPounce:
                    yield return ToxicPounceRoutine();
                    break;
                case EnemySignatureSkill.TricksterDash:
                    yield return TricksterDashRoutine();
                    break;
                case EnemySignatureSkill.BackstepVolley:
                    yield return BackstepVolleyRoutine();
                    break;
                case EnemySignatureSkill.ArcanePhase:
                    yield return ArcanePhaseRoutine();
                    break;
                case EnemySignatureSkill.SiegeCharge:
                    yield return SiegeChargeRoutine();
                    break;
                case EnemySignatureSkill.CrystalBurrow:
                    yield return CrystalBurrowRoutine();
                    break;
                case EnemySignatureSkill.RiftCross:
                    yield return RiftCrossRoutine();
                    break;
                default:
                    yield return AncientQuakeRoutine();
                    break;
            }

            movement.Stop();
            IsPerforming = false;
            ScheduleNext(1f);
        }

        private IEnumerator ToxicPounceRoutine()
        {
            Vector2 lockedPosition = PredictTargetPosition(0.18f);
            Vector2 direction = DirectionToTarget();
            const float windup = 0.52f;
            const float radius = 1.25f;
            Color color = new Color(0.3f, 1f, 0.22f, 0.78f);
            EnemyCombatVfx.TelegraphCircle(lockedPosition, radius, windup,
                color);
            characterAnimator?.PlaySkillPhased(direction, windup * 0.64f,
                windup * 0.36f, 0.18f, color);
            yield return new WaitForSeconds(windup * 0.64f);
            if (!CanContinue()) yield break;
            movement.AddImpulse(direction * Mathf.Max(6.5f,
                owner.MoveSpeed * 2.8f));
            yield return new WaitForSeconds(windup * 0.36f);
            if (!CanContinue()) yield break;
            Vector2 impact = transform.position;
            EnemyCombatVfx.Burst(impact, radius, color, 0.38f);
            DamageTargetNear(impact, radius, 1.3f, 3f);
            EnemyHazardZone.Spawn(impact, radius * 0.88f, 3.2f, 0.72f,
                owner.Damage * 0.24f, gameObject,
                new Color(0.24f, 0.9f, 0.16f, 0.52f));
        }

        private IEnumerator TricksterDashRoutine()
        {
            Vector2 direction = DirectionToTarget();
            Vector2 tangent = new Vector2(-direction.y, direction.x) *
                (((EntityId.ToULong(GetEntityId()) & 1UL) == 0UL) ? 1f : -1f);
            const float windup = 0.3f;
            Color color = new Color(1f, 0.72f, 0.12f, 0.76f);
            EnemyCombatVfx.ForestDashFootprints(transform.position,
                (direction + tangent * 0.42f).normalized, 3.2f, windup,
                color);
            characterAnimator?.PlaySkillPhased(direction, windup, 0.2f,
                0.18f, color);
            yield return new WaitForSeconds(windup);
            if (!CanContinue()) yield break;
            movement.AddImpulse((direction * 7.5f + tangent * 4f));
            yield return new WaitForSeconds(0.2f);
            if (!CanContinue()) yield break;
            EnemyCombatVfx.ForestSlashArc(transform.position, direction,
                1.12f, color, 0.22f);
            if (ForestCombatGeometry.IsInsideStrike(transform.position,
                TargetPosition, direction, 1.12f, 65f))
                DamageTargetNear(transform.position, 1.12f, 0.82f, 2.2f);
            movement.AddImpulse(-tangent * 5.5f + direction * 2.5f);
            yield return new WaitForSeconds(0.14f);
            if (!CanContinue()) yield break;
            EnemyCombatVfx.ForestSlashArc(transform.position, -tangent,
                1.22f, new Color(1f, 0.42f, 0.1f, 0.82f), 0.25f);
            if (ForestCombatGeometry.IsInsideStrike(transform.position,
                TargetPosition, -tangent, 1.22f, 65f))
                DamageTargetNear(transform.position, 1.22f, 1.05f, 3.6f);
        }

        private IEnumerator BackstepVolleyRoutine()
        {
            Vector2 direction = DirectionToTarget();
            const float windup = 0.42f;
            Color color = new Color(1f, 0.72f, 0.18f, 0.74f);
            EnemyCombatVfx.ForestArrowAim(transform.position, direction,
                Mathf.Min(owner.AttackRange, 6.5f), windup, color);
            characterAnimator?.PlaySkillPhased(direction, windup, 0.12f,
                0.2f, color);
            yield return new WaitForSeconds(windup);
            if (!CanContinue()) yield break;
            movement.AddImpulse(-direction * 5.2f);
            EnemyCombatVfx.ForestLeafBurst(transform.position, 0.74f,
                color, 0.24f);
            LaunchFan(direction, 3, 11f, 7.8f, 0.72f, color);
            yield return new WaitForSeconds(0.18f);
            if (!CanContinue()) yield break;
            direction = DirectionToTarget();
            LaunchFan(direction, 5, 24f, 8.4f, 0.46f,
                new Color(1f, 0.88f, 0.34f, 0.82f));
        }

        private IEnumerator ArcanePhaseRoutine()
        {
            Vector2 origin = transform.position;
            Vector2 direction = DirectionToTarget();
            float side = (EntityId.ToULong(GetEntityId()) & 1UL) == 0UL
                ? 1f
                : -1f;
            Vector2 tangent = new Vector2(-direction.y, direction.x) * side;
            const float windup = 0.5f;
            Color color = new Color(0.72f, 0.22f, 1f, 0.78f);
            EnemyCombatVfx.TelegraphCircle(transform.position, 0.9f, windup,
                color);
            characterAnimator?.PlaySkillPhased(tangent, windup, 0.14f,
                0.22f, color);
            yield return new WaitForSeconds(windup);
            if (!CanContinue()) yield break;
            EnemyCombatVfx.Burst(transform.position, 0.9f, color, 0.24f);
            EnemyHazardZone.Spawn(origin, 0.95f, 2.6f, 0.68f,
                owner.Damage * 0.22f, gameObject,
                new Color(0.62f, 0.14f, 1f, 0.48f));
            movement.AddImpulse((tangent * 7.5f - direction * 2f));
            yield return new WaitForSeconds(0.14f);
            if (!CanContinue()) yield break;
            direction = DirectionToTarget();
            EnemyCombatVfx.Burst(transform.position, 0.82f,
                new Color(0.84f, 0.3f, 1f, 0.82f), 0.26f);
            LaunchFan(direction, 3, 14f, 6.8f, 0.68f, color);
        }

        private IEnumerator SiegeChargeRoutine()
        {
            Vector2 direction = DirectionToTarget();
            const float windup = 0.68f;
            Color color = new Color(1f, 0.3f, 0.08f, 0.78f);
            Vector2 predictedImpact = PredictTargetPosition(0.22f);
            EnemyCombatVfx.TelegraphLine(transform.position, direction, 3.8f,
                0.82f, windup, color);
            EnemyCombatVfx.TelegraphCircle(predictedImpact, 2.15f, windup,
                new Color(1f, 0.22f, 0.06f, 0.46f));
            characterAnimator?.PlaySkillPhased(direction, windup, 0.24f,
                0.34f, color);
            yield return new WaitForSeconds(windup);
            if (!CanContinue()) yield break;
            movement.AddImpulse(direction * 10.5f);
            yield return new WaitForSeconds(0.24f);
            if (!CanContinue()) yield break;
            EnemyCombatVfx.Burst(transform.position, 1.5f, color, 0.38f);
            DamageTargetNear(transform.position, 1.55f, 1.48f, 5.2f);
            yield return new WaitForSeconds(0.12f);
            EnemyCombatVfx.Burst(transform.position, 2.2f,
                new Color(1f, 0.2f, 0.04f, 0.72f), 0.42f);
            DamageTargetNear(transform.position, 2.2f, 0.58f, 2.4f);
        }

        private IEnumerator CrystalBurrowRoutine()
        {
            Vector2 lockedPosition = PredictTargetPosition(0.24f);
            Vector2 direction = DirectionToTarget();
            const float windup = 0.7f;
            const float radius = 1.12f;
            Color color = new Color(0.1f, 0.82f, 1f, 0.8f);
            EnemyCombatVfx.TelegraphCircle(lockedPosition, radius, windup,
                color);
            characterAnimator?.PlaySkillPhased(direction, windup, 0.16f,
                0.24f, color);
            yield return new WaitForSeconds(windup);
            if (!CanContinue()) yield break;
            movement.AddImpulse(direction * 9f);
            EnemyCombatVfx.Burst(lockedPosition, radius, color, 0.4f);
            DamageTargetNear(lockedPosition, radius, 1.32f, 3.1f);
            EnemyHazardZone.Spawn(lockedPosition, radius, 3f, 0.75f,
                owner.Damage * 0.2f, gameObject,
                new Color(0.08f, 0.72f, 1f, 0.5f));
            yield return new WaitForSeconds(0.16f);
        }

        private IEnumerator RiftCrossRoutine()
        {
            Vector2 direction = DirectionToTarget();
            Vector2 tangent = new Vector2(-direction.y, direction.x);
            const float windup = 0.58f;
            Color color = new Color(0.56f, 0.14f, 1f, 0.82f);
            EnemyCombatVfx.TelegraphLine(transform.position, direction, 4.4f,
                0.5f, windup, color);
            EnemyCombatVfx.TelegraphLine(transform.position, tangent, 2.8f,
                0.32f, windup, new Color(0.1f, 0.82f, 1f, 0.72f));
            characterAnimator?.PlaySkillPhased(direction, windup, 0.2f,
                0.24f, color);
            yield return new WaitForSeconds(windup);
            if (!CanContinue()) yield break;
            movement.AddImpulse(direction * 11.5f);
            yield return new WaitForSeconds(0.2f);
            if (!CanContinue()) yield break;
            EnemyCombatVfx.Burst(transform.position, 1.62f, color, 0.34f);
            DamageTargetNear(transform.position, 1.68f, 1.62f, 4.4f);
            EnemyHazardZone.Spawn(transform.position, 1.5f, 2.8f, 0.66f,
                owner.Damage * 0.26f, gameObject,
                new Color(0.42f, 0.08f, 1f, 0.52f));
            LaunchFan(tangent, 2, 180f, 6f, 0.42f,
                new Color(0.16f, 0.82f, 1f, 0.9f));
        }

        private IEnumerator AncientQuakeRoutine()
        {
            Vector2 lockedPosition = PredictTargetPosition(0.2f);
            Vector2 direction = DirectionToTarget();
            Vector2 tangent = new Vector2(-direction.y, direction.x);
            const float windup = 0.82f;
            const float radius = 2.45f;
            Color color = new Color(0.42f, 1f, 0.18f, 0.78f);
            EnemyCombatVfx.TelegraphCircle(lockedPosition, radius, windup,
                color, owner);
            EnemyCombatVfx.TelegraphCircle(lockedPosition + tangent * 1.55f,
                radius * 0.72f, windup,
                new Color(0.32f, 0.9f, 0.12f, 0.5f), owner);
            EnemyCombatVfx.TelegraphCircle(lockedPosition - tangent * 1.55f,
                radius * 0.72f, windup,
                new Color(0.32f, 0.9f, 0.12f, 0.5f), owner);
            characterAnimator?.PlaySkillPhased(direction, windup, 0.18f,
                0.32f, color);
            yield return new WaitForSeconds(windup * 0.7f);
            if (!CanContinue()) yield break;
            movement.AddImpulse(direction * Mathf.Max(7f,
                owner.MoveSpeed * 4.8f));
            yield return new WaitForSeconds(windup * 0.3f);
            if (!CanContinue()) yield break;
            EnemyCombatVfx.Burst(lockedPosition, radius, color, 0.52f);
            DamageTargetNear(lockedPosition, radius, 1.42f, 5.5f);
            yield return new WaitForSeconds(0.12f);
            if (!CanContinue()) yield break;
            Vector2 leftQuake = lockedPosition + tangent * 1.55f;
            Vector2 rightQuake = lockedPosition - tangent * 1.55f;
            EnemyCombatVfx.Burst(leftQuake, radius * 0.72f,
                new Color(0.32f, 0.9f, 0.12f, 0.82f), 0.42f);
            EnemyCombatVfx.Burst(rightQuake, radius * 0.72f,
                new Color(0.32f, 0.9f, 0.12f, 0.82f), 0.42f);
            DamageTargetNear(leftQuake, radius * 0.72f, 0.72f, 3.2f);
            DamageTargetNear(rightQuake, radius * 0.72f, 0.72f, 3.2f);
            EnemyHazardZone.Spawn(lockedPosition, radius * 0.82f, 2.4f,
                0.7f, owner.Damage * 0.22f, gameObject,
                new Color(0.28f, 0.82f, 0.1f, 0.44f));
        }

        private void LaunchFan(Vector2 direction, int projectileCount,
            float totalArc, float speed, float damageMultiplier, Color color)
        {
            int safeCount = Mathf.Max(1, projectileCount);
            float startAngle = safeCount == 1 ? 0f : -totalArc * 0.5f;
            float step = safeCount == 1 ? 0f : totalArc / (safeCount - 1);
            for (int i = 0; i < safeCount; i++)
            {
                Vector2 shotDirection = Quaternion.Euler(0f, 0f,
                    startAngle + step * i) * direction;
                EnemyProjectile projectile = EnemyProjectilePool.Acquire(null,
                    transform.position, shotDirection, color,
                    new Vector3(0.36f, 0.14f, 1f));
                projectile.Launch(shotDirection, speed,
                    owner.Damage * damageMultiplier, gameObject);
            }
        }

        private void DamageTargetNear(Vector2 position, float radius,
            float damageMultiplier, float knockback)
        {
            if (!CanContinue() ||
                Vector2.Distance(position, target.position) > radius)
            {
                return;
            }

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            Vector2 direction = (Vector2)target.position -
                (Vector2)transform.position;
            float dealtDamage = owner.Damage * damageMultiplier;
            damageable.TakeDamage(new DamageInfo(dealtDamage, gameObject,
                target.position, direction, knockback));
            owner.Profile?.ApplyLifeSteal(dealtDamage);
        }

        private bool CanContinue()
        {
            return health != null && health.IsAlive && target != null &&
                (!(owner is ForestGoblinEnemy || owner is ForestArcherEnemy) ||
                 ForestCombatGeometry.HasClearPath(transform.position, target.position));
        }

        private Vector2 DirectionToTarget()
        {
            Vector2 direction = TargetPosition - (Vector2)transform.position;
            return direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
        }

        private Vector2 TargetPosition => target != null
            ? (Vector2)target.position
            : (Vector2)transform.position;

        private Vector2 PredictTargetPosition(float predictionSeconds)
        {
            if (target == null)
            {
                return transform.position;
            }

            Vector2 position = target.position;
            Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
            if (targetBody != null)
            {
                position += targetBody.linearVelocity *
                    Mathf.Clamp(predictionSeconds, 0f, 0.5f);
            }
            return position;
        }

        private bool CanUseAtDistance(float distance)
        {
            switch (Archetype)
            {
                case EnemyArchetype.Slime:
                    return distance <= 4.4f;
                case EnemyArchetype.Goblin:
                    return distance >= 0.85f && distance <= 4.8f;
                case EnemyArchetype.Archer:
                    return distance >= 1.8f && distance <=
                        Mathf.Min(owner.AttackRange, 6.2f);
                case EnemyArchetype.Mage:
                    return distance >= 1.4f && distance <=
                        Mathf.Min(owner.AttackRange, 7.2f);
                case EnemyArchetype.Tank:
                    return distance >= 1.1f && distance <= 5f;
                case EnemyArchetype.CrystalScorpion:
                    return distance >= 1f && distance <= 5.2f;
                case EnemyArchetype.VoidReaver:
                    return distance >= 0.9f && distance <= 5.6f;
                default:
                    return distance <= owner.DetectionRange;
            }
        }

        private void ScheduleNext(float multiplier)
        {
            nextReadyTime = Time.time + Cooldown * Mathf.Max(0.1f, multiplier) *
                Random.Range(0.9f, 1.1f);
        }

        private static float ResolveCooldown(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Slime:
                    return 5.8f;
                case EnemyArchetype.Goblin:
                    return 5.2f;
                case EnemyArchetype.Archer:
                    return 6.4f;
                case EnemyArchetype.Mage:
                    return 7.2f;
                case EnemyArchetype.Tank:
                    return 7.8f;
                case EnemyArchetype.CrystalScorpion:
                    return 6.8f;
                case EnemyArchetype.VoidReaver:
                    return 6.2f;
                default:
                    return 8.6f;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            IsPerforming = false;
            target = null;
        }
    }
}
