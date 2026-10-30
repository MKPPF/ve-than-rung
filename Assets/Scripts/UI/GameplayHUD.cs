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
    /// <summary>Keeps combat resources and actionable skill states readable without covering the encounter space.</summary>
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

        private SkillManager skills;
        private EnemyHealth bossHealth;
        private Coroutine levelUpRoutine;
        private Coroutine huntNoticeRoutine;


        private float nextHuntHudRefresh;
        private TMP_Text healthValue, manaValue, recommendation;
        private readonly TMP_Text[] skillNames = new TMP_Text[4];
        private readonly TMP_Text[] skillStates = new TMP_Text[4];
        private GameObject displayRoot;
        private GameObject promptBackdrop;

        private static readonly string[] SkillKeyLabels = { "Q", "E", "R", "F" };

        private void Start()
        {
            PlayerAutomationUI.Ensure(gameObject);
            HeroSelectionUI.Ensure(GetComponentInParent<Canvas>());
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

                skills = player.GetComponent<SkillManager>();
                health.HealthChanged += SetHealth;
                stats.ManaChanged += SetMana;
                stats.GoldChanged += SetGold;
                stats.ProgressionChanged += SetProgression;
                stats.LevelChanged += ShowLevelUp;
            }

            EnsureLabels();

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
                if (bossText != null) bossText.transform.parent.gameObject.SetActive(true);
                if (bossBar != null) bossBar.gameObject.SetActive(true);
            }
            else
            {
                if (bossText != null) bossText.transform.parent.gameObject.SetActive(false);
                if (bossBar != null) bossBar.gameObject.SetActive(false);
            }
        }

        private void Update()
        {


            if (MapManager.Instance != null && Time.unscaledTime >= nextHuntHudRefresh)
            {
                nextHuntHudRefresh = Time.unscaledTime + 0.2f;
                int active = MapManager.Instance.RemainingEnemies;
                int target = MapManager.Instance.TargetEnemyCount;
                enemyCountText.text = "Quái đang có: " + active + " / " + target;
                MapData data = MapManager.Instance.Data;
                mapText.text = data != null
                    ? data.DisplayName
                    : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                recommendation.text = data != null ? "Cấp đề nghị: " + data.RecommendedLevelLabel.Replace("Lv. ", "") : string.Empty;
            }

            promptText.text = interaction != null ? interaction.CurrentPrompt : string.Empty;
            if (promptBackdrop != null) promptBackdrop.SetActive(!string.IsNullOrWhiteSpace(promptText.text));

            if (skills != null && skills.Inventory != null)
            {
                EnsureSkillLabelsResolved();
                for (int i = 0; i < 4 && i < skillTexts.Length; i++)
                {
                    float remaining = skills.GetCooldownRemaining(i);
                    float duration = skills.GetCooldownDuration(i);
                    SkillData data = skills.GetSkillData(i);

                    if (skillTexts[i] != null)
                    {
                        skillTexts[i].text = SkillKeyLabels[i];
                    }

                    if (skillNames != null && i < skillNames.Length && skillNames[i] != null)
                    {
                        skillNames[i].text = data != null ? data.DisplayName : "Ô trống";
                    }

                    bool dead = health != null && !health.IsAlive;
                    bool insufficientMana = data != null && stats != null && stats.CurrentMana < data.ManaCost;

                    if (skillStates != null && i < skillStates.Length && skillStates[i] != null)
                    {
                        skillStates[i].text = data == null ? "K: TRANG BỊ" : dead ? "ĐÃ GỤC" : remaining > 0f ?
                            "HỒI " + remaining.ToString("0.0") + " s" : insufficientMana ? "THIẾU MANA" : "SẴN SÀNG";
                        skillStates[i].color = data == null ? FantasyEquipmentUI.Muted : dead ? new Color(1f,0.65f,0.56f) :
                            remaining > 0f ? FantasyEquipmentUI.Gold : insufficientMana ? new Color(1f,0.65f,0.56f) : FantasyEquipmentUI.Green;
                    }

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

        private void SetHealth(float current, float maximum)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = maximum;
                healthBar.value = current;
                if (healthValue == null)
                {
                    healthValue = healthBar.transform.Find("Value")?.GetComponent<TMP_Text>();
                }
            }
            if (healthValue != null) healthValue.text = "MÁU  " + Mathf.CeilToInt(current).ToString("N0") + " / " + Mathf.CeilToInt(maximum).ToString("N0");
        }

        private void SetMana(float current, float maximum)
        {
            if (manaBar != null)
            {
                manaBar.maxValue = maximum;
                manaBar.value = current;
                if (manaValue == null)
                {
                    manaValue = manaBar.transform.Find("Value")?.GetComponent<TMP_Text>();
                }
            }
            if (manaValue != null) manaValue.text = "MANA  " + Mathf.FloorToInt(current).ToString("N0") + " / " + Mathf.FloorToInt(maximum).ToString("N0");
        }

        private void SetGold(int amount)
        {
            if (goldText != null)
            {
                int essence = GameProgressManager.Instance != null
                    ? GameProgressManager.Instance.HunterEssence
                    : 0;
                goldText.text = amount.ToString("N0") + " vàng   •   " + essence.ToString("N0") + " tinh hoa";
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
                levelText.text = "Cấp " + level + "   •   XP " + currentExperience.ToString("N0") +
                    " / " + requiredExperience.ToString("N0");
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
                ? "+" + notice.Experience + " XP   +" + notice.Gold + " vàng"
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
            ShowHuntNotice("TINH ANH XUẤT HIỆN   Cấp " + profile.Level + " " +
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
            levelUpText.text = "LÊN CẤP " + newLevel;
            levelUpText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2.25f);
            levelUpText.gameObject.SetActive(false);
            levelUpRoutine = null;
        }

        private void EnsureSkillLabelsResolved()
        {
            if (skillNames != null && skillNames[0] != null && skillStates != null && skillStates[0] != null)
            {
                return;
            }

            if (skillBoxes == null)
            {
                return;
            }

            for (int i = 0; i < 4 && i < skillBoxes.Length; i++)
            {
                if (skillBoxes[i] == null)
                {
                    continue;
                }

                Transform slot = skillBoxes[i].transform.parent;
                if (slot == null)
                {
                    continue;
                }

                if (skillNames != null && i < skillNames.Length && skillNames[i] == null)
                {
                    Transform tName = slot.Find("Name");
                    if (tName != null)
                    {
                        skillNames[i] = tName.GetComponent<TMP_Text>();
                    }
                }

                if (skillStates != null && i < skillStates.Length && skillStates[i] == null)
                {
                    Transform tState = slot.Find("State");
                    if (tState != null)
                    {
                        skillStates[i] = tState.GetComponent<TMP_Text>();
                    }
                }

                if (skillTexts != null && i < skillTexts.Length && skillTexts[i] == null)
                {
                    Transform tLabel = slot.Find("Label");
                    if (tLabel != null)
                    {
                        skillTexts[i] = tLabel.GetComponent<TMP_Text>();
                    }
                }
            }
        }

        private void EnsureLabels()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            RectTransform root = FantasyEquipmentUI.Rect(canvas.transform, "CombatHUDRoot", 0, 0, 0, 0);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            root.SetAsFirstSibling();
            displayRoot = root.gameObject;

            // Nút Menu / Tạm dừng ký hiệu mp3 (❚❚) ở góc trên cùng bên trái màn hình
            Transform pauseFrame = FantasyEquipmentUI.Frame(root, "TopLeftPauseButton", 22, 22, 56, 56, FantasyEquipmentUI.Ink);
            Button pauseBtn = pauseFrame.gameObject.AddComponent<Button>();
            Image pauseFace = pauseFrame.Find("Inset").GetComponent<Image>();
            pauseFrame.GetComponent<Image>().raycastTarget = true;
            pauseBtn.targetGraphic = pauseFace;
            EquipmentPanelLayout.StyleButton(pauseBtn);
            pauseBtn.onClick.AddListener(() =>
            {
                PauseMenu pauseMenu = Object.FindAnyObjectByType<PauseMenu>();
                if (pauseMenu != null) pauseMenu.Toggle();
            });
            TMP_Text pauseIcon = FantasyEquipmentUI.Text(pauseFrame, "PauseSymbol", "||", 0, 6, 56, 44, 26, FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            pauseIcon.fontStyle = FontStyles.Bold;

            Transform card = FantasyEquipmentUI.Frame(root, "PlayerStatus", 86, 22, 340, 280, FantasyEquipmentUI.Ink);
            mapText = Label(card, "Map", "", 14, 10, 312, 48, 24, FantasyEquipmentUI.Gold);
            recommendation = Label(card, "RecommendedLevel", "", 14, 60, 312, 24, 20, FantasyEquipmentUI.Muted);
            enemyCountText = Label(card, "Enemies", "", 14, 87, 312, 26, 21);
            healthBar = ResourceBar(card, "HealthBar", 14, 120, 312, 28, new Color(0.64f,0.17f,0.21f));
            healthValue = Label(healthBar.transform, "Value", "", 8, 0, 296, 28, 21);
            manaBar = ResourceBar(card, "ManaBar", 14, 154, 312, 28, new Color(0.12f,0.38f,0.57f));
            manaValue = Label(manaBar.transform, "Value", "", 8, 0, 296, 28, 21);
            levelText = Label(card, "Level", "", 14, 190, 312, 26, 21, FantasyEquipmentUI.Gold);
            experienceBar = ResourceBar(card, "ExperienceBar", 14, 221, 312, 6, FantasyEquipmentUI.Gold);
            goldText = Label(card, "Gold", "", 14, 236, 312, 28, 21);

            Transform hints = FantasyEquipmentUI.Frame(root, "ControlHints", 86, 310, 340, 96, FantasyEquipmentUI.Ink);
            controlsText = Label(hints, "Controls", "WASD / Chuột trái: Di chuyển\nJ / Chuột phải: Đánh  •  E: Tương tác\nI: Túi  •  K: Kỹ năng  •  H: Nhân vật  •  Esc: Tạm dừng", 12, 8, 316, 80, 18);

            Transform pauseCard = FantasyEquipmentUI.Frame(root, "HUDPauseCard", 22, 530, 400, 48, FantasyEquipmentUI.Ink);
            Button pauseButton = FantasyEquipmentUI.Button(pauseCard, "HUDPauseButton", "|| TẠM DỪNG & LƯU TIẾN TRÌNH", 4, 4, 392, 40, () =>
            {
                PauseMenu pause = Object.FindAnyObjectByType<PauseMenu>();
                if (pause != null) pause.Toggle();
            });
            TMP_Text pauseBtnText = pauseButton.GetComponentInChildren<TMP_Text>();
            if (pauseBtnText != null)
            {
                pauseBtnText.fontSize = 17f;
                pauseBtnText.color = FantasyEquipmentUI.Gold;
            }
            Transform dodgeCard = canvas.transform.Find("DodgeCard");
            if (dodgeCard != null) ((RectTransform)dodgeCard).anchoredPosition = new Vector2(22,-310);

            RectTransform strip = FantasyEquipmentUI.Rect(root, "SkillStrip", 0, 0, 596, 180);
            strip.anchorMin = strip.anchorMax = strip.pivot = new Vector2(0.5f,0);
            strip.anchoredPosition = new Vector2(0,16);
            skillTexts = new TMP_Text[4];
            skillBoxes = new Image[4];
            skillIconImages = new Image[4];
            skillCooldownFills = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                Transform slot = FantasyEquipmentUI.Frame(strip, "Skill" + i + "Box", i * 152, 0, 140, 180, FantasyEquipmentUI.Ink);
                skillBoxes[i] = slot.Find("Brass").GetComponent<Image>();
                skillIconImages[i] = FantasyEquipmentUI.Icon(slot, "Icon", null, 30, 8, 80, 80);
                skillCooldownFills[i] = FantasyEquipmentUI.Box(slot, "Cooldown", 30, 8, 80, 80, new Color(0,0,0,0.72f));
                skillCooldownFills[i].sprite = TopDownGame.Core.RuntimeSpriteFactory.WhiteSprite;
                skillCooldownFills[i].type = Image.Type.Filled;
                skillCooldownFills[i].fillMethod = Image.FillMethod.Radial360;
                skillCooldownFills[i].fillOrigin = 2;
                skillCooldownFills[i].fillClockwise = false;
                FantasyEquipmentUI.Box(slot, "KeyBadge", 7, 7, 30, 30, FantasyEquipmentUI.Ink);
                skillTexts[i] = Label(slot, "Label", SkillKeyLabels[i], 7, 7, 30, 30, 25, FantasyEquipmentUI.TextColor, TextAlignmentOptions.Center);
                skillNames[i] = Label(slot, "Name", "", 8, 90, 124, 56, 21, FantasyEquipmentUI.TextColor, TextAlignmentOptions.Center);
                skillStates[i] = Label(slot, "State", "", 6, 151, 128, 23, 20, FantasyEquipmentUI.Green, TextAlignmentOptions.Center);
            }

            Transform boss = FantasyEquipmentUI.Frame(root, "BossStatus", 0, 0, 500, 78, FantasyEquipmentUI.Ink);
            RectTransform bossRect = (RectTransform)boss;
            bossRect.anchorMin = bossRect.anchorMax = bossRect.pivot = new Vector2(0.5f,1);
            bossRect.anchoredPosition = new Vector2(0,-22);
            bossText = Label(boss, "BossName", "VỆ THẦN RỪNG", 16, 8, 468, 28, 23, FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            bossBar = ResourceBar(boss, "BossHealth", 16, 42, 468, 24, new Color(0.64f,0.17f,0.21f));
            Label(bossBar.transform, "Value", "", 0, 0, 468, 24, 20, FantasyEquipmentUI.TextColor, TextAlignmentOptions.Center);
            Transform hint = FantasyEquipmentUI.Frame(root, "InteractionHint", 0, 0, 720, 76, FantasyEquipmentUI.Ink);
            RectTransform hintRect = (RectTransform)hint;
            hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = new Vector2(0.5f, 0);
            hintRect.anchoredPosition = new Vector2(0,198);
            promptBackdrop = hint.gameObject;
            promptBackdrop.SetActive(false);
            promptText = FloatingLabel(root, "Prompt", new Vector2(0.5f,0), new Vector2(0,206), 680, 60, 23);
            levelUpText = FloatingLabel(root, "LevelUp", new Vector2(0.5f,1), new Vector2(0,-116), 700, 50, 32);
            levelUpText.color = FantasyEquipmentUI.Gold;
            levelUpText.gameObject.SetActive(false);
            huntNoticeText = FloatingLabel(root, "HuntNotice", new Vector2(0.5f,1), new Vector2(0,-178), 660, 88, 23);
            huntNoticeText.gameObject.SetActive(false);
        }

        private static TMP_Text Label(Transform parent, string name, string value, float x, float y, float width, float height,
            float size, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            TMP_Text text = FantasyEquipmentUI.Text(parent,name,value,x,y,width,height,size,color,alignment);
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }

        private static TMP_Text FloatingLabel(Transform parent, string name, Vector2 anchor, Vector2 position, float width, float height, float size)
        {
            TMP_Text text = Label(parent,name,"",0,0,width,height,size,Color.white,TextAlignmentOptions.Center);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            text.outlineWidth = 0.2f;
            text.outlineColor = Color.black;
            return text;
        }

        private static Slider ResourceBar(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = FantasyEquipmentUI.Rect(parent,name,x,y,width,height);
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.interactable = false;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.transition = Selectable.Transition.None;
            FantasyEquipmentUI.Box(rect,"Background",0,0,width,height,new Color(0.03f,0.04f,0.06f,1));
            Image fill = FantasyEquipmentUI.Box(rect,"Fill",0,0,width,height,color);
            // Slider drives the anchors; the fill must start with zero offsets.
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            slider.fillRect = fill.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
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
            if (displayRoot != null) Destroy(displayRoot);
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
            bossBar.GetComponentInChildren<TMP_Text>().text = Mathf.CeilToInt(current).ToString("N0") + " / " + Mathf.CeilToInt(maximum).ToString("N0");
        }
    }
}
