using System.Linq;
using NUnit.Framework;
using TopDownGame.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownGame.Tests.EditMode
{
    /// <summary>Guards the generated prefab contract so a rebuild cannot leave dodge logic disconnected from physics or input.</summary>
    public sealed class DodgeAuthoringTests
    {
        [Test]
        public void GeneratedDodgeHasPhysicsHealthPresentationAndKeyboardGamepadBindings()
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            PlayerDodge dodge = player.GetComponent<PlayerDodge>();
            Assert.That(dodge, Is.Not.Null);
            Assert.That(dodge.HasRequiredReferences, Is.True);
            Assert.That(new SerializedObject(player.GetComponent<PlayerMovement>()).FindProperty("dodge").objectReferenceValue, Is.EqualTo(dodge));
            Assert.That(dodge.InvulnerabilityDuration, Is.GreaterThan(0).And.LessThan(dodge.Duration));
            Assert.That(dodge.CooldownDuration, Is.GreaterThanOrEqualTo(dodge.Duration));
            InputAction action = player.GetComponent<PlayerInput>().actions.FindAction("Player/Dodge",false);
            Assert.That(action, Is.Not.Null);
            Assert.That(action.bindings.Any(binding => binding.path == "<Keyboard>/space"), Is.True);
            Assert.That(action.bindings.Any(binding => binding.path == "<Gamepad>/buttonSouth"), Is.True);
        }
    }
}
