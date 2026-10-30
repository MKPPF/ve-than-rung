using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Enemies;
using TopDownGame.Skills;
using UnityEngine;

namespace TopDownGame.Player
{
    public enum HeroCharacterId
    {
        ForestWarrior = 0,
        ThanhGiong = 1
    }

    /// <summary>
    /// Controls the playable Thanh Giong hero mode on the Player:
    /// swaps the character visual to Thanh Giong riding the fire-breathing Iron Horse,
    /// overrides the normal attack to use the Iron Sword, and provides the 4 signature
    /// skills (Q: Iron Sword + Iron Horse, E: Single Bamboo Smash, R: 360° Mounted
    /// Bamboo Whirlwind, F: Iron Horse + Bamboo Bundle Charge).
    /// </summary>
    [DefaultExecutionOrder(120)]
    public sealed class ThanhGiongHeroController : MonoBehaviour
    {
        private const string PreferenceKey = "TopDownGame.SelectedHero";

        [Header("Hero Selection")]
        [SerializeField] private HeroCharacterId currentHero = HeroCharacterId.ForestWarrior;

        [Header("Iron Sword Normal Attack")]
        [SerializeField, Min(0.5f)] private float ironSwordRange = 1.95f;
        [SerializeField, Range(30f, 180f)] private float ironSwordArc = 125f;
        [SerializeField, Min(0.5f)] private float ironSwordDamageMultiplier = 1.25f;
        [SerializeField, Min(0f)] private float ironSwordKnockback = 4.5f;

        private PlayerMovement movement;
        private PlayerCharacterAnimator characterAnimator;
        private PlayerStats stats;
        private PlayerHealth health;
        private PlayerCombat combat;
        private SkillInventory skillInventory;
        private SpriteRenderer visualRenderer;
        private Sprite defaultSpriteSnapshot;
        private bool capturedDefaultSprite;

        private SkillData[] thanhGiongSkills;
        private readonly float[] slotCooldownEndTimes = new float[4];
        private readonly float[] slotCooldownDurations = new float[4];
        private float swordPoseUntil;
        private float bambooPoseUntil;
        private float strideTimer;
        private bool isExecutingSkill;

        public HeroCharacterId CurrentHero => currentHero;
        public bool IsThanhGiongActive => currentHero == HeroCharacterId.ThanhGiong;
        public bool IsExecutingSkill => isExecutingSkill;
        public float IronSwordRange => ironSwordRange;
        public float IronSwordArc => ironSwordArc;
        public float IronSwordDamageMultiplier => ironSwordDamageMultiplier;
        public float IronSwordKnockback => ironSwordKnockback;

        public event Action<HeroCharacterId> HeroChanged;

        public static ThanhGiongHeroController Ensure(GameObject player)
        {
            if (player == null)
            {
                return null;
            }

            ThanhGiongHeroController controller = player.GetComponent<ThanhGiongHeroController>();
            if (controller == null)
            {
                controller = player.AddComponent<ThanhGiongHeroController>();
            }

            return controller;
        }

        public static HeroCharacterId LoadSavedHero()
        {
            string key = GamePreferences.Key(PreferenceKey);
            int raw = PlayerPrefs.GetInt(key, (int)HeroCharacterId.ForestWarrior);
            return raw == (int)HeroCharacterId.ThanhGiong
                ? HeroCharacterId.ThanhGiong
                : HeroCharacterId.ForestWarrior;
        }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            characterAnimator = GetComponent<PlayerCharacterAnimator>();
            stats = GetComponent<PlayerStats>();
            health = GetComponent<PlayerHealth>();
            combat = GetComponent<PlayerCombat>();
            skillInventory = GetComponent<SkillInventory>();
            EnsureSkillDefinitions();

            if (Application.isPlaying)
            {
                currentHero = LoadSavedHero();
            }
        }

        private void Start()
        {
            ResolveVisualRenderer();
            if (visualRenderer != null && !capturedDefaultSprite)
            {
                defaultSpriteSnapshot = visualRenderer.sprite;
                capturedDefaultSprite = true;
            }

            if (IsThanhGiongActive && visualRenderer != null)
            {
                visualRenderer.sprite = ThanhGiongVisualFactory.GetMountedHeroSprite(0, false, false);
            }
        }

        public void SelectHero(HeroCharacterId hero, bool savePreference = true)
        {
            EnsureSkillDefinitions();
            ResolveVisualRenderer();
            if (visualRenderer != null && !capturedDefaultSprite)
            {
                defaultSpriteSnapshot = visualRenderer.sprite;
                capturedDefaultSprite = true;
            }

            HeroCharacterId previous = currentHero;
            currentHero = hero;
            if (savePreference)
            {
                PlayerPrefs.SetInt(GamePreferences.Key(PreferenceKey), (int)hero);
                PlayerPrefs.Save();
            }

            if (currentHero == HeroCharacterId.ThanhGiong && visualRenderer != null)
            {
                visualRenderer.sprite = ThanhGiongVisualFactory.GetMountedHeroSprite(0, false, false);
            }
            else if (currentHero == HeroCharacterId.ForestWarrior && visualRenderer != null && defaultSpriteSnapshot != null)
            {
                visualRenderer.sprite = defaultSpriteSnapshot;
            }

            if (previous != currentHero)
            {
                HeroChanged?.Invoke(currentHero);
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || !IsThanhGiongActive)
            {
                return;
            }

            ResolveVisualRenderer();
            if (visualRenderer == null)
            {
                return;
            }

            bool moving = movement != null && (movement.IsMoving || movement.CurrentVelocity.sqrMagnitude > 0.01f);
            if (moving || isExecutingSkill)
            {
                strideTimer += Time.deltaTime * (isExecutingSkill ? 14f : 9f);
            }
            else
            {
                strideTimer = 0f;
            }

            int strideFrame = moving || isExecutingSkill ? Mathf.FloorToInt(strideTimer) % 4 : 0;
            bool inSwordPose = Time.time < swordPoseUntil;
            bool inBambooPose = Time.time < bambooPoseUntil;

            visualRenderer.sprite = ThanhGiongVisualFactory.GetMountedHeroSprite(strideFrame, inSwordPose, inBambooPose);

            Vector2 faceDir = combat != null && (inSwordPose || inBambooPose || isExecutingSkill)
                ? combat.AimDirection
                : characterAnimator != null
                    ? characterAnimator.FacingDirection
                    : Vector2.right;
            if (Mathf.Abs(faceDir.x) > 0.05f)
            {
                visualRenderer.flipX = faceDir.x < 0f;
            }
        }

        public void NotifyIronSwordAttack(Vector2 direction, float duration)
        {
            swordPoseUntil = Time.time + Mathf.Max(0.22f, duration);
            ThanhGiongVfx.SpawnIronSwordSlash(transform, direction, ironSwordRange);
        }

        public SkillData GetThanhGiongSkillData(int slotIndex)
        {
            EnsureSkillDefinitions();
            if (slotIndex < 0 || slotIndex >= thanhGiongSkills.Length)
            {
                return null;
            }

            return thanhGiongSkills[slotIndex];
        }

        public float GetSlotCooldownRemaining(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotCooldownEndTimes.Length)
            {
                return 0f;
            }

            return Mathf.Max(0f, slotCooldownEndTimes[slotIndex] - Time.time);
        }

        public float GetSlotCooldownDuration(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotCooldownDurations.Length)
            {
                return 0f;
            }

            return slotCooldownDurations[slotIndex];
        }

        public int GetEffectiveSkillLevel(int slotIndex)
        {
            if (skillInventory != null && slotIndex >= 0 && slotIndex < skillInventory.Slots.Count)
            {
                int slotLevel = skillInventory.Slots[slotIndex].level;
                if (slotLevel > 0)
                {
                    return slotLevel;
                }
            }

            return 1;
        }

        public bool TryCastThanhGiongSkill(int slotIndex, Vector2? targetPosition, LayerMask enemyLayers)
        {
            if (!IsThanhGiongActive || isExecutingSkill || slotIndex < 0 || slotIndex >= 4)
            {
                return false;
            }

            if (health != null && !health.IsAlive)
            {
                return false;
            }

            if (GetSlotCooldownRemaining(slotIndex) > 0.001f)
            {
                return false;
            }

            EnsureSkillDefinitions();
            SkillData data = thanhGiongSkills[slotIndex];
            if (data == null)
            {
                return false;
            }

            if (stats != null && !stats.SpendMana(data.ManaCost))
            {
                return false;
            }

            int skillLevel = GetEffectiveSkillLevel(slotIndex);
            float cooldownRate = stats != null ? Mathf.Max(0.1f, stats.CooldownRate) : 1f;
            float duration = (data.Cooldown / (1f + Mathf.Max(0, skillLevel - 1) * 0.04f)) / cooldownRate;
            slotCooldownDurations[slotIndex] = duration;
            slotCooldownEndTimes[slotIndex] = Time.time + duration;

            Vector2 aim = combat != null ? combat.AimDirection : Vector2.right;
            if (targetPosition.HasValue)
            {
                Vector2 delta = targetPosition.Value - (Vector2)transform.position;
                if (delta.sqrMagnitude > 0.001f)
                {
                    aim = delta.normalized;
                }
            }

            if (aim.sqrMagnitude <= 0.001f)
            {
                aim = Vector2.right;
            }
            else
            {
                aim.Normalize();
            }

            combat?.SetAimDirection(aim);

            switch (slotIndex)
            {
                case 0:
                    StartCoroutine(Skill1IronSwordHorseRoutine(aim, skillLevel, enemyLayers));
                    break;
                case 1:
                    StartCoroutine(Skill2SingleBambooSmashRoutine(aim, skillLevel, enemyLayers));
                    break;
                case 2:
                    StartCoroutine(Skill3MountedBambooSpin360Routine(skillLevel, enemyLayers));
                    break;
                case 3:
                    StartCoroutine(Skill4HorseBambooChargeRoutine(aim, skillLevel, enemyLayers));
                    break;
            }

            return true;
        }

        private IEnumerator Skill1IronSwordHorseRoutine(Vector2 dir, int level, LayerMask enemyLayers)
        {
            isExecutingSkill = true;
            swordPoseUntil = Time.time + 0.42f;
            float range = 3.2f * (1f + (level - 1) * 0.08f);
            ThanhGiongVfx.SpawnSkill1IronSwordHorse(transform, dir, range);

            // Short Iron Horse lunge forward
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            const float lungeDuration = 0.14f;
            const float lungeDistance = 1.85f;
            float elapsed = 0f;
            while (elapsed < lungeDuration)
            {
                yield return new WaitForFixedUpdate();
                float dt = Mathf.Min(Time.fixedDeltaTime, lungeDuration - elapsed);
                elapsed += dt;
                Vector2 step = dir * (lungeDistance / lungeDuration) * dt;
                if (movement != null)
                {
                    movement.MoveKinematically(step);
                }
                else if (body != null)
                {
                    body.MovePosition(body.position + step);
                }
            }

            // Cleave enemies in front with Iron Sword + Iron Horse fire wave
            float baseDamage = 55f * (1f + (level - 1) * 0.2f);
            float bonusDamage = stats != null ? stats.BaseDamage * 0.85f : 0f;
            bool critical = stats != null && UnityEngine.Random.value < stats.CriticalChance;
            float totalDamage = (baseDamage + bonusDamage) * (critical ? 1.5f : 1f);

            HashSet<int> hitIds = new HashSet<int>();
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayers);
            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit.CompareTag("Player"))
                {
                    continue;
                }

                Vector2 toTarget = (Vector2)hit.transform.position - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.001f && Vector2.Angle(dir, toTarget) > 75f)
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                int rootId = hit.transform.root.gameObject.GetHashCode();
                if (damageable != null && damageable.IsAlive && hitIds.Add(rootId))
                {
                    damageable.TakeDamage(new DamageInfo(totalDamage, gameObject,
                        hit.ClosestPoint(transform.position), dir, 6.5f, critical));
                }
            }

            yield return new WaitForSeconds(0.16f);
            isExecutingSkill = false;
        }

        private IEnumerator Skill2SingleBambooSmashRoutine(Vector2 dir, int level, LayerMask enemyLayers)
        {
            isExecutingSkill = true;
            bambooPoseUntil = Time.time + 0.44f;
            float length = 4.6f * (1f + (level - 1) * 0.08f);
            const float halfWidth = 1.15f;
            movement?.LockMovement(0.28f);
            ThanhGiongVfx.SpawnSkill2SingleBambooSmash(transform, dir, length);

            yield return new WaitForSeconds(0.11f);

            float baseDamage = 64f * (1f + (level - 1) * 0.2f);
            float bonusDamage = stats != null ? stats.BaseDamage * 0.9f : 0f;
            bool critical = stats != null && UnityEngine.Random.value < stats.CriticalChance;
            float totalDamage = (baseDamage + bonusDamage) * (critical ? 1.5f : 1f);

            Vector2 origin = transform.position;
            Vector2 boxCenter = origin + dir * (length * 0.5f);
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            HashSet<int> hitIds = new HashSet<int>();
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, new Vector2(length, halfWidth * 2f), angleDeg, enemyLayers);
            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit.CompareTag("Player"))
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                int rootId = hit.transform.root.gameObject.GetHashCode();
                if (damageable != null && damageable.IsAlive && hitIds.Add(rootId))
                {
                    damageable.TakeDamage(new DamageInfo(totalDamage, gameObject,
                        hit.ClosestPoint(origin), dir, 7.5f, critical));
                    EnemyMovement enemyMovement = hit.GetComponentInParent<EnemyMovement>();
                    enemyMovement?.ApplySlow(0.45f, 2.2f + level * 0.2f);
                }
            }

            yield return new WaitForSeconds(0.18f);
            isExecutingSkill = false;
        }

        private IEnumerator Skill3MountedBambooSpin360Routine(int level, LayerMask enemyLayers)
        {
            isExecutingSkill = true;
            const float duration = 1.38f;
            const int pulseCount = 6;
            float radius = 3.25f * (1f + (level - 1) * 0.06f);
            bambooPoseUntil = Time.time + duration;

            ThanhGiongVfx.SpawnSkill3MountedBambooSpin360(transform, radius, duration);

            float pulseInterval = duration / pulseCount;
            float basePerHit = 26f * (1f + (level - 1) * 0.2f);
            float bonusPerHit = stats != null ? stats.BaseDamage * 0.35f : 0f;

            for (int p = 0; p < pulseCount; p++)
            {
                if (health != null && !health.IsAlive)
                {
                    break;
                }

                bool critical = stats != null && UnityEngine.Random.value < stats.CriticalChance;
                float hitDamage = (basePerHit + bonusPerHit) * (critical ? 1.5f : 1f);
                HashSet<int> hitIds = new HashSet<int>();
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayers);
                foreach (Collider2D hit in hits)
                {
                    if (hit == null || hit.CompareTag("Player"))
                    {
                        continue;
                    }

                    IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                    int rootId = hit.transform.root.gameObject.GetHashCode();
                    if (damageable != null && damageable.IsAlive && hitIds.Add(rootId))
                    {
                        Vector2 pushDir = (Vector2)hit.transform.position - (Vector2)transform.position;
                        if (pushDir.sqrMagnitude < 0.001f)
                        {
                            pushDir = Vector2.right;
                        }

                        damageable.TakeDamage(new DamageInfo(hitDamage, gameObject,
                            hit.ClosestPoint(transform.position), pushDir.normalized, 4.2f, critical));
                    }
                }

                yield return new WaitForSeconds(pulseInterval);
            }

            isExecutingSkill = false;
        }

        private IEnumerator Skill4HorseBambooChargeRoutine(Vector2 dir, int level, LayerMask enemyLayers)
        {
            isExecutingSkill = true;
            const float chargeDuration = 0.42f;
            float chargeDistance = 5.8f * (1f + (level - 1) * 0.05f);
            const float sweepRadius = 1.65f;
            bambooPoseUntil = Time.time + chargeDuration + 0.15f;

            ThanhGiongVfx.SpawnSkill4HorseBambooCharge(transform, dir, chargeDuration);

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            bool disabledMovement = false;
            if (movement != null && movement.enabled)
            {
                movement.enabled = false;
                disabledMovement = true;
            }

            health?.BeginDodgeInvulnerability(chargeDuration + 0.1f);

            float baseSweepDamage = 38f * (1f + (level - 1) * 0.2f);
            float bonusSweepDamage = stats != null ? stats.BaseDamage * 0.55f : 0f;
            Dictionary<int, float> nextHitTimeByEnemy = new Dictionary<int, float>();

            float elapsed = 0f;
            float chargeSpeed = chargeDistance / chargeDuration;
            while (elapsed < chargeDuration)
            {
                yield return new WaitForFixedUpdate();
                float dt = Mathf.Min(Time.fixedDeltaTime, chargeDuration - elapsed);
                elapsed += dt;

                Vector2 prevPos = body != null ? body.position : (Vector2)transform.position;
                Vector2 step = dir * chargeSpeed * dt;
                Vector2 newPos;
                if (movement != null)
                {
                    newPos = movement.MoveKinematically(step);
                }
                else if (body != null)
                {
                    newPos = prevPos + step;
                    body.MovePosition(newPos);
                }
                else
                {
                    newPos = prevPos + step;
                    transform.position = newPos;
                }

                Collider2D[] hits = Physics2D.OverlapCircleAll(newPos, sweepRadius, enemyLayers);
                foreach (Collider2D hit in hits)
                {
                    if (hit == null || hit.CompareTag("Player"))
                    {
                        continue;
                    }

                    IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                    int rootId = hit.transform.root.gameObject.GetHashCode();
                    if (damageable == null || !damageable.IsAlive)
                    {
                        continue;
                    }

                    if (nextHitTimeByEnemy.TryGetValue(rootId, out float readyAt) && Time.time < readyAt)
                    {
                        continue;
                    }

                    nextHitTimeByEnemy[rootId] = Time.time + 0.14f;
                    bool critical = stats != null && UnityEngine.Random.value < stats.CriticalChance;
                    float dmg = (baseSweepDamage + bonusSweepDamage) * (critical ? 1.5f : 1f);
                    damageable.TakeDamage(new DamageInfo(dmg, gameObject,
                        hit.ClosestPoint(newPos), dir, 6.8f, critical));
                }
            }

            yield return new WaitForFixedUpdate();

            // Final bamboo bundle impact burst at the end of the charge
            float finisherDamage = (46f * (1f + (level - 1) * 0.2f)) + (stats != null ? stats.BaseDamage * 0.65f : 0f);
            HashSet<int> finisherHits = new HashSet<int>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, 2.55f, enemyLayers))
            {
                if (hit == null || hit.CompareTag("Player"))
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                int rootId = hit.transform.root.gameObject.GetHashCode();
                if (damageable != null && damageable.IsAlive && finisherHits.Add(rootId))
                {
                    damageable.TakeDamage(new DamageInfo(finisherDamage, gameObject,
                        hit.ClosestPoint(transform.position), dir, 8f, false));
                }
            }

            if (disabledMovement && movement != null)
            {
                movement.enabled = true;
            }

            isExecutingSkill = false;
        }

        private void EnsureSkillDefinitions()
        {
            if (thanhGiongSkills != null && thanhGiongSkills.Length == 4 && thanhGiongSkills[0] != null)
            {
                return;
            }

            thanhGiongSkills = new[]
            {
                CreateRuntimeSkillData(
                    SkillId.Fireball,
                    "Kiếm Sắt Chiến Mã",
                    "Thúc ngựa sắt chồm lên phun lửa, kết hợp vung kiếm sắt chém càn quét kẻ địch phía trước.",
                    ThanhGiongVisualFactory.GetSkillIcon(0),
                    55f, 16f, 2.4f, 3.2f,
                    new Color(1f, 0.58f, 0.15f, 1f)),
                CreateRuntimeSkillData(
                    SkillId.IceDash,
                    "Tre Ngà Quật Địch",
                    "Dùng 1 cây tre ngà khổng lồ quật mạnh xuống kẻ địch phía trước, gây chấn động và làm chậm.",
                    ThanhGiongVisualFactory.GetSkillIcon(1),
                    64f, 20f, 3.6f, 4.6f,
                    new Color(0.68f, 0.88f, 0.22f, 1f)),
                CreateRuntimeSkillData(
                    SkillId.SwordSpin,
                    "Thần Mã Quật Tre 360°",
                    "Khi đang cưỡi ngựa sắt, vung cụm vài cây tre ngà quật 360 độ liên hoàn quanh bản thân.",
                    ThanhGiongVisualFactory.GetSkillIcon(2),
                    156f, 26f, 5.2f, 3.25f,
                    new Color(0.95f, 0.82f, 0.20f, 1f)),
                CreateRuntimeSkillData(
                    SkillId.LightningStrike,
                    "Thiết Mã Càn Lướt",
                    "Cưỡi ngựa sắt phun lửa cùng vài cây tre cầm trên tay càn lướt xuyên qua đám kẻ địch.",
                    ThanhGiongVisualFactory.GetSkillIcon(3),
                    118f, 28f, 5.8f, 5.8f,
                    new Color(1f, 0.42f, 0.12f, 1f))
            };
        }

        private static SkillData CreateRuntimeSkillData(SkillId id, string displayName, string description,
            Sprite icon, float damage, float manaCost, float cooldown, float range, Color color)
        {
            SkillData data = ScriptableObject.CreateInstance<SkillData>();
            data.name = "ThanhGiong_" + id;
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SkillData).GetField("skillId", Flags)?.SetValue(data, id);
            typeof(SkillData).GetField("displayName", Flags)?.SetValue(data, displayName);
            typeof(SkillData).GetField("description", Flags)?.SetValue(data, description);
            typeof(SkillData).GetField("icon", Flags)?.SetValue(data, icon);
            typeof(SkillData).GetField("damage", Flags)?.SetValue(data, damage);
            typeof(SkillData).GetField("manaCost", Flags)?.SetValue(data, manaCost);
            typeof(SkillData).GetField("cooldown", Flags)?.SetValue(data, cooldown);
            typeof(SkillData).GetField("range", Flags)?.SetValue(data, range);
            typeof(SkillData).GetField("maxLevel", Flags)?.SetValue(data, 5);
            typeof(SkillData).GetField("effectColor", Flags)?.SetValue(data, color);
            return data;
        }

        private void ResolveVisualRenderer()
        {
            if (visualRenderer != null)
            {
                return;
            }

            if (movement != null && movement.VisualRenderer != null)
            {
                visualRenderer = movement.VisualRenderer;
                return;
            }

            Transform child = transform.Find("MovementVisual");
            visualRenderer = child != null
                ? child.GetComponent<SpriteRenderer>()
                : GetComponent<SpriteRenderer>();
        }
    }
}
