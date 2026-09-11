using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TopDownGame.EditorTools;
using TopDownGame.Enemies;
using TopDownGame.Equipment;
using TopDownGame.Player;
using TopDownGame.Skills;
using TopDownGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.Tests.EditMode
{
    public sealed class CatalogSmokeTests
    {
        private static readonly string[] RequiredScenes =
        {
            "MainMenu", "Lobby", "Map01", "Map02", "Map03", "BossMap01",
            "CaveMap01", "RiftMap01"
        };

        private static readonly string[] GeneratedDecorProps =
        {
            "ForestTreeClusterV2",
            "MossyRockClusterV2",
            "ForgeSupplyClusterV3",
            "CaveCrystalClusterV1",
            "AncientForestShrineV1",
            "FallenLogSupplyClusterV1",
            "ForestPondRuinClusterV1",
            "CavePoolMineClusterV1",
            "ForestRelicPickupV1"
        };

        private readonly struct MazeSceneExpectation
        {
            public MazeSceneExpectation(string landmarkAssetName,
                Vector2 playerSpawn, Vector2 exitPosition,
                Vector2 chestPosition, Vector2 itemPosition)
            {
                LandmarkAssetName = landmarkAssetName;
                PlayerSpawn = playerSpawn;
                ExitPosition = exitPosition;
                ChestPosition = chestPosition;
                ItemPosition = itemPosition;
            }

            public string LandmarkAssetName { get; }
            public Vector2 PlayerSpawn { get; }
            public Vector2 ExitPosition { get; }
            public Vector2 ChestPosition { get; }
            public Vector2 ItemPosition { get; }
        }

        [Test]
        public void GeneratedCatalogsContainExpectedUniqueEntries()
        {
            SkillData[] skills = LoadAssets<SkillData>("Assets/ScriptableObjects/Skills");
            EnemyData[] enemies = LoadAssets<EnemyData>("Assets/ScriptableObjects/Enemies");
            EquipmentData[] equipment =
                LoadAssets<EquipmentData>("Assets/ScriptableObjects/Equipment");

            Assert.That(skills.Select(skill => skill.Id),
                Is.EquivalentTo(Enum.GetValues(typeof(SkillId)).Cast<SkillId>()));
            Assert.That(enemies, Has.Length.EqualTo(10));
            Assert.That(enemies.Select(enemy => enemy.EnemyId).Distinct(
                StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(enemies.Length));
            Assert.That(enemies.Select(enemy => enemy.EnemyId),
                Does.Contain("crystalscorpion"));
            Assert.That(enemies.Select(enemy => enemy.EnemyId),
                Does.Contain("voidreaver"));
            Assert.That(equipment, Has.Length.EqualTo(40));
            Assert.That(equipment.Select(item => item.ItemId).Distinct(
                StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(equipment.Length));
        }

        [Test]
        public void EditorStartupOnlyRecoversTheDefaultUntitledScene()
        {
            Scene temporary = EditorSceneManager.NewPreviewScene();
            try
            {
                Assert.That(TopDownEditorStartup.IsTemporaryStartupScene(temporary),
                    Is.True);

                GameObject work = new GameObject("DeveloperWork");
                SceneManager.MoveGameObjectToScene(work, temporary);
                Assert.That(TopDownEditorStartup.IsTemporaryStartupScene(temporary),
                    Is.False,
                    "An unsaved scene containing developer work must be preserved.");

                UnityEngine.Object.DestroyImmediate(work);
                EditorSceneManager.MarkSceneDirty(temporary);
                Assert.That(TopDownEditorStartup.IsTemporaryStartupScene(temporary),
                    Is.False,
                    "Unsaved changes must never be discarded, even in an empty scene.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(temporary);
            }
        }

        [Test]
        public void ExpansionSkillsAndShopDiscountHaveStableBalanceRules()
        {
            Assert.That(ChainLightningSkill.DamageMultiplierForHop(0, 0.75f),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ChainLightningSkill.DamageMultiplierForHop(1, 0.75f),
                Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(ChainLightningSkill.DamageMultiplierForHop(2, 0.75f),
                Is.EqualTo(0.5625f).Within(0.0001f));
            Assert.That(WeaponShopUI.DiscountedPrice(999, 0.2f),
                Is.EqualTo(800));

            foreach (string skillName in new[]
                { "ChainLightning", "MeteorShower", "VoidPulse" })
            {
                SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(
                    $"Assets/ScriptableObjects/Skills/{skillName}.asset");
                Assert.That(data, Is.Not.Null, skillName);
                Assert.That(data.Icon, Is.Not.Null, skillName + " icon");
                Assert.That(data.Cooldown, Is.GreaterThan(1f), skillName);
            }
        }

        [Test]
        public void RiftExpansionAuthorsEliteEnemiesAndWorldEvent()
        {
            EnemyData reaver = AssetDatabase.LoadAssetAtPath<EnemyData>(
                "Assets/ScriptableObjects/Enemies/VoidReaver.asset");
            GameObject reaverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/VoidReaver.prefab");
            Assert.That(reaver, Is.Not.Null);
            Assert.That(reaver.Archetype, Is.EqualTo(EnemyArchetype.VoidReaver));
            Assert.That(reaverPrefab, Is.Not.Null);
            Assert.That(reaverPrefab.GetComponent<VoidReaverEnemy>(), Is.Not.Null);

            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/RiftMap01.unity", OpenSceneMode.Additive);
            try
            {
                TopDownGame.World.RiftStormController storm = scene
                    .GetRootGameObjects().SelectMany(root => root
                        .GetComponentsInChildren<TopDownGame.World.RiftStormController>(
                            true)).SingleOrDefault();
                Assert.That(storm, Is.Not.Null);
                Assert.That(scene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<EnemyBase>(true)).Count(),
                    Is.GreaterThanOrEqualTo(12));
            }
            finally
            {
                if (SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void CrystalScorpionContentIsAuthoredAndPlacedInTheCave()
        {
            const string SpritePath =
                "Assets/Art/Generated/Characters/CrystalScorpion.png";
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(
                "Assets/ScriptableObjects/Enemies/CrystalScorpion.asset");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/CrystalScorpion.prefab");

            Assert.That(data, Is.Not.Null);
            Assert.That(data.EnemyId, Is.EqualTo("crystalscorpion"));
            Assert.That(data.DisplayName, Is.EqualTo("Bọ Cạp Pha Lê"));
            Assert.That(data.Archetype,
                Is.EqualTo(EnemyArchetype.CrystalScorpion));
            Assert.That(data.MaxHealth, Is.EqualTo(210f));
            Assert.That(data.Damage, Is.EqualTo(18f));
            Assert.That(data.Armor, Is.EqualTo(14f));

            Assert.That(prefab, Is.Not.Null);
            CrystalScorpionEnemy controller =
                prefab.GetComponent<CrystalScorpionEnemy>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.Data, Is.SameAs(data));
            SerializedObject serializedController =
                new SerializedObject(controller);
            Assert.That(serializedController.FindProperty("retreatDistance")
                .floatValue, Is.EqualTo(2.2f).Within(0.001f));
            Assert.That(serializedController.FindProperty("orbitOuterDistance")
                .floatValue, Is.EqualTo(4f).Within(0.001f));
            Assert.That(serializedController.FindProperty("pincerWindup")
                .floatValue, Is.GreaterThanOrEqualTo(0.3f));
            Assert.That(serializedController.FindProperty("chargeWindup")
                .floatValue, Is.GreaterThanOrEqualTo(0.5f));
            Assert.That(serializedController.FindProperty("spikeWindup")
                .floatValue, Is.GreaterThanOrEqualTo(0.6f));
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(renderer.sprite),
                Is.EqualTo(SpritePath));

            TextureImporter importer = AssetImporter.GetAtPath(SpritePath)
                as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.alphaIsTransparency, Is.True);
            Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32,
                false);
            try
            {
                Assert.That(decoded.LoadImage(File.ReadAllBytes(SpritePath), false),
                    Is.True);
                Assert.That(decoded.GetPixels32().Any(pixel => pixel.a < 10),
                    Is.True, "CrystalScorpion.png must contain genuine alpha.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(decoded);
            }

            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/CaveMap01.unity", OpenSceneMode.Additive);
            try
            {
                EnemyBase[] caveEnemies = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<EnemyBase>(true))
                    .ToArray();
                Assert.That(caveEnemies.Count(enemy => enemy.Data != null &&
                    enemy.Data.EnemyId == "mage"), Is.EqualTo(3));
                Assert.That(caveEnemies.Count(enemy => enemy.Data != null &&
                    enemy.Data.EnemyId == "tank"), Is.EqualTo(4));
                Assert.That(caveEnemies.Count(enemy => enemy.Data != null &&
                    enemy.Data.EnemyId == "crystalscorpion"), Is.EqualTo(3));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void CaveMageAndTankPrefabsUseAuthoredCombatControllers()
        {
            GameObject magePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/Mage.prefab");
            GameObject tankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/Tank.prefab");

            Assert.That(magePrefab, Is.Not.Null);
            MageEnemy mage = magePrefab.GetComponent<MageEnemy>();
            Assert.That(mage, Is.Not.Null);
            SerializedObject serializedMage = new SerializedObject(mage);
            Assert.That(serializedMage.FindProperty("combatPreferredDistance").floatValue,
                Is.EqualTo(5.6f).Within(0.001f));
            Assert.That(serializedMage.FindProperty("combatRetreatDistance").floatValue,
                Is.EqualTo(3.2f).Within(0.001f));
            Assert.That(serializedMage.FindProperty("castTime").floatValue,
                Is.GreaterThanOrEqualTo(0.6f));
            Assert.That(serializedMage.FindProperty("greaterRuneEveryCasts").intValue,
                Is.EqualTo(3));

            Assert.That(tankPrefab, Is.Not.Null);
            TankEnemy tank = tankPrefab.GetComponent<TankEnemy>();
            Assert.That(tank, Is.Not.Null,
                "Cave Tank must use its dedicated controller instead of generic melee AI.");
            SerializedObject serializedTank = new SerializedObject(tank);
            Assert.That(serializedTank.FindProperty("circleOuterDistance").floatValue,
                Is.EqualTo(2.8f).Within(0.001f));
            Assert.That(serializedTank.FindProperty("bashWindup").floatValue,
                Is.GreaterThanOrEqualTo(0.4f));
            Assert.That(serializedTank.FindProperty("slamEveryAttacks").intValue,
                Is.EqualTo(3));
            Assert.That(serializedTank.FindProperty("slamWindup").floatValue,
                Is.GreaterThanOrEqualTo(0.7f));

            GameObject scorpionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/CrystalScorpion.prefab");
            foreach (GameObject enemyPrefab in new[]
                { magePrefab, tankPrefab, scorpionPrefab })
            {
                Assert.That(enemyPrefab, Is.Not.Null);
                EnemyMovement movement = enemyPrefab.GetComponent<EnemyMovement>();
                EnemyCharacterAnimator animator =
                    enemyPrefab.GetComponent<EnemyCharacterAnimator>();
                Assert.That(movement, Is.Not.Null, enemyPrefab.name);
                Assert.That(animator, Is.Not.Null, enemyPrefab.name);

                SerializedObject serializedMovement =
                    new SerializedObject(movement);
                SerializedObject serializedAnimator =
                    new SerializedObject(animator);
                Assert.That(serializedMovement.FindProperty("acceleration")
                    .floatValue, Is.EqualTo(12f).Within(0.001f));
                Assert.That(serializedMovement.FindProperty("deceleration")
                    .floatValue, Is.EqualTo(18f).Within(0.001f));
                Assert.That(serializedMovement.FindProperty("turnSpeedDegrees")
                    .floatValue, Is.EqualTo(540f).Within(0.001f));
                Assert.That(serializedAnimator.FindProperty("actionPoseFollow")
                    .floatValue, Is.EqualTo(28f).Within(0.001f));
            }
        }

        [Test]
        public void EveryEnemyFamilyHasSignatureSkillAndAdvancedMobility()
        {
            (string prefabName, EnemyArchetype archetype,
                EnemySignatureSkill skill)[] catalog =
            {
                ("Slime", EnemyArchetype.Slime,
                    EnemySignatureSkill.ToxicPounce),
                ("Goblin", EnemyArchetype.Goblin,
                    EnemySignatureSkill.TricksterDash),
                ("Archer", EnemyArchetype.Archer,
                    EnemySignatureSkill.BackstepVolley),
                ("Mage", EnemyArchetype.Mage,
                    EnemySignatureSkill.ArcanePhase),
                ("RiftMage", EnemyArchetype.Mage,
                    EnemySignatureSkill.ArcanePhase),
                ("Tank", EnemyArchetype.Tank,
                    EnemySignatureSkill.SiegeCharge),
                ("ObsidianGolem", EnemyArchetype.Tank,
                    EnemySignatureSkill.SiegeCharge),
                ("CrystalScorpion", EnemyArchetype.CrystalScorpion,
                    EnemySignatureSkill.CrystalBurrow),
                ("VoidReaver", EnemyArchetype.VoidReaver,
                    EnemySignatureSkill.RiftCross),
                ("ForestGuardian", EnemyArchetype.Boss,
                    EnemySignatureSkill.AncientQuake)
            };

            foreach ((string prefabName, EnemyArchetype archetype,
                EnemySignatureSkill expectedSkill) in catalog)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/Prefabs/Enemies/{prefabName}.prefab");
                Assert.That(prefab, Is.Not.Null, prefabName);
                EnemyBase enemy = prefab.GetComponent<EnemyBase>();
                EnemyMovement movement = prefab.GetComponent<EnemyMovement>();
                EnemyCharacterAnimator animator =
                    prefab.GetComponent<EnemyCharacterAnimator>();
                EnemySignatureAbility signature =
                    prefab.GetComponent<EnemySignatureAbility>();

                Assert.That(enemy, Is.Not.Null, prefabName);
                Assert.That(enemy.Data.Archetype, Is.EqualTo(archetype),
                    prefabName);
                Assert.That(movement, Is.Not.Null, prefabName);
                Assert.That(animator, Is.Not.Null, prefabName);
                Assert.That(signature, Is.Not.Null, prefabName);
                Assert.That(signature.Skill, Is.EqualTo(expectedSkill),
                    prefabName);
                Assert.That(signature.DisplayName, Is.Not.Empty, prefabName);

                SerializedObject serializedMovement =
                    new SerializedObject(movement);
                Assert.That(serializedMovement.FindProperty(
                    "separationRadius").floatValue,
                    Is.EqualTo(0.78f).Within(0.001f), prefabName);
                Assert.That(serializedMovement.FindProperty(
                    "stuckDetectionTime").floatValue,
                    Is.EqualTo(0.55f).Within(0.001f), prefabName);
                Assert.That(serializedMovement.FindProperty(
                    "obstacleProbeDistance").floatValue,
                    Is.EqualTo(0.9f).Within(0.001f), prefabName);
                Assert.That(serializedMovement.FindProperty(
                    "obstacleAvoidanceStrength").floatValue,
                    Is.EqualTo(0.85f).Within(0.001f), prefabName);
                Assert.That(serializedMovement.FindProperty(
                    "maxSlideIterations").intValue,
                    Is.EqualTo(2), prefabName);
            }
        }

        [Test]
        public void Map02UsesDedicatedForestFootworkAndSkills()
        {
            GameObject goblinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/Goblin.prefab");
            GameObject archerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/Archer.prefab");
            Assert.That(goblinPrefab, Is.Not.Null);
            Assert.That(archerPrefab, Is.Not.Null);

            ForestGoblinEnemy goblin =
                goblinPrefab.GetComponent<ForestGoblinEnemy>();
            ForestArcherEnemy archer =
                archerPrefab.GetComponent<ForestArcherEnemy>();
            Assert.That(goblin, Is.Not.Null);
            Assert.That(archer, Is.Not.Null);
            Assert.That(goblinPrefab.GetComponent<MeleeEnemy>(), Is.Not.Null);
            Assert.That(archerPrefab.GetComponent<RangedEnemy>(), Is.Not.Null);
            Assert.That(goblin.MovementName, Is.EqualTo("Vòng Sườn Truy Kích"));
            Assert.That(goblin.SkillName, Is.EqualTo("Ảnh Bộ Song Trảm"));
            Assert.That(archer.MovementName, Is.EqualTo("Đổi Chốt Né Bắn"));
            Assert.That(archer.SkillName, Is.EqualTo("Vũ Tiễn Phong Tỏa"));
            Assert.That(goblin.UsesLinearCombatVfx, Is.False);
            Assert.That(goblin.UsesCircularCombatVfx, Is.False);
            Assert.That(archer.UsesLinearCombatVfx, Is.False);
            Assert.That(archer.UsesCircularCombatVfx, Is.True, "Arrow rain marks its locked landing area.");

            SerializedObject serializedGoblin = new SerializedObject(goblin);
            SerializedObject serializedArcher = new SerializedObject(archer);
            Assert.That(serializedGoblin.FindProperty("leafDashEveryAttacks")
                .intValue, Is.EqualTo(3));
            Assert.That(serializedGoblin.FindProperty("leafDashWindup")
                .floatValue, Is.EqualTo(0.48f).Within(0.001f));
            Assert.That(serializedArcher.FindProperty("leafVolleyEveryShots")
                .intValue, Is.EqualTo(3));
            Assert.That(serializedArcher.FindProperty("quickShotWindup")
                .floatValue, Is.EqualTo(0.4f).Within(0.001f));

            SerializedObject goblinAnimator = new SerializedObject(
                goblinPrefab.GetComponent<EnemyCharacterAnimator>());
            SerializedObject archerAnimator = new SerializedObject(
                archerPrefab.GetComponent<EnemyCharacterAnimator>());
            Sprite goblinSheet = goblinAnimator.FindProperty(
                "articulatedSheet").objectReferenceValue as Sprite;
            Sprite archerSheet = archerAnimator.FindProperty(
                "articulatedSheet").objectReferenceValue as Sprite;
            Assert.That(goblinSheet, Is.Not.Null,
                "Goblin needs visible left/right steps and hand-action frames.");
            Assert.That(archerSheet, Is.Not.Null,
                "Archer needs visible cross-steps and bow-hand frames.");
            Assert.That(AssetDatabase.GetAssetPath(goblinSheet),
                Does.EndWith("GoblinAnimationSheetV2.png"));
            Assert.That(AssetDatabase.GetAssetPath(archerSheet),
                Does.EndWith("ArcherAnimationSheetV2.png"));
            Assert.That(goblinAnimator.FindProperty("articulatedGrid")
                .vector2IntValue, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(archerAnimator.FindProperty("articulatedGrid")
                .vector2IntValue, Is.EqualTo(new Vector2Int(3, 2)));
            Assert.That(goblinAnimator.FindProperty("strideFrequency")
                .floatValue, Is.EqualTo(1.08f).Within(0.001f));
            Assert.That(goblinAnimator.FindProperty("walkSway")
                .floatValue, Is.EqualTo(4.4f).Within(0.001f));
            Assert.That(archerAnimator.FindProperty("strideFrequency")
                .floatValue, Is.EqualTo(0.84f).Within(0.001f));
            Assert.That(archerAnimator.FindProperty("walkSway")
                .floatValue, Is.EqualTo(2.2f).Within(0.001f));

            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/Map02.unity", OpenSceneMode.Additive);
            try
            {
                ForestGoblinEnemy[] sceneGoblins = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<
                        ForestGoblinEnemy>(true)).ToArray();
                ForestArcherEnemy[] sceneArchers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<
                        ForestArcherEnemy>(true)).ToArray();
                Assert.That(sceneGoblins.Length, Is.GreaterThanOrEqualTo(1));
                Assert.That(sceneArchers.Length, Is.GreaterThanOrEqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Map01UsesReadableSlimeMarshAndDedicatedHoppingSlime()
        {
            GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Enemies/Slime.prefab");
            Assert.That(slimePrefab, Is.Not.Null);
            SlimeEnemy slime = slimePrefab.GetComponent<SlimeEnemy>();
            Assert.That(slime, Is.Not.Null);
            Assert.That(slimePrefab.GetComponent<MeleeEnemy>(), Is.Not.Null,
                "SlimeEnemy must remain compatible with melee enemy consumers.");
            Assert.That(slime.NewSkillName, Is.EqualTo("Phun \u0110\u1ED9c \u0102n M\u00F2n"));
            Assert.That(slime.UsesCircularCombatVfx, Is.False,
                "Slime combat must not bring back the large green rings.");
            SerializedObject serializedSlime = new SerializedObject(slime);
            Assert.That(serializedSlime.FindProperty("hopWindup").floatValue,
                Is.EqualTo(0.16f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("hopAirTime").floatValue,
                Is.EqualTo(0.34f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("landingRecovery").floatValue,
                Is.EqualTo(0.13f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("poisonSprayEveryAttacks")
                .intValue, Is.EqualTo(2));
            Assert.That(serializedSlime.FindProperty("poisonSprayWindup")
                .floatValue, Is.EqualTo(0.22f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("poisonSprayDuration")
                .floatValue, Is.EqualTo(0.68f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("poisonSprayRecovery")
                .floatValue, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty(
                "poisonSprayCooldownMultiplier").floatValue,
                Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("poisonSprayRange")
                .floatValue, Is.EqualTo(5.2f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("poisonSprayAngle")
                .floatValue, Is.EqualTo(54f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("poisonStreamCount")
                .intValue, Is.EqualTo(5));
            Assert.That(serializedSlime.FindProperty("poisonBurstInterval")
                .floatValue, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("poisonApplicationChance")
                .floatValue, Is.EqualTo(0.45f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("poisonDuration")
                .floatValue, Is.EqualTo(3.6f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty("poisonTickInterval")
                .floatValue, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(serializedSlime.FindProperty(
                "poisonDamagePerTickMultiplier").floatValue,
                Is.EqualTo(0.12f).Within(0.001f));
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Player/Player.prefab");
            Assert.That(playerPrefab.GetComponent<PlayerPoisonStatus>(),
                Is.Not.Null,
                "The player prefab must carry the visible poison status system.");

            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/Map01.unity", OpenSceneMode.Additive);
            try
            {
                GameObject floor = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .First(transform => transform.name == "Floor").gameObject;
                SpriteRenderer floorRenderer = floor.GetComponent<SpriteRenderer>();
                Camera camera = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                    .Single(component => component.CompareTag("MainCamera"));
                Transform[] transforms = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .ToArray();
                TopDownGame.World.SlimeMarshController marsh =
                    scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<
                            TopDownGame.World.SlimeMarshController>(true))
                        .Single();

                Assert.That(floorRenderer, Is.Not.Null);
                float floorBrightness = floorRenderer.color.r +
                    floorRenderer.color.g + floorRenderer.color.b;
                float clearBrightness = camera.backgroundColor.r +
                    camera.backgroundColor.g + camera.backgroundColor.b;
                Assert.That(floorBrightness, Is.GreaterThan(0.5f),
                    "The forest outside the walkable maze must remain readable.");
                Assert.That(clearBrightness, Is.GreaterThan(0.35f),
                    "The camera clear color must not expose a near-black frame.");
                Assert.That(transforms.Any(transform =>
                    transform.name == "SlimeMarshBiome"), Is.True);
                Assert.That(transforms.Count(transform =>
                    transform.name.StartsWith("SlimeTrail_")),
                    Is.GreaterThanOrEqualTo(8));
                Assert.That(transforms.Count(transform =>
                    transform.name.StartsWith("SlimeCombatClearing_")),
                    Is.EqualTo(5));
                Assert.That(transforms.Count(transform =>
                    transform.name.StartsWith("SlimeNestGround_")),
                    Is.EqualTo(4));
                Assert.That(marsh.NestCount, Is.EqualTo(4));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void PlayerPrefabUsesFullExperimentalVitals()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Player/Player.prefab");
            Assert.That(playerPrefab, Is.Not.Null);
            TopDownGame.Player.PlayerStats stats =
                playerPrefab.GetComponent<TopDownGame.Player.PlayerStats>();
            Assert.That(stats, Is.Not.Null);

            SerializedObject serializedStats = new SerializedObject(stats);
            Assert.That(serializedStats.FindProperty("experimentalVitals")
                .boolValue, Is.True);
            Assert.That(serializedStats.FindProperty("maxHealth").floatValue,
                Is.EqualTo(TopDownGame.Player.PlayerStats.ExperimentalMaxHealth));
            Assert.That(serializedStats.FindProperty("maxMana").floatValue,
                Is.EqualTo(TopDownGame.Player.PlayerStats.ExperimentalMaxMana));
            Assert.That(serializedStats.FindProperty("manaRegeneration").floatValue,
                Is.EqualTo(TopDownGame.Player.PlayerStats
                    .ExperimentalManaRegeneration));
        }

        [Test]
        public void RequiredScenesExistAndAreEnabledInBuildSettings()
        {
            HashSet<string> enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string sceneName in RequiredScenes)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    $"Assets/Scenes/{sceneName}.unity"), Is.Not.Null, sceneName);
                Assert.That(enabledScenes, Does.Contain(sceneName), sceneName);
            }
        }

        [Test]
        public void GeneratedDecorPropsAreValidSpritesWithRealTransparency()
        {
            const string PropsRoot = "Assets/Art/Generated/Props";
            foreach (string propName in GeneratedDecorProps)
            {
                string path = $"{PropsRoot}/{propName}.png";
                Assert.That(File.Exists(path), Is.True, path);

                TextureImporter importer = AssetImporter.GetAtPath(path)
                    as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType,
                    Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode,
                    Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear),
                    path);

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(sprite.texture, Is.Not.Null, path);

                Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32,
                    false);
                try
                {
                    Assert.That(decoded.LoadImage(File.ReadAllBytes(path), false),
                        Is.True, path);
                    bool hasTransparentPixel = decoded.GetPixels32().Any(pixel =>
                        pixel.a / 255f < 0.05f);
                    Assert.That(hasTransparentPixel, Is.True,
                        $"{path} must contain genuine transparent pixels, not a baked background.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(decoded);
                }
            }
        }

        [Test]
        public void RebuiltLobbyContainsGeneratedDecorRoot()
        {
            const string LobbyPath = "Assets/Scenes/Lobby.unity";
            Assert.That(File.Exists(LobbyPath), Is.True, LobbyPath);
            Assert.That(File.ReadAllText(LobbyPath),
                Does.Contain("m_Name: LobbyDecor_Generated"),
                "Rebuild Lobby Scene Only must persist the generated decor root.");
        }

        [Test]
        public void ForestRelicUsesAuthoredPickupArt()
        {
            var relic = AssetDatabase.LoadAssetAtPath<
                TopDownGame.Items.ItemData>(
                "Assets/ScriptableObjects/Items/forest_relic.asset");
            Assert.That(relic, Is.Not.Null);
            Assert.That(relic.Icon, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(relic.Icon), Is.EqualTo(
                "Assets/Art/Generated/Props/ForestRelicPickupV1.png"));
        }

        [Test]
        public void RebuiltCombatMapsContainDistinctDecorAndNoBlockingObstacle()
        {
            var expectedMaps = new Dictionary<string, MazeSceneExpectation>
            {
                {
                    "Map01", new MazeSceneExpectation(
                        "ForestPondRuinClusterV1",
                        new Vector2(-18f, -9f), new Vector2(18f, 8f),
                        new Vector2(15f, 10f), new Vector2(4f, -9f))
                },
                {
                    "Map03", new MazeSceneExpectation(
                        "ForestPondRuinClusterV1",
                        new Vector2(-17f, -8f), new Vector2(18f, 7f),
                        new Vector2(-5f, 8f), new Vector2(2f, -8f))
                },
                {
                    "BossMap01", new MazeSceneExpectation(
                        "ForestPondRuinClusterV1",
                        new Vector2(-18f, -9f), new Vector2(19f, 9f),
                        new Vector2(17f, 8f), new Vector2(16f, -9f))
                },
                {
                    "CaveMap01", new MazeSceneExpectation(
                        "CavePoolMineClusterV1",
                        new Vector2(-18f, -9f), new Vector2(18f, 4f),
                        new Vector2(16f, 7f), new Vector2(-4f, -11f))
                },
                {
                    "RiftMap01", new MazeSceneExpectation(
                        "ForestPondRuinClusterV1",
                        new Vector2(-20f, -10f), new Vector2(20f, 10f),
                        new Vector2(17f, 10f), new Vector2(2f, -7f))
                }
            };
            string[] clutterGroupNames =
            {
                "Clutter_Entrance", "Clutter_LobeEdges",
                "Clutter_Junctions", "Clutter_SideBranches",
                "Clutter_Landmark", "Clutter_CombatRooms",
                "Clutter_RewardRoom", "Clutter_Exit"
            };

            foreach (KeyValuePair<string, MazeSceneExpectation> entry in
                expectedMaps)
            {
                string path = $"Assets/Scenes/{entry.Key}.unity";
                string sceneText = File.ReadAllText(path);
                Assert.That(sceneText, Does.Not.Contain("m_Name: Obstacle"),
                    $"{entry.Key}: the old central obstacle blocks cardinal click paths.");
                Assert.That(sceneText, Does.Contain("targetTag: Player"),
                    $"{entry.Key}: camera must recover a rebuilt Player reference.");

                Scene scene = EditorSceneManager.OpenScene(path,
                    OpenSceneMode.Additive);
                try
                {
                    Transform[] sceneTransforms = scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<Transform>(
                            true)).ToArray();
                    Transform decorRoot = sceneTransforms.SingleOrDefault(
                        transform => transform.name == "CombatDecor_Generated");
                    Assert.That(decorRoot, Is.Not.Null,
                        $"{entry.Key}: generated combat decor root is missing.");

                    Transform environment = sceneTransforms.SingleOrDefault(
                        transform => transform.name == "Environment");
                    Assert.That(environment, Is.Not.Null,
                        $"{entry.Key}: outer environment is missing.");
                    SpriteRenderer outsideFloor = environment.Find("Floor")
                        ?.GetComponent<SpriteRenderer>();
                    Assert.That(outsideFloor, Is.Not.Null,
                        $"{entry.Key}: dark biome background floor is missing.");
                    Camera sceneCamera = sceneTransforms.Select(transform =>
                        transform.GetComponent<Camera>()).FirstOrDefault(camera =>
                            camera != null);
                    Assert.That(sceneCamera, Is.Not.Null, entry.Key);
                    Assert.That(sceneCamera.backgroundColor.maxColorComponent,
                        Is.GreaterThan(0.35f),
                        $"{entry.Key}: camera fallback color is too close to black.");

                    SpriteRenderer[] walkableFloors = decorRoot.Cast<Transform>()
                        .Where(transform => transform.name.StartsWith(
                            "WalkableFloor_", StringComparison.Ordinal))
                        .Select(transform => transform.GetComponent<SpriteRenderer>())
                        .Where(renderer => renderer != null)
                        .ToArray();
                    Assert.That(walkableFloors.Length, Is.GreaterThan(3),
                        $"{entry.Key}: organic floor must contain merged walkable regions.");
                    foreach (SpriteRenderer walkableFloor in walkableFloors)
                    {
                        Assert.That(walkableFloor.sprite, Is.Not.Null, entry.Key);
                        Assert.That(walkableFloor.drawMode,
                            Is.EqualTo(SpriteDrawMode.Tiled), entry.Key);
                        Assert.That(walkableFloor.size.x, Is.GreaterThan(0.45f));
                        Assert.That(walkableFloor.size.y, Is.GreaterThan(0.45f));
                        Assert.That(walkableFloor.sortingOrder,
                            Is.GreaterThan(outsideFloor.sortingOrder), entry.Key);
                    }

                    Bounds walkableBounds = walkableFloors[0].bounds;
                    for (int i = 1; i < walkableFloors.Length; i++)
                    {
                        walkableBounds.Encapsulate(walkableFloors[i].bounds);
                    }
                    Assert.That(outsideFloor.bounds.min.x,
                        Is.LessThan(walkableBounds.min.x - 8f), entry.Key);
                    Assert.That(outsideFloor.bounds.max.x,
                        Is.GreaterThan(walkableBounds.max.x + 8f), entry.Key);
                    Assert.That(outsideFloor.bounds.min.y,
                        Is.LessThan(walkableBounds.min.y - 5f), entry.Key);
                    Assert.That(outsideFloor.bounds.max.y,
                        Is.GreaterThan(walkableBounds.max.y + 5f), entry.Key);

                    Transform topology = decorRoot.Find("MazeTopology");
                    Assert.That(topology, Is.Not.Null,
                        $"{entry.Key}: semantic maze topology is missing.");
                    Transform[] lobes = topology.Cast<Transform>()
                        .Where(transform => transform.name.StartsWith(
                            "MazeLobe_", StringComparison.Ordinal)).ToArray();
                    Transform[] corridors = topology.Cast<Transform>()
                        .Where(transform => transform.name.StartsWith(
                            "MazeCorridor_", StringComparison.Ordinal)).ToArray();
                    Assert.That(lobes.Length, Is.GreaterThanOrEqualTo(4), entry.Key);
                    Assert.That(corridors.Length, Is.GreaterThanOrEqualTo(4), entry.Key);
                    foreach (Transform lobe in lobes)
                    {
                        Assert.That(lobe.localScale.x, Is.GreaterThan(2f), lobe.name);
                        Assert.That(lobe.localScale.y, Is.GreaterThan(2f), lobe.name);
                        Assert.That(IsInsideWalkableFloor(lobe.position,
                            walkableFloors), Is.True,
                            $"{entry.Key}/{lobe.name}: lobe is outside its organic floor.");
                    }
                    foreach (Transform corridor in corridors)
                    {
                        Transform[] points = corridor.Cast<Transform>()
                            .Where(point => point.name.StartsWith(
                                "Point_", StringComparison.Ordinal)).ToArray();
                        Assert.That(corridor.localScale.x,
                            Is.GreaterThanOrEqualTo(2.8f), corridor.name);
                        Assert.That(points.Length, Is.GreaterThanOrEqualTo(2),
                            corridor.name);
                        for (int i = 0; i < points.Length; i++)
                        {
                            Assert.That(IsInsideWalkableFloor(points[i].position,
                                walkableFloors), Is.True,
                                $"{entry.Key}/{corridor.name}/{points[i].name}");
                            if (i > 0)
                            {
                                Vector2 delta = points[i].position -
                                    points[i - 1].position;
                                bool cardinal = (Mathf.Abs(delta.x) > 0.01f &&
                                    Mathf.Abs(delta.y) < 0.01f) ||
                                    (Mathf.Abs(delta.y) > 0.01f &&
                                     Mathf.Abs(delta.x) < 0.01f);
                                Assert.That(cardinal, Is.True,
                                    $"{entry.Key}/{corridor.name}: corridor bends must stay cardinal.");
                            }
                        }
                    }

                    Transform mazeWalls = decorRoot.Find("MazeWalls");
                    Assert.That(mazeWalls, Is.Not.Null,
                        $"{entry.Key}: maze wall root is missing.");

                    Transform[] wallSegments = mazeWalls.Cast<Transform>()
                        .Where(transform => transform.name.StartsWith(
                            "MazeWall_", StringComparison.Ordinal) &&
                            transform.GetComponent<BoxCollider2D>() != null)
                        .ToArray();
                    Assert.That(wallSegments.Length, Is.GreaterThan(4),
                        $"{entry.Key}: organic boundary needs multiple merged segments.");
                    foreach (Transform wallSegment in wallSegments)
                    {
                        BoxCollider2D wallCollider =
                            wallSegment.GetComponent<BoxCollider2D>();
                        Assert.That(wallCollider.isTrigger, Is.False,
                            $"{entry.Key}/{wallSegment.name}: maze walls must be solid.");
                        Assert.That(wallCollider.size.x * wallCollider.size.y,
                            Is.GreaterThan(0.1f),
                            $"{entry.Key}/{wallSegment.name}: wall collider is empty.");
                        Assert.That(wallSegment.Cast<Transform>().Any(child =>
                            child.name.StartsWith(wallSegment.name + "_Visual_",
                                StringComparison.Ordinal) &&
                            child.GetComponent<SpriteRenderer>() != null), Is.True,
                            $"{entry.Key}/{wallSegment.name}: boundary lacks organic visuals.");
                    }

                    foreach (string groupName in clutterGroupNames)
                    {
                        Transform group = decorRoot.Find(groupName);
                        Assert.That(group, Is.Not.Null,
                            $"{entry.Key}: semantic clutter group '{groupName}' is missing.");
                        Assert.That(group.GetComponentsInChildren<SpriteRenderer>(true)
                            .Any(renderer => renderer.enabled &&
                                renderer.sprite != null), Is.True,
                            $"{entry.Key}/{groupName}: clutter group has no visible prop.");
                    }

                    Transform landmarkGroup = decorRoot.Find("Clutter_Landmark");
                    BoxCollider2D landmarkCollider = landmarkGroup.Find(
                        "SceneryCollider_Landmark")?.GetComponent<BoxCollider2D>();
                    Assert.That(landmarkCollider, Is.Not.Null,
                        $"{entry.Key}: landmark scenery needs a solid collider.");
                    Assert.That(landmarkCollider.isTrigger, Is.False, entry.Key);
                    SpriteRenderer landmarkRenderer = landmarkGroup
                        .GetComponentsInChildren<SpriteRenderer>(true)
                        .SingleOrDefault(renderer => renderer.gameObject.name
                            .StartsWith("Landmark_", StringComparison.Ordinal));
                    Assert.That(landmarkRenderer, Is.Not.Null,
                        $"{entry.Key}: organic landmark visual is missing.");
                    Assert.That(AssetDatabase.GetAssetPath(landmarkRenderer.sprite),
                        Is.EqualTo("Assets/Art/Generated/Props/" +
                            entry.Value.LandmarkAssetName + ".png"), entry.Key);

                    Transform spawn = sceneTransforms.SingleOrDefault(
                        transform => transform.name == "PlayerSpawnPoint");
                    Transform door = sceneTransforms.SingleOrDefault(
                        transform => transform.name == "MapDoor");
                    Transform chest = sceneTransforms.SingleOrDefault(
                        transform => transform.name == "RewardChest");
                    Transform item = sceneTransforms.SingleOrDefault(
                        transform => transform.name == "ItemPickup");
                    Assert.That(spawn, Is.Not.Null,
                        $"{entry.Key}: player spawn marker is missing.");
                    Assert.That(door, Is.Not.Null,
                        $"{entry.Key}: exit door is missing.");
                    Assert.That(chest, Is.Not.Null,
                        $"{entry.Key}: reward chest is missing.");
                    Assert.That(item, Is.Not.Null,
                        $"{entry.Key}: reward item is missing.");
                    AssertWorldPosition(entry.Key, "spawn", spawn.position,
                        entry.Value.PlayerSpawn);
                    AssertWorldPosition(entry.Key, "door", door.position,
                        entry.Value.ExitPosition);
                    AssertWorldPosition(entry.Key, "chest", chest.position,
                        entry.Value.ChestPosition);
                    AssertWorldPosition(entry.Key, "item", item.position,
                        entry.Value.ItemPosition);

                    Transform guidanceRoot = sceneTransforms.SingleOrDefault(
                        transform => transform.name == "MapGuidance");
                    Assert.That(guidanceRoot, Is.Not.Null,
                        $"{entry.Key}: map guidance root is missing.");
                    TopDownGame.World.MapGuidanceVisuals guidance = guidanceRoot
                        .GetComponent<TopDownGame.World.MapGuidanceVisuals>();
                    Assert.That(guidance, Is.Not.Null, entry.Key);
                    Assert.That(guidance.MapId, Is.EqualTo(entry.Key), entry.Key);
                    Assert.That(guidance.HasValidAnchors, Is.True, entry.Key);
                    Assert.That(guidanceRoot.GetComponentsInChildren<Collider2D>(true),
                        Is.Empty,
                        $"{entry.Key}: guidance visuals must never block movement.");
                    Assert.That(guidanceRoot.GetComponentsInChildren<TextMesh>(true)
                        .Length, Is.EqualTo(3),
                        $"{entry.Key}: entrance, reward and exit labels are required.");
                    AssertWorldPosition(entry.Key, "guidance entrance",
                        guidanceRoot.Find("EntranceAnchor").position,
                        entry.Value.PlayerSpawn);
                    AssertWorldPosition(entry.Key, "guidance exit",
                        guidanceRoot.Find("ExitAnchor").position,
                        entry.Value.ExitPosition);
                    AssertWorldPosition(entry.Key, "guidance reward",
                        guidanceRoot.Find("RewardAnchor").position,
                        entry.Value.ChestPosition);

                    Vector2[] gameplayPositions =
                    {
                        entry.Value.PlayerSpawn, entry.Value.ExitPosition,
                        entry.Value.ChestPosition, entry.Value.ItemPosition
                    };
                    foreach (Vector2 gameplayPosition in gameplayPositions)
                    {
                        Assert.That(IsInsideWalkableFloor(gameplayPosition,
                            walkableFloors), Is.True,
                            $"{entry.Key}: gameplay marker {gameplayPosition} is outside walkable floor.");
                    }

                    Transform safetyWalls = environment.Find("Walls");
                    Assert.That(safetyWalls, Is.Not.Null, entry.Key);
                    string[] safetyNames =
                    {
                        "Wall_Top", "Wall_Bottom", "Wall_Left", "Wall_Right"
                    };
                    BoxCollider2D[] outerSafety = safetyNames.Select(name =>
                        safetyWalls.Find(name)?.GetComponent<BoxCollider2D>())
                        .ToArray();
                    Assert.That(outerSafety.All(collider => collider != null &&
                        !collider.isTrigger), Is.True,
                        $"{entry.Key}: outer safety boundary must be closed and solid.");
                    AssertOrganicBoundaryIsClosed(entry.Key,
                        entry.Value.PlayerSpawn, gameplayPositions,
                        walkableFloors,
                        wallSegments.Select(segment =>
                            segment.GetComponent<BoxCollider2D>()).ToArray(),
                        outerSafety);
                }
                finally
                {
                    // EditMode can begin with no persistent scene. Unity logs
                    // an error when asked to close the last loaded scene, so
                    // replace it with a clean empty scene in that case.
                    if (SceneManager.sceneCount > 1)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                    else
                    {
                        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                            NewSceneMode.Single);
                    }
                }
            }
        }

        [Test]
        public void RebuiltLobbyContainsNonBlockingGuidanceForCoreStations()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity",
                OpenSceneMode.Additive);
            try
            {
                Transform guidanceRoot = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .SingleOrDefault(transform => transform.name == "MapGuidance");
                Assert.That(guidanceRoot, Is.Not.Null);
                TopDownGame.World.MapGuidanceVisuals guidance = guidanceRoot
                    .GetComponent<TopDownGame.World.MapGuidanceVisuals>();
                Assert.That(guidance, Is.Not.Null);
                Assert.That(guidance.MapId, Is.EqualTo("Lobby"));
                Assert.That(guidance.HasValidAnchors, Is.True);
                Assert.That(guidanceRoot.GetComponentsInChildren<Collider2D>(true),
                    Is.Empty);
                AssertWorldPosition("Lobby", "guidance entrance",
                    guidanceRoot.Find("EntranceAnchor").position,
                    new Vector2(-5.5f, 0f));
                AssertWorldPosition("Lobby", "guidance exit",
                    guidanceRoot.Find("ExitAnchor").position,
                    new Vector2(8.73f, -0.20f));
                AssertWorldPosition("Lobby", "guidance reward",
                    guidanceRoot.Find("RewardAnchor").position,
                    new Vector2(5.85f, 1.65f));
            }
            finally
            {
                if (SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
            }
        }

        [Test]
        public void RebuiltGameplayScenesContainMiniMapController()
        {
            foreach (string sceneName in RequiredScenes.Where(name =>
                name != "MainMenu"))
            {
                Scene scene = EditorSceneManager.OpenScene(
                    $"Assets/Scenes/{sceneName}.unity", OpenSceneMode.Additive);
                try
                {
                    TopDownGame.UI.MiniMapController[] miniMaps = scene
                        .GetRootGameObjects().SelectMany(root =>
                            root.GetComponentsInChildren<
                                TopDownGame.UI.MiniMapController>(true)).ToArray();
                    Assert.That(miniMaps.Length, Is.EqualTo(1),
                        $"{sceneName}: GameplayCanvas must own exactly one minimap controller.");
                    Assert.That(miniMaps[0].GetComponent<Canvas>(), Is.Not.Null,
                        $"{sceneName}: minimap should share the gameplay overlay canvas.");
                }
                finally
                {
                    if (SceneManager.sceneCount > 1)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                    else
                    {
                        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                            NewSceneMode.Single);
                    }
                }
            }
        }

        [Test]
        public void PlayerWalkSpritesMatchIdleWorldHeight()
        {
            Sprite idle = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Generated/Characters/Player.png");
            Assert.That(idle, Is.Not.Null);
            float idleHeight = idle.bounds.size.y;

            string[] animationFolders =
            {
                "Assets/Art/Generated/Characters/PlayerV3Smooth",
                "Assets/Art/Generated/Characters/PlayerV4Walk",
                "Assets/Art/Generated/Characters/PlayerV6EightFrame",
                "Assets/Art/Generated/Characters/PlayerV7Seamless",
                "Assets/Art/Generated/Characters/PlayerV8Biomechanical",
                "Assets/Art/Generated/Characters/PlayerV9Complete"
            };
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", animationFolders);
            Assert.That(guids.Length, Is.GreaterThan(0));
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(frame, Is.Not.Null, path);
                Assert.That(frame.bounds.size.y, Is.EqualTo(idleHeight).Within(0.02f),
                    path);
            }
        }

        [Test]
        public void PlayerUsesSheathedIdleAndSwordFramesOnlyForSkillCast()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Player/Player.prefab");
            Assert.That(playerPrefab, Is.Not.Null);

            TopDownGame.Player.PlayerCharacterAnimator animator =
                playerPrefab.GetComponent<TopDownGame.Player.PlayerCharacterAnimator>();
            Assert.That(animator, Is.Not.Null);

            SerializedObject serializedAnimator = new SerializedObject(animator);
            SerializedProperty idleSprite = serializedAnimator.FindProperty("idleSprite");
            Assert.That(idleSprite.objectReferenceValue, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(idleSprite.objectReferenceValue),
                Is.EqualTo("Assets/Art/Generated/Characters/PlayerV2/Idle_00.png"));

            SerializedProperty skillFrames =
                serializedAnimator.FindProperty("skillCastFrames");
            Assert.That(skillFrames.arraySize, Is.EqualTo(3));
            for (int i = 0; i < skillFrames.arraySize; i++)
            {
                Assert.That(skillFrames.GetArrayElementAtIndex(i).objectReferenceValue,
                    Is.Not.Null, $"skillCastFrames[{i}]");
            }
        }

        [Test]
        public void PlayerLocomotionUsesSynchronizedNaturalWalkCadence()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Player/Player.prefab");
            Assert.That(playerPrefab, Is.Not.Null);

            var stats = playerPrefab.GetComponent<
                TopDownGame.Player.PlayerStats>();
            var movement = playerPrefab.GetComponent<
                TopDownGame.Player.PlayerMovement>();
            var animator = playerPrefab.GetComponent<
                TopDownGame.Player.PlayerCharacterAnimator>();
            var body = playerPrefab.GetComponent<Rigidbody2D>();
            Assert.That(stats, Is.Not.Null);
            Assert.That(movement, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(body, Is.Not.Null);

            SerializedObject serializedStats = new SerializedObject(stats);
            SerializedObject serializedMovement = new SerializedObject(movement);
            SerializedObject serializedAnimator = new SerializedObject(animator);
            Assert.That(serializedStats.FindProperty("moveSpeed").floatValue,
                Is.EqualTo(TopDownGame.Player.PlayerStats.DefaultMoveSpeed)
                    .Within(0.001f));
            Assert.That(serializedMovement.FindProperty("moveSpeed").floatValue,
                Is.EqualTo(TopDownGame.Player.PlayerStats.DefaultMoveSpeed)
                    .Within(0.001f),
                "PlayerMovement fallback and PlayerStats speed must stay synchronized.");
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(body.useFullKinematicContacts, Is.True);
            Assert.That(body.interpolation,
                Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
            Assert.That(serializedMovement.FindProperty("contactOffset").floatValue,
                Is.EqualTo(TopDownGame.Player.PlayerMovement.DefaultContactOffset)
                    .Within(0.001f));
            Assert.That(serializedMovement.FindProperty("maxSlideIterations").intValue,
                Is.EqualTo(TopDownGame.Player.PlayerMovement
                    .DefaultMaxSlideIterations));
            Assert.That(serializedMovement.FindProperty("movementLayers").intValue,
                Is.EqualTo(TopDownGame.Core.GameplayCollisionRules.PlayerMovementMask),
                "Player navigation should ignore enemies but retain solid scenery.");
            Assert.That(playerPrefab.layer,
                Is.EqualTo(TopDownGame.Core.GameplayCollisionRules.PlayerLayer));
            Assert.That(Physics2D.GetIgnoreLayerCollision(
                TopDownGame.Core.GameplayCollisionRules.PlayerLayer,
                TopDownGame.Core.GameplayCollisionRules.EnemyLayer), Is.True,
                "Player and enemy bodies must pass through each other.");
            Assert.That(serializedMovement.FindProperty("impulseDamping").floatValue,
                Is.EqualTo(TopDownGame.Player.PlayerMovement.DefaultImpulseDamping)
                    .Within(0.001f));
            Assert.That(serializedMovement.FindProperty("gaitCyclesPerUnit").floatValue,
                Is.EqualTo(TopDownGame.Player.PlayerMovement
                    .DefaultGaitCyclesPerUnit).Within(0.001f));
            Assert.That(serializedAnimator.FindProperty("walkStartSpeed").floatValue,
                Is.EqualTo(0.08f).Within(0.001f));
            Assert.That(serializedAnimator.FindProperty("walkStopSpeed").floatValue,
                Is.EqualTo(0.04f).Within(0.001f));
            string[] generatedScenes =
            {
                "Lobby", "Map01", "Map02", "Map03", "CaveMap01",
                "RiftMap01", "BossMap01"
            };
            foreach (string sceneName in generatedScenes)
            {
                string sceneText = File.ReadAllText(
                    $"Assets/Scenes/{sceneName}.unity");
                Assert.That(sceneText, Does.Contain("smoothTime: 0"), sceneName);
                Assert.That(sceneText, Does.Not.Contain("smoothTime: 0.06"),
                    $"{sceneName}: camera damping must not stack with Rigidbody interpolation.");
                Assert.That(sceneText, Does.Contain("pixelSnap: 0"),
                    $"{sceneName}: pixel snapping reintroduces movement judder.");
            }

            Assert.That(File.Exists(
                "Assets/ThirdParty/UnityPhysicsExamples2D/LICENSE.md"), Is.True);
            Assert.That(File.Exists(
                "Assets/ThirdParty/UnityPhysicsExamples2D/NOTICE.md"), Is.True);
        }

        [Test]
        public void PlayerStopReturnsToAuthoredIdlePose()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Player/Player.prefab");
            GameObject player = UnityEngine.Object.Instantiate(playerPrefab);
            try
            {
                var animator = player.GetComponent<
                    TopDownGame.Player.PlayerCharacterAnimator>();
                const System.Reflection.BindingFlags Flags =
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic;
                System.Reflection.MethodInfo start = animator.GetType()
                    .GetMethod("Start", Flags);
                System.Reflection.MethodInfo showStandingOrIdle = animator.GetType()
                    .GetMethod("ShowStandingOrIdle", Flags);
                Assert.That(start, Is.Not.Null);
                Assert.That(showStandingOrIdle, Is.Not.Null);

                start.Invoke(animator, null);
                Transform movementVisual = player.transform.Find("MovementVisual");
                SpriteRenderer renderer = movementVisual != null
                    ? movementVisual.GetComponent<SpriteRenderer>()
                    : player.GetComponent<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null);

                SerializedObject serializedAnimator = new SerializedObject(animator);
                Sprite idle = serializedAnimator.FindProperty("idleSprite")
                    .objectReferenceValue as Sprite;
                Sprite walkingFrame = serializedAnimator.FindProperty("walkDownFrames")
                    .GetArrayElementAtIndex(3).objectReferenceValue as Sprite;
                Assert.That(idle, Is.Not.Null);
                Assert.That(walkingFrame, Is.Not.Null);

                renderer.sprite = walkingFrame;
                showStandingOrIdle.Invoke(animator, null);
                Assert.That(renderer.sprite, Is.SameAs(idle),
                    "Stopping must restore Idle_00 instead of freezing a walk pose.");
                Assert.That(animator.GetType().GetField("standingSprite", Flags),
                    Is.Null, "Walk frames must never be cached as the idle pose.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerFourDirectionWalkUsesGroundedReadableCycles()
        {
            const string idlePath =
                "Assets/Art/Generated/Characters/PlayerV2/Idle_00.png";
            Sprite idle = AssetDatabase.LoadAssetAtPath<Sprite>(idlePath);
            Assert.That(idle, Is.Not.Null);
            Assert.That(idle.texture.filterMode, Is.EqualTo(FilterMode.Bilinear),
                "Downsampled character art needs bilinear filtering.");
            float idleGround = OpaqueBottomWorldOffset(idlePath, idle);

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Player/Player.prefab");
            var animator = playerPrefab.GetComponent<
                TopDownGame.Player.PlayerCharacterAnimator>();
            SerializedObject serializedAnimator = new SerializedObject(animator);
            Assert.That(serializedAnimator.FindProperty("walkUpRightFrames"), Is.Null);
            Assert.That(serializedAnimator.FindProperty("walkDownRightFrames"), Is.Null);

            string[] directions = { "Down", "Up", "Left", "Right" };
            SerializedProperty leftFrames = serializedAnimator.FindProperty(
                "walkLeftFrames");
            SerializedProperty rightFrames = serializedAnimator.FindProperty(
                "walkRightFrames");
            foreach (string direction in directions)
            {
                SerializedProperty frames = serializedAnimator.FindProperty(
                    "walk" + direction + "Frames");
                Assert.That(frames.arraySize, Is.EqualTo(8), direction);

                string[] expectedSuffixes =
                    { "00.png", "01.png", "02.png", "03.png",
                        "04.png", "05.png", "06.png", "07.png" };
                for (int i = 0; i < frames.arraySize; i++)
                {
                    Sprite frame = frames.GetArrayElementAtIndex(i)
                        .objectReferenceValue as Sprite;
                    Assert.That(frame, Is.Not.Null, $"{direction}[{i}]");
                    string path = AssetDatabase.GetAssetPath(frame);
                    string expectedSet = direction == "Left" ||
                        direction == "Right"
                        ? "/PlayerV10Horizontal/"
                        : "/PlayerV6EightFrame/";
                    Assert.That(path, Does.Contain(expectedSet),
                        $"{direction}[{i}]");
                    Assert.That(path, Does.EndWith(expectedSuffixes[i]),
                        $"{direction}[{i}]");
                    Assert.That(OpaqueBottomWorldOffset(path, frame),
                        Is.EqualTo(idleGround).Within(0.012f), path);
                    Assert.That(frame.texture.filterMode,
                        Is.EqualTo(FilterMode.Bilinear), path);
                    TextureImporter importer = AssetImporter.GetAtPath(path)
                        as TextureImporter;
                    Assert.That(importer, Is.Not.Null, path);
                    Assert.That(importer.textureType,
                        Is.EqualTo(TextureImporterType.Sprite), path);
                    Assert.That(importer.spriteImportMode,
                        Is.EqualTo(SpriteImportMode.Single), path);
                    Assert.That(importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.textureCompression,
                        Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                    Assert.That(importer.wrapMode,
                        Is.EqualTo(TextureWrapMode.Clamp), path);
                    Assert.That(importer.alphaIsTransparency, Is.True, path);
                    TextureImporterSettings settings =
                        new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    Assert.That(settings.spriteMeshType,
                        Is.EqualTo(SpriteMeshType.FullRect), path);
                    Assert.That(settings.spriteAlignment,
                        Is.EqualTo((int)SpriteAlignment.Custom), path);
                }
            }

            for (int i = 0; i < 8; i++)
            {
                Assert.That(leftFrames.GetArrayElementAtIndex(i)
                        .objectReferenceValue,
                    Is.SameAs(rightFrames.GetArrayElementAtIndex(i)
                        .objectReferenceValue),
                    $"Horizontal pose {i:00} must be mirrored, not replaced.");
            }

        }

        [Test]
        public void PlayerInputAlwaysResolvesToOneCardinalAxis()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Player/Player.prefab");
            GameObject player = UnityEngine.Object.Instantiate(playerPrefab);
            try
            {
                var movement = player.GetComponent<
                    TopDownGame.Player.PlayerMovement>();
                const System.Reflection.BindingFlags Flags =
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic;
                System.Reflection.MethodInfo resolve = movement.GetType()
                    .GetMethod("ResolveCardinalInput", Flags);
                Assert.That(resolve, Is.Not.Null);

                Vector2 neutralDiagonal = (Vector2)resolve.Invoke(movement,
                    new object[] { Vector2.one });
                AssertCardinal(neutralDiagonal);

                resolve.Invoke(movement, new object[] { Vector2.zero });
                resolve.Invoke(movement, new object[] { Vector2.right });
                Vector2 verticalPressedLast = (Vector2)resolve.Invoke(movement,
                    new object[] { Vector2.one });
                Assert.That(verticalPressedLast, Is.EqualTo(Vector2.up));

                resolve.Invoke(movement, new object[] { Vector2.zero });
                resolve.Invoke(movement, new object[] { Vector2.up });
                Vector2 horizontalPressedLast = (Vector2)resolve.Invoke(movement,
                    new object[] { Vector2.one });
                Assert.That(horizontalPressedLast, Is.EqualTo(Vector2.right));

                resolve.Invoke(movement, new object[] { Vector2.zero });
                Vector2 analogDominant = (Vector2)resolve.Invoke(movement,
                    new object[] { new Vector2(0.25f, 0.9f) });
                Assert.That(analogDominant, Is.EqualTo(Vector2.up));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ActiveCardinalWalkCycleContainsEightCompleteNonHoldingFrames()
        {
            string[] directions =
                { "Down", "Up", "Right" };

            foreach (string direction in directions)
            {
                string root = direction == "Right"
                    ? "Assets/Art/Generated/Characters/PlayerV10Horizontal"
                    : "Assets/Art/Generated/Characters/PlayerV6EightFrame";
                string firstFrame = $"{root}/{direction}/Walk{direction}_00.png";
                string loopCheck =
                    $"{root}/{direction}/Walk{direction}_08_LoopCheck.png";

                Assert.That(File.Exists(firstFrame), Is.True, firstFrame);
                if (File.Exists(loopCheck))
                {
                    Assert.That(File.ReadAllBytes(loopCheck),
                        Is.EqualTo(File.ReadAllBytes(firstFrame)), direction);
                }

                int minimumOpaqueHeight = int.MaxValue;
                int maximumOpaqueHeight = 0;
                List<byte[]> distinctFrames = new List<byte[]>();
                for (int i = 0; i < 8; i++)
                {
                    string current = $"{root}/{direction}/Walk{direction}_" +
                        $"{i:00}.png";
                    byte[] currentBytes = File.ReadAllBytes(current);
                    if (!distinctFrames.Any(frame =>
                        frame.SequenceEqual(currentBytes)))
                    {
                        distinctFrames.Add(currentBytes);
                    }

                    int opaqueHeight = OpaqueHeightPixels(current);
                    minimumOpaqueHeight = Mathf.Min(minimumOpaqueHeight, opaqueHeight);
                    maximumOpaqueHeight = Mathf.Max(maximumOpaqueHeight, opaqueHeight);
                    int next = (i + 1) % 8;
                    string nextFrame = $"{root}/{direction}/Walk{direction}_" +
                        $"{next:00}.png";
                    Assert.That(currentBytes,
                        Is.Not.EqualTo(File.ReadAllBytes(nextFrame)),
                        $"{direction} holds pose {i:00} at {next:00}");
                    Assert.That(SignificantlyDifferentPixelCount(current,
                            nextFrame), Is.GreaterThan(2000),
                        $"{direction} barely changes pose {i:00} at {next:00}");

                    // Opposite passing poses may intentionally share a neutral
                    // silhouette, but the active cycle still needs enough
                    // distinct artwork to read as a full walk rather than a
                    // two-frame shuffle.
                    for (int other = i + 1; other < 8; other++)
                    {
                        string comparison = $"{root}/{direction}/Walk{direction}_" +
                            $"{other:00}.png";
                        if (currentBytes.SequenceEqual(
                            File.ReadAllBytes(comparison)))
                        {
                            Assert.That(Mathf.Abs(other - i), Is.EqualTo(4),
                                $"{direction} repeats a non-passing pose " +
                                $"{i:00} at {other:00}");
                        }
                    }
                }

                int maximumHeightChange = direction == "Right" ? 8 : 3;
                Assert.That(maximumOpaqueHeight - minimumOpaqueHeight,
                    Is.LessThanOrEqualTo(maximumHeightChange),
                    $"{direction} changes silhouette height between frames. " +
                    "The side cycle may lower the hips during its down pose, " +
                    "while the vertical sheets should remain nearly level.");
                Assert.That(distinctFrames.Count, Is.GreaterThanOrEqualTo(7),
                    $"{direction} needs at least seven distinct walk poses.");
            }
        }

        [Test]
        public void ActiveHorizontalWalkHasVisibleOpposingMotion()
        {
            const string Root =
                "Assets/Art/Generated/Characters/PlayerV10Horizontal/Right";
            string first = Root + "/WalkRight_00.png";
            string opposite = Root + "/WalkRight_04.png";

            Assert.That(SignificantlyDifferentPixelCount(first, opposite),
                Is.GreaterThan(5000),
                "The side walk needs a clearly readable arm/leg swing at game scale.");
        }

        [Test]
        public void ActiveHorizontalWalkKeepsTorsoCentered()
        {
            const string Root =
                "Assets/Art/Generated/Characters/PlayerV10Horizontal/Right";
            float minimumCenter = float.MaxValue;
            float maximumCenter = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                float center = BlueClothCentroidX(
                    Root + $"/WalkRight_{i:00}.png");
                minimumCenter = Mathf.Min(minimumCenter, center);
                maximumCenter = Mathf.Max(maximumCenter, center);
            }

            Assert.That(maximumCenter - minimumCenter, Is.LessThanOrEqualTo(1f),
                "The horizontal walk torso must not drift sideways between poses.");
        }

        [Test]
        public void ProjectValidatorPassesForGeneratedBaseline()
        {
            ProjectValidationResult result = TopDownProjectValidator.ValidateProject();
            Assert.That(result.Errors.Select(error => error.Message), Is.Empty);
        }

        [Test]
        public void ProjectValidatorReportsDisabledDestinationAndSettingsAreRestored()
        {
            EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
            try
            {
                EditorBuildSettings.scenes = original.Select(scene =>
                    new EditorBuildSettingsScene(scene.path,
                        scene.enabled && !scene.path.EndsWith("/Map02.unity",
                            StringComparison.OrdinalIgnoreCase))).ToArray();

                ProjectValidationResult result = TopDownProjectValidator.ValidateProject();
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors.Any(error =>
                    error.Message.IndexOf("Map02", StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.True);
            }
            finally
            {
                EditorBuildSettings.scenes = original;
            }
        }

        private static bool IsInsideWalkableFloor(Vector2 point,
            IEnumerable<SpriteRenderer> walkableFloors)
        {
            const float BoundsTolerance = 0.03f;
            return walkableFloors.Any(renderer =>
                renderer != null &&
                point.x >= renderer.bounds.min.x - BoundsTolerance &&
                point.x <= renderer.bounds.max.x + BoundsTolerance &&
                point.y >= renderer.bounds.min.y - BoundsTolerance &&
                point.y <= renderer.bounds.max.y + BoundsTolerance);
        }

        private static void AssertWorldPosition(string sceneName,
            string markerName, Vector3 actual, Vector2 expected)
        {
            Vector2 actual2D = new Vector2(actual.x, actual.y);
            Assert.That(Vector2.Distance(actual2D, expected), Is.LessThan(0.01f),
                $"{sceneName}: {markerName} is at {actual2D}, expected {expected}.");
        }

        private static void AssertOrganicBoundaryIsClosed(string sceneName,
            Vector2 spawn, IEnumerable<Vector2> gameplayPositions,
            IEnumerable<SpriteRenderer> walkableFloors,
            IEnumerable<BoxCollider2D> mazeWalls,
            IEnumerable<BoxCollider2D> outerSafetyWalls)
        {
            const float GridStep = 0.5f;
            const float PlayerRadius = 0.45f;
            const int MaximumVisitedNodes = 30000;

            SpriteRenderer[] floors = walkableFloors
                .Where(renderer => renderer != null).ToArray();
            BoxCollider2D[] blockers = mazeWalls
                .Concat(outerSafetyWalls)
                .Where(collider => collider != null && !collider.isTrigger)
                .ToArray();
            BoxCollider2D[] safetyWalls = outerSafetyWalls
                .Where(collider => collider != null).ToArray();

            Assert.That(floors, Is.Not.Empty,
                $"{sceneName}: no walkable floor was supplied to the boundary check.");
            Assert.That(safetyWalls, Is.Not.Empty,
                $"{sceneName}: no outer safety wall was supplied to the boundary check.");

            Bounds searchBounds = safetyWalls[0].bounds;
            for (int i = 1; i < safetyWalls.Length; i++)
            {
                searchBounds.Encapsulate(safetyWalls[i].bounds);
            }

            bool IsInsideSearchBounds(Vector2 point)
            {
                return point.x >= searchBounds.min.x &&
                    point.x <= searchBounds.max.x &&
                    point.y >= searchBounds.min.y &&
                    point.y <= searchBounds.max.y;
            }

            bool IsBlocked(Vector2 point)
            {
                return blockers.Any(collider =>
                {
                    Bounds bounds = collider.bounds;
                    return point.x >= bounds.min.x - PlayerRadius &&
                        point.x <= bounds.max.x + PlayerRadius &&
                        point.y >= bounds.min.y - PlayerRadius &&
                        point.y <= bounds.max.y + PlayerRadius;
                });
            }

            Assert.That(IsInsideWalkableFloor(spawn, floors), Is.True,
                $"{sceneName}: flood-fill spawn {spawn} is outside walkable floor.");
            Assert.That(IsBlocked(spawn), Is.False,
                $"{sceneName}: flood-fill spawn {spawn} overlaps a solid wall.");

            var origin = Vector2Int.zero;
            var frontier = new Queue<Vector2Int>();
            var reachable = new HashSet<Vector2Int>();
            frontier.Enqueue(origin);
            reachable.Add(origin);
            Vector2Int[] cardinalSteps =
            {
                Vector2Int.right, Vector2Int.left,
                Vector2Int.up, Vector2Int.down
            };

            while (frontier.Count > 0)
            {
                Vector2Int cell = frontier.Dequeue();
                Vector2 worldPoint = spawn + new Vector2(
                    cell.x * GridStep, cell.y * GridStep);
                Assert.That(IsInsideWalkableFloor(worldPoint, floors), Is.True,
                    $"{sceneName}: organic wall boundary leaks near {worldPoint}.");

                foreach (Vector2Int direction in cardinalSteps)
                {
                    Vector2Int neighbour = cell + direction;
                    if (reachable.Contains(neighbour))
                    {
                        continue;
                    }

                    Vector2 neighbourPoint = spawn + new Vector2(
                        neighbour.x * GridStep, neighbour.y * GridStep);
                    if (!IsInsideSearchBounds(neighbourPoint) ||
                        IsBlocked(neighbourPoint))
                    {
                        continue;
                    }

                    reachable.Add(neighbour);
                    frontier.Enqueue(neighbour);
                    Assert.That(reachable.Count,
                        Is.LessThanOrEqualTo(MaximumVisitedNodes),
                        $"{sceneName}: boundary flood fill exceeded " +
                        $"{MaximumVisitedNodes} nodes near {neighbourPoint}.");
                }
            }

            foreach (Vector2 gameplayPosition in gameplayPositions)
            {
                Vector2 offset = (gameplayPosition - spawn) / GridStep;
                var targetCell = new Vector2Int(Mathf.RoundToInt(offset.x),
                    Mathf.RoundToInt(offset.y));
                Vector2 sampledPosition = spawn + new Vector2(
                    targetCell.x * GridStep, targetCell.y * GridStep);
                Assert.That(Vector2.Distance(sampledPosition, gameplayPosition),
                    Is.LessThan(0.01f),
                    $"{sceneName}: gameplay marker {gameplayPosition} is not aligned " +
                    $"to the {GridStep}-unit reachability grid.");
                Assert.That(reachable.Contains(targetCell), Is.True,
                    $"{sceneName}: gameplay marker {gameplayPosition} is not " +
                    "cardinally reachable from the player spawn.");
            }
        }

        private static T[] LoadAssets<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToArray();
        }

        private static void AssertCardinal(Vector2 direction)
        {
            bool horizontal = Mathf.Abs(direction.x) > 0.001f;
            bool vertical = Mathf.Abs(direction.y) > 0.001f;
            Assert.That(horizontal ^ vertical, Is.True,
                $"Expected one cardinal axis, got {direction}.");
            Assert.That(direction.magnitude,
                Is.EqualTo(1f).Within(0.001f));
        }

        private static float OpaqueBottomWorldOffset(string path, Sprite sprite)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(path), false), Is.True,
                    path);
                Color32[] pixels = texture.GetPixels32();
                for (int y = 0; y < texture.height; y++)
                {
                    int rowStart = y * texture.width;
                    for (int x = 0; x < texture.width; x++)
                    {
                        if (pixels[rowStart + x].a > 10)
                        {
                            return (y - sprite.pivot.y) / sprite.pixelsPerUnit;
                        }
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            Assert.Fail("No opaque pixels found: " + path);
            return 0f;
        }

        private static int OpaqueHeightPixels(string path)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(path), false), Is.True,
                    path);
                Color32[] pixels = texture.GetPixels32();
                int minimumY = texture.height;
                int maximumY = -1;
                for (int y = 0; y < texture.height; y++)
                {
                    int rowStart = y * texture.width;
                    for (int x = 0; x < texture.width; x++)
                    {
                        if (pixels[rowStart + x].a <= 10)
                        {
                            continue;
                        }

                        minimumY = Mathf.Min(minimumY, y);
                        maximumY = Mathf.Max(maximumY, y);
                    }
                }

                Assert.That(maximumY, Is.GreaterThanOrEqualTo(minimumY), path);
                return maximumY - minimumY + 1;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static int SignificantlyDifferentPixelCount(string firstPath,
            string secondPath)
        {
            Texture2D first = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D second = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(first.LoadImage(File.ReadAllBytes(firstPath), false),
                    Is.True, firstPath);
                Assert.That(second.LoadImage(File.ReadAllBytes(secondPath), false),
                    Is.True, secondPath);
                Assert.That(second.width, Is.EqualTo(first.width));
                Assert.That(second.height, Is.EqualTo(first.height));

                Color32[] firstPixels = first.GetPixels32();
                Color32[] secondPixels = second.GetPixels32();
                int changed = 0;
                for (int i = 0; i < firstPixels.Length; i++)
                {
                    Color32 a = firstPixels[i];
                    Color32 b = secondPixels[i];
                    int difference = Mathf.Abs(a.r - b.r) +
                        Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) +
                        Mathf.Abs(a.a - b.a);
                    if (difference > 80)
                    {
                        changed++;
                    }
                }

                return changed;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        private static float BlueClothCentroidX(string path)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(path), false),
                    Is.True, path);
                long weightedX = 0;
                int count = 0;
                Color32[] pixels = texture.GetPixels32();
                for (int y = 0; y < texture.height; y++)
                {
                    int rowStart = y * texture.width;
                    for (int x = 0; x < texture.width; x++)
                    {
                        Color32 color = pixels[rowStart + x];
                        if (color.a <= 10 || color.b <= 90 ||
                            color.b <= color.r * 1.1f || color.b <= color.g)
                        {
                            continue;
                        }

                        weightedX += x;
                        count++;
                    }
                }

                Assert.That(count, Is.GreaterThan(0), path);
                return weightedX / (float)count;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
