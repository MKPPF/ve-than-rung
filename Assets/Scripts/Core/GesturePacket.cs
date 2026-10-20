using System;
using UnityEngine;

namespace TopDownGame.Core
{
    /// <summary>Lets the camera distinguish a running sender from a game accepting its commands.</summary>
    [Serializable]
    public sealed class GestureConnectionStatus
    {
        public int version = 1;
        public string kind = "unity_status";
        public string session;
        public long sequence;
        public bool enabled;
        public bool focused;
        public bool playerReady;
        public bool blocked;
        public string reason;
    }

    /// <summary>
    /// A versioned localhost protocol keeps model inference outside Unity while
    /// rejecting invalid values before they reach physics or spend player mana.
    /// </summary>
    [Serializable]
    public sealed class GesturePacket
    {
        public int version;
        public string session;
        public long sequence;
        public long sentUnixMs;
        public float moveX;
        public float moveY;
        public float aimX;
        public float aimY;
        public long castId;
        public int skillSlot = -1;

        public bool IsValid(long nowUnixMs)
        {
            return version == 1 && !string.IsNullOrEmpty(session) &&
                session.Length <= 64 && sequence >= 0 && castId >= 0 &&
                skillSlot >= -1 && skillSlot < 4 &&
                sentUnixMs >= nowUnixMs - 500 && sentUnixMs <= nowUnixMs + 100 &&
                IsFinite(moveX) && IsFinite(moveY) &&
                IsFinite(aimX) && IsFinite(aimY) &&
                Mathf.Abs(moveX) <= 1f && Mathf.Abs(moveY) <= 1f &&
                Mathf.Abs(aimX) <= 1f && Mathf.Abs(aimY) <= 1f &&
                new Vector2(aimX, aimY).sqrMagnitude > 0.001f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
