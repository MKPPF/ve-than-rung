using UnityEngine;

namespace TopDownGame.Core
{
    /// <summary>
    /// Shares optional external input with the existing physics and skill systems;
    /// stale packets cannot keep a player moving after the camera disconnects.
    /// </summary>
    public static class GestureInputState
    {
        private static float expiresAt;
        private static Vector2 movement;
        private static Vector2 aim = Vector2.right;

        public static bool IsActive => Time.realtimeSinceStartup < expiresAt;
        public static Vector2 Movement => IsActive ? movement : Vector2.zero;
        public static Vector2 Aim => IsActive ? aim : Vector2.right;

        public static void Set(Vector2 direction, Vector2 aimDirection, float lifetime)
        {
            movement = Vector2.ClampMagnitude(direction, 1f);
            aim = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector2.right;
            expiresAt = Time.realtimeSinceStartup + Mathf.Clamp(lifetime, 0.05f, 0.5f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            expiresAt = 0f;
            movement = Vector2.zero;
            aim = Vector2.right;
        }
    }
}
