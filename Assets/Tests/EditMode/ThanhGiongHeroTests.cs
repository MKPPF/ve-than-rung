using System.Linq;
using NUnit.Framework;
using TopDownGame.Player;
using TopDownGame.Skills;
using TopDownGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.Tests.EditMode
{
    public sealed class ThanhGiongHeroTests
    {
        [Test]
        public void AllHeroesAreUnlockedDuringTestingPhase()
        {
            Assert.That(ThanhGiongHeroController.IsHeroUnlocked(HeroCharacterId.ForestWarrior), Is.True);
            Assert.That(ThanhGiongHeroController.IsHeroUnlocked(HeroCharacterId.ThanhGiong), Is.True);
            Assert.That(ThanhGiongHeroController.IsHeroUnlocked(HeroCharacterId.SonTinh), Is.True);
        }

        [Test]
        public void ThanhGiongVisualFactoryGeneratesMountedHeroWeaponsAndSkillIcons()
        {
            for (int stride = 0; stride < 4; stride++)
            {
                Assert.That(ThanhGiongVisualFactory.GetMountedHeroSprite(stride, false, false), Is.Not.Null);
                Assert.That(ThanhGiongVisualFactory.GetMountedHeroSprite(stride, true, false), Is.Not.Null);
                Assert.That(ThanhGiongVisualFactory.GetMountedHeroSprite(stride, false, true), Is.Not.Null);
            }

            Assert.That(ThanhGiongVisualFactory.GetIronSwordSprite(), Is.Not.Null);
            Assert.That(ThanhGiongVisualFactory.GetSingleBambooSprite(), Is.Not.Null);
            Assert.That(ThanhGiongVisualFactory.GetBambooBundleSprite(), Is.Not.Null);
            Assert.That(ThanhGiongVisualFactory.GetHeroPortraitSprite(), Is.Not.Null);
            for (int slot = 0; slot < 4; slot++)
            {
                Sprite icon = ThanhGiongVisualFactory.GetSkillIcon(slot);
                Assert.That(icon, Is.Not.Null, "Slot " + slot);
            }
        }

        [Test]
        public void SonTinhVisualFactoryGeneratesAllPosesBoulderMountainsAndSkillIcons()
        {
            foreach (SonTinhPose pose in System.Enum.GetValues(typeof(SonTinhPose)))
            {
                for (int stride = 0; stride < 4; stride++)
                {
                    Sprite sp = SonTinhVisualFactory.GetHeroSprite(stride, pose);
                    Assert.That(sp, Is.Not.Null, $"Pose {pose} frame {stride}");
                }
            }

            Assert.That(SonTinhVisualFactory.GetGiantBoulderSprite(), Is.Not.Null);
            for (int v = 0; v < 3; v++)
            {
                Assert.That(SonTinhVisualFactory.GetMountainPeakSprite(v), Is.Not.Null, "Mountain variant " + v);
            }
            Assert.That(SonTinhVisualFactory.GetEarthFissureSprite(), Is.Not.Null);
            Assert.That(SonTinhVisualFactory.GetRockShardSprite(), Is.Not.Null);
            Assert.That(SonTinhVisualFactory.GetShockwaveRingSprite(), Is.Not.Null);
            Assert.That(SonTinhVisualFactory.GetHeroPortraitSprite(), Is.Not.Null);

            for (int slot = 0; slot < 4; slot++)
            {
                Sprite icon = SonTinhVisualFactory.GetSkillIcon(slot);
                Assert.That(icon, Is.Not.Null, "Son Tinh Skill Slot " + slot);
            }
        }

        [Test]
        public void PlayerPrefabAndLobbySceneIncludeThanhGiongAndSonTinhHeroSystem()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Player/Player.prefab");
            Assert.That(playerPrefab, Is.Not.Null);
            ThanhGiongHeroController hero = playerPrefab.GetComponent<ThanhGiongHeroController>();
            Assert.That(hero, Is.Not.Null);

            string[] expectedThanhGiongNames =
            {
                "Kiếm Sắt Chiến Mã",
                "Tre Ngà Quật Địch",
                "Thần Mã Quật Tre 360°",
                "Thiết Mã Càn Lướt"
            };
            for (int i = 0; i < 4; i++)
            {
                SkillData skill = hero.GetThanhGiongSkillData(i);
                Assert.That(skill, Is.Not.Null);
                Assert.That(skill.DisplayName, Is.EqualTo(expectedThanhGiongNames[i]));
                Assert.That(skill.Icon, Is.Not.Null);
            }

            string[] expectedSonTinhNames =
            {
                "Ném Đại Thạch",
                "Dâng Núi Trấn Địch",
                "Đấm Đất Dựng Núi",
                "Bát Phương Quần Sơn"
            };
            for (int i = 0; i < 4; i++)
            {
                SkillData skill = hero.GetSonTinhSkillData(i);
                Assert.That(skill, Is.Not.Null);
                Assert.That(skill.DisplayName, Is.EqualTo(expectedSonTinhNames[i]));
                Assert.That(skill.Icon, Is.Not.Null);
            }

            Scene lobby = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity", OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = lobby.GetRootGameObjects();
                HeroSelectionStation station = roots
                    .SelectMany(r => r.GetComponentsInChildren<HeroSelectionStation>(true))
                    .SingleOrDefault();
                HeroSelectionUI selectionUI = roots
                    .SelectMany(r => r.GetComponentsInChildren<HeroSelectionUI>(true))
                    .SingleOrDefault();
                Assert.That(station, Is.Not.Null, "Lobby must contain HeroSelectionStation.");
                Assert.That(station.GetComponent<Collider2D>().isTrigger, Is.True);
                Assert.That(selectionUI, Is.Not.Null, "Lobby must contain HeroSelectionUI.");
            }
            finally
            {
                if (SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(lobby, true);
                }
            }
        }

        [Test]
        public void ForestWarriorVisualFactoryProvidesPortraitAndSkillIcons()
        {
            Sprite portrait = ForestWarriorVisualFactory.GetHeroPortraitSprite();
            Assert.That(portrait, Is.Not.Null, "Forest Warrior portrait sprite must not be null.");
            Assert.That(ThanhGiongHeroController.IsCustomHeroSprite(portrait), Is.False,
                "Forest Warrior portrait sprite must never be Thanh Giong or Son Tinh.");

            for (int slot = 0; slot < 4; slot++)
            {
                Sprite icon = ForestWarriorVisualFactory.GetSkillIcon(slot);
                Assert.That(icon, Is.Not.Null, "Forest Warrior skill icon at slot " + slot + " must not be null.");
            }
        }

        [Test]
        public void SkillEquipmentUIDisplaysCorrectFourSkillsForEachHero()
        {
            GameObject player = new GameObject("TestPlayer");
            GameObject canvasObj = new GameObject("TestCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            GameObject hud = new GameObject("HUD");
            hud.transform.SetParent(canvas.transform, false);

            try
            {
                player.tag = "Player";
                PlayerStats stats = player.AddComponent<PlayerStats>();
                stats.AddGold(1000);
                SkillInventory inventory = player.AddComponent<SkillInventory>();
                SkillManager manager = player.AddComponent<SkillManager>();

                string[] guids = AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/ScriptableObjects/Skills" });
                SkillData[] allSkills = guids.Select(g => AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                typeof(SkillManager).GetField("skillDatabase", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(manager, allSkills);

                ThanhGiongHeroController heroController = player.AddComponent<ThanhGiongHeroController>();
                SkillEquipmentUI ui = hud.AddComponent<SkillEquipmentUI>();

                var method = typeof(SkillEquipmentUI).GetMethod("ResolveSkillData",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);

                // 1. Forest Warrior
                heroController.SelectHero(HeroCharacterId.ForestWarrior, false);
                ui.Open();
                Assert.That(ui.IsOpen, Is.True);

                SkillData fwQ = (SkillData)method.Invoke(ui, new object[] { "Fireball", 0 });
                Assert.That(fwQ, Is.Not.Null);
                Assert.That(fwQ.DisplayName, Is.EqualTo("Cầu Lửa"));
                ui.Close();

                // 2. Thanh Giong
                heroController.SelectHero(HeroCharacterId.ThanhGiong, false);
                ui.Open();
                SkillData tgQ = (SkillData)method.Invoke(ui, new object[] { "ThanhGiong_0", 0 });
                SkillData tgE = (SkillData)method.Invoke(ui, new object[] { "ThanhGiong_1", 1 });
                SkillData tgR = (SkillData)method.Invoke(ui, new object[] { "ThanhGiong_2", 2 });
                SkillData tgF = (SkillData)method.Invoke(ui, new object[] { "ThanhGiong_3", 3 });

                Assert.That(tgQ, Is.Not.Null);
                Assert.That(tgQ.DisplayName, Is.EqualTo("Kiếm Sắt Chiến Mã"));
                Assert.That(tgE.DisplayName, Is.EqualTo("Tre Ngà Quật Địch"));
                Assert.That(tgR.DisplayName, Is.EqualTo("Thần Mã Quật Tre 360°"));
                Assert.That(tgF.DisplayName, Is.EqualTo("Thiết Mã Càn Lướt"));
                ui.Close();

                // 3. Son Tinh
                heroController.SelectHero(HeroCharacterId.SonTinh, false);
                ui.Open();
                SkillData stQ = (SkillData)method.Invoke(ui, new object[] { "SonTinh_0", 0 });
                SkillData stE = (SkillData)method.Invoke(ui, new object[] { "SonTinh_1", 1 });
                SkillData stR = (SkillData)method.Invoke(ui, new object[] { "SonTinh_2", 2 });
                SkillData stF = (SkillData)method.Invoke(ui, new object[] { "SonTinh_3", 3 });

                Assert.That(stQ, Is.Not.Null);
                Assert.That(stQ.DisplayName, Is.EqualTo("Ném Đại Thạch"));
                Assert.That(stE.DisplayName, Is.EqualTo("Dâng Núi Trấn Địch"));
                Assert.That(stR.DisplayName, Is.EqualTo("Đấm Đất Dựng Núi"));
                Assert.That(stF.DisplayName, Is.EqualTo("Bát Phương Quần Sơn"));
                ui.Close();
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(canvasObj);
            }
        }
    }
}
