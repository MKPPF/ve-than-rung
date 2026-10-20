using UnityEngine;

namespace TopDownGame.Player
{
    /// <summary>
    /// Keeps the player's automation choices through scene changes for the current
    /// play session, without changing character progress or authored prefab defaults.
    /// </summary>
    public static class PlayerAutomationState
    {
        public static bool? AutoAttackOverride { get; private set; }
        public static bool AutoPickupEnabled { get; private set; } = true;

        public static void SetAutoAttackEnabled(bool enabled) => AutoAttackOverride = enabled;
        public static void SetAutoPickupEnabled(bool enabled) => AutoPickupEnabled = enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            AutoAttackOverride = null;
            AutoPickupEnabled = true;
        }

        public static bool CanCollect(GameObject collector)
        {
            if (collector == null || !collector.CompareTag("Player") ||
                Core.GameplayState.IsInputBlocked || Time.timeScale <= 0f)
                return false;

            PlayerHealth health = collector.GetComponent<PlayerHealth>();
            return health == null || health.IsAlive;
        }
    }
}
