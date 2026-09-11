using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TopDownGame.EditorTools
{
    /// <summary>Matched before/after camera samples; restores the user's scene setup and never accesses player saves.</summary>
    public static class Map02ClarityVerification
    {
        public static void Capture()
        {
            if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Stop Play and save edits first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            Material material=null;
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/Map02.unity");
                var roots=scene.GetRootGameObjects();
                foreach(var canvas in roots.SelectMany(r=>r.GetComponentsInChildren<Canvas>()))canvas.enabled=false;
                var floor=roots.Single(r=>r.name=="Environment").transform.Find("Floor").GetComponent<SpriteRenderer>();
                material=new Material(floor.sharedMaterial);floor.sharedMaterial=material;
                var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).First();
                foreach(var view in new[]{("Pond",new Vector3(8.7f,1,-10)),("Camp",new Vector3(-7,-1,-10))})
                foreach(int height in new[]{1080,2160})
                foreach(float clarity in new[]{0f,0.65f})
                {
                    camera.transform.position=view.Item2;camera.orthographicSize=5.2f;
                    material.SetFloat("_Clarity",clarity);
                    Frame(camera,height,"Map02_ClarityV2_"+view.Item1+"_"+height+(clarity==0?"_Before":"_After"));
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
                if(material!=null)Object.DestroyImmediate(material);
            }
        }
        private static void Frame(Camera camera,int height,string name)
        {
            int width=height*16/9;
            var target=new RenderTexture(width,height,24);
            var previous=RenderTexture.active;var priorTarget=camera.targetTexture;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                Directory.CreateDirectory("Assets/Screenshots");
                File.WriteAllBytes("Assets/Screenshots/"+name+".png",image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=priorTarget;RenderTexture.active=previous;
                target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
            }
        }
    }
}
