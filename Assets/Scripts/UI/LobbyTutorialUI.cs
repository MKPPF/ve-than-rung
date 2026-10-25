using System.Collections;
using TopDownGame.Core;
using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.UI
{
    /// <summary>The generated Lobby owns automatic onboarding; gameplay and manual replay share the same modal rules.</summary>
    public sealed class LobbyTutorialUI : MonoBehaviour
    {
        [SerializeField] private MenuDialog dialog;

        private IEnumerator Start()
        {
            // Allow the scene-loaded reset and player restoration to finish first.
            yield return null;
            GameProgressManager progress = GameProgressManager.Instance;
            while (progress != null && progress.ShouldShowTutorial)
            {
                if (!GameplayState.IsInputBlocked && !GameplayState.ModalClosedThisFrame)
                {
                    TutorialGuide.Show(dialog, true);
                    yield break;
                }
                yield return null;
            }
        }
    }
}
