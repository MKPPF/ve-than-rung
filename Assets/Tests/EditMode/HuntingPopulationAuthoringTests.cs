using System.Linq;
using System.Reflection;
using TopDownGame.EditorTools;
using UnityEditor;
using UnityEngine;
using NUnit.Framework;
using TopDownGame.Enemies;
using TopDownGame.Enemies.Progression;
using TopDownGame.Enemies.Spawning;
using TopDownGame.Maps;
using UnityEditor.SceneManagement;

namespace TopDownGame.Tests.EditMode
{
    /// <summary>Protects the authored seeds that a hunting population needs before its pool can replenish the map.</summary>
    public sealed class HuntingPopulationAuthoringTests
    {
        [Test]
        public void Map03ContainsArcherMageSeedsAndPreservesItsSkillRewardAndBossExit()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Map03.unity");
            try
            {
                var roots = scene.GetRootGameObjects();
                var enemies = roots.SelectMany(r => r.GetComponentsInChildren<EnemyBase>(true)).ToArray();
                Assert.That(enemies.Length, Is.EqualTo(12), "An empty skill hunting zone cannot initialize or replenish a population.");
                Assert.That(enemies.Count(e => e.Data.Archetype == EnemyArchetype.Archer), Is.EqualTo(8));
                Assert.That(enemies.Count(e => e.Data.Archetype == EnemyArchetype.Mage), Is.EqualTo(4));
                var manager = roots.SelectMany(r => r.GetComponentsInChildren<MapManager>(true)).Single();
                Assert.That(manager.GetComponent<EnemySpawnManager>(), Is.Not.Null);
                Assert.That(manager.Data.TargetActiveEnemies, Is.EqualTo(18));
                Assert.That(new SerializedObject(manager.GetComponent<EnemySpawnManager>()).FindProperty("expandSpawnAnchors").boolValue, Is.False);
                Assert.That(manager.Data.OpensSkillSelection, Is.True);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<RewardChest>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<MapDoor>(true)).Single().DestinationScene, Is.EqualTo("BossMap01"));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        [Test]
        public void GeneratedEnemyNamesAndEveryVariantUseVietnameseWithoutChangingIds()
        {
            var root = new GameObject("LocalizedEnemyProfileProbe");
            try
            {
                var profile = root.AddComponent<EnemyRuntimeProfile>();
                string[] files = { "Slime", "Goblin", "Archer", "Mage", "Tank", "Forest Guardian" };
                string[] names = { "Slime Độc", "Yêu Tinh Rừng", "Cung Thủ", "Pháp Sư", "Hộ Vệ", "Vệ Thần Rừng" };
                string[] variants = { "", "Nhanh Nhẹn", "Bọc Giáp", "Hút Máu", "Bộc Phá", "Tinh Anh" };
                for (int i = 0; i < files.Length; i++)
                {
                    var data = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Enemies/" + files[i] + ".asset");
                    Assert.That(data.DisplayName, Is.EqualTo(names[i]));
                    Assert.That(data.EnemyId, Is.EqualTo(files[i].ToLowerInvariant()));
                    foreach (EnemyVariant variant in System.Enum.GetValues(typeof(EnemyVariant)))
                    {
                        profile.Configure(data, 18, variant);
                        Assert.That(profile.VariantDisplayName, Is.EqualTo(variants[(int)variant]));
                        Assert.That(profile.DisplayName, Is.EqualTo(names[i] + (variant == EnemyVariant.Normal ? "" : " " + variants[(int)variant])));
                    }
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ValidatorRejectsUnseededDisabledOrIncompleteHuntingPopulations()
        {
            foreach (string state in new[] { "noEnemies", "disabledEnemies", "missingData", "disabledHealth", "disabledSpawner", "missingSpawner" })
            {
                var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Map01.unity");
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var manager = roots.SelectMany(r => r.GetComponentsInChildren<MapManager>(true)).Single();
                    var enemies = roots.SelectMany(r => r.GetComponentsInChildren<EnemyBase>(true)).ToArray();
                    Assert.That(enemies, Is.Not.Empty);
                    foreach (var enemy in enemies)
                    {
                        if (state == "noEnemies") Object.DestroyImmediate(enemy.gameObject);
                        else if (state == "disabledEnemies") enemy.enabled = false;
                        else if (state == "disabledHealth") enemy.GetComponent<EnemyHealth>().enabled = false;
                        else if (state == "missingData")
                        {
                            var serialized = new SerializedObject(enemy);
                            serialized.FindProperty("data").objectReferenceValue = null;
                            serialized.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                    if (state == "disabledSpawner") manager.GetComponent<EnemySpawnManager>().enabled = false;
                    if (state == "missingSpawner") Object.DestroyImmediate(manager.GetComponent<EnemySpawnManager>());
                    var result = new ProjectValidationResult();
                    typeof(TopDownProjectValidator).GetMethod("ValidateSceneManagers", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { scene, "Map01", result });
                    Assert.That(result.IsValid, Is.False, state);
                    Assert.That(result.Errors.Any(e => e.Message.Contains("hunting population")), Is.True, state);
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }
    }
}
