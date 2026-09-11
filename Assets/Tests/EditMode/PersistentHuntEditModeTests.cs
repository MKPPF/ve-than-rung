using System.Reflection;
using NUnit.Framework;
using TopDownGame.Audio;
using TopDownGame.Enemies.Progression;
using TopDownGame.Enemies.Spawning;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Skills;
using TopDownGame.Equipment;
using TopDownGame.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace TopDownGame.Tests.EditMode
{
    public sealed class PersistentHuntEditModeTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance |
            BindingFlags.NonPublic;

        [Test]
        public void HuntingZoneDefaultsUseFastBoundedRespawn()
        {
            GameObject root = new GameObject("SpawnSettingsProbe");
            EnemySpawnManager manager = root.AddComponent<EnemySpawnManager>();
            Assert.That(manager.NormalRespawnSeconds.x,
                Is.InRange(2f, 5f));
            Assert.That(manager.NormalRespawnSeconds.y,
                Is.InRange(2f, 5f));
            Assert.That(manager.MinimumPlayerDistance,
                Is.InRange(6f, 10f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void RegionLevelRangesAndPopulationAreLongTermProgressionReady()
        {
            MapData map01 = AssetDatabase.LoadAssetAtPath<MapData>(
                "Assets/ScriptableObjects/Maps/Map01.asset");
            MapData map02 = AssetDatabase.LoadAssetAtPath<MapData>(
                "Assets/ScriptableObjects/Maps/Map02.asset");
            MapData map03 = AssetDatabase.LoadAssetAtPath<MapData>(
                "Assets/ScriptableObjects/Maps/Map03.asset");
            MapData cave = AssetDatabase.LoadAssetAtPath<MapData>(
                "Assets/ScriptableObjects/Maps/CaveMap01.asset");
            Assert.That(map01.RecommendedMinLevel, Is.EqualTo(1));
            Assert.That(map01.RecommendedMaxLevel, Is.EqualTo(10));
            Assert.That(map02.RecommendedMinLevel, Is.EqualTo(8));
            Assert.That(map02.RecommendedMaxLevel, Is.EqualTo(20));
            Assert.That(map03.RecommendedMinLevel, Is.EqualTo(18));
            Assert.That(map03.RecommendedMaxLevel, Is.EqualTo(35));
            Assert.That(cave.RecommendedMinLevel, Is.EqualTo(30));
            Assert.That(cave.RecommendedMaxLevel, Is.EqualTo(50));
            Assert.That(map01.TargetActiveEnemies, Is.InRange(12, 25));
            Assert.That(cave.TargetActiveEnemies, Is.InRange(12, 25));
        }

        [Test]
        public void AudioMixerAndLicensedClipPlaceholdersAreWired()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(
                "Assets/Resources/Audio/GameAudioMixer.mixer");
            Assert.That(mixer, Is.Not.Null);
            Assert.That(mixer.FindMatchingGroups(string.Empty).Length,
                Is.EqualTo(5));
            string[] cues = AssetDatabase.FindAssets("t:AudioCue",
                new[] { "Assets/Resources/Audio/Cues" });
            Assert.That(cues.Length, Is.GreaterThanOrEqualTo(20));
        }

        [Test]
        public void ExperienceCurveIncreasesSmoothlyWithoutHardCap()
        {
            GameObject player = new GameObject("ExperienceCurveProbe");
            PlayerStats stats = player.AddComponent<PlayerStats>();
            int previous = stats.ExperienceToNextLevel;
            FieldInfo levelField = typeof(PlayerStats).GetField("level",
                PrivateInstance);
            for (int level = 2; level <= 100; level++)
            {
                levelField.SetValue(stats, level);
                int current = stats.ExperienceToNextLevel;
                Assert.That(current, Is.GreaterThan(previous));
                Assert.That(current, Is.LessThan(previous * 2),
                    "The XP curve must not jump abruptly between levels.");
                previous = current;
            }
            Object.DestroyImmediate(player);
        }

        [Test]
        public void LevelUpsGrantSpendablePersistentPotentialPoints()
        {
            GameObject player = new GameObject("PotentialProgressionProbe");
            PlayerStats stats = player.AddComponent<PlayerStats>();
            stats.AddExperience(stats.ExperienceToNextLevel);
            Assert.That(stats.Level, Is.EqualTo(2));
            Assert.That(stats.AvailablePotentialPoints, Is.EqualTo(1));

            float healthBeforeSpend = stats.MaxHealth;
            Assert.That(stats.TrySpendPotential(PotentialAttribute.Vitality), Is.True);
            Assert.That(stats.AvailablePotentialPoints, Is.Zero);
            Assert.That(stats.VitalityPoints, Is.EqualTo(1));
            Assert.That(stats.MaxHealth, Is.EqualTo(healthBeforeSpend +
                PlayerStats.VitalityHealthPerPoint).Within(0.001f));
            Assert.That(stats.TrySpendPotential(PotentialAttribute.Power), Is.False,
                "Potential cannot be spent twice.");

            stats.LoadPotentialProgress(3, 2, 4, 5, 6);
            Assert.That(stats.AvailablePotentialPoints, Is.EqualTo(3));
            Assert.That(stats.TotalPotentialPoints, Is.EqualTo(20));
            Object.DestroyImmediate(player);
        }

        [Test]
        public void SkillUpgradesAreCappedSyncedAndIncreaseInCost()
        {
            GameObject player = new GameObject("SkillUpgradeProbe");
            SkillInventory inventory = player.AddComponent<SkillInventory>();
            inventory.AddOrUpgrade("Fireball", 5);
            Assert.That(inventory.TryEquip("Fireball", 0), Is.True);
            Assert.That(inventory.TryUpgrade("Fireball", 5), Is.True);
            Assert.That(inventory.GetLevel("Fireball"), Is.EqualTo(2));
            Assert.That(inventory.Slots[0].level, Is.EqualTo(2),
                "Equipped slots must immediately receive the upgraded level.");
            Assert.That(SkillInventory.UpgradeGoldCost(2),
                Is.GreaterThan(SkillInventory.UpgradeGoldCost(1)));

            inventory.TryUpgrade("Fireball", 5);
            inventory.TryUpgrade("Fireball", 5);
            inventory.TryUpgrade("Fireball", 5);
            Assert.That(inventory.GetLevel("Fireball"), Is.EqualTo(5));
            Assert.That(inventory.TryUpgrade("Fireball", 5), Is.False,
                "Skills cannot exceed their authored maximum level.");
            Object.DestroyImmediate(player);
        }

        [Test]
        public void EquipmentEnhancementHasBoundedPredictableGrowth()
        {
            Assert.That(EquipmentInventory.EnhancementMultiplier(0), Is.EqualTo(1f));
            Assert.That(EquipmentInventory.EnhancementMultiplier(10),
                Is.EqualTo(1.8f).Within(0.001f));
            int previous = GameProgressManager.EnhancementEssenceCost(0);
            for (int level = 1; level < EquipmentInventory.MaximumEnhancementLevel; level++)
            {
                int current = GameProgressManager.EnhancementEssenceCost(level);
                Assert.That(current, Is.GreaterThan(previous));
                previous = current;
            }

            EquipmentData common = ScriptableObject.CreateInstance<EquipmentData>();
            EquipmentData legendary = ScriptableObject.CreateInstance<EquipmentData>();
            typeof(EquipmentData).GetField("price", PrivateInstance)?.SetValue(common, 100);
            typeof(EquipmentData).GetField("price", PrivateInstance)?.SetValue(legendary, 650);
            Assert.That(legendary.DuplicateEssenceValue,
                Is.GreaterThan(common.DuplicateEssenceValue));
            Object.DestroyImmediate(common);
            Object.DestroyImmediate(legendary);
        }
    }
}
