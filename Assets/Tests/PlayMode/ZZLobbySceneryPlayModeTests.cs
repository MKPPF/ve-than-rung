#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Equipment;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Checks the generated town using the real player/colliders/UI and an isolated Test Runner save.</summary>
    public sealed class ZZLobbySceneryPlayModeTests
    {
        private Scene previous, lobby;
        private GameProgressManager progress;
        private string originalProgress, originalSave;
        private bool hadSave, applied;
        private float oldCapture;
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [UnityTest]
        public IEnumerator WoodlandLobbyRoutesReachEveryStationAndCameraNeverLeavesArtwork()
        {
            oldCapture = Time.captureDeltaTime;
            Assert.That((bool)typeof(GameProgressManager).GetProperty("IsAutomatedTestRun",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), Is.True);
            if (GameProgressManager.Instance == null)
                new GameObject("LobbyTestProgress").AddComponent<GameProgressManager>();
            progress = GameProgressManager.Instance;
            originalProgress = JsonUtility.ToJson(progress);
            hadSave = PlayerPrefs.HasKey("TopDownGame.Tests.Progress.v1");
            originalSave = PlayerPrefs.GetString("TopDownGame.Tests.Progress.v1", "");
            applied = (bool)typeof(GameProgressManager).GetField("playerProgressApplied", Private).GetValue(progress);
            typeof(GameProgressManager).GetField("hasSafeRespawn", Private).SetValue(progress, false);
            previous = SceneManager.GetActiveScene();
            yield return SceneManager.LoadSceneAsync("Lobby", LoadSceneMode.Additive);
            lobby = SceneManager.GetSceneByName("Lobby");
            SceneManager.SetActiveScene(lobby);
            yield return null;
            var roots = lobby.GetRootGameObjects();
            // Keep the automatic portal armed but slow during scenery captures;
            // dedicated portal tests cover charge/cancel and scene travel.
            typeof(LobbyPortal).GetField("chargeDuration",Private).SetValue(
                roots.Single(r=>r.name=="BattlePortal").GetComponent<LobbyPortal>(),1000f);
            GameObject player = roots.Single(r => r.CompareTag("Player"));
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            var interaction = player.GetComponent<PlayerInteraction>();
            Camera camera = roots.Single(r => r.name == "Main Camera").GetComponent<Camera>();
            CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
            Canvas canvas = roots.Single(r => r.name == "GameplayCanvas").GetComponent<Canvas>();
            Assert.That(canvas.GetComponentInChildren<LobbyHUD>(), Is.Not.Null);
            Assert.That(canvas.GetComponentInChildren<GameplayHUD>(), Is.Null,
                "The hub must not show oversized combat skill cards.");
            Assert.That(player.transform.position.x, Is.EqualTo(-5.5f).Within(0.05));
            yield return Capture(camera, canvas, "SanhChoV3_Spawn", 1920, 1080, false);
            Assert.That(player.GetComponent<PlayerCombat>().ClickToShoot, Is.False);
            follow.enabled = false;
            camera.transform.position = new Vector3(0,0,-10);
            camera.orthographicSize = 7.8f;
            yield return Capture(camera, canvas, "SanhChoV3_ToanCanh", 1920, 1152, false);
            yield return VerifyLivingEffects(roots, camera, canvas);
            yield return VerifyLowerGarden(roots,camera,canvas);
            yield return VerifyProgressionScreens(roots,camera,canvas);
            yield return VerifyVisitors(roots,camera,canvas);
            camera.orthographicSize = 5.6f;
            follow.enabled = true;

            foreach (string stationName in new[] { "WeaponMerchant", "StorageChest", "BattlePortal" })
            {
                GameObject station = roots.Single(r => r.name == stationName);
                Vector2 destination = station.transform.position;
                Assert.That(movement.TrySetClickDestination(destination), Is.True, stationName + " click path");
                float deadline = Time.realtimeSinceStartup + 10;
                while (Vector2.Distance(player.transform.position, destination) > 0.35f && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(Vector2.Distance(player.transform.position, destination), Is.LessThan(0.5f), stationName + " reachable");
                movement.CancelMovement();
                if (stationName == "WeaponMerchant")
                {
                    station.GetComponent<WeaponShopNpc>().Interact(interaction);
                    WeaponShopUI shop = canvas.GetComponentInChildren<WeaponShopUI>();
                    Assert.That(shop.IsOpen, Is.True);
                    shop.Close();
                }
                else if (stationName == "StorageChest")
                {
                    station.GetComponent<StorageChest>().Interact(interaction);
                    StorageChestUI storage = canvas.GetComponentInChildren<StorageChestUI>();
                    Assert.That(storage.IsOpen, Is.True);
                    storage.Close();
                }
                else
                {
                    Assert.That(station.GetComponent<LobbyPortal>().CanInteract, Is.True);
                    Assert.That(typeof(LobbyPortal).GetField("destinationScene", Private).GetValue(station.GetComponent<LobbyPortal>()), Is.EqualTo("Map01"));
                    typeof(LobbyPortal).GetField("chargeTime",Private).SetValue(station.GetComponent<LobbyPortal>(),650f);
                    yield return Capture(camera,canvas,"TruyenTongTran_Charging",1920,1080,false);
                    yield return VerifyAlignedCircle(station, camera, canvas);
                    foreach(var renderer in station.transform.Find("TeleportRunes").GetComponentsInChildren<Renderer>())
                        Assert.That(renderer.sortingOrder,Is.LessThan(0),"Portal light must stay beneath the player.");
                    var portal=station.GetComponent<LobbyPortal>();
                    typeof(LobbyPortal).GetField("chargeTime",Private).SetValue(portal,1001f);
                    yield return null;
                    Assert.That(portal.IsChoosing,Is.True);
                    yield return Capture(camera,station.GetComponentInChildren<Canvas>(),"TruyenTongTran_Choice",1920,1080,true);
                    portal.CancelChoice();
                    yield return VerifyDeparture(station, player, camera, canvas);
                    typeof(LobbyPortal).GetField("chargeTime",Private).SetValue(station.GetComponent<LobbyPortal>(),0f);
                }
                yield return null;
                yield return Capture(camera, canvas, "SanhChoV3_" + stationName, 1920,1080,false);
            }
            foreach (Vector2Int resolution in new[] { new Vector2Int(1024,768), new Vector2Int(1920,1200), new Vector2Int(2560,1080) })
            {
                yield return Capture(camera, canvas, "SanhChoV3_" + resolution.x + "x" + resolution.y,
                    resolution.x, resolution.y, false);
            }
            yield return Capture(camera, canvas, "SanhChoV3_GameplayHUD", 1920,1080,true);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            movement.CancelMovement();
            body.position = new Vector2(-1.15f, 1.1f);
            Physics2D.SyncTransforms();
            for (int i = 0; i < 15; i++)
            {
                movement.MoveKinematically(Vector2.up * 0.2f);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(body.position.y, Is.LessThan(2.5f),
                "The forge facade must block physical movement, not just click destinations.");
            LobbyWaterFlow waterEffects=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyWaterFlow>()).Single();
            LobbyFireEffects fireEffects=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyFireEffects>()).Single();
            Mesh waterMesh=waterEffects.transform.Find("LivingWater/FlowingStream").GetComponent<MeshFilter>().sharedMesh;
            Material waterMaterial=waterEffects.transform.Find("LivingWater/FlowingStream").GetComponent<MeshRenderer>().sharedMaterial;
            Object.Destroy(waterEffects);
            Object.Destroy(fireEffects);
            LobbyWildlife wildlifeEffects=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyWildlife>()).Single();
            Object.Destroy(wildlifeEffects);
            yield return null;
            yield return null;
            Assert.That(waterMesh==null,Is.True,"Generated water mesh must be released.");
            Assert.That(waterMaterial==null,Is.True,"Generated water material must be released.");
            Assert.That(roots.Single(r=>r.name=="LobbyDecor_Generated").transform.Find("LivingFires"),Is.Null);
        }

        private static IEnumerator VerifyAlignedCircle(GameObject station, Camera camera, Canvas canvas)
        {
            yield return new WaitForSeconds(0.2f); // Let the last walking pose settle before the close-up.
            var visual = station.GetComponent<TeleportCircleVisual>();
            Transform root = station.transform.Find("TeleportRunes");
            Assert.That(visual.VisualCount, Is.EqualTo(25));
            Assert.That(root.GetComponentsInChildren<Collider2D>(), Is.Empty);
            var ring = root.Find("EngravedRing_0").GetComponent<LineRenderer>();
            Assert.That(ring.loop, Is.True);
            var rim = root.Find("EngravedRing_2").GetComponent<LineRenderer>();
            Assert.That(rim.loop, Is.True, "The outer circle must surround the entire dais.");
            for (int i = 0; i < ring.positionCount; i++)
            {
                Vector3 point = ring.GetPosition(i);
                float ellipse = Mathf.Pow(point.x / visual.EngravedRadii.x, 2) +
                    Mathf.Pow(point.y / visual.EngravedRadii.y, 2);
                Assert.That(ellipse, Is.EqualTo(1f).Within(0.0001f));
            }
            float beforePause = visual.AnimationTime;
            Time.timeScale = 0;
            yield return null;
            yield return null;
            Assert.That(visual.AnimationTime, Is.EqualTo(beforePause));
            Time.timeScale = 1;
            var follow = camera.GetComponent<CameraFollow2D>();
            Vector3 oldPosition = camera.transform.position;
            float oldSize = camera.orthographicSize;
            follow.enabled = false;
            camera.transform.position = station.transform.position + new Vector3(0, 0.3f, -10);
            camera.orthographicSize = 2.7f;
            try
            {
                yield return Capture(camera, canvas, "TruyenTongTranV8_CanKhop", 1100, 900, false);
            }
            finally
            {
                camera.transform.position = oldPosition;
                camera.orthographicSize = oldSize;
                follow.enabled = true;
            }
            visual.enabled = false;
            Assert.That(root.gameObject.activeSelf, Is.False);
            visual.enabled = true;
            Assert.That(visual.VisualCount, Is.EqualTo(25));
        }

        private static IEnumerator VerifyDeparture(GameObject station, GameObject player, Camera camera, Canvas canvas)
        {
            var portal = station.GetComponent<LobbyPortal>();
            var visual = station.GetComponent<TeleportDepartureVisual>();
            var follow = camera.GetComponent<CameraFollow2D>();
            float initialSize = camera.orthographicSize;
            portal.Interact(player.GetComponent<PlayerInteraction>());
            yield return null;
            Assert.That(portal.IsChoosing, Is.True);
            Assert.That(portal.ChooseDestination("Map01"), Is.True);
            Assert.That(portal.IsChoosing, Is.False);
            Assert.That(portal.IsTransporting, Is.True);
            Assert.That(GameplayState.IsInputBlocked, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Ceremony blocks input without pausing itself.");
            Assert.That(portal.ChooseDestination("Map01"), Is.False);
            float deadline = Time.realtimeSinceStartup + 8f;
            while (portal.DepartureProgress < 0.65f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(portal.DepartureProgress, Is.InRange(0.65f, 0.95f));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"), "Must show the rays before loading.");
            Assert.That(visual.IsVisible, Is.True);
            Assert.That(visual.HasBeamMaterial, Is.True);
            var sign = station.scene.GetRootGameObjects().Single(r => r.name == "PortalLabel");
            Assert.That(sign.GetComponent<Renderer>().enabled, Is.False, "Station text must not float across the cinematic rays.");
            Assert.That(visual.VisualCount, Is.EqualTo(37));
            var particles=station.GetComponentsInChildren<SpriteRenderer>()
                .Where(r=>r.name.StartsWith("AscendingSpark_")).OrderBy(r=>r.name).ToArray();
            var particleX=particles.Select(r=>r.transform.localPosition.x).ToArray();
            yield return null;
            for(int i=0;i<particles.Length;i++)
                Assert.That(particles[i].transform.localPosition.x,Is.EqualTo(particleX[i]).Within(0.00001f),
                    "Ascending particles must not spiral or converge horizontally.");
            Assert.That(camera.orthographicSize, Is.GreaterThan(initialSize * 1.1f));
            float pausedProgress = portal.DepartureProgress;
            GameplayState.EnterModal(canvas);
            yield return null; yield return null;
            Assert.That(portal.DepartureProgress, Is.EqualTo(pausedProgress));
            yield return Capture(camera, canvas, "TruyenTongTranV9_LuiMay_TiaSang", 1920, 1080, false);
            // The close-up is supplementary; the previous capture verifies the real zoom and bounds.
            follow.enabled = false;
            Vector3 oldPosition = camera.transform.position;
            float oldSize = camera.orthographicSize;
            camera.transform.position = station.transform.position + new Vector3(0, 1.7f, -10);
            camera.orthographicSize = 4.1f;
            yield return Capture(camera, canvas, "TruyenTongTranV9_CanCanh_TiaSang", 1100, 1100, false);
            camera.transform.position = oldPosition;
            camera.orthographicSize = oldSize;
            follow.enabled = true;
            GameplayState.ExitModal(canvas);
            portal.enabled = false; // Abort before travel so the remaining scenery checks keep their scene.
            yield return null;
            Assert.That(visual.IsVisible, Is.False);
            Assert.That(follow.IsCinematic, Is.False);
            Assert.That(camera.orthographicSize, Is.EqualTo(initialSize).Within(0.02f));
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            portal.enabled = true;
        }

        private static IEnumerator VerifyLowerGarden(GameObject[] roots, Camera camera, Canvas canvas)
        {
            yield return VerifyLowerGardenContent(roots,camera,canvas);
        }

        private static IEnumerator VerifyVisitors(GameObject[] roots,Camera camera,Canvas canvas)
        {
            var visitors=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyVisitor>()).ToArray();
            Assert.That(visitors.Length,Is.EqualTo(3));
            foreach(var visitor in visitors)
            {
                Assert.That(visitor.GetComponentsInChildren<Collider2D>(),Is.Empty);
                Assert.That(visitor.GetComponent<PlayerStats>(),Is.Null);
                Assert.That(visitor.RoutePointCount,Is.EqualTo(4));
            }
            var positions=visitors.Select(v=>v.transform.position).ToArray();
            Time.timeScale=0;yield return null;yield return null;
            for(int i=0;i<visitors.Length;i++)Assert.That(visitors[i].transform.position,Is.EqualTo(positions[i]));
            Time.timeScale=1;
            float oldCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/30f;
            var originalPosition=camera.transform.position;float originalSize=camera.orthographicSize;
            float[] before=visitors.Select(v=>v.DistanceTravelled).ToArray();
            Directory.CreateDirectory("Captures/LobbyVisitorsV1");
            var movieTarget=new RenderTexture(960,540,24);
            var previousTarget=camera.targetTexture;
            try
            {
                camera.transform.position=new Vector3(0.8f,-0.1f,-10);camera.orthographicSize=3.4f;
                for(int frame=0;frame<330;frame++)
                {
                    yield return null;
                    foreach(var visitor in visitors)
                    {
                        var pos=visitor.transform.position;
                        Assert.That(pos.x,Is.InRange(-1.7f,4.6f));Assert.That(pos.y,Is.InRange(-1.9f,1.2f));
                        Assert.That(visitor.Velocity.magnitude,Is.LessThan(1));
                        foreach(var blocker in roots.SelectMany(r=>r.GetComponentsInChildren<BoxCollider2D>()).Where(c=>!c.isTrigger))
                            Assert.That(Vector2.Distance(blocker.ClosestPoint(pos),pos),Is.GreaterThan(0.25f),visitor.name+" near "+blocker.name);
                    }
                    if(frame==30 || frame==300)yield return Capture(camera,canvas,"Lobby_VisitorsV1_"+frame,1920,1080,false);
                    if(frame%2==0)
                    {
                        camera.targetTexture=movieTarget;
                        SaveFrame(camera,movieTarget,"Captures/LobbyVisitorsV1/Frame_"+(frame/2).ToString("000")+".png");
                        camera.targetTexture=previousTarget;
                    }
                }
                for(int i=0;i<visitors.Length;i++)Assert.That(visitors[i].DistanceTravelled,Is.GreaterThan(before[i]+0.5f));
            }
            finally {camera.targetTexture=previousTarget;movieTarget.Release();Object.Destroy(movieTarget);Time.captureDeltaTime=oldCapture;camera.transform.position=originalPosition;camera.orthographicSize=originalSize;}
        }

        private static IEnumerator VerifyProgressionScreens(GameObject[] roots,Camera camera,Canvas canvas)
        {
            var player=roots.Single(r=>r.CompareTag("Player"));
            var stats=player.GetComponent<PlayerStats>();
            var skills=player.GetComponent<TopDownGame.Skills.SkillInventory>();
            var manager=player.GetComponent<TopDownGame.Skills.SkillManager>();
            var potential=canvas.GetComponentInChildren<PotentialUpgradeUI>();
            var equipment=canvas.GetComponentInChildren<SkillEquipmentUI>();
            Assert.That(potential,Is.Not.Null);Assert.That(equipment,Is.Not.Null);
            stats.LoadPotentialProgress(2,0,0,0,0);
            potential.Open();yield return null;
            Assert.That(potential.IsOpen,Is.True);Assert.That(Time.timeScale,Is.Zero);
            equipment.Open();Assert.That(equipment.IsOpen,Is.False);
            var potentialPanel=canvas.transform.Find("PotentialPanel");
            var spend=potentialPanel.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="Spend0");
            float health=stats.MaxHealth;spend.onClick.Invoke();
            Assert.That(stats.VitalityPoints,Is.EqualTo(1));
            Assert.That(stats.MaxHealth,Is.EqualTo(health+PlayerStats.VitalityHealthPerPoint));
            Assert.That(GameProgressManager.Instance.PotentialPoints,Is.EqualTo(1));
            spend.onClick.Invoke();Assert.That(spend.interactable,Is.False);
            spend.onClick.Invoke();Assert.That(stats.VitalityPoints,Is.EqualTo(2));
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(1024,768)})
                yield return Capture(camera,canvas,"PotentialUIV2_"+resolution.x+"x"+resolution.y,resolution.x,resolution.y,true);
            potential.Close();Assert.That(Time.timeScale,Is.EqualTo(1));
            stats.AddGold(10000);
            equipment.Open();yield return null;
            var skillPanel=canvas.transform.Find("SkillEquipmentPanel");
            var buttons=skillPanel.GetComponentsInChildren<UnityEngine.UI.Button>();
            var card=buttons.First(b=>b.name.StartsWith("Skill") && b.name!="SkillLauncher");card.onClick.Invoke();
            Assert.That(skillPanel.GetComponentsInChildren<UnityEngine.UI.Image>().Count(i=>i.name=="Icon" && i.sprite!=null),Is.GreaterThanOrEqualTo(2));
            var selected=typeof(SkillEquipmentUI).GetField("selectedSkill",Private);
            var owned=(System.Collections.Generic.List<string>)typeof(SkillEquipmentUI).GetField("owned",Private).GetValue(equipment);
            string id=owned[(int)selected.GetValue(equipment)];
            var upgrade=buttons.Single(b=>b.name=="Upgrade");
            int oldLevel=skills.GetLevel(id),oldGold=stats.Gold;
            if(oldLevel<manager.GetSkillData(id).MaxLevel)
            {
                upgrade.onClick.Invoke();Assert.That(skills.GetLevel(id),Is.EqualTo(oldLevel+1));
                Assert.That(stats.Gold,Is.EqualTo(oldGold-TopDownGame.Skills.SkillInventory.UpgradeGoldCost(oldLevel)));
            }
            buttons.Single(b=>b.name=="Slot3").onClick.Invoke();
            buttons.Single(b=>b.name=="Remove").onClick.Invoke();Assert.That(skills.Slots[3].IsEmpty,Is.True);
            for(int i=0;i<3;i++)if(skills.Slots[i].skillId==id)skills.RemoveFromSlot(i);
            buttons.Single(b=>b.name=="Equip").onClick.Invoke();Assert.That(skills.Slots[3].skillId,Is.EqualTo(id));
            Assert.That(skillPanel.GetComponentsInChildren<UnityEngine.UI.Image>().Any(i=>i.name=="SkillIcon"&&i.sprite!=null),Is.True);
            buttons.Single(b=>b.name=="Slot0").onClick.Invoke();
            Assert.That(buttons.Single(b=>b.name=="Equip").interactable,Is.False,"Do not equip a duplicate skill.");
            buttons.Single(b=>b.name=="Slot3").onClick.Invoke();
            foreach(TopDownGame.Skills.SkillId skillId in System.Enum.GetValues(typeof(TopDownGame.Skills.SkillId)))
                if(!skills.Owns(skillId.ToString()))skills.AddOrUpgrade(skillId.ToString(),manager.GetSkillData(skillId.ToString()).MaxLevel);
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(1024,768)})
                yield return Capture(camera,canvas,"SkillsUIV2_"+resolution.x+"x"+resolution.y,resolution.x,resolution.y,true);
            int reserve=stats.Gold;stats.TrySpendGold(reserve);
            oldLevel=skills.GetLevel(id);Assert.That(upgrade.interactable,Is.False);
            upgrade.onClick.Invoke();Assert.That(skills.GetLevel(id),Is.EqualTo(oldLevel));
            stats.AddGold(reserve);
            while(skills.TryUpgrade(id,manager.GetSkillData(id).MaxLevel)){}
            Assert.That(upgrade.interactable,Is.False);oldGold=stats.Gold;
            upgrade.onClick.Invoke();Assert.That(stats.Gold,Is.EqualTo(oldGold));
            equipment.Close();Assert.That(Time.timeScale,Is.EqualTo(1));
            potential.Open();potential.enabled=false;Assert.That(Time.timeScale,Is.EqualTo(1));potential.enabled=true;
            equipment.Open();equipment.enabled=false;Assert.That(Time.timeScale,Is.EqualTo(1));equipment.enabled=true;
        }

        private static IEnumerator VerifyLowerGardenContent(GameObject[] roots, Camera camera, Canvas canvas)
        {
            var garden=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyGardenWildlife>()).Single();
            Assert.That(garden.ButterflyCount,Is.EqualTo(6));
            Assert.That(garden.BirdCount,Is.EqualTo(3));
            var root=garden.transform.Find("LowerGardenWildlife");
            Assert.That(root.GetComponentsInChildren<Collider2D>(),Is.Empty);
            var objects=root.GetComponentsInChildren<Transform>(true);
            var positions=objects.Select(t=>t.localPosition).ToArray();
            var rotations=objects.Select(t=>t.localRotation).ToArray();
            float time=garden.AnimationTime;
            Time.timeScale=0;
            yield return null;yield return null;
            Assert.That(garden.AnimationTime,Is.EqualTo(time));
            for(int i=0;i<objects.Length;i++)
            {
                Assert.That(objects[i].localPosition,Is.EqualTo(positions[i]));
                Assert.That(objects[i].localRotation,Is.EqualTo(rotations[i]));
            }
            Time.timeScale=1;
            var colors=root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name=="UpperColor").Select(r=>r.color).Distinct().ToArray();
            Assert.That(colors.Length,Is.EqualTo(3));
            var meshes=root.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh).ToArray();
            var before=camera.transform.position;float size=camera.orthographicSize;
            camera.transform.position=new Vector3(-0.9f,-4.8f,-10);camera.orthographicSize=2.6f;
            var field=typeof(LobbyGardenWildlife).GetField("elapsed",Private);
            // Deterministic presentation samples without advancing or modifying player saves.
            field.SetValue(garden,0f);yield return null;
            Assert.That(root.Find("GardenBird_0/Feet").gameObject.activeSelf,Is.True);
            Assert.That(root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Toe"),Is.EqualTo(18));
            yield return Capture(camera,canvas,"Lobby_GardenWildlife_Perched",1920,1080,false);
            field.SetValue(garden,12.5f);yield return null;
            Assert.That(garden.IsFlying(0),Is.True);
            Assert.That(root.Find("GardenBird_0/Feet").gameObject.activeSelf,Is.False);
            yield return Capture(camera,canvas,"Lobby_GardenWildlife_Flying",1920,1080,false);
            foreach(var item in objects)
                Assert.That(float.IsNaN(item.position.x)||float.IsInfinity(item.position.y),Is.False);
            Assert.That(root.GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(objects.Length));
            garden.enabled=false;Assert.That(root.gameObject.activeSelf,Is.False);
            float stopped=garden.AnimationTime;yield return null;Assert.That(garden.AnimationTime,Is.EqualTo(stopped));
            garden.enabled=true;yield return null;Assert.That(root.gameObject.activeSelf,Is.True);
            Assert.That(root.GetComponentsInChildren<Transform>(true).Length,Is.EqualTo(objects.Length));
            camera.transform.position=before;camera.orthographicSize=size;
            Object.Destroy(garden);yield return null;yield return null;
            Assert.That(root==null,Is.True);
            foreach(var mesh in meshes)Assert.That(mesh==null,Is.True,"Owned mesh was not cleaned up.");
        }

        private static IEnumerator VerifyLivingEffects(GameObject[] roots, Camera camera, Canvas canvas)
        {
            LobbyWaterFlow water=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyWaterFlow>()).Single();
            LobbyFireEffects fire=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyFireEffects>()).Single();
            LobbyWildlife wildlife=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyWildlife>()).Single();
            Assert.That(wildlife.FishCount,Is.EqualTo(10));
            var fishDetails=wildlife.transform.Find("LivingWildlife").GetComponentsInChildren<Transform>()
                .Where(t=>t.name=="FishDetails").ToArray();
            Assert.That(fishDetails.Length,Is.EqualTo(10));
            foreach(var detail in fishDetails)
            {
                Assert.That(detail.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name=="PectoralFin"),Is.EqualTo(2));
                Assert.That(detail.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name=="Gill"),Is.EqualTo(2));
                Assert.That(detail.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name=="Pupil"),Is.EqualTo(2));
            }
            Assert.That(wildlife.ButterflyCount,Is.EqualTo(6));
            int wildlifeCount=wildlife.VisualCount;
            float wildlifeStart=wildlife.AnimationTime;
            Assert.That(water.IsReady,Is.True);
            var floor=roots.SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>()).Single(r=>r.name=="Floor");
            Assert.That(floor.sprite.texture.width,Is.EqualTo(6476));
            var waterSurfaces=water.GetComponentsInChildren<MeshRenderer>()
                .Where(r=>r.name=="FlowingStream" || r.name=="FallingWater").ToArray();
            Assert.That(waterSurfaces.Length,Is.EqualTo(2));
            foreach(var surface in waterSurfaces)
                Assert.That(surface.sharedMaterial.mainTexture,Is.SameAs(floor.sprite.texture),
                    "Animated water must sample the restored painting, not the old low-resolution source.");
            var priorPosition=camera.transform.position;
            float priorSize=camera.orthographicSize;
            camera.transform.position=new Vector3(-7.2f,-0.8f,-10);
            camera.orthographicSize=3.3f;
            yield return Capture(camera,canvas,"Lobby_RestoredV1_Ho",1920,1080,false);
            camera.transform.position=priorPosition;
            camera.orthographicSize=priorSize;
            wildlifeStart=wildlife.AnimationTime;
            int waterCount=water.VisualCount;
            int fireCount=fire.VisualCount;
            Assert.That(waterCount,Is.InRange(20,40));
            Assert.That(fireCount,Is.InRange(20,60));
            var fishBodies=wildlife.GetComponentsInChildren<SpriteRenderer>()
                .Where(r=>r.name=="Body" && r.transform.parent.name.StartsWith("PondFish_")).ToArray();
            Assert.That(fishBodies.Length,Is.EqualTo(10));
            Assert.That(fishBodies.Select(r=>new Vector3(r.color.r,r.color.g,r.color.b)).Distinct().Count(),Is.EqualTo(5));
            Assert.That(fishBodies.All(r=>r.transform.parent.localScale.x>=1.2f),Is.True);
            Assert.That(water.GetComponentsInChildren<Collider2D>(),Is.Empty);
            float waterStart=water.AnimationTime;
            float fireStart=fire.AnimationTime;
            Time.timeScale=0;
            yield return null;
            yield return null;
            Assert.That(water.AnimationTime,Is.EqualTo(waterStart));
            Assert.That(fire.AnimationTime,Is.EqualTo(fireStart));
            Assert.That(wildlife.AnimationTime,Is.EqualTo(wildlifeStart));
            Time.timeScale=1;
            bool wasUI=canvas.enabled;
            canvas.enabled=false;
            float priorCapture=Time.captureDeltaTime;
            Time.captureDeltaTime=1f/30f;
            string directory="Captures/LobbyLivingV4";
            Directory.CreateDirectory(directory);
            var target=new RenderTexture(640,480,24);
            RenderTexture priorTarget=camera.targetTexture;
            camera.targetTexture=target;
            Color32[] firstWater=null;
            try
            {
                for(int frame=0;frame<150;frame++)
                {
                    yield return null;
                    camera.orthographicSize=1.85f;
                    camera.transform.position=new Vector3(-10.4f,-1.9f,-10);
                    Color32[] pixels=SaveFrame(camera,target,directory+"/Water_"+frame.ToString("000")+".png");
                    if(frame==0) firstWater=pixels;
                    if(frame==90)
                    {
                        int changed=0;
                        for(int i=0;i<pixels.Length;i++)
                            if(Mathf.Abs(pixels[i].r-firstWater[i].r)+Mathf.Abs(pixels[i].g-firstWater[i].g)+Mathf.Abs(pixels[i].b-firstWater[i].b)>12) changed++;
                        Assert.That(changed,Is.GreaterThan(400),"Rendered water must visibly move, not just advance a timer.");
                    }
                    camera.transform.position=new Vector3(-0.4f,3.6f,-10);
                    SaveFrame(camera,target,directory+"/Forge_"+frame.ToString("000")+".png");
                    camera.transform.position=new Vector3(-7.5f,-4.9f,-10);
                    SaveFrame(camera,target,directory+"/Camp_"+frame.ToString("000")+".png");
                    camera.transform.position=new Vector3(2.7f,2.55f,-10);
                    SaveFrame(camera,target,directory+"/Garden_"+frame.ToString("000")+".png");
                    Assert.That(wildlife.FishStayInPond,Is.True,"Fish must never swim over the banks.");
                }
                Assert.That(water.VisualCount,Is.EqualTo(waterCount));
                Assert.That(fire.VisualCount,Is.EqualTo(fireCount));
                Assert.That(water.AnimationTime,Is.GreaterThan(waterStart));
                Assert.That(wildlife.VisualCount,Is.EqualTo(wildlifeCount));
                Assert.That(wildlife.AnimationTime,Is.GreaterThan(wildlifeStart));
                Assert.That(wildlife.JumpCount,Is.GreaterThan(0));
                Transform wildlifeRoot=wildlife.transform.Find("LivingWildlife");
                wildlife.enabled=false;
                Assert.That(wildlifeRoot.gameObject.activeSelf,Is.False);
                wildlife.enabled=true;
                Assert.That(wildlife.VisualCount,Is.EqualTo(wildlifeCount));
                Transform waterRoot=water.transform.Find("LivingWater");
                Transform fireRoot=fire.transform.Find("LivingFires");
                water.enabled=false;
                fire.enabled=false;
                Assert.That(waterRoot.gameObject.activeSelf,Is.False);
                Assert.That(fireRoot.gameObject.activeSelf,Is.False);
                water.enabled=true;
                fire.enabled=true;
                Assert.That(water.VisualCount,Is.EqualTo(waterCount));
                Assert.That(fire.VisualCount,Is.EqualTo(fireCount));
            }
            finally
            {
                camera.targetTexture=priorTarget;
                target.Release();
                Object.Destroy(target);
                Time.captureDeltaTime=priorCapture;
                canvas.enabled=wasUI;
            }
        }

        private static Color32[] SaveFrame(Camera camera,RenderTexture target,string path)
        {
            camera.Render();
            RenderTexture prior=RenderTexture.active;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                image.Apply();
                Color32[] pixels=image.GetPixels32();
                File.WriteAllBytes(path,image.EncodeToPNG());
                return pixels;
            }
            finally { RenderTexture.active=prior; Object.Destroy(image); }
        }

        private static IEnumerator Capture(Camera camera, Canvas canvas, string name, int width, int height, bool showUI)
        {
            bool canvasEnabled = canvas.enabled;
            var mode = canvas.renderMode;
            Camera originalCanvasCamera = canvas.worldCamera;
            float planeDistance = canvas.planeDistance;
            var target = new RenderTexture(width,height,24);
            RenderTexture oldTarget = camera.targetTexture;
            camera.targetTexture = target;
            canvas.enabled = showUI;
            if (showUI) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previousActive = RenderTexture.active;
            Texture2D image = new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,width,height),0,0);
                image.Apply();
                Directory.CreateDirectory("Assets/Screenshots");
                File.WriteAllBytes("Assets/Screenshots/" + name + "_Verified.png", image.EncodeToPNG());
                CameraFollow2D follow = camera.GetComponent<CameraFollow2D>();
                if (follow.enabled)
                {
                    float xExtent = camera.orthographicSize * camera.aspect;
                    Assert.That(camera.transform.position.x - xExtent, Is.GreaterThanOrEqualTo(-13.01));
                    Assert.That(camera.transform.position.x + xExtent, Is.LessThanOrEqualTo(13.01));
                    Assert.That(camera.transform.position.y - camera.orthographicSize, Is.GreaterThanOrEqualTo(-7.81));
                    Assert.That(camera.transform.position.y + camera.orthographicSize, Is.LessThanOrEqualTo(7.81));
                }
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = oldTarget;
                canvas.renderMode = mode;
                canvas.worldCamera = originalCanvasCamera;
                canvas.planeDistance = planeDistance;
                canvas.enabled = canvasEnabled;
                target.Release();
                Object.Destroy(target);
                Object.Destroy(image);
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.captureDeltaTime=oldCapture;
            Time.timeScale = 1;
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (lobby.IsValid() && lobby.isLoaded) yield return SceneManager.UnloadSceneAsync(lobby);
            if (progress != null && originalProgress != null)
            {
                JsonUtility.FromJsonOverwrite(originalProgress, progress);
                typeof(GameProgressManager).GetField("playerProgressApplied", Private).SetValue(progress, applied);
                if (hadSave) PlayerPrefs.SetString("TopDownGame.Tests.Progress.v1", originalSave);
                else PlayerPrefs.DeleteKey("TopDownGame.Tests.Progress.v1");
            }
            yield return null;
        }
    }
}
#endif
