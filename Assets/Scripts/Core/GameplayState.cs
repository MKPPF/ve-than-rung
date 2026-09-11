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

        public static bool IsInputBlocked => InputOwners.Count > 0;
        public static bool IsPaused => PauseOwners.Count > 0;
        public static bool ModalClosedThisFrame => lastModalCloseFrame == Time.frameCount;
        private static int lastModalCloseFrame = -1;

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
            Time.timeScale = 1f;
        }

        private static void ApplyTimeScale()
        {
            Time.timeScale = IsPaused ? 0f : 1f;
        }
    }
}
