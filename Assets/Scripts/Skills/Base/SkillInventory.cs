using System;
using System.Collections.Generic;
using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.Skills
{
    public sealed class SkillInventory : MonoBehaviour
    {
        [SerializeField] private SkillSlot[] slots =
        {
            new SkillSlot { key = "Q" },
            new SkillSlot { key = "E" },
            new SkillSlot { key = "R" },
            new SkillSlot { key = "F" }
        };

        private readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public IReadOnlyList<SkillSlot> Slots => slots;
        public IReadOnlyDictionary<string, int> OwnedSkills => levels;
        public event Action Changed;

        private void Awake()
        {
            if (slots == null || slots.Length != 4)
            {
                slots = new[]
                {
                    new SkillSlot { key = "Q" },
                    new SkillSlot { key = "E" },
                    new SkillSlot { key = "R" },
                    new SkillSlot { key = "F" }
                };
            }

            foreach (SkillSlot slot in slots)
            {
                if (slot != null && !slot.IsEmpty)
                {
                    levels[slot.skillId] = Mathf.Max(1, slot.level);
                }
            }
        }

        public int GetLevel(string skillId)
        {
            return !string.IsNullOrWhiteSpace(skillId) && levels.TryGetValue(skillId, out int value)
                ? value
                : 0;
        }

        public bool Owns(string skillId)
        {
            return GetLevel(skillId) > 0;
        }

        public bool TryEquip(string skillId, int slotIndex)
        {
            if (string.IsNullOrWhiteSpace(skillId) || slotIndex < 0 ||
                slotIndex >= slots.Length)
            {
                return false;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (i != slotIndex && slots[i].skillId == skillId)
                {
                    return false;
                }
            }

            slots[slotIndex].skillId = skillId;
            slots[slotIndex].level = Mathf.Max(1, GetLevel(skillId));
            Changed?.Invoke();
            return true;
        }

        public void AddOrUpgrade(string skillId, int maxLevel = int.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return;
            }

            int currentLevel = GetLevel(skillId);
            int nextLevel = currentLevel <= 0
                ? 1
                : Mathf.Min(Mathf.Max(1, maxLevel), currentLevel + 1);
            if (nextLevel == currentLevel) return;
            levels[skillId] = nextLevel;
            SynchronizeEquippedLevel(skillId, nextLevel);
            Changed?.Invoke();
        }

        public bool TryUpgrade(string skillId, int maxLevel)
        {
            int currentLevel = GetLevel(skillId);
            int cap = Mathf.Max(1, maxLevel);
            if (currentLevel <= 0 || currentLevel >= cap)
            {
                return false;
            }

            int nextLevel = currentLevel + 1;
            levels[skillId] = nextLevel;
            SynchronizeEquippedLevel(skillId, nextLevel);
            Changed?.Invoke();
            return true;
        }

        public static int UpgradeGoldCost(int currentLevel)
        {
            int level = Mathf.Clamp(currentLevel, 1, 100);
            return 200 * level * level;
        }

        public void RemoveFromSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < slots.Length)
            {
                slots[slotIndex].skillId = string.Empty;
                slots[slotIndex].level = 1;
                Changed?.Invoke();
            }
        }

        public void LoadProgress(IReadOnlyList<GameProgressManager.SkillProgress> owned,
            IReadOnlyList<string> equipped)
        {
            levels.Clear();
            if (owned != null)
            {
                foreach (GameProgressManager.SkillProgress progress in owned)
                {
                    if (progress != null && !string.IsNullOrWhiteSpace(progress.skillId))
                    {
                        levels[progress.skillId] = Mathf.Max(1, progress.level);
                    }
                }
            }

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].skillId = equipped != null && i < equipped.Count ? equipped[i] : string.Empty;
                slots[i].level = Mathf.Max(1, GetLevel(slots[i].skillId));
            }
            Changed?.Invoke();
        }

        private void SynchronizeEquippedLevel(string skillId, int skillLevel)
        {
            foreach (SkillSlot slot in slots)
            {
                if (slot != null && string.Equals(slot.skillId, skillId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    slot.level = Mathf.Max(1, skillLevel);
                }
            }
        }

        public void WriteProgress(List<GameProgressManager.SkillProgress> owned,
            string[] equipped)
        {
            owned.Clear();
            foreach (KeyValuePair<string, int> pair in levels)
            {
                owned.Add(new GameProgressManager.SkillProgress
                {
                    skillId = pair.Key,
                    level = pair.Value
                });
            }

            for (int i = 0; i < equipped.Length; i++)
            {
                equipped[i] = i < slots.Length ? slots[i].skillId : string.Empty;
            }
        }
    }
}
