using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.EditorTools
{
    /// <summary>
    /// Recovers from Unity or the Test Runner leaving the editor on its temporary
    /// untitled scene, without replacing any real scene the developer was editing.
    /// Configures PlayMode to always boot from MainMenu so clicking Play in the Editor
    /// runs the game through the loading splash screen into the main menu.
    /// </summary>
    [InitializeOnLoad]
    public static class TopDownEditorStartup
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string SessionCheckKey =
            "TopDownGame.Editor.StartupSceneChecked.v1";

        static TopDownEditorStartup()
        {
            ConfigurePlayModeStartScene();
            EditorApplication.delayCall += EnsureProjectSceneIsOpen;
        }

        public static void ConfigurePlayModeStartScene()
        {
            SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (mainScene != null && EditorSceneManager.playModeStartScene != mainScene)
            {
                EditorSceneManager.playModeStartScene = mainScene;
            }
        }

        private static void EnsureProjectSceneIsOpen()
        {
            ConfigurePlayModeStartScene();

            if (SessionState.GetBool(SessionCheckKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureProjectSceneIsOpen;
                return;
            }

            SessionState.SetBool(SessionCheckKey, true);
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode ||
                SceneManager.sceneCount != 1 ||
                !AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath))
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!IsTemporaryStartupScene(activeScene))
            {
                return;
            }

            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            Debug.Log("Top Down Game: restored MainMenu after Unity opened a " +
                "temporary Untitled scene.");
        }

        public static bool IsTemporaryStartupScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.isDirty ||
                !string.IsNullOrEmpty(scene.path))
            {
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            if (roots.Length == 0)
            {
                return true;
            }

            return roots.Length == 1 && roots[0].name == "Main Camera" &&
                roots[0].GetComponent<Camera>() != null;
        }
    }
}
