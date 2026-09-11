using TopDownGame.Maps;
using UnityEditor;
using UnityEngine;

namespace TopDownGame.EditorTools
{
    /// <summary>Small authored plaza routes preserve the existing station approaches and compass landmark.</summary>
    public static partial class TopDownGameBuilder
    {
        private static void CreateLobbyVisitors()
        {
            var root=new GameObject("LobbyVisitors_Generated");
            Vector2[][] routes={
                new[]{new Vector2(-1.6f,-1.8f),new Vector2(0.3f,-1.8f),new Vector2(0.3f,-0.7f),new Vector2(-1.6f,-0.7f)},
                new[]{new Vector2(1.1f,-0.45f),new Vector2(4.5f,-0.45f),new Vector2(4.5f,-1.3f),new Vector2(1.1f,-1.3f)},
                new[]{new Vector2(0.7f,1.1f),new Vector2(3.6f,1.1f),new Vector2(3.6f,0.4f),new Vector2(0.7f,0.4f)}
            };
            Color[] colors={new Color(0.42f,0.70f,0.37f),new Color(0.75f,0.35f,0.27f),new Color(0.65f,0.43f,0.77f)};
            for(int i=0;i<routes.Length;i++)
            {
                var visitor=new GameObject("Lữ khách "+(i+1));visitor.transform.SetParent(root.transform,false);
                visitor.transform.position=routes[i][0];
                var body=visitor.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
                body.gravityScale=0;body.freezeRotation=true;body.interpolation=RigidbodyInterpolation2D.Interpolate;
                var movement=visitor.AddComponent<LobbyVisitor>();SetVector2Array(movement,"route",routes[i]);
                Set(movement,"speed",0.65f+i*0.09f);Set(movement,"initialDelay",i*1.7f);Set(movement,"restDuration",2.5f+i*0.8f);
                var visual=new GameObject("Appearance");visual.transform.SetParent(visitor.transform,false);visual.transform.localScale=Vector3.one*0.72f;
                var renderer=visual.AddComponent<SpriteRenderer>();renderer.sprite=PlayerIdleSprite();renderer.sortingOrder=9;
                string path="Assets/Settings/LobbyVisitorCloth"+i+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/LobbyVisitorCloth.shader");
                if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}else mat.shader=shader;
                mat.SetColor("_Cloth",colors[i]);EditorUtility.SetDirty(mat);renderer.sharedMaterial=mat;
                var animator=visitor.AddComponent<LobbyVisitorAnimator>();Set(animator,"movement",movement);Set(animator,"visual",renderer);
                Set(animator,"idle",PlayerIdleSprite());SetArray(animator,"rightFrames",CardinalPlayerWalkSprites("Right"));
                SetArray(animator,"upFrames",CardinalPlayerWalkSprites("Up"));SetArray(animator,"downFrames",CardinalPlayerWalkSprites("Down"));
                Set(animator,"strideLength",0.72f);
            }
            var decor=new GameObject("PlazaSupplies_Generated");
            foreach(var point in new[]{new Vector2(2.6f,-2.25f),new Vector2(-3.1f,1.45f)})
            {
                var prop=CreateSpriteProp("TravelSupplies",decor.transform,GeneratedPropSprite("ForgeSupplyClusterV3"),point,1.05f,8,false);
                // Low footprints keep paths consistent with the visual props.
                var blocker=new GameObject("SupplyFootprint",typeof(BoxCollider2D));blocker.transform.SetParent(decor.transform,false);
                blocker.transform.position=point+Vector2.down*0.33f;blocker.GetComponent<BoxCollider2D>().size=new Vector2(0.62f,0.32f);
            }
        }
    }
}
