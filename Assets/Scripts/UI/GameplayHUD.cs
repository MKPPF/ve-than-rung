using System.Collections;
using TMPro;
using TopDownGame.Maps;
using TopDownGame.Enemies.Bosses;
using TopDownGame.Enemies;
using TopDownGame.Enemies.Progression;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    public sealed class GameplayHUD : MonoBehaviour
    {
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider manaBar;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text mapText;
        [SerializeField] private TMP_Text enemyCountText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Slider experienceBar;
        [SerializeField] private TMP_Text levelUpText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text controlsText;
        [SerializeField] private TMP_Text[] skillTexts = new TMP_Text[4];
        [SerializeField] private Image[] skillBoxes = new Image[4];
        [SerializeField] private Image[] skillIconImages = new Image[4];
        [SerializeField] private Image[] skillCooldownFills = new Image[4];
        [SerializeField] private Slider bossBar;
        [SerializeField] private TMP_Text bossText;
        [SerializeField] private TMP_Text huntNoticeText;

        private PlayerHealth health;
        private PlayerStats stats;
        private PlayerInteraction interaction;
        private PlayerCombat combat;
        private SkillManager skills;
        private EnemyHealth bossHealth;
        private Coroutine levelUpRoutine;
        private Coroutine huntNoticeRoutine;
        private bool displayedAutoAttackState;
        private bool hasDisplayedAutoAttackState;
        private float nextHuntHudRefresh;

        private static readonly string[] SkillKeyLabels = { "Q", "E", "R", "F" };

        private void Start()
        {
            if (GetComponent<PotentialUpgradeUI>() == null)
            {
                gameObject.AddComponent<PotentialUpgradeUI>();
            }
            if (GetComponent<EquipmentEnhancementUI>() == null)
            {
                gameObject.AddComponent<EquipmentEnhancementUI>();
            }

            HuntRewardEvents.RewardGranted += ShowHuntReward;
            HuntRewardEvents.EliteSpawned += ShowEliteSpawned;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                health = player.GetComponent<PlayerHealth>();
                stats = player.GetComponent<PlayerStats>();
                interaction = player.GetComponent<PlayerInteraction>();
                combat = player.GetComponent<PlayerCombat>();
                skills = player.GetComponent<SkillManager>();
                health.HealthChanged += SetHealth;
                stats.ManaChanged += SetMana;
                stats.GoldChanged += SetGold;
                stats.ProgressionChanged += SetProgression;
                stats.LevelChanged += ShowLevelUp;
            }

            EnsureLabels();
            RefreshAutoAttackHint(true);
            DisableDisplayRaycasts();
            SetHealth(health != null ? health.CurrentHealth : 100f,
                stats != null ? stats.MaxHealth : 100f);
            SetMana(stats != null ? stats.CurrentMana : 100f,
                stats != null ? stats.MaxMana : 100f);
            SetGold(stats != null ? stats.Gold : 0);
            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.HunterEssenceChanged += SetHunterEssence;
            }
            SetProgression(stats != null ? stats.Level : 1,
                stats != null ? stats.Experience : 0,
                stats != null ? stats.ExperienceToNextLevel : 100);

            ForestGuardianBoss boss = FindAnyObjectByType<ForestGuardianBoss>();
            if (boss != null)
            {
                bossHealth = boss.GetComponent<EnemyHealth>();
                bossHealth.HealthChanged += SetBossHealth;
                SetBossHealth(bossHealth.CurrentHealth, bossHealth.MaxHealth);
                if (bossText != null) bossText.gameObject.SetActive(true);
                if (bossBar != null) bossBar.gameObject.SetActive(true);
            }
            else
            {
                if (bossText != null) bossText.gameObject.SetActive(false);
                if (bossBar != null) bossBar.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            RefreshAutoAttackHint(false);

            if (MapManager.Instance != null && Time.unscaledTime >= nextHuntHudRefresh)
            {
                nextHuntHudRefresh = Time.unscaledTime + 0.2f;
                int active = MapManager.Instance.RemainingEnemies;
                int target = MapManager.Instance.TargetEnemyCount;
                enemyCountText.text = "Lên cấp / Nhặt đồ   Quái: " + active +
                    " / " + target;
                MapData data = MapManager.Instance.Data;
                mapText.text = data != null
                    ? data.DisplayName + "   [" + data.RecommendedLevelLabel + "]"
                    : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }

            promptText.text = interaction != null ? interaction.CurrentPrompt : string.Empty;

            if (skills != null && skills.Inventory != null)
            {
                for (int i = 0; i < 4 && i < skillTexts.Length; i++)
                {
                    float remaining = skills.GetCooldownRemaining(i);
                    float duration = skills.GetCooldownDuration(i);
                    SkillData data = skills.GetSkillData(i);

                    // Chỉ hiển thị phím Q/E/R/F
                    skillTexts[i].text = SkillKeyLabels[i];

                    if (skillIconImages != null && i < skillIconImages.Length &&
                        skillIconImages[i] != null)
                    {
                        Sprite icon = data != null ? data.Icon : null;
                        if (skillIconImages[i].sprite != icon)
                        {
                            skillIconImages[i].sprite = icon;
                        }

                        skillIconImages[i].enabled = icon != null;
                    }

                    // Cập nhật radial cooldown fill
                    if (skillCooldownFills != null && i < skillCooldownFills.Length &&
                        skillCooldownFills[i] != null)
                    {
                        skillCooldownFills[i].fillAmount = duration > 0f
                            ? Mathf.Clamp01(remaining / duration)
                            : 0f;
                    }

                    // Cập nhật màu border theo skill color
                    if (skillBoxes != null && i < skillBoxes.Length && skillBoxes[i] != null)
                    {
                        if (data != null)
                        {
                            Color c = data.EffectColor;
                            // Border sáng và đẹp theo màu skill
                            skillBoxes[i].color = new Color(
                                Mathf.Clamp01(c.r * 0.8f + 0.1f),
                                Mathf.Clamp01(c.g * 0.8f + 0.1f),
                                Mathf.Clamp01(c.b * 0.8f + 0.1f), 1f);
                        }
                        else
                        {
                            skillBoxes[i].color = new Color(0.25f, 0.32f, 0.55f, 1f);
                        }
                    }
                }
            }
        }

        private void RefreshAutoAttackHint(bool force)
        {
            if (controlsText == null || combat == null)
            {
                return;
            }

            bool enabled = combat.AutoAttackEnabled;
            if (!force && hasDisplayedAutoAttackState &&
                displayedAutoAttackState == enabled)
            {
                return;
            }

            displayedAutoAttackState = enabled;
            hasDisplayedAutoAttackState = true;
            controlsText.text =
                "WASD / Chuột trái: Di chuyển   Chuột phải / J: Đánh   " +
                "T: Auto khi dừng " + (enabled ? "BẬT" : "TẮT") +
                "   Q E R F: Skills";
            controlsText.color = enabled
                ? new Color(0.5f, 1f, 0.65f)
                : new Color(0.8f, 0.9f, 1f);
        }

        private void SetHealth(float current, float maximum)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = maximum;
                healthBar.value = current;
            }
        }

        private void SetMana(float current, float maximum)
        {
            if (manaBar != null)
            {
                manaBar.maxValue = maximum;
                manaBar.value = current;
            }
        }

        private void SetGold(int amount)
        {
            if (goldText != null)
            {
                int essence = GameProgressManager.Instance != null
                    ? GameProgressManager.Instance.HunterEssence
                    : 0;
                goldText.text = "Gold: " + amount + "   Tinh hoa: " + essence;
            }
        }

        private void SetHunterEssence(int amount)
        {
            SetGold(stats != null ? stats.Gold : 0);
        }

        private void SetProgression(int level, int currentExperience, int requiredExperience)
        {
            if (levelText != null)
            {
                levelText.text = "Level " + level + "   XP " + currentExperience +
                    " / " + requiredExperience;
            }

            if (experienceBar != null)
            {
                experienceBar.maxValue = Mathf.Max(1, requiredExperience);
                experienceBar.value = currentExperience;
            }
        }

        private void ShowLevelUp(int newLevel)
        {
            if (levelUpText == null)
            {
                return;
            }

            if (levelUpRoutine != null)
            {
                StopCoroutine(levelUpRoutine);
            }

            levelUpRoutine = StartCoroutine(LevelUpRoutine(newLevel));
        }

        private void ShowHuntReward(HuntRewardNotice notice)
        {
            string message = notice.Experience > 0 || notice.Gold > 0
                ? "+" + notice.Experience + " XP   +" + notice.Gold + " Gold"
                : "Đã nhặt chiến lợi phẩm";
            Color color = new Color(0.72f, 1f, 0.62f);
            if (notice.Loot != null)
            {
                message += "\n" + notice.Loot.RarityLabel + ": " +
                    notice.Loot.DisplayName;
                color = notice.Loot.RarityColor;
            }
            if (notice.HunterEssence > 0 && notice.SalvagedLoot != null)
            {
                message += "\nPhân rã " + notice.SalvagedLoot.DisplayName +
                    "   +" + notice.HunterEssence + " Tinh hoa";
                color = new Color(0.48f, 0.9f, 1f);
            }
            ShowHuntNotice(message, color,
                notice.Loot != null || notice.HunterEssence > 0 ? 3.2f : 1.6f);
        }

        private void ShowEliteSpawned(EnemyRuntimeProfile profile)
        {
            ShowHuntNotice("ELITE APPEARED   Lv. " + profile.Level + " " +
                profile.DisplayName, new Color(1f, 0.64f, 0.15f), 3f);
        }

        private void ShowHuntNotice(string message, Color color, float duration)
        {
            if (huntNoticeText == null) return;
            if (huntNoticeRoutine != null) StopCoroutine(huntNoticeRoutine);
            huntNoticeRoutine = StartCoroutine(HuntNoticeRoutine(message,
                color, duration));
        }

        private IEnumerator HuntNoticeRoutine(string message, Color color,
            float duration)
        {
            huntNoticeText.text = message;
            huntNoticeText.color = color;
            huntNoticeText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(duration);
            huntNoticeText.gameObject.SetActive(false);
            huntNoticeRoutine = null;
        }

        private IEnumerator LevelUpRoutine(int newLevel)
        {
            levelUpText.text = "LEVEL UP!   " + newLevel;
            levelUpText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2.25f);
            levelUpText.gameObject.SetActive(false);
            levelUpRoutine = null;
        }

        private void EnsureLabels()
        {
            if (skillTexts == null || skillTexts.Length != 4)
            {
                skillTexts = new TMP_Text[4];
            }
            if (skillBoxes == null || skillBoxes.Length != 4)
            {
                skillBoxes = new Image[4];
            }
            if (skillIconImages == null || skillIconImages.Length != 4)
            {
                skillIconImages = new Image[4];
            }
            if (skillCooldownFills == null || skillCooldownFills.Length != 4)
            {
                skillCooldownFills = new Image[4];
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            if (goldText == null) goldText = CreateText(canvas.transform, "Gold", new Vector2(15f, -15f));
            if (mapText == null) mapText = CreateText(canvas.transform, "Map", new Vector2(15f, -45f));
            if (enemyCountText == null) enemyCountText = CreateText(canvas.transform, "Enemies", new Vector2(15f, -75f));
            enemyCountText.rectTransform.sizeDelta = new Vector2(480f, 40f);
            if (levelText == null)
            {
                levelText = CreateText(canvas.transform, "Level", new Vector2(15f, -105f));
                levelText.color = new Color(1f, 0.85f, 0.3f);
            }
            if (experienceBar == null) experienceBar = CreateSlider(canvas.transform,
                "ExperienceBar", new Vector2(15f, -135f), new Color(1f, 0.72f, 0.15f));
            if (levelUpText == null)
            {
                levelUpText = CreateText(canvas.transform, "LevelUp", new Vector2(0f, -145f));
                RectTransform levelUpRect = levelUpText.rectTransform;
                levelUpRect.anchorMin = new Vector2(0.5f, 1f);
                levelUpRect.anchorMax = new Vector2(0.5f, 1f);
                levelUpRect.pivot = new Vector2(0.5f, 1f);
                levelUpRect.sizeDelta = new Vector2(700f, 70f);
                levelUpText.alignment = TextAlignmentOptions.Center;
                levelUpText.fontSize = 42f;
                levelUpText.color = new Color(1f, 0.82f, 0.2f);
                levelUpText.outlineWidth = 0.2f;
                levelUpText.outlineColor = Color.black;
                levelUpText.gameObject.SetActive(false);
            }
            if (huntNoticeText == null)
            {
                huntNoticeText = CreateText(canvas.transform, "HuntNotice",
                    new Vector2(0f, -210f));
                RectTransform noticeRect = huntNoticeText.rectTransform;
                noticeRect.anchorMin = new Vector2(0.5f, 1f);
                noticeRect.anchorMax = new Vector2(0.5f, 1f);
                noticeRect.pivot = new Vector2(0.5f, 1f);
                noticeRect.sizeDelta = new Vector2(720f, 90f);
                huntNoticeText.alignment = TextAlignmentOptions.Center;
                huntNoticeText.fontSize = 25f;
                huntNoticeText.outlineWidth = 0.18f;
                huntNoticeText.outlineColor = Color.black;
                huntNoticeText.gameObject.SetActive(false);
            }
            if (promptText == null) promptText = CreateText(canvas.transform, "Prompt", new Vector2(15f, 20f));
            if (controlsText == null)
            {
                controlsText = CreateText(canvas.transform, "Controls", new Vector2(15f, -225f));
                controlsText.text = "WASD / Chuột trái: Di chuyển   Chuột phải / J: Đánh   Q E R F: Kỹ năng";
                controlsText.fontSize = 14f;
                controlsText.color = new Color(0.8f, 0.9f, 1f);
            }
            if (healthBar == null) healthBar = CreateSlider(canvas.transform, "HealthBar", new Vector2(15f, -165f), Color.red);
            if (manaBar == null) manaBar = CreateSlider(canvas.transform, "ManaBar", new Vector2(15f, -195f), Color.cyan);
            if (bossText == null)
            {
                bossText = CreateText(canvas.transform, "BossName", new Vector2(430f, -15f));
                bossText.text = "FOREST GUARDIAN";
            }
            if (bossBar == null) bossBar = CreateSlider(canvas.transform, "BossHealth", new Vector2(430f, -50f), new Color(0.75f, 0.1f, 0.15f));

            for (int i = 0; i < skillTexts.Length; i++)
            {
                if (skillTexts[i] == null)
                {
                    skillTexts[i] = CreateSkillBox(canvas.transform, "Skill" + i,
                        new Vector2(15f + i * 295f, 15f), out skillBoxes[i],
                        out skillIconImages[i], out skillCooldownFills[i]);
                }
                else
                {
                    // Áp dụng lại kích thước cho ô đã tồn tại trong scene
                    Transform boxTransform = skillTexts[i].transform.parent;
                    if (boxTransform != null)
                    {
                        RectTransform boxRect = boxTransform.GetComponent<RectTransform>();
                        if (boxRect != null)
                        {
                            boxRect.sizeDelta = new Vector2(280f, 280f);
                            boxRect.anchoredPosition = new Vector2(15f + i * 295f, 15f);
                        }

                        if (skillIconImages[i] == null)
                        {
                            Transform iconTransform = boxTransform.Find("Icon");
                            if (iconTransform != null)
                            {
                                skillIconImages[i] = iconTransform.GetComponent<Image>();
                            }
                        }
                    }
                    // Cập nhật font cho label đã có
                    skillTexts[i].enableAutoSizing = false;
                    skillTexts[i].fontSize = 40f;
                    skillTexts[i].fontSizeMin = 40f;
                    skillTexts[i].fontSizeMax = 40f;
                    skillTexts[i].overflowMode = TextOverflowModes.Overflow;
                    skillTexts[i].color = Color.white;
                }
            }
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 position)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(240f, 40f);
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 18f;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static TMP_Text CreateSkillBox(Transform parent, string name, Vector2 position,
            out Image backgroundImage, out Image skillIcon, out Image cooldownFill)
        {
            // === Lớp ngoài: Border màu skill ===
            GameObject box = new GameObject(name + "Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = Vector2.zero;
            boxRect.anchorMax = Vector2.zero;
            boxRect.pivot = Vector2.zero;
            boxRect.anchoredPosition = position;
            boxRect.sizeDelta = new Vector2(280f, 280f);
            backgroundImage = box.GetComponent<Image>();
            backgroundImage.color = new Color(0.25f, 0.32f, 0.55f, 1f); // border xanh mặc định
            backgroundImage.raycastTarget = false;

            // === Lớp trong: nền tối tạo hiệu ứng border ===
            GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(box.transform, false);
            RectTransform innerRect = inner.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(5f, 5f);
            innerRect.offsetMax = new Vector2(-5f, -5f);
            inner.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.10f, 0.98f);
            inner.GetComponent<Image>().raycastTarget = false;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(box.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(20f, 20f);
            iconRect.offsetMax = new Vector2(-20f, -20f);
            skillIcon = iconObject.GetComponent<Image>();
            skillIcon.preserveAspect = true;
            skillIcon.color = Color.white;
            skillIcon.raycastTarget = false;

            // === Lớp glow: ánh sáng mờ phía trên ===
            GameObject glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(box.transform, false);
            RectTransform glowRect = glow.GetComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0f, 0.55f);
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(6f, 0f);
            glowRect.offsetMax = new Vector2(-6f, -6f);
            glow.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
            glow.GetComponent<Image>().raycastTarget = false;

            // === Cooldown radial fill (đè lên tất cả) ===
            GameObject overlay = new GameObject("Cooldown", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(box.transform, false);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            cooldownFill = overlay.GetComponent<Image>();
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = 2;
            cooldownFill.fillClockwise = false;
            cooldownFill.color = new Color(0f, 0f, 0f, 0.76f);
            cooldownFill.raycastTarget = false;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(box.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(12f, -8f);
            labelRect.sizeDelta = new Vector2(64f, 58f);
            TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.TopLeft;
            label.enableAutoSizing = false;
            label.fontSize = 40f;
            label.fontSizeMin = 40f;
            label.fontSizeMax = 40f;
            label.overflowMode = TextOverflowModes.Overflow;
            label.color = Color.white;
            label.outlineWidth = 0.22f;
            label.outlineColor = Color.black;
            label.raycastTarget = false;
            return label;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 position, Color fillColor)
        {
            GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(240f, 18f);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;
            slider.interactable = false;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
            backgroundImage.raycastTarget = false;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(sliderObject.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            slider.fillRect = fillRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;
            return slider;
        }

        private void DisableDisplayRaycasts()
        {
            DisableRaycast(goldText);
            DisableRaycast(mapText);
            DisableRaycast(enemyCountText);
            DisableRaycast(levelText);
            DisableRaycast(promptText);
            DisableRaycast(controlsText);
            DisableRaycast(bossText);
            DisableRaycast(huntNoticeText);

            if (skillTexts != null)
            {
                foreach (TMP_Text text in skillTexts)
                {
                    DisableRaycast(text);
                }
            }

            DisableRaycasts(skillBoxes);
            DisableRaycasts(skillIconImages);
            DisableRaycasts(skillCooldownFills);
            DisableSliderRaycasts(healthBar);
            DisableSliderRaycasts(manaBar);
            DisableSliderRaycasts(experienceBar);
            DisableSliderRaycasts(bossBar);
        }

        private void OnDestroy()
        {
            HuntRewardEvents.RewardGranted -= ShowHuntReward;
            HuntRewardEvents.EliteSpawned -= ShowEliteSpawned;
            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.HunterEssenceChanged -= SetHunterEssence;
            }
            if (health != null)
            {
                health.HealthChanged -= SetHealth;
            }

            if (stats != null)
            {
                stats.ManaChanged -= SetMana;
                stats.GoldChanged -= SetGold;
                stats.ProgressionChanged -= SetProgression;
                stats.LevelChanged -= ShowLevelUp;
            }

            if (bossHealth != null)
            {
                bossHealth.HealthChanged -= SetBossHealth;
            }
        }

        private static void DisableRaycast(Graphic graphic)
        {
            if (graphic != null)
            {
                graphic.raycastTarget = false;
            }
        }

        private static void DisableRaycasts(Image[] images)
        {
            if (images == null)
            {
                return;
            }

            foreach (Image image in images)
            {
                DisableRaycast(image);
            }
        }

        private static void DisableSliderRaycasts(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.interactable = false;
            foreach (Graphic graphic in slider.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        private void SetBossHealth(float current, float maximum)
        {
            if (bossBar == null)
            {
                return;
            }

            bossBar.maxValue = maximum;
            bossBar.value = current;
        }
    }
}
