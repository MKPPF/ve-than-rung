using NUnit.Framework;
using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Tests.EditMode
{
    public sealed class GesturePacketTests
    {
        private static GesturePacket ValidPacket()
        {
            return new GesturePacket
            {
                version = 1, session = "test", sequence = 1,
                sentUnixMs = 10000, aimX = 1, skillSlot = -1
            };
        }

        [Test]
        public void AcceptsCurrentProtocolButRejectsOldAndFuturePackets()
        {
            GesturePacket packet = ValidPacket();
            Assert.That(packet.IsValid(10000), Is.True);
            Assert.That(packet.IsValid(10501), Is.False);
            Assert.That(packet.IsValid(9899), Is.False);
            packet.version = 2;
            Assert.That(packet.IsValid(10000), Is.False);
        }

        [Test]
        public void RejectsNonFiniteAndOutOfRangeMovement()
        {
            GesturePacket packet = ValidPacket();
            packet.moveX = float.NaN;
            Assert.That(packet.IsValid(10000), Is.False);
            packet.moveX = float.PositiveInfinity;
            Assert.That(packet.IsValid(10000), Is.False);
            packet.moveX = 2;
            Assert.That(packet.IsValid(10000), Is.False);
            packet.moveX = 0;
            packet.skillSlot = 4;
            Assert.That(packet.IsValid(10000), Is.False);
        }

        [Test]
        public void ClearingInputReleasesMovementAndNormalizesAim()
        {
            try
            {
                GestureInputState.Set(Vector2.up, new Vector2(1, 1), 0.3f);
                Assert.That(GestureInputState.IsActive, Is.True);
                Assert.That(GestureInputState.Movement, Is.EqualTo(Vector2.up));
                Assert.That(GestureInputState.Aim.magnitude, Is.EqualTo(1).Within(0.001f));
                GestureInputState.Clear();
                Assert.That(GestureInputState.IsActive, Is.False);
                Assert.That(GestureInputState.Movement, Is.EqualTo(Vector2.zero));
            }
            finally { GestureInputState.Clear(); }
        }
    }
}
