using TMPro;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Keeps death modal input isolated and releases its health subscription on scene exit.</summary>
    public sealed class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text message;
        private PlayerHealth playerHealth;

        private void Start()
        {
            if (panel != null)
            {
                panel.SetActive(false);
                Button[] buttons = panel.GetComponentsInChildren<Button>(true);
                if (buttons.Length > 0) buttons[0].onClick.AddListener(Restart);
                if (buttons.Length > 1) buttons[1].onClick.AddListener(ReturnToMenu);
                if (buttons.Length > 0) buttons[0].GetComponentInChildren<TMP_Text>().text = "THỬ LẠI";
                if (buttons.Length > 1) buttons[1].GetComponentInChildren<TMP_Text>().text = "VỀ MÀN HÌNH CHÍNH";
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.Died += Show;
                }
            }
        }

        private void Show()
        {
            GameplayState.EnterModal(this);
            if (message != null)
            {
                message.text = "BẠN ĐÃ GỤC NGÃ";
            }

            panel?.SetActive(true);
        }

        public void Restart()
        {
            GameProgressManager.Instance?.PrepareRetry();
            GameplayState.ExitModal(this);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMenu()
        {
            GameplayState.ExitModal(this);
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.Died -= Show;
            GameplayState.ExitModal(this);
        }
    }
}
