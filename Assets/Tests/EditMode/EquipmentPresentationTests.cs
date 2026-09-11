using NUnit.Framework;
using TopDownGame.Equipment;
using TopDownGame.UI;
using UnityEditor;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    public sealed class EquipmentPresentationTests
    {
        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        [TestCase(1920, 1200)]
        [TestCase(1024, 768)]
        [TestCase(2560, 1080)]
        public void DesktopFitsWithoutCropping(int width, int height)
        {
            float scale = EquipmentPanelLayout.FitScale(new Vector2(width, height));
            Assert.That(1920f * scale, Is.LessThanOrEqualTo(width + 0.01f));
            Assert.That(1080f * scale, Is.LessThanOrEqualTo(height + 0.01f));
            Assert.That(scale, Is.GreaterThan(0f));
        }

        [TestCase(999, 0.2f, 800)]
        [TestCase(100, 2f, 0)]
        [TestCase(100, -1f, 100)]
        [TestCase(-100, 0.2f, 0)]
        public void DiscountIsRoundedUpAndClamped(int price, float discount, int expected)
        {
            Assert.That(WeaponShopUI.DiscountedPrice(price, discount), Is.EqualTo(expected));
        }

        [Test]
        public void DetailsAndComparisonIncludeEnhancementMultiplier()
        {
            var item = ScriptableObject.CreateInstance<EquipmentData>();
            var current = ScriptableObject.CreateInstance<EquipmentData>();
            try
            {
                var data = new SerializedObject(item);
                data.FindProperty("bonusDamage").floatValue = 10f;
                data.ApplyModifiedPropertiesWithoutUndo();
                data = new SerializedObject(current);
                data.FindProperty("bonusDamage").floatValue = 12f;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(item.BuildDetailedStatSummary(2f), Does.Contain("+20"));
                Assert.That(item.BuildDetailedComparison(current, 2f, 1f), Does.Contain("+8"));
                Assert.That(item.BuildDetailedComparison(current, 1f, 2f), Does.Contain("-14"));
            }
            finally
            {
                Object.DestroyImmediate(item);
                Object.DestroyImmediate(current);
            }
        }
    }
}
