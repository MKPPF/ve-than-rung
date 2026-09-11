#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Captures the real prefab at a fixed presentation rate so timing changes can be compared visually.</summary>
    public sealed class PlayerWalkContinuityTests
    {
        private GameObject player, cameraObject;
        private RenderTexture target;
        private float oldCapture;
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [UnityTest]
        public IEnumerator SmoothWalkDoesNotInsertIdleAndReturnsToNeutralAtRest()
        {
            oldCapture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60;
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            player.name = "SmoothStatePlayerProbe";
            player.transform.position = new Vector3(40,40);
            player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            var movement = player.GetComponent<PlayerMovement>();
            var animator = player.GetComponent<PlayerCharacterAnimator>();
            Sprite idle = (Sprite)typeof(PlayerCharacterAnimator).GetField("idleSprite", Private).GetValue(animator);
            yield return null;
            Vector2 start = player.GetComponent<Rigidbody2D>().position;
            movement.TrySetClickDestination(start + Vector2.up * 2);
            yield return new WaitForSeconds(0.12f);
            int samples = 0;
            while (movement.IsFollowingClickPath)
            {
                yield return null;
                Assert.That(movement.VisualRenderer.sprite, Is.Not.SameAs(idle),
                    "No front-facing idle flash during continuous up movement.");
                Assert.That(samples++, Is.LessThan(180));
            }
            yield return new WaitForSeconds(0.2f);
            Assert.That(movement.VisualRenderer.sprite, Is.SameAs(idle),
                "Settling must finish on authored neutral idle, never a raised-foot walk pose.");
            Assert.That(player.GetComponent<Rigidbody2D>().position.y, Is.EqualTo(start.y + 2).Within(0.01));
            float stopPhase = animator.PresentedGaitPhase;
            yield return new WaitForSeconds(0.2f);
            Assert.That(animator.PresentedGaitPhase, Is.EqualTo(stopPhase).Within(0.0001), "Rest must not keep walking.");
            movement.TrySetClickDestination((Vector2)player.transform.position + Vector2.left);
            yield return new WaitForSeconds(0.15f);
            Assert.That(movement.VisualRenderer.flipX, Is.True);
            Assert.That(animator.IsWalking, Is.True);
            while (movement.IsFollowingClickPath) yield return null;
            yield return new WaitForSeconds(0.2f);
            Assert.That(movement.VisualRenderer.sprite, Is.SameAs(idle),
                "Stopping to the left also needs neutral feet.");
            foreach (Vector2 direction in new[] { Vector2.right, Vector2.down })
            {
                movement.TrySetClickDestination((Vector2)player.transform.position + direction * 0.5f);
                yield return new WaitForSeconds(0.45f);
                Assert.That(movement.VisualRenderer.sprite, Is.SameAs(idle),
                    "Every cardinal direction must finish on neutral idle.");
                Assert.That(animator.IsWalking, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator ShortRampBrakesWithinBoundAndNeverDriftsPastClickGoal()
        {
            oldCapture = Time.captureDeltaTime;
            player = new GameObject("ShortRampPlayerProbe");
            player.AddComponent<SpriteRenderer>();
            player.AddComponent<PlayerStats>();
            player.AddComponent<CircleCollider2D>();
            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            typeof(PlayerMovement).GetField("smoothLocomotion", Private).SetValue(movement,true);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            yield return null;
            movement.TrySetClickDestination(new Vector2(0.4f,0));
            yield return new WaitForFixedUpdate();
            Assert.That(movement.CurrentVelocity.x, Is.GreaterThan(0).And.LessThan(3));
            yield return new WaitForSeconds(0.5f);
            Assert.That(body.position.x, Is.EqualTo(0.4f).Within(0.001));
            Assert.That(movement.IsFollowingClickPath, Is.False);
            yield return new WaitForSeconds(0.2f);
            Assert.That(body.position.x, Is.EqualTo(0.4f).Within(0.001));
            // Isolate the physics response from keyboard polling.
            MethodInfo fixedTick = typeof(PlayerMovement).GetMethod("FixedUpdate", Private);
            FieldInfo intent = typeof(PlayerMovement).GetField("moveDirection", Private);
            for (int i=0; i<4; i++)
            {
                intent.SetValue(movement,Vector2.right);
                fixedTick.Invoke(movement,null);
            }
            intent.SetValue(movement,Vector2.zero);
            fixedTick.Invoke(movement,null);
            Assert.That(movement.CurrentVelocity.x, Is.GreaterThan(0).And.LessThan(3));
            fixedTick.Invoke(movement,null);
            Assert.That(movement.CurrentVelocity.magnitude, Is.LessThan(0.01));
        }

        [UnityTest]
        public IEnumerator CaptureCardinalWalkStopsAndTurns()
        {
            oldCapture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            player.name = "WalkContinuityPlayerProbe";
            player.transform.position = new Vector3(40,40,0);
            player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            var animator = player.GetComponent<PlayerCharacterAnimator>();
            cameraObject = new GameObject("WalkContinuityCameraProbe", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f,0.14f,0.17f);
            target = new RenderTexture(640,480,24);
            camera.targetTexture = target;
            yield return null;
            string variant = typeof(PlayerCharacterAnimator).GetField("smoothLocomotion", Private) != null ? "After" : "Before";
            string directory = "Captures/WalkContinuity_" + variant;
            Directory.CreateDirectory(directory);
            var trace = new StringBuilder("frame,time,bodyX,bodyY,phase,sprite\n");
            int changes = 0;
            Sprite previousSprite = null;
            for (int i = 0; i < 300; i++)
            {
                if (i == 0) movement.TrySetClickDestination((Vector2)player.transform.position + Vector2.right * 2.4f);
                if (i == 75) movement.TrySetClickDestination((Vector2)player.transform.position + Vector2.up * 1.6f);
                if (i == 130) movement.TrySetClickDestination((Vector2)player.transform.position + Vector2.left * 2.2f);
                if (i == 205) movement.TrySetClickDestination((Vector2)player.transform.position + Vector2.down * 1.6f);
                if (i == 255) movement.TrySetClickDestination((Vector2)player.transform.position + Vector2.right * 0.35f);
                if (i == 271) movement.TrySetClickDestination((Vector2)player.transform.position + Vector2.right * 0.35f);
                yield return null;
                camera.transform.position = player.transform.position + new Vector3(0,0,-10);
                camera.Render();
                RenderTexture old = RenderTexture.active;
                var image = new Texture2D(640,480,TextureFormat.RGB24,false);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,640,480),0,0);
                image.Apply();
                File.WriteAllBytes(directory + "/frame_" + i.ToString("000") + ".png", image.EncodeToPNG());
                RenderTexture.active = old;
                Object.Destroy(image);
                Sprite sprite = movement.VisualRenderer.sprite;
                if (sprite != previousSprite) changes++;
                previousSprite = sprite;
                trace.Append(i).Append(',').Append(Time.time.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(',').Append(player.transform.position.x.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(',').Append(player.transform.position.y.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(',').Append(movement.GaitPhase.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(',').Append(sprite != null ? sprite.name : "none").AppendLine();
                Assert.That(float.IsNaN(movement.GaitPhase), Is.False);
            }
            File.WriteAllText(directory + "/trace.csv", trace.ToString());
            Assert.That(changes, Is.GreaterThan(20), "The walk sequence must keep changing poses.");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.captureDeltaTime = oldCapture;
            if (cameraObject != null) Object.Destroy(cameraObject);
            if (player != null) Object.Destroy(player);
            if (target != null) { target.Release(); Object.Destroy(target); }
            yield return null;
        }
    }
}
#endif
