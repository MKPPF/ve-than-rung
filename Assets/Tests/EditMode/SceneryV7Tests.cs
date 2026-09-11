using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    public sealed class SceneryV7Tests
    {
        [Test]
        public void Map01UsesDetailedTiledGroundWithoutReplacingNavigationOrEnemies()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Map01.unity",OpenSceneMode.Additive);
            try
            {
                var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).ToArray();
                Assert.That(all.Any(t=>t.name=="EmeraldSanctuaryBackdrop"),Is.False);
                var art=all.First(t=>t.name.StartsWith("WalkableFloor_")).GetComponent<SpriteRenderer>();
                Assert.That(AssetDatabase.GetAssetPath(art.sprite),Does.EndWith("Map01_MossSoilV8.png"));
                Assert.That(art.sharedMaterial.shader.name,Is.EqualTo("TopDownGame/WorldMossGround"));
                Assert.That(art.sharedMaterial.shader.isSupported,Is.True);
                Assert.That(art.sprite.texture.width/6f,Is.GreaterThan(150),"Enough source pixels per world unit at the gameplay camera.");
                Assert.That(all.Any(t=>t.name=="OuterGroveV8"),Is.True);
                Assert.That(all.Any(t=>t.name=="MazeWalls"),Is.True);
                Assert.That(all.Count(t=>t.GetComponent<TopDownGame.Enemies.EnemyHealth>()!=null),Is.GreaterThanOrEqualTo(10));
            }
            finally {EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
