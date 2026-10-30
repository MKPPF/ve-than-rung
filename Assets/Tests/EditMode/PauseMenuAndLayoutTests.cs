using NUnit.Framework;
using TMPro;
using TopDownGame.Save;
using TopDownGame.UI;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    public sealed class PauseMenuAndLayoutTests
    {
        [Test]
        public void EquipmentPanelLayoutConfigureWindowHandlesNullBalanceAndNullStatusSafely()
        {
            GameObject canvasObj = new GameObject("TestCanvas", typeof(Canvas), typeof(RectTransform));
            GameObject panel = new GameObject("TestPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(canvasObj.transform, false);

            Transform surface = EquipmentPanelLayout.CreateSurface(panel);
            Transform window = FantasyEquipmentUI.Window(surface, "TEST TITLE", "TEST EYEBROW", null);

            EquipmentPanelLayout layout = panel.GetComponent<EquipmentPanelLayout>();
            Assert.That(layout, Is.Not.Null);

            // Calling ConfigureWindow with null balance and null status must not throw NullReferenceException
            Assert.DoesNotThrow(() =>
            {
                layout.ConfigureWindow(window, true, null, null);
            });

            // Calling ConfigureWindow with null balance and non-null status must also succeed
            GameObject statusObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusObj.transform.SetParent(window, false);
            TMP_Text statusText = statusObj.GetComponent<TMP_Text>();

            Assert.DoesNotThrow(() =>
            {
                layout.ConfigureWindow(window, true, null, statusText);
            });

            Object.DestroyImmediate(canvasObj);
        }

        [Test]
        public void GameProgressManagerAllowsSettingCurrentMap()
        {
            GameObject obj = new GameObject("TestProgress", typeof(GameProgressManager));
            GameProgressManager progress = obj.GetComponent<GameProgressManager>();

            progress.SetCurrentMap("Lobby");
            Assert.That(progress.CurrentMap, Is.EqualTo("Lobby"));

            progress.SetCurrentMap("Map01");
            Assert.That(progress.CurrentMap, Is.EqualTo("Map01"));

            Object.DestroyImmediate(obj);
        }
    }
}
