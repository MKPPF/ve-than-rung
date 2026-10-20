using System.Collections;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Skills;
using System.Reflection;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    public sealed class GestureSkillProbe : SkillBase
    {
        public int Casts { get; private set; }
        public override void Cast() { Casts++; }
    }

    public sealed class GestureInputPlayModeTests
    {
        [UnityTest]
        public IEnumerator LoopbackPacketsReceiveStatusAndRespectPauseAndTimeout()
        {
            var owner = new GameObject("GestureUdpPlayerProbe", typeof(PlayerStats), typeof(PlayerMovement));
            owner.transform.position = new Vector3(1000, 1000, 0);
            var host = new GameObject("GestureUdpReceiverProbe");
            var receiver = host.AddComponent<GestureInputReceiver>();
            bool originalBackground = Application.runInBackground;
            using var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            int testPort;
            using (var probe = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
                testPort = ((IPEndPoint)probe.Client.LocalEndPoint).Port;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(GestureInputReceiver).GetField("port", flags).SetValue(receiver, testPort);
            typeof(GestureInputReceiver).GetField("player", flags).SetValue(receiver, owner.GetComponent<PlayerMovement>());
            try
            {
                receiver.SetControlEnabled(true);
                Assert.That(receiver.ControlEnabled, Is.True);
                Assert.That(Application.runInBackground, Is.True);
                var packet = new GesturePacket { version = 1, session = "loopback-test", sequence = 1,
                    sentUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), aimX = 1, moveX = 1, skillSlot = -1 };
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
                client.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, testPort));
                float deadline = Time.realtimeSinceStartup + 1;
                while (client.Available == 0 && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(client.Available, Is.GreaterThan(0), "Camera must receive a real Unity acknowledgement.");
                var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
                var reply = JsonUtility.FromJson<GestureConnectionStatus>(Encoding.UTF8.GetString(client.Receive(ref endpoint)));
                Assert.That(reply.kind, Is.EqualTo("unity_status"));
                Assert.That(reply.session, Is.EqualTo(packet.session));
                Assert.That(reply.playerReady, Is.True);
                Assert.That(receiver.HasRecentPacket, Is.True);
                Assert.That(reply.blocked, Is.False, "Explicitly enabled control also accepts input while the camera has focus.");
                Assert.That(GestureInputState.Movement, Is.EqualTo(Vector2.right));

                GameplayState.EnterModal(owner);
                yield return new WaitForSecondsRealtime(0.17f);
                packet.sequence++;
                packet.sentUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
                client.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, testPort));
                deadline = Time.realtimeSinceStartup + 1;
                while (client.Available == 0 && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(client.Available, Is.GreaterThan(0));
                reply = JsonUtility.FromJson<GestureConnectionStatus>(Encoding.UTF8.GetString(client.Receive(ref endpoint)));
                Assert.That(reply.blocked, Is.True);
                Assert.That(reply.reason, Is.EqualTo("paused"));
                Assert.That(GestureInputState.Movement, Is.EqualTo(Vector2.zero));
                GameplayState.ExitModal(owner);
                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(GestureInputState.IsActive, Is.False);
                receiver.SetControlEnabled(false);
                Assert.That(receiver.ControlEnabled, Is.False);
                Assert.That(Application.runInBackground, Is.EqualTo(originalBackground));
            }
            finally
            {
                GameplayState.ExitModal(owner);
                receiver.SetControlEnabled(false);
                GestureInputState.Clear();
                UnityEngine.Object.Destroy(host);
                UnityEngine.Object.Destroy(owner);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator GestureUsesExistingMovementAndModalBlock()
        {
            var owner = new GameObject("GestureMovementProbe", typeof(PlayerStats), typeof(PlayerMovement));
            owner.transform.position = new Vector3(1000, 1000, 0);
            var movement = owner.GetComponent<PlayerMovement>();
            Keyboard previous = Keyboard.current;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                GestureInputState.Set(Vector2.right, Vector2.right, 0.3f);
                movement.SendMessage("Update");
                Assert.That(movement.MoveDirection, Is.EqualTo(Vector2.right));
                Assert.That(movement.WantsToMove, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                InputSystem.Update();
                movement.SendMessage("Update");
                Assert.That(movement.MoveDirection, Is.EqualTo(Vector2.up), "Keyboard retains priority.");
                GameplayState.EnterModal(owner);
                movement.SendMessage("Update");
                Assert.That(movement.MoveDirection, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                GameplayState.ExitModal(owner);
                GestureInputState.Clear();
                InputSystem.RemoveDevice(keyboard);
                if (previous != null) previous.MakeCurrent();
                Object.Destroy(owner);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator GestureCastSpendsManaOnceAndRespectsCooldownAndPause()
        {
            var owner = new GameObject("GestureSkillProbe");
            SkillData data = ScriptableObject.CreateInstance<SkillData>();
            var stats = owner.AddComponent<PlayerStats>();
            var inventory = owner.AddComponent<SkillInventory>();
            var probe = owner.AddComponent<GestureSkillProbe>();
            probe.Initialize(data, 1);
            var manager = owner.AddComponent<SkillManager>();
            typeof(SkillManager).GetField("skillDatabase", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, new[] { data });
            inventory.TryEquip("Fireball", 0);
            try
            {
                yield return null;
                float before = stats.CurrentMana;
                Assert.That(manager.TryCastFromGesture(0, Vector2.up), Is.True);
                Assert.That(stats.CurrentMana, Is.EqualTo(before - data.ManaCost).Within(0.01f));
                Assert.That(manager.TryCastFromGesture(0, Vector2.up), Is.False);
                yield return new WaitForSeconds(0.3f);
                Assert.That(probe.Casts, Is.EqualTo(1));
                Assert.That(manager.GetCooldownRemaining(0), Is.GreaterThan(0));
                GameplayState.EnterModal(owner);
                Assert.That(manager.TryCastFromGesture(0, Vector2.up), Is.False);
                Assert.That(manager.TryCastFromGesture(4, Vector2.up), Is.False);
            }
            finally
            {
                GameplayState.ExitModal(owner);
                Object.Destroy(owner);
                Object.Destroy(data);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisconnectTimeoutStopsMovementEvenWhilePaused()
        {
            float oldScale = Time.timeScale;
            try
            {
                GestureInputState.Set(Vector2.right, Vector2.up, 0.05f);
                Assert.That(GestureInputState.Movement, Is.EqualTo(Vector2.right));
                Time.timeScale = 0;
                yield return new WaitForSecondsRealtime(0.09f);
                Assert.That(GestureInputState.IsActive, Is.False);
                Assert.That(GestureInputState.Movement, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Time.timeScale = oldScale;
                GestureInputState.Clear();
            }
        }
    }
}
