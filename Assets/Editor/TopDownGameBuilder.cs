using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TopDownGame.Enemies.Bosses;
using TopDownGame.Enemies;
using TopDownGame.Enemies.Spawning;
using TopDownGame.Equipment;
using TopDownGame.Items;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Skills;
using TopDownGame.UI;
using TopDownGame.Core;
using TopDownGame.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopDownGame.EditorTools
{
    public static partial class TopDownGameBuilder
    {
        private const string Root = "Assets";
        private const string DataRoot = Root + "/ScriptableObjects";
        private const string PrefabRoot = Root + "/Prefabs";
        private const string SceneRoot = Root + "/Scenes";
        private const string CharacterArtRoot = Root + "/Art/Generated/Characters";
        private const string SmoothPlayerRunArtRoot =
            CharacterArtRoot + "/PlayerV3Smooth";
        private const string DirectionalPlayerWalkArtRoot =
            CharacterArtRoot + "/PlayerV4Walk";
        private const string DiagonalPlayerWalkArtRoot =
            CharacterArtRoot + "/PlayerV5Diagonal";
        private const string LegacyEightFramePlayerWalkArtRoot =
            CharacterArtRoot + "/PlayerV6EightFrame";
        private const string LegacySeamlessPlayerWalkArtRoot =
            CharacterArtRoot + "/PlayerV7Seamless";
        private const string ExpressiveHorizontalPlayerWalkArtRoot =
            CharacterArtRoot + "/PlayerV8Biomechanical";
        private const string EightFramePlayerWalkArtRoot =
            CharacterArtRoot + "/PlayerV9Complete";
        private const string StableHorizontalPlayerWalkArtRoot =
            CharacterArtRoot + "/PlayerV10Horizontal";
        private const string PropsArtRoot = Root + "/Art/Generated/Props";
        private const string SkillIconArtRoot = Root + "/Art/Generated/SkillIcons";
        private const string EnvironmentArtRoot = Root + "/Art/Generated/Environment";
        private const string EquipmentIconArtRoot = Root + "/Art/Generated/EquipmentIcons";
        private const string EquipmentUIArtRoot = Root + "/Art/Generated/UI";
        private const string LobbyArtRoot = Root + "/Art/Generated/Lobby";
        private const string RestoredLobbyArt = LobbyArtRoot + "/WoodlandWaystation_RestoredV1.png";
        private const string RestoredMap02Art = EnvironmentArtRoot + "/Map02_AncientWoodland_RestoredV1.png";
        private static readonly Vector3 LobbyForgePosition = new Vector3(-1.15f, 1.85f, 0f);
        private static readonly Vector3 LobbyStoragePosition = new Vector3(5.85f, 1.65f, 0f);
        // V7 artwork: centre of the engraved dais at source pixel (1353, 498).
        private static readonly Vector3 LobbyPortalPosition = new Vector3(8.73f, -0.20f, 0f);
        private sealed class MazeLayout
        {
            public Vector2 ArenaSize;
            public Vector3 PlayerSpawn;
            public Vector3 ExitPosition;
            public Vector3 ChestPosition;
            public Vector3 ItemPosition;
            public Vector3 BossPosition;
            public Vector3 LandmarkPosition;
            public Vector2 LandmarkColliderSize;
            public MazeLobe[] Lobes = new MazeLobe[0];
            public MazeCorridor[] Corridors = new MazeCorridor[0];
            public MazeClutter[] Clutter = new MazeClutter[0];
            public Vector3[] PrimaryEnemyPositions = new Vector3[0];
            public Vector3[] SecondaryEnemyPositions = new Vector3[0];
        }

        private struct MazeArea
        {
            public Vector2 Center;
            public Vector2 Size;

            public MazeArea(float x, float y, float width, float height)
            {
                Center = new Vector2(x, y);
                Size = new Vector2(width, height);
            }
        }

        private struct MazeLobe
        {
            public Vector2 Center;
            public Vector2 Radius;

            public MazeLobe(float x, float y, float radiusX, float radiusY)
            {
                Center = new Vector2(x, y);
                Radius = new Vector2(radiusX, radiusY);
            }
        }

        private sealed class MazeCorridor
        {
            public float Width;
            public Vector2[] Points;

            public MazeCorridor(float width, params Vector2[] points)
            {
                Width = width;
                Points = points ?? new Vector2[0];
            }
        }

        private enum MazeClutterKind
        {
            Tree,
            Rock,
            FallenLog,
            Shrine,
            Supplies,
            Crystal,
            Landmark
        }

        private struct MazeClutter
        {
            public int Group;
            public MazeClutterKind Kind;
            public Vector2 Position;
            public float Height;
            public bool FlipX;

            public MazeClutter(int group, MazeClutterKind kind, float x,
                float y, float height, bool flipX = false)
            {
                Group = Mathf.Clamp(group, 0, 7);
                Kind = kind;
                Position = new Vector2(x, y);
                Height = height;
                FlipX = flipX;
            }
        }

        private sealed class MazeMask
        {
            public const float CellSize = 0.5f;
            public readonly bool[,] Walkable;
            public readonly int Width;
            public readonly int Height;
            public readonly Vector2 Minimum;

            public MazeMask(Vector2 arenaSize)
            {
                Width = Mathf.CeilToInt(arenaSize.x / CellSize);
                Height = Mathf.CeilToInt(arenaSize.y / CellSize);
                Minimum = -arenaSize * 0.5f;
                Walkable = new bool[Width, Height];
            }

            public Vector2 CellCenter(int x, int y)
            {
                return Minimum + new Vector2((x + 0.5f) * CellSize,
                    (y + 0.5f) * CellSize);
            }

            public bool Contains(int x, int y)
            {
                return x >= 0 && x < Width && y >= 0 && y < Height;
            }
        }

        [MenuItem("Top Down Game/Open Main Menu", priority = 0)]
        public static void OpenMainMenu()
        {
            string mainMenuPath = SceneRoot + "/MainMenu.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(mainMenuPath) == null)
            {
                Debug.LogError("Main menu scene is missing. Run Top Down Game/Build Complete MVP first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(mainMenuPath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(mainMenuPath);
        }

        [MenuItem("Top Down Game/Edit Map01", priority = 1)]
        public static void EditMainMap()
        {
            string mapPath = SceneRoot + "/Map01.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(mapPath) == null)
            {
                Debug.LogError("Map01 is missing. Run Top Down Game/Build Complete MVP first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(mapPath);
            GameObject environment = GameObject.Find("Environment");
            Selection.activeGameObject = environment;
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.in2DMode = true;
                sceneView.FrameSelected();
                sceneView.Repaint();
            }
        }

        [MenuItem("Top Down Game/Build Complete MVP")]
        public static void BuildCompleteMvp()
        {
            if (!Application.isBatchMode && GeneratedScenesExist() &&
                !EditorUtility.DisplayDialog("Rebuild the whole game?",
                    "Build Complete MVP recreates MainMenu, Lobby and every map. " +
                    "Any scene edits will be overwritten. Use 'Rebuild Player " +
                    "Prefab (Keep Scenes)' for movement or animation changes.",
                    "Overwrite all scenes", "Cancel"))
            {
                return;
            }

            BuildCompleteMvpAutomated();
        }

        /// <summary>
        /// Rebuilds generated content without an interactive confirmation so
        /// automated development and verification can run the source of truth.
        /// </summary>
        [MenuItem("Top Down Game/Build Complete Automated _F12")]
        public static void BuildCompleteMvpAutomated()
        {

            EnsureFolders();
            PrepareVietnameseFont();
            PrepareGeneratedArt();
            SkillData[] skills = CreateSkills();
            EquipmentData[] equipment = CreateEquipmentCatalog();
            EnemyData slime = CreateEnemy("Slime", EnemyArchetype.Slime, 90f, 8f, 2f, 1.2f, 1.1f, 3, new Color(0.25f, 0.9f, 0.35f));
            EnemyData goblin = CreateEnemy("Goblin", EnemyArchetype.Goblin, 140f, 12f, 3.2f, 1.2f, 1.2f, 5, new Color(0.55f, 0.9f, 0.25f));
            EnemyData archer = CreateEnemy("Archer", EnemyArchetype.Archer, 100f, 10f, 2.2f, 6f, 7f, 6, new Color(0.95f, 0.75f, 0.25f));
            EnemyData mage = CreateEnemy("Mage", EnemyArchetype.Mage, 120f, 16f, 1.8f, 7f, 8f, 8, new Color(0.75f, 0.35f, 1f));
            EnemyData tank = CreateEnemy("Tank", EnemyArchetype.Tank, 360f, 25f, 1f, 1.5f, 1.5f, 15, new Color(0.45f, 0.48f, 0.55f));
            EnemyData crystalScorpion = CreateEnemy("CrystalScorpion",
                EnemyArchetype.CrystalScorpion, 210f, 18f, 2.5f, 2.4f,
                4.6f, 12, new Color(0.12f, 0.82f, 1f), 14f);
            Set(crystalScorpion, "displayName", "Bọ Cạp Pha Lê");
            EnemyData voidReaver = CreateEnemy("VoidReaver",
                EnemyArchetype.VoidReaver, 420f, 28f, 3.15f, 2.2f,
                1.65f, 24, new Color(0.56f, 0.18f, 1f), 18f);
            Set(voidReaver, "displayName", "Đao Phủ Hư Không");
            EnemyData riftMage = CreateEnemy("RiftMage",
                EnemyArchetype.Mage, 245f, 24f, 2.05f, 5.2f,
                7.5f, 18, new Color(0.62f, 0.18f, 1f), 9f);
            Set(riftMage, "displayName", "Pháp Sư Khe Nứt");
            EnemyData obsidianGolem = CreateEnemy("ObsidianGolem",
                EnemyArchetype.Tank, 680f, 34f, 1.25f, 2.2f,
                1.7f, 32, new Color(0.28f, 0.12f, 0.42f), 34f);
            Set(obsidianGolem, "displayName", "Vệ Binh Hắc Diện");
            EnemyData boss = CreateEnemy("Forest Guardian", EnemyArchetype.Boss, 1200f, 35f, 1.1f, 5f, 2.3f, 100, new Color(0.7f, 0.15f, 0.2f));

            ItemData forestRelic = CreateItem("forest_relic", "Di Vật Rừng", "Vật kỷ niệm từ Vệ Thần Rừng.");
            CreatePlayerPrefab(skills, equipment);
            GameObject slimePrefab = CreateEnemyPrefab("Slime", slime,
                typeof(SlimeEnemy), new Color(0.25f, 0.9f, 0.35f));
            GameObject goblinPrefab = CreateEnemyPrefab("Goblin", goblin,
                typeof(ForestGoblinEnemy), new Color(0.55f, 0.9f, 0.25f));
            GameObject archerPrefab = CreateEnemyPrefab("Archer", archer,
                typeof(ForestArcherEnemy), new Color(0.95f, 0.75f, 0.25f));
            GameObject magePrefab = CreateEnemyPrefab("Mage", mage, typeof(MageEnemy), new Color(0.75f, 0.35f, 1f));
            GameObject tankPrefab = CreateEnemyPrefab("Tank", tank,
                typeof(TankEnemy), new Color(0.45f, 0.48f, 0.55f));
            GameObject crystalScorpionPrefab = CreateEnemyPrefab(
                "CrystalScorpion", crystalScorpion,
                typeof(CrystalScorpionEnemy), new Color(0.12f, 0.82f, 1f));
            GameObject voidReaverPrefab = CreateEnemyPrefab("VoidReaver",
                voidReaver, typeof(VoidReaverEnemy),
                new Color(0.56f, 0.18f, 1f));
            GameObject riftMagePrefab = CreateEnemyPrefab("RiftMage",
                riftMage, typeof(MageEnemy), new Color(0.62f, 0.28f, 1f),
                "Mage", true);
            GameObject obsidianGolemPrefab = CreateEnemyPrefab("ObsidianGolem",
                obsidianGolem, typeof(TankEnemy),
                new Color(0.42f, 0.2f, 0.62f), "Tank", true);
            GameObject bossPrefab = CreateEnemyPrefab("ForestGuardian", boss, typeof(ForestGuardianBoss), new Color(0.7f, 0.15f, 0.2f));

            MapData map01 = CreateMap("Map01", "Đầm Lầy Dịch Xanh",
                "Map02", false, false);
            MapData map02 = CreateMap("Map02", "Tàn Tích Suối Ngọc", "Map03", false, false);
            MapData map03 = CreateMap("Map03", "Đền Kỹ Năng", "BossMap01", false, true);
            MapData bossMap = CreateMap("BossMap01", "Lãnh Địa Vệ Thần", "CaveMap01", true, false);
            MapData caveMap = CreateMap("CaveMap01", "Hang Pha Lê", "RiftMap01", false, false);
            MapData riftMap = CreateMap("RiftMap01", "Tàn Tích Nhật Thực", "Lobby", false, true);

            CreateCombatScene("Map01", map01, slimePrefab, 10, null, skills, forestRelic, "Map02");
            CreateCombatScene("Map02", map02, goblinPrefab, 8, archerPrefab, skills, forestRelic, "Map03");
            CreateCombatScene("Map03", map03, archerPrefab, 8, magePrefab, skills, forestRelic, "BossMap01");
            CreateBossScene("BossMap01", bossMap, bossPrefab, slimePrefab, skills, forestRelic);
            CreateCombatScene("CaveMap01", caveMap, magePrefab, 3, tankPrefab, skills,
                forestRelic, "RiftMap01", crystalScorpionPrefab, 3);
            CreateCombatScene("RiftMap01", riftMap, voidReaverPrefab, 5,
                riftMagePrefab, skills, forestRelic, "Lobby",
                obsidianGolemPrefab, 3);
            CreateLobbyScene(skills, equipment);
            CreateMainMenu();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneRoot + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/Lobby.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/Map01.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/Map02.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/Map03.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/BossMap01.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/CaveMap01.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/RiftMap01.unity", true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(SceneRoot + "/MainMenu.unity");
            Debug.Log("Top Down Game MVP built: MainMenu, Lobby, combat maps and weapon shop.");
        }

        [MenuItem("Top Down Game/Rebuild Deep Forest Animation")]
        public static void RebuildDeepForestAnimationAutomated()
        {
            EnsureFolders();
            PrepareForestAnimationSheet("GoblinAnimationSheetV2");
            PrepareForestAnimationSheet("ArcherAnimationSheetV2");

            EnemyData goblin = AssetDatabase.LoadAssetAtPath<EnemyData>(
                DataRoot + "/Enemies/Goblin.asset");
            EnemyData archer = AssetDatabase.LoadAssetAtPath<EnemyData>(
                DataRoot + "/Enemies/Archer.asset");
            MapData map02 = AssetDatabase.LoadAssetAtPath<MapData>(
                DataRoot + "/Maps/Map02.asset");
            ItemData forestRelic = AssetDatabase.LoadAssetAtPath<ItemData>(
                DataRoot + "/Items/forest_relic.asset");
            if (goblin == null || archer == null || map02 == null ||
                forestRelic == null)
            {
                throw new InvalidOperationException(
                    "Deep Forest data assets are incomplete.");
            }

            GameObject goblinPrefab = CreateEnemyPrefab("Goblin", goblin,
                typeof(ForestGoblinEnemy), new Color(0.55f, 0.9f, 0.25f));
            GameObject archerPrefab = CreateEnemyPrefab("Archer", archer,
                typeof(ForestArcherEnemy), new Color(0.95f, 0.75f, 0.25f));
            SkillData[] skills = LoadAssets<SkillData>(DataRoot + "/Skills");
            CreateCombatScene("Map02", map02, goblinPrefab, 8,
                archerPrefab, skills, forestRelic, "Map03");

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SceneRoot + "/MainMenu.unity",
                OpenSceneMode.Single);
            Debug.Log("Deep Forest animation rebuilt: articulated feet and " +
                "hand-action frames assigned to Goblin and Archer.");
        }

        [MenuItem("Top Down Game/Rebuild Player Prefab (Keep Scenes)", priority = 2)]
        public static void RebuildPlayerPrefabOnly()
        {
            EnsureFolders();
            PrepareGeneratedArt();
            SkillData[] skills = LoadAssets<SkillData>(DataRoot + "/Skills");
            EquipmentData[] equipment = LoadAssets<EquipmentData>(
                DataRoot + "/Equipment");
            CreatePlayerPrefab(skills, equipment);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Player prefab rebuilt. Existing Lobby and map scenes were kept.");
        }

        [MenuItem("Top Down Game/Rebuild Lobby Scene Only", priority = 3)]
        public static void RebuildLobbySceneOnly()
        {
            EnsureFolders();
            PrepareGeneratedArt();
            SkillData[] skills = LoadAssets<SkillData>(DataRoot + "/Skills");
            EquipmentData[] equipment = LoadAssets<EquipmentData>(
                DataRoot + "/Equipment");
            CreateLobbyScene(skills, equipment);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(SceneRoot + "/Lobby.unity");
            Debug.Log("Lobby scene rebuilt with generated decor. Other scenes were kept.");
        }

        [MenuItem("Top Down Game/Rebuild Combat Maps Only", priority = 4)]
        public static void RebuildCombatMapsOnly()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureGeneratedSprites(PropsArtRoot, 768f, TextureWrapMode.Clamp);

            SkillData[] skills = LoadAssets<SkillData>(DataRoot + "/Skills");
            ItemData forestRelic = AssetDatabase.LoadAssetAtPath<ItemData>(
                DataRoot + "/Items/forest_relic.asset");
            if (forestRelic != null)
            {
                Set(forestRelic, "icon", GeneratedPropSprite(
                    "ForestRelicPickupV1"));
            }
            GameObject slime = LoadEnemyPrefab("Slime");
            GameObject goblin = LoadEnemyPrefab("Goblin");
            GameObject archer = LoadEnemyPrefab("Archer");
            GameObject mage = LoadEnemyPrefab("Mage");
            GameObject tank = LoadEnemyPrefab("Tank");
            GameObject crystalScorpion = LoadEnemyPrefab("CrystalScorpion");
            GameObject voidReaver = LoadEnemyPrefab("VoidReaver");
            GameObject riftMage = LoadEnemyPrefab("RiftMage");
            GameObject obsidianGolem = LoadEnemyPrefab("ObsidianGolem");
            GameObject boss = LoadEnemyPrefab("ForestGuardian");

            CreateCombatScene("Map01", LoadMapData("Map01"), slime, 10,
                null, skills, forestRelic, "Map02");
            CreateCombatScene("Map02", LoadMapData("Map02"), goblin, 8,
                archer, skills, forestRelic, "Map03");
            CreateCombatScene("Map03", LoadMapData("Map03"), archer, 8,
                mage, skills, forestRelic, "BossMap01");
            CreateBossScene("BossMap01", LoadMapData("BossMap01"), boss,
                slime, skills, forestRelic);
            CreateCombatScene("CaveMap01", LoadMapData("CaveMap01"), mage,
                3, tank, skills, forestRelic, "RiftMap01", crystalScorpion, 3);
            CreateCombatScene("RiftMap01", LoadMapData("RiftMap01"),
                voidReaver, 5, riftMage, skills, forestRelic, "Lobby",
                obsidianGolem, 3);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(SceneRoot + "/Map01.unity");
            Debug.Log("All combat maps rebuilt with generated decor. Lobby and MainMenu were kept.");
        }

        private static MapData LoadMapData(string id)
        {
            MapData asset = AssetDatabase.LoadAssetAtPath<MapData>(
                DataRoot + "/Maps/" + id + ".asset");
            if (asset == null) return null;
            Set(asset, "recommendedMinLevel", MapData.RecommendedMinimumForScene(id));
            int maximum = id.StartsWith("Rift", StringComparison.OrdinalIgnoreCase)
                ? 75 : id.StartsWith("Cave", StringComparison.OrdinalIgnoreCase)
                ? 50 : id.StartsWith("Boss", StringComparison.OrdinalIgnoreCase)
                    ? 65 : id == "Map02" ? 20 : id == "Map03" ? 35 : 10;
            int target = asset.IsBossMap ? 1 : id.StartsWith("Rift",
                StringComparison.OrdinalIgnoreCase) ? 22 : id.StartsWith("Cave",
                StringComparison.OrdinalIgnoreCase) ? 20 :
                id == "Map02" ? 16 : id == "Map03" ? 18 : 14;
            Set(asset, "recommendedMaxLevel", maximum);
            Set(asset, "targetActiveEnemies", target);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static GameObject LoadEnemyPrefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabRoot + "/Enemies/" + name + ".prefab");
        }

        private static bool GeneratedScenesExist()
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(
                SceneRoot + "/Lobby.unity") != null;
        }

        private static T[] LoadAssets<T>(string folder) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name,
                new[] { folder });
            string[] paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
            T[] assets = new T[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                assets[i] = AssetDatabase.LoadAssetAtPath<T>(paths[i]);
            }

            return assets;
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "ScriptableObjects/Skills"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "ScriptableObjects/Enemies"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "ScriptableObjects/Items"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "ScriptableObjects/Maps"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "ScriptableObjects/Equipment"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs/Player"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs/Enemies"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
            AssetDatabase.Refresh();
            EnsureGameplayLayers();
        }

        private static void EnsureGameplayLayers()
        {
            EnsureLayer(GameplayCollisionRules.PlayerLayerName, 8);
            EnsureLayer(GameplayCollisionRules.EnemyLayerName, 9);
            GameplayCollisionRules.Apply();
        }

        private static int EnsureLayer(string layerName, int preferredIndex)
        {
            UnityEngine.Object[] tagManagerAssets =
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                throw new InvalidOperationException("TagManager.asset is unavailable.");
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    return i;
                }
            }

            int targetIndex = preferredIndex;
            if (targetIndex < 8 || targetIndex >= layers.arraySize ||
                !string.IsNullOrEmpty(layers.GetArrayElementAtIndex(targetIndex).stringValue))
            {
                targetIndex = -1;
                for (int i = 8; i < layers.arraySize; i++)
                {
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    {
                        targetIndex = i;
                        break;
                    }
                }
            }

            if (targetIndex < 0)
            {
                throw new InvalidOperationException("No free gameplay layer is available for " +
                    layerName + ".");
            }

            layers.GetArrayElementAtIndex(targetIndex).stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return targetIndex;
        }

        private static SkillData[] CreateSkills()
        {
            return new[]
            {
                CreateSkill("Fireball", SkillId.Fireball, "Cầu Lửa", "Bắn cầu lửa nổ lan tại điểm va chạm.", 40f, 15f, 1.6f, 6f, new Color(1f, 0.25f, 0.1f)),
                CreateSkill("IceDash", SkillId.IceDash, "Lướt Băng", "Lướt xuyên và làm chậm kẻ địch.", 35f, 20f, 3.4f, 4f, new Color(0.25f, 0.8f, 1f)),
                CreateSkill("SwordSpin", SkillId.SwordSpin, "Kiếm Luân", "Triệu hồi vòng kiếm vàng liên tục cắt kẻ địch xung quanh.", 50f, 22f, 2.8f, 2f, new Color(1f, 0.72f, 0.08f)),
                CreateSkill("LightningStrike", SkillId.LightningStrike, "Thiên Lôi", "Gọi sét đánh xuống vùng đang chỉ định.", 55f, 30f, 4.8f, 7f, new Color(1f, 0.72f, 0.05f)),
                CreateSkill("Healing", SkillId.Healing, "Hồi Phục", "Hồi một phần sinh lực đã mất.", 0f, 25f, 7f, 0f, new Color(0.3f, 1f, 0.45f)),
                CreateSkill("Shield", SkillId.Shield, "Hộ Thuẫn", "Giảm sát thương nhận vào trong thời gian ngắn.", 0f, 20f, 8f, 0f, new Color(0.45f, 0.55f, 1f)),
                CreateSkill("ChainLightning", SkillId.ChainLightning, "Sét Liên Hoàn", "Sét nhảy qua tối đa ba mục tiêu, giảm 25% sát thương mỗi lần lan.", 62f, 28f, 4.2f, 7f, new Color(0.35f, 0.78f, 1f)),
                CreateSkill("MeteorShower", SkillId.MeteorShower, "Mưa Thiên Thạch", "Đánh dấu một vùng rồi dội nhiều thiên thạch chồng sát thương.", 72f, 36f, 6.5f, 8f, new Color(1f, 0.24f, 0.04f)),
                CreateSkill("VoidPulse", SkillId.VoidPulse, "Xung Kích Hư Không", "Gây sát thương và kéo kẻ địch gần vào tâm vụ nổ.", 58f, 26f, 4.6f, 3.2f, new Color(0.55f, 0.16f, 1f))
            };
        }

        private static SkillData CreateSkill(string fileName, SkillId id, string displayName,
            string description, float damage, float mana, float cooldown, float range, Color color)
        {
            string path = DataRoot + "/Skills/" + fileName + ".asset";
            SkillData asset = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            Set(asset, "skillId", id);
            Set(asset, "displayName", displayName);
            Set(asset, "description", description);
            Set(asset, "damage", damage);
            Set(asset, "manaCost", mana);
            Set(asset, "cooldown", cooldown);
            Set(asset, "range", range);
            Set(asset, "maxLevel", 5);
            Set(asset, "effectColor", color);
            string visualPath=DataRoot+"/Skills/"+fileName+"Presentation.asset";
            var visual=AssetDatabase.LoadAssetAtPath<SkillPresentationProfile>(visualPath);
            if(visual==null){visual=ScriptableObject.CreateInstance<SkillPresentationProfile>();AssetDatabase.CreateAsset(visual,visualPath);}
            SpellElement element=id==SkillId.IceDash ? SpellElement.Ice : id==SkillId.SwordSpin ? SpellElement.Blade :
                id==SkillId.LightningStrike || id==SkillId.ChainLightning ? SpellElement.Lightning :
                id==SkillId.Healing ? SpellElement.Nature : id==SkillId.Shield ? SpellElement.Ward :
                id==SkillId.VoidPulse ? SpellElement.Void : SpellElement.Fire;
            Set(visual,"element",(int)element);
            Set(visual,"shader",AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Shaders/ElementalSpell.shader"));
            Set(visual,"primary",element==SpellElement.Lightning ? new Color(0.2f,0.65f,1f) : color);
            Set(visual,"core",element==SpellElement.Fire ? new Color(1f,0.95f,0.65f) : Color.white);
            Set(visual,"windup",id==SkillId.IceDash ? 0.08f : id==SkillId.MeteorShower ? 0.25f : 0.18f);
            Set(visual,"recovery",id==SkillId.IceDash ? 0.17f : 0.22f);
            string paintedName=id==SkillId.ChainLightning || id==SkillId.LightningStrike ? "Lightning" :
                id==SkillId.MeteorShower ? "Meteor" : id==SkillId.VoidPulse ? "Void" : id.ToString();
            if(id!=SkillId.SwordSpin)
            {
                SetArray(visual,"paintedFrames",CreatePaintedAtlas(SkillIconArtRoot+"/"+paintedName+"PaintedV4.png",paintedName+"V4",3,2,false));
                Set(visual,"paintedMaterial",SwordMaterial("SpellPaintedV4","PaintedSpell"));
                SetArray(visual,"actorFrames",CreatePaintedAtlas(CharacterArtRoot+"/PlayerMagicCastV4.png","MagicActorV4",3,2,true));
                Set(visual,"actorMaterial",SwordMaterial("SwordActorV3","SwordActorMatte"));
                Set(visual,"releasePose",id==SkillId.MeteorShower || id==SkillId.LightningStrike ? 3 :
                    id==SkillId.Healing || id==SkillId.Shield || id==SkillId.VoidPulse ? 4 : 2);
            }
            Set(asset,"presentation",visual);
            Set(asset,"icon",CreatePaintedAtlas(SkillIconArtRoot+"/SkillIconsPaintedV4.png","SkillIconV4",3,3,false)[(int)id]);
            return asset;
        }

        private static Sprite[] CreatePaintedAtlas(string path,string name,int columns,int rows,bool actor)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.maxTextureSize!=2048 || importer.textureCompression!=TextureImporterCompression.Uncompressed)
            {
                importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            }
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            float width=texture.width/(float)columns,height=texture.height/(float)rows;
            var frames=new Sprite[columns*rows];
            for(int i=0;i<frames.Length;i++)
            {
                string assetPath="Assets/Settings/"+name+"_"+i+".asset";
                float ppu=actor ? 260 : 200;
                float[] centers={.5f,.4f,.36f,.5f,.42f,.29f};
                Vector2 pivot=actor ? new Vector2(centers[i],(i<3?.09f:.23f)+.52f*ppu/height) : Vector2.one*.5f;
                var sprite=Sprite.Create(texture,new Rect((i%columns)*width,(rows-1-i/columns)*height,width,height),pivot,ppu,0,SpriteMeshType.FullRect);
                sprite.name=name+"_"+i;
                frames[i]=AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if(frames[i]==null){frames[i]=sprite;AssetDatabase.CreateAsset(sprite,assetPath);}
                else{EditorUtility.CopySerialized(sprite,frames[i]);UnityEngine.Object.DestroyImmediate(sprite);EditorUtility.SetDirty(frames[i]);}
            }
            return frames;
        }

        private static EquipmentData[] CreateEquipmentCatalog()
        {
            return new[]
            {
                CreateEquipment("steel_sword", "Kiếm Thép", WeaponType.Sword,
                    "Cân bằng, dễ sử dụng.", "SteelSword", 200,
                    8f, 2f, 0f, 0f, 0f, 0f),
                CreateEquipment("greatsword", "Đại Kiếm", WeaponType.Greatsword,
                    "Sát thương rất cao nhưng hơi nặng.", "Greatsword", 450,
                    18f, 0f, 0f, -0.25f, 0.02f, -0.03f),
                CreateEquipment("broadsword", "Kiếm Rộng", WeaponType.Broadsword,
                    "Lưỡi kiếm rộng tăng cả công lẫn sinh lực.", "Broadsword", 380,
                    12f, 3f, 25f, 0f, 0f, 0f),
                CreateEquipment("battle_axe", "Rìu Chiến", WeaponType.BattleAxe,
                    "Đòn chém mạnh với tỉ lệ chí mạng cao.", "BattleAxe", 500,
                    17f, 0f, 0f, -0.1f, 0.05f, 0f),
                CreateEquipment("war_hammer", "Búa Chiến", WeaponType.WarHammer,
                    "Búa nặng giúp tăng phòng thủ và sinh lực.", "WarHammer", 520,
                    14f, 10f, 15f, -0.15f, 0f, 0f),
                CreateEquipment("long_spear", "Thương Dài", WeaponType.Spear,
                    "Vũ khí linh hoạt, cân bằng tốc độ và sát thương.", "LongSpear", 420,
                    11f, 1f, 0f, 0.2f, 0.03f, 0.04f),
                CreateEquipment("dual_blades", "Song Đao", WeaponType.DualBlades,
                    "Tấn công nhanh và rút ngắn hồi chiêu.", "DualBlades", 600,
                    10f, 0f, 0f, 0.35f, 0.06f, 0.15f),
                CreateEquipment("dagger", "Dao Găm", WeaponType.Dagger,
                    "Nhẹ, nhanh và có khả năng chí mạng cao.", "Dagger", 350,
                    7f, 0f, 0f, 0.45f, 0.12f, 0.08f),

                CreateEquipmentPiece("iron_helmet", "Mũ Sắt", EquipmentSlot.Helmet,
                    "Bảo vệ cơ bản.", "IronHelmet", 200, 0f, 3f, 10f, 0f, 0f, 0f),
                CreateEquipmentPiece("warrior_helmet", "Mũ Chiến Binh", EquipmentSlot.Helmet,
                    "Cân bằng phòng thủ.", "WarriorHelmet", 280, 0f, 4f, 15f, 0f, 0f, 0f),
                CreateEquipmentPiece("guardian_helmet", "Mũ Hộ Vệ", EquipmentSlot.Helmet,
                    "Tăng phòng thủ và hồi chiêu.", "GuardianHelmet", 360, 0f, 6f, 20f, 0f, 0f, 0.02f),
                CreateEquipmentPiece("knight_helmet", "Mũ Kỵ Sĩ", EquipmentSlot.Helmet,
                    "Giáp đầu nặng và chắc chắn.", "KnightHelmet", 450, 0f, 8f, 25f, -0.05f, 0f, 0f),
                CreateEquipmentPiece("lion_helmet", "Mũ Sư Tử", EquipmentSlot.Helmet,
                    "Uy dũng, tăng cả công và thủ.", "LionHelmet", 520, 4f, 6f, 30f, 0f, 0.02f, 0f),
                CreateEquipmentPiece("destroyer_helmet", "Mũ Hủy Diệt", EquipmentSlot.Helmet,
                    "Gia tăng sức tấn công.", "DestroyerHelmet", 600, 6f, 8f, 0f, 0f, 0.03f, 0f),
                CreateEquipmentPiece("gold_helmet", "Mũ Vàng", EquipmentSlot.Helmet,
                    "Kết hợp tấn công và phòng thủ.", "GoldHelmet", 700, 5f, 10f, 40f, 0f, 0.02f, 0f),
                CreateEquipmentPiece("divine_helmet", "Mũ Thần", EquipmentSlot.Helmet,
                    "Sức mạnh thần thánh.", "DivineHelmet", 850, 7f, 12f, 50f, 0f, 0f, 0.05f),

                CreateEquipmentPiece("iron_armor", "Giáp Sắt", EquipmentSlot.Armor,
                    "Bảo vệ cơ bản.", "IronArmor", 250, 0f, 6f, 20f, 0f, 0f, 0f),
                CreateEquipmentPiece("warrior_armor", "Giáp Chiến Binh", EquipmentSlot.Armor,
                    "Cân bằng phòng thủ.", "WarriorArmor", 330, 0f, 8f, 30f, 0f, 0f, 0f),
                CreateEquipmentPiece("guardian_armor", "Giáp Hộ Vệ", EquipmentSlot.Armor,
                    "Phòng thủ cao.", "GuardianArmor", 430, 0f, 10f, 45f, 0f, 0f, 0f),
                CreateEquipmentPiece("knight_armor", "Giáp Kỵ Sĩ", EquipmentSlot.Armor,
                    "Bảo vệ toàn diện nhưng hơi nặng.", "KnightArmor", 550, 0f, 13f, 60f, -0.1f, 0f, 0f),
                CreateEquipmentPiece("lion_armor", "Giáp Sư Tử", EquipmentSlot.Armor,
                    "Uy dũng và mạnh mẽ.", "LionArmor", 650, 5f, 12f, 70f, 0f, 0f, 0f),
                CreateEquipmentPiece("destroyer_armor", "Giáp Hủy Diệt", EquipmentSlot.Armor,
                    "Tăng sát thương và giáp.", "DestroyerArmor", 750, 8f, 14f, 60f, 0f, 0f, 0f),
                CreateEquipmentPiece("gold_armor", "Giáp Vàng", EquipmentSlot.Armor,
                    "Kết hợp công thủ cao cấp.", "GoldArmor", 850, 7f, 16f, 90f, 0f, 0f, 0f),
                CreateEquipmentPiece("divine_armor", "Giáp Thần", EquipmentSlot.Armor,
                    "Trang bị phòng thủ tối thượng.", "DivineArmor", 1000, 10f, 20f, 120f, 0f, 0f, 0.05f),

                CreateEquipmentPiece("iron_pants", "Quần Sắt", EquipmentSlot.Pants,
                    "Bảo vệ cơ bản.", "IronPants", 180, 0f, 2f, 10f, 0f, 0f, 0f),
                CreateEquipmentPiece("warrior_pants", "Quần Chiến Binh", EquipmentSlot.Pants,
                    "Cân bằng phòng thủ và tốc độ.", "WarriorPants", 260, 0f, 3f, 15f, 0.1f, 0f, 0f),
                CreateEquipmentPiece("guardian_pants", "Quần Hộ Vệ", EquipmentSlot.Pants,
                    "Phòng thủ cao và linh hoạt.", "GuardianPants", 340, 0f, 4f, 20f, 0.15f, 0f, 0f),
                CreateEquipmentPiece("knight_pants", "Quần Kỵ Sĩ", EquipmentSlot.Pants,
                    "Bảo vệ chắc chắn.", "KnightPants", 430, 0f, 6f, 25f, 0f, 0f, 0f),
                CreateEquipmentPiece("lion_pants", "Quần Sư Tử", EquipmentSlot.Pants,
                    "Nhanh và bền bỉ.", "LionPants", 520, 0f, 5f, 30f, 0.2f, 0f, 0f),
                CreateEquipmentPiece("destroyer_pants", "Quần Hủy Diệt", EquipmentSlot.Pants,
                    "Công thủ cân bằng.", "DestroyerPants", 620, 3f, 7f, 35f, 0.1f, 0f, 0f),
                CreateEquipmentPiece("gold_pants", "Quần Vàng", EquipmentSlot.Pants,
                    "Linh hoạt và chính xác.", "GoldPants", 720, 0f, 8f, 40f, 0.25f, 0.02f, 0f),
                CreateEquipmentPiece("divine_pants", "Quần Thần", EquipmentSlot.Pants,
                    "Sức mạnh và tốc độ tối thượng.", "DivinePants", 900, 4f, 10f, 55f, 0.35f, 0f, 0.05f),

                CreateEquipmentPiece("iron_necklace", "Dây Chuyền Sắt", EquipmentSlot.Necklace,
                    "Mặt dây sắt đơn giản, tăng sức bền.", "IronNecklace", 190, 0f, 1f, 15f, 0f, 0f, 0f, "IronHelmet"),
                CreateEquipmentPiece("warrior_necklace", "Dây Chuyền Chiến Binh", EquipmentSlot.Necklace,
                    "Bùa hộ mệnh cân bằng công thủ.", "WarriorNecklace", 280, 2f, 2f, 15f, 0f, 0f, 0f, "WarriorHelmet"),
                CreateEquipmentPiece("guardian_necklace", "Dây Chuyền Hộ Vệ", EquipmentSlot.Necklace,
                    "Tăng phòng thủ và tốc độ hồi kỹ năng.", "GuardianNecklace", 380, 0f, 3f, 20f, 0f, 0f, 0.04f, "GuardianHelmet"),
                CreateEquipmentPiece("knight_necklace", "Dây Chuyền Kỵ Sĩ", EquipmentSlot.Necklace,
                    "Mặt dây nặng, gia tăng giáp và sinh lực.", "KnightNecklace", 480, 0f, 5f, 35f, -0.03f, 0f, 0f, "KnightHelmet"),
                CreateEquipmentPiece("lion_necklace", "Dây Chuyền Sư Tử", EquipmentSlot.Necklace,
                    "Biểu tượng sư tử tăng sức mạnh và chí mạng.", "LionNecklace", 580, 4f, 3f, 25f, 0f, 0.03f, 0f, "LionHelmet"),
                CreateEquipmentPiece("destroyer_necklace", "Dây Chuyền Hủy Diệt", EquipmentSlot.Necklace,
                    "Nguồn sức mạnh hung bạo dành cho chiến binh.", "DestroyerNecklace", 680, 7f, 2f, 0f, 0f, 0.05f, 0f, "DestroyerHelmet"),
                CreateEquipmentPiece("gold_necklace", "Dây Chuyền Vàng", EquipmentSlot.Necklace,
                    "Phụ kiện quý giá tăng công, thủ và độ linh hoạt.", "GoldNecklace", 800, 5f, 4f, 30f, 0.05f, 0.03f, 0f, "GoldHelmet"),
                CreateEquipmentPiece("divine_necklace", "Dây Chuyền Thần", EquipmentSlot.Necklace,
                    "Thánh vật tối thượng cộng hưởng mọi sức mạnh.", "DivineNecklace", 950, 8f, 6f, 50f, 0.1f, 0.04f, 0.05f, "DivineHelmet")
            };
        }

        private static EquipmentData CreateEquipment(string itemId, string displayName,
            WeaponType type, string description, string iconName, int price,
            float damage, float armor, float health, float speed,
            float criticalChance, float cooldownRate)
        {
            string path = DataRoot + "/Equipment/" + iconName + ".asset";
            EquipmentData asset = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EquipmentData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            Set(asset, "itemId", itemId);
            Set(asset, "displayName", displayName);
            Set(asset, "slot", EquipmentSlot.Weapon);
            Set(asset, "weaponType", type);
            Set(asset, "description", description);
            Set(asset, "icon", LoadEquipmentIcon(iconName));
            Set(asset, "price", price);
            Set(asset, "bonusDamage", damage);
            Set(asset, "bonusArmor", armor);
            Set(asset, "bonusMaxHealth", health);
            Set(asset, "bonusMoveSpeed", 0f);
            Set(asset, "bonusCriticalChance", criticalChance);
            Set(asset, "bonusCooldownRate", cooldownRate);
            return asset;
        }

        private static EquipmentData CreateEquipmentPiece(string itemId,
            string displayName, EquipmentSlot slot, string description,
            string iconName, int price, float damage, float armor, float health,
            float speed, float criticalChance, float cooldownRate,
            string fallbackIconName = null)
        {
            EquipmentData asset = CreateEquipment(itemId, displayName,
                WeaponType.Sword, description, iconName, price, damage, armor,
                health, speed, criticalChance, cooldownRate);
            Set(asset, "slot", slot);
            if (asset.Icon == null && !string.IsNullOrWhiteSpace(fallbackIconName))
            {
                Set(asset, "icon", LoadEquipmentIcon(fallbackIconName));
            }
            return asset;
        }

        private static Sprite LoadEquipmentIcon(string iconName)
        {
            string directPath = EquipmentIconArtRoot + "/" + iconName + ".png";
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(directPath);
            if (icon != null)
            {
                return icon;
            }

            string[] guids = AssetDatabase.FindAssets(iconName + " t:Sprite",
                new[] { EquipmentIconArtRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(Path.GetFileNameWithoutExtension(path), iconName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                icon = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (icon != null)
                {
                    return icon;
                }
            }

            Debug.LogWarning("Không tìm thấy icon trang bị: " + iconName);
            return null;
        }

        private static EnemyData CreateEnemy(string fileName, EnemyArchetype archetype,
            float health, float damage, float speed, float attackCooldown, float attackRange,
            int gold, Color color, float armor = -1f)
        {
            string path = DataRoot + "/Enemies/" + fileName + ".asset";
            EnemyData asset = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            Set(asset, "enemyId", fileName.ToLowerInvariant());
            Set(asset, "displayName", fileName switch
            {
                "Slime" => "Slime Độc", "Goblin" => "Yêu Tinh Rừng",
                "Archer" => "Cung Thủ", "Mage" => "Pháp Sư", "Tank" => "Hộ Vệ",
                "Forest Guardian" => "Vệ Thần Rừng", _ => fileName
            });
            Set(asset, "archetype", archetype);
            Set(asset, "maxHealth", health);
            Set(asset, "damage", damage);
            Set(asset, "armor", armor >= 0f ? armor :
                archetype == EnemyArchetype.Tank || archetype == EnemyArchetype.Boss
                    ? 25f : 3f);
            Set(asset, "moveSpeed", speed);
            Set(asset, "detectionRange", 10f);
            Set(asset, "attackRange", attackRange);
            Set(asset, "attackCooldown", attackCooldown);
            Set(asset, "goldReward", gold);
            Set(asset, "experienceReward", Mathf.Max(5, gold * 4));
            Set(asset, "itemDropChance", 0.15f);
            return asset;
        }

        private static ItemData CreateItem(string id, string displayName, string description)
        {
            string path = DataRoot + "/Items/" + id + ".asset";
            ItemData asset = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            Set(asset, "itemId", id);
            Set(asset, "displayName", displayName);
            Set(asset, "description", description);
            if (string.Equals(id, "forest_relic",
                StringComparison.OrdinalIgnoreCase))
            {
                Set(asset, "icon", GeneratedPropSprite(
                    "ForestRelicPickupV1"));
            }
            return asset;
        }

        private static MapData CreateMap(string id, string displayName, string next,
            bool boss, bool skillRoom)
        {
            string path = DataRoot + "/Maps/" + id + ".asset";
            MapData asset = AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MapData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            Set(asset, "mapId", id);
            Set(asset, "displayName", displayName);
            Set(asset, "nextScene", next);
            Set(asset, "isBossMap", boss);
            Set(asset, "opensSkillSelection", skillRoom);
            Set(asset, "recommendedMinLevel", MapData.RecommendedMinimumForScene(id));
            int maximum = id.StartsWith("Rift", StringComparison.OrdinalIgnoreCase)
                ? 75 : id.StartsWith("Cave", StringComparison.OrdinalIgnoreCase)
                ? 50 : id.StartsWith("Boss", StringComparison.OrdinalIgnoreCase)
                    ? 65 : id == "Map02" ? 20 : id == "Map03" ? 35 : 10;
            int target = boss ? 1 : id.StartsWith("Rift",
                StringComparison.OrdinalIgnoreCase) ? 22 : id.StartsWith("Cave",
                StringComparison.OrdinalIgnoreCase) ? 20 :
                id == "Map02" ? 16 : id == "Map03" ? 18 : 14;
            Set(asset, "recommendedMaxLevel", maximum);
            Set(asset, "targetActiveEnemies", target);
            return asset;
        }

        private static void CreatePlayerPrefab(SkillData[] skills, EquipmentData[] equipment)
        {
            string path = PrefabRoot + "/Player/Player.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            GameObject player = new GameObject("Player");
            player.tag = "Player";
            GameplayCollisionRules.ConfigurePlayer(player);
            player.transform.localScale = Vector3.one * 0.8f;
            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = PlayerIdleSprite() ??
                GeneratedCharacterSprite("Player") ?? BuiltinSprite();
            renderer.color = renderer.sprite == BuiltinSprite()
                ? new Color(0.2f, 0.7f, 1f)
                : Color.white;
            renderer.sortingOrder = 10;

            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.48f;
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.useFullKinematicContacts = true;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            PlayerInput input = player.AddComponent<PlayerInput>();
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Settings/InputSystem_Actions.inputactions");
            input.actions = inputActions;
            input.defaultActionMap = "Player";
            input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            Set(input, "m_Actions", inputActions);
            Set(input, "m_DefaultActionMap", "Player");

            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            Set(movement, "moveSpeed", PlayerStats.DefaultMoveSpeed);
            Set(movement, "smoothLocomotion", true);
            Set(movement, "acceleration", 60f);
            Set(movement, "deceleration", 90f);
            Set(movement, "contactOffset", PlayerMovement.DefaultContactOffset);
            Set(movement, "maxSlideIterations",
                PlayerMovement.DefaultMaxSlideIterations);
            Set(movement, "movementLayers",
                GameplayCollisionRules.PlayerMovementMask);
            Set(movement, "impulseDamping",
                PlayerMovement.DefaultImpulseDamping);
            Set(movement, "gaitCyclesPerUnit",
                PlayerMovement.DefaultGaitCyclesPerUnit);
            Set(movement, "clickPathGridSize",
                PlayerMovement.DefaultClickPathGridSize);
            Set(movement, "clickPathSearchPadding",
                PlayerMovement.DefaultClickPathSearchPadding);
            Set(movement, "maxPathSearchNodes",
                PlayerMovement.DefaultMaxPathSearchNodes);
            Set(movement, "clickPathTurnPenalty",
                PlayerMovement.DefaultClickPathTurnPenalty);
            Set(movement, "clickGoalSnapDistance",
                PlayerMovement.DefaultClickGoalSnapDistance);
            PlayerCharacterAnimator characterAnimator =
                player.AddComponent<PlayerCharacterAnimator>();
            Set(characterAnimator, "idleSprite", PlayerIdleSprite() ??
                GeneratedCharacterSprite("Player"));
            Set(characterAnimator, "smoothLocomotion", true);
            Set(characterAnimator, "stopSettleDuration", 0.1f);
            SetArray(characterAnimator, "walkFrames", new[]
            {
                SmoothPlayerRunSprite(0),
                SmoothPlayerRunSprite(1),
                SmoothPlayerRunSprite(2),
                SmoothPlayerRunSprite(3),
                SmoothPlayerRunSprite(4),
                SmoothPlayerRunSprite(5),
                SmoothPlayerRunSprite(6),
                SmoothPlayerRunSprite(7)
            });
            Sprite[] horizontalWalk = CardinalPlayerWalkSprites("Right");
            SetArray(characterAnimator, "walkLeftFrames", horizontalWalk);
            SetArray(characterAnimator, "walkRightFrames", horizontalWalk);
            SetArray(characterAnimator, "walkUpFrames",
                CardinalPlayerWalkSprites("Up"));
            SetArray(characterAnimator, "walkDownFrames",
                CardinalPlayerWalkSprites("Down"));
            SetArray(characterAnimator, "skillCastFrames", new[]
            {
                GeneratedCharacterSprite("PlayerSkillWindup"),
                GeneratedCharacterSprite("PlayerSkillCast"),
                GeneratedCharacterSprite("PlayerSkillFollowThrough")
            });
            PlayerStats stats = player.AddComponent<PlayerStats>();
            Set(stats, "moveSpeed", PlayerStats.DefaultMoveSpeed);
            Set(stats, "experimentalVitals", true);
            Set(stats, "maxHealth", PlayerStats.ExperimentalMaxHealth);
            Set(stats, "maxMana", PlayerStats.ExperimentalMaxMana);
            Set(stats, "manaRegeneration",
                PlayerStats.ExperimentalManaRegeneration);
            EquipmentInventory equipmentInventory = player.AddComponent<EquipmentInventory>();
            SetArray(equipmentInventory, "catalog", equipment);
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            PlayerDodge dodge = player.AddComponent<PlayerDodge>();
            Set(dodge, "movement", movement);
            Set(dodge, "health", health);
            Set(dodge, "characterAnimator", characterAnimator);
            Set(dodge, "playerInput", input);
            Set(dodge, "duration", PlayerDodge.DefaultDuration);
            Set(dodge, "invulnerability", PlayerDodge.DefaultInvulnerability);
            Set(dodge, "cooldown", PlayerDodge.DefaultCooldown);
            Set(dodge, "speed", PlayerDodge.DefaultSpeed);
            Set(movement, "dodge", dodge);
            player.AddComponent<PlayerPoisonStatus>();
            PlayerCombat combat = player.AddComponent<PlayerCombat>();
            Set(combat, "autoAttackEnabled", true);
            Set(combat, "clickToShoot", false);
            Set(movement, "clickToMove", true);
            Set(combat, "autoTargetScanInterval", 0.08f);
            Set(combat, "autoSkillSearchRange", 7f);
            Set(combat, "autoCombatResumeDelay", 0.12f);
            Set(combat, "stationarySpeedThreshold", 0.08f);
            PlayerInteraction interaction = player.AddComponent<PlayerInteraction>();
            Set(interaction, "worldDoubleClickEnabled", true);
            player.AddComponent<ThanhGiongHeroController>();
            SkillInventory inventory = player.AddComponent<SkillInventory>();
            SkillManager manager = player.AddComponent<SkillManager>();
            SetArray(manager, "skillDatabase", skills);
            SetInventoryDefaults(inventory);

            SkillBase[] components =
            {
                player.AddComponent<FireballSkill>(),
                player.AddComponent<IceDashSkill>(),
                player.AddComponent<SwordSpinSkill>(),
                player.AddComponent<LightningStrikeSkill>(),
                player.AddComponent<HealingSkill>(),
                player.AddComponent<ShieldSkill>(),
                player.AddComponent<ChainLightningSkill>(),
                player.AddComponent<MeteorShowerSkill>(),
                player.AddComponent<VoidPulseSkill>()
            };
            for (int i = 0; i < components.Length; i++)
            {
                Set(components[i], "data", skills[i]);
                if (components[i] is SwordSpinSkill)
                {
                    Set(components[i],"comboProfile",CreateSwordComboProfile());
                    Set(components[i], "effectSprite", AssetDatabase.LoadAssetAtPath<Sprite>(
                        SkillIconArtRoot + "/SwordSpinEffect.png"));
                }
            }

            PrefabUtility.SaveAsPrefabAsset(player, path);
            UnityEngine.Object.DestroyImmediate(player);
        }

        private static SwordComboProfile CreateSwordComboProfile()
        {
            const string path="Assets/ScriptableObjects/Skills/SwordComboV3.asset";
            var profile=AssetDatabase.LoadAssetAtPath<SwordComboProfile>(path);
            if(profile==null){profile=ScriptableObject.CreateInstance<SwordComboProfile>();AssetDatabase.CreateAsset(profile,path);}
            Sprite[] actor=CreateSwordSheet(CharacterArtRoot+"/PlayerSwordComboV3.png","SwordActor",true);
            Sprite[] slash=CreateSwordSheet(SkillIconArtRoot+"/SwordSlashPaintedV3.png","SwordSlash",false);
            SetArray(profile,"actorFrames",actor);SetArray(profile,"slashFrames",slash);
            Set(profile,"actorMaterial",SwordMaterial("SwordActorV3","SwordActorMatte"));
            Set(profile,"slashMaterial",SwordMaterial("SwordSlashV3","SwordPaintedLight"));
            Set(profile,"swingCue",CreateSwordSound("SwordSwing",false));
            Set(profile,"hitCue",CreateSwordSound("SwordContact",true));
            return profile;
        }

        private static Material SwordMaterial(string name,string shader)
        {
            string path="Assets/Settings/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            var source=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Shaders/"+shader+".shader");
            if(mat==null){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}
            mat.shader=source;EditorUtility.SetDirty(mat);return mat;
        }

        private static Sprite[] CreateSwordSheet(string sourcePath,string label,bool actor)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(sourcePath);
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            int width=texture.width/3,height=texture.height/2;
            var frames=new Sprite[6];
            // Explicit cell rectangles and matched foot pivots. Source sheet is never destructively cut.
            float[] bottom={.095f,.095f,.095f,.23f,.23f,.23f};
            float[] center={.49f,.42f,.35f,.51f,.43f,.39f};
            for(int i=0;i<6;i++)
            {
                string path="Assets/Settings/"+label+i+".asset";
                float ppu=actor ? 260f : 200f;
                Vector2 pivot=actor ? new Vector2(center[i],bottom[i]+.52f*ppu/height) : Vector2.one*.5f;
                var candidate=Sprite.Create(texture,new Rect((i%3)*width,(1-i/3)*height,width,height),pivot,ppu,0,SpriteMeshType.FullRect);
                candidate.name=label+i;
                frames[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(frames[i]==null){frames[i]=candidate;AssetDatabase.CreateAsset(candidate,path);}
                else{EditorUtility.CopySerialized(candidate,frames[i]);UnityEngine.Object.DestroyImmediate(candidate);EditorUtility.SetDirty(frames[i]);}
            }
            return frames;
        }

        private static TopDownGame.Audio.AudioCue CreateSwordSound(string name,bool impact)
        {
            const int rate=22050;
            int count=Mathf.RoundToInt(rate*(impact?.13f:.18f));
            string audioPath="Assets/Audio/"+name+"V3.wav";
            var random=new System.Random(impact?901:417);
            using(var stream=new MemoryStream())
            using(var writer=new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
                writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                float last=0;
                for(int i=0;i<count;i++)
                {
                    float t=i/(float)count;
                    float noise=(float)(random.NextDouble()*2-1);
                    last=Mathf.Lerp(last,noise,impact?.8f:.22f);
                    float env=impact?Mathf.Exp(-t*9):Mathf.Pow(Mathf.Sin(Mathf.PI*t),1.5f);
                    float tone=Mathf.Sin(i/(float)rate*2*Mathf.PI*(impact?1150:330)*(1-t*.3f));
                    writer.Write((short)(Mathf.Clamp((last*.65f+tone*.13f)*env,-1,1)*20000));
                }
                File.WriteAllBytes(audioPath,stream.ToArray());
            }
            AssetDatabase.ImportAsset(audioPath,ImportAssetOptions.ForceSynchronousImport);
            string cuePath="Assets/Audio/Cues/"+name+"V3.asset";
            var cue=AssetDatabase.LoadAssetAtPath<TopDownGame.Audio.AudioCue>(cuePath);
            if(cue==null){cue=ScriptableObject.CreateInstance<TopDownGame.Audio.AudioCue>();AssetDatabase.CreateAsset(cue,cuePath);}
            SetArray(cue,"clips",new[]{AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath)});
            Set(cue,"volume",impact?.45f:.28f);Set(cue,"maxConcurrent",3);Set(cue,"spatial",false);
            return cue;
        }

        private static void SetInventoryDefaults(SkillInventory inventory)
        {
            SerializedObject serialized = new SerializedObject(inventory);
            SerializedProperty slots = serialized.FindProperty("slots");
            slots.arraySize = 4;
            string[] ids = { "Fireball", "IceDash", "SwordSpin", "LightningStrike" };
            string[] keys = { "Q", "E", "R", "F" };
            for (int i = 0; i < 4; i++)
            {
                SerializedProperty element = slots.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("key").stringValue = keys[i];
                element.FindPropertyRelative("skillId").stringValue = ids[i];
                element.FindPropertyRelative("level").intValue = 1;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateEnemyPrefab(string fileName, EnemyData data,
            Type enemyType, Color color, string visualSource = null,
            bool tintGeneratedSprite = false)
        {
            string path = PrefabRoot + "/Enemies/" + fileName + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            GameObject enemy = new GameObject(fileName);
            GameplayCollisionRules.ConfigureEnemy(enemy);
            enemy.transform.localScale = Vector3.one * 0.85f;
            SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = GeneratedCharacterSprite(
                string.IsNullOrWhiteSpace(visualSource) ? fileName : visualSource) ??
                BuiltinSprite();
            renderer.color = renderer.sprite == BuiltinSprite() || tintGeneratedSprite
                ? color : Color.white;
            renderer.sortingOrder = 5;
            CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
            collider.radius = 0.48f;
            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            EnemyMovement movement = enemy.AddComponent<EnemyMovement>();
            Set(movement, "acceleration", 12f);
            Set(movement, "deceleration", 18f);
            Set(movement, "turnSpeedDegrees", 540f);
            Set(movement, "impulseDamping", 20f);
            Set(movement, "separationRadius", 0.78f);
            Set(movement, "separationStrength", 1.35f);
            Set(movement, "navigationRefreshRate", 0.12f);
            Set(movement, "stuckDetectionTime", 0.55f);
            Set(movement, "unstickDuration", 0.42f);
            Set(movement, "obstacleProbeDistance", 0.9f);
            Set(movement, "obstacleAvoidanceStrength", 0.85f);
            Set(movement, "surfaceSkin", 0.035f);
            Set(movement, "maxSlideIterations", 2);
            EnemyCharacterAnimator characterAnimator =
                enemy.GetComponent<EnemyCharacterAnimator>();
            if (characterAnimator == null)
            {
                characterAnimator = enemy.AddComponent<EnemyCharacterAnimator>();
            }
            Set(characterAnimator, "locomotionFollow", 14f);
            Set(characterAnimator, "actionPoseFollow", 28f);
            EnemyHealth health = enemy.AddComponent<EnemyHealth>();
            enemy.AddComponent<EnemyHealthBar>();
            EnemyBase controller = (EnemyBase)enemy.AddComponent(enemyType);
            Set(controller, "data", data);
            enemy.AddComponent<EnemySignatureAbility>();
            movement.ConfigureArchetype(data.Archetype);
            characterAnimator.ConfigureArchetype(data.Archetype);

            if (controller is MeleeEnemy)
            {
                Set(controller, "approachSlowingDistance", 1.35f);
                Set(controller, "basicAttackWindup", 0.15f);
                Set(controller, "basicAttackActive", 0.08f);
                Set(controller, "basicAttackRecovery", 0.16f);
            }

            if (controller is SlimeEnemy)
            {
                Set(controller, "hopWindup", 0.16f);
                Set(controller, "hopAirTime", 0.34f);
                Set(controller, "landingRecovery", 0.13f);
                Set(controller, "hopImpulse", 7.4f);
                Set(controller, "hopPause", 0.06f);
                Set(controller, "approachStopDistance", 1.05f);
                Set(controller, "poisonSprayEveryAttacks", 2);
                Set(controller, "poisonSprayWindup", 0.22f);
                Set(controller, "poisonSprayDuration", 0.68f);
                Set(controller, "poisonSprayRecovery", 0.1f);
                Set(controller, "poisonSprayCooldownMultiplier", 0.42f);
                Set(controller, "poisonSprayRange", 5.2f);
                Set(controller, "poisonSprayAngle", 54f);
                Set(controller, "poisonStreamCount", 5);
                Set(controller, "poisonBurstInterval", 0.1f);
                Set(controller, "poisonProjectileSpeed", 6.8f);
                Set(controller, "poisonApplicationChance", 0.45f);
                Set(controller, "poisonDuration", 3.6f);
                Set(controller, "poisonTickInterval", 0.6f);
                Set(controller, "poisonDamagePerTickMultiplier", 0.12f);
            }

            if (controller is ForestGoblinEnemy)
            {
                Set(characterAnimator, "articulatedSheet",
                    GeneratedCharacterSprite("GoblinAnimationSheetV2"));
                Set(characterAnimator, "articulatedPixelsPerUnit", 320f);
                Set(controller, "stalkingDistance", 2.45f);
                Set(controller, "scurrySpeedMultiplier", 1.18f);
                Set(controller, "zigzagDirectionSeconds",
                    new Vector2(1.15f, 1.8f));
                Set(controller, "leafDashEveryAttacks", 3);
                Set(controller, "slashWindup", 0.3f);
                Set(controller, "slashActive", 0.08f);
                Set(controller, "slashRecovery", 0.24f);
                Set(controller, "leafDashDistance", 2.7f);
                Set(controller, "leafDashWindup", 0.48f);
                Set(controller, "leafDashActive", 0.28f);
                Set(controller, "ambushCooldown", 5.5f);
                Set(controller, "retreatDuration", 0.7f);
                Set(controller, "secondSlashWindup", 0.38f);
                Set(controller, "chaseLeash", 12f);
            }

            if (controller is ForestArcherEnemy)
            {
                Set(characterAnimator, "articulatedSheet",
                    GeneratedCharacterSprite("ArcherAnimationSheetV2"));
                Set(characterAnimator, "articulatedPixelsPerUnit", 320f);
                Set(controller, "forestPreferredDistance", 5.25f);
                Set(controller, "forestRetreatDistance", 2.85f);
                Set(controller, "crossStepSpeedMultiplier", 0.72f);
                Set(controller, "crossStepDirectionSeconds",
                    new Vector2(0.72f, 1.28f));
                Set(controller, "leafVolleyEveryShots", 3);
                Set(controller, "quickShotWindup", 0.4f);
                Set(controller, "volleyWindup", 0.65f);
                Set(controller, "volleyBackstepImpulse", 4.8f);
                Set(controller, "evasionCooldown", 4f);
                Set(controller, "arrowRainCooldown", 7.5f);
                Set(controller, "arrowRainWindup", 0.95f);
                Set(controller, "arrowRainRadius", 1.45f);
                Set(controller, "postHoldSeconds", 1.1f);
            }

            if (controller is CrystalScorpionEnemy)
            {
                Set(controller, "retreatDistance", 2.2f);
                Set(controller, "orbitOuterDistance", 4f);
                Set(controller, "strafeSpeedMultiplier", 0.78f);
                Set(controller, "orbitDirectionSeconds", new Vector2(1.2f, 2.2f));
                Set(controller, "recoveryDuration", 0.32f);
                Set(controller, "pincerRange", 1.55f);
                Set(controller, "pincerWindup", 0.36f);
                Set(controller, "chargeWindup", 0.58f);
                Set(controller, "spikeEveryAttacks", 3);
                Set(controller, "spikeWindup", 0.72f);
            }

            if (controller is MageEnemy)
            {
                Set(controller, "combatPreferredDistance", 5.6f);
                Set(controller, "combatRetreatDistance", 3.2f);
                Set(controller, "orbitSpeedMultiplier", 0.68f);
                Set(controller, "orbitDirectionSeconds",
                    new Vector2(1.35f, 2.45f));
                Set(controller, "recoveryDuration", 0.3f);
                Set(controller, "castTime", 0.65f);
                Set(controller, "areaRadius", 1.5f);
                Set(controller, "greaterRuneEveryCasts", 3);
                Set(controller, "greaterRuneWindupMultiplier", 1.25f);
                Set(controller, "greaterRuneRadiusMultiplier", 1.38f);
                Set(controller, "greaterRuneDamageMultiplier", 1.35f);
            }

            if (controller is TankEnemy)
            {
                Set(controller, "circleOuterDistance", 2.8f);
                Set(controller, "circleSpeedMultiplier", 0.52f);
                Set(controller, "circleDirectionSeconds",
                    new Vector2(1.6f, 2.8f));
                Set(controller, "recoveryDuration", 0.38f);
                Set(controller, "bashWindup", 0.42f);
                Set(controller, "bashImpulse", 7.5f);
                Set(controller, "slamEveryAttacks", 3);
                Set(controller, "slamWindup", 0.78f);
                Set(controller, "slamRadius", 2.05f);
            }

            if (controller is VoidReaverEnemy)
            {
                enemy.transform.localScale = Vector3.one * 1.08f;
                Set(controller, "riftDashEveryAttacks", 3);
                Set(controller, "dashWindup", 0.65f);
                Set(controller, "dashDistance", 4.8f);
            }

            if (fileName == "ObsidianGolem")
            {
                enemy.transform.localScale = Vector3.one * 1.18f;
            }

            if (enemyType == typeof(ForestGuardianBoss))
            {
                enemy.transform.localScale = Vector3.one * 1.7f;
            }

            PrefabUtility.SaveAsPrefabAsset(enemy, path);
            UnityEngine.Object.DestroyImmediate(enemy);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void CreateLobbyScene(SkillData[] skills,
            EquipmentData[] equipment)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLobbyDecor();

            GameObject player = PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabRoot + "/Player/Player.prefab"), scene) as GameObject;
            player.transform.position = new Vector3(-5.5f, 0f, 0f);
            CreateSpawnPoint(player.transform.position);
            CreateCamera(player.transform, "Lobby");
            CreateLobbyVisitors();
            CreateLight();

            Canvas canvas = CreateGameplayCanvas(skills, true);
            GameObject shopHost = new GameObject("WeaponShopUI");
            shopHost.transform.SetParent(canvas.transform, false);
            WeaponShopUI shop = shopHost.AddComponent<WeaponShopUI>();
            SetArray(shop, "catalog", equipment);
            Set(shop, "merchantPortrait", AssetDatabase.LoadAssetAtPath<Sprite>(
                EquipmentUIArtRoot + "/ForgeKeeperV2.png"));

            GameObject chestUiHost = new GameObject("StorageChestUI");
            chestUiHost.transform.SetParent(canvas.transform, false);
            StorageChestUI storageChestUI = chestUiHost.AddComponent<StorageChestUI>();

            GameObject npc = new GameObject("WeaponMerchant");
            npc.transform.position = LobbyForgePosition;
            SpriteRenderer npcRenderer = npc.AddComponent<SpriteRenderer>();
            npcRenderer.sprite = GeneratedCharacterSprite("Tank") ?? BuiltinSprite();
            npcRenderer.color = npcRenderer.sprite == BuiltinSprite()
                ? new Color(0.85f, 0.55f, 0.18f)
                : Color.white;
            npcRenderer.sortingOrder = 12;
            // This stationary merchant is painted into the backplate. Keep the
            // interaction object separate; do not draw the old tank proxy.
            npcRenderer.enabled = false;
            float npcScale = 1.15f / Mathf.Max(0.01f, npcRenderer.sprite.bounds.size.y);
            npc.transform.localScale = Vector3.one * npcScale;
            CircleCollider2D npcCollider = npc.AddComponent<CircleCollider2D>();
            npcCollider.radius = 0.55f / npcScale;
            npcCollider.isTrigger = true;
            WeaponShopNpc merchant = npc.AddComponent<WeaponShopNpc>();
            Set(merchant, "shop", shop);
            CreateLobbyStationLabel(npc.transform, "MerchantLabel", "LÒ RÈN", new Color(1f, 0.83f, 0.5f));

            GameObject storageObject = new GameObject("StorageChest");
            storageObject.transform.position = LobbyStoragePosition;
            SpriteRenderer storageRenderer = storageObject.AddComponent<SpriteRenderer>();
            storageRenderer.sprite = GeneratedPropSprite("TreasureChest") ?? BuiltinSprite();
            storageRenderer.color = storageRenderer.sprite == BuiltinSprite()
                ? new Color(0.85f, 0.55f, 0.15f)
                : Color.white;
            storageRenderer.sortingOrder = 11;
            float chestScale = 0.95f / Mathf.Max(0.01f, storageRenderer.sprite.bounds.size.y);
            storageObject.transform.localScale = Vector3.one * chestScale;
            BoxCollider2D storageCollider = storageObject.AddComponent<BoxCollider2D>();
            storageCollider.isTrigger = true;
            storageCollider.size = new Vector2(1.05f, 0.8f) / chestScale;
            StorageChest storage = storageObject.AddComponent<StorageChest>();
            Set(storage, "chestUI", storageChestUI);
            SetArray(storage, "startingItems", new[]
            {
                equipment[8],
                equipment[16],
                equipment[24]
            });
            CreateLobbyStationLabel(storageObject.transform, "StorageLabel", "KHO TRANG BỊ", new Color(0.75f, 0.89f, 1f));

            GameObject portalObject = new GameObject("BattlePortal");
            portalObject.transform.position = LobbyPortalPosition;
            // The painted rune dais is the portal; no upright arch sprite.
            CapsuleCollider2D portalCollider = portalObject.AddComponent<CapsuleCollider2D>();
            portalCollider.direction = CapsuleDirection2D.Horizontal;
            portalCollider.isTrigger = true;
            portalCollider.size = new Vector2(2.4f, 1.25f);
            LobbyPortal portal = portalObject.AddComponent<LobbyPortal>();
            MapData[] destinationMaps = new MapData[LobbyPortal.Destinations.Length];
            for (int i = 0; i < destinationMaps.Length; i++)
            {
                string path = DataRoot + "/Maps/" + LobbyPortal.Destinations[i] + ".asset";
                destinationMaps[i] = AssetDatabase.LoadAssetAtPath<MapData>(path);
                if (destinationMaps[i] == null) throw new System.InvalidOperationException("Missing travel destination: " + path);
            }
            SetArray(portal, "destinationMaps", destinationMaps);
            Set(portal, "chargeDuration", 1.15f);
            Set(portal, "departureDuration", 3.0f);
            TeleportDepartureVisual departure = portalObject.AddComponent<TeleportDepartureVisual>();
            Set(departure, "beamShader", AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/TeleportDeparture.shader"));
            Set(departure, "zoomOutMultiplier", 1.18f);
            TeleportCircleVisual circleVisual = portalObject.AddComponent<TeleportCircleVisual>();
            Set(circleVisual, "engravedRadii", new Vector2(1.22f, 0.87f));
            Set(circleVisual, "outerRadii", new Vector2(1.95f, 1.44f));
            CreateMapGuidance("Lobby", "Trạm nghỉ Tán Rừng",
                player.transform.position, portalObject.transform.position,
                storageObject.transform.position, "Map01", "RƯƠNG ĐỒ");
            CreateLobbyStationLabel(portalObject.transform, "PortalLabel", "TRUYỀN TỐNG TRẬN", new Color(0.6f, 1f, 0.85f), 1.55f);

            HeroSelectionUI heroSelectionUI = canvas.GetComponentInChildren<HeroSelectionUI>(true);
            GameObject heroShrine = new GameObject("HeroSelectionStation");
            heroShrine.transform.position = new Vector3(-4.8f, -1.45f, 0f);
            SpriteRenderer shrineRenderer = heroShrine.AddComponent<SpriteRenderer>();
            shrineRenderer.sprite = GeneratedPropSprite("AncientForestShrineV1") ?? BuiltinSprite();
            shrineRenderer.color = new Color(1f, 0.88f, 0.45f, 1f);
            shrineRenderer.sortingOrder = 11;
            float shrineScale = 1.25f / Mathf.Max(0.01f, shrineRenderer.sprite.bounds.size.y);
            heroShrine.transform.localScale = Vector3.one * shrineScale;
            CircleCollider2D shrineCollider = heroShrine.AddComponent<CircleCollider2D>();
            shrineCollider.isTrigger = true;
            shrineCollider.radius = 0.65f / shrineScale;
            HeroSelectionStation stationComponent = heroShrine.AddComponent<HeroSelectionStation>();
            Set(stationComponent, "selectionUI", heroSelectionUI);
            GameObject shrineLabel = CreateWorldLabel("HeroShrineLabel",
                "ĐÀI THÁNH GIÓNG\nE / NHẤP ĐÚP  •  PHÍM H",
                heroShrine.transform.position + new Vector3(0f, 1.15f, 0f),
                new Color(1f, 0.86f, 0.28f), 0.032f);
            shrineLabel.transform.SetParent(heroShrine.transform, true);

            canvas.gameObject.AddComponent<LobbyWelcomeUI>();
            GameObject tutorialObject = new GameObject("FirstEntryTutorial", typeof(RectTransform));
            tutorialObject.transform.SetParent(canvas.transform, false);
            MenuDialog tutorialDialog = tutorialObject.AddComponent<MenuDialog>();
            LobbyTutorialUI tutorial = tutorialObject.AddComponent<LobbyTutorialUI>();
            Set(tutorial, "dialog", tutorialDialog);
            EditorSceneManager.SaveScene(scene, SceneRoot + "/Lobby.unity");
        }

        private static GameObject CreateWorldLabel(string name, string value,
            Vector3 position, Color color, float characterSize = 0.065f)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.position = position;
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = value;
            label.fontSize = 48;
            label.characterSize = characterSize;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            labelObject.GetComponent<MeshRenderer>().sortingOrder = 30;
            return labelObject;
        }

        private static void CreateMapGuidance(string mapId, string displayName,
            Vector3 entrancePosition, Vector3 exitPosition,
            Vector3 rewardPosition, string nextScene, string rewardName)
        {
            GameObject root = new GameObject("MapGuidance");
            Transform entrance = CreateGuidanceAnchor(root.transform,
                "EntranceAnchor", entrancePosition);
            Transform exit = CreateGuidanceAnchor(root.transform,
                "ExitAnchor", exitPosition);
            Transform reward = CreateGuidanceAnchor(root.transform,
                "RewardAnchor", rewardPosition);

            MapGuidanceVisuals guidance = root.AddComponent<MapGuidanceVisuals>();
            Set(guidance, "mapId", mapId);
            Set(guidance, "entranceAnchor", entrance);
            Set(guidance, "exitAnchor", exit);
            Set(guidance, "rewardAnchor", reward);

            Color accent = GuidanceAccent(mapId);
            if (mapId == "Lobby")
            {
                // The painted plaza and contextual station signs replace the
                // combat-map beacon/rings without losing minimap anchor data.
                guidance.enabled = false;
                return;
            }
            GameObject entranceLabel = CreateWorldLabel("Guidance_EntranceLabel",
                displayName.ToUpperInvariant() + "\nĐIỂM XUẤT PHÁT",
                entrancePosition + new Vector3(0f, 1.35f, 0f), accent, 0.052f);
            entranceLabel.transform.SetParent(root.transform, true);
            if (mapId == "Lobby")
            {
                return;
            }

            MapData destination = AssetDatabase.LoadAssetAtPath<MapData>(
                DataRoot + "/Maps/" + nextScene + ".asset");
            if (nextScene != "Lobby" && destination == null)
                throw new InvalidOperationException("Missing destination MapData: " + nextScene);
            string destinationName = nextScene == "Lobby" ? MapDoor.LobbyDisplayName : destination.DisplayName;
            string destinationHint = destination != null
                ? "CẤP ĐỀ NGHỊ: " + destination.RecommendedMinLevel + "-" + destination.RecommendedMaxLevel
                : "NGHỈ NGƠI VÀ CHUẨN BỊ";
            GameObject exitLabel = CreateWorldLabel("Guidance_ExitLabel",
                "ĐẾN " + destinationName.ToUpperInvariant() + "\n" + destinationHint,
                exitPosition + new Vector3(0f, 2.05f, 0f), accent, 0.058f);
            exitLabel.transform.SetParent(root.transform, true);
            GameObject rewardLabel = CreateWorldLabel("Guidance_RewardLabel",
                rewardName,
                rewardPosition + new Vector3(0f, 1.2f, 0f), accent, 0.052f);
            rewardLabel.transform.SetParent(root.transform, true);
        }

        private static Transform CreateGuidanceAnchor(Transform parent,
            string name, Vector3 position)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent, true);
            anchor.transform.position = position;
            return anchor.transform;
        }

        private static Color GuidanceAccent(string mapId)
        {
            switch (mapId)
            {
                case "Map02": return new Color(1f, 0.62f, 0.2f);
                case "Map03": return new Color(0.68f, 0.55f, 1f);
                case "BossMap01": return new Color(1f, 0.42f, 0.18f);
                case "CaveMap01": return new Color(0.38f, 0.82f, 1f);
                case "Lobby": return new Color(0.38f, 1f, 0.62f);
                default: return new Color(1f, 0.84f, 0.28f);
            }
        }

        private static void CreateLobbyDecor()
        {
            Sprite backdrop = AssetDatabase.LoadAssetAtPath<Sprite>(
                RestoredLobbyArt);
            if (backdrop == null)
                throw new InvalidOperationException("Missing restored Lobby artwork.");

            GameObject environment = new GameObject("Environment");
            GameObject root = new GameObject("LobbyDecor_Generated");
            GameObject floor = new GameObject("Floor");
            floor.transform.SetParent(environment.transform, false);
            SpriteRenderer ground = floor.AddComponent<SpriteRenderer>();
            ground.sprite = backdrop;
            ground.sortingOrder = -20;
            floor.transform.localScale = new Vector3(26f / backdrop.bounds.size.x,
                15.6f / backdrop.bounds.size.y, 1f);

            GameObject walls = new GameObject("Walls");
            walls.transform.SetParent(environment.transform, false);
            // The hand-authored perimeter follows the river bank, building
            // facades and southern stone walls. No collider crosses the plaza.
            Vector2[] perimeter =
            {
                new Vector2(-8.9f, 2.25f), new Vector2(-6.2f, 3.3f),
                new Vector2(-4.6f, 3.3f), new Vector2(-4.6f, 2.45f),
                new Vector2(1.65f, 2.45f), new Vector2(3.4f, 3.1f),
                new Vector2(4.15f, 2.6f), new Vector2(7.25f, 2.6f),
                new Vector2(8.0f, 2.1f), new Vector2(10.55f, 1.6f),
                new Vector2(11.0f, -1.6f), new Vector2(8.8f, -2.65f),
                new Vector2(4.25f, -2.65f), new Vector2(1.8f, -3.35f),
                new Vector2(-0.5f, -4.0f), new Vector2(-3.8f, -3.1f),
                new Vector2(-6.55f, -3.0f), new Vector2(-8.35f, -0.8f)
            };
            for (int i = 0; i < perimeter.Length; i++)
            {
                Vector2 a = perimeter[i];
                Vector2 b = perimeter[(i + 1) % perimeter.Length];
                GameObject wall = new GameObject("LobbyBoundary_" + i.ToString("00"));
                wall.transform.SetParent(walls.transform, false);
                wall.transform.position = (a + b) * 0.5f;
                Vector2 direction = b - a;
                wall.transform.rotation = Quaternion.Euler(0, 0,
                    Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(direction.magnitude + 0.14f, 0.22f);
            }

            LobbyAtmosphere atmosphere = root.AddComponent<LobbyAtmosphere>();
            SetVector2Array(atmosphere, "warmLightPositions", new Vector2[]
            {
                new Vector2(-2.7f, 3.2f), new Vector2(0.45f, 3.35f),
                new Vector2(4.45f, 3.45f), new Vector2(7.65f, 0.95f),
                new Vector2(10.45f, 0.2f)
            });
            Set(atmosphere, "portalPosition", LobbyPortalPosition);
            Set(atmosphere, "chimneyPosition", new Vector3(-1.9f, 6.35f));
            LobbyWaterFlow water = root.AddComponent<LobbyWaterFlow>();
            Set(water, "sourceArt", backdrop);
            Set(water, "flowShader", AssetDatabase.LoadAssetAtPath<Shader>(
                Root + "/Shaders/LobbyWaterFlow.shader"));
            // Centre and half-width of each cross-section, aligned to the
            // painted river. The river is outside the player's walkable plaza.
            SetVector2Array(water, "streamCenters", new Vector2[]
            {
                new Vector2(-11.35f, 1.45f), new Vector2(-11.1f, 0.25f),
                new Vector2(-10.65f, -1.0f), new Vector2(-10.05f, -2.5f),
                new Vector2(-9.9f, -3.7f), new Vector2(-9.7f, -4.6f)
            });
            Set(water, "waterfallTop", new Vector2(-11.4f, 2.6f));
            Set(water, "waterfallBottom", new Vector2(-11.35f, 1.5f));
            LobbyFireEffects fire = root.AddComponent<LobbyFireEffects>();
            SetVector2Array(fire, "firePositions", new Vector2[]
            {
                new Vector2(-0.4f, 3.55f)
            });
            LobbyWildlife wildlife = root.AddComponent<LobbyWildlife>();
            var garden = root.AddComponent<LobbyGardenWildlife>();
            SetVector2Array(garden, "flowerBeds", new[] {
                new Vector2(-3.1f,-4.0f), new Vector2(1.1f,-4.15f),
                new Vector2(-2.4f,-6.0f) });
            SetVector2Array(garden, "birdPerches", new[] {
                new Vector2(-2.2f,-4.25f), new Vector2(1.6f,-4.2f),
                new Vector2(-3.7f,-5.55f) });
            Set(wildlife, "fishCount", 10);
            Set(wildlife, "pondCenter", new Vector2(-10.4f, -1.9f));
            Set(wildlife, "pondRadii", new Vector2(1.4f, 2.25f));
            SetVector2Array(wildlife, "flowerPatches", new Vector2[]
            {
                new Vector2(-7.9f, 2.1f), new Vector2(2.7f, 2.55f),
                new Vector2(7.3f, -2.35f)
            });
        }

        private static void CreateLobbyStationLabel(Transform station, string name,
            string title, Color accent, float height = 1.05f)
        {
            GameObject sign = CreateWorldLabel(name, title,
                station.position + Vector3.up * height, accent, 0.036f);
            TextMesh titleMesh = sign.GetComponent<TextMesh>();
            titleMesh.fontStyle = FontStyle.Bold;
            GameObject prompt = CreateWorldLabel(name + "_Prompt", "E  /  NHẤP ĐÚP",
                sign.transform.position - Vector3.up * 0.34f, Color.white, 0.025f);
            prompt.transform.SetParent(sign.transform, true);
            LobbyStationSign proximity = sign.AddComponent<LobbyStationSign>();
            Set(proximity, "station", station);
            Set(proximity, "title", titleMesh);
            Set(proximity, "prompt", prompt.GetComponent<TextMesh>());
            Set(proximity, "accent", accent);
        }

        private static GameObject CreateTiledArea(string name, Transform parent,
            Sprite sprite, Vector3 position, Vector2 size, Color color,
            int sortingOrder)
        {
            GameObject area = new GameObject(name);
            area.transform.SetParent(parent);
            area.transform.position = position;
            SpriteRenderer renderer = area.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite ?? BuiltinSprite();
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return area;
        }

        private static GameObject CreateSpriteProp(string name, Transform parent,
            Sprite sprite, Vector3 position, float worldHeight, int sortingOrder,
            bool flipX)
        {
            GameObject prop = new GameObject(name);
            prop.transform.SetParent(parent);
            prop.transform.position = position;
            SpriteRenderer renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite ?? BuiltinSprite();
            renderer.color = sprite == null ? Color.magenta : Color.white;
            renderer.sortingOrder = sortingOrder;
            renderer.flipX = flipX;
            float sourceHeight = Mathf.Max(0.01f, renderer.sprite.bounds.size.y);
            prop.transform.localScale = Vector3.one *
                (Mathf.Max(0.1f, worldHeight) / sourceHeight);
            return prop;
        }

        private static void CreateCombatDecor(string mapId)
        {
            if (mapId == "Map02")
            {
                CreatePaintedForestCollision();
                return;
            }
            MazeLayout layout = MazeLayoutFor(mapId);
            if (layout == null)
            {
                return;
            }

            GameObject root = new GameObject("CombatDecor_Generated");
            Sprite tree = GeneratedPropSprite("ForestTreeClusterV2");
            Sprite rock = GeneratedPropSprite("MossyRockClusterV2");
            Sprite fallenLog = GeneratedPropSprite("FallenLogSupplyClusterV1");
            Sprite shrine = GeneratedPropSprite("AncientForestShrineV1");
            Sprite crystal = GeneratedPropSprite("CaveCrystalClusterV1");

            bool isRift = mapId.StartsWith("Rift",
                StringComparison.OrdinalIgnoreCase);
            bool isCave = mapId.StartsWith("Cave",
                StringComparison.OrdinalIgnoreCase) || isRift;
            MazeMask mask = BuildMazeMask(layout);
            ValidateMazeLayout(mapId, layout, mask);
            Sprite biomeFloor = AssetDatabase.LoadAssetAtPath<Sprite>(
                EnvironmentArtRoot + (mapId == "Map02" ? "/Map02_LeafSoilV3.png" : mapId == "Map01" ? "/Map01_MossSoilV8.png" : isCave ? "/CaveFloor.png" :
                    "/ForestFloor.png"));
            CreateOrganicMazeFloor(root.transform, mapId, isCave, biomeFloor,
                mask);
            CreateMazeTopologyMarkers(root.transform, layout);

            bool usesStoneWalls = mapId == "Map03";
            // Let stone carry the cave silhouette and reserve the bright crystals
            // for accents. A crystal-first wall turns the whole boundary into one
            // repeated cyan band and competes with enemies and landmarks.
            Sprite wallSprite = isCave ? rock : (usesStoneWalls ? rock : tree);
            Sprite accentSprite = isCave ? crystal : (usesStoneWalls ? tree : rock);
            Sprite detailSprite = isCave ? crystal : fallenLog;
            float wallHeight = isCave ? 1.75f :
                (usesStoneWalls ? 1.15f : 2.15f);
            float accentHeight = isCave ? 1.05f :
                (usesStoneWalls ? 1.85f : 0.95f);
            float detailHeight = isCave ? 1.45f : 1.2f;
            float propSpacing = isCave ? 1.55f : (usesStoneWalls ? 1.35f : 1.6f);
            Color accentTint = isRift
                ? new Color(0.62f, 0.28f, 1f)
                : isCave ? new Color(0.72f, 0.82f, 1f)
                : Color.white;
            if (mapId == "Map01")
            {
                wallSprite=rock; accentSprite=tree;
                wallHeight=0.95f; accentHeight=2.8f;
                detailHeight=0.85f; propSpacing=1.25f;
                accentTint=new Color(0.8f,0.9f,0.86f);
            }
            if (mapId == "Map02")
            {
                wallSprite = rock;
                accentSprite = fallenLog;
                detailSprite = rock;
                wallHeight = 0.9f;
                accentHeight = 0.8f;
                detailHeight = 0.65f;
                propSpacing = 1.45f;
                accentTint = new Color(0.85f, 0.9f, 0.78f);
            }

            GameObject mazeWalls = new GameObject("MazeWalls");
            mazeWalls.transform.SetParent(root.transform);
            List<MazeArea> boundarySegments = ExtractMazeBoundarySegments(mask);
            for (int i = 0; i < boundarySegments.Count; i++)
            {
                CreateMazeWallSegment(mazeWalls.transform,
                    "MazeWall_" + (i + 1).ToString("00"),
                    boundarySegments[i], wallSprite, accentSprite, detailSprite,
                    wallHeight, accentHeight, detailHeight, propSpacing,
                    accentTint);
            }

            CreateMazeInteriorClutter(mapId, root.transform, layout, tree, rock,
                fallenLog, shrine, crystal);
            if (mapId == "Map02")
            {
                CreateDeepForestScenery(root.transform, layout, tree);
                Material material = Map02GroundMaterial();
                foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.drawMode == SpriteDrawMode.Tiled)
                        renderer.sharedMaterial = material;
            }
            if (mapId == "Map01")
            {
                CreateMap01SlimeBiome(root.transform, layout, biomeFloor);
                Material material=Map01GroundMaterial();
                foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>())
                    if(renderer.drawMode==SpriteDrawMode.Tiled)
                        renderer.sharedMaterial=material;
                // Tall scenery sits in blocked areas, never in combat lanes.
                var grove=new GameObject("OuterGroveV8");
                grove.transform.SetParent(root.transform,false);
                for(float y=-13;y<=13;y+=3.5f)
                for(float x=-21;x<=21;x+=3.8f)
                {
                    Vector2 p=new Vector2(x,y);
                    bool clear=true;
                    for(int dx=-2;dx<=2;dx++)
                    for(int dy=-2;dy<=2;dy++)
                        if(IsInsideMazeShape(p+new Vector2(dx,dy)*0.65f,layout)) clear=false;
                    if(!clear) continue;
                    var prop=CreateSpriteProp("GroveTree",grove.transform,tree,
                        new Vector3(x,y,0),3.2f,-1,((int)x&1)==0);
                    prop.GetComponent<SpriteRenderer>().color=new Color(0.56f,0.7f,0.66f);
                }
            }
        }

        private static Material Map02GroundMaterial()
        {
            const string path = "Assets/Settings/Map02GroundV3.mat";
            const string maskPath = "Assets/Settings/Map02GroundBlendV3.asset";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/DeepForestGround.shader");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                EnvironmentArtRoot + "/Map02_LeafSoilV3.png");
            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
            const int width = 176, height = 120;
            if (mask == null)
            {
                mask = new Texture2D(width, height, TextureFormat.RGBA32, false);
                mask.name = "Map02GroundBlendV3";
                AssetDatabase.CreateAsset(mask, maskPath);
            }
            mask.wrapMode = TextureWrapMode.Clamp;
            mask.filterMode = FilterMode.Bilinear;
            MazeLayout layout = CreateMap02MazeLayout();
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2((x + 0.5f) * 0.25f - 22f,
                    (y + 0.5f) * 0.25f - 15f);
                float value = 0f;
                for (int dy = -3; dy <= 3; dy++)
                for (int dx = -3; dx <= 3; dx++)
                    if (IsInsideMazeShape(position + new Vector2(dx, dy) * 0.25f, layout))
                        value++;
                pixels[y * width + x] = new Color(value / 49f, 0f, 0f, 1f);
            }
            mask.SetPixels(pixels);
            mask.Apply();
            material.SetTexture("_WalkMask", mask);
            EditorUtility.SetDirty(mask);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateDeepForestScenery(Transform parent,
            MazeLayout layout, Sprite tree)
        {
            GameObject grove = new GameObject("DeepForestGroveV3");
            grove.transform.SetParent(parent, false);
            // Large canopies belong outside the combat silhouette, not on its rim.
            for (int row = 0; row < 11; row++)
            for (int column = 0; column < 18; column++)
            {
                int seed = row * 137 + column * 41;
                Vector2 position = new Vector2(-29f + column * 3.4f,
                    -18f + row * 3.6f);
                position += new Vector2(MazeNoise01(seed) - 0.5f,
                    MazeNoise01(seed + 17) - 0.5f);
                bool clear = true;
                for (int dy = -3; dy <= 3; dy++)
                for (int dx = -3; dx <= 3; dx++)
                    if (IsInsideMazeShape(position + new Vector2(dx, dy) * 0.65f, layout))
                        clear = false;
                if (!clear) continue;
                GameObject prop = CreateSpriteProp("ForestCanopy_" + row + "_" + column,
                    grove.transform, tree, position,
                    Mathf.Lerp(3.2f, 4.4f, MazeNoise01(seed + 31)), -1,
                    MazeNoise01(seed + 57) > 0.5f);
                prop.GetComponent<SpriteRenderer>().color = Color.Lerp(
                    new Color(0.52f, 0.72f, 0.69f),
                    new Color(0.94f, 0.84f, 0.61f), MazeNoise01(seed + 73));
            }
        }

        private static Material Map01GroundMaterial()
        {
            const string path="Assets/Settings/Map01GroundV8.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/WorldMossGround.shader");
            if(material==null) {material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.shader=shader;
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(EnvironmentArtRoot+"/Map01_MossSoilV8.png");
            const string maskPath="Assets/Settings/Map01GroundBlendV8.asset";
            var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
            if(mask==null)
            {
                mask=new Texture2D(176,120,TextureFormat.RGBA32,false);
                mask.name="Map01GroundBlendV8";
                AssetDatabase.CreateAsset(mask,maskPath);
            }
            mask.wrapMode=TextureWrapMode.Clamp;mask.filterMode=FilterMode.Bilinear;
            var layout=CreateMap01MazeLayout();
            var pixels=new Color[176*120];
            for(int y=0;y<120;y++)
            for(int x=0;x<176;x++)
            {
                Vector2 p=new Vector2((x+0.5f)*0.25f-22,(y+0.5f)*0.25f-15);
                float total=0;
                for(int dy=-3;dy<=3;dy++)
                for(int dx=-3;dx<=3;dx++)
                    if(IsInsideMazeShape(p+new Vector2(dx,dy)*0.25f,layout)) total++;
                pixels[y*176+x]=new Color(total/49,total/49,total/49,1);
            }
            mask.SetPixels(pixels);mask.Apply();EditorUtility.SetDirty(mask);
            material.SetTexture("_WalkMask",mask);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateMap01SlimeBiome(Transform parent,
            MazeLayout layout, Sprite floorSprite)
        {
            GameObject biome = new GameObject("SlimeMarshBiome");
            biome.transform.SetParent(parent);
            Color trailTint = new Color(0.9f, 0.87f, 0.73f, 0.12f);
            int trailIndex = 0;
            foreach (MazeCorridor corridor in layout.Corridors)
            {
                for (int pointIndex = 1;
                    pointIndex < corridor.Points.Length; pointIndex++)
                {
                    Vector2 from = corridor.Points[pointIndex - 1];
                    Vector2 to = corridor.Points[pointIndex];
                    Vector2 center = (from + to) * 0.5f;
                    bool horizontal = Mathf.Abs(to.x - from.x) > 0.001f;
                    float length = Vector2.Distance(from, to) + 0.45f;
                    Vector2 size = horizontal
                        ? new Vector2(length, Mathf.Max(0.9f,
                            corridor.Width * 0.34f))
                        : new Vector2(Mathf.Max(0.9f,
                            corridor.Width * 0.34f), length);
                    CreateTiledArea("SlimeTrail_" +
                        (++trailIndex).ToString("00"), biome.transform,
                        floorSprite, new Vector3(center.x, center.y, 0.7f),
                        size, trailTint, -8);
                }
            }

            int[] clearingLobes = { 1, 2, 4, 5, 7 };
            foreach (int lobeIndex in clearingLobes)
            {
                if (lobeIndex < 0 || lobeIndex >= layout.Lobes.Length) continue;
                MazeLobe lobe = layout.Lobes[lobeIndex];
                CreateTiledArea("SlimeCombatClearing_" +
                    (lobeIndex + 1).ToString("00"), biome.transform,
                    floorSprite, new Vector3(lobe.Center.x, lobe.Center.y,
                        0.72f), lobe.Radius * 1.45f,
                    new Color(0.78f, 0.92f, 0.78f, 0.08f), -8);
            }

            Sprite pond = GeneratedPropSprite("ForestPondRuinClusterV1");
            Vector2[] nestPositions = SlimeNestPositions();
            for (int i = 0; i < nestPositions.Length; i++)
            {
                CreateSpriteProp("SlimeNestGround_" +
                    (i + 1).ToString("00"), biome.transform, pond,
                    new Vector3(nestPositions[i].x, nestPositions[i].y,
                        0f), 2.1f, -7, i % 2 == 1);
            }
        }

        private static Vector2[] SlimeNestPositions()
        {
            return new[]
            {
                new Vector2(-11f, -6f),
                new Vector2(-2f, -1f),
                new Vector2(5f, 7f),
                new Vector2(13f, -3f)
            };
        }

        private static void CreateMazeTopologyMarkers(Transform parent,
            MazeLayout layout)
        {
            GameObject topology = new GameObject("MazeTopology");
            topology.transform.SetParent(parent);
            for (int i = 0; i < layout.Lobes.Length; i++)
            {
                MazeLobe lobe = layout.Lobes[i];
                GameObject marker = new GameObject("MazeLobe_" +
                    (i + 1).ToString("00"));
                marker.transform.SetParent(topology.transform);
                marker.transform.position = new Vector3(lobe.Center.x,
                    lobe.Center.y, 0f);
                marker.transform.localScale = new Vector3(
                    lobe.Radius.x * 2f, lobe.Radius.y * 2f, 1f);
            }

            for (int i = 0; i < layout.Corridors.Length; i++)
            {
                GameObject corridorMarker = new GameObject("MazeCorridor_" +
                    (i + 1).ToString("00"));
                corridorMarker.transform.SetParent(topology.transform);
                MazeCorridor corridor = layout.Corridors[i];
                corridorMarker.transform.localScale = new Vector3(
                    corridor.Width, corridor.Width, 1f);
                for (int p = 0; p < corridor.Points.Length; p++)
                {
                    GameObject point = new GameObject("Point_" +
                        (p + 1).ToString("00"));
                    point.transform.SetParent(corridorMarker.transform);
                    point.transform.position = new Vector3(
                        corridor.Points[p].x, corridor.Points[p].y, 0f);
                }
            }
        }

        private static MazeMask BuildMazeMask(MazeLayout layout)
        {
            MazeMask mask = new MazeMask(layout.ArenaSize);
            for (int y = 0; y < mask.Height; y++)
            {
                for (int x = 0; x < mask.Width; x++)
                {
                    mask.Walkable[x, y] = IsInsideMazeShape(
                        mask.CellCenter(x, y), layout);
                }
            }

            return mask;
        }

        private static bool IsInsideMazeShape(Vector2 point, MazeLayout layout)
        {
            for (int i = 0; i < layout.Lobes.Length; i++)
            {
                MazeLobe lobe = layout.Lobes[i];
                Vector2 delta = point - lobe.Center;
                float normalizedX = delta.x / Mathf.Max(0.01f, lobe.Radius.x);
                float normalizedY = delta.y / Mathf.Max(0.01f, lobe.Radius.y);
                if (normalizedX * normalizedX + normalizedY * normalizedY <= 1f)
                {
                    return true;
                }
            }

            for (int i = 0; i < layout.Corridors.Length; i++)
            {
                MazeCorridor corridor = layout.Corridors[i];
                float halfWidth = corridor.Width * 0.5f;
                // Overlap cardinal runs at each waypoint so right-angle turns
                // remain open without adding extra curved navigation cells.
                for (int p = 1; p < corridor.Points.Length; p++)
                {
                    Vector2 from = corridor.Points[p - 1];
                    Vector2 to = corridor.Points[p];
                    if (Mathf.Abs(from.y - to.y) <= 0.001f &&
                        point.x >= Mathf.Min(from.x, to.x) - halfWidth &&
                        point.x <= Mathf.Max(from.x, to.x) + halfWidth &&
                        Mathf.Abs(point.y - from.y) <= halfWidth)
                    {
                        return true;
                    }

                    if (Mathf.Abs(from.x - to.x) <= 0.001f &&
                        point.y >= Mathf.Min(from.y, to.y) - halfWidth &&
                        point.y <= Mathf.Max(from.y, to.y) + halfWidth &&
                        Mathf.Abs(point.x - from.x) <= halfWidth)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void CreateOrganicMazeFloor(Transform parent, string mapId,
            bool isCave, Sprite floorSprite, MazeMask mask)
        {
            List<MazeArea> floorAreas = MergeWalkableRectangles(mask);
            Color floorTint = Color.Lerp(EnvironmentTint(mapId, isCave),
                Color.white, isCave ? 0.16f : 0.1f);
            if(mapId=="Map01") floorTint=new Color(1.08f,1.13f,1.06f);
            for (int i = 0; i < floorAreas.Count; i++)
            {
                MazeArea area = floorAreas[i];
                CreateTiledArea("WalkableFloor_" + (i + 1).ToString("000"),
                    parent, floorSprite,
                    new Vector3(area.Center.x, area.Center.y, 0.8f),
                    area.Size + Vector2.one * 0.03f, floorTint, -9);
            }
        }

        private static List<MazeArea> MergeWalkableRectangles(MazeMask mask)
        {
            bool[,] used = new bool[mask.Width, mask.Height];
            List<MazeArea> areas = new List<MazeArea>();
            for (int y = 0; y < mask.Height; y++)
            {
                for (int x = 0; x < mask.Width; x++)
                {
                    if (!mask.Walkable[x, y] || used[x, y])
                    {
                        continue;
                    }

                    int width = 1;
                    while (x + width < mask.Width &&
                        mask.Walkable[x + width, y] && !used[x + width, y])
                    {
                        width++;
                    }

                    int height = 1;
                    bool canExtend = true;
                    while (y + height < mask.Height && canExtend)
                    {
                        for (int offset = 0; offset < width; offset++)
                        {
                            if (!mask.Walkable[x + offset, y + height] ||
                                used[x + offset, y + height])
                            {
                                canExtend = false;
                                break;
                            }
                        }

                        if (canExtend)
                        {
                            height++;
                        }
                    }

                    for (int markY = 0; markY < height; markY++)
                    {
                        for (int markX = 0; markX < width; markX++)
                        {
                            used[x + markX, y + markY] = true;
                        }
                    }

                    Vector2 minimum = mask.Minimum + new Vector2(
                        x * MazeMask.CellSize, y * MazeMask.CellSize);
                    Vector2 size = new Vector2(width * MazeMask.CellSize,
                        height * MazeMask.CellSize);
                    areas.Add(new MazeArea(minimum.x + size.x * 0.5f,
                        minimum.y + size.y * 0.5f, size.x, size.y));
                }
            }

            return areas;
        }

        private static List<MazeArea> ExtractMazeBoundarySegments(MazeMask mask)
        {
            const float wallThickness = 0.68f;
            const float cornerOverlap = 0.08f;
            List<MazeArea> segments = new List<MazeArea>();

            for (int edgeY = 0; edgeY <= mask.Height; edgeY++)
            {
                int x = 0;
                while (x < mask.Width)
                {
                    bool below = edgeY > 0 && mask.Walkable[x, edgeY - 1];
                    bool above = edgeY < mask.Height && mask.Walkable[x, edgeY];
                    if (below == above)
                    {
                        x++;
                        continue;
                    }

                    int start = x;
                    while (x < mask.Width)
                    {
                        below = edgeY > 0 && mask.Walkable[x, edgeY - 1];
                        above = edgeY < mask.Height && mask.Walkable[x, edgeY];
                        if (below == above)
                        {
                            break;
                        }
                        x++;
                    }

                    float length = (x - start) * MazeMask.CellSize;
                    float centerX = mask.Minimum.x +
                        (start + (x - start) * 0.5f) * MazeMask.CellSize;
                    float worldY = mask.Minimum.y + edgeY * MazeMask.CellSize;
                    segments.Add(new MazeArea(centerX, worldY,
                        length + cornerOverlap, wallThickness));
                }
            }

            for (int edgeX = 0; edgeX <= mask.Width; edgeX++)
            {
                int y = 0;
                while (y < mask.Height)
                {
                    bool left = edgeX > 0 && mask.Walkable[edgeX - 1, y];
                    bool right = edgeX < mask.Width && mask.Walkable[edgeX, y];
                    if (left == right)
                    {
                        y++;
                        continue;
                    }

                    int start = y;
                    while (y < mask.Height)
                    {
                        left = edgeX > 0 && mask.Walkable[edgeX - 1, y];
                        right = edgeX < mask.Width && mask.Walkable[edgeX, y];
                        if (left == right)
                        {
                            break;
                        }
                        y++;
                    }

                    float length = (y - start) * MazeMask.CellSize;
                    float worldX = mask.Minimum.x + edgeX * MazeMask.CellSize;
                    float centerY = mask.Minimum.y +
                        (start + (y - start) * 0.5f) * MazeMask.CellSize;
                    segments.Add(new MazeArea(worldX, centerY,
                        wallThickness, length + cornerOverlap));
                }
            }

            return segments;
        }

        private static void CreateMazeWallSegment(Transform parent, string name,
            MazeArea segment, Sprite wallSprite, Sprite accentSprite,
            Sprite detailSprite, float wallHeight, float accentHeight,
            float detailHeight, float spacing, Color accentTint)
        {
            GameObject wall = CreateSizedBlock(name, parent,
                new Vector3(segment.Center.x, segment.Center.y, 0f),
                segment.Size, Color.clear, true, 0);
            CreateMazeWallVisualLine(wall.transform, name, segment, wallSprite,
                accentSprite, detailSprite, wallHeight, accentHeight,
                detailHeight, spacing, accentTint);
        }

        private static void CreateMazeWallVisualLine(Transform parent, string name,
            MazeArea segment, Sprite wallSprite, Sprite accentSprite,
            Sprite detailSprite, float wallHeight, float accentHeight,
            float detailHeight, float spacing, Color accentTint)
        {
            bool horizontal = segment.Size.x >= segment.Size.y;
            float length = horizontal ? segment.Size.x : segment.Size.y;
            float inset = Mathf.Min(0.28f, length * 0.25f);
            float cursor = -length * 0.5f + inset;
            float end = length * 0.5f - inset;
            int seed = StableMazeHash(name);
            int index = 0;

            do
            {
                float alongJitter = Mathf.Lerp(-0.16f, 0.16f,
                    MazeNoise01(seed + index * 37));
                float normalJitter = Mathf.Lerp(-0.3f, 0.3f,
                    MazeNoise01(seed + index * 53 + 11));
                float along = Mathf.Clamp(cursor + alongJitter,
                    -length * 0.5f, length * 0.5f);
                Vector2 position = segment.Center + (horizontal
                    ? new Vector2(along, normalJitter)
                    : new Vector2(normalJitter, along));
                float choice = MazeNoise01(seed + index * 71 + 23);
                Sprite sprite = wallSprite;
                float height = wallHeight;
                bool useAccent = choice >= 0.7f && choice < 0.93f &&
                    accentSprite != null;
                if (useAccent)
                {
                    sprite = accentSprite;
                    height = accentHeight;
                }
                else if (choice >= 0.93f && detailSprite != null)
                {
                    sprite = detailSprite;
                    height = detailHeight;
                }

                height *= Mathf.Lerp(0.82f, 1.18f,
                    MazeNoise01(seed + index * 97 + 41));
                GameObject prop = CreateSpriteProp(name + "_Visual_" +
                    (index + 1).ToString("00"), parent, sprite,
                    new Vector3(position.x, position.y, 0f), height, 4,
                    MazeNoise01(seed + index * 113 + 7) > 0.5f);
                if (useAccent && sprite != null)
                {
                    prop.GetComponent<SpriteRenderer>().color = accentTint;
                }

                float variableSpacing = spacing * Mathf.Lerp(0.72f, 1.28f,
                    MazeNoise01(seed + index * 131 + 19));
                cursor += Mathf.Max(0.55f, variableSpacing);
                index++;
            }
            while (cursor <= end + 0.05f && index < 128);
        }

        private static int StableMazeHash(string value)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }
                return hash;
            }
        }

        private static float MazeNoise01(int seed)
        {
            unchecked
            {
                uint value = (uint)seed;
                value ^= value >> 16;
                value *= 0x7feb352d;
                value ^= value >> 15;
                value *= 0x846ca68b;
                value ^= value >> 16;
                return (value & 0x00ffffff) / 16777215f;
            }
        }

        private static void CreateMazeInteriorClutter(string mapId,
            Transform parent, MazeLayout layout, Sprite tree, Sprite rock,
            Sprite fallenLog, Sprite shrine, Sprite crystal)
        {
            string[] groupNames =
            {
                "Clutter_Entrance", "Clutter_LobeEdges",
                "Clutter_Junctions", "Clutter_SideBranches",
                "Clutter_Landmark", "Clutter_CombatRooms",
                "Clutter_RewardRoom", "Clutter_Exit"
            };
            GameObject[] groups = new GameObject[groupNames.Length];
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i] = new GameObject(groupNames[i]);
                groups[i].transform.SetParent(parent);
            }

            bool isCave = mapId.StartsWith("Cave",
                StringComparison.OrdinalIgnoreCase);
            bool landmarkColliderCreated = false;
            for (int i = 0; i < layout.Clutter.Length; i++)
            {
                MazeClutter clutter = layout.Clutter[i];
                Sprite sprite;
                switch (clutter.Kind)
                {
                    case MazeClutterKind.Tree:
                        sprite = tree;
                        break;
                    case MazeClutterKind.Rock:
                        sprite = rock;
                        break;
                    case MazeClutterKind.FallenLog:
                        sprite = fallenLog;
                        break;
                    case MazeClutterKind.Shrine:
                        sprite = isCave ? crystal : shrine;
                        break;
                    case MazeClutterKind.Supplies:
                        sprite = GeneratedPropSprite("ForgeSupplyClusterV3");
                        break;
                    case MazeClutterKind.Crystal:
                        sprite = crystal;
                        break;
                    default:
                        sprite = GeneratedPropSprite(isCave
                            ? "CavePoolMineClusterV1"
                            : "ForestPondRuinClusterV1");
                        break;
                }

                GameObject group = groups[clutter.Group];
                GameObject prop = DecorProp(group,
                    clutter.Kind + "_" + (i + 1).ToString("00"), sprite,
                    clutter.Position.x, clutter.Position.y, clutter.Height,
                    clutter.FlipX, clutter.Kind == MazeClutterKind.Landmark
                        ? 3 : 2);
                if (isCave && (clutter.Kind == MazeClutterKind.Rock ||
                    clutter.Kind == MazeClutterKind.Supplies))
                {
                    prop.GetComponent<SpriteRenderer>().color =
                        new Color(0.8f, 0.88f, 1f);
                }

                if (!landmarkColliderCreated &&
                    clutter.Kind == MazeClutterKind.Landmark &&
                    layout.LandmarkColliderSize.sqrMagnitude > 0.01f)
                {
                    CreateSizedBlock("SceneryCollider_Landmark",
                        group.transform,
                        new Vector3(clutter.Position.x, clutter.Position.y, 0f),
                        layout.LandmarkColliderSize, Color.clear, true, 0);
                    landmarkColliderCreated = true;
                }
            }
        }

        private static void ValidateMazeLayout(string mapId, MazeLayout layout,
            MazeMask mask)
        {
            for (int i = 0; i < layout.Corridors.Length; i++)
            {
                MazeCorridor corridor = layout.Corridors[i];
                if (corridor.Width < 2.8f)
                {
                    throw new InvalidOperationException(mapId +
                        " has a corridor narrower than 2.8 world units.");
                }
                if (corridor.Points.Length < 2)
                {
                    throw new InvalidOperationException(mapId +
                        " has a corridor without two cardinal points.");
                }
                for (int p = 1; p < corridor.Points.Length; p++)
                {
                    Vector2 delta = corridor.Points[p] - corridor.Points[p - 1];
                    bool horizontal = Mathf.Abs(delta.y) <= 0.001f &&
                        Mathf.Abs(delta.x) > 0.001f;
                    bool vertical = Mathf.Abs(delta.x) <= 0.001f &&
                        Mathf.Abs(delta.y) > 0.001f;
                    if (!horizontal && !vertical)
                    {
                        throw new InvalidOperationException(mapId +
                            " has a non-cardinal or zero-length corridor segment.");
                    }
                }
            }

            bool[,] navigable = new bool[mask.Width, mask.Height];
            for (int y = 0; y < mask.Height; y++)
            {
                for (int x = 0; x < mask.Width; x++)
                {
                    Vector2 point = mask.CellCenter(x, y);
                    bool landmarkBlocked =
                        layout.LandmarkColliderSize.sqrMagnitude > 0.01f &&
                        Mathf.Abs(point.x - layout.LandmarkPosition.x) <=
                            layout.LandmarkColliderSize.x * 0.5f + 0.65f &&
                        Mathf.Abs(point.y - layout.LandmarkPosition.y) <=
                            layout.LandmarkColliderSize.y * 0.5f + 0.65f;
                    navigable[x, y] = mask.Walkable[x, y] && !landmarkBlocked;
                }
            }

            Vector2Int start = MazeCellForWorld(mask, layout.PlayerSpawn);
            if (!mask.Contains(start.x, start.y) || !navigable[start.x, start.y])
            {
                throw new InvalidOperationException(mapId +
                    " player spawn is outside the organic maze floor.");
            }

            bool[,] visited = new bool[mask.Width, mask.Height];
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited[start.x, start.y] = true;
            int[] offsetsX = { 1, -1, 0, 0 };
            int[] offsetsY = { 0, 0, 1, -1 };
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                for (int i = 0; i < offsetsX.Length; i++)
                {
                    int nextX = cell.x + offsetsX[i];
                    int nextY = cell.y + offsetsY[i];
                    if (!mask.Contains(nextX, nextY) ||
                        visited[nextX, nextY] || !navigable[nextX, nextY])
                    {
                        continue;
                    }
                    visited[nextX, nextY] = true;
                    queue.Enqueue(new Vector2Int(nextX, nextY));
                }
            }

            List<Vector3> goals = new List<Vector3>
            {
                layout.ExitPosition, layout.ChestPosition, layout.ItemPosition
            };
            if (mapId == "BossMap01")
            {
                goals.Add(layout.BossPosition);
            }
            goals.AddRange(layout.PrimaryEnemyPositions);
            goals.AddRange(layout.SecondaryEnemyPositions);
            for (int i = 0; i < goals.Count; i++)
            {
                Vector2Int cell = MazeCellForWorld(mask, goals[i]);
                if (!mask.Contains(cell.x, cell.y) || !visited[cell.x, cell.y])
                {
                    throw new InvalidOperationException(mapId +
                        " has an unreachable gameplay position at " + goals[i] + ".");
                }
            }
        }

        private static Vector2Int MazeCellForWorld(MazeMask mask,
            Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt((worldPosition.x - mask.Minimum.x) /
                    MazeMask.CellSize),
                Mathf.FloorToInt((worldPosition.y - mask.Minimum.y) /
                    MazeMask.CellSize));
        }

        private static GameObject DecorProp(GameObject root, string name,
            Sprite sprite, float x, float y, float height, bool flipX,
            int sortingOrder = 2)
        {
            return DecorProp(root, name, sprite, x, y, height, flipX,
                sortingOrder, Color.white);
        }

        private static GameObject DecorProp(GameObject root, string name,
            Sprite sprite, float x, float y, float height, bool flipX,
            int sortingOrder, Color tint)
        {
            GameObject prop = CreateSpriteProp(name, root.transform, sprite,
                new Vector3(x, y, 0f), height, sortingOrder, flipX);
            prop.GetComponent<SpriteRenderer>().color = sprite == null
                ? Color.magenta
                : tint;
            return prop;
        }

        private static MazeLayout MazeLayoutFor(string mapId)
        {
            switch (mapId)
            {
                case "Map01":
                    return CreateMap01MazeLayout();
                case "Map02":
                    return CreateMap02MazeLayout();
                case "Map03":
                    return CreateMap03MazeLayout();
                case "BossMap01":
                    return CreateBossMazeLayout();
                case "CaveMap01":
                    return CreateCaveMazeLayout();
                case "RiftMap01":
                    return CreateRiftMazeLayout();
                default:
                    return null;
            }
        }

        private static MazeLayout CreateMap01MazeLayout()
        {
            return new MazeLayout
            {
                ArenaSize = new Vector2(44f, 30f),
                PlayerSpawn = new Vector3(-18f, -9f, 0f),
                ExitPosition = new Vector3(18f, 8f, 0f),
                ChestPosition = new Vector3(15f, 10f, 0f),
                ItemPosition = new Vector3(4f, -9f, 0f),
                LandmarkPosition = new Vector3(-12f, 8f, 0f),
                LandmarkColliderSize = new Vector2(2.4f, 1.3f),
                Lobes = new[]
                {
                    new MazeLobe(-18f, -9f, 3.2f, 3f),
                    new MazeLobe(-11f, -6f, 4.2f, 3.6f),
                    new MazeLobe(-2f, -1f, 5.2f, 4.5f),
                    new MazeLobe(-12f, 8f, 3.5f, 3f),
                    new MazeLobe(4f, -9f, 4.2f, 3.2f),
                    new MazeLobe(5f, 7f, 4.5f, 3.5f),
                    new MazeLobe(16f, 8f, 4.2f, 4f),
                    new MazeLobe(13f, -3f, 4.5f, 3.5f)
                },
                Corridors = new[]
                {
                    new MazeCorridor(4.2f, new Vector2(-18f, -9f),
                        new Vector2(-18f, -6f), new Vector2(-11f, -6f)),
                    new MazeCorridor(4.4f, new Vector2(-11f, -6f),
                        new Vector2(-6f, -6f), new Vector2(-6f, -1f),
                        new Vector2(-2f, -1f)),
                    new MazeCorridor(3.6f, new Vector2(-11f, -6f),
                        new Vector2(-11f, 8f), new Vector2(-12f, 8f)),
                    new MazeCorridor(3.6f, new Vector2(-12f, 8f),
                        new Vector2(-5f, 8f), new Vector2(5f, 8f),
                        new Vector2(5f, 7f)),
                    new MazeCorridor(4.2f, new Vector2(-2f, -1f),
                        new Vector2(2f, -1f), new Vector2(2f, 7f),
                        new Vector2(5f, 7f)),
                    new MazeCorridor(4.2f, new Vector2(-2f, -1f),
                        new Vector2(-2f, -5f), new Vector2(4f, -5f),
                        new Vector2(4f, -9f)),
                    new MazeCorridor(4f, new Vector2(4f, -9f),
                        new Vector2(10f, -9f), new Vector2(10f, -3f),
                        new Vector2(13f, -3f)),
                    new MazeCorridor(4.4f, new Vector2(5f, 7f),
                        new Vector2(10f, 7f), new Vector2(10f, 8f),
                        new Vector2(16f, 8f)),
                    new MazeCorridor(4f, new Vector2(13f, -3f),
                        new Vector2(16f, -3f), new Vector2(16f, 8f))
                },
                Clutter = new[]
                {
                    new MazeClutter(0, MazeClutterKind.Tree, -19.2f, -7.2f, 1.7f),
                    new MazeClutter(1, MazeClutterKind.Rock, -13.5f, -4.2f, 0.82f, true),
                    new MazeClutter(2, MazeClutterKind.FallenLog, -5.6f, -3.5f, 1.1f),
                    new MazeClutter(3, MazeClutterKind.Shrine, -1.8f, 1.8f, 1.9f),
                    new MazeClutter(4, MazeClutterKind.Landmark, -12f, 8f, 3.1f),
                    new MazeClutter(5, MazeClutterKind.Tree, 6.8f, 8.7f, 1.55f, true),
                    new MazeClutter(6, MazeClutterKind.Rock, 15f, 9.8f, 0.78f),
                    new MazeClutter(7, MazeClutterKind.Tree, 17.8f, 6.2f, 1.65f),
                    new MazeClutter(5, MazeClutterKind.Supplies, 12.8f, -1.2f, 1.05f)
                },
                PrimaryEnemyPositions = new[]
                {
                    new Vector3(-13f, -7f, 0f),
                    new Vector3(-10f, -5f, 0f),
                    new Vector3(-4f, -2f, 0f),
                    new Vector3(-1f, 1f, 0f),
                    new Vector3(1f, -1f, 0f),
                    new Vector3(4f, 6f, 0f),
                    new Vector3(7f, 7f, 0f),
                    new Vector3(3f, -9f, 0f),
                    new Vector3(6f, -8f, 0f),
                    new Vector3(12f, -3f, 0f),
                    new Vector3(15f, -2f, 0f),
                    new Vector3(15f, 7f, 0f)
                }
            };
        }

        private static MazeLayout CreateMap02MazeLayout()
        {
            return PaintedForestLayout();
        }

        // Retained solely as an explicit record of the rejected V3 layout.
        private static MazeLayout LegacyMap02MazeLayout()
        {
            return new MazeLayout
            {
                ArenaSize = new Vector2(44f, 30f),
                PlayerSpawn = new Vector3(-17f, 8f, 0f),
                ExitPosition = new Vector3(16f, -7f, 0f),
                ChestPosition = new Vector3(13f, 9f, 0f),
                ItemPosition = new Vector3(8f, 3f, 0f),
                LandmarkPosition = new Vector3(16.5f, 1f, 0f),
                LandmarkColliderSize = new Vector2(2.5f, 1.35f),
                Lobes = new[]
                {
                    new MazeLobe(-17f, 8f, 3f, 3f),
                    new MazeLobe(-11f, 1f, 4.6f, 4.1f),
                    new MazeLobe(-14f, -8f, 4.1f, 3.6f),
                    new MazeLobe(-4f, -5f, 5.1f, 4.1f),
                    new MazeLobe(-3f, 7f, 4.6f, 3.7f),
                    new MazeLobe(6f, 1f, 5.2f, 4.5f),
                    new MazeLobe(13f, 8f, 4.1f, 3.7f),
                    new MazeLobe(14f, -7f, 4.7f, 4.1f),
                    new MazeLobe(16.5f, 1f, 3f, 2.6f)
                },
                Corridors = new[]
                {
                    new MazeCorridor(3.5f, new Vector2(-17f, 8f),
                        new Vector2(-11f, 8f), new Vector2(-11f, 1f)),
                    new MazeCorridor(3.5f, new Vector2(-11f, 1f),
                        new Vector2(-15f, 1f), new Vector2(-15f, -8f),
                        new Vector2(-14f, -8f)),
                    new MazeCorridor(3.5f, new Vector2(-14f, -8f),
                        new Vector2(-4f, -8f), new Vector2(-4f, -5f)),
                    new MazeCorridor(3.5f, new Vector2(-11f, 1f),
                        new Vector2(-7f, 1f), new Vector2(-7f, 7f),
                        new Vector2(-3f, 7f)),
                    new MazeCorridor(3.5f, new Vector2(-3f, 7f),
                        new Vector2(6f, 7f), new Vector2(6f, 1f)),
                    new MazeCorridor(3.5f, new Vector2(-4f, -5f),
                        new Vector2(6f, -5f), new Vector2(6f, 1f)),
                    new MazeCorridor(3.2f, new Vector2(6f, 1f),
                        new Vector2(10f, 1f), new Vector2(10f, 8f),
                        new Vector2(13f, 8f)),
                    new MazeCorridor(3.5f, new Vector2(6f, 1f),
                        new Vector2(10f, 1f), new Vector2(10f, -7f),
                        new Vector2(14f, -7f)),
                    new MazeCorridor(2.8f, new Vector2(6f, 1f),
                        new Vector2(16.5f, 1f))
                },
                Clutter = new[]
                {
                    new MazeClutter(0, MazeClutterKind.Tree, -18.4f, 6.6f, 1.9f),
                    new MazeClutter(1, MazeClutterKind.Rock, -14.1f, 3.2f, 0.9f),
                    new MazeClutter(2, MazeClutterKind.FallenLog, -8f, -0.8f, 1.25f, true),
                    new MazeClutter(3, MazeClutterKind.Shrine, -1.6f, 9.1f, 2.5f),
                    new MazeClutter(4, MazeClutterKind.Landmark, 16.5f, 1f, 3.25f),
                    new MazeClutter(5, MazeClutterKind.FallenLog, -6.6f, -2.7f, 1.1f, true),
                    new MazeClutter(6, MazeClutterKind.Rock, 15.4f, 9.4f, 0.82f),
                    new MazeClutter(7, MazeClutterKind.Shrine, 16.3f, -4.5f, 1.6f),
                    new MazeClutter(3, MazeClutterKind.Supplies, 3.1f, 3.4f, 1.15f)
                },
                PrimaryEnemyPositions = new[]
                {
                    new Vector3(-12f, 0f, 0f),
                    new Vector3(-10f, 2f, 0f),
                    new Vector3(-14f, -8f, 0f),
                    new Vector3(-5f, -5f, 0f),
                    new Vector3(-3f, -4f, 0f),
                    new Vector3(5f, 0f, 0f),
                    new Vector3(7f, 2f, 0f),
                    new Vector3(13f, -7f, 0f)
                },
                SecondaryEnemyPositions = new[]
                {
                    new Vector3(-3f, 7f, 0f),
                    new Vector3(12f, 7f, 0f),
                    new Vector3(-2f, -6.8f, 0f),
                    new Vector3(15.2f, -8.4f, 0f)
                }
            };
        }

        private static MazeLayout CreateMap03MazeLayout()
        {
            return new MazeLayout
            {
                ArenaSize = new Vector2(42f, 28f),
                PlayerSpawn = new Vector3(-17f, -8f, 0f),
                ExitPosition = new Vector3(18f, 7f, 0f),
                ChestPosition = new Vector3(-5f, 8f, 0f),
                ItemPosition = new Vector3(2f, -8f, 0f),
                LandmarkPosition = new Vector3(-15f, 6f, 0f),
                LandmarkColliderSize = new Vector2(2.4f, 1.3f),
                Lobes = new[]
                {
                    new MazeLobe(-17f, -8f, 3f, 3f),
                    new MazeLobe(-10f, -3f, 3.8f, 3.2f),
                    new MazeLobe(0f, 0f, 5f, 4.5f),
                    new MazeLobe(-5f, 8f, 3.5f, 3f),
                    new MazeLobe(2f, -8f, 4f, 3f),
                    new MazeLobe(10f, -2f, 4f, 3.5f),
                    new MazeLobe(16f, 7f, 3.5f, 3f),
                    new MazeLobe(-15f, 6f, 3f, 2.7f)
                },
                Corridors = new[]
                {
                    new MazeCorridor(3.5f, new Vector2(-17f, -8f),
                        new Vector2(-17f, -3f), new Vector2(-10f, -3f)),
                    new MazeCorridor(3.5f, new Vector2(-10f, -3f),
                        new Vector2(-5f, -3f), new Vector2(-5f, 0f),
                        new Vector2(0f, 0f)),
                    new MazeCorridor(3.2f, new Vector2(0f, 0f),
                        new Vector2(0f, 5f), new Vector2(-5f, 5f),
                        new Vector2(-5f, 8f)),
                    new MazeCorridor(3.2f, new Vector2(-5f, 8f),
                        new Vector2(11f, 8f), new Vector2(11f, 7f),
                        new Vector2(16f, 7f)),
                    new MazeCorridor(3.5f, new Vector2(0f, 0f),
                        new Vector2(5f, 0f), new Vector2(5f, -2f),
                        new Vector2(10f, -2f)),
                    new MazeCorridor(3.2f, new Vector2(10f, -2f),
                        new Vector2(14f, -2f), new Vector2(14f, 7f),
                        new Vector2(16f, 7f)),
                    new MazeCorridor(3.2f, new Vector2(0f, 0f),
                        new Vector2(0f, -8f), new Vector2(2f, -8f)),
                    new MazeCorridor(3.2f, new Vector2(2f, -8f),
                        new Vector2(10f, -8f), new Vector2(10f, -2f)),
                    new MazeCorridor(2.8f, new Vector2(-10f, -3f),
                        new Vector2(-15f, -3f), new Vector2(-15f, 6f)),
                    new MazeCorridor(2.8f, new Vector2(-15f, 6f),
                        new Vector2(-9f, 6f), new Vector2(-9f, 8f),
                        new Vector2(-5f, 8f))
                },
                Clutter = new[]
                {
                    new MazeClutter(0, MazeClutterKind.Tree, -18.2f, -6.4f, 1.8f),
                    new MazeClutter(1, MazeClutterKind.Rock, -11.6f, -1.3f, 0.9f),
                    new MazeClutter(2, MazeClutterKind.FallenLog, -5.8f, -1.8f, 1.15f),
                    new MazeClutter(3, MazeClutterKind.Shrine, 0f, 1.2f, 3.2f),
                    new MazeClutter(4, MazeClutterKind.Landmark, -15f, 6f, 3.2f),
                    new MazeClutter(5, MazeClutterKind.Tree, 1.8f, -6.2f, 1.65f, true),
                    new MazeClutter(6, MazeClutterKind.Rock, -6.2f, 6.6f, 0.85f),
                    new MazeClutter(7, MazeClutterKind.Tree, 17.4f, 8.2f, 1.7f),
                    new MazeClutter(2, MazeClutterKind.Supplies, 8.2f, -0.5f, 1.1f)
                },
                PrimaryEnemyPositions = new[]
                {
                    new Vector3(-11f, -3f, 0f),
                    new Vector3(-2f, -1f, 0f),
                    new Vector3(2f, 1f, 0f),
                    new Vector3(-5f, 6.5f, 0f),
                    new Vector3(2f, -8f, 0f),
                    new Vector3(9f, -2f, 0f),
                    new Vector3(15f, 7f, 0f),
                    new Vector3(-10.5f, 6f, 0f)
                },
                SecondaryEnemyPositions = new[]
                {
                    new Vector3(0f, 2f, 0f),
                    new Vector3(11f, -3f, 0f),
                    new Vector3(12f, 7f, 0f),
                    new Vector3(5f, -8f, 0f)
                }
            };
        }

        private static MazeLayout CreateBossMazeLayout()
        {
            return new MazeLayout
            {
                ArenaSize = new Vector2(46f, 32f),
                PlayerSpawn = new Vector3(-18f, -9f, 0f),
                ExitPosition = new Vector3(19f, 9f, 0f),
                ChestPosition = new Vector3(17f, 8f, 0f),
                ItemPosition = new Vector3(16f, -9f, 0f),
                BossPosition = new Vector3(10f, 0f, 0f),
                LandmarkPosition = new Vector3(-12f, 5f, 0f),
                LandmarkColliderSize = new Vector2(2.5f, 1.35f),
                Lobes = new[]
                {
                    new MazeLobe(-18f, -9f, 3f, 3f),
                    new MazeLobe(-12f, -5f, 4f, 3f),
                    new MazeLobe(-12f, 5f, 3.5f, 3f),
                    new MazeLobe(-5f, 0f, 4f, 3.5f),
                    new MazeLobe(1f, 8f, 3.5f, 3f),
                    new MazeLobe(10f, 0f, 7f, 6.5f),
                    new MazeLobe(18f, 9f, 3.5f, 3f),
                    new MazeLobe(16f, -9f, 3.5f, 3f)
                },
                Corridors = new[]
                {
                    new MazeCorridor(3.5f, new Vector2(-18f, -9f),
                        new Vector2(-12f, -9f), new Vector2(-12f, -5f)),
                    new MazeCorridor(3.5f, new Vector2(-12f, -5f),
                        new Vector2(-5f, -5f), new Vector2(-5f, 0f)),
                    new MazeCorridor(3.2f, new Vector2(-12f, -5f),
                        new Vector2(-15f, -5f), new Vector2(-15f, 5f),
                        new Vector2(-12f, 5f)),
                    new MazeCorridor(3.2f, new Vector2(-12f, 5f),
                        new Vector2(-5f, 5f), new Vector2(-5f, 0f)),
                    new MazeCorridor(3.5f, new Vector2(-5f, 0f),
                        new Vector2(-1f, 0f), new Vector2(-1f, 8f),
                        new Vector2(1f, 8f)),
                    new MazeCorridor(3.5f, new Vector2(1f, 8f),
                        new Vector2(6f, 8f), new Vector2(6f, 4f),
                        new Vector2(10f, 4f)),
                    new MazeCorridor(3.5f, new Vector2(10f, 0f),
                        new Vector2(18f, 0f), new Vector2(18f, 9f)),
                    new MazeCorridor(2.8f, new Vector2(10f, 0f),
                        new Vector2(10f, -9f), new Vector2(16f, -9f))
                },
                Clutter = new[]
                {
                    new MazeClutter(0, MazeClutterKind.Tree, -19.2f, -7.3f, 1.9f),
                    new MazeClutter(1, MazeClutterKind.Rock, -13.8f, -3.9f, 0.9f),
                    new MazeClutter(2, MazeClutterKind.FallenLog, -8.5f, -3.7f, 1.25f),
                    new MazeClutter(3, MazeClutterKind.Shrine, 0.8f, 9.1f, 2.3f),
                    new MazeClutter(4, MazeClutterKind.Landmark, -12f, 5f, 3.3f),
                    new MazeClutter(5, MazeClutterKind.Rock, 13.8f, -2.8f, 0.95f, true),
                    new MazeClutter(6, MazeClutterKind.Tree, 17f, 7f, 1.75f),
                    new MazeClutter(7, MazeClutterKind.Tree, 19.1f, 10f, 1.8f, true),
                    new MazeClutter(3, MazeClutterKind.Supplies, -3.4f, 1.8f, 1.1f)
                }
            };
        }

        private static MazeLayout CreateCaveMazeLayout()
        {
            return new MazeLayout
            {
                ArenaSize = new Vector2(44f, 30f),
                PlayerSpawn = new Vector3(-18f, -9f, 0f),
                ExitPosition = new Vector3(18f, 4f, 0f),
                ChestPosition = new Vector3(16f, 7f, 0f),
                ItemPosition = new Vector3(-4f, -11f, 0f),
                LandmarkPosition = new Vector3(16f, -9f, 0f),
                LandmarkColliderSize = new Vector2(2.6f, 1.45f),
                Lobes = new[]
                {
                    new MazeLobe(-18f, -9f, 3f, 3f),
                    new MazeLobe(-12f, -4f, 4f, 3.5f),
                    new MazeLobe(-5f, -10f, 3.5f, 3f),
                    new MazeLobe(-2f, 0f, 5f, 4f),
                    new MazeLobe(-12f, 7f, 3.5f, 3f),
                    new MazeLobe(6f, 6f, 4f, 3.5f),
                    new MazeLobe(7f, -6f, 4.5f, 3.5f),
                    new MazeLobe(16f, 4f, 4f, 4f),
                    new MazeLobe(16f, -9f, 3.5f, 3f)
                },
                Corridors = new[]
                {
                    new MazeCorridor(3.5f, new Vector2(-18f, -9f),
                        new Vector2(-12f, -9f), new Vector2(-12f, -4f)),
                    new MazeCorridor(3.5f, new Vector2(-12f, -4f),
                        new Vector2(-8f, -4f), new Vector2(-8f, -10f),
                        new Vector2(-5f, -10f)),
                    new MazeCorridor(3.5f, new Vector2(-5f, -10f),
                        new Vector2(-2f, -10f), new Vector2(-2f, 0f)),
                    new MazeCorridor(3.2f, new Vector2(-12f, -4f),
                        new Vector2(-15f, -4f), new Vector2(-15f, 7f),
                        new Vector2(-12f, 7f)),
                    new MazeCorridor(3.2f, new Vector2(-12f, 7f),
                        new Vector2(-2f, 7f), new Vector2(-2f, 0f)),
                    new MazeCorridor(3.5f, new Vector2(-2f, 0f),
                        new Vector2(2f, 0f), new Vector2(2f, 6f),
                        new Vector2(6f, 6f)),
                    new MazeCorridor(3.5f, new Vector2(-2f, 0f),
                        new Vector2(2f, 0f), new Vector2(2f, -6f),
                        new Vector2(7f, -6f)),
                    new MazeCorridor(3.5f, new Vector2(6f, 6f),
                        new Vector2(12f, 6f), new Vector2(12f, 4f),
                        new Vector2(16f, 4f)),
                    new MazeCorridor(3.5f, new Vector2(7f, -6f),
                        new Vector2(12f, -6f), new Vector2(12f, 4f),
                        new Vector2(16f, 4f)),
                    new MazeCorridor(2.8f, new Vector2(7f, -6f),
                        new Vector2(16f, -6f), new Vector2(16f, -9f))
                },
                Clutter = new[]
                {
                    new MazeClutter(0, MazeClutterKind.Crystal, -19f, -7.5f, 1.45f),
                    new MazeClutter(1, MazeClutterKind.Rock, -13.8f, -2.9f, 0.95f),
                    new MazeClutter(2, MazeClutterKind.Supplies, -8.8f, -8.3f, 1.2f),
                    new MazeClutter(3, MazeClutterKind.Shrine, -0.5f, 1.8f, 2.1f),
                    new MazeClutter(4, MazeClutterKind.Landmark, 16f, -9f, 3.6f),
                    new MazeClutter(5, MazeClutterKind.Crystal, 8.6f, -4.6f, 1.4f, true),
                    new MazeClutter(6, MazeClutterKind.Rock, 14.2f, 6f, 0.95f),
                    new MazeClutter(7, MazeClutterKind.Crystal, 18f, 2.5f, 1.45f),
                    new MazeClutter(3, MazeClutterKind.Crystal, -11f, 8.2f, 1.35f, true)
                },
                PrimaryEnemyPositions = new[]
                {
                    new Vector3(-13f, -4f, 0f),
                    new Vector3(-6f, -9f, 0f),
                    new Vector3(-3f, 1f, 0f),
                    new Vector3(-12f, 7f, 0f),
                    new Vector3(6f, 6f, 0f),
                    new Vector3(15f, 3f, 0f)
                },
                SecondaryEnemyPositions = new[]
                {
                    new Vector3(-11f, -3f, 0f),
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(7f, -6f, 0f),
                    new Vector3(16f, 5f, 0f)
                }
            };
        }

        private static MazeLayout CreateRiftMazeLayout()
        {
            return new MazeLayout
            {
                ArenaSize = new Vector2(48f, 32f),
                PlayerSpawn = new Vector3(-20f, -10f, 0f),
                ExitPosition = new Vector3(20f, 10f, 0f),
                ChestPosition = new Vector3(17f, 10f, 0f),
                ItemPosition = new Vector3(2f, -7f, 0f),
                LandmarkPosition = new Vector3(0f, 9f, 0f),
                LandmarkColliderSize = new Vector2(2.8f, 1.5f),
                Lobes = new[]
                {
                    new MazeLobe(-20f, -10f, 3f, 3f),
                    new MazeLobe(-12f, -8f, 4f, 3.5f),
                    new MazeLobe(-12f, 1f, 4f, 3.5f),
                    new MazeLobe(-3f, 1f, 4.5f, 4f),
                    new MazeLobe(0f, 9f, 4f, 3.5f),
                    new MazeLobe(6f, -7f, 4f, 3.5f),
                    new MazeLobe(10f, 2f, 4.5f, 4f),
                    new MazeLobe(20f, 10f, 3.5f, 3f),
                    new MazeLobe(18f, -7f, 3.5f, 3f)
                },
                Corridors = new[]
                {
                    new MazeCorridor(3.6f, new Vector2(-20f, -10f),
                        new Vector2(-16f, -10f), new Vector2(-16f, -8f),
                        new Vector2(-12f, -8f)),
                    new MazeCorridor(3.4f, new Vector2(-12f, -8f),
                        new Vector2(-12f, 1f)),
                    new MazeCorridor(3.6f, new Vector2(-12f, 1f),
                        new Vector2(-3f, 1f)),
                    new MazeCorridor(3.3f, new Vector2(-3f, 1f),
                        new Vector2(-3f, 9f), new Vector2(0f, 9f)),
                    new MazeCorridor(3.6f, new Vector2(-3f, 1f),
                        new Vector2(2f, 1f), new Vector2(2f, -7f),
                        new Vector2(6f, -7f)),
                    new MazeCorridor(3.5f, new Vector2(6f, -7f),
                        new Vector2(10f, -7f), new Vector2(10f, 2f)),
                    new MazeCorridor(3.6f, new Vector2(0f, 9f),
                        new Vector2(10f, 9f), new Vector2(10f, 2f)),
                    new MazeCorridor(3.5f, new Vector2(10f, 2f),
                        new Vector2(16f, 2f), new Vector2(16f, 10f),
                        new Vector2(20f, 10f)),
                    new MazeCorridor(3.2f, new Vector2(10f, 2f),
                        new Vector2(18f, 2f), new Vector2(18f, -7f)),
                    new MazeCorridor(2.9f, new Vector2(6f, -7f),
                        new Vector2(18f, -7f))
                },
                Clutter = new[]
                {
                    new MazeClutter(0, MazeClutterKind.Crystal, -21f, -8.5f, 1.5f),
                    new MazeClutter(1, MazeClutterKind.Rock, -13f, -6.5f, 1f),
                    new MazeClutter(2, MazeClutterKind.Supplies, -10f, 2.5f, 1.2f),
                    new MazeClutter(3, MazeClutterKind.Shrine, -2f, 2.5f, 2.2f),
                    new MazeClutter(4, MazeClutterKind.Landmark, 0f, 9f, 3.8f),
                    new MazeClutter(5, MazeClutterKind.Crystal, 5f, -5.5f, 1.45f, true),
                    new MazeClutter(6, MazeClutterKind.Rock, 11f, 3.5f, 1f),
                    new MazeClutter(7, MazeClutterKind.Crystal, 19f, 8.5f, 1.55f),
                    new MazeClutter(8, MazeClutterKind.Rock, 19f, -6f, 1.05f, true)
                },
                PrimaryEnemyPositions = new[]
                {
                    new Vector3(-12f, -8f, 0f),
                    new Vector3(-12f, 1f, 0f),
                    new Vector3(-3f, 1f, 0f),
                    new Vector3(3f, 9f, 0f),
                    new Vector3(6f, -7f, 0f),
                    new Vector3(10f, 2f, 0f),
                    new Vector3(18f, -7f, 0f),
                    new Vector3(17f, 10f, 0f)
                },
                SecondaryEnemyPositions = new[]
                {
                    new Vector3(-10f, -7f, 0f),
                    new Vector3(-4f, 2f, 0f),
                    new Vector3(7f, -6f, 0f),
                    new Vector3(11f, 1f, 0f),
                    new Vector3(18f, 9f, 0f)
                }
            };
        }

        private static void CreateCombatScene(string id, MapData map, GameObject primary,
            int primaryCount, GameObject secondary, SkillData[] skills, ItemData item,
            string nextScene, GameObject tertiary = null, int tertiaryCount = 0)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            map = ReloadMapData(id, map);
            item = ReloadItemData(item);
            MazeLayout layout = MazeLayoutFor(id) ?? throw new InvalidOperationException(
                "No maze layout is configured for scene '" + id + "'.");
            CreateEnvironment(id);
            CreateCombatDecor(id);
            GameObject player = PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/Player/Player.prefab")) as GameObject;
            player.transform.position = layout.PlayerSpawn;
            CreateSpawnPoint(layout.PlayerSpawn);
            CreateCamera(player.transform, id);
            CreateLight();

            GameObject mapObject = new GameObject("MapManager");
            MapManager manager = mapObject.AddComponent<MapManager>();
            EnemySpawnManager spawnManager = mapObject.AddComponent<EnemySpawnManager>();
            Set(spawnManager, "reservePoolSize", 8);
            // Narrow forest and temple lanes use authored anchors; radial expansion
            // can place reserve enemies beyond a thin maze boundary.
            if (id == "Map02" || id == "Map03")
                Set(spawnManager, "expandSpawnAnchors", false);
            if (id == "Map03")
                SetVector2Array(spawnManager, "additionalSpawnPoints", new[]
                {
                    new Vector2(-14f, -3f), new Vector2(-7f, -3f),
                    new Vector2(-5f, 0f), new Vector2(0f, 5f),
                    new Vector2(5f, -2f), new Vector2(10f, -8f),
                    new Vector2(11f, 8f), new Vector2(14f, 3f),
                    new Vector2(-15f, 1f)
                });
            mapObject.AddComponent<HuntingZoneScenery>();
            if (id == "Map01")
            {
                SlimeMarshController marsh =
                    mapObject.AddComponent<SlimeMarshController>();
                SetVector2Array(marsh, "nestPositions", SlimeNestPositions());
                Set(marsh, "bubblesPerNest", 4);
            }
            if (id.StartsWith("Rift", StringComparison.OrdinalIgnoreCase))
            {
                mapObject.AddComponent<RiftStormController>();
            }
            Set(manager, "mapData", map);
            Canvas canvas = CreateGameplayCanvas(skills);
            Set(manager, "completionMessage", FindText(canvas, "CompletionMessage"));

            if (primary != null)
            {
                for (int i = 0; i < primaryCount; i++)
                {
                    Vector3 position = EnemySpawnPosition(layout, i, false);
                    GameObject instance = PrefabUtility.InstantiatePrefab(primary, scene) as GameObject;
                    instance.transform.position = position;
                }
            }

            if (secondary != null)
            {
                for (int i = 0; i < layout.SecondaryEnemyPositions.Length; i++)
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(secondary, scene) as GameObject;
                    instance.transform.position = EnemySpawnPosition(layout, i, true);
                }
            }

            if (tertiary != null)
            {
                for (int i = 0; i < tertiaryCount; i++)
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(
                        tertiary, scene) as GameObject;
                    instance.transform.position = EnemySpawnPosition(layout,
                        primaryCount + i, false);
                }
            }

            MapDoor door = CreateDoor(layout.ExitPosition, nextScene);
            RewardChest chest = CreateChest(layout.ChestPosition,
                map.OpensSkillSelection);
            SetArray(manager, "doors", new[] { door });
            Set(manager, "rewardChest", chest);
            if (item != null)
            {
                CreateItemPickup(item, layout.ItemPosition);
            }

            CreateMapGuidance(id, map.DisplayName + " • " +
                map.RecommendedLevelLabel, layout.PlayerSpawn,
                layout.ExitPosition, layout.ChestPosition, nextScene,
                map.OpensSkillSelection ? "RƯƠNG KỸ NĂNG" :
                    "RƯƠNG PHẦN THƯỞNG");

            EditorSceneManager.SaveScene(scene, SceneRoot + "/" + id + ".unity");
        }

        private static Vector3 EnemySpawnPosition(MazeLayout layout, int index,
            bool secondary)
        {
            Vector3[] positions = secondary
                ? layout.SecondaryEnemyPositions
                : layout.PrimaryEnemyPositions;
            if (positions == null || positions.Length == 0)
            {
                throw new InvalidOperationException("Maze enemy spawn list is empty.");
            }

            return positions[index % positions.Length];
        }

        private static void CreateBossScene(string id, MapData map, GameObject boss,
            GameObject minion, SkillData[] skills, ItemData item)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            map = ReloadMapData(id, map);
            item = ReloadItemData(item);
            MazeLayout layout = MazeLayoutFor(id) ?? throw new InvalidOperationException(
                "No maze layout is configured for scene '" + id + "'.");
            CreateEnvironment(id);
            CreateCombatDecor(id);
            GameObject player = PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/Player/Player.prefab")) as GameObject;
            player.transform.position = layout.PlayerSpawn;
            CreateSpawnPoint(player.transform.position);
            CreateCamera(player.transform, id);
            CreateLight();

            GameObject bossObject = PrefabUtility.InstantiatePrefab(boss, scene) as GameObject;
            bossObject.transform.position = layout.BossPosition;
            ForestGuardianBoss bossController = bossObject.GetComponent<ForestGuardianBoss>();
            Set(bossController, "minionPrefab", minion);
            Transform[] summonPoints = new Transform[5];
            for (int i = 0; i < summonPoints.Length; i++)
            {
                GameObject point = new GameObject("SummonPoint" + (i + 1));
                point.transform.SetParent(bossObject.transform);
                float angle = i * Mathf.PI * 2f / summonPoints.Length;
                point.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 2f;
                summonPoints[i] = point.transform;
            }
            SetArray(bossController, "summonPoints", summonPoints);

            GameObject mapObject = new GameObject("MapManager");
            MapManager manager = mapObject.AddComponent<MapManager>();
            EnemySpawnManager spawnManager = mapObject.AddComponent<EnemySpawnManager>();
            Set(spawnManager, "reservePoolSize", 0);
            mapObject.AddComponent<HuntingZoneScenery>();
            Set(manager, "mapData", map);
            Canvas canvas = CreateGameplayCanvas(skills);
            Set(manager, "completionMessage", FindText(canvas, "CompletionMessage"));
            RewardChest chest = CreateChest(layout.ChestPosition, true);
            MapDoor door = CreateDoor(layout.ExitPosition, map.NextScene);
            SetArray(manager, "doors", new[] { door });
            Set(manager, "rewardChest", chest);
            if (item != null)
            {
                CreateItemPickup(item, layout.ItemPosition);
            }
            CreateMapGuidance(id, map.DisplayName + " • " +
                map.RecommendedLevelLabel, layout.PlayerSpawn,
                layout.ExitPosition, layout.ChestPosition, map.NextScene,
                "RƯƠNG PHẦN THƯỞNG BOSS");
            EditorSceneManager.SaveScene(scene, SceneRoot + "/" + id + ".unity");
        }

        private static MapData ReloadMapData(string id, MapData fallback)
        {
            MapData persisted = AssetDatabase.LoadAssetAtPath<MapData>(
                DataRoot + "/Maps/" + id + ".asset");
            if (persisted != null)
            {
                return persisted;
            }

            if (fallback != null && EditorUtility.IsPersistent(fallback))
            {
                return fallback;
            }

            throw new InvalidOperationException(
                "Could not load persistent MapData for scene '" + id + "'.");
        }

        private static ItemData ReloadItemData(ItemData fallback)
        {
            ItemData persisted = AssetDatabase.LoadAssetAtPath<ItemData>(
                DataRoot + "/Items/forest_relic.asset");
            return persisted != null ? persisted : fallback;
        }

        private static void CreateEnvironment(string mapId)
        {
            if (mapId == "Map02")
            {
                CreatePaintedForestEnvironment();
                return;
            }
            MazeLayout layout = MazeLayoutFor(mapId);
            Vector2 floorSize = layout != null
                ? layout.ArenaSize + new Vector2(24f, 16f)
                : new Vector2(40f, 24f);
            GameObject environment = new GameObject("Environment");
            GameObject floor = new GameObject("Floor");
            floor.transform.SetParent(environment.transform);
            SpriteRenderer floorRenderer = floor.AddComponent<SpriteRenderer>();
            bool isCave = mapId.StartsWith("Cave", StringComparison.OrdinalIgnoreCase) ||
                mapId.StartsWith("Rift", StringComparison.OrdinalIgnoreCase);
            floorRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnvironmentArtRoot +
                (mapId == "Map02" ? "/Map02_LeafSoilV3.png" : mapId=="Map01" ? "/Map01_MossSoilV8.png" : isCave ? "/CaveFloor.png" : "/ForestFloor.png"));
            if (floorRenderer.sprite != null)
            {
                floorRenderer.drawMode = SpriteDrawMode.Tiled;
                floorRenderer.tileMode = SpriteTileMode.Continuous;
                // Extend beyond the arena so a following camera never exposes its clear color.
                floorRenderer.size = floorSize;
                floorRenderer.color = layout != null
                    ? MazeBackgroundTint(mapId, isCave)
                    : EnvironmentTint(mapId, isCave);
            }
            else
            {
                floorRenderer.sprite = BuiltinSprite();
                floorRenderer.color = new Color(0.16f, 0.28f, 0.22f);
                floor.transform.localScale = new Vector3(floorSize.x,
                    floorSize.y, 1f);
            }
            floorRenderer.sortingOrder = -10;
            if (mapId == "Map02") floorRenderer.sharedMaterial = Map02GroundMaterial();
            if(mapId=="Map01")
            {
                floorRenderer.sharedMaterial=Map01GroundMaterial();
                floorRenderer.color=new Color(0.46f,0.61f,0.58f);
            }
            floor.transform.position = new Vector3(0f, 0f, 1f);
            GameObject walls = new GameObject("Walls");
            walls.transform.SetParent(environment.transform);
            if (layout == null)
            {
                CreateSizedBlock("Wall_Top", walls.transform,
                    new Vector3(0f, 5.25f, 0f), new Vector2(19f, 0.5f),
                    Color.clear, true, 0);
                CreateSizedBlock("Wall_Bottom", walls.transform,
                    new Vector3(0f, -5.25f, 0f), new Vector2(19f, 0.5f),
                    Color.clear, true, 0);
                CreateSizedBlock("Wall_Left", walls.transform,
                    new Vector3(-9.25f, 0f, 0f), new Vector2(0.5f, 10f),
                    Color.clear, true, 0);
                CreateSizedBlock("Wall_Right", walls.transform,
                    new Vector3(9.25f, 0f, 0f), new Vector2(0.5f, 10f),
                    Color.clear, true, 0);
                return;
            }

            float halfWidth = layout.ArenaSize.x * 0.5f;
            float halfHeight = layout.ArenaSize.y * 0.5f;
            CreateSizedBlock("Wall_Top", walls.transform,
                new Vector3(0f, halfHeight, 0f),
                new Vector2(layout.ArenaSize.x, 0.75f),
                Color.clear, true, 0);
            CreateSizedBlock("Wall_Bottom", walls.transform,
                new Vector3(0f, -halfHeight, 0f),
                new Vector2(layout.ArenaSize.x, 0.75f),
                Color.clear, true, 0);
            CreateSizedBlock("Wall_Left", walls.transform,
                new Vector3(-halfWidth, 0f, 0f),
                new Vector2(0.75f, layout.ArenaSize.y),
                Color.clear, true, 0);
            CreateSizedBlock("Wall_Right", walls.transform,
                new Vector3(halfWidth, 0f, 0f),
                new Vector2(0.75f, layout.ArenaSize.y),
                Color.clear, true, 0);
        }

        private static Color MazeBackgroundTint(string mapId, bool isCave)
        {
            if (mapId.StartsWith("Rift", StringComparison.OrdinalIgnoreCase))
            {
                return new Color(0.34f, 0.22f, 0.52f, 1f);
            }
            Color baseTint = EnvironmentTint(mapId, isCave);
            Color shadow = isCave
                ? new Color(0.46f, 0.58f, 0.78f, 1f)
                : new Color(0.48f, 0.62f, 0.46f, 1f);
            return Color.Lerp(baseTint, shadow, isCave ? 0.16f : 0.12f);
        }

        private static Color EnvironmentTint(string mapId, bool isCave)
        {
            if (mapId.StartsWith("Rift", StringComparison.OrdinalIgnoreCase))
            {
                return new Color(0.42f, 0.26f, 0.62f, 1f);
            }
            if (isCave)
            {
                return new Color(0.78f, 0.88f, 1f);
            }

            switch (mapId)
            {
                case "Map01":
                    return new Color(0.9f, 0.98f, 0.88f);
                case "Map02":
                    return new Color(0.8f, 0.9f, 0.78f);
                case "Map03":
                    return new Color(0.82f, 0.86f, 0.96f);
                case "BossMap01":
                    return new Color(0.9f, 0.78f, 0.68f);
                default:
                    return new Color(0.86f, 0.94f, 0.84f);
            }
        }

        private static void PrepareGeneratedArt()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureGeneratedSprites(CharacterArtRoot, 768f, TextureWrapMode.Clamp,
                pixelsPerUnitResolver: ResolveCharacterPixelsPerUnit);
            ConfigureGeneratedSprites(PropsArtRoot, 768f, TextureWrapMode.Clamp);
            ConfigureGeneratedSprites(SkillIconArtRoot, 256f, TextureWrapMode.Clamp,
                256, TextureImporterCompression.Compressed);
            ConfigureGeneratedSprites(EquipmentIconArtRoot, 256f, TextureWrapMode.Clamp,
                1024, TextureImporterCompression.Uncompressed);
            ConfigureGeneratedSprites(EquipmentUIArtRoot, 256f, TextureWrapMode.Clamp,
                2048, TextureImporterCompression.Uncompressed);
            ConfigureGeneratedSprites(LobbyArtRoot, 256f, TextureWrapMode.Clamp,
                4096, TextureImporterCompression.Uncompressed);
            ConfigureGeneratedSprites(EnvironmentArtRoot, 128f, TextureWrapMode.Repeat);
        }

        private static void PrepareForestAnimationSheet(string assetName)
        {
            string path = CharacterArtRoot + "/" + assetName + ".png";
            AssetDatabase.ImportAsset(path,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as
                TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Missing Deep Forest animation sheet: " + path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 320f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void ConfigureGeneratedSprites(string folder, float pixelsPerUnit,
            TextureWrapMode wrapMode, int maxTextureSize = 2048,
            TextureImporterCompression compression = TextureImporterCompression.Uncompressed,
            Func<string, Sprite, float> pixelsPerUnitResolver = null)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                Sprite currentSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                float resolvedPixelsPerUnit = pixelsPerUnitResolver != null
                    ? Mathf.Max(1f, pixelsPerUnitResolver(path, currentSprite))
                    : pixelsPerUnit;
                importer.spritePixelsPerUnit = resolvedPixelsPerUnit;
                bool useGroundedPivot = TryResolveGroundedPlayerPivot(path,
                    resolvedPixelsPerUnit, out Vector2 groundedPivot);
                importer.alphaIsTransparency = folder == CharacterArtRoot ||
                    folder == PropsArtRoot || folder == SkillIconArtRoot ||
                    folder == EquipmentIconArtRoot;
                importer.mipmapEnabled = false;
                // Painted scenery is not pixel art. Point sampling magnifies
                // texels and shimmers under camera motion; preserve smooth edges.
                importer.filterMode = folder == EquipmentIconArtRoot ||
                    folder == CharacterArtRoot || folder == PropsArtRoot ||
                    folder == LobbyArtRoot || folder == EnvironmentArtRoot
                    ? FilterMode.Bilinear
                    : FilterMode.Point;
                importer.textureCompression = compression;
                importer.wrapMode = wrapMode;
                importer.maxTextureSize = maxTextureSize;
                // Full-map paintings need room for future higher-resolution
                // sources. This cap does not upscale today's smaller originals.
                if (path == EnvironmentArtRoot + "/Map01_EmeraldSanctuaryV7.png" ||
                    path == EnvironmentArtRoot + "/Map02_AncientWoodlandV4.png")
                {
                    importer.maxTextureSize = 4096;
                    importer.wrapMode = TextureWrapMode.Clamp;
                }
                TextureImporterSettings settings = new TextureImporterSettings();
                // Retain the approved 4x restoration, including the texture sampled
                // by LobbyWaterFlow. Geometry remains authored in original pixels.
                if (path == RestoredLobbyArt || path == RestoredMap02Art)
                {
                    importer.maxTextureSize = 8192;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.isReadable = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.ClearPlatformTextureSettings("Standalone");
                }
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = useGroundedPivot
                    ? (int)SpriteAlignment.Custom
                    : (int)SpriteAlignment.Center;
                settings.spritePivot = useGroundedPivot
                    ? groundedPivot
                    : new Vector2(0.5f, 0.5f);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static float ResolveCharacterPixelsPerUnit(string path, Sprite sprite)
        {
            bool isPlayerAnimationFrame = path.StartsWith(SmoothPlayerRunArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(DirectionalPlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(DiagonalPlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(LegacyEightFramePlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(LegacySeamlessPlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(ExpressiveHorizontalPlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(EightFramePlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(StableHorizontalPlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(CharacterArtRoot + "/PlayerV2/",
                    StringComparison.OrdinalIgnoreCase);
            if (!isPlayerAnimationFrame || sprite == null)
            {
                return 768f;
            }

            Sprite idle = GeneratedCharacterSprite("Player");
            if (idle == null || idle.rect.height <= 0f || idle.pixelsPerUnit <= 0f)
            {
                return 768f;
            }

            // AI-generated walk frames are tightly cropped while the idle artwork
            // uses a much larger canvas. Matching world-space height prevents the
            // character from shrinking when the renderer swaps sprites.
            float idleWorldHeight = idle.rect.height / idle.pixelsPerUnit;
            float sourceHeight = sprite.rect.height;
            if (TryFindOpaqueBottomRow(path, out _, out int textureHeight))
            {
                // A newly imported transparent PNG can initially expose a tight
                // sprite rect. Use the actual canvas height so first-time builds
                // do not make new animation sets larger than established ones.
                sourceHeight = textureHeight;
            }

            return sourceHeight / Mathf.Max(0.01f, idleWorldHeight);
        }

        private static bool TryResolveGroundedPlayerPivot(string path,
            float pixelsPerUnit, out Vector2 pivot)
        {
            pivot = new Vector2(0.5f, 0.5f);
            bool isGroundedWalkFrame =
                path.StartsWith(DirectionalPlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(DiagonalPlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(LegacyEightFramePlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(LegacySeamlessPlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(ExpressiveHorizontalPlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(EightFramePlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(StableHorizontalPlayerWalkArtRoot + "/",
                    StringComparison.OrdinalIgnoreCase);
            if (!isGroundedWalkFrame)
            {
                return false;
            }

            Sprite idle = PlayerIdleSprite();
            string idlePath = CharacterArtRoot + "/PlayerV2/Idle_00.png";
            if (idle == null || idle.pixelsPerUnit <= 0f ||
                !TryFindOpaqueBottomRow(idlePath, out int idleBottom, out _) ||
                !TryFindOpaqueBottomRow(path, out int frameBottom, out int frameHeight))
            {
                return false;
            }

            // Preserve the idle pose's ground line while keeping its centered
            // pivot and collider layout. The generated walk canvases leave more
            // transparent pixels below the boots, which otherwise makes the
            // character visibly rise when movement starts.
            float idleGroundOffset = (idleBottom - idle.pivot.y) /
                idle.pixelsPerUnit;
            float framePivotY = frameBottom - idleGroundOffset * pixelsPerUnit;
            pivot = new Vector2(0.5f,
                Mathf.Clamp01(framePivotY / Mathf.Max(1f, frameHeight)));
            return true;
        }

        private static bool TryFindOpaqueBottomRow(string path, out int bottomRow,
            out int textureHeight)
        {
            bottomRow = 0;
            textureHeight = 0;
            if (!File.Exists(path))
            {
                return false;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path), false))
                {
                    return false;
                }

                textureHeight = texture.height;
                Color32[] pixels = texture.GetPixels32();
                for (int y = 0; y < texture.height; y++)
                {
                    int rowStart = y * texture.width;
                    for (int x = 0; x < texture.width; x++)
                    {
                        if (pixels[rowStart + x].a > 10)
                        {
                            bottomRow = y;
                            return true;
                        }
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            return false;
        }

        private static Sprite GeneratedCharacterSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(CharacterArtRoot + "/" + name + ".png");
        }

        private static Sprite PlayerIdleSprite()
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(
                CharacterArtRoot + "/PlayerV2/Idle_00.png");
        }

        private static Sprite SmoothPlayerRunSprite(int frame)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(SmoothPlayerRunArtRoot +
                "/RunSmooth_" + frame.ToString("00") + ".png");
        }

        private static Sprite[] CardinalPlayerWalkSprites(string direction)
        {
            // V10 is normalized around a fixed torso centre and ground line.
            // This removes the side-to-side body jitter left in V9 while
            // preserving a full contact/down/passing/up horizontal cycle.
            // Keep the steadier V6 art active for up/down movement.
            string artRoot = string.Equals(direction, "Right",
                StringComparison.OrdinalIgnoreCase)
                ? StableHorizontalPlayerWalkArtRoot
                : LegacyEightFramePlayerWalkArtRoot;
            Sprite[] frames = new Sprite[8];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                    artRoot + "/" + direction + "/Walk" +
                    direction + "_" + i.ToString("00") + ".png");
            }

            return frames;
        }

        private static Sprite GeneratedPropSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(PropsArtRoot + "/" + name + ".png");
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 position,
            Vector3 scale, Color color, bool collider)
        {
            GameObject block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = BuiltinSprite();
            renderer.color = color;
            renderer.sortingOrder = collider ? 0 : -10;
            if (collider)
            {
                block.AddComponent<BoxCollider2D>();
            }

            return block;
        }

        private static GameObject CreateSizedBlock(string name, Transform parent,
            Vector3 position, Vector2 size, Color color, bool collider,
            int sortingOrder)
        {
            GameObject block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.position = position;

            SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = BuiltinSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (color.a <= 0f)
            {
                renderer.enabled = false;
            }
            else
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = size;
            }

            if (collider)
            {
                BoxCollider2D box = block.AddComponent<BoxCollider2D>();
                box.size = size;
            }

            return block;
        }

        private static void CreateSpawnPoint(Vector3 position)
        {
            GameObject spawn = new GameObject("PlayerSpawnPoint");
            spawn.transform.position = position;
            spawn.AddComponent<PlayerSpawnPoint>();
        }

        private static void CreateCamera(Transform target, string mapId)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = target.position + new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            bool isCave = mapId.StartsWith("Cave",
                StringComparison.OrdinalIgnoreCase);
            Color biome = EnvironmentTint(mapId, isCave);
            camera.backgroundColor = Color.Lerp(biome,
                isCave ? new Color(0.22f, 0.3f, 0.48f) :
                    new Color(0.24f, 0.4f, 0.25f), 0.42f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
            Set(follow, "target", target);
            Set(follow, "targetTag", "Player");
            Set(follow, "offset", new Vector3(0f, 0f, -10f));
            Set(follow, "smoothTime", 0f);
            Set(follow, "pixelSnap", false);
            if (mapId == "Map02")
            {
                Set(follow, "constrainToBounds", true);
                Set(follow, "worldBounds", new Rect(-18f, -12f, 36f, 24f));
                camera.backgroundColor = new Color(0.035f, 0.09f, 0.08f);
            }
            if (mapId == "Lobby")
            {
                Set(follow, "constrainToBounds", true);
                Set(follow, "worldBounds", new Rect(-13f, -7.8f, 26f, 15.6f));
                Set(follow, "smoothTime", 0.18f);
                camera.orthographicSize = 5.6f;
                camera.backgroundColor = new Color(0.035f, 0.09f, 0.08f);
            }
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
        }

        private static MapDoor CreateDoor(Vector3 position, string nextScene)
        {
            GameObject doorObject = CreateBlock("MapDoor", null, position,
                new Vector3(0.7f, 2f, 1f), new Color(0.45f, 0.12f, 0.12f), true);
            SpriteRenderer visual = doorObject.GetComponent<SpriteRenderer>();
            Sprite portalSprite = GeneratedPropSprite("ForestPortal");
            if (portalSprite != null)
            {
                visual.sprite = portalSprite;
                visual.color = Color.white;
            }
            MapDoor door = doorObject.AddComponent<MapDoor>();
            Set(door, "nextScene", nextScene);
            if (nextScene != "Lobby")
                Set(door, "destinationMap", AssetDatabase.LoadAssetAtPath<MapData>(
                    DataRoot + "/Maps/" + nextScene + ".asset"));
            Set(door, "visual", visual);
            return door;
        }

        private static RewardChest CreateChest(Vector3 position, bool skillChoice)
        {
            GameObject chestObject = CreateBlock("RewardChest", null, position,
                new Vector3(1.2f, 0.9f, 1f), new Color(0.35f, 0.18f, 0.05f), true);
            SpriteRenderer visual = chestObject.GetComponent<SpriteRenderer>();
            Sprite chestSprite = GeneratedPropSprite("TreasureChest");
            if (chestSprite != null)
            {
                visual.sprite = chestSprite;
                visual.color = Color.white;
            }
            RewardChest chest = chestObject.AddComponent<RewardChest>();
            Set(chest, "offersSkillChoice", skillChoice);
            Set(chest, "visual", visual);
            return chest;
        }

        private static void CreateItemPickup(ItemData item, Vector3 position)
        {
            GameObject pickup = CreateBlock("ItemPickup", null, position,
                Vector3.one, new Color(1f, 0.82f, 0.12f), false);
            SpriteRenderer renderer = pickup.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 8;
            Sprite relicSprite = GeneratedPropSprite("ForestRelicPickupV1");
            float visualScale;
            if (relicSprite != null)
            {
                renderer.sprite = relicSprite;
                renderer.color = Color.white;
                visualScale = 0.72f / Mathf.Max(0.01f,
                    relicSprite.bounds.size.y);
            }
            else
            {
                visualScale = 3.4f;
            }
            pickup.transform.localScale = Vector3.one * visualScale;
            CircleCollider2D collider = pickup.AddComponent<CircleCollider2D>();
            collider.radius = 0.38f / visualScale;
            collider.isTrigger = true;
            ItemPickup itemPickup = pickup.AddComponent<ItemPickup>();
            Set(itemPickup, "item", item);
        }

        private static Canvas CreateGameplayCanvas(SkillData[] skills, bool lobby = false)
        {
            // NewScene can unload native assets held only by the previous
            // builder stack. Reacquire persistent skills before wiring UI.
            Array skillIds = Enum.GetValues(typeof(SkillId));
            skills = new SkillData[skillIds.Length];
            for (int i = 0; i < skillIds.Length; i++)
            {
                string path = DataRoot + "/Skills/" + skillIds.GetValue(i) + ".asset";
                skills[i] = AssetDatabase.LoadAssetAtPath<SkillData>(path);
                if (skills[i] == null)
                    throw new InvalidOperationException("Cannot build reward UI: missing SkillData at " + path);
            }
            GameObject canvasObject = new GameObject("GameplayCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ConfigureCanvasScaler(canvasObject.AddComponent<CanvasScaler>());
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<MiniMapController>();
            GameObject hudObject = new GameObject("HUD");
            hudObject.transform.SetParent(canvasObject.transform, false);
            if (lobby) hudObject.AddComponent<LobbyHUD>();
            else hudObject.AddComponent<GameplayHUD>();
            PlayerDodge dodge = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerDodge>();
            DodgeHUD dodgeHud = hudObject.AddComponent<DodgeHUD>();
            Transform dodgeCard = FantasyEquipmentUI.Frame(canvas.transform, "DodgeCard", lobby ? 22 : 15,
                lobby ? 140 : 310, 260, 66, FantasyEquipmentUI.Ink);
            FantasyEquipmentUI.Text(dodgeCard, "DodgeKey", "NÉ ĐÒN [SPACE]", 12, 6, 236, 22, 18, FantasyEquipmentUI.TextColor);
            TMP_Text dodgeStatus = FantasyEquipmentUI.Text(dodgeCard, "DodgeStatus", "SẴN SÀNG", 12, 29, 236, 20, 16, FantasyEquipmentUI.Green);
            FantasyEquipmentUI.Box(dodgeCard, "Track", 12, 55, 236, 4, FantasyEquipmentUI.Panel);
            Image dodgeFill = FantasyEquipmentUI.Box(dodgeCard, "Cooldown", 12, 55, 236, 4, FantasyEquipmentUI.Green);
            Set(dodgeHud, "dodge", dodge);
            Set(dodgeHud, "status", dodgeStatus);
            Set(dodgeHud, "fill", dodgeFill.rectTransform);
            SkillSelectionUI selection = hudObject.AddComponent<SkillSelectionUI>();
            SetArray(selection, "skillDatabase", skills);
            hudObject.AddComponent<InventoryUI>();
            hudObject.AddComponent<SkillEquipmentUI>();
            hudObject.AddComponent<PotentialUpgradeUI>();
            hudObject.AddComponent<EquipmentEnhancementUI>();
            hudObject.AddComponent<HeroSelectionUI>();
            GameObject pauseObject = new GameObject("PauseMenu");
            pauseObject.transform.SetParent(canvasObject.transform, false);
            PauseMenu pauseMenu = pauseObject.AddComponent<PauseMenu>();
            GameObject pausePanel = CreateOverlayPanel(canvasObject.transform, "PausePanel", "Paused");
            CreateButton(pausePanel.transform, "ResumeButton", "Resume", new Vector2(0f, 10f));
            CreateButton(pausePanel.transform, "MenuButton", "Main Menu", new Vector2(0f, -70f));
            Set(pauseMenu, "panel", pausePanel);
            GameObject gameOverObject = new GameObject("GameOverUI");
            gameOverObject.transform.SetParent(canvasObject.transform, false);
            GameOverUI gameOver = gameOverObject.AddComponent<GameOverUI>();
            GameObject gameOverPanel = CreateOverlayPanel(canvasObject.transform, "GameOverPanel", "Game Over");
            CreateButton(gameOverPanel.transform, "RestartButton", "Restart", new Vector2(0f, 10f));
            CreateButton(gameOverPanel.transform, "GameOverMenuButton", "Main Menu", new Vector2(0f, -70f));
            Set(gameOver, "panel", gameOverPanel);
            Set(gameOver, "message", FindTextIn(gameOverPanel.transform, "PanelTitle"));
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            GameObject messageObject = new GameObject("CompletionMessage", typeof(RectTransform));
            messageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform messageRect = messageObject.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 1f);
            messageRect.anchorMax = new Vector2(0.5f, 1f);
            messageRect.anchoredPosition = new Vector2(0f, -120f);
            messageRect.sizeDelta = new Vector2(800f, 60f);
            TMP_Text message = messageObject.AddComponent<TextMeshProUGUI>();
            message.alignment = TextAlignmentOptions.Center;
            message.fontSize = 24f;
            message.color = new Color(1f, 0.85f, 0.25f);
            message.gameObject.SetActive(false);

            GameObject fadeObject = new GameObject("FadeOverlay", typeof(RectTransform),
                typeof(Image), typeof(CanvasGroup), typeof(FadeTransition));
            fadeObject.transform.SetParent(canvasObject.transform, false);
            RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            fadeObject.GetComponent<Image>().color = Color.black;
            fadeObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
            return canvas;
        }

        private static GameObject CreateOverlayPanel(Transform parent, string name, string title)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(440f, 330f);
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 0.95f);
            CreateText(panel.transform, "PanelTitle", title, 32f, new Vector2(0f, 105f));
            return panel;
        }

        private static TMP_Text FindTextIn(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static TMP_Text FindText(Canvas canvas, string objectName)
        {
            Transform child = canvas.transform.Find(objectName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static void CreateMainMenu()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canvasObject = new GameObject("MainMenuCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ConfigureCanvasScaler(canvasObject.AddComponent<CanvasScaler>());
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<MainMenuController>();

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                EnvironmentArtRoot + "/ForestFloor.png");
            backgroundImage.color = new Color(0.2f, 0.32f, 0.25f, 1f);

            GameObject panel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620f, 690f);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.04f, 0.93f);

            CreateText(panel.transform, "Title", "TOP-DOWN ADVENTURE", 54f, new Vector2(0f, 250f));
            TMP_Text subtitle = CreateText(panel.transform, "Subtitle",
                "FOREST GUARDIAN", 24f, new Vector2(0f, 195f));
            subtitle.color = new Color(0.55f, 0.95f, 0.68f);
            CreateButton(panel.transform, "ContinueButton", "Continue", new Vector2(0f, 95f))
                .GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 65f);
            CreateButton(panel.transform, "StartButton", "New Game", new Vector2(0f, 5f))
                .GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 65f);
            CreateButton(panel.transform, "QuitButton", "Quit", new Vector2(0f, -85f))
                .GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 65f);
            TMP_Text controls = CreateText(panel.transform, "Controls",
                "WASD / Chuột trái: Di chuyển    •    Chuột phải / J: Đánh\nQ • E • R • F: Kỹ năng    •    E: Tương tác    •    Esc: Tạm dừng",
                19f, new Vector2(0f, -215f));
            controls.color = new Color(0.78f, 0.86f, 0.8f);
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            CreateLight();
            EditorSceneManager.SaveScene(scene, SceneRoot + "/MainMenu.unity");
        }

        private static void ConfigureCanvasScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text,
            float size, Vector2 position)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 100f);
            rect.anchoredPosition = position;
            TMP_Text label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string text,
            Vector2 position)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform),
                typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(280f, 65f);
            rect.anchoredPosition = position;
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.22f, 0.3f);
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            return buttonObject.GetComponent<Button>();
        }

        private static Sprite BuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static void Set(UnityEngine.Object target, string propertyName, object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning("Property not found: " + propertyName + " on " + target.name);
                return;
            }

            if (value is UnityEngine.Object objectValue) property.objectReferenceValue = objectValue;
            else if (value is string stringValue) property.stringValue = stringValue;
            else if (value is float floatValue) property.floatValue = floatValue;
            else if (value is int intValue) property.intValue = intValue;
            else if (value is bool boolValue) property.boolValue = boolValue;
            else if (value is Color colorValue) property.colorValue = colorValue;
            else if (value is Vector2 vector2Value) property.vector2Value = vector2Value;
            else if (value is Vector3 vectorValue) property.vector3Value = vectorValue;
            else if (value is Rect rectValue) property.rectValue = rectValue;
            else if (value is SkillId skillId) property.enumValueIndex = (int)skillId;
            else if (value is EnemyArchetype archetype) property.enumValueIndex = (int)archetype;
            else if (value is WeaponType weaponType) property.enumValueIndex = (int)weaponType;
            else if (value is EquipmentSlot equipmentSlot) property.enumValueIndex = (int)equipmentSlot;
            else throw new InvalidOperationException("Unsupported serialized value: " + value.GetType());

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray<T>(UnityEngine.Object target, string propertyName, T[] values)
            where T : UnityEngine.Object
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(propertyName);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector2Array(UnityEngine.Object target,
            string propertyName, Vector2[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(propertyName);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).vector2Value = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
