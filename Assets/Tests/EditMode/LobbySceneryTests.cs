using System.Linq;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Equipment;
using TopDownGame.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.Tests.EditMode
{
    public sealed class LobbySceneryTests
    {
        [TestCase(1.333333f)]
        [TestCase(1.6f)]
        [TestCase(1.777778f)]
        [TestCase(2.388889f)]
        public void CameraBoundsKeepEveryViewportCornerOnPaintedMap(float aspect)
        {
            Rect bounds = new Rect(-13, -7.8f, 26, 15.6f);
            foreach (float zoom in new[] { 1f, 1.18f })
            {
            float height = Mathf.Min(5.6f * zoom, bounds.height * 0.5f, bounds.width / (2 * aspect));
            foreach (Vector2 requested in new[] { new Vector2(-100,-100), new Vector2(100,100), Vector2.zero })
            {
                Vector2 result = CameraFollow2D.ClampViewportCenter(requested, bounds, height, aspect);
                Assert.That(result.x - height * aspect, Is.GreaterThanOrEqualTo(bounds.xMin - 0.001f));
                Assert.That(result.x + height * aspect, Is.LessThanOrEqualTo(bounds.xMax + 0.001f));
                Assert.That(result.y - height, Is.GreaterThanOrEqualTo(bounds.yMin - 0.001f));
                Assert.That(result.y + height, Is.LessThanOrEqualTo(bounds.yMax + 0.001f));
            }
            }
        }

        [Test]
        public void LobbyHasAuthoredArtworkBoundariesAndAllThreeStations()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var transforms = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                SpriteRenderer floor = transforms.Single(t => t.name == "Floor").GetComponent<SpriteRenderer>();
                Assert.That(AssetDatabase.GetAssetPath(floor.sprite), Is.EqualTo("Assets/Art/Generated/Lobby/WoodlandWaystation_RestoredV1.png"));
                Assert.That(floor.bounds.size.x, Is.EqualTo(26).Within(0.01));
                Assert.That(floor.bounds.size.y, Is.EqualTo(15.6).Within(0.01));
                Assert.That(transforms.Count(t => t.name.StartsWith("LobbyBoundary_")), Is.EqualTo(18));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<LobbyAtmosphere>(true)).Count(), Is.EqualTo(1));
                LobbyWaterFlow water = roots.SelectMany(r => r.GetComponentsInChildren<LobbyWaterFlow>(true)).Single();
                var waterData = new SerializedObject(water);
                Assert.That(waterData.FindProperty("sourceArt").objectReferenceValue, Is.SameAs(floor.sprite));
                Shader waterShader = waterData.FindProperty("flowShader").objectReferenceValue as Shader;
                Assert.That(waterShader, Is.Not.Null);
                Assert.That(waterShader.isSupported, Is.True);
                Assert.That(waterData.FindProperty("streamCenters").arraySize, Is.EqualTo(6));
                LobbyFireEffects fire = roots.SelectMany(r => r.GetComponentsInChildren<LobbyFireEffects>(true)).Single();
                Assert.That(new SerializedObject(fire).FindProperty("firePositions").arraySize, Is.EqualTo(1));
                LobbyWildlife wildlife=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyWildlife>(true)).Single();
                Assert.That(new SerializedObject(wildlife).FindProperty("flowerPatches").arraySize,Is.EqualTo(3));
                var garden=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyGardenWildlife>(true)).Single();
                var gardenData=new SerializedObject(garden);
                Assert.That(gardenData.FindProperty("flowerBeds").arraySize,Is.EqualTo(3));
                Assert.That(gardenData.FindProperty("birdPerches").arraySize,Is.EqualTo(3));
                Assert.That(garden.transform.lossyScale,Is.EqualTo(Vector3.one));
                var visitors=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyVisitor>(true)).ToArray();
                Assert.That(visitors.Length,Is.EqualTo(3));
                foreach(var visitor in visitors)
                {
                    Assert.That(visitor.RoutePointCount,Is.EqualTo(4));
                    Assert.That(visitor.GetComponent<Rigidbody2D>().bodyType,Is.EqualTo(RigidbodyType2D.Kinematic));
                    Assert.That(visitor.GetComponentsInChildren<Collider2D>(),Is.Empty);
                    var renderer=visitor.GetComponentInChildren<SpriteRenderer>();
                    Assert.That(renderer.sprite,Is.Not.Null);
                    Assert.That(renderer.sharedMaterial.shader.isSupported,Is.True);
                }
                Assert.That(transforms.Count(t=>t.name=="TravelSupplies"),Is.EqualTo(2));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<LobbyStationSign>(true)).Count(), Is.EqualTo(3));
                foreach (string name in new[] { "WeaponMerchant", "StorageChest", "BattlePortal" })
                {
                    Transform station = transforms.Single(t => t.name == name);
                    Assert.That(station.GetComponent<Collider2D>().isTrigger, Is.True, name);
                    if(name=="BattlePortal")
                    {
                        Assert.That(station.GetComponent<SpriteRenderer>(),Is.Null,"No upright arch should cover the painted dais.");
                        var circle = station.GetComponent<TeleportCircleVisual>();
                        Assert.That(circle, Is.Not.Null);
                        Assert.That(station.position.x, Is.EqualTo(8.73f).Within(0.001f));
                        Assert.That(station.position.y, Is.EqualTo(-0.20f).Within(0.001f));
                        Assert.That(circle.EngravedRadii, Is.EqualTo(new Vector2(1.22f, 0.87f)));
                        Assert.That(circle.OuterRadii, Is.EqualTo(new Vector2(1.95f, 1.44f)));
                        Assert.That(station.GetComponent<LobbyPortal>().DepartureDuration, Is.EqualTo(3f));
                        var departure = station.GetComponent<TeleportDepartureVisual>();
                        Assert.That(departure, Is.Not.Null);
                        var beam = new SerializedObject(departure).FindProperty("beamShader").objectReferenceValue as Shader;
                        Assert.That(beam, Is.Not.Null);
                        Assert.That(beam.isSupported, Is.True);
                    }
                    else Assert.That(station.GetComponent<SpriteRenderer>().sprite, Is.Not.Null, name);
                }
                CameraFollow2D camera = roots.SelectMany(r => r.GetComponentsInChildren<CameraFollow2D>()).Single();
                var serialized = new SerializedObject(camera);
                Assert.That(serialized.FindProperty("constrainToBounds").boolValue, Is.True);
                Assert.That(serialized.FindProperty("worldBounds").rectValue, Is.EqualTo(new Rect(-13,-7.8f,26,15.6f)));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
