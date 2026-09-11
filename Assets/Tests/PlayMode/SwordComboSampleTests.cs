#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Skills;
using TopDownGame.Enemies;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    public sealed class SwordComboSampleTests
    {
        private GameObject player,stage,cameraObject;
        private readonly List<GameObject> targets=new List<GameObject>();
        private readonly List<Vector2> cues=new List<Vector2>();
        private readonly List<EnemyData> data=new List<EnemyData>();
        private float oldCapture,startTime;
        private void OnCue(bool hit)=>cues.Add(new Vector2(Time.time-startTime,hit?1:0));

        [UnityTest]
        public IEnumerator ComboDamagesRealEnemiesPausesAndReturnsToIdle()
        {
            oldCapture=Time.captureDeltaTime;
            player=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            player.transform.position=new Vector3(1000,1000,0);player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            stage=new GameObject("SwordReviewGround",typeof(SpriteRenderer));stage.transform.position=player.transform.position;
            var ground=stage.GetComponent<SpriteRenderer>();ground.sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/Environment/Map01_MossSoilV8.png");
            ground.drawMode=SpriteDrawMode.Tiled;ground.size=new Vector2(20,20);ground.sortingOrder=-20;ground.color=new Color(.8f,.88f,.82f);
            cameraObject=new GameObject("SwordReviewCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=3.4f;camera.transform.position=new Vector3(1000,1000.2f,-10);
            for(int i=0;i<3;i++)
            {
                var enemy=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Slime.prefab"));
                targets.Add(enemy);enemy.transform.position=player.transform.position+new Vector3(i==0?1.25f:i==1?-1.2f:.6f,i==2?1.25f:-.1f,0);
                foreach(var behaviour in enemy.GetComponents<EnemyBase>()) behaviour.enabled=false;
                enemy.GetComponent<EnemyMovement>().enabled=false;
                yield return null;
                var d=ScriptableObject.CreateInstance<EnemyData>();data.Add(d);
                typeof(EnemyData).GetField("maxHealth",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(d,5000f);
                var runtime=enemy.GetComponent<TopDownGame.Enemies.Progression.EnemyRuntimeProfile>();
                runtime.Configure(d,1,TopDownGame.Enemies.Progression.EnemyVariant.Normal);
                enemy.GetComponent<EnemyHealth>().ResetForSpawn(d,runtime);
            }
            yield return null;
            var inventory=player.GetComponent<SkillInventory>();for(int i=0;i<4;i++) inventory.RemoveFromSlot(i);
            inventory.TryEquip("SwordSpin",0);var manager=player.GetComponent<SkillManager>();
            var cast=typeof(SkillManager).GetMethod("TryCastSlot",BindingFlags.NonPublic|BindingFlags.Instance);
            var health=targets[0].GetComponent<EnemyHealth>();float before=health.CurrentHealth;
            Directory.CreateDirectory("Captures/SwordSampleV3");Time.captureDeltaTime=1f/30f;
            startTime=Time.time;SwordComboVisual.FeedbackPlayed+=OnCue;
            Assert.That((bool)cast.Invoke(manager,new object[]{0,true,(Vector2?)new Vector2(1001,1000)}),Is.True);
            for(int frame=0;frame<150;frame++)
            {
                yield return null;
                if(frame==18)
                {
                    float pausedHealth=health.CurrentHealth;Time.timeScale=0;
                    yield return new WaitForSecondsRealtime(.15f);
                    Assert.That(health.CurrentHealth,Is.EqualTo(pausedHealth));Time.timeScale=1;
                }
                if(frame==95) Assert.That((bool)cast.Invoke(manager,new object[]{0,true,(Vector2?)new Vector2(999,1000)}),Is.True);
                Capture(camera,frame);
            }
            SwordComboVisual.FeedbackPlayed-=OnCue;
            Assert.That(health.CurrentHealth,Is.LessThan(before));
            Assert.That(manager.IsCasting,Is.False);
            Assert.That(Object.FindObjectsByType<SwordComboVisual>(),Is.Empty);
            var visual=player.GetComponent<PlayerMovement>().VisualRenderer;
            Assert.That(visual.sharedMaterial,Is.Not.SameAs(player.GetComponent<SwordSpinSkill>().ComboProfile.ActorMaterial));
            WriteAudio(player.GetComponent<SwordSpinSkill>().ComboProfile);
            Time.captureDeltaTime=oldCapture;
        }

        private static void Capture(Camera camera,int frame)
        {
            var rt=new RenderTexture(960,640,24);var old=RenderTexture.active;var image=new Texture2D(960,640,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,960,640),0,0);image.Apply();
                File.WriteAllBytes("Captures/SwordSampleV3/Frame_"+frame.ToString("0000")+".png",image.EncodeToPNG());
                if(frame==10)
                {
                    File.WriteAllBytes("Assets/Screenshots/SwordComboV3_Contact.png",image.EncodeToPNG());
                    var description=new System.Text.StringBuilder();
                    foreach(var renderer in Object.FindObjectsByType<SpriteRenderer>())
                        if(Vector3.Distance(renderer.transform.position,new Vector3(1000,1000,0))<5)
                            description.AppendLine(renderer.name+"|"+renderer.color+"|"+renderer.sprite?.name+"|"+renderer.transform.position+"|"+renderer.sharedMaterial?.shader.name);
                    File.WriteAllText("Logs/SwordSampleRenderers.txt",description.ToString());
                }
            }finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
        }
        private void WriteAudio(SwordComboProfile profile)
        {
            const int rate=22050;var mix=new float[rate*5];
            foreach(var cue in cues)
            {
                var source=cue.y>0?profile.HitCue:profile.SwingCue;var clip=source.RandomClip;
                var samples=new float[clip.samples*clip.channels];clip.GetData(samples,0);
                int offset=Mathf.RoundToInt(cue.x*rate);
                for(int i=0;i<samples.Length && offset+i<mix.Length;i++) if(offset+i>=0) mix[offset+i]+=samples[i]*source.Volume*.65f;
            }
            using(var writer=new BinaryWriter(File.Create("Captures/SwordSampleV3/Audio.wav")))
            {writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+mix.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(mix.Length*2);foreach(float sample in mix)writer.Write((short)(Mathf.Clamp(sample,-1,1)*32767));}
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            SwordComboVisual.FeedbackPlayed-=OnCue;Time.captureDeltaTime=oldCapture;Time.timeScale=1;
            Object.Destroy(player);Object.Destroy(stage);Object.Destroy(cameraObject);
            foreach(var t in targets)Object.Destroy(t);foreach(var d in data)Object.Destroy(d);
            yield return null;
        }
    }
}
#endif
