using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopDownGame.Enemies;
using TopDownGame.Enemies.Spawning;
using TopDownGame.Equipment;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Skills;
using TopDownGame.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TopDownGame.EditorTools
{
    public sealed class ProjectValidationIssue
    {
        public ProjectValidationIssue(string message, UnityEngine.Object context = null)
        {
            Message = message;
            Context = context;
        }

        public string Message { get; }
        public UnityEngine.Object Context { get; }
    }

    public sealed class ProjectValidationResult
    {
        private readonly List<ProjectValidationIssue> errors =
            new List<ProjectValidationIssue>();

        public IReadOnlyList<ProjectValidationIssue> Errors => errors;
        public bool IsValid => errors.Count == 0;

        public void AddError(string message, UnityEngine.Object context = null)
        {
            errors.Add(new ProjectValidationIssue(message, context));
        }
    }

    /// <summary>
    /// Validates generated content as a graph so broken scene destinations are
    /// reported before a player reaches them at runtime. All problems are collected
    /// in one pass to avoid a slow fix-rebuild-fail loop.
    /// </summary>
    public static class TopDownProjectValidator
    {
        private const string SceneRoot = "Assets/Scenes";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

        [MenuItem("Top Down Game/Validate Project", priority = 101)]
        public static void ValidateProjectFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ProjectValidationResult result = ValidateProject(true);
            if (result.IsValid)
            {
                EditorUtility.DisplayDialog("Top Down Game Validator",
                    "Validation passed. No project errors were found.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Top Down Game Validator",
                    $"Validation failed with {result.Errors.Count} error(s). " +
                    "See Console for details.", "OK");
            }
        }

        [MenuItem("Top Down Game/Validate Project Automated _F11", priority = 102)]
        public static void ValidateProjectAutomated()
        {
            ProjectValidationResult result = ValidateProject(true);
            if (!result.IsValid)
            {
                throw new BuildFailedException("Top Down Game validation failed with " +
                    result.Errors.Count + " error(s).");
            }

            Debug.Log("Top Down Game automated validation passed.");
        }

        public static void ValidateProjectCommandLine()
        {
            ProjectValidationResult result = ValidateProject(true);
            if (!result.IsValid)
            {
                throw new BuildFailedException(
                    $"Top Down Game validation failed with {result.Errors.Count} error(s).");
            }
        }

        public static ProjectValidationResult ValidateProject(bool logResult = false)
        {
            ProjectValidationResult result = new ProjectValidationResult();
            Dictionary<string, string> scenes = FindProjectScenes(result);
            HashSet<string> enabledBuildScenes = ValidateBuildSettings(scenes, result);

            ValidateMapData(scenes, enabledBuildScenes, result);
            ValidateCatalogs(result);
            ValidatePrefabs(result);
            ValidateSceneContents(scenes, enabledBuildScenes, result);

            if (logResult)
            {
                LogResult(result);
            }

            return result;
        }

        private static Dictionary<string, string> FindProjectScenes(
            ProjectValidationResult result)
        {
            Dictionary<string, string> scenes =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { SceneRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = Path.GetFileNameWithoutExtension(path);
                if (scenes.ContainsKey(sceneName))
                {
                    result.AddError($"Duplicate scene name '{sceneName}': " +
                        $"'{scenes[sceneName]}' and '{path}'.");
                }
                else
                {
                    scenes.Add(sceneName, path);
                }
            }

            if (scenes.Count == 0)
            {
                result.AddError($"No scenes were found under '{SceneRoot}'.");
            }

            return scenes;
        }

        private static HashSet<string> ValidateBuildSettings(
            IReadOnlyDictionary<string, string> scenes, ProjectValidationResult result)
        {
            HashSet<string> enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                string path = NormalizePath(buildScene.path);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    result.AddError($"Build Settings contains a missing scene: '{path}'.");
                    continue;
                }

                if (buildScene.enabled)
                {
                    enabled.Add(Path.GetFileNameWithoutExtension(path));
                }
            }

            foreach (KeyValuePair<string, string> scene in scenes)
            {
                if (!enabled.Contains(scene.Key))
                {
                    result.AddError($"Scene '{scene.Key}' exists at '{scene.Value}' but is " +
                        "missing or disabled in Build Settings.",
                        AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.Value));
                }
            }

            return enabled;
        }

        private static void ValidateMapData(IReadOnlyDictionary<string, string> scenes,
            ISet<string> enabledBuildScenes, ProjectValidationResult result)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<MapData> maps = LoadAssets<MapData>("Assets/ScriptableObjects/Maps");
            if (maps.Count == 0)
            {
                result.AddError("No MapData assets were found under " +
                    "'Assets/ScriptableObjects/Maps'.");
            }

            foreach (MapData map in maps)
            {
                string path = AssetDatabase.GetAssetPath(map);
                if (string.IsNullOrWhiteSpace(map.MapId))
                {
                    result.AddError($"MapData at '{path}' has an empty map id.", map);
                }
                else
                {
                    if (!ids.Add(map.MapId))
                    {
                        result.AddError($"Duplicate MapData id '{map.MapId}' at '{path}'.", map);
                    }

                    ValidateSceneTarget("MapData", map.MapId, map.NextScene, scenes,
                        enabledBuildScenes, result, map);
                    if (!scenes.ContainsKey(map.MapId))
                    {
                        result.AddError($"MapData '{map.MapId}' has no scene with the same name.",
                            map);
                    }
                }
            }
        }

        private static void ValidateCatalogs(ProjectValidationResult result)
        {
            ValidateUniqueIds(LoadAssets<EnemyData>("Assets/ScriptableObjects/Enemies"),
                enemy => enemy.EnemyId, "EnemyData", result);
            ValidateUniqueIds(LoadAssets<EquipmentData>("Assets/ScriptableObjects/Equipment"),
                item => item.ItemId, "EquipmentData", result);

            HashSet<SkillId> skillIds = new HashSet<SkillId>();
            foreach (SkillData skill in LoadAssets<SkillData>("Assets/ScriptableObjects/Skills"))
            {
                if (!skillIds.Add(skill.Id))
                {
                    result.AddError($"Duplicate SkillData id '{skill.Id}' at " +
                        $"'{AssetDatabase.GetAssetPath(skill)}'.", skill);
                }

                if (string.IsNullOrWhiteSpace(skill.DisplayName))
                {
                    result.AddError($"SkillData '{skill.name}' has an empty display name.", skill);
                }
            }

            foreach (SkillId id in Enum.GetValues(typeof(SkillId)))
            {
                if (!skillIds.Contains(id))
                {
                    result.AddError($"Skill catalog is missing SkillId '{id}'.");
                }
            }
        }

        private static void ValidatePrefabs(ProjectValidationResult result)
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (player == null)
            {
                result.AddError($"Required player prefab is missing: '{PlayerPrefabPath}'.");
            }
            else
            {
                RequireComponent<PlayerMovement>(player, PlayerPrefabPath, result);
                RequireComponent<PlayerDodge>(player, PlayerPrefabPath, result);
                RequireComponent<PlayerHealth>(player, PlayerPrefabPath, result);
                RequireComponent<PlayerStats>(player, PlayerPrefabPath, result);
                RequireComponent<SkillManager>(player, PlayerPrefabPath, result);
                PlayerDodge dodge = player.GetComponent<PlayerDodge>();
                if (dodge != null && !dodge.HasRequiredReferences)
                    result.AddError("Player dodge is missing movement, health, animator or input reference.", dodge);
                if (dodge != null && player.GetComponent<PlayerMovement>() != null &&
                    new SerializedObject(player.GetComponent<PlayerMovement>()).FindProperty("dodge").objectReferenceValue != dodge)
                    result.AddError("Player movement is not connected to its dodge controller.", player);
                InputAction dodgeAction = player.GetComponent<PlayerInput>()?.actions?.FindAction("Player/Dodge", false);
                if (dodgeAction == null || !dodgeAction.bindings.Any(binding => binding.path == "<Keyboard>/space") ||
                    !dodgeAction.bindings.Any(binding => binding.path == "<Gamepad>/buttonSouth"))
                    result.AddError("Player dodge requires Space and gamepad South bindings.", player);
            }

            string[] enemyPrefabGuids = AssetDatabase.FindAssets("t:Prefab",
                new[] { "Assets/Prefabs/Enemies" });
            if (enemyPrefabGuids.Length == 0)
            {
                result.AddError("No enemy prefabs were found under 'Assets/Prefabs/Enemies'.");
            }

            foreach (string guid in enemyPrefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                RequireComponent<EnemyBase>(prefab, path, result);
                RequireComponent<EnemyHealth>(prefab, path, result);
                if (prefab != null && prefab.GetComponent<EnemyBase>()?.Data == null)
                {
                    result.AddError($"Enemy prefab '{path}' has no EnemyData reference.", prefab);
                }
            }
        }

        private static void ValidateSceneContents(IReadOnlyDictionary<string, string> scenes,
            ISet<string> enabledBuildScenes, ProjectValidationResult result)
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (KeyValuePair<string, string> entry in scenes.OrderBy(pair => pair.Key))
                {
                    Scene scene = EditorSceneManager.OpenScene(entry.Value, OpenSceneMode.Single);
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        int missingScripts = CountMissingScripts(root);
                        if (missingScripts > 0)
                        {
                            result.AddError($"Scene '{entry.Key}' contains {missingScripts} " +
                                $"missing script reference(s) under '{root.name}'.", root);
                        }
                    }

                    ValidateSceneManagers(scene, entry.Key, result);
                    ValidateSceneTargets(scene, entry.Key, scenes, enabledBuildScenes, result);
                }
            }
            catch (Exception exception)
            {
                result.AddError("Could not inspect scene contents: " + exception.Message);
            }
            finally
            {
                if (originalSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
            }
        }

        private static void ValidateSceneManagers(Scene scene, string sceneName,
            ProjectValidationResult result)
        {
            if (sceneName != "MainMenu")
            {
                DodgeHUD[] huds = FindComponentsInScene<DodgeHUD>(scene).ToArray();
                if (huds.Length != 1) result.AddError($"Scene '{sceneName}' must contain exactly one dodge HUD.");
                foreach (DodgeHUD hud in huds)
                    if (!hud.HasRequiredReferences) result.AddError($"Dodge HUD in '{sceneName}' has missing references.", hud);
            }
            if (sceneName == "Lobby")
            {
                LobbyTutorialUI[] tutorials = FindComponentsInScene<LobbyTutorialUI>(scene).ToArray();
                if (tutorials.Length != 1) result.AddError("Lobby must contain exactly one first-entry tutorial.");
                foreach (LobbyTutorialUI tutorial in tutorials)
                    if (new SerializedObject(tutorial).FindProperty("dialog").objectReferenceValue == null)
                        result.AddError("Lobby tutorial has no dialog reference.", tutorial);
            }
            foreach (SkillSelectionUI rewards in FindComponentsInScene<SkillSelectionUI>(scene))
            {
                SerializedProperty catalog = new SerializedObject(rewards).FindProperty("skillDatabase");
                var ids = new HashSet<SkillId>();
                if (catalog != null)
                    for (int i = 0; i < catalog.arraySize; i++)
                    {
                        SkillData data = catalog.GetArrayElementAtIndex(i).objectReferenceValue as SkillData;
                        if (data == null)
                            result.AddError($"Skill reward catalog in scene '{sceneName}' has an empty entry at {i}.", rewards);
                        else if (!ids.Add(data.Id))
                            result.AddError($"Skill reward catalog in scene '{sceneName}' duplicates '{data.Id}'.", rewards);
                    }
                foreach (SkillId id in Enum.GetValues(typeof(SkillId)))
                    if (!ids.Contains(id)) result.AddError($"Skill reward catalog in scene '{sceneName}' is missing '{id}'.", rewards);
            }
            foreach (MapManager manager in FindComponentsInScene<MapManager>(scene))
            {
                if (manager.Data == null)
                {
                    result.AddError($"MapManager in scene '{sceneName}' has no MapData.", manager);
                }
                else if (!string.Equals(manager.Data.MapId, sceneName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError($"MapManager in scene '{sceneName}' references MapData " +
                        $"'{manager.Data.MapId}'.", manager);
                }
                if (manager.Data != null && !manager.Data.IsBossMap)
                    ValidateHuntingPopulation(scene, sceneName, manager, result);
            }
        }

        private static void ValidateHuntingPopulation(Scene scene, string sceneName,
            MapManager manager, ProjectValidationResult result)
        {
            EnemySpawnManager spawner = manager.GetComponent<EnemySpawnManager>();
            if (spawner == null || !spawner.enabled || !spawner.gameObject.activeInHierarchy)
                result.AddError($"Scene '{sceneName}' hunting population has no active EnemySpawnManager.", manager);

            int validSeeds = 0;
            foreach (EnemyBase enemy in FindComponentsInScene<EnemyBase>(scene))
            {
                if (!enemy.enabled || !enemy.gameObject.activeInHierarchy) continue;
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (enemy.Data == null || health == null || !health.enabled)
                    result.AddError($"Scene '{sceneName}' hunting population seed '{enemy.name}' needs EnemyData and enabled EnemyHealth.", enemy);
                else validSeeds++;
            }
            if (validSeeds == 0)
                result.AddError($"Scene '{sceneName}' hunting population has no active enemy seed; its pool cannot initialize.", manager);
        }

        private static void ValidateSceneTargets(Scene scene, string sceneName,
            IReadOnlyDictionary<string, string> scenes,
            ISet<string> enabledBuildScenes, ProjectValidationResult result)
        {
            foreach (MapDoor door in FindComponentsInScene<MapDoor>(scene))
            {
                string target = ReadStringProperty(door, "nextScene");
                ValidateSceneTarget("MapDoor", sceneName, target, scenes,
                    enabledBuildScenes, result, door);
                MapData destination = door.DestinationMap;
                if (target != "Lobby" && (destination == null || destination.MapId != target ||
                    string.IsNullOrWhiteSpace(destination.DisplayName)))
                    result.AddError("MapDoor '" + sceneName + "' has missing or mismatched destination metadata.", door);
                if (target == "Lobby" && destination != null)
                    result.AddError("MapDoor to Lobby must not reference combat map metadata.", door);
            }

            foreach (LobbyPortal portal in FindComponentsInScene<LobbyPortal>(scene))
            {
                string target = ReadStringProperty(portal, "destinationScene");
                ValidateSceneTarget("LobbyPortal", sceneName, target, scenes,
                    enabledBuildScenes, result, portal);
                SerializedProperty catalog = new SerializedObject(portal).FindProperty("destinationMaps");
                var ids = new HashSet<string>();
                for (int i = 0; catalog != null && i < catalog.arraySize; i++)
                {
                    MapData data = catalog.GetArrayElementAtIndex(i).objectReferenceValue as MapData;
                    if (data == null) result.AddError("Lobby travel catalog contains an empty map reference.", portal);
                    else if (!ids.Add(data.MapId)) result.AddError("Lobby travel catalog duplicates '" + data.MapId + "'.", portal);
                }
                foreach (string destination in LobbyPortal.Destinations)
                    if (portal.GetDestinationData(destination) == null)
                        result.AddError("Lobby travel catalog is missing '" + destination + "'.", portal);
            }
        }

        private static void ValidateSceneTarget(string sourceType, string sourceName,
            string target, IReadOnlyDictionary<string, string> scenes,
            ISet<string> enabledBuildScenes, ProjectValidationResult result,
            UnityEngine.Object context)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                result.AddError($"{sourceType} '{sourceName}' has an empty destination.", context);
                return;
            }

            if (!scenes.ContainsKey(target))
            {
                result.AddError($"{sourceType} '{sourceName}' targets missing scene " +
                    $"'{target}'.", context);
            }
            else if (!enabledBuildScenes.Contains(target))
            {
                result.AddError($"{sourceType} '{sourceName}' targets scene '{target}', which is " +
                    "disabled in Build Settings.", context);
            }
        }

        private static void ValidateUniqueIds<T>(IEnumerable<T> assets,
            Func<T, string> getId, string label, ProjectValidationResult result)
            where T : UnityEngine.Object
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (T asset in assets)
            {
                string id = getId(asset);
                string path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrWhiteSpace(id))
                {
                    result.AddError($"{label} at '{path}' has an empty id.", asset);
                }
                else if (!ids.Add(id))
                {
                    result.AddError($"Duplicate {label} id '{id}' at '{path}'.", asset);
                }
            }
        }

        private static List<T> LoadAssets<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToList();
        }

        private static IEnumerable<T> FindComponentsInScene<T>(Scene scene)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true));
        }

        private static int CountMissingScripts(GameObject root)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            foreach (Transform child in root.transform)
            {
                count += CountMissingScripts(child.gameObject);
            }

            return count;
        }

        private static void RequireComponent<T>(GameObject prefab, string path,
            ProjectValidationResult result) where T : Component
        {
            if (prefab != null && prefab.GetComponent<T>() == null)
            {
                result.AddError($"Prefab '{path}' is missing required component " +
                    $"'{typeof(T).Name}'.", prefab);
            }
        }

        private static string ReadStringProperty(UnityEngine.Object target, string propertyName)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            return property != null ? property.stringValue : string.Empty;
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static void LogResult(ProjectValidationResult result)
        {
            foreach (ProjectValidationIssue issue in result.Errors)
            {
                Debug.LogError("[Top Down Game Validator] " + issue.Message, issue.Context);
            }

            if (result.IsValid)
            {
                Debug.Log("[Top Down Game Validator] PASS — no project errors found.");
            }
            else
            {
                Debug.LogError($"[Top Down Game Validator] FAIL — " +
                    $"{result.Errors.Count} error(s) found.");
            }
        }
    }
}
