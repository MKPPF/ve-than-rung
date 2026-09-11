using System.Linq;
using NUnit.Framework;
using TopDownGame.Enemies;
using TopDownGame.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    public sealed class Map02ReworkTests
    {
        [Test]
        public void Map02HasOwnGroundAndSafeSeparatedEnemyAnchors()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Map02.unity", OpenSceneMode.Additive);
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>()).ToArray();
                var enemies = all.Select(t => t.GetComponent<EnemyBase>()).Where(e => e != null).ToArray();
                Assert.That(enemies.OfType<ForestGoblinEnemy>().Count(), Is.EqualTo(8));
                Assert.That(enemies.OfType<ForestArcherEnemy>().Count(), Is.EqualTo(4));
                var spawn = all.Single(t => t.CompareTag("Player")).position;
                var blockers = all.Select(t => t.GetComponent<BoxCollider2D>())
                    .Where(c => c != null && !c.isTrigger && c.GetComponent<EnemyBase>() == null).ToArray();
                Physics2D.SyncTransforms();
                foreach (var enemy in enemies)
                {
                    Assert.That(Vector2.Distance(enemy.transform.position, spawn), Is.GreaterThan(7f));
                    foreach (var other in enemies.Where(e => e != enemy))
                        Assert.That(Vector2.Distance(enemy.transform.position, other.transform.position), Is.GreaterThan(1.6f));
                    foreach (var blocker in blockers)
                        Assert.That(Vector2.Distance(blocker.ClosestPoint(enemy.transform.position), enemy.transform.position),
                            Is.GreaterThan(0.65f), enemy.name + " overlaps " + blocker.name);
                }
                var floor = all.Single(t => t.name == "Floor").GetComponent<SpriteRenderer>();
                Assert.That(AssetDatabase.GetAssetPath(floor.sprite), Does.EndWith("Map02_AncientWoodland_RestoredV1.png"));
                Assert.That(floor.bounds.size.x, Is.EqualTo(36).Within(0.01f));
                Assert.That(floor.bounds.size.y, Is.EqualTo(24).Within(0.01f));
                Assert.That(all.Any(t => t.name == "PaintedForestCollision"), Is.True);
                Assert.That(all.Any(t => t.name.StartsWith("MazeWall_")), Is.False);
                Assert.That(floor.GetComponent<ForestNavigation>(), Is.Not.Null);
                var school=all.Select(t=>t.GetComponent<ForestFishSchool>()).Single(f=>f!=null);
                Assert.That(school.HabitatCount,Is.EqualTo(8));
                var fishConfig=new SerializedObject(school);
                Assert.That(fishConfig.FindProperty("fishPerPondHabitat").intValue,Is.EqualTo(4));
                Assert.That(fishConfig.FindProperty("fishPerRiverHabitat").intValue,Is.EqualTo(2));
                Assert.That(floor.sharedMaterial.shader.name, Is.EqualTo("TopDownGame/PaintedForestColor"));
                Assert.That(floor.sharedMaterial.shader.isSupported, Is.True);
                Assert.That(floor.sharedMaterial.GetFloat("_Strength"), Is.EqualTo(1f));
                Assert.That(floor.sharedMaterial.GetFloat("_Detail"), Is.Zero);
                Assert.That(floor.sharedMaterial.GetFloat("_Clarity"), Is.EqualTo(0.65f));
                foreach (var renderer in all.Select(t => t.GetComponent<SpriteRenderer>()).Where(r => r != null && r != floor))
                    Assert.That(renderer.sharedMaterial, Is.Not.SameAs(floor.sharedMaterial), "Grade must affect only the painting.");
                var navigation = floor.GetComponent<ForestNavigation>();
                navigation.Rebuild();
                foreach (var enemy in enemies)
                {
                    Assert.That(navigation.CanStandAt(enemy.transform.position), Is.True);
                    Assert.That(navigation.TryWaypoint(spawn, enemy.transform.position, out _),
                        Is.True, "Enemy has no clearance-safe route from entrance: " + enemy.transform.position);
                }
                Assert.That(navigation.CanStandAt(new Vector2(13.5f, 1f)), Is.False, "Pond water must be solid to navigation.");
                Assert.That(navigation.CanTravelDirectly(new Vector2(-11, 2), new Vector2(4, 2)),
                    Is.False, "The grove must require a detour.");
                Assert.That(AssetDatabase.LoadAssetAtPath<MapData>("Assets/ScriptableObjects/Maps/Map02.asset").DisplayName,
                    Is.EqualTo("Tàn Tích Suối Ngọc"));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [TestCase(1f, 0f, true)]
        [TestCase(-1f, 0f, false)]
        [TestCase(0f, 1f, false)]
        [TestCase(2f, 0f, false)]
        public void SlashOnlyHitsTheTelegraphedSector(float x, float y, bool expected)
        {
            Vector2 origin = new Vector2(500, 500);
            Assert.That(ForestCombatGeometry.IsInsideStrike(origin, origin + new Vector2(x, y),
                Vector2.right, 1.46f, 65f), Is.EqualTo(expected));
        }

        [Test]
        public void SolidSceneryBlocksForestAttacksButTriggersDoNot()
        {
            var wall = new GameObject("ForestSightTest");
            try
            {
                wall.transform.position = new Vector3(501, 500);
                var collider = wall.AddComponent<BoxCollider2D>();
                Physics2D.SyncTransforms();
                Assert.That(ForestCombatGeometry.HasClearPath(new Vector2(500, 500), new Vector2(503, 500)), Is.False);
                collider.isTrigger = true;
                Physics2D.SyncTransforms();
                Assert.That(ForestCombatGeometry.HasClearPath(new Vector2(500, 500), new Vector2(503, 500)), Is.True);
            }
            finally { Object.DestroyImmediate(wall); }
        }
    }
}
