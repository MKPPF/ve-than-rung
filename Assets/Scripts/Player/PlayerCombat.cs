using System.Collections;
using System.Collections.Generic;
using TopDownGame.Combat;
using TopDownGame.Enemies;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TopDownGame.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float attackRange = 1.35f;
        [SerializeField, Range(10f, 180f)] private float attackArc = 95f;
        [SerializeField, Min(0.05f)] private float attackCooldown = 0.4f;
        [SerializeField, Min(0f)] private float knockback = 3f;
        [SerializeField] private LayerMask enemyLayers = ~0;
        [Header("Click Shooting")]
        [SerializeField] private bool clickToShoot;
        [SerializeField, Min(1f)] private float shotSpeed = 16f;
        [SerializeField, Min(1f)] private float shotRange = 12f;
        private float nextShotTime;
        public bool ClickToShoot => clickToShoot;
        [Header("Auto Attack")]
        [SerializeField] private bool autoAttackEnabled = true;
        [SerializeField, Min(0.03f)] private float autoTargetScanInterval = 0.08f;
        [SerializeField, Min(1f)] private float autoSkillSearchRange = 7f;
        [SerializeField, Min(0f)] private float autoCombatResumeDelay = 0.12f;
        [SerializeField, Min(0f)] private float stationarySpeedThreshold = 0.08f;

        private readonly HashSet<int> damagedTargets = new HashSet<int>();
        private PlayerStats stats;
        private PlayerMovement movement;
        private PlayerCharacterAnimator characterAnimator;
        private SkillManager skillManager;
        private Camera mainCamera;
        private bool coolingDown;
        private float nextAutoTargetScanTime;
        private float lastMovementTime;

        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public bool AutoAttackEnabled => autoAttackEnabled;
        public bool IsAttacking => coolingDown;
        public bool CanRunAutoCombat => autoAttackEnabled && !coolingDown &&
            IsStationaryForAutoCombat() &&
            Time.time >= lastMovementTime + autoCombatResumeDelay;

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            movement = GetComponent<PlayerMovement>();
            characterAnimator = GetComponent<PlayerCharacterAnimator>();
            skillManager = GetComponent<SkillManager>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (skillManager != null && skillManager.IsCasting) return;
            if (TopDownGame.Core.GameplayState.IsInputBlocked || Time.timeScale <= 0f) return;
            UpdateAimDirection();

            if (clickToShoot && Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            {
                TryShoot(AimDirection);
                return;
            }

            bool mouseAttack = Mouse.current != null &&
                Mouse.current.rightButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
            bool keyboardAttack = Keyboard.current != null &&
                Keyboard.current.jKey.wasPressedThisFrame;
            bool toggleAutoAttack = Keyboard.current != null &&
                Keyboard.current.tKey.wasPressedThisFrame;
            if (toggleAutoAttack)
            {
                autoAttackEnabled = !autoAttackEnabled;
            }

            if (coolingDown || Time.time < nextShotTime)
            {
                return;
            }

            if (mouseAttack || keyboardAttack)
            {
                StartCoroutine(AttackRoutine(AimDirection, false));
                return;
            }
        }

        private void LateUpdate()
        {
            if (skillManager != null && skillManager.IsCasting) return;
            if (TopDownGame.Core.GameplayState.IsInputBlocked || Time.timeScale <= 0f) return;
            if (!autoAttackEnabled || coolingDown || Time.time < nextShotTime)
            {
                return;
            }

            if (!IsStationaryForAutoCombat())
            {
                lastMovementTime = Time.time;
                return;
            }

            if (Time.time < lastMovementTime + autoCombatResumeDelay)
            {
                return;
            }

            if (Time.time >= nextAutoTargetScanTime)
            {
                nextAutoTargetScanTime = Time.time + autoTargetScanInterval;
                if (TryFindNearestAutoTarget(autoSkillSearchRange,
                    out EnemyHealth target, out Vector2 targetDirection,
                    out float targetDistance))
                {
                    AimDirection = targetDirection;
                    if (skillManager != null && skillManager.TryAutoCast(
                        target.transform.position, targetDistance))
                    {
                        return;
                    }

                    if (targetDistance <= attackRange)
                    {
                        StartCoroutine(AttackRoutine(targetDirection, true));
                    }
                }
            }
        }

        private bool IsStationaryForAutoCombat()
        {
            return movement == null || (!movement.WantsToMove &&
                movement.CurrentVelocity.magnitude <= stationarySpeedThreshold);
        }

        public void SetAutoAttackEnabled(bool enabled)
        {
            autoAttackEnabled = enabled;
            nextAutoTargetScanTime = 0f;
        }

        public void SetAimDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                AimDirection = direction.normalized;
            }
        }

        /// <summary>Locks each shot to its click direction, without stopping WASD movement.</summary>
        public bool TryShoot(Vector2 direction)
        {
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (!isActiveAndEnabled || !clickToShoot || coolingDown ||
                Time.time < nextShotTime || Time.timeScale <= 0f ||
                TopDownGame.Core.GameplayState.ModalClosedThisFrame ||
                TopDownGame.Core.GameplayState.IsInputBlocked ||
                (health != null && !health.IsAlive) || direction.sqrMagnitude < 0.001f)
                return false;

            SetAimDirection(direction);
            nextShotTime = Time.time + attackCooldown / Mathf.Max(0.1f, stats.CooldownRate);
            lastMovementTime = nextShotTime;
            float damage = stats.RollDamage(1f, out bool critical);
            PlayerShot.Spawn(gameObject, AimDirection, shotSpeed, shotRange,
                damage, knockback, critical, enemyLayers);
            characterAnimator?.PlayShot(AimDirection);
            return true;
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current == null || Mouse.current == null) return false;
            var pointer = new PointerEventData(EventSystem.current)
                { position = Mouse.current.position.ReadValue() };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            return hits.Exists(hit => hit.module is UnityEngine.UI.GraphicRaycaster);
        }

        private void UpdateAimDirection()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null || Mouse.current == null)
            {
                return;
            }

            Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 direction = mouseWorld - (Vector2)transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                AimDirection = direction.normalized;
            }
        }

        private bool TryFindNearestAutoTarget(float searchRange,
            out EnemyHealth target, out Vector2 targetDirection,
            out float targetDistance)
        {
            target = null;
            targetDirection = Vector2.zero;
            targetDistance = float.PositiveInfinity;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position,
                Mathf.Max(attackRange, searchRange), enemyLayers);
            float nearestDistanceSquared = float.PositiveInfinity;

            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit.CompareTag("Player"))
                {
                    continue;
                }

                EnemyHealth targetHealth = hit.GetComponentInParent<EnemyHealth>();
                if (targetHealth == null || !targetHealth.IsAlive)
                {
                    continue;
                }

                Vector2 direction = (Vector2)hit.transform.position -
                    (Vector2)transform.position;
                float distanceSquared = direction.sqrMagnitude;
                if (distanceSquared <= 0.001f ||
                    distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                target = targetHealth;
                targetDirection = direction.normalized;
            }

            if (target == null)
            {
                return false;
            }

            targetDistance = Mathf.Sqrt(nearestDistanceSquared);
            return true;
        }

        private IEnumerator AttackRoutine(Vector2 attackDirection, bool automatic)
        {
            coolingDown = true;
            Vector2 lockedAttackDirection = attackDirection.sqrMagnitude > 0.001f
                ? attackDirection.normalized
                : AimDirection;
            float adjustedCooldown = attackCooldown / stats.CooldownRate;
            float impactDelay = Mathf.Min(0.11f, adjustedCooldown * 0.3f);
            float animationTime = Mathf.Min(0.34f, adjustedCooldown);
            // Manual attacks deliberately commit the player to their windup.
            // Auto combat must stay interruptible: locking PlayerMovement here
            // clears its click destination before WantsToMove can cancel the
            // automatic attack, which makes a nearby enemy feel like a wall.
            if (!automatic)
            {
                movement?.LockMovement(animationTime);
            }
            characterAnimator?.PlayAttack(lockedAttackDirection,
                animationTime);
            float impactTime = Time.time + impactDelay;
            while (Time.time < impactTime)
            {
                if (automatic && movement != null && movement.WantsToMove)
                {
                    coolingDown = false;
                    lastMovementTime = Time.time;
                    yield break;
                }
                yield return null;
            }

            damagedTargets.Clear();
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayers);
            float damage = stats.RollDamage(1f, out bool critical);

            foreach (Collider2D hit in hits)
            {
                Vector2 direction = (Vector2)hit.transform.position - (Vector2)transform.position;
                if (direction.sqrMagnitude <= 0.001f ||
                    Vector2.Angle(lockedAttackDirection, direction) >
                    attackArc * 0.5f)
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                int id = hit.transform.root.gameObject.GetHashCode();
                if (hit.CompareTag("Player") || damageable == null ||
                    !damageable.IsAlive || !damagedTargets.Add(id))
                {
                    continue;
                }

                damageable.TakeDamage(new DamageInfo(damage, gameObject,
                    hit.ClosestPoint(transform.position), direction, knockback, critical));
            }

            float cooldownEnd = Time.time + Mathf.Max(0f,
                adjustedCooldown - impactDelay);
            while (Time.time < cooldownEnd)
            {
                if (automatic && movement != null && movement.WantsToMove)
                {
                    lastMovementTime = Time.time;
                    coolingDown = false;
                    yield break;
                }
                yield return null;
            }
            coolingDown = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
