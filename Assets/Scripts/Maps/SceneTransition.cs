using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.Maps
{
    public static class SceneTransition
    {
        private static bool loading;

        public static void LoadScene(string sceneName, PlayerInteraction player)
        {
            if (loading || string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;
            PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;
            Skills.SkillInventory inventory = player != null
                ? player.GetComponent<Skills.SkillInventory>()
                : null;

            GameProgressManager.Instance?.CapturePlayer(stats, health, inventory);
            loading = true;
            FadeTransition transition = Object.FindAnyObjectByType<FadeTransition>();
            if (transition != null)
            {
                transition.FadeAndLoad(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public static void ResetLoading()
        {
            loading = false;
        }
    }
}
