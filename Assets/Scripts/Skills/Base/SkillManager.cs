using System.Collections;
using System.Collections.Generic;
using TopDownGame.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownGame.Skills
{
    public sealed class SkillManager : MonoBehaviour
    {
        [SerializeField] private SkillData[] skillDatabase;

        private readonly SkillCooldown cooldown = new SkillCooldown();
        private readonly Dictionary<SkillId, SkillBase> skillComponents =
            new Dictionary<SkillId, SkillBase>();
        private SkillInventory inventory;
        private PlayerStats stats;
        private PlayerInteraction interaction;
        private PlayerMovement movement;
        private PlayerCharacterAnimator characterAnimator;
        private PlayerHealth health;
        private PlayerCombat combat;
        private ThanhGiongHeroController thanhGiongHero;
        private int nextAutoSkillSlot;
        private SkillCastController castController;
        public bool IsCasting => (castController != null && castController.IsCasting) ||
            (thanhGiongHero != null && thanhGiongHero.IsExecutingSkill);
        public const float CastWindup = 0.12f;

        public SkillCooldown Cooldowns => cooldown;
        public SkillInventory Inventory => inventory;

        private void Awake()
        {
            castController=GetComponent<SkillCastController>();
            if(castController==null) castController=gameObject.AddComponent<SkillCastController>();
            inventory = GetComponent<SkillInventory>();
            stats = GetComponent<PlayerStats>();
            interaction = GetComponent<PlayerInteraction>();
            movement = GetComponent<PlayerMovement>();
            characterAnimator = GetComponent<PlayerCharacterAnimator>();
            health = GetComponent<PlayerHealth>();
            combat = GetComponent<PlayerCombat>();
            thanhGiongHero = GetComponent<ThanhGiongHeroController>();

            foreach (SkillBase component in GetComponents<SkillBase>())
            {
                if (component != null && component.Data != null)
                {
                    skillComponents[component.Data.Id] = component;
                }
            }
        }

        private void Start()
        {
            if (skillDatabase == null)
            {
                return;
            }

            foreach (SkillData data in skillDatabase)
            {
                if (data == null)
                {
                    continue;
                }

                if (skillComponents.TryGetValue(data.Id, out SkillBase component))
                {
                    component.Initialize(data, Mathf.Max(1, inventory.GetLevel(data.Id.ToString())));
                }
            }
        }

        public void RefreshSkills()
        {
            if (skillDatabase == null || inventory == null)
            {
                return;
            }

            foreach (SkillData data in skillDatabase)
            {
                if (data != null && skillComponents.TryGetValue(data.Id, out SkillBase component))
                {
                    component.Initialize(data, Mathf.Max(1, inventory.GetLevel(data.Id.ToString())));
                }
            }
        }

        private void Update()
        {
            if (TopDownGame.Core.GameplayState.IsInputBlocked || Time.timeScale <= 0f) return;
            if (Keyboard.current == null || inventory == null)
            {
                return;
            }

            TryCast(0, Keyboard.current.qKey.wasPressedThisFrame);
            TryCast(1, Keyboard.current.eKey.wasPressedThisFrame);
            TryCast(2, Keyboard.current.rKey.wasPressedThisFrame);
            TryCast(3, Keyboard.current.fKey.wasPressedThisFrame);
        }

        public bool TryAutoCast(Vector2 targetPosition, float targetDistance)
        {
            if (TopDownGame.Core.GameplayState.IsInputBlocked || Time.timeScale <= 0f) return false;
            if (inventory == null || stats == null || inventory.Slots.Count == 0)
            {
                return false;
            }

            Vector2 targetDirection = targetPosition - (Vector2)transform.position;
            if (targetDirection.sqrMagnitude > 0.001f)
            {
                combat?.SetAimDirection(targetDirection);
            }

            // Defensive skills are conditional so auto mode does not waste
            // healing at full health or shielding when no enemy is threatening.
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                SkillData data = GetSkillData(i);
                if (data == null || !ShouldAutoCastUtility(data.Id,
                    targetDistance))
                {
                    continue;
                }

                if (TryCastSlot(i, true, targetPosition))
                {
                    nextAutoSkillSlot = (i + 1) % inventory.Slots.Count;
                    return true;
                }
            }

            // Rotate through offensive slots instead of always consuming the
            // first ready skill. This lets Q/E/R/F all participate in combat.
            for (int offset = 0; offset < inventory.Slots.Count; offset++)
            {
                int slotIndex = (nextAutoSkillSlot + offset) %
                    inventory.Slots.Count;
                SkillData data = GetSkillData(slotIndex);
                if (data == null || IsUtilitySkill(data.Id) ||
                    targetDistance > Mathf.Max(0.75f,
                        EffectiveRange(data, inventory.Slots[slotIndex].level)))
                {
                    continue;
                }

                if (TryCastSlot(slotIndex, true, targetPosition))
                {
                    nextAutoSkillSlot = (slotIndex + 1) % inventory.Slots.Count;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A gesture selects an equipped slot without synthesizing keyboard E,
        /// so nearby interactions do not swallow the second gesture skill.
        /// The regular cast pipeline still owns mana, cooldown and alive checks.
        /// </summary>
        public bool TryCastFromGesture(int slotIndex, Vector2 direction)
        {
            SkillData data = GetSkillData(slotIndex);
            if (data == null || direction.sqrMagnitude <= 0.001f ||
                TopDownGame.Core.GameplayState.ModalClosedThisFrame) return false;
            combat?.SetAimDirection(direction);
            Vector2 target = (Vector2)transform.position + direction.normalized *
                EffectiveRange(data, inventory.Slots[slotIndex].level);
            return TryCastSlot(slotIndex, true, target);
        }

        public float GetCooldownRemaining(int slotIndex)
        {
            if (thanhGiongHero == null)
            {
                thanhGiongHero = GetComponent<ThanhGiongHeroController>();
            }
            if (thanhGiongHero != null && thanhGiongHero.IsCustomHeroActive)
            {
                return thanhGiongHero.GetSlotCooldownRemaining(slotIndex);
            }

            if (inventory == null || slotIndex < 0 || slotIndex >= inventory.Slots.Count ||
                inventory.Slots[slotIndex].IsEmpty)
            {
                return 0f;
            }

            return cooldown.Remaining(ParseId(inventory.Slots[slotIndex].skillId));
        }

        public float GetCooldownDuration(int slotIndex)
        {
            if (thanhGiongHero == null)
            {
                thanhGiongHero = GetComponent<ThanhGiongHeroController>();
            }
            if (thanhGiongHero != null && thanhGiongHero.IsCustomHeroActive)
            {
                float tracked = thanhGiongHero.GetSlotCooldownDuration(slotIndex);
                if (tracked > 0f)
                {
                    return tracked;
                }
                SkillData heroData = thanhGiongHero.GetActiveHeroSkillData(slotIndex);
                return heroData != null
                    ? EffectiveCooldown(heroData, thanhGiongHero.GetEffectiveSkillLevel(slotIndex))
                    : 0f;
            }

            SkillData data = GetSkillData(slotIndex);
            return data != null ? EffectiveCooldown(data,
                inventory.Slots[slotIndex].level) : 0f;
        }

        public SkillData GetSkillData(string skillId)
        {
            if (skillDatabase == null || string.IsNullOrWhiteSpace(skillId))
            {
                return null;
            }

            SkillId id = ParseId(skillId);
            foreach (SkillData data in skillDatabase)
            {
                if (data != null && data.Id == id) return data;
            }
            return null;
        }

        public SkillData GetSkillData(int slotIndex)
        {
            if (thanhGiongHero == null)
            {
                thanhGiongHero = GetComponent<ThanhGiongHeroController>();
            }
            if (thanhGiongHero != null && thanhGiongHero.IsCustomHeroActive)
            {
                return thanhGiongHero.GetActiveHeroSkillData(slotIndex);
            }

            if (inventory == null || skillDatabase == null || slotIndex < 0 ||
                slotIndex >= inventory.Slots.Count || inventory.Slots[slotIndex].IsEmpty)
            {
                return null;
            }

            SkillId id = ParseId(inventory.Slots[slotIndex].skillId);
            foreach (SkillData data in skillDatabase)
            {
                if (data != null && data.Id == id)
                {
                    return data;
                }
            }

            return null;
        }

        private void TryCast(int slotIndex, bool pressed)
        {
            if (pressed)
            {
                TryCastSlot(slotIndex, false, null);
            }
        }

        private bool TryCastSlot(int slotIndex, bool ignoreInteraction,
            Vector2? targetPosition)
        {
            if (GetComponent<PlayerDodge>()?.IsDodging == true || IsCasting || castController==null || !castController.isActiveAndEnabled ||
                !isActiveAndEnabled || inventory == null || stats == null ||
                (health != null && !health.IsAlive) ||
                TopDownGame.Core.GameplayState.IsInputBlocked || Time.timeScale <= 0f ||
                slotIndex < 0 || slotIndex >= inventory.Slots.Count)
            {
                return false;
            }

            if (!ignoreInteraction && slotIndex == 1 && interaction != null &&
                interaction.HasTarget)
            {
                return false;
            }

            if (thanhGiongHero == null)
            {
                thanhGiongHero = GetComponent<ThanhGiongHeroController>();
            }
            if (thanhGiongHero != null && thanhGiongHero.IsCustomHeroActive)
            {
                return thanhGiongHero.TryCastActiveHeroSkill(slotIndex, targetPosition, ~0);
            }

            SkillSlot slot = inventory.Slots[slotIndex];
            if (slot.IsEmpty)
            {
                return false;
            }

            SkillId id = ParseId(slot.skillId);
            if (!skillComponents.TryGetValue(id, out SkillBase skill) ||
                !skill.IsInitialized || !cooldown.IsReady(id))
            {
                return false;
            }

            SkillData data = skill.Data;
            if (!stats.SpendMana(data.ManaCost))
            {
                return false;
            }

            Vector2 aim = combat != null ? combat.AimDirection : Vector2.right;
            Vector2 point = targetPosition ?? (Camera.main != null && Mouse.current != null
                ? (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue())
                : (Vector2)transform.position + aim * data.Range);
            if ((point - (Vector2)transform.position).sqrMagnitude > 0.001f)
                aim = (point - (Vector2)transform.position).normalized;
            cooldown.Start(id, EffectiveCooldown(data, slot.level),
                stats.CooldownRate);
            castController.Begin(skill,slot.level,point,aim);
            return true;
        }

        private void OnDisable()
        {
            castController?.Cancel();
        }

        private static float EffectiveCooldown(SkillData data, int skillLevel)
        {
            if (data == null) return 0f;
            return data.Cooldown / (1f + Mathf.Max(0, skillLevel - 1) * 0.04f);
        }

        private static float EffectiveRange(SkillData data, int skillLevel)
        {
            if (data == null) return 0f;
            return data.Range * (1f + Mathf.Max(0, skillLevel - 1) * 0.1f);
        }

        private bool ShouldAutoCastUtility(SkillId id, float targetDistance)
        {
            if (id == SkillId.Healing)
            {
                return health != null && health.IsAlive &&
                    health.CurrentHealth <= stats.MaxHealth * 0.55f;
            }

            return id == SkillId.Shield && targetDistance <= 3f &&
                health != null && health.IsAlive &&
                health.CurrentHealth <= stats.MaxHealth * 0.8f;
        }

        private static bool IsUtilitySkill(SkillId id)
        {
            return id == SkillId.Healing || id == SkillId.Shield;
        }

        private static SkillId ParseId(string id)
        {
            return System.Enum.TryParse(id, true, out SkillId result)
                ? result
                : SkillId.Fireball;
        }
    }
}
