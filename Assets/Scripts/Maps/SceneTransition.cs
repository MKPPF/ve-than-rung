using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.Maps
{
    /// <summary>Rejects invalid or duplicate travel before capturing progress; arrival unlocks the destination in MapManager.</summary>
    public static class SceneTransition
    {
        private static bool loading;
        public static bool IsLoading => loading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            loading = false;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) ResetLoading();
        }

        public static void LoadScene(string sceneName, PlayerInteraction player)
        {
            TryLoadScene(sceneName, player);
        }

        public static bool TryLoadScene(string sceneName, PlayerInteraction player, Object inputOwner = null)
        {
            if (loading || string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName) ||
                player == null || !player.isActiveAndEnabled || GameplayState.IsBlockedByOther(inputOwner) ||
                Time.timeScale <= 0f) return false;

            PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;
            PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;
            Skills.SkillInventory inventory = player != null
                ? player.GetComponent<Skills.SkillInventory>()
                : null;
            if (stats == null || health == null || !health.IsAlive) return false;

            FadeTransition transition = Object.FindAnyObjectByType<FadeTransition>();
            if (transition != null && (!transition.isActiveAndEnabled || transition.IsTransitioning)) return false;

            loading = true;
            GameProgressManager.Instance?.CapturePlayer(stats, health, inventory);
            if (transition != null)
            {
                transition.FadeAndLoad(sceneName);
                if (inputOwner != null) GameplayState.ExitModal(inputOwner);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
            return true;
        }

        public static void ResetLoading()
        {
            loading = false;
        }
    }
}
