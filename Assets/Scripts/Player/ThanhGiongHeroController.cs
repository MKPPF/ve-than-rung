using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Enemies;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownGame.Player
{
    public enum HeroCharacterId
    {
        ForestWarrior = 0,
        ThanhGiong = 1,
        SonTinh = 2
    }

    /// <summary>
    /// Central playable hero controller on the Player supporting:
    /// - ForestWarrior (default customizable warrior)
    /// - ThanhGiong (Iron Horse, Iron Sword, and Ivory Bamboo 5-move kit)
    /// - SonTinh (Mountain God with Overhead Giant Boulder, Skyward-Palm Mountain Eruption,
    ///   Ground-Slam Cascading Ridges, and 360-degree Surrounding Mountain Fortress)
    /// </summary>
    [DefaultExecutionOrder(120)]
    public sealed class ThanhGiongHeroController : MonoBehaviour
    {
        private const string PreferenceKey = "TopDownGame.SelectedHero";

        /// <summary>
        /// Enabled during hero creation & playtesting phase so all heroes can be freely
        /// selected and tested before re-enabling map-by-map progression locks.
        /// </summary>
        public static bool UnlockAllHeroesForTesting { get; set; } = true;

        [Header("Hero Selection")]
        [SerializeField] private HeroCharacterId currentHero = HeroCharacterId.ForestWarrior;

        [Header("Thanh Giong — Iron Sword Normal Attack")]
        [SerializeField, Min(0.5f)] private float ironSwordRange = 1.95f;
        [SerializeField, Range(30f, 180f)] private float ironSwordArc = 125f;
        [SerializeField, Min(0.5f)] private float ironSwordDamageMultiplier = 1.25f;
        [SerializeField, Min(0f)] private float ironSwordKnockback = 4.5f;

        [Header("Son Tinh — Stone Fist Normal Attack")]
        [SerializeField, Min(0.5f)] private float sonTinhPunchRange = 2.05f;
        [SerializeField, Range(30f, 180f)] private float sonTinhPunchArc = 120f;
        [SerializeField, Min(0.5f)] private float sonTinhPunchDamageMultiplier = 1.3f;
        [SerializeField, Min(0f)] private float sonTinhPunchKnockback = 4.8f;

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
        private SkillData[] sonTinhSkills;
        private readonly float[] slotCooldownEndTimes = new float[4];
        private readonly float[] slotCooldownDurations = new float[4];
        private float swordPoseUntil;
        private float bambooPoseUntil;
        private SonTinhPose activeSonTinhPose = SonTinhPose.Locomotion;
        private float sonTinhPoseUntil;
        private float strideTimer;
        private bool isExecutingSkill;

        public HeroCharacterId CurrentHero => currentHero;
        public bool IsThanhGiongActive => currentHero == HeroCharacterId.ThanhGiong;
        public bool IsSonTinhActive => currentHero == HeroCharacterId.SonTinh;
        public bool IsCustomHeroActive => currentHero != HeroCharacterId.ForestWarrior;
        public bool IsExecutingSkill => isExecutingSkill;

        public float IronSwordRange => ironSwordRange;
        public float IronSwordArc => ironSwordArc;
        public float IronSwordDamageMultiplier => ironSwordDamageMultiplier;
        public float IronSwordKnockback => ironSwordKnockback;

        public float CustomNormalAttackRange => IsSonTinhActive ? sonTinhPunchRange : ironSwordRange;
        public float CustomNormalAttackArc => IsSonTinhActive ? sonTinhPunchArc : ironSwordArc;
        public float CustomNormalAttackDamageMultiplier => IsSonTinhActive ? sonTinhPunchDamageMultiplier : ironSwordDamageMultiplier;
        public float CustomNormalAttackKnockback => IsSonTinhActive ? sonTinhPunchKnockback : ironSwordKnockback;

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

        public static bool IsHeroUnlocked(HeroCharacterId hero)
        {
            if (UnlockAllHeroesForTesting || hero == HeroCharacterId.ForestWarrior)
            {
                return true;
            }

            return IsHeroUnlockedByProgression(hero);
        }

        public static bool IsHeroUnlockedByProgression(HeroCharacterId hero)
        {
            if (hero == HeroCharacterId.ForestWarrior)
            {
                return true;
            }

            TopDownGame.Save.GameProgressManager progress = TopDownGame.Save.GameProgressManager.Instance;
            if (progress == null)
            {
                return false;
            }

            if (hero == HeroCharacterId.ThanhGiong)
            {
                return progress.IsBossDefeated("ForestGuardian") ||
                       progress.IsRegionUnlocked("BossMap01") ||
                       progress.IsRegionUnlocked("CaveMap01") ||
                       progress.IsRegionUnlocked("RiftMap01");
            }

            if (hero == HeroCharacterId.SonTinh)
            {
                return progress.IsRegionUnlocked("CaveMap01") ||
                       progress.IsRegionUnlocked("RiftMap01");
            }

            return false;
        }

        public static HeroCharacterId LoadSavedHero()
        {
            string key = GamePreferences.Key(PreferenceKey);
            int raw = PlayerPrefs.GetInt(key, (int)HeroCharacterId.ForestWarrior);
            if (Enum.IsDefined(typeof(HeroCharacterId), raw))
            {
                HeroCharacterId saved = (HeroCharacterId)raw;
                if (IsHeroUnlocked(saved))
                {
                    return saved;
                }
            }

            return HeroCharacterId.ForestWarrior;
        }

        public static Sprite GetInitialSavedHeroSprite()
        {
            HeroCharacterId saved = LoadSavedHero();
            return saved switch
            {
                HeroCharacterId.ThanhGiong => ThanhGiongVisualFactory.GetMountedHeroSprite(0, false, false),
                HeroCharacterId.SonTinh => SonTinhVisualFactory.GetHeroSprite(0, SonTinhPose.Locomotion),
                _ => null
            };
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

            ApplyHeroVisual(currentHero);
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
            if (previous != currentHero)
            {
                Array.Clear(slotCooldownEndTimes, 0, slotCooldownEndTimes.Length);
                Array.Clear(slotCooldownDurations, 0, slotCooldownDurations.Length);
                swordPoseUntil = 0f;
                bambooPoseUntil = 0f;
                sonTinhPoseUntil = 0f;
                activeSonTinhPose = SonTinhPose.Locomotion;
            }

            if (savePreference)
            {
                PlayerPrefs.SetInt(GamePreferences.Key(PreferenceKey), (int)hero);
                PlayerPrefs.Save();
            }

            ApplyHeroVisual(currentHero);

            if (previous != currentHero)
            {
                HeroChanged?.Invoke(currentHero);
            }
        }

        private void ApplyHeroVisual(HeroCharacterId hero)
        {
            ResolveVisualRenderer();
            if (visualRenderer == null)
            {
                return;
            }

            if (hero == HeroCharacterId.ThanhGiong || hero == HeroCharacterId.SonTinh)
            {
                if (characterAnimator != null)
                {
                    characterAnimator.enabled = false;
                }

                Sprite sp = hero == HeroCharacterId.ThanhGiong
                    ? ThanhGiongVisualFactory.GetMountedHeroSprite(0, false, false)
                    : SonTinhVisualFactory.GetHeroSprite(0, SonTinhPose.Locomotion);
                AssignHeroSprite(sp);
            }
            else
            {
                if (characterAnimator != null)
                {
                    characterAnimator.enabled = true;
                }

                if (defaultSpriteSnapshot != null)
                {
                    visualRenderer.sprite = defaultSpriteSnapshot;
                }

                visualRenderer.enabled = true;
            }
        }

        private void AssignHeroSprite(Sprite sp)
        {
            if (sp == null || visualRenderer == null)
            {
                return;
            }

            visualRenderer.sprite = sp;
            visualRenderer.color = Color.white;
            visualRenderer.enabled = true;

            SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
            if (rootSr != null && rootSr != visualRenderer)
            {
                rootSr.sprite = sp;
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || !IsCustomHeroActive)
            {
                return;
            }

            ResolveVisualRenderer();
            if (visualRenderer == null)
            {
                return;
            }

            if (characterAnimator != null && characterAnimator.enabled)
            {
                characterAnimator.enabled = false;
            }

            bool moving = movement != null && (movement.IsMoving || movement.CurrentVelocity.sqrMagnitude > 0.01f);
            if (moving || isExecutingSkill)
            {
                strideTimer += Time.deltaTime * (isExecutingSkill ? 12f : 9f);
            }
            else
            {
                strideTimer = 0f;
            }

            int strideFrame = moving || isExecutingSkill ? Mathf.FloorToInt(strideTimer) % 4 : 0;

            if (IsThanhGiongActive)
            {
                bool inSwordPose = Time.time < swordPoseUntil;
                bool inBambooPose = Time.time < bambooPoseUntil;
                Sprite currentMountedSprite = ThanhGiongVisualFactory.GetMountedHeroSprite(strideFrame, inSwordPose, inBambooPose);
                AssignHeroSprite(currentMountedSprite);

                Vector2 faceDir = combat != null && (inSwordPose || inBambooPose || isExecutingSkill)
                    ? combat.AimDirection
                    : (movement != null && movement.CurrentVelocity.sqrMagnitude > 0.01f ? movement.CurrentVelocity : Vector2.right);
                if (Mathf.Abs(faceDir.x) > 0.05f)
                {
                    visualRenderer.flipX = faceDir.x < 0f;
                }
            }
            else if (IsSonTinhActive)
            {
                bool inActionPose = Time.time < sonTinhPoseUntil;
                SonTinhPose pose = inActionPose ? activeSonTinhPose : SonTinhPose.Locomotion;
                Sprite currentSonTinhSprite = SonTinhVisualFactory.GetHeroSprite(strideFrame, pose);
                AssignHeroSprite(currentSonTinhSprite);

                Vector2 faceDir = combat != null && (inActionPose || isExecutingSkill)
                    ? combat.AimDirection
                    : (movement != null && movement.CurrentVelocity.sqrMagnitude > 0.01f ? movement.CurrentVelocity : Vector2.right);
                if (Mathf.Abs(faceDir.x) > 0.05f)
                {
                    visualRenderer.flipX = faceDir.x < 0f;
                }
            }
        }

        public void NotifyCustomNormalAttack(Vector2 direction, float duration)
        {
            if (IsSonTinhActive)
            {
                NotifySonTinhRockAttack(direction, duration);
            }
            else if (IsThanhGiongActive)
            {
                NotifyIronSwordAttack(direction, duration);
            }
        }

        public void NotifyIronSwordAttack(Vector2 direction, float duration)
        {
            swordPoseUntil = Time.time + Mathf.Max(0.22f, duration);
            ThanhGiongVfx.SpawnIronSwordSlash(transform, direction, ironSwordRange);
        }

        public void NotifySonTinhRockAttack(Vector2 direction, float duration)
        {
            activeSonTinhPose = SonTinhPose.RockPunch;
            sonTinhPoseUntil = Time.time + Mathf.Max(0.24f, duration);
            SonTinhVfx.SpawnNormalRockStrike(transform, direction, sonTinhPunchRange);
        }

        public SkillData GetActiveHeroSkillData(int slotIndex)
        {
            if (IsSonTinhActive)
            {
                return GetSonTinhSkillData(slotIndex);
            }

            if (IsThanhGiongActive)
            {
                return GetThanhGiongSkillData(slotIndex);
            }

            return null;
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

        public SkillData GetSonTinhSkillData(int slotIndex)
        {
            EnsureSkillDefinitions();
            if (slotIndex < 0 || slotIndex >= sonTinhSkills.Length)
            {
                return null;
            }

            return sonTinhSkills[slotIndex];
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

        public bool TryCastActiveHeroSkill(int slotIndex, Vector2? targetPosition, LayerMask enemyLayers)
        {
            if (IsSonTinhActive)
            {
                return TryCastSonTinhSkill(slotIndex, targetPosition, enemyLayers);
            }

            if (IsThanhGiongActive)
            {
                return TryCastThanhGiongSkill(slotIndex, targetPosition, enemyLayers);
            }

            return false;
        }

        public bool TryCastThanhGiongSkill(int slotIndex, Vector2? targetPosition, LayerMask enemyLayers)
        {
            if (!IsThanhGiongActive || isExecutingSkill || slotIndex < 0 || slotIndex >= 4)
            {
                return false;
            }

            if (!BeginCustomSkillCast(thanhGiongSkills, slotIndex, targetPosition, out Vector2 aim, out int skillLevel))
            {
                return false;
            }

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

        public bool TryCastSonTinhSkill(int slotIndex, Vector2? targetPosition, LayerMask enemyLayers)
        {
            if (!IsSonTinhActive || isExecutingSkill || slotIndex < 0 || slotIndex >= 4)
            {
                return false;
            }

            if (!BeginCustomSkillCast(sonTinhSkills, slotIndex, targetPosition, out Vector2 aim, out int skillLevel))
            {
                return false;
            }

            switch (slotIndex)
            {
                case 0:
                    StartCoroutine(SonTinhSkill1BoulderThrowRoutine(aim, targetPosition, skillLevel, enemyLayers));
                    break;
                case 1:
                    StartCoroutine(SonTinhSkill2RaiseMountainAtEnemyRoutine(aim, targetPosition, skillLevel, enemyLayers));
                    break;
                case 2:
                    StartCoroutine(SonTinhSkill3CascadingRidgesRoutine(aim, skillLevel, enemyLayers));
                    break;
                case 3:
                    StartCoroutine(SonTinhSkill4SurroundingMountainsRoutine(skillLevel, enemyLayers));
                    break;
            }

            return true;
        }

        private bool BeginCustomSkillCast(SkillData[] skillSet, int slotIndex, Vector2? targetPosition,
            out Vector2 aim, out int skillLevel)
        {
            aim = Vector2.right;
            skillLevel = 1;

            if (health != null && !health.IsAlive)
            {
                return false;
            }

            if (GetSlotCooldownRemaining(slotIndex) > 0.001f)
            {
                return false;
            }

            EnsureSkillDefinitions();
            SkillData data = skillSet != null && slotIndex < skillSet.Length ? skillSet[slotIndex] : null;
            if (data == null)
            {
                return false;
            }

            if (stats != null && !stats.SpendMana(data.ManaCost))
            {
                return false;
            }

            skillLevel = GetEffectiveSkillLevel(slotIndex);
            float cooldownRate = stats != null ? Mathf.Max(0.1f, stats.CooldownRate) : 1f;
            float duration = (data.Cooldown / (1f + Mathf.Max(0, skillLevel - 1) * 0.04f)) / cooldownRate;
            slotCooldownDurations[slotIndex] = duration;
            slotCooldownEndTimes[slotIndex] = Time.time + duration;

            aim = combat != null ? combat.AimDirection : Vector2.right;
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
            return true;
        }

        // =====================================================================
        // SON TINH — 4 SIGNATURE MOUNTAIN & BOULDER SKILLS
        // =====================================================================

        /// <summary>
        /// Skill 1 (Q): Lifts a giant blue-grey boulder overhead (matching the reference illustration)
        /// and hurls it at the enemy, shattering for heavy AoE damage and knockback.
        /// </summary>
        private IEnumerator SonTinhSkill1BoulderThrowRoutine(Vector2 aim, Vector2? targetPosition, int level, LayerMask enemyLayers)
        {
            isExecutingSkill = true;
            activeSonTinhPose = SonTinhPose.BoulderLift;
            sonTinhPoseUntil = Time.time + 0.36f;
            movement?.LockMovement(0.20f);

            float maxRange = 6.2f * (1f + (level - 1) * 0.06f);
            float impactRadius = 2.65f * (1f + (level - 1) * 0.05f);

            // Brief overhead boulder-lift windup so the iconic pose is clearly visible
            yield return new WaitForSeconds(0.10f);

            Vector2 origin = transform.position;
            Vector2 impactPoint = ResolveBoulderTarget(origin, aim, targetPosition, maxRange, enemyLayers);
            Vector2 throwDir = impactPoint - origin;
            if (throwDir.sqrMagnitude > 0.001f)
            {
                throwDir.Normalize();
                combat?.SetAimDirection(throwDir);
            }
            else
            {
                throwDir = aim;
            }

            const float flightTime = 0.20f;
            SonTinhVfx.SpawnSkill1BoulderThrow(origin, impactPoint, flightTime, impactRadius);

            yield return new WaitForSeconds(flightTime);

            float baseDamage = 72f * (1f + (level - 1) * 0.2f);
            float bonusDamage = stats != null ? stats.BaseDamage * 0.95f : 0f;
            bool critical = stats != null && UnityEngine.Random.value < stats.CriticalChance;
            float totalDamage = (baseDamage + bonusDamage) * (critical ? 1.5f : 1f);

            HashSet<int> hitIds = new HashSet<int>();
            // Damage enemies in the impact crater + any enemy clipped along the boulder's throw corridor
            DamageEnemiesInCircle(impactPoint, impactRadius, totalDamage, throwDir, 7.5f, critical, enemyLayers, hitIds, 0.35f, 1.4f);

            float throwDist = Vector2.Distance(origin, impactPoint);
            if (throwDist > 1.2f)
            {
                Vector2 midCenter = (origin + impactPoint) * 0.5f;
                float angleDeg = Mathf.Atan2(throwDir.y, throwDir.x) * Mathf.Rad2Deg;
                Collider2D[] corridorHits = Physics2D.OverlapBoxAll(midCenter, new Vector2(throwDist, 1.6f), angleDeg, enemyLayers);
                foreach (Collider2D hit in corridorHits)
                {
                    TryApplySkillDamage(hit, origin, throwDir, totalDamage, 7.0f, critical, hitIds, 0.35f, 1.2f);
                }
            }

            yield return new WaitForSeconds(0.10f);
            isExecutingSkill = false;
        }

        /// <summary>
        /// Skill 2 (E): Raises both hands palms-up skyward ("2 tay ngửa ra dâng lên cao")
        /// to erupt a colossal rocky mountain right at the enemy's area ("ở khu vực có địch").
        /// </summary>
        private IEnumerator SonTinhSkill2RaiseMountainAtEnemyRoutine(Vector2 aim, Vector2? targetPosition, int level, LayerMask enemyLayers)
        {
            isExecutingSkill = true;
            activeSonTinhPose = SonTinhPose.PalmsRaiseSkyward;
            sonTinhPoseUntil = Time.time + 0.54f;
            movement?.LockMovement(0.28f);

            float searchRange = 7.4f * (1f + (level - 1) * 0.06f);
            float mountainRadius = 2.85f * (1f + (level - 1) * 0.05f);
            Vector2 origin = transform.position;
            Vector2 zoneCenter = ResolveEnemyMountainZone(origin, aim, targetPosition, searchRange, enemyLayers);

            Vector2 toZone = zoneCenter - origin;
            if (toZone.sqrMagnitude > 0.001f)
            {
                combat?.SetAimDirection(toZone.normalized);
            }

            const float preTremor = 0.12f;
            SonTinhVfx.SpawnSkill2RaiseMountainAtEnemy(transform, zoneCenter, mountainRadius, preTremor);

            yield return new WaitForSeconds(preTremor);

            // Primary mountain eruption damage
            float eruptionDamage = (68f * (1f + (level - 1) * 0.2f)) + (stats != null ? stats.BaseDamage * 0.90f : 0f);
            bool critical = stats != null && UnityEngine.Random.value < stats.CriticalChance;
            HashSet<int> eruptionHits = new HashSet<int>();
            DamageEnemiesAroundPointRadially(zoneCenter, mountainRadius, eruptionDamage * (critical ? 1.5f : 1f),
                6.8f, critical, enemyLayers, eruptionHits, 0.40f, 2.0f);

            // Secondary seismic tremor on the risen mountain slopes
            yield return new WaitForSeconds(0.20f);
            float aftershockDamage = (26f * (1f + (level - 1) * 0.2f)) + (stats != null ? stats.BaseDamage * 0.35f : 0f);
            HashSet<int> aftershockHits = new HashSet<int>();
            DamageEnemiesAroundPointRadially(zoneCenter, mountainRadius, aftershockDamage,
                3.5f, false, enemyLayers, aftershockHits, 0f, 0f);

            yield return new WaitForSeconds(0.14f);
            isExecutingSkill = false;
        }

        /// <summary>
        /// Skill 3 (R): Slams both fists into the earth ("đấm lòng đất") to unleash a chain of
        /// 5 consecutive mountain ridges advancing forward ("dãy núi liên hoàn tiến về phía trước").
        /// </summary>
        private IEnumerator SonTinhSkill3CascadingRidgesRoutine(Vector2 dir, int level, LayerMask enemyLayers)
        {
            isExecutingSkill = true;
            activeSonTinhPose = SonTinhPose.GroundSlam;
            sonTinhPoseUntil = Time.time + 0.56f;
            movement?.LockMovement(0.32f);

            yield return new WaitForSeconds(0.08f);

            Vector2 origin = transform.position;
            SonTinhVfx.SpawnSkill3GroundSlamOrigin(origin, dir);

            const int ridgeCount = 5;
            float stepDistance = 1.32f * (1f + (level - 1) * 0.04f);
            float ridgeWidth = 2.65f * (1f + (level - 1) * 0.04f);
            const float ridgeDepth = 1.85f;
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            float baseFirstHit = (60f * (1f + (level - 1) * 0.2f)) + (stats != null ? stats.BaseDamage * 0.85f : 0f);
            Dictionary<int, int> hitCountsByEnemy = new Dictionary<int, int>();

            for (int w = 0; w < ridgeCount; w++)
            {
                if (health != null && !health.IsAlive)
                {
                    break;
                }

                float dist = (w + 1) * stepDistance;
                Vector2 ridgeCenter = origin + dir * dist;
                SonTinhVfx.SpawnSkill3MountainRidgeWave(ridgeCenter, dir, ridgeWidth, w);

                bool critical = stats != null && UnityEngine.Random.value < stats.CriticalChance;
                Collider2D[] hits = Physics2D.OverlapBoxAll(ridgeCenter, new Vector2(ridgeDepth, ridgeWidth), angleDeg, enemyLayers);
                HashSet<int> waveHitIds = new HashSet<int>();

                foreach (Collider2D hit in hits)
                {
                    if (hit == null || hit.CompareTag("Player"))
                    {
                        continue;
                    }

                    IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                    int rootId = hit.transform.root.gameObject.GetHashCode();
                    if (damageable == null || !damageable.IsAlive || !waveHitIds.Add(rootId))
                    {
                        continue;
                    }

                    hitCountsByEnemy.TryGetValue(rootId, out int priorHits);
                    hitCountsByEnemy[rootId] = priorHits + 1;

                    float scale = priorHits == 0 ? 1f : 0.35f;
                    float dmg = baseFirstHit * scale * (critical ? 1.5f : 1f);
                    damageable.TakeDamage(new DamageInfo(dmg, gameObject,
                        hit.ClosestPoint(ridgeCenter), dir, priorHits == 0 ? 6.2f : 3.5f, critical));
                }

                yield return new WaitForSeconds(0.08f);
            }

            yield return new WaitForSeconds(0.08f);
            isExecutingSkill = false;
        }

        /// <summary>
        /// Skill 4 (F): Raises both hands palms-up skyward ("2 tay ngửa ra dâng lên")
        /// to erect concentric 360-degree mountain ranges around Son Tinh when surrounded by enemies.
        /// </summary>
        private IEnumerator SonTinhSkill4SurroundingMountainsRoutine(int level, LayerMask enemyLayers)
        {
            isExecutingSkill = true;
            activeSonTinhPose = SonTinhPose.PalmsRaiseSkyward;
            const float totalDuration = 0.92f;
            sonTinhPoseUntil = Time.time + totalDuration;
            movement?.LockMovement(0.36f);
            health?.BeginDodgeInvulnerability(0.45f);

            float radius = 3.85f * (1f + (level - 1) * 0.06f);
            SonTinhVfx.SpawnSkill4SurroundingMountains(transform, radius, totalDuration);

            const int pulseCount = 3;
            float basePerPulse = (38f * (1f + (level - 1) * 0.2f)) + (stats != null ? stats.BaseDamage * 0.45f : 0f);

            yield return new WaitForSeconds(0.08f);

            for (int p = 0; p < pulseCount; p++)
            {
                if (health != null && !health.IsAlive)
                {
                    break;
                }

                bool critical = stats != null && UnityEngine.Random.value < stats.CriticalChance;
                float pulseDmg = basePerPulse * (critical ? 1.5f : 1f);
                HashSet<int> pulseHits = new HashSet<int>();
                DamageEnemiesAroundPointRadially(transform.position, radius, pulseDmg,
                    6.5f, critical, enemyLayers, pulseHits, 0.35f, 1.5f);

                yield return new WaitForSeconds(0.22f);
            }

            isExecutingSkill = false;
        }

        private Vector2 ResolveBoulderTarget(Vector2 origin, Vector2 aim, Vector2? targetPosition, float maxRange, LayerMask enemyLayers)
        {
            if (targetPosition.HasValue)
            {
                Vector2 delta = targetPosition.Value - origin;
                float clampedDist = Mathf.Clamp(delta.magnitude, 1.2f, maxRange);
                Vector2 desired = delta.sqrMagnitude > 0.001f ? origin + delta.normalized * clampedDist : origin + aim * 3.5f;

                if (TryFindNearestEnemyNearPoint(desired, 1.8f, origin, maxRange, enemyLayers, out Vector2 snappedEnemy))
                {
                    return snappedEnemy;
                }

                return desired;
            }

            // Look for an enemy ahead in the aim cone first
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, maxRange, enemyLayers);
            float bestScore = float.PositiveInfinity;
            Vector2 bestPos = Vector2.zero;
            bool found = false;

            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit.CompareTag("Player"))
                {
                    continue;
                }

                IDamageable dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive)
                {
                    continue;
                }

                Vector2 pos = hit.transform.position;
                Vector2 toEnemy = pos - origin;
                float dist = toEnemy.magnitude;
                if (dist < 0.2f)
                {
                    continue;
                }

                float angle = Vector2.Angle(aim, toEnemy);
                if (angle <= 65f)
                {
                    float score = dist + angle * 0.05f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPos = pos;
                        found = true;
                    }
                }
            }

            if (found)
            {
                return bestPos;
            }

            if (Camera.main != null && Mouse.current != null)
            {
                Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 d = mouseWorld - origin;
                if (d.sqrMagnitude > 0.01f)
                {
                    return origin + d.normalized * Mathf.Clamp(d.magnitude, 1.8f, maxRange);
                }
            }

            return origin + aim * Mathf.Min(4.2f, maxRange);
        }

        private Vector2 ResolveEnemyMountainZone(Vector2 origin, Vector2 aim, Vector2? targetPosition, float searchRange, LayerMask enemyLayers)
        {
            if (targetPosition.HasValue)
            {
                Vector2 delta = targetPosition.Value - origin;
                Vector2 clamped = delta.sqrMagnitude > 0.001f
                    ? origin + delta.normalized * Mathf.Min(delta.magnitude, searchRange)
                    : origin + aim * 3.8f;

                if (TryFindNearestEnemyNearPoint(clamped, 3.2f, origin, searchRange, enemyLayers, out Vector2 enemyPos))
                {
                    return enemyPos;
                }

                return clamped;
            }

            // Collect all live enemies within searchRange so the mountain rises right where enemies are!
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, searchRange, enemyLayers);
            List<Vector2> enemyPositions = new List<Vector2>();
            HashSet<int> seenRoots = new HashSet<int>();

            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit.CompareTag("Player"))
                {
                    continue;
                }

                IDamageable dmg = hit.GetComponentInParent<IDamageable>();
                int rootId = hit.transform.root.gameObject.GetHashCode();
                if (dmg != null && dmg.IsAlive && seenRoots.Add(rootId))
                {
                    enemyPositions.Add(hit.transform.position);
                }
            }

            if (enemyPositions.Count > 0)
            {
                // Pick the enemy position with the highest cluster density + aim alignment
                Vector2 bestClusterPos = enemyPositions[0];
                float bestWeight = float.NegativeInfinity;

                for (int i = 0; i < enemyPositions.Count; i++)
                {
                    Vector2 candidate = enemyPositions[i];
                    int neighbors = 0;
                    Vector2 centroid = Vector2.zero;
                    for (int j = 0; j < enemyPositions.Count; j++)
                    {
                        if (Vector2.Distance(candidate, enemyPositions[j]) <= 2.5f)
                        {
                            neighbors++;
                            centroid += enemyPositions[j];
                        }
                    }

                    Vector2 toCand = candidate - origin;
                    float alignment = toCand.sqrMagnitude > 0.01f ? Vector2.Dot(aim, toCand.normalized) : 0f;
                    float weight = neighbors * 3.0f + alignment * 1.5f - toCand.magnitude * 0.12f;
                    if (weight > bestWeight)
                    {
                        bestWeight = weight;
                        bestClusterPos = neighbors > 0 ? centroid / neighbors : candidate;
                    }
                }

                return bestClusterPos;
            }

            return origin + aim * Mathf.Min(3.8f, searchRange);
        }

        private static bool TryFindNearestEnemyNearPoint(Vector2 point, float snapRadius, Vector2 origin, float maxOriginDist,
            LayerMask enemyLayers, out Vector2 enemyPos)
        {
            enemyPos = point;
            Collider2D[] hits = Physics2D.OverlapCircleAll(point, snapRadius, enemyLayers);
            float nearestSq = float.PositiveInfinity;
            bool found = false;

            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit.CompareTag("Player"))
                {
                    continue;
                }

                IDamageable dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive)
                {
                    continue;
                }

                Vector2 pos = hit.transform.position;
                if (Vector2.Distance(origin, pos) > maxOriginDist + 0.5f)
                {
                    continue;
                }

                float sq = (pos - point).sqrMagnitude;
                if (sq < nearestSq)
                {
                    nearestSq = sq;
                    enemyPos = pos;
                    found = true;
                }
            }

            return found;
        }

        private void DamageEnemiesInCircle(Vector2 center, float radius, float damage, Vector2 pushDir, float knockbackForce,
            bool critical, LayerMask enemyLayers, HashSet<int> hitIds, float slowAmount, float slowDuration)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayers);
            foreach (Collider2D hit in hits)
            {
                TryApplySkillDamage(hit, center, pushDir, damage, knockbackForce, critical, hitIds, slowAmount, slowDuration);
            }
        }

        private void DamageEnemiesAroundPointRadially(Vector2 center, float radius, float damage, float knockbackForce,
            bool critical, LayerMask enemyLayers, HashSet<int> hitIds, float slowAmount, float slowDuration)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayers);
            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                Vector2 push = (Vector2)hit.transform.position - center;
                if (push.sqrMagnitude < 0.001f)
                {
                    push = Vector2.right;
                }

                TryApplySkillDamage(hit, center, push.normalized, damage, knockbackForce, critical, hitIds, slowAmount, slowDuration);
            }
        }

        private void TryApplySkillDamage(Collider2D hit, Vector2 hitOrigin, Vector2 pushDir, float damage, float knockbackForce,
            bool critical, HashSet<int> hitIds, float slowAmount, float slowDuration)
        {
            if (hit == null || hit.CompareTag("Player"))
            {
                return;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            int rootId = hit.transform.root.gameObject.GetHashCode();
            if (damageable == null || !damageable.IsAlive || !hitIds.Add(rootId))
            {
                return;
            }

            damageable.TakeDamage(new DamageInfo(damage, gameObject,
                hit.ClosestPoint(hitOrigin), pushDir, knockbackForce, critical));

            if (slowAmount > 0f && slowDuration > 0f)
            {
                EnemyMovement enemyMovement = hit.GetComponentInParent<EnemyMovement>();
                enemyMovement?.ApplySlow(slowAmount, slowDuration);
            }
        }

        // =====================================================================
        // THANH GIONG — 4 SIGNATURE IRON HORSE & IVORY BAMBOO SKILLS
        // =====================================================================

        private IEnumerator Skill1IronSwordHorseRoutine(Vector2 dir, int level, LayerMask enemyLayers)
        {
            isExecutingSkill = true;
            swordPoseUntil = Time.time + 0.42f;
            float range = 3.2f * (1f + (level - 1) * 0.08f);
            ThanhGiongVfx.SpawnSkill1IronSwordHorse(transform, dir, range);

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
            if (thanhGiongSkills == null || thanhGiongSkills.Length != 4 || thanhGiongSkills[0] == null)
            {
                thanhGiongSkills = new[]
                {
                    CreateRuntimeSkillData(
                        "ThanhGiong",
                        SkillId.Fireball,
                        "Kiếm Sắt Chiến Mã",
                        "Thúc ngựa sắt chồm lên phun lửa, kết hợp vung kiếm sắt chém càn quét kẻ địch phía trước.",
                        ThanhGiongVisualFactory.GetSkillIcon(0),
                        55f, 16f, 2.4f, 3.2f,
                        new Color(1f, 0.58f, 0.15f, 1f)),
                    CreateRuntimeSkillData(
                        "ThanhGiong",
                        SkillId.IceDash,
                        "Tre Ngà Quật Địch",
                        "Dùng 1 cây tre ngà khổng lồ quật mạnh xuống kẻ địch phía trước, gây chấn động và làm chậm.",
                        ThanhGiongVisualFactory.GetSkillIcon(1),
                        64f, 20f, 3.6f, 4.6f,
                        new Color(0.68f, 0.88f, 0.22f, 1f)),
                    CreateRuntimeSkillData(
                        "ThanhGiong",
                        SkillId.SwordSpin,
                        "Thần Mã Quật Tre 360°",
                        "Khi đang cưỡi ngựa sắt, vung cụm vài cây tre ngà quật 360 độ liên hoàn quanh bản thân.",
                        ThanhGiongVisualFactory.GetSkillIcon(2),
                        156f, 26f, 5.2f, 3.25f,
                        new Color(0.95f, 0.82f, 0.20f, 1f)),
                    CreateRuntimeSkillData(
                        "ThanhGiong",
                        SkillId.LightningStrike,
                        "Thiết Mã Càn Lướt",
                        "Cưỡi ngựa sắt phun lửa cùng vài cây tre cầm trên tay càn lướt xuyên qua đám kẻ địch.",
                        ThanhGiongVisualFactory.GetSkillIcon(3),
                        118f, 28f, 5.8f, 5.8f,
                        new Color(1f, 0.42f, 0.12f, 1f))
                };
            }

            if (sonTinhSkills == null || sonTinhSkills.Length != 4 || sonTinhSkills[0] == null)
            {
                sonTinhSkills = new[]
                {
                    CreateRuntimeSkillData(
                        "SonTinh",
                        SkillId.Fireball,
                        "Ném Đại Thạch",
                        "Nâng tảng đá lớn qua đầu rồi ném mạnh vào kẻ địch, đập vỡ gây sát thương nổ diện rộng.",
                        SonTinhVisualFactory.GetSkillIcon(0),
                        72f, 16f, 2.6f, 6.2f,
                        new Color(0.45f, 0.68f, 0.96f, 1f)),
                    CreateRuntimeSkillData(
                        "SonTinh",
                        SkillId.IceDash,
                        "Dâng Núi Trấn Địch",
                        "Dùng 2 tay ngửa ra dâng lên cao để dâng thành ngọn núi trồi lên ngay khu vực có địch.",
                        SonTinhVisualFactory.GetSkillIcon(1),
                        94f, 22f, 4.0f, 7.4f,
                        new Color(0.42f, 0.82f, 0.48f, 1f)),
                    CreateRuntimeSkillData(
                        "SonTinh",
                        SkillId.SwordSpin,
                        "Đấm Đất Dựng Núi",
                        "Đấm mạnh xuống lòng đất tạo các dãy núi liên hoàn tiến thẳng về phía trước nghiền nát địch.",
                        SonTinhVisualFactory.GetSkillIcon(2),
                        110f, 25f, 4.8f, 6.8f,
                        new Color(0.96f, 0.68f, 0.22f, 1f)),
                    CreateRuntimeSkillData(
                        "SonTinh",
                        SkillId.LightningStrike,
                        "Bát Phương Quần Sơn",
                        "Dùng 2 tay ngửa ra dâng lên cao dựng các dãy núi 360° vây quanh bản thân khi bị địch bao vây.",
                        SonTinhVisualFactory.GetSkillIcon(3),
                        114f, 28f, 5.6f, 3.85f,
                        new Color(1f, 0.84f, 0.28f, 1f))
                };
            }
        }

        private static SkillData CreateRuntimeSkillData(string heroPrefix, SkillId id, string displayName, string description,
            Sprite icon, float damage, float manaCost, float cooldown, float range, Color color)
        {
            SkillData data = ScriptableObject.CreateInstance<SkillData>();
            data.hideFlags = HideFlags.DontSave;
            data.name = heroPrefix + "_" + id;
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
            if (visualRenderer == null || visualRenderer.gameObject == gameObject)
            {
                if (movement != null && movement.VisualRenderer != null)
                {
                    visualRenderer = movement.VisualRenderer;
                }
                else
                {
                    Transform child = transform.Find("MovementVisual");
                    if (child != null)
                    {
                        visualRenderer = child.GetComponent<SpriteRenderer>();
                    }
                    else if (visualRenderer == null)
                    {
                        visualRenderer = GetComponent<SpriteRenderer>();
                    }
                }
            }

            if (visualRenderer != null)
            {
                visualRenderer.enabled = true;
            }
        }
    }
}
