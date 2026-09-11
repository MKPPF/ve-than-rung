using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Checks automatic charge, cancellation, pause and real travel using only the isolated test save.</summary>
    public sealed class ZZZPortalTransitionTests
    {
        private GameObject player, portalObject;
        private string originalProgress;
        private GameProgressManager progress;
        private bool travelled;
        private int arrivals;
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator StepIntoCircleChargesCancelsAndTravelsExactlyOnce()
        {
            Assert.That((bool)typeof(GameProgressManager).GetProperty("IsAutomatedTestRun",
                BindingFlags.Static|BindingFlags.NonPublic).GetValue(null),Is.True);
            if(GameProgressManager.Instance==null) new GameObject("PortalTestProgress").AddComponent<GameProgressManager>();
            progress=GameProgressManager.Instance;
            originalProgress=JsonUtility.ToJson(progress);
            typeof(GameProgressManager).GetField("hasSafeRespawn",Private).SetValue(progress,false);
            SceneTransition.ResetLoading();
            portalObject=new GameObject("TeleportTestProbe");
            portalObject.transform.position=new Vector3(40,40,0);
            portalObject.AddComponent<CircleCollider2D>().radius=0.7f;
            LobbyPortal portal=portalObject.AddComponent<LobbyPortal>();
            typeof(LobbyPortal).GetField("chargeDuration",Private).SetValue(portal,0.65f);
            player=new GameObject("TeleportPlayerProbe");
            player.tag="Player";
            player.transform.position=new Vector3(42,40,0);
            player.AddComponent<PlayerStats>();
            player.AddComponent<PlayerHealth>();
            var interaction=player.AddComponent<PlayerInteraction>();
            player.AddComponent<CircleCollider2D>().radius=0.15f;
            Rigidbody2D body=player.AddComponent<Rigidbody2D>();
            body.bodyType=RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts=true;
            yield return null;
            portal.Interact(interaction);
            yield return new WaitForSeconds(0.1f);
            Assert.That(portal.ChargeProgress,Is.Zero,"Nearby E must not skip walking into the dais.");
            body.position=new Vector2(40,40);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.18f);
            Assert.That(portal.ChargeProgress,Is.GreaterThan(0));
            float pausedCharge=portal.ChargeProgress;
            Time.timeScale=0;
            yield return null; yield return null;
            Assert.That(portal.ChargeProgress,Is.EqualTo(pausedCharge));
            Time.timeScale=1;
            body.position=new Vector2(42,40);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null; yield return null;
            Assert.That(portal.ChargeProgress,Is.Zero,"Leaving cancels charge.");
            GameplayState.EnterModal(portalObject);
            body.position=new Vector2(40,40);
            Physics2D.SyncTransforms();
            yield return null;
            Assert.That(portal.ChargeProgress,Is.Zero);
            GameplayState.ExitModal(portalObject);
            SceneManager.sceneLoaded+=OnSceneLoaded;
            float choiceTimeout=Time.realtimeSinceStartup+5;
            while(!portal.IsChoosing && Time.realtimeSinceStartup<choiceTimeout) yield return null;
            Assert.That(portal.IsChoosing,Is.True);
            Assert.That(portal.IsTransporting,Is.False,"Entering must only open the picker.");
            Assert.That(portal.ChooseDestination("MainMenu"),Is.False);
            if(!portal.IsDestinationAvailable("RiftMap01"))
                Assert.That(portal.ChooseDestination("RiftMap01"),Is.False);
            portal.CancelChoice();
            Assert.That(GameplayState.IsInputBlocked,Is.False);
            yield return new WaitForSeconds(0.1f);
            Assert.That(portal.IsChoosing,Is.False,"Cancel must not immediately reopen.");
            portal.Interact(interaction);
            yield return null;
            Assert.That(portal.IsChoosing,Is.True);
            Assert.That(portal.ChooseDestination("Map01"),Is.True);
            Assert.That(portal.IsTransporting, Is.True);
            Assert.That(portal.IsChoosing, Is.False);
            Assert.That(portal.ChooseDestination("Map01"), Is.False, "Double clicks cannot start a second departure.");
            yield return new WaitForSeconds(0.4f);
            Assert.That(travelled, Is.False, "Destination selection starts effects, not an immediate scene load.");
            Assert.That(portal.DepartureProgress, Is.GreaterThan(0f));
            float departureBeforePause = portal.DepartureProgress;
            Time.timeScale = 0f;
            yield return null; yield return null;
            Assert.That(portal.DepartureProgress, Is.EqualTo(departureBeforePause));
            Time.timeScale = 1f;
            float timeout=Time.realtimeSinceStartup+20;
            while(!travelled && Time.realtimeSinceStartup<timeout) yield return null;
            Assert.That(travelled,Is.True,"Stepping back in must travel to Map01.");
            yield return null;
            Assert.That(arrivals,Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("Map01"));
            var camera=Camera.main;
            var follow=camera.GetComponent<TopDownGame.Core.CameraFollow2D>();
            follow.enabled=false;
            camera.transform.position=new Vector3(0,0,-10);
            camera.orthographicSize=17f;
            var render=new RenderTexture(1500,1000,24);
            var old=RenderTexture.active;
            try
            {
                camera.targetTexture=render;
                camera.Render();
                RenderTexture.active=render;
                var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();
                System.IO.File.WriteAllBytes("Assets/Screenshots/Map01_V8_Verified.png",image.EncodeToPNG());
                Object.Destroy(image);
            }
            finally {RenderTexture.active=old;camera.targetTexture=null;render.Release();Object.Destroy(render);}
        }

        private void OnSceneLoaded(Scene scene,LoadSceneMode mode)
        {
            if(scene.name=="Map01") {arrivals++; travelled=true;}
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded-=OnSceneLoaded;
            if(portalObject!=null) GameplayState.ExitModal(portalObject);
            Time.timeScale=1;
            if(player!=null) Object.Destroy(player);
            if(portalObject!=null) Object.Destroy(portalObject);
            if(travelled)
            {
                Scene combat=SceneManager.GetSceneByName("Map01");
                Scene clean=SceneManager.CreateScene("PortalTestCleanup");
                SceneManager.SetActiveScene(clean);
                if(combat.IsValid() && combat.isLoaded) yield return SceneManager.UnloadSceneAsync(combat);
            }
            if(progress!=null && originalProgress!=null) JsonUtility.FromJsonOverwrite(originalProgress,progress);
            PlayerPrefs.DeleteKey("TopDownGame.Tests.Progress.v1");
            PlayerPrefs.DeleteKey("TopDownGame.Tests.Progress.GoldGrant.v1");
            SceneTransition.ResetLoading();
            yield return null;
        }
    }
}
