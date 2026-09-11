#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    public sealed class SpellShowcaseTests
    {
        private GameObject player, cameraObject, ground;
        private float priorCapture;
        private readonly System.Collections.Generic.List<GameObject> enemies=new System.Collections.Generic.List<GameObject>();
        private readonly System.Collections.Generic.List<TopDownGame.Enemies.EnemyData> enemyData=new System.Collections.Generic.List<TopDownGame.Enemies.EnemyData>();
        [UnityTest]
        public IEnumerator AllNineSpellsHaveSynchronizedPresentation()
        {
            priorCapture=Time.captureDeltaTime;
            player=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            player.transform.position=new Vector3(1000,1000,0);
            player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            cameraObject=new GameObject("SpellReviewCamera",typeof(Camera));
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4f;
            camera.transform.position=new Vector3(1001,1000.5f,-10);
            ground=new GameObject("SpellReviewGround",typeof(SpriteRenderer));
            ground.transform.position=player.transform.position;
            var floor=ground.GetComponent<SpriteRenderer>();
            floor.sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/Environment/Map01_MossSoilV8.png");
            floor.drawMode=SpriteDrawMode.Tiled;floor.size=new Vector2(20,20);floor.sortingOrder=-20;
            floor.color=new Color(0.75f,0.83f,0.78f);
            yield return null;
            for(int i=0;i<3;i++)
            {
                var enemy=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Slime.prefab"));
                enemies.Add(enemy);enemy.transform.position=new Vector3(1002+i*.85f,1000+(i%2)*.7f,0);
                foreach(var behaviour in enemy.GetComponents<TopDownGame.Enemies.EnemyBase>()) behaviour.enabled=false;
                enemy.GetComponent<TopDownGame.Enemies.EnemyMovement>().enabled=false;
                yield return null;
                var data=ScriptableObject.CreateInstance<TopDownGame.Enemies.EnemyData>();enemyData.Add(data);
                typeof(TopDownGame.Enemies.EnemyData).GetField("maxHealth",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(data,50000f);
                var profile=enemy.GetComponent<TopDownGame.Enemies.Progression.EnemyRuntimeProfile>();
                profile.Configure(data,1,TopDownGame.Enemies.Progression.EnemyVariant.Normal);
                enemy.GetComponent<TopDownGame.Enemies.EnemyHealth>().ResetForSpawn(data,profile);
            }
            var manager=player.GetComponent<SkillManager>();
            var inventory=player.GetComponent<SkillInventory>();
            for(int i=0;i<4;i++) inventory.RemoveFromSlot(i);
            var cast=typeof(SkillManager).GetMethod("TryCastSlot",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(SkillId id in System.Enum.GetValues(typeof(SkillId)))
            {
                player.GetComponent<PlayerMovement>().CancelMovement();
                player.GetComponent<Rigidbody2D>().position=new Vector2(1000,1000);
                inventory.AddOrUpgrade(id.ToString());inventory.TryEquip(id.ToString(),0);
                Assert.That((bool)cast.Invoke(manager,new object[]{0,true,(Vector2?)new Vector2(1002,1000)}),Is.True,id.ToString());
                if(id==SkillId.Fireball)
                {
                    yield return new WaitForSeconds(0.06f);
                    Capture(camera,"Spell_Windup");
                    yield return new WaitForSeconds(0.15f);
                }
                else yield return new WaitForSeconds(id==SkillId.MeteorShower ? 0.58f : id==SkillId.LightningStrike ? 0.34f : 0.22f);
                Capture(camera,"Spell_"+id);
                yield return new WaitForSeconds(4.5f);
                Assert.That(manager.IsCasting,Is.False);
            }
            System.IO.Directory.CreateDirectory("Captures/SpellV4");
            Time.captureDeltaTime=1f/30;
            int frame=0;
            foreach(SkillId id in System.Enum.GetValues(typeof(SkillId)))
            {
                player.GetComponent<Rigidbody2D>().position=new Vector2(1000,1000);
                inventory.TryEquip(id.ToString(),0);
                yield return null;
                Assert.That((bool)cast.Invoke(manager,new object[]{0,true,(Vector2?)new Vector2(1002,1000)}),Is.True);
                for(int j=0;j<60;j++)
                {
                    yield return null;
                    Capture(camera,"../../Captures/SpellV4/Frame_"+(frame++).ToString("0000"));
                }
            }
            Time.captureDeltaTime=priorCapture;
        }

        private static void Capture(Camera camera,string name)
        {
            int width=name.Contains("Captures") ? 720 : 1200;
            int height=name.Contains("Captures") ? 480 : 800;
            var target=new RenderTexture(width,height,24);var old=RenderTexture.active;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                System.IO.File.WriteAllBytes("Assets/Screenshots/"+name+"_Verified.png",image.EncodeToPNG());
            }
            finally{camera.targetTexture=null;RenderTexture.active=old;target.Release();Object.Destroy(target);Object.Destroy(image);}
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(player);Object.Destroy(cameraObject);Object.Destroy(ground);
            foreach(var enemy in enemies)Object.Destroy(enemy);
            foreach(var data in enemyData)Object.Destroy(data);
            Time.captureDeltaTime=priorCapture;
            foreach(var effect in Object.FindObjectsByType<SpellVfx>()) Object.Destroy(effect.gameObject);
            foreach(var effect in Object.FindObjectsByType<SpellPresentation>()) Object.Destroy(effect.gameObject);
            yield return null;
        }
    }
}
#endif
