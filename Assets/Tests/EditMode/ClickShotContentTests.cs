using NUnit.Framework;
using TopDownGame.Player;
using UnityEditor;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    public sealed class ClickShotContentTests
    {
        [Test]
        public void GeneratedPlayerRestoresClickMovementAndDoubleInteraction()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<PlayerCombat>().ClickToShoot, Is.False);
            Assert.That(player.GetComponent<PlayerCombat>().AutoAttackEnabled, Is.True);
            Assert.That(new SerializedObject(player.GetComponent<PlayerMovement>())
                .FindProperty("clickToMove").boolValue, Is.True);
            Assert.That(new SerializedObject(player.GetComponent<PlayerInteraction>())
                .FindProperty("worldDoubleClickEnabled").boolValue, Is.True);
        }
    }
}
