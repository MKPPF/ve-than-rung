using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TopDownGame.EditorTools
{
    /// <summary>Reproducible two-pass builder and scene previews, without loading player saves.</summary>
    public static class Map02Verification
    {
        public static void BuildLivingAndValidate()
        {
            if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Stop Play and save current edits before rebuilding.");
            TopDownGameBuilder.BuildCompleteMvpAutomated();
            Debug.Log("MAP02_LIVING_V6_BUILD_PASS_1");
            TopDownGameBuilder.BuildCompleteMvpAutomated();
            Debug.Log("MAP02_LIVING_V6_BUILD_PASS_2");
            TopDownProjectValidator.ValidateProjectAutomated();
        }
        public static void BuildColorAndValidate()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Stop Play and save current edits before rebuilding.");
            TopDownGameBuilder.BuildCompleteMvpAutomated();
            Debug.Log("MAP02_COLOR_V5_BUILD_PASS_1");
            TopDownGameBuilder.BuildCompleteMvpAutomated();
            Debug.Log("MAP02_COLOR_V5_BUILD_PASS_2");
            TopDownProjectValidator.ValidateProjectAutomated();
            CaptureColorComparison();
        }

        public static void CaptureColorComparison()
        {
            if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Capture requires a clean non-playing scene.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            Material previewMaterial = null;
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/Map02.unity");
                var roots = scene.GetRootGameObjects();
                var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).First();
                foreach (var canvas in roots.SelectMany(r => r.GetComponentsInChildren<Canvas>())) canvas.enabled = false;
                var floor = roots.Single(r => r.name == "Environment").transform.Find("Floor").GetComponent<SpriteRenderer>();
                previewMaterial = new Material(floor.sharedMaterial);
                floor.sharedMaterial = previewMaterial;
                foreach (var view in new[] { ("HoThac", new Vector3(6, 3, -10)), ("TraiRung", new Vector3(-7,-1,-10)) })
                {
                    previewMaterial.SetFloat("_Strength", 0);
                    CaptureFrame(camera, view.Item2, 5.2f, "Map02_ColorV5_" + view.Item1 + "_Before");
                    previewMaterial.SetFloat("_Strength", 1);
                    CaptureFrame(camera, view.Item2, 5.2f, "Map02_ColorV5_" + view.Item1 + "_After");
                }
                CaptureFrame(camera, new Vector3(0,0,-10), 12, "Map02_ColorV5_ToanCanh_After");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
                if (previewMaterial != null) Object.DestroyImmediate(previewMaterial);
            }
        }

        public static void BuildAndValidate()
        {
            TopDownGameBuilder.BuildCompleteMvpAutomated();
            Debug.Log("MAP02_BUILD_PASS_1");
            TopDownGameBuilder.BuildCompleteMvpAutomated();
            Debug.Log("MAP02_BUILD_PASS_2");
            TopDownProjectValidator.ValidateProjectAutomated();
            Capture();
        }

        public static void Capture()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Map02.unity");
            var all = scene.GetRootGameObjects();
            var camera = all.SelectMany(r => r.GetComponentsInChildren<Camera>()).First();
            foreach (var canvas in all.SelectMany(r => r.GetComponentsInChildren<Canvas>())) canvas.enabled = false;
            CaptureFrame(camera, new Vector3(0, 0, -10), 12f, "Map02_V4_ToanCanh");
            CaptureFrame(camera, new Vector3(-7, -1, -10), 5.2f, "Map02_V4_TraiGoblin");
            CaptureFrame(camera, new Vector3(6, 3, -10), 5.2f, "Map02_V4_TanTich");
            foreach (var blocker in all.SelectMany(r => r.GetComponentsInChildren<BoxCollider2D>()))
            {
                if (!blocker.name.StartsWith("ForestTerrain_")) continue;
                var renderer = blocker.GetComponent<SpriteRenderer>();
                renderer.enabled = true;
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = blocker.size;
                renderer.color = new Color(1, 0.05f, 0.1f, 0.3f);
                renderer.sortingOrder = -25;
            }
            CaptureFrame(camera, new Vector3(0, 0, -10), 12f, "Map02_V4_CollisionOverlay");
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

        private static void CaptureFrame(Camera camera, Vector3 position, float size, string name)
        {
            camera.transform.position = position;
            camera.orthographicSize = size;
            bool overview = size > 10f;
            int width = overview ? 1800 : 1920;
            int height = overview ? 1200 : 1080;
            var target = new RenderTexture(width, height, 24);
            var old = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                Directory.CreateDirectory("Assets/Screenshots");
                File.WriteAllBytes("Assets/Screenshots/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = old;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
