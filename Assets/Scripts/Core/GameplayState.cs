using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.Core
{
    /// <summary>
    /// Coordinates modal UI and pause ownership so independent screens cannot
    /// accidentally resume gameplay while another screen still needs it paused.
    /// </summary>
    public static class GameplayState
    {
        private static readonly HashSet<ulong> InputOwners = new HashSet<ulong>();
        private static readonly HashSet<ulong> PauseOwners = new HashSet<ulong>();

        public static bool IsInputBlocked => InputOwners.Count > 0 || navigationInputFrame == Time.frameCount;
        private static int navigationInputFrame = -1;
        public static bool NavigationInputConsumedThisFrame => navigationInputFrame == Time.frameCount;

        /// <summary>Consumes a HUD navigation frame before world controls read the same keys; UI modal ownership remains independent.</summary>
        public static void ConsumeNavigationInput() => navigationInputFrame = Time.frameCount;
        public static bool IsPaused => PauseOwners.Count > 0;
        public static bool ModalClosedThisFrame => lastModalCloseFrame == Time.frameCount;
        private static int lastModalCloseFrame = -1;
        public const float MaximumHitstopDuration = 0.065f;
        public const float HitstopRecoveryDuration = 0.12f;
        private static float hitstopStartedAt, hitstopEndsAt, hitstopReadyAt;
        public static bool IsHitstopActive => hitstopEndsAt > 0f && Time.unscaledTime < hitstopEndsAt;
        public static float HitstopRemaining => Mathf.Max(0f, hitstopEndsAt - Time.unscaledTime);

        /// <summary>Coalesces a burst into one bounded freeze; sustained multi-hit attacks must allow recovery frames.</summary>
        public static bool RequestHitstop(float duration)
        {
            if (duration <= 0f || !Application.isPlaying || !CombatFeedback.HasActiveDriver ||
                !FeedbackPreferences.HitstopEnabled || IsInputBlocked || IsPaused) return false;
            float now = Time.unscaledTime;
            if (!IsHitstopActive)
            {
                if (now < hitstopReadyAt || Time.timeScale <= 0f) return false;
                hitstopStartedAt = now;
                hitstopEndsAt = now;
                hitstopReadyAt = now + MaximumHitstopDuration + HitstopRecoveryDuration;
            }
            hitstopEndsAt = Mathf.Min(hitstopStartedAt + MaximumHitstopDuration,
                Mathf.Max(hitstopEndsAt, now + duration));
            ApplyTimeScale();
            return true;
        }

        internal static void TickHitstop()
        {
            if (hitstopEndsAt <= 0f || IsHitstopActive) return;
            hitstopEndsAt = 0f;
            ApplyTimeScale();
        }

        public static void CancelHitstop()
        {
            if (hitstopEndsAt <= 0f) return;
            hitstopEndsAt = 0f;
            ApplyTimeScale();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ResetState();
        }

        public static bool IsBlockedByOther(Object owner)
        {
            if (InputOwners.Count == 0)
            {
                return false;
            }

            ulong ownerId = GetOwnerId(owner);
            return InputOwners.Count > 1 || !InputOwners.Contains(ownerId);
        }

        public static void EnterModal(Object owner, bool pauseGameplay = true)
        {
            ulong ownerId = GetOwnerId(owner);
            if (ownerId == 0)
            {
                return;
            }

            InputOwners.Add(ownerId);
            if (pauseGameplay)
            {
                PauseOwners.Add(ownerId);
                hitstopEndsAt = 0f;
            }

            ApplyTimeScale();
        }

        public static void ExitModal(Object owner)
        {
            ulong ownerId = GetOwnerId(owner);
            if (ownerId == 0)
            {
                return;
            }

            bool removedInput = InputOwners.Remove(ownerId);
            bool removedPause = PauseOwners.Remove(ownerId);
            if (!removedInput && !removedPause) return;
            if (removedInput) lastModalCloseFrame = Time.frameCount;
            ApplyTimeScale();
        }

        private static ulong GetOwnerId(Object owner)
        {
            return owner != null ? EntityId.ToULong(owner.GetEntityId()) : 0UL;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetState();
        }

        private static void ResetState()
        {
            InputOwners.Clear();
            PauseOwners.Clear();
            lastModalCloseFrame = -1;
            navigationInputFrame = -1;
            hitstopStartedAt = hitstopEndsAt = hitstopReadyAt = 0f;
            Time.timeScale = 1f;
        }

        private static void ApplyTimeScale()
        {
            Time.timeScale = IsPaused || IsHitstopActive ? 0f : 1f;
        }
    }
}
