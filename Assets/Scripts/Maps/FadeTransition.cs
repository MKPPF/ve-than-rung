using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.Maps
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FadeTransition : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float duration = 0.25f;
        private CanvasGroup canvasGroup;
        private bool transitioning;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            SceneTransition.ResetLoading();
        }

        public void FadeAndLoad(string sceneName)
        {
            if (!transitioning)
            {
                StartCoroutine(FadeRoutine(sceneName));
            }
        }

        private IEnumerator FadeRoutine(string sceneName)
        {
            transitioning = true;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = duration <= 0f ? 1f : elapsed / duration;
                yield return null;
            }

            canvasGroup.alpha = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
