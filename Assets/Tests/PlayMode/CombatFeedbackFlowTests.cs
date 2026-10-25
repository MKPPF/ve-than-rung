using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Enemies;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.Skills;
using TopDownGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Verifies bounded impact feedback through real health, camera, pause and settings paths with isolated preferences.</summary>
    public sealed class CombatFeedbackFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private readonly Dictionary<string,(bool exists,int value)> preferences = new Dictionary<string,(bool,int)>();
        private string saved, state;
        private bool hadSave;
        private GameObject owner, secondOwner, player, enemyObject;
        private EnemyData data;
        private PlayerHealth health;
        private CameraFollow2D follow;
        private CombatFeedback driver;
        private GameProgressManager Progress => GameProgressManager.Instance;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            foreach (string name in new[] { "CameraShake","Hitstop" })
            {
                string key=GamePreferences.Key("TopDownGame.Feedback."+name);
                Assert.That(key,Does.StartWith("TopDownGame.Tests."));
                preferences[key]=(PlayerPrefs.HasKey(key),PlayerPrefs.GetInt(key));
            }
            if(Progress==null) new GameObject("FeedbackProgressProbe").AddComponent<GameProgressManager>();
            saved=PlayerPrefs.GetString(SaveKey,""); hadSave=PlayerPrefs.HasKey(SaveKey);
            state=JsonUtility.ToJson(Progress);
            FeedbackPreferences.SetCameraShakeEnabled(true);
            FeedbackPreferences.SetHitstopEnabled(true);
            Progress.BeginNewGame(); Progress.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null; yield return null;
            player=GameObject.FindWithTag("Player");
            player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            health=player.GetComponent<PlayerHealth>();
            follow=Camera.main.GetComponent<CameraFollow2D>();
            Assert.That(CameraFollow2D.Active,Is.SameAs(follow));
            driver=CombatFeedback.EnsureExists(); driver.enabled=true;
            GameplayState.CancelHitstop();
            owner=new GameObject("FeedbackPauseProbe"); secondOwner=new GameObject("FeedbackNestedProbe");
            follow.ClearShake();
        }

        [UnityTest]
        public IEnumerator HitstopCoalescesHitsCapsEachBurstAndAllowsRecoveryBetweenContinuousRequests()
        {
            Assert.That(GameplayState.RequestHitstop(0.02f),Is.True);
            for(int i=0;i<100;i++) GameplayState.RequestHitstop(10f);
            Assert.That(Time.timeScale,Is.Zero);
            Assert.That(GameplayState.HitstopRemaining,Is.InRange(0f,GameplayState.MaximumHitstopDuration+0.001f));
            float end=Time.unscaledTime+0.3f;
            bool sawRecovery=false;
            while(Time.unscaledTime<end)
            {
                GameplayState.RequestHitstop(10f);
                Assert.That(GameplayState.HitstopRemaining,Is.LessThanOrEqualTo(GameplayState.MaximumHitstopDuration+0.001f));
                if(Time.timeScale>0f) sawRecovery=true;
                yield return null;
            }
            Assert.That(sawRecovery,Is.True,"Multi-hit must not hold the game frozen continuously.");
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(GameplayState.IsHitstopActive,Is.False);
            Assert.That(Time.timeScale,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PauseNestedModalDriverDisableAndSceneTransitionCannotLeaveTimeFrozenOrResumeBehindMenu()
        {
            GameplayState.RequestHitstop(0.065f);
            GameplayState.EnterModal(owner);
            GameplayState.EnterModal(secondOwner);
            Assert.That(GameplayState.IsHitstopActive,Is.False);
            yield return new WaitForSecondsRealtime(0.12f);
            GameplayState.ExitModal(owner);
            Assert.That(Time.timeScale,Is.Zero);
            Assert.That(GameplayState.RequestHitstop(1),Is.False);
            FeedbackPreferences.SetHitstopEnabled(false);
            Assert.That(Time.timeScale,Is.Zero);
            GameplayState.ExitModal(secondOwner);
            Assert.That(Time.timeScale,Is.EqualTo(1));
            FeedbackPreferences.SetHitstopEnabled(true);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(GameplayState.RequestHitstop(0.065f),Is.True);
            driver.enabled=false;
            Assert.That(Time.timeScale,Is.EqualTo(1));
            Assert.That(GameplayState.RequestHitstop(1),Is.False);
            driver.enabled=true;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(GameplayState.RequestHitstop(0.065f),Is.True);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            Assert.That(GameplayState.IsHitstopActive,Is.False);
            Assert.That(Time.timeScale,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ScaledCooldownAndCoroutineWaitFreezeWhileUnscaledUiClockContinues()
        {
            bool finished=false;
            IEnumerator Timeline() {yield return new WaitForSeconds(0.05f);finished=true;}
            driver.StartCoroutine(Timeline());
            SkillManager skills=player.GetComponent<SkillManager>();
            SkillData skill=skills.GetSkillData(0);
            skills.Cooldowns.Start(skill.Id,0.5f,1);
            float scaled=Time.time, real=Time.unscaledTime;
            Assert.That(GameplayState.RequestHitstop(0.065f),Is.True);
            yield return new WaitForSecondsRealtime(0.025f);
            Assert.That(Time.unscaledTime,Is.GreaterThan(real));
            Assert.That(Time.time-scaled,Is.LessThan(0.035f));
            Assert.That(finished,Is.False);
            Assert.That(skills.GetCooldownRemaining(0),Is.GreaterThan(0.46f));
            yield return new WaitForSeconds(0.08f);
            Assert.That(finished,Is.True);
            Assert.That(Time.timeScale,Is.EqualTo(1));
        }

        private EnemyHealth Enemy()
        {
            enemyObject=new GameObject("FeedbackEnemy",typeof(BoxCollider2D));
            enemyObject.transform.position=player.transform.position+Vector3.right;
            EnemyHealth target=enemyObject.AddComponent<EnemyHealth>();
            data=ScriptableObject.CreateInstance<EnemyData>();
            typeof(EnemyData).GetField("maxHealth",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(data,5000f);
            target.Initialize(data);
            return target;
        }

        [UnityTest]
        public IEnumerator OnlyConfirmedVisiblePlayerDirectHitsTriggerFeedbackAndPoisonOrDodgedHitsDoNot()
        {
            EnemyHealth target=Enemy();
            yield return null;
            ((IDamageable)target).TakeDamage(new DamageInfo(10,player,target.transform.position,Vector2.right,3,true));
            Assert.That(target.CurrentHealth,Is.LessThan(target.MaxHealth));
            Assert.That(GameplayState.IsHitstopActive,Is.True);
            Assert.That(follow.IsShaking,Is.True);
            yield return new WaitForSecondsRealtime(0.25f);
            follow.ClearShake();
            ((IDamageable)target).TakeDamage(new DamageInfo(10,player,target.transform.position,Vector2.right,3,true,false,true));
            Assert.That(GameplayState.IsHitstopActive,Is.False);
            Assert.That(follow.IsShaking,Is.False);
            ((IDamageable)target).TakeDamage(new DamageInfo(10,enemyObject,target.transform.position,Vector2.right,3,true));
            Assert.That(GameplayState.IsHitstopActive,Is.False,"Non-player sources cannot request outgoing feedback.");
            target.transform.position=player.transform.position+Vector3.right*1000;
            ((IDamageable)target).TakeDamage(new DamageInfo(10,player,target.transform.position,Vector2.right,3,true));
            Assert.That(GameplayState.IsHitstopActive,Is.False,"Offscreen damage cannot interrupt the visible encounter.");
            Assert.That(follow.IsShaking,Is.False);
            target.transform.position=player.transform.position+Vector3.right;
            health.TakeStatusDamage(10);
            Assert.That(follow.IsShaking,Is.False);
            Assert.That(GameplayState.IsHitstopActive,Is.False);
            Assert.That(player.GetComponent<PlayerDodge>().TryDodge(Vector2.up),Is.True);
            float hp=health.CurrentHealth;
            ((IDamageable)health).TakeDamage(new DamageInfo(10,enemyObject,player.transform.position,Vector2.left));
            Assert.That(health.CurrentHealth,Is.EqualTo(hp));
            Assert.That(follow.IsShaking,Is.False);
            yield return new WaitForSeconds(0.35f);
            ((IDamageable)health).TakeDamage(new DamageInfo(10,enemyObject,player.transform.position,Vector2.left));
            Assert.That(health.CurrentHealth,Is.LessThan(hp));
            Assert.That(follow.IsShaking,Is.True);
            Assert.That(GameplayState.IsHitstopActive,Is.False,"Player damage shakes but does not request outgoing hitstop.");
            FeedbackPreferences.SetCameraShakeEnabled(false);
            FeedbackPreferences.SetHitstopEnabled(false);
            float targetHealth=target.CurrentHealth;
            ((IDamageable)target).TakeDamage(new DamageInfo(10,player,target.transform.position,Vector2.right,3,true));
            Assert.That(target.CurrentHealth,Is.LessThan(targetHealth));
            Assert.That(GameplayState.IsHitstopActive,Is.False);
            Assert.That(follow.IsShaking,Is.False);
        }

        [UnityTest]
        public IEnumerator ShakeNeverAccumulatesDriftRespectsMapBoundsAndCancelsForPauseSettingAndDisable()
        {
            Vector3 baseline=follow.transform.position;
            typeof(CameraFollow2D).GetField("smoothTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(follow,0.2f);
            float maximum=0;
            follow.RequestShake(100,10);
            float end=Time.unscaledTime+0.16f;
            while(Time.unscaledTime<end)
            {
                yield return null;
                maximum=Mathf.Max(maximum,follow.ShakeOffset.magnitude);
                Assert.That(follow.ShakeOffset.magnitude,Is.LessThanOrEqualTo(follow.MaximumShakeAmplitude+0.0001f));
            }
            Assert.That(maximum,Is.GreaterThan(0.001f));
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(follow.ShakeOffset,Is.EqualTo(Vector2.zero));
            Assert.That(Vector3.Distance(follow.transform.position,baseline),Is.LessThan(0.001f));
            follow.RequestShake(0.1f,0.3f);
            FeedbackPreferences.SetCameraShakeEnabled(false);
            Assert.That(follow.ShakeOffset,Is.EqualTo(Vector2.zero));
            Assert.That(follow.IsShaking,Is.False);
            follow.RequestShake(1,1);
            Assert.That(follow.IsShaking,Is.False);
            FeedbackPreferences.SetCameraShakeEnabled(true);
            follow.RequestShake(0.1f,0.3f);
            GameplayState.EnterModal(owner);
            yield return null; yield return null;
            Assert.That(follow.IsShaking,Is.False);
            GameplayState.ExitModal(owner);
            yield return null;
            follow.RequestShake(0.1f,0.3f);
            follow.enabled=false;
            Assert.That(follow.ShakeOffset,Is.EqualTo(Vector2.zero));
            Assert.That(CameraFollow2D.Active,Is.Null);
            follow.enabled=true;
            Rect bounds=(Rect)typeof(CameraFollow2D).GetField("worldBounds",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(follow);
            var edgeTarget=new GameObject("FeedbackBoundsTarget");
            edgeTarget.transform.position=new Vector3(bounds.xMax,bounds.yMax,0);
            follow.SetTarget(edgeTarget.transform);
            follow.RequestShake(0.16f,0.15f);
            yield return null; yield return null;
            Camera camera=follow.GetComponent<Camera>();
            Vector2 clamped=CameraFollow2D.ClampViewportCenter(follow.transform.position,bounds,camera.orthographicSize,camera.aspect);
            Assert.That(Vector2.Distance(clamped,follow.transform.position),Is.LessThan(0.001f));
            follow.BeginCinematicFocus(owner,player.transform.position,1.2f);
            Assert.That(follow.IsShaking,Is.False);
            follow.RequestShake(1,1);
            Assert.That(follow.IsShaking,Is.False);
            follow.EndCinematicFocus(owner);
            follow.RequestShake(0.1f,0.2f);
            typeof(CameraFollow2D).GetField("target",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(follow,null);
            typeof(CameraFollow2D).GetField("targetTag",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(follow,"");
            yield return null;yield return null;
            Assert.That(follow.IsShaking,Is.False);
            Assert.That(follow.ShakeOffset,Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator SettingsTogglesPersistAcrossNewGameReloadAndMainMenuPauseAndRenderAtThreeRatios()
        {
            foreach(string scene in new[]{"MainMenu","Lobby"})
            {
                yield return SceneManager.LoadSceneAsync(scene); yield return null; yield return null;
                MenuDialog dialog;
                SettingsPanel settings;
                if(scene=="MainMenu")
                {
                    var menu=Object.FindAnyObjectByType<MainMenuController>();
                    dialog=menu.GetComponent<MenuDialog>(); settings=menu.GetComponent<SettingsPanel>();
                }
                else
                {
                    var pause=Object.FindAnyObjectByType<PauseMenu>(); pause.Toggle();
                    dialog=pause.GetComponent<MenuDialog>(); settings=pause.GetComponent<SettingsPanel>();
                }
                settings.Open(dialog); yield return null;
                foreach(bool enabled in new[]{true,false})
                {
                    foreach(string name in new[]{"CameraShake","Hitstop"})
                    {
                        Button button=GameObject.Find(name).GetComponent<Button>();
                        bool current=name=="CameraShake" ? FeedbackPreferences.CameraShakeEnabled : FeedbackPreferences.HitstopEnabled;
                        if(current!=enabled) button.onClick.Invoke();
                        Assert.That(button.GetComponentInChildren<TMP_Text>().text,Does.EndWith(enabled ? "BẬT" : "TẮT"));
                    }
                    Assert.That(Time.timeScale,Is.Zero);
                    yield return RenderSettings(scene,enabled);
                }
                Button closedToggle=GameObject.Find("CameraShake").GetComponent<Button>();
                dialog.Close();
                bool before=FeedbackPreferences.CameraShakeEnabled;
                closedToggle.onClick.Invoke();
                Assert.That(FeedbackPreferences.CameraShakeEnabled,Is.EqualTo(before));
                if(scene=="Lobby") Object.FindAnyObjectByType<PauseMenu>().Resume();
                Progress.BeginNewGame(); Progress.CompleteTutorial();
                FeedbackPreferences.Reload();
                Assert.That(FeedbackPreferences.CameraShakeEnabled,Is.False);
                Assert.That(FeedbackPreferences.HitstopEnabled,Is.False);
            }
        }

        [UnityTest]
        public IEnumerator AuthoredMeteorImpactAndBossSlamUseFeedbackAndBossWindupSurvivesPause()
        {
            var meteor=player.GetComponent<MeteorShowerSkill>();
            Assert.That(meteor,Is.Not.Null);
            typeof(MeteorShowerSkill).GetMethod("DamageImpact",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(meteor,new object[]{(Vector2)player.transform.position,10f});
            Assert.That(follow.IsShaking,Is.True,"Authored meteor impact must request camera feedback even on empty ground.");
            yield return SceneManager.LoadSceneAsync("BossMap01");yield return null;yield return null;
            player=GameObject.FindWithTag("Player");
            player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            health=player.GetComponent<PlayerHealth>();
            follow=Camera.main.GetComponent<CameraFollow2D>();
            var boss=Object.FindAnyObjectByType<TopDownGame.Enemies.Bosses.ForestGuardianBoss>();
            boss.StopAllCoroutines();boss.enabled=false;
            player.GetComponent<PlayerMovement>().CancelMovement();
            player.GetComponent<Rigidbody2D>().position=(Vector2)boss.transform.position+Vector2.right;
            Physics2D.SyncTransforms();
            follow.SetTarget(player.transform);
            yield return new WaitForSecondsRealtime(0.25f);
            GameplayState.CancelHitstop();follow.ClearShake();
            float before=health.CurrentHealth;
            var routine=(IEnumerator)boss.GetType().GetMethod("SlamRoutine",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(boss,null);
            boss.StartCoroutine(routine);
            yield return new WaitForSeconds(0.1f);
            GameplayState.RequestHitstop(0.065f);
            owner=new GameObject("BossFeedbackPauseProbe");
            GameplayState.EnterModal(owner);
            yield return new WaitForSecondsRealtime(0.75f);
            Assert.That(health.CurrentHealth,Is.EqualTo(before),"Paused boss windup cannot finish on an unscaled clock.");
            GameplayState.ExitModal(owner);
            yield return new WaitForSeconds(0.6f);
            Assert.That(health.CurrentHealth,Is.LessThan(before));
            Assert.That(follow.IsShaking,Is.True);
            Assert.That(Time.timeScale,Is.EqualTo(1));
        }

        private IEnumerator RenderSettings(string scene,bool enabled)
        {
            Transform panel=GameObject.Find("MenuDialogPanel").transform;
            Canvas canvas=panel.GetComponentInParent<Canvas>();
            Camera camera=Camera.main;
            RenderMode mode=canvas.renderMode; Camera oldCamera=canvas.worldCamera; int sorting=canvas.sortingOrder;
            RenderTexture target=null;
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1; canvas.sortingOrder=30000;
                foreach(Vector2Int size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,800),new Vector2Int(1024,768)})
                {
                    target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
                    yield return null;yield return null;
                    foreach(TMP_Text text in panel.GetComponentsInChildren<TMP_Text>())
                    {text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.name+": "+text.text);}
                    var corners=new Vector3[4];
                    foreach(Button button in panel.GetComponentsInChildren<Button>())
                    {
                        ((RectTransform)button.transform).GetWorldCorners(corners);
                        foreach(Vector3 point in corners) Assert.That(((RectTransform)canvas.transform).rect.Contains(canvas.transform.InverseTransformPoint(point)),Is.True,button.name);
                    }
                    camera.Render();RenderTexture previous=RenderTexture.active;
                    var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                    try
                    {
                        RenderTexture.active=target;image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
                        File.WriteAllBytes("Assets/Screenshots/PhanHoi_CaiDat_"+scene+"_"+(enabled?"Bat":"Tat")+"_"+size.x+"x"+size.y+"_Verified.png",image.EncodeToPNG());
                    }
                    finally{RenderTexture.active=previous;Object.Destroy(image);}
                    camera.targetTexture=null;target.Release();Object.Destroy(target);target=null;
                }
            }
            finally
            {
                camera.targetTexture=null;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.sortingOrder=sorting;
                if(target!=null){target.Release();Object.Destroy(target);}
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(owner!=null) GameplayState.ExitModal(owner);
            if(secondOwner!=null) GameplayState.ExitModal(secondOwner);
            if(driver!=null) driver.enabled=true;
            GameplayState.CancelHitstop();
            Scene scene=SceneManager.GetActiveScene();Scene clean=SceneManager.CreateScene("FeedbackClean");SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(scene);
            if(data!=null) Object.Destroy(data);
            if(state!=null) JsonUtility.FromJsonOverwrite(state,Progress);
            if(hadSave) PlayerPrefs.SetString(SaveKey,saved);else PlayerPrefs.DeleteKey(SaveKey);
            foreach(var pair in preferences)
                if(pair.Value.exists) PlayerPrefs.SetInt(pair.Key,pair.Value.value);else PlayerPrefs.DeleteKey(pair.Key);
            FeedbackPreferences.Reload();
        }
    }
}
