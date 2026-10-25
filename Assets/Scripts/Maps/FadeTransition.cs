using System.Collections;
using TopDownGame.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.Maps
{
    /// <summary>Owns the brief travel pause and releases it if the fade is interrupted or its scene is unloaded.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FadeTransition : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float duration = 0.25f;
        private CanvasGroup canvasGroup;
        private bool transitioning;
        public bool IsTransitioning => transitioning;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        public void FadeAndLoad(string sceneName)
        {
            if (isActiveAndEnabled && !transitioning)
            {
                transitioning = true;
                GameplayState.EnterModal(this);
                StartCoroutine(FadeRoutine(sceneName));
            }
        }

        private IEnumerator FadeRoutine(string sceneName)
        {
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

        private void OnDisable()
        {
            if (!transitioning) return;
            StopAllCoroutines();
            transitioning = false;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            GameplayState.ExitModal(this);
            SceneTransition.ResetLoading();
        }
    }
}
