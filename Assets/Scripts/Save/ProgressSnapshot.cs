using System;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Save
{
    public enum ProgressLoadStatus
    {
        Missing,
        Ready,
        RecoveryAvailable,
        Unreadable,
        Recovered
    }

    /// <summary>Parses v1 saves away from the live manager so rejected or incomplete data cannot partially change a running campaign.</summary>
    [Serializable]
    public sealed class ProgressSnapshot
    {
        // These core fields existed in the original v1 save. Sentinels reject
        // unrelated JSON objects instead of treating them as a fresh campaign.
        public string currentMap;
        public float currentHealth = float.NaN;
        public float maxHealth = float.NaN;
        public float currentMana = float.NaN;
        public int gold = -1;
        public float baseDamage = float.NaN;
        public float armor = float.NaN;
        public float cooldownRate = float.NaN;
        public int playerLevel = 1;
        public int experience;
        public int potentialPoints, vitalityPoints, powerPoints, guardPoints, hastePoints;
        public bool potentialInitialized;
        public int hunterEssence;
        public List<GameProgressManager.EquipmentEnhancementProgress> equipmentEnhancements = new List<GameProgressManager.EquipmentEnhancementProgress>();
        public List<string> items = new List<string>();
        public List<GameProgressManager.SkillProgress> ownedSkills = new List<GameProgressManager.SkillProgress>();
        public string[] equippedSkills = new string[4];
        public List<string> defeatedBosses = new List<string>();
        public string equippedWeaponId, equippedHelmetId, equippedArmorId, equippedPantsId, equippedNecklaceId;
        public List<string> chestItems = new List<string>();
        public bool chestInitialized;
        public List<string> unlockedRegions = new List<string>();
        public string safeRespawnScene;
        public Vector3 safeRespawnPosition;
        public bool hasSafeRespawn;
        public bool tutorialInitialized, tutorialCompleted;
        [NonSerialized] public bool needsMigration;

        public static bool TryDeserialize(string json, out ProgressSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal)) return false;
            try
            {
                var candidate = new ProgressSnapshot();
                JsonUtility.FromJsonOverwrite(trimmed, candidate);
                if (!candidate.ValidateAndNormalize()) return false;
                snapshot = candidate;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ValidateAndNormalize()
        {
            if (string.IsNullOrWhiteSpace(currentMap) || !Finite(maxHealth) || maxHealth <= 0 ||
                !Finite(currentHealth) || currentHealth < 0 || !Finite(currentMana) || currentMana < 0 ||
                !Finite(baseDamage) || baseDamage < 0 || !Finite(armor) || armor < 0 ||
                !Finite(cooldownRate) || cooldownRate <= 0 || gold < 0 || playerLevel < 1 || experience < 0 ||
                potentialPoints < 0 || vitalityPoints < 0 || powerPoints < 0 || guardPoints < 0 || hastePoints < 0 || hunterEssence < 0)
                return false;
            if (hasSafeRespawn && (string.IsNullOrWhiteSpace(safeRespawnScene) ||
                !Finite(safeRespawnPosition.x) || !Finite(safeRespawnPosition.y) || !Finite(safeRespawnPosition.z))) return false;

            items ??= new List<string>();
            ownedSkills ??= new List<GameProgressManager.SkillProgress>();
            equipmentEnhancements ??= new List<GameProgressManager.EquipmentEnhancementProgress>();
            defeatedBosses ??= new List<string>();
            chestItems ??= new List<string>();
            unlockedRegions ??= new List<string>();
            foreach (string item in items) if (string.IsNullOrWhiteSpace(item)) return false;
            foreach (string item in chestItems) if (string.IsNullOrWhiteSpace(item)) return false;
            foreach (var skill in ownedSkills) if (skill == null || string.IsNullOrWhiteSpace(skill.skillId) || skill.level < 1) return false;
            foreach (var equipment in equipmentEnhancements) if (equipment == null || string.IsNullOrWhiteSpace(equipment.itemId) || equipment.level < 0) return false;
            if (equippedSkills == null || equippedSkills.Length != 4) Array.Resize(ref equippedSkills, 4);
            if (!unlockedRegions.Contains("Lobby")) unlockedRegions.Add("Lobby");
            if (!unlockedRegions.Contains("Map01")) unlockedRegions.Add("Map01");

            needsMigration = !potentialInitialized || !tutorialInitialized;
            if (!potentialInitialized)
            {
                potentialPoints = Mathf.Max(0, playerLevel - 1);
                potentialInitialized = true;
            }
            if (!tutorialInitialized)
            {
                tutorialInitialized = true;
                tutorialCompleted = true;
            }
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
