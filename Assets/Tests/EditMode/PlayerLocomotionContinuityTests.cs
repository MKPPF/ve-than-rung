using NUnit.Framework;
using TopDownGame.Player;
using UnityEditor;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    public sealed class PlayerLocomotionContinuityTests
    {
        [TestCase(0f,0.25f)]
        [TestCase(0.3f,0.25f)]
        [TestCase(0.49f,0.25f)]
        [TestCase(0.5f,0.75f)]
        [TestCase(0.8f,0.75f)]
        [TestCase(0.99f,0.75f)]
        public void RestPoseNeedsAtMostQuarterCycle(float phase, float expected)
        {
            float result = PlayerCharacterAnimator.NearestRestPhase(phase);
            Assert.That(result, Is.EqualTo(expected).Within(0.0001));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(phase * 360, result * 360)), Is.LessThanOrEqualTo(90.01));
        }

        [Test]
        public void BuilderEnablesShortResponseAndPresentationContinuity()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            var movement = new SerializedObject(prefab.GetComponent<PlayerMovement>());
            var animation = new SerializedObject(prefab.GetComponent<PlayerCharacterAnimator>());
            Assert.That(movement.FindProperty("smoothLocomotion").boolValue, Is.True);
            Assert.That(animation.FindProperty("smoothLocomotion").boolValue, Is.True);
            Assert.That(movement.FindProperty("acceleration").floatValue, Is.EqualTo(60));
            Assert.That(movement.FindProperty("deceleration").floatValue, Is.EqualTo(90));
            Assert.That(animation.FindProperty("stopSettleDuration").floatValue, Is.InRange(0.04f,0.15f));
        }
    }
}
