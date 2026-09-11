using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Combat;
using TopDownGame.Player;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TopDownGame.Tests.PlayMode
{
    public sealed class ShotTargetProbe : MonoBehaviour, IDamageable
    {
        public bool IsAlive => true;
        public int Hits;
        public void TakeDamage(DamageInfo info) { Hits++; }
    }

    public sealed class ClickShotTests
    {
        private GameObject owner, target, wall;

        [UnityTest]
        public IEnumerator LeftClickShootsButUIClickDoesNot()
        {
            // The Test Runner can keep the user's MainMenu canvas loaded.
            // Isolate raycasters so that unrelated full-screen menus do not
            // turn our intended world click into a UI click.
            var otherRaycasters=Object.FindObjectsByType<GraphicRaycaster>();
            var enabledRaycasters=new System.Collections.Generic.List<GraphicRaycaster>();
            foreach(var raycaster in otherRaycasters)
                if(raycaster.enabled){enabledRaycasters.Add(raycaster);raycaster.enabled=false;}
            var combat = CreateShooter();
            var previousMouse = Mouse.current;
            var mouse = InputSystem.AddDevice<Mouse>();
            var ui = new GameObject("ShotUI", typeof(Canvas), typeof(GraphicRaycaster));
            var events = new GameObject("ShotEvents", typeof(EventSystem));
            var cameraObject = new GameObject("ShotCamera", typeof(Camera));
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.transform.position = owner.transform.position + Vector3.back * 10f;
                typeof(PlayerCombat).GetField("mainCamera", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(combat, camera);
                ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(ui.transform, false);
                var rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                InputSystem.QueueStateEvent(mouse, new MouseState
                    { position = new Vector2(Screen.width * 0.7f, Screen.height * 0.5f) });
                yield return null;
                Canvas.ForceUpdateCanvases();
                InputSystem.QueueStateEvent(mouse, new MouseState
                    { position = new Vector2(Screen.width * 0.7f, Screen.height * 0.5f) }
                    .WithButton(MouseButton.Left));
                InputSystem.Update();mouse.MakeCurrent();combat.SendMessage("Update");
                Assert.That(Object.FindObjectsByType<PlayerShot>(FindObjectsSortMode.None), Is.Empty);
                ui.SetActive(false);
                InputSystem.QueueStateEvent(mouse, new MouseState
                    { position = new Vector2(Screen.width * 0.7f, Screen.height * 0.5f) });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState
                    { position = new Vector2(Screen.width * 0.7f, Screen.height * 0.5f) }
                    .WithButton(MouseButton.Left));
                InputSystem.Update();mouse.MakeCurrent();combat.SendMessage("Update");
                var shot = Object.FindFirstObjectByType<PlayerShot>();
                Assert.That(shot, Is.Not.Null);
                Assert.That(shot.Direction.x, Is.GreaterThan(0.99f));
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                if (previousMouse != null) previousMouse.MakeCurrent();
                Object.Destroy(ui); Object.Destroy(events); Object.Destroy(cameraObject);
                foreach(var raycaster in enabledRaycasters) if(raycaster!=null) raycaster.enabled=true;
            }
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            foreach (var shot in Object.FindObjectsByType<PlayerShot>(FindObjectsSortMode.None))
                Object.Destroy(shot.gameObject);
            Object.Destroy(owner); Object.Destroy(target); Object.Destroy(wall);
            yield return null;
        }

        private PlayerCombat CreateShooter()
        {
            owner = new GameObject("ClickShotProbe");
            owner.transform.position = new Vector3(1000, 1000, 0);
            owner.AddComponent<PlayerStats>();
            var combat = owner.AddComponent<PlayerCombat>();
            combat.SetAutoAttackEnabled(false);
            typeof(PlayerCombat).GetField("clickToShoot", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(combat, true);
            return combat;
        }

        [UnityTest]
        public IEnumerator ShotsLockDirectionRespectCooldownPauseAndRange()
        {
            var combat = CreateShooter();
            Assert.That(combat.TryShoot(Vector2.up), Is.True);
            Assert.That(combat.TryShoot(Vector2.left), Is.False);
            var shot = Object.FindFirstObjectByType<PlayerShot>();
            Assert.That(shot.Direction, Is.EqualTo(Vector2.up));
            combat.SetAimDirection(Vector2.left);
            Vector3 start = shot.transform.position;
            Time.timeScale = 0f;
            Assert.That(combat.TryShoot(Vector2.left), Is.False);
            yield return new WaitForSecondsRealtime(0.06f);
            Assert.That(shot.transform.position, Is.EqualTo(start));
            Time.timeScale = 1f;
            yield return new WaitForSeconds(0.45f);
            Assert.That(shot.transform.position.y, Is.GreaterThan(start.y));
            Assert.That(shot.Direction, Is.EqualTo(Vector2.up));
            Assert.That(combat.TryShoot(Vector2.left), Is.True);
            yield return new WaitForSeconds(1f);
            Assert.That(Object.FindObjectsByType<PlayerShot>(FindObjectsSortMode.None), Is.Empty);
        }

        [UnityTest]
        public IEnumerator SweptShotHitsOnlyOnceAndCannotPassThinWall()
        {
            CreateShooter();
            target = new GameObject("ShotTarget");
            target.transform.position = owner.transform.position + Vector3.right * 2f;
            target.AddComponent<BoxCollider2D>();
            var probe = target.AddComponent<ShotTargetProbe>();
            wall = new GameObject("ThinWall");
            wall.transform.position = owner.transform.position + Vector3.right;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.02f, 2f);
            Physics2D.SyncTransforms();
            PlayerShot.Spawn(owner, Vector2.right, 200f, 10f, 10f, 0f, false, ~0);
            yield return new WaitForSeconds(0.06f);
            Assert.That(probe.Hits, Is.Zero);
            Object.Destroy(wall);
            yield return null;
            Physics2D.SyncTransforms();
            PlayerShot.Spawn(owner, Vector2.right, 200f, 10f, 10f, 0f, false, ~0);
            yield return new WaitForSeconds(0.06f);
            Assert.That(probe.Hits, Is.EqualTo(1));
        }
    }
}
