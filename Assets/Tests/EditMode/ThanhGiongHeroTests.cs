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
    }
}
