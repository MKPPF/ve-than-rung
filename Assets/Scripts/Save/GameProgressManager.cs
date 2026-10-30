using System;
using System.Collections.Generic;
using TopDownGame.Equipment;
using TopDownGame.Player;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownGame.Save
{
    /// <summary>Keeps the live campaign separate from unreadable preferences until the player explicitly resolves recovery.</summary>
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
        [SerializeField] private bool tutorialInitialized;
        [SerializeField] private bool tutorialCompleted;
        private bool playerProgressApplied;
        private ProgressLoadStatus loadStatus;

        public string CurrentMap => currentMap;
        public void SetCurrentMap(string mapName)
        {
            if (!string.IsNullOrWhiteSpace(mapName))
            {
                currentMap = mapName;
            }
        }
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
        public ProgressLoadStatus LoadStatus => loadStatus;
        public bool NeedsSaveRecovery => loadStatus == ProgressLoadStatus.RecoveryAvailable || loadStatus == ProgressLoadStatus.Unreadable;
        public bool HasStoredProgress => PlayerPrefs.HasKey(ActiveSaveKey) || PlayerPrefs.HasKey(BackupSaveKey);
        public bool HasSavedGame => !NeedsSaveRecovery && PlayerPrefs.HasKey(ActiveSaveKey);
        public bool TutorialCompleted => tutorialCompleted;
        public bool ShouldShowTutorial => HasSavedGame && tutorialInitialized && !tutorialCompleted;
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
            LoadProgress();
            if (!potentialInitialized)
            {
                // A fresh or unresolved campaign has no points to migrate. Valid
                // legacy progression is migrated by ProgressSnapshot before application.
                potentialPoints = 0;
                potentialInitialized = true;
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
            ArchiveUnreadablePrimary();
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
            tutorialInitialized = true;
            tutorialCompleted = false;
            playerProgressApplied = false;
            PlayerPrefs.SetInt(TopDownGame.Core.GamePreferences.Key("TopDownGame.SelectedHero"), (int)HeroCharacterId.ForestWarrior);
            NormalizeCollections();
            string json = JsonUtility.ToJson(CreateSnapshot());
            // A new campaign owns its own backup; an older campaign must not
            // reappear after the player has explicitly chosen to start over.
            PlayerPrefs.SetString(BackupSaveKey, json);
            PlayerPrefs.SetString(ActiveSaveKey, json);
            PlayerPrefs.Save();
            loadStatus = ProgressLoadStatus.Ready;
        }

        public void CompleteTutorial()
        {
            if (!HasSavedGame || tutorialCompleted) return;
            tutorialInitialized = true;
            tutorialCompleted = true;
            // Do not capture a partially initialized player or modify their loadout.
            SaveProgress();
        }

        public void CapturePlayer(PlayerStats stats, PlayerHealth health, SkillInventory inventory)
        {
            if (UpdatePlayerSnapshot(stats, health, inventory)) SaveProgress();
        }

        /// <summary>Keeps earned progress on defeat and saves recovered vitals without reviving the current, defeated avatar.</summary>
        public void CaptureDefeatedPlayer(PlayerStats stats, PlayerHealth health, SkillInventory inventory)
        {
            if (health == null || health.IsAlive || !UpdatePlayerSnapshot(stats, health, inventory)) return;
            currentHealth = stats.MaxHealth;
            currentMana = stats.MaxMana;
            SaveProgress();
        }

        private bool UpdatePlayerSnapshot(PlayerStats stats, PlayerHealth health, SkillInventory inventory)
        {
            if (NeedsSaveRecovery || stats == null || health == null)
            {
                return false;
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

            return true;
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
            if (NeedsSaveRecovery) return;
            bool primaryExists = PlayerPrefs.HasKey(ActiveSaveKey);
            string previous = PlayerPrefs.GetString(ActiveSaveKey, string.Empty);
            if ((primaryExists && !ProgressSnapshot.TryDeserialize(previous, out _)) ||
                (!primaryExists && PlayerPrefs.HasKey(BackupSaveKey)))
            {
                RefreshRecoveryStatus();
                return;
            }
            NormalizeCollections();
            string json = JsonUtility.ToJson(CreateSnapshot());
            if (primaryExists && previous != json) PlayerPrefs.SetString(BackupSaveKey, previous);
            else if (!ProgressSnapshot.TryDeserialize(PlayerPrefs.GetString(BackupSaveKey, string.Empty), out _))
                PlayerPrefs.SetString(BackupSaveKey, json);
            PlayerPrefs.SetString(ActiveSaveKey, json);
            PlayerPrefs.Save();
            if (loadStatus != ProgressLoadStatus.Recovered) loadStatus = ProgressLoadStatus.Ready;
        }

        public bool LoadProgress()
        {
            if (!PlayerPrefs.HasKey(ActiveSaveKey) ||
                !ProgressSnapshot.TryDeserialize(PlayerPrefs.GetString(ActiveSaveKey, string.Empty), out ProgressSnapshot snapshot))
            {
                RefreshRecoveryStatus();
                return false;
            }

            ApplySnapshot(snapshot);
            loadStatus = ProgressLoadStatus.Ready;
            if (snapshot.needsMigration) SaveProgress();
            return true;
        }

        public bool TryGetBackupSummary(out int level, out int savedGold) => TryGetBackupSummary(out level, out savedGold, out _);

        public bool TryGetBackupSummary(out int level, out int savedGold, out string revision)
        {
            revision = PlayerPrefs.GetString(BackupSaveKey, string.Empty);
            bool valid = ProgressSnapshot.TryDeserialize(revision, out ProgressSnapshot backup);
            level = valid ? backup.playerLevel : 0;
            savedGold = valid ? backup.gold : 0;
            return valid;
        }

        public bool TryRestoreBackup(string expectedRevision = null)
        {
            if (!NeedsSaveRecovery) return false;
            // Recheck both records at confirmation time. A newly valid primary
            // takes precedence over an older backup shown by a stale dialog.
            if (ProgressSnapshot.TryDeserialize(PlayerPrefs.GetString(ActiveSaveKey, string.Empty), out _))
            {
                LoadProgress();
                return false;
            }
            string revision = PlayerPrefs.GetString(BackupSaveKey, string.Empty);
            if ((expectedRevision != null && !string.Equals(revision, expectedRevision, StringComparison.Ordinal)) ||
                !ProgressSnapshot.TryDeserialize(revision, out ProgressSnapshot backup))
            {
                RefreshRecoveryStatus();
                return false;
            }
            ArchiveUnreadablePrimary();
            PlayerPrefs.SetString(ActiveSaveKey, JsonUtility.ToJson(backup));
            PlayerPrefs.Save();
            ApplySnapshot(backup);
            playerProgressApplied = false;
            loadStatus = ProgressLoadStatus.Recovered;
            return true;
        }

        private void RefreshRecoveryStatus()
        {
            loadStatus = !HasStoredProgress ? ProgressLoadStatus.Missing
                : TryGetBackupSummary(out _, out _) ? ProgressLoadStatus.RecoveryAvailable : ProgressLoadStatus.Unreadable;
        }

        private void ArchiveUnreadablePrimary()
        {
            if (!PlayerPrefs.HasKey(ActiveSaveKey)) return;
            string original = PlayerPrefs.GetString(ActiveSaveKey, string.Empty);
            if (!ProgressSnapshot.TryDeserialize(original, out _)) PlayerPrefs.SetString(UnreadableSaveKey, original);
        }

        private ProgressSnapshot CreateSnapshot() => new ProgressSnapshot
        {
            currentMap = currentMap, currentHealth = currentHealth, maxHealth = maxHealth, currentMana = currentMana,
            gold = gold, baseDamage = baseDamage, armor = armor, cooldownRate = cooldownRate,
            playerLevel = playerLevel, experience = experience, potentialPoints = potentialPoints,
            vitalityPoints = vitalityPoints, powerPoints = powerPoints, guardPoints = guardPoints, hastePoints = hastePoints,
            potentialInitialized = potentialInitialized, hunterEssence = hunterEssence, equipmentEnhancements = equipmentEnhancements,
            items = items, ownedSkills = ownedSkills, equippedSkills = equippedSkills, defeatedBosses = defeatedBosses,
            equippedWeaponId = equippedWeaponId, equippedHelmetId = equippedHelmetId, equippedArmorId = equippedArmorId,
            equippedPantsId = equippedPantsId, equippedNecklaceId = equippedNecklaceId, chestItems = chestItems,
            chestInitialized = chestInitialized, unlockedRegions = unlockedRegions, safeRespawnScene = safeRespawnScene,
            safeRespawnPosition = safeRespawnPosition, hasSafeRespawn = hasSafeRespawn,
            tutorialInitialized = tutorialInitialized, tutorialCompleted = tutorialCompleted,
            selectedHero = PlayerPrefs.GetInt(TopDownGame.Core.GamePreferences.Key("TopDownGame.SelectedHero"), (int)HeroCharacterId.ForestWarrior)
        };

        private void ApplySnapshot(ProgressSnapshot snapshot)
        {
            currentMap = snapshot.currentMap; currentHealth = snapshot.currentHealth; maxHealth = snapshot.maxHealth; currentMana = snapshot.currentMana;
            gold = snapshot.gold; baseDamage = snapshot.baseDamage; armor = snapshot.armor; cooldownRate = snapshot.cooldownRate;
            playerLevel = snapshot.playerLevel; experience = snapshot.experience; potentialPoints = snapshot.potentialPoints;
            vitalityPoints = snapshot.vitalityPoints; powerPoints = snapshot.powerPoints; guardPoints = snapshot.guardPoints; hastePoints = snapshot.hastePoints;
            potentialInitialized = snapshot.potentialInitialized; hunterEssence = snapshot.hunterEssence; equipmentEnhancements = snapshot.equipmentEnhancements;
            items = snapshot.items; ownedSkills = snapshot.ownedSkills; equippedSkills = snapshot.equippedSkills; defeatedBosses = snapshot.defeatedBosses;
            equippedWeaponId = snapshot.equippedWeaponId; equippedHelmetId = snapshot.equippedHelmetId; equippedArmorId = snapshot.equippedArmorId;
            equippedPantsId = snapshot.equippedPantsId; equippedNecklaceId = snapshot.equippedNecklaceId; chestItems = snapshot.chestItems;
            chestInitialized = snapshot.chestInitialized; unlockedRegions = snapshot.unlockedRegions; safeRespawnScene = snapshot.safeRespawnScene;
            safeRespawnPosition = snapshot.safeRespawnPosition; hasSafeRespawn = snapshot.hasSafeRespawn;
            tutorialInitialized = snapshot.tutorialInitialized; tutorialCompleted = snapshot.tutorialCompleted;
            int heroVal = snapshot.selectedHero >= 0 ? snapshot.selectedHero : (int)HeroCharacterId.ForestWarrior;
            if (!ThanhGiongHeroController.IsHeroUnlocked((HeroCharacterId)heroVal))
            {
                heroVal = (int)HeroCharacterId.ForestWarrior;
            }
            PlayerPrefs.SetInt(TopDownGame.Core.GamePreferences.Key("TopDownGame.SelectedHero"), heroVal);
            PlayerPrefs.Save();
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
            if (NeedsSaveRecovery || stats == null || health == null)
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

            ThanhGiongHeroController heroController = stats.GetComponent<ThanhGiongHeroController>();
            if (heroController == null)
            {
                heroController = ThanhGiongHeroController.Ensure(stats.gameObject);
            }
            int savedHero = PlayerPrefs.GetInt(TopDownGame.Core.GamePreferences.Key("TopDownGame.SelectedHero"), (int)HeroCharacterId.ForestWarrior);
            if (!ThanhGiongHeroController.IsHeroUnlocked((HeroCharacterId)savedHero))
            {
                savedHero = (int)HeroCharacterId.ForestWarrior;
                PlayerPrefs.SetInt(TopDownGame.Core.GamePreferences.Key("TopDownGame.SelectedHero"), (int)HeroCharacterId.ForestWarrior);
                PlayerPrefs.Save();
            }
            heroController?.SelectHero((HeroCharacterId)savedHero, false);

            playerProgressApplied = true;
        }

        public static void ResetAllGameProgress()
        {
            PlayerPrefs.DeleteKey(TopDownGame.Core.GamePreferences.Key("TopDownGame.Progress.v1"));
            PlayerPrefs.DeleteKey(TopDownGame.Core.GamePreferences.Key("TopDownGame.Progress.v1.backup"));
            PlayerPrefs.DeleteKey(TopDownGame.Core.GamePreferences.Key("TopDownGame.Progress.v1.unreadable"));
            PlayerPrefs.DeleteKey(TopDownGame.Core.GamePreferences.Key("TopDownGame.SelectedHero"));
            PlayerPrefs.DeleteKey("TopDownGame.Progress.v1");
            PlayerPrefs.DeleteKey("TopDownGame.Progress.v1.backup");
            PlayerPrefs.DeleteKey("TopDownGame.Progress.v1.unreadable");
            PlayerPrefs.DeleteKey("TopDownGame.SelectedHero");
            PlayerPrefs.Save();

            if (Instance != null)
            {
                Instance.BeginNewGame();
            }
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
        private static string BackupSaveKey => ActiveSaveKey + ".Backup";
        private static string UnreadableSaveKey => ActiveSaveKey + ".Unreadable";

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
