using System;
using System.Collections.Generic;
using NUnit.Framework;
using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    /// <summary>Checks v1 compatibility and rejection before any live manager or preference is touched.</summary>
    public sealed class ProgressSnapshotTests
    {
        private static ProgressSnapshot Valid() => new ProgressSnapshot
        {
            currentMap = "Map03", currentHealth = 135, maxHealth = 100, currentMana = 22,
            gold = 2727, baseDamage = 23, armor = 8, cooldownRate = 1,
            playerLevel = 4, experience = 23, potentialInitialized = true,
            potentialPoints = 1, vitalityPoints = 1, guardPoints = 1, hunterEssence = 19,
            tutorialInitialized = true, tutorialCompleted = true,
            items = new List<string> { "weapon", "relic", "relic" },
            chestItems = new List<string> { "helmet" }, chestInitialized = true,
            ownedSkills = new List<GameProgressManager.SkillProgress> { new GameProgressManager.SkillProgress { skillId = "Shield", level = 3 } },
            equippedSkills = new[] { "Shield", "", "", "" }, equippedWeaponId = "weapon",
            equipmentEnhancements = new List<GameProgressManager.EquipmentEnhancementProgress> { new GameProgressManager.EquipmentEnhancementProgress { itemId = "weapon", level = 4 } },
            defeatedBosses = new List<string> { "guardian" }, unlockedRegions = new List<string> { "Lobby", "Map01", "Map02", "Map03" },
            hasSafeRespawn = true, safeRespawnScene = "Map03", safeRespawnPosition = new Vector3(3, 4, 0)
        };

        [Test]
        public void IncompleteUnrelatedAndMalformedDocumentsCannotBecomeAPlayableSave()
        {
            foreach (string json in new[] { null, "", " ", "null", "[]", "{}", "{\"gold\":3000}", "{\"currentMap\":\"Lobby\"}", "{\"gold\":3,", JsonUtility.ToJson(Valid()) + " trailing" })
            {
                Assert.That(ProgressSnapshot.TryDeserialize(json, out var rejected), Is.False, json);
                Assert.That(rejected, Is.Null);
            }
        }

        [Test]
        public void InvalidVitalsCurrencyProgressionAndEntriesAreRejected()
        {
            var mutations = new Action<ProgressSnapshot>[]
            {
                d => d.maxHealth = 0, d => d.currentHealth = -1, d => d.currentMana = float.NaN,
                d => d.baseDamage = float.PositiveInfinity, d => d.cooldownRate = 0,
                d => d.gold = -1, d => d.playerLevel = 0, d => d.experience = -1,
                d => d.vitalityPoints = -1, d => d.hunterEssence = -1,
                d => d.items.Add(null), d => d.ownedSkills[0].level = 0,
                d => d.equipmentEnhancements[0].level = -1,
                d => d.safeRespawnPosition = new Vector3(float.NaN, 1, 0)
            };
            for (int i = 0; i < mutations.Length; i++)
            {
                var data = Valid(); mutations[i](data);
                Assert.That(ProgressSnapshot.TryDeserialize(JsonUtility.ToJson(data), out _), Is.False, "case " + i);
            }
        }

        [Test]
        public void SnapshotRoundtripPreservesAllCampaignFieldsAndDoesNotShareMutableCollections()
        {
            string json = JsonUtility.ToJson(Valid());
            Assert.That(ProgressSnapshot.TryDeserialize(json, out var first), Is.True);
            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(json));
            Assert.That(ProgressSnapshot.TryDeserialize(json, out var second), Is.True);
            first.items.Clear(); first.ownedSkills[0].level = 1;
            Assert.That(second.items.Count, Is.EqualTo(3));
            Assert.That(second.ownedSkills[0].level, Is.EqualTo(3));
            Assert.That(second.safeRespawnPosition, Is.EqualTo(new Vector3(3,4,0)));
        }

        [Test]
        public void OriginalV1CoreGetsFreshDefaultsForLaterFieldsWithoutInheritingAnotherCampaign()
        {
            const string legacy = "{\"currentMap\":\"Map02\",\"currentHealth\":70,\"maxHealth\":100,\"currentMana\":20,\"gold\":3456,\"baseDamage\":25,\"armor\":6,\"cooldownRate\":1}";
            Assert.That(ProgressSnapshot.TryDeserialize(legacy, out var data), Is.True);
            Assert.That(data.playerLevel, Is.EqualTo(1)); Assert.That(data.experience, Is.Zero);
            Assert.That(data.items, Is.Empty); Assert.That(data.chestItems, Is.Empty);
            Assert.That(data.ownedSkills, Is.Empty); Assert.That(data.equippedSkills.Length, Is.EqualTo(4));
            Assert.That(data.tutorialCompleted, Is.True); Assert.That(data.potentialPoints, Is.Zero);
        }

        [Test]
        public void LegacyPotentialTutorialAndNullCollectionsMigrateExactlyOnce()
        {
            var old = Valid(); old.potentialInitialized = false; old.tutorialInitialized = false;
            old.items = null; old.chestItems = null; old.unlockedRegions = null; old.equippedSkills = null;
            Assert.That(ProgressSnapshot.TryDeserialize(JsonUtility.ToJson(old), out var migrated), Is.True);
            Assert.That(migrated.needsMigration, Is.True); Assert.That(migrated.potentialPoints, Is.EqualTo(3));
            Assert.That(migrated.tutorialCompleted, Is.True); Assert.That(migrated.items, Is.Empty);
            Assert.That(migrated.unlockedRegions, Is.EquivalentTo(new[] { "Lobby", "Map01" }));
            Assert.That(migrated.equippedSkills.Length, Is.EqualTo(4));
            Assert.That(ProgressSnapshot.TryDeserialize(JsonUtility.ToJson(migrated), out var repeated), Is.True);
            Assert.That(repeated.needsMigration, Is.False); Assert.That(repeated.potentialPoints, Is.EqualTo(3));
        }

        [Test]
        public void EquipmentBonusHealthAndDefeatedCheckpointsRemainValid()
        {
            var data = Valid();
            Assert.That(data.currentHealth, Is.GreaterThan(data.maxHealth));
            Assert.That(ProgressSnapshot.TryDeserialize(JsonUtility.ToJson(data), out _), Is.True);
            data.currentHealth = 0;
            Assert.That(ProgressSnapshot.TryDeserialize(JsonUtility.ToJson(data), out _), Is.True);
        }
    }
}
