using TMPro;
using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using F = TopDownGame.UI.FantasyEquipmentUI;

namespace TopDownGame.UI
{
    /// <summary>
    /// Modal dialog allowing the player to preview and switch between playable heroes:
    /// Forest Warrior (default), Thanh Giong, and Son Tinh.
    /// Can be opened from the Lobby Hero Shrine (E / double-click) or with the H shortcut.
    /// </summary>
    public sealed class HeroSelectionUI : MonoBehaviour
    {
        private static readonly string[] SonTinhSlotLabels =
        {
            "Q: Ném Đại Thạch",
            "E: Dâng Núi Địch",
            "R: Dãy Núi Tiến",
            "F: Núi Quanh Mình"
        };

        private static readonly string[] ThanhGiongSlotLabels =
        {
            "Q: Kiếm + Ngựa Sắt",
            "E: 1 Cây Tre Quật",
            "R: Cưỡi Ngựa Quật 360°",
            "F: Càn Lướt Ngựa + Tre"
        };

        private static readonly string[] ForestWarriorSlotLabels =
        {
            "Q: Cầu Lửa",
            "E: Lướt Băng",
            "R: Kiếm Luân",
            "F: Thiên Lôi"
        };

        private GameObject panel;
        private ThanhGiongHeroController heroController;
        private HeroCharacterId previewHero = HeroCharacterId.SonTinh;
        private Button sonTinhCardButton;
        private Button thanhGiongCardButton;
        private Button forestCardButton;
        private Button confirmButton;
        private Button closeButton;
        private Image heroPortrait;
        private readonly Image[] skillPreviewIcons = new Image[4];
        private readonly TMP_Text[] skillPreviewLabels = new TMP_Text[4];
        private TMP_Text heroHeading;
        private TMP_Text heroSubheading;
        private TMP_Text movesDescription;
        private TMP_Text confirmLabel;
        private TMP_Text statusLabel;
        private readonly MenuModalFocus focus = new MenuModalFocus();

        public bool IsOpen => panel != null && panel.activeSelf;

        public static HeroSelectionUI Ensure(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            HeroSelectionUI existing = canvas.GetComponentInChildren<HeroSelectionUI>(true);
            if (existing != null)
            {
                return existing;
            }

            Transform hud = canvas.transform.Find("HUD");
            GameObject host = hud != null ? hud.gameObject : canvas.gameObject;
            return host.AddComponent<HeroSelectionUI>();
        }

        private void Start()
        {
            ResolveHeroController();
            EnsurePanel();
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.hKey.wasPressedThisFrame)
            {
                if (IsOpen)
                {
                    Close();
                }
                else
                {
                    Open();
                }

                return;
            }

            if (!IsOpen)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open()
        {
            ResolveHeroController();
            EnsurePanel();
            if (!isActiveAndEnabled || IsOpen || panel == null || Time.timeScale <= 0f ||
                GameplayState.IsBlockedByOther(this))
            {
                return;
            }

            previewHero = heroController != null
                ? heroController.CurrentHero
                : HeroCharacterId.ForestWarrior;

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            focus.Capture(panel.transform);
            GameplayState.EnterModal(this);
            RefreshView();

            SetStatus("Đài Anh Hùng: Tất cả nhân vật đang được mở khóa sẵn sàng để chọn và thử nghiệm bộ chiêu thức!");

            if (confirmButton != null && confirmButton.interactable)
            {
                confirmButton.Select();
            }
            else if (sonTinhCardButton != null)
            {
                sonTinhCardButton.Select();
            }
        }

        public void Close()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            GameplayState.ExitModal(this);
            focus.Release();
        }

        public void SelectHeroImmediate(HeroCharacterId hero)
        {
            ResolveHeroController();
            if (heroController == null)
            {
                return;
            }

            if (!ThanhGiongHeroController.IsHeroUnlocked(hero))
            {
                previewHero = hero;
                RefreshView();
                SetStatus("Nhân vật đang bị phong ấn! Cần vượt qua các Map chiến đấu để mở khóa.");
                return;
            }

            previewHero = hero;
            heroController.SelectHero(hero, true);
            RefreshView();

            string statusMsg = hero switch
            {
                HeroCharacterId.SonTinh => "Đã hóa thân thành SƠN TINH! Dùng Chuột phải/J và Q (Ném Đá), E (Dâng Núi), R (Dãy Núi Tiến), F (Núi Vây Quanh).",
                HeroCharacterId.ThanhGiong => "Đã hóa thân thành THÁNH GIÓNG! Sử dụng Chuột phải/J và Q, E, R, F để thi triển 5 tuyệt kỹ.",
                _ => "Đã chuyển về Chiến Binh Rừng (Mặc định)."
            };
            SetStatus(statusMsg);
        }

        private void PreviewHero(HeroCharacterId hero)
        {
            previewHero = hero;
            RefreshView();
        }

        private void ConfirmPreviewedHero()
        {
            SelectHeroImmediate(previewHero);
        }

        private void ResolveHeroController()
        {
            if (heroController != null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                heroController = ThanhGiongHeroController.Ensure(player);
            }
        }

        private void RefreshView()
        {
            if (panel == null)
            {
                return;
            }

            HeroCharacterId activeHero = heroController != null
                ? heroController.CurrentHero
                : HeroCharacterId.ForestWarrior;

            UpdateHeroCard(
                sonTinhCardButton,
                HeroCharacterId.SonTinh,
                activeHero,
                SonTinhVisualFactory.GetHeroPortraitSprite(),
                "Đại Thạch • Dâng Núi • Quần Sơn",
                "Vượt CaveMap01 để mở khóa");

            UpdateHeroCard(
                thanhGiongCardButton,
                HeroCharacterId.ThanhGiong,
                activeHero,
                ThanhGiongVisualFactory.GetHeroPortraitSprite(),
                "Ngựa Sắt • Kiếm Sắt • Tre Ngà",
                "Vượt Map03 & mở BossMap01");

            UpdateHeroCard(
                forestCardButton,
                HeroCharacterId.ForestWarrior,
                activeHero,
                ForestWarriorVisualFactory.GetHeroPortraitSprite(),
                "Kiếm Pháp • Kỹ Năng Tự Do",
                "Nhân vật mặc định");

            bool isPreviewUnlocked = ThanhGiongHeroController.IsHeroUnlocked(previewHero);

            if (previewHero == HeroCharacterId.SonTinh)
            {
                Sprite stPortrait = SonTinhVisualFactory.GetHeroPortraitSprite();
                heroPortrait.sprite = stPortrait;
                heroPortrait.enabled = stPortrait != null;
                heroPortrait.color = isPreviewUnlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.85f);
                heroHeading.text = isPreviewUnlocked
                    ? "SƠN TINH — TẢN VIÊN SƠN THÁNH"
                    : "🔒 SƠN TINH (CHƯA MỞ KHÓA)";
                heroSubheading.text = isPreviewUnlocked
                    ? "Mũ Lông Chim  •  Áo Choàng Đỏ Hồng  •  Thần Lực Dời Non Dựng Núi"
                    : "Vượt qua các Map chiến đấu để mở khóa Tản Viên Sơn Thánh";

                movesDescription.text =
                    "<b>• Chiêu bình thường [Chuột phải / J]:</b> Tung nắm đấm đá xanh công phá kẻ địch phía trước.\n" +
                    "<b>• Chiêu 1 [Phím Q] — Ném Đại Thạch:</b> Nâng <b>cục đá lớn</b> qua đầu rồi ném mạnh vào kẻ địch gây sát thương nổ diện rộng.\n" +
                    "<b>• Chiêu 2 [Phím E] — Dâng Núi Trấn Địch:</b> Dùng <b>2 tay ngửa ra dâng lên cao</b> để dâng lên thành núi ở khu vực có địch gây sát thương quân địch.\n" +
                    "<b>• Chiêu 3 [Phím R] — Đấm Đất Dựng Núi:</b> <b>Đấm lòng đất</b> để tạo các <b>dãy núi liên hoàn tiến về phía trước</b> gây sát thương quân địch.\n" +
                    "<b>• Chiêu 4 [Phím F] — Bát Phương Quần Sơn:</b> Dùng <b>2 tay ngửa ra dâng lên</b> để dâng các <b>dãy núi xung quanh mình</b> khi kẻ địch bao vây.";

                for (int i = 0; i < 4; i++)
                {
                    if (skillPreviewIcons[i] != null)
                    {
                        skillPreviewIcons[i].sprite = SonTinhVisualFactory.GetSkillIcon(i);
                        skillPreviewIcons[i].enabled = true;
                    }

                    if (skillPreviewLabels[i] != null)
                    {
                        skillPreviewLabels[i].text = SonTinhSlotLabels[i];
                    }
                }
            }
            else if (previewHero == HeroCharacterId.ThanhGiong)
            {
                Sprite tgPortrait = ThanhGiongVisualFactory.GetHeroPortraitSprite();
                heroPortrait.sprite = tgPortrait;
                heroPortrait.enabled = tgPortrait != null;
                heroPortrait.color = isPreviewUnlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.85f);
                heroHeading.text = isPreviewUnlocked
                    ? "THÁNH GIÓNG — PHÙ ĐỔNG THIÊN VƯƠNG"
                    : "🔒 THÁNH GIÓNG (CHƯA MỞ KHÓA)";
                heroSubheading.text = isPreviewUnlocked
                    ? "Giáp Vàng  •  Ngựa Sắt Phun Lửa  •  Kiếm Sắt & Bụi Tre Ngà"
                    : "Cần vượt qua các Map và diện kiến Boss Vệ Thần Rừng";

                movesDescription.text =
                    "<b>• Chiêu bình thường [Chuột phải / J]:</b> Dùng <b>Kiếm Sắt</b> chém vòng cung rộng phía trước.\n" +
                    "<b>• Chiêu 1 [Phím Q] — Kiếm Sắt Chiến Mã:</b> Dùng <b>kiếm sắt cùng con ngựa sắt</b> phun lửa chồm lên chém địch.\n" +
                    "<b>• Chiêu 2 [Phím E] — Tre Ngà Quật Địch:</b> Dùng <b>1 cây tre ngà</b> khổng lồ quật mạnh vào kẻ địch phía trước.\n" +
                    "<b>• Chiêu 3 [Phím R] — Thần Mã Quật Tre 360°:</b> Dùng <b>vài cây tre quật xung quanh kẻ địch (360 độ)</b> khi đang cưỡi ngựa.\n" +
                    "<b>• Chiêu 4 [Phím F] — Thiết Mã Càn Lướt:</b> <b>Càn lướt 1 đoạn có kẻ địch</b> với con ngựa sắt và vài cây tre cầm trên tay.";

                for (int i = 0; i < 4; i++)
                {
                    if (skillPreviewIcons[i] != null)
                    {
                        skillPreviewIcons[i].sprite = ThanhGiongVisualFactory.GetSkillIcon(i);
                        skillPreviewIcons[i].enabled = true;
                    }

                    if (skillPreviewLabels[i] != null)
                    {
                        skillPreviewLabels[i].text = ThanhGiongSlotLabels[i];
                    }
                }
            }
            else
            {
                Sprite fwPortrait = ForestWarriorVisualFactory.GetHeroPortraitSprite();
                heroPortrait.sprite = fwPortrait;
                heroPortrait.enabled = fwPortrait != null;
                heroPortrait.color = Color.white;
                heroHeading.text = "CHIẾN BINH RỪNG (MẶC ĐỊNH)";
                heroSubheading.text = "Kiếm Pháp Cơ Bản  •  Kỹ Năng Nguyên Tố Tự Do";
                movesDescription.text =
                    "<b>• Đánh thường [Chuột phải / J]:</b> Chém cận chiến cơ bản.\n" +
                    "<b>• Kỹ năng [Q / E / R / F]:</b> Sử dụng 4 kỹ năng nguyên tố đã trang bị trong bảng Kỹ Năng [K].\n\n" +
                    "Nhân vật cơ bản với khả năng tùy biến trang bị và học 9 kỹ năng nguyên tố khác nhau trong suốt hành trình.";

                for (int i = 0; i < 4; i++)
                {
                    if (skillPreviewIcons[i] != null)
                    {
                        skillPreviewIcons[i].sprite = ForestWarriorVisualFactory.GetSkillIcon(i);
                        skillPreviewIcons[i].enabled = skillPreviewIcons[i].sprite != null;
                    }

                    if (skillPreviewLabels[i] != null)
                    {
                        skillPreviewLabels[i].text = ForestWarriorSlotLabels[i];
                    }
                }
            }

            bool alreadyActive = activeHero == previewHero;
            if (confirmButton != null)
            {
                confirmButton.interactable = isPreviewUnlocked && !alreadyActive;
            }

            if (confirmLabel != null)
            {
                if (!isPreviewUnlocked)
                {
                    confirmLabel.text = "🔒 CHƯA MỞ KHÓA — CẦN VƯỢT QUA CÁC MAP";
                }
                else if (alreadyActive)
                {
                    confirmLabel.text = "ĐANG SỬ DỤNG NHÂN VẬT NÀY";
                }
                else
                {
                    confirmLabel.text = previewHero switch
                    {
                        HeroCharacterId.SonTinh => "CHỌN NHÂN VẬT SƠN TINH",
                        HeroCharacterId.ThanhGiong => "CHỌN NHÂN VẬT THÁNH GIÓNG",
                        _ => "CHUYỂN VỀ CHIẾN BINH RỪNG"
                    };
                }
            }
        }

        private void UpdateHeroCard(Button cardButton, HeroCharacterId heroId, HeroCharacterId activeHero,
            Sprite portrait, string unlockedDesc, string lockedDesc)
        {
            if (cardButton == null)
            {
                return;
            }

            bool unlocked = ThanhGiongHeroController.IsHeroUnlocked(heroId);
            cardButton.transform.Find("Brass").GetComponent<Image>().color =
                previewHero == heroId ? F.Gold : F.Edge;

            TMP_Text stateText = cardButton.transform.Find("State").GetComponent<TMP_Text>();
            TMP_Text descText = cardButton.transform.Find("Desc")?.GetComponent<TMP_Text>();
            if (!unlocked)
            {
                stateText.text = "🔒 CHƯA MỞ KHÓA";
                stateText.color = F.Muted;
                if (descText != null)
                {
                    descText.text = lockedDesc;
                }
            }
            else
            {
                stateText.text = activeHero == heroId ? "ĐANG DÙNG" : "NHẤN ĐỂ CHỌN";
                stateText.color = activeHero == heroId ? F.Gold : F.Green;
                if (descText != null)
                {
                    descText.text = unlockedDesc;
                }
            }

            Transform iconTr = cardButton.transform.Find("Icon");
            if (iconTr != null)
            {
                Image iconImg = iconTr.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.sprite = portrait;
                    iconImg.enabled = iconImg.sprite != null;
                    iconImg.color = unlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
                    iconImg.transform.SetAsLastSibling();
                }
            }
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null)
            {
                statusLabel.text = text;
            }
        }

        private void EnsurePanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || panel != null)
            {
                return;
            }

            panel = new GameObject("HeroSelectionPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            Transform window = F.Window(EquipmentPanelLayout.CreateSurface(panel),
                "ĐÀI ANH HÙNG", "CHỌN NHÂN VẬT  /  THÁNH GIÓNG  •  SƠN TINH", Close);
            TMP_Text hints = window.Find("KeyHints").GetComponent<TMP_Text>();
            hints.text = "CHUỘT / ENTER  Chọn nhân vật   •   Q / E / R / F  4 Chiêu thức   •   H / ESC  Đóng";
            hints.rectTransform.sizeDelta = new Vector2(1272f, 26f);
            closeButton = window.Find("Close").GetComponent<Button>();

            Transform listFrame = F.Frame(window, "HeroList", 24f, 122f, 380f, 724f, F.Panel);
            F.Text(listFrame, "Heading", "DANH SÁCH NHÂN VẬT", 24f, 18f, 332f, 36f, 24f, F.Gold);

            sonTinhCardButton = F.Button(listFrame, "SelectSonTinhCard", "", 22f, 72f, 336f, 132f,
                () =>
                {
                    PreviewHero(HeroCharacterId.SonTinh);
                    SelectHeroImmediate(HeroCharacterId.SonTinh);
                });
            F.Icon(sonTinhCardButton.transform, "Icon", SonTinhVisualFactory.GetHeroPortraitSprite(), 12f, 18f, 92f, 92f);
            F.Text(sonTinhCardButton.transform, "Name", "SƠN TINH", 114f, 14f, 210f, 34f, 24f, F.Gold);
            F.Text(sonTinhCardButton.transform, "Desc", "Đại Thạch • Dâng Núi • Quần Sơn", 114f, 50f, 210f, 30f, 16f, F.TextColor);
            F.Text(sonTinhCardButton.transform, "State", "NHẤN ĐỂ CHỌN", 114f, 86f, 210f, 28f, 17f, F.Green);

            thanhGiongCardButton = F.Button(listFrame, "SelectThanhGiongCard", "", 22f, 222f, 336f, 132f,
                () =>
                {
                    PreviewHero(HeroCharacterId.ThanhGiong);
                    SelectHeroImmediate(HeroCharacterId.ThanhGiong);
                });
            F.Icon(thanhGiongCardButton.transform, "Icon", ThanhGiongVisualFactory.GetHeroPortraitSprite(), 12f, 18f, 92f, 92f);
            F.Text(thanhGiongCardButton.transform, "Name", "THÁNH GIÓNG", 114f, 14f, 210f, 34f, 24f, F.Gold);
            F.Text(thanhGiongCardButton.transform, "Desc", "Ngựa Sắt • Kiếm Sắt • Tre Ngà", 114f, 50f, 210f, 30f, 16f, F.TextColor);
            F.Text(thanhGiongCardButton.transform, "State", "NHẤN ĐỂ CHỌN", 114f, 86f, 210f, 28f, 17f, F.Green);

            forestCardButton = F.Button(listFrame, "SelectForestWarriorCard", "", 22f, 372f, 336f, 124f,
                () =>
                {
                    PreviewHero(HeroCharacterId.ForestWarrior);
                    SelectHeroImmediate(HeroCharacterId.ForestWarrior);
                });
            F.Icon(forestCardButton.transform, "Icon", ForestWarriorVisualFactory.GetHeroPortraitSprite(), 12f, 18f, 92f, 92f);
            F.Text(forestCardButton.transform, "Name", "CHIẾN BINH RỪNG", 114f, 14f, 210f, 34f, 24f, F.Gold);
            F.Text(forestCardButton.transform, "Desc", "Kiếm Pháp • Kỹ Năng Tự Do", 114f, 50f, 210f, 30f, 16f, F.TextColor);
            F.Text(forestCardButton.transform, "State", "ĐANG DÙNG", 114f, 86f, 210f, 28f, 17f, F.Green);

            TMP_Text testModeNote = F.Text(listFrame, "TestModeHint",
                "Chế độ thử nghiệm: Đã mở khóa toàn bộ anh hùng để kiểm tra chiêu thức trước khi bật khóa theo tiến trình Map.",
                24f, 520f, 332f, 140f, 16f, F.Muted);
            testModeNote.alignment = TextAlignmentOptions.TopLeft;

            Transform detailFrame = F.Frame(window, "HeroDetails", 424f, 122f, 888f, 724f, F.Panel);
            F.Medallion(detailFrame, 26f, 24f, 180f, F.Gold);
            heroPortrait = F.Icon(detailFrame, "HeroPortrait", SonTinhVisualFactory.GetHeroPortraitSprite(), 38f, 36f, 156f, 156f);
            heroHeading = F.Text(detailFrame, "HeroHeading", "", 224f, 26f, 640f, 44f, 30f, F.Gold);
            heroSubheading = F.Text(detailFrame, "HeroSubheading", "", 224f, 74f, 640f, 32f, 20f, F.Green);

            for (int i = 0; i < 4; i++)
            {
                Transform badge = F.Frame(detailFrame, "SkillBadge_" + i, 224f + i * 160f, 118f, 150f, 86f, F.Ink);
                skillPreviewIcons[i] = F.Icon(badge, "Icon", SonTinhVisualFactory.GetSkillIcon(i), 8f, 14f, 54f, 54f);
                skillPreviewLabels[i] = F.Text(badge, "Label", SonTinhSlotLabels[i], 66f, 10f, 78f, 66f, 15f, F.TextColor);
            }

            movesDescription = F.Text(detailFrame, "MovesDescription", "", 28f, 224f, 832f, 380f, 21f, F.TextColor);
            movesDescription.alignment = TextAlignmentOptions.TopLeft;

            confirmButton = F.Button(detailFrame, "ConfirmHeroButton", "CHỌN NHÂN VẬT SƠN TINH",
                28f, 632f, 832f, 64f, ConfirmPreviewedHero, true);
            confirmLabel = confirmButton.GetComponentInChildren<TMP_Text>();

            statusLabel = F.Text(window, "Status", "", 32f, 910f, 1272f, 26f, 17f, F.Gold);
            panel.GetComponent<EquipmentPanelLayout>().ConfigureWindow(window, true, null, statusLabel);
        }

        private void OnDisable()
        {
            Close();
        }

        private void OnDestroy()
        {
            focus.Release();
            GameplayState.ExitModal(this);
            if (panel != null)
            {
                Destroy(panel);
            }
        }
    }
}
