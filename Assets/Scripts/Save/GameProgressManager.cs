using System;
using System.Collections.Generic;
using TopDownGame.Equipment;
using TopDownGame.Player;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.Save
{
    public sealed class GameProgressManager : MonoBehaviour
    {
        private const string SaveKey = "TopDownGame.Progress.v1";
        private const string TestSaveKey = "TopDownGame.Tests.Progress.v1";

        [SerializeField, Min(5f)] private float autosaveInterval = 20f;
        private float nextAutosaveTime;

        [Serializable]
        public sealed class SkillProgress
        {
            public string skillId;
            public int level = 1;
        }

        [Serializable]
        public sealed class EquipmentEnhancementProgress
        {
            public string itemId;
            public int level;
        }

        public static GameProgressManager Instance { get; private set; }
        public event Action<int> HunterEssenceChanged;

        [SerializeField] private string currentMap = "Lobby";
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentMana = 100f;
        [SerializeField] private int gold = 2500;
        [SerializeField] private float baseDamage = 20f;
        [SerializeField] private float armor = 5f;
        [SerializeField] private float cooldownRate = 1f;
        [SerializeField] private int playerLevel = 1;
        [SerializeField] private int experience;
        [SerializeField] private int potentialPoints;
        [SerializeField] private int vitalityPoints;
        [SerializeField] private int powerPoints;
        [SerializeField] private int guardPoints;
        [SerializeField] private int hastePoints;
        [SerializeField] private bool potentialInitialized;
        [SerializeField, Min(0)] private int hunterEssence;
        [SerializeField] private List<EquipmentEnhancementProgress>
            equipmentEnhancements = new List<EquipmentEnhancementProgress>();
        [SerializeField] private List<string> items = new List<string>();
        [SerializeField] private List<SkillProgress> ownedSkills = new List<SkillProgress>();
        [SerializeField] private string[] equippedSkills = new string[4];
        [SerializeField] private List<string> defeatedBosses = new List<string>();
        [SerializeField] private string equippedWeaponId;
        [SerializeField] private string equippedHelmetId;
        [SerializeField] private string equippedArmorId;
        [SerializeField] private string equippedPantsId;
        [SerializeField] private string equippedNecklaceId;
        [SerializeField] private List<string> chestItems = new List<string>();
        [SerializeField] private bool chestInitialized;
        [SerializeField] private List<string> unlockedRegions =
            new List<string> { "Lobby", "Map01" };
        [SerializeField] private string safeRespawnScene;
        [SerializeField] private Vector3 safeRespawnPosition;
        [SerializeField] private bool hasSafeRespawn;
        private bool playerProgressApplied;

        public string CurrentMap => currentMap;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float CurrentMana => currentMana;
        public int Gold => gold;
        public IReadOnlyList<string> Items => items;
        public IReadOnlyList<SkillProgress> OwnedSkills => ownedSkills;
        public IReadOnlyList<string> EquippedSkills => equippedSkills;
        public string EquippedWeaponId => equippedWeaponId;
        public string EquippedHelmetId => equippedHelmetId;
        public string EquippedArmorId => equippedArmorId;
        public string EquippedPantsId => equippedPantsId;
        public string EquippedNecklaceId => equippedNecklaceId;
        public IReadOnlyList<string> ChestItems => chestItems;
        public bool HasSavedGame => PlayerPrefs.HasKey(ActiveSaveKey);
        public int PlayerLevel => playerLevel;
        public int PotentialPoints => potentialPoints;
        public int HunterEssence => hunterEssence;
        public IReadOnlyList<string> UnlockedRegions => unlockedRegions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject managerObject = new GameObject(nameof(GameProgressManager));
            managerObject.AddComponent<GameProgressManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            NormalizeCollections();
            bool loadedProgress = LoadProgress();
            bool migratedPotential = loadedProgress && !potentialInitialized;
            if (!potentialInitialized)
            {
                // Save v1 files created before Potential existed receive every
                // point their current level should have earned exactly once.
                potentialPoints = loadedProgress ? Mathf.Max(0, playerLevel - 1) : 0;
                potentialInitialized = true;
            }
            if (migratedPotential)
            {
                SaveProgress();
            }
            // Opening the menu must not create a save or overwrite existing gold.
            // A fresh game is persisted only after the player chooses New Game.
            nextAutosaveTime = Time.unscaledTime + autosaveInterval;
        }

        private void Update()
        {
#if UNITY_EDITOR
            return;
#else
            if (Time.unscaledTime < nextAutosaveTime)
            {
                return;
            }

            nextAutosaveTime = Time.unscaledTime + autosaveInterval;
            CaptureActivePlayer();
#endif
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && playerProgressApplied)
            {
                CaptureActivePlayer();
            }
        }

        private void OnApplicationQuit()
        {
#if UNITY_EDITOR
            // Stopping Play Mode is a test workflow, not a real game quit.
            return;
#else
            CaptureActivePlayer();
#endif
        }

        public void BeginNewGame()
        {
            currentMap = "Lobby";
            currentHealth = PlayerStats.ExperimentalMaxHealth;
            maxHealth = PlayerStats.ExperimentalMaxHealth;
            currentMana = PlayerStats.ExperimentalMaxMana;
            gold = 2500;
            baseDamage = 20f;
            armor = 5f;
            cooldownRate = 1f;
            playerLevel = 1;
            experience = 0;
            potentialPoints = 0;
            vitalityPoints = 0;
            powerPoints = 0;
            guardPoints = 0;
            hastePoints = 0;
            potentialInitialized = true;
            hunterEssence = 0;
            equipmentEnhancements.Clear();
            items.Clear();
            ownedSkills.Clear();
            defeatedBosses.Clear();
            equippedSkills = new string[4];
            equippedWeaponId = string.Empty;
            equippedHelmetId = string.Empty;
            equippedArmorId = string.Empty;
            equippedPantsId = string.Empty;
            equippedNecklaceId = string.Empty;
            chestItems.Clear();
            chestInitialized = false;
            unlockedRegions.Clear();
            unlockedRegions.Add("Lobby");
            unlockedRegions.Add("Map01");
            safeRespawnScene = string.Empty;
            safeRespawnPosition = Vector3.zero;
            hasSafeRespawn = false;
            SaveProgress();
        }

        public void CapturePlayer(PlayerStats stats, PlayerHealth health, SkillInventory inventory)
        {
            if (stats == null || health == null)
            {
                return;
            }

            currentMap = SceneManager.GetActiveScene().name;
            maxHealth = stats.PersistentMaxHealth;
            currentHealth = health.CurrentHealth;
            currentMana = stats.CurrentMana;
            gold = stats.Gold;
            baseDamage = stats.PersistentBaseDamage;
            armor = stats.PersistentArmor;
            cooldownRate = stats.PersistentCooldownRate;
            playerLevel = stats.Level;
            experience = stats.Experience;
            potentialPoints = stats.AvailablePotentialPoints;
            vitalityPoints = stats.VitalityPoints;
            powerPoints = stats.PowerPoints;
            guardPoints = stats.GuardPoints;
            hastePoints = stats.HastePoints;
            potentialInitialized = true;
            UnlockRegion(currentMap);

            if (inventory != null)
            {
                inventory.WriteProgress(ownedSkills, equippedSkills);
            }

            EquipmentInventory equipment = stats.GetComponent<EquipmentInventory>();
            if (equipment != null)
            {
                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {
                    EquipmentData equipped = equipment.GetEquipped(slot);
                    SetEquippedItem(slot, equipped != null ? equipped.ItemId : string.Empty);
                }
            }

            SaveProgress();
        }

        public void CaptureActivePlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return;
            }

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health == null || !health.IsAlive)
            {
                return;
            }

            CapturePlayer(
                player.GetComponent<PlayerStats>(),
                health,
                player.GetComponent<SkillInventory>());
        }

        public void SaveProgress()
        {
            NormalizeCollections();
            PlayerPrefs.SetString(ActiveSaveKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public bool LoadProgress()
        {
            if (!PlayerPrefs.HasKey(ActiveSaveKey))
            {
                return false;
            }

            string json = PlayerPrefs.GetString(ActiveSaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                PlayerPrefs.DeleteKey(ActiveSaveKey);
                return false;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(json, this);
                NormalizeCollections();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not load saved progress: " + exception.Message);
                PlayerPrefs.DeleteKey(ActiveSaveKey);
                return false;
            }
        }

        public void PrepareRetry()
        {
            maxHealth = Mathf.Max(maxHealth, PlayerStats.ExperimentalMaxHealth);
            currentHealth = maxHealth;
            currentMana = PlayerStats.ExperimentalMaxMana;
            SaveProgress();
        }

        public void ApplyToPlayer(PlayerStats stats, PlayerHealth health, SkillInventory inventory)
        {
            if (stats == null || health == null)
            {
                return;
            }

            stats.LoadProgress(maxHealth, currentMana, gold, baseDamage, armor,
                cooldownRate, playerLevel, experience);
            stats.LoadPotentialProgress(potentialPoints, vitalityPoints,
                powerPoints, guardPoints, hastePoints);
            health.LoadProgress(stats.UsesExperimentalVitals
                ? stats.MaxHealth
                : currentHealth);
            if (hasSafeRespawn && string.Equals(safeRespawnScene,
                SceneManager.GetActiveScene().name, StringComparison.OrdinalIgnoreCase))
            {
                stats.transform.position = safeRespawnPosition;
            }
            bool hasSkillProgress = ownedSkills.Count > 0;
            for (int i = 0; i < equippedSkills.Length; i++)
            {
                hasSkillProgress |= !string.IsNullOrWhiteSpace(equippedSkills[i]);
            }

            if (hasSkillProgress)
            {
                inventory?.LoadProgress(ownedSkills, equippedSkills);
            }

            playerProgressApplied = true;
        }

        public void AddGold(int amount)
        {
            gold = Mathf.Max(0, gold + amount);
        }

        public void AddHunterEssence(int amount)
        {
            if (amount <= 0) return;
            hunterEssence = Mathf.Max(0, hunterEssence + amount);
            HunterEssenceChanged?.Invoke(hunterEssence);
        }

        public int GetEnhancementLevel(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return 0;
            foreach (EquipmentEnhancementProgress progress in equipmentEnhancements)
            {
                if (progress != null && string.Equals(progress.itemId, itemId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return Mathf.Max(0, progress.level);
                }
            }
            return 0;
        }

        public static int EnhancementEssenceCost(int currentLevel)
        {
            int level = Mathf.Clamp(currentLevel, 0, 100);
            return 10 + level * 8 + level * level * 2;
        }

        public bool TryEnhanceEquipment(string itemId, int maximumLevel,
            out int spentEssence)
        {
            spentEssence = 0;
            if (!HasItem(itemId)) return false;
            int currentLevel = GetEnhancementLevel(itemId);
            int cap = Mathf.Max(1, maximumLevel);
            if (currentLevel >= cap) return false;
            int cost = EnhancementEssenceCost(currentLevel);
            if (hunterEssence < cost) return false;

            EquipmentEnhancementProgress target = null;
            foreach (EquipmentEnhancementProgress progress in equipmentEnhancements)
            {
                if (progress != null && string.Equals(progress.itemId, itemId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    target = progress;
                    break;
                }
            }
            if (target == null)
            {
                target = new EquipmentEnhancementProgress { itemId = itemId };
                equipmentEnhancements.Add(target);
            }

            hunterEssence -= cost;
            target.level = currentLevel + 1;
            spentEssence = cost;
            HunterEssenceChanged?.Invoke(hunterEssence);
            SaveProgress();
            return true;
        }

        public void AddItem(string itemId)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                items.Add(itemId);
            }
        }

        public void AddUniqueItem(string itemId)
        {
            if (!string.IsNullOrWhiteSpace(itemId) && !items.Contains(itemId))
            {
                items.Add(itemId);
            }
        }

        public void UnlockRegion(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName) &&
                !unlockedRegions.Contains(sceneName))
            {
                unlockedRegions.Add(sceneName);
                SaveProgress();
            }
        }

        public bool IsRegionUnlocked(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) &&
                unlockedRegions.Contains(sceneName);
        }

        public void SetSafeRespawn(string sceneName, Vector3 position)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || sceneName == "Lobby") return;
            safeRespawnScene = sceneName;
            safeRespawnPosition = position;
            hasSafeRespawn = true;
            UnlockRegion(sceneName);
        }

        public bool HasItem(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && items.Contains(itemId);
        }

        public void SetEquippedWeapon(string itemId)
        {
            SetEquippedItem(EquipmentSlot.Weapon, itemId);
        }

        public string GetEquippedItemId(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Helmet => equippedHelmetId,
                EquipmentSlot.Armor => equippedArmorId,
                EquipmentSlot.Pants => equippedPantsId,
                EquipmentSlot.Necklace => equippedNecklaceId,
                _ => equippedWeaponId
            };
        }

        public void SetEquippedItem(EquipmentSlot slot, string itemId)
        {
            if (!string.IsNullOrWhiteSpace(itemId) && !HasItem(itemId))
            {
                return;
            }

            itemId ??= string.Empty;
            switch (slot)
            {
                case EquipmentSlot.Helmet:
                    equippedHelmetId = itemId;
                    break;
                case EquipmentSlot.Armor:
                    equippedArmorId = itemId;
                    break;
                case EquipmentSlot.Pants:
                    equippedPantsId = itemId;
                    break;
                case EquipmentSlot.Necklace:
                    equippedNecklaceId = itemId;
                    break;
                default:
                    equippedWeaponId = itemId;
                    break;
            }
        }

        public void InitializeChest(IEnumerable<string> startingItems)
        {
            if (chestInitialized)
            {
                return;
            }

            chestInitialized = true;
            chestItems.Clear();
            if (startingItems == null)
            {
                return;
            }

            foreach (string itemId in startingItems)
            {
                if (!string.IsNullOrWhiteSpace(itemId) && !chestItems.Contains(itemId))
                {
                    chestItems.Add(itemId);
                }
            }
        }

        public bool MoveItemToChest(string itemId)
        {
            if (!HasItem(itemId) || chestItems.Contains(itemId))
            {
                return false;
            }

            items.Remove(itemId);
            chestItems.Add(itemId);
            ClearEquippedItem(itemId);
            return true;
        }

        public bool MoveItemFromChest(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !chestItems.Remove(itemId))
            {
                return false;
            }

            AddUniqueItem(itemId);
            return true;
        }

        private void ClearEquippedItem(string itemId)
        {
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (string.Equals(GetEquippedItemId(slot), itemId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    SetEquippedItem(slot, string.Empty);
                }
            }
        }

        public bool IsBossDefeated(string bossId)
        {
            return defeatedBosses.Contains(bossId);
        }

        public void MarkBossDefeated(string bossId)
        {
            if (!string.IsNullOrWhiteSpace(bossId) && !defeatedBosses.Contains(bossId))
            {
                defeatedBosses.Add(bossId);
            }
        }

        private void NormalizeCollections()
        {
            items ??= new List<string>();
            ownedSkills ??= new List<SkillProgress>();
            equipmentEnhancements ??= new List<EquipmentEnhancementProgress>();
            defeatedBosses ??= new List<string>();
            chestItems ??= new List<string>();
            unlockedRegions ??= new List<string>();
            if (!unlockedRegions.Contains("Lobby")) unlockedRegions.Add("Lobby");
            if (!unlockedRegions.Contains("Map01")) unlockedRegions.Add("Map01");

            if (equippedSkills == null || equippedSkills.Length != 4)
            {
                Array.Resize(ref equippedSkills, 4);
            }
        }

        private static string ActiveSaveKey => IsAutomatedTestRun ? TestSaveKey : SaveKey;

        private static bool IsAutomatedTestRun
        {
            get
            {
#if UNITY_EDITOR
                foreach (string argument in System.Environment.GetCommandLineArgs())
                {
                    if (string.Equals(argument, "-runTests", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Interactive Test Runner sessions do not include -runTests. Use
                // reflection so the player assembly stays free of an UnityEditor
                // reference while still isolating the real save in that workflow.
                Type runnerApi = Type.GetType(
                    "UnityEditor.TestTools.TestRunner.Api.TestRunnerApi, UnityEditor.TestRunner");
                System.Reflection.MethodInfo isRunActive = runnerApi?.GetMethod(
                    "IsRunActive", System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.NonPublic);
                if (isRunActive?.Invoke(null, null) is bool running && running)
                {
                    return true;
                }
#endif
                return false;
            }
        }
    }
}
