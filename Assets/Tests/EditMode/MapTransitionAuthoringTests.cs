using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.EditorTools;
using TopDownGame.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    /// <summary>Checks builder-generated exit metadata and rejects scene wiring that could expose an unusable destination.</summary>
    public sealed class MapTransitionAuthoringTests
    {
        [Test]
        public void EveryGeneratedExitMatchesTheNextMapAndItsLocalizedWorldSign()
        {
            foreach (string id in new[] { "Map01", "Map02", "Map03", "BossMap01", "CaveMap01", "RiftMap01" })
            {
                var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + id + ".unity");
                try
                {
                    MapData map = AssetDatabase.LoadAssetAtPath<MapData>("Assets/ScriptableObjects/Maps/" + id + ".asset");
                    MapDoor door = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MapDoor>(true)).Single();
                    Assert.That(door.DestinationScene, Is.EqualTo(map.NextScene));
                    Assert.That(door.HasValidDestination, Is.True, id);
                    Assert.That(door.InteractionPrompt, Does.Not.Contain(map.NextScene));
                    var label = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TextMesh>(true))
                        .Single(t => t.name == "Guidance_ExitLabel");
                    Assert.That(label.text, Does.Contain(door.DestinationName.ToUpperInvariant()));
                    if (map.NextScene == "Lobby") Assert.That(door.DestinationMap, Is.Null);
                    else Assert.That(AssetDatabase.GetAssetPath(door.DestinationMap),
                        Is.EqualTo("Assets/ScriptableObjects/Maps/" + map.NextScene + ".asset"));
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }

        [Test]
        public void ValidatorRejectsMissingMismatchedAndLobbyCombatMetadata()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Map01.unity");
            try
            {
                var door = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MapDoor>(true)).Single();
                var serialized = new SerializedObject(door);
                var scenes = new Dictionary<string,string> { { "Map02", "Assets/Scenes/Map02.unity" }, { "Lobby", "Assets/Scenes/Lobby.unity" } };
                var enabled = new HashSet<string>(scenes.Keys);
                MethodInfo validate = typeof(TopDownProjectValidator).GetMethod("ValidateSceneTargets", BindingFlags.Static | BindingFlags.NonPublic);
                foreach (string state in new[] { "missing", "mismatched", "lobby" })
                {
                    serialized.FindProperty("destinationMap").objectReferenceValue = state == "missing" ? null
                        : AssetDatabase.LoadAssetAtPath<MapData>("Assets/ScriptableObjects/Maps/Map01.asset");
                    serialized.FindProperty("nextScene").stringValue = state == "lobby" ? "Lobby" : "Map02";
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    var result = new ProjectValidationResult();
                    validate.Invoke(null,new object[] { scene,"Map01",scenes,enabled,result });
                    Assert.That(result.IsValid, Is.False, state);
                    Assert.That(result.Errors.Any(e => e.Message.Contains("metadata")), Is.True, state);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
