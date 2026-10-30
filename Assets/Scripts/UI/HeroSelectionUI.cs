using TMPro;
using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using F = TopDownGame.UI.FantasyEquipmentUI;

namespace TopDownGame.UI
{
    /// <summary>
    /// Modal dialog allowing the player to switch between the default Forest Warrior
    /// and the legendary hero Thanh Giong (with his 5 Iron Sword, Iron Horse & Ivory Bamboo moves).
    /// Can be opened from the Lobby Hero Shrine (E / double-click) or with the H shortcut.
    /// </summary>
    public sealed class HeroSelectionUI : MonoBehaviour
    {
        private GameObject panel;
        private ThanhGiongHeroController heroController;
        private HeroCharacterId previewHero = HeroCharacterId.ThanhGiong;
        private Button forestCardButton;
        private Button thanhGiongCardButton;
        private Button confirmButton;
        private Button closeButton;
        private Image heroPortrait;
        private readonly Image[] skillPreviewIcons = new Image[4];
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

            previewHero = heroController != null && heroController.IsThanhGiongActive
                ? HeroCharacterId.ThanhGiong
                : HeroCharacterId.ThanhGiong;

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            focus.Capture(panel.transform);
            GameplayState.EnterModal(this);
            RefreshView();
            SetStatus("Chọn Thánh Gióng để cưỡi Ngựa Sắt và sử dụng bộ 5 chiêu Kiếm Sắt & Tre Ngà.");
            if (confirmButton != null && confirmButton.interactable)
            {
                confirmButton.Select();
            }
            else if (thanhGiongCardButton != null)
            {
                thanhGiongCardButton.Select();
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

            previewHero = hero;
            heroController.SelectHero(hero, true);
            RefreshView();
            SetStatus(hero == HeroCharacterId.ThanhGiong
                ? "Đã hóa thân thành THÁNH GIÓNG! Sử dụng Chuột phải/J và Q, E, R, F để thi triển 5 tuyệt kỹ."
                : "Đã chuyển về Chiến Binh Rừng (Mặc định).");
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

            if (thanhGiongCardButton != null)
            {
                thanhGiongCardButton.transform.Find("Brass").GetComponent<Image>().color =
                    previewHero == HeroCharacterId.ThanhGiong ? F.Gold : F.Edge;
                thanhGiongCardButton.transform.Find("State").GetComponent<TMP_Text>().text =
                    activeHero == HeroCharacterId.ThanhGiong ? "ĐANG DÙNG" : "NHẤN ĐỂ XEM";

                Transform tgIcon = thanhGiongCardButton.transform.Find("Icon");
                if (tgIcon != null)
                {
                    Image iconImg = tgIcon.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        iconImg.sprite = ThanhGiongVisualFactory.GetHeroPortraitSprite();
                        iconImg.enabled = iconImg.sprite != null;
                        iconImg.color = Color.white;
                        iconImg.transform.SetAsLastSibling();
                    }
                }
            }

            if (forestCardButton != null)
            {
                forestCardButton.transform.Find("Brass").GetComponent<Image>().color =
                    previewHero == HeroCharacterId.ForestWarrior ? F.Gold : F.Edge;
                forestCardButton.transform.Find("State").GetComponent<TMP_Text>().text =
                    activeHero == HeroCharacterId.ForestWarrior ? "ĐANG DÙNG" : "NHẤN ĐỂ XEM";
            }

            if (previewHero == HeroCharacterId.ThanhGiong)
            {
                Sprite tgPortrait = ThanhGiongVisualFactory.GetHeroPortraitSprite();
                heroPortrait.sprite = tgPortrait;
                heroPortrait.enabled = tgPortrait != null;
                heroPortrait.color = Color.white;
                heroHeading.text = "THÁNH GIÓNG — PHÙ ĐỔNG THIÊN VƯƠNG";
                heroSubheading.text = "Giáp Vàng  •  Ngựa Sắt Phun Lửa  •  Kiếm Sắt & Bụi Tre Ngà";
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
                }
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Sprite fallback = player != null && player.GetComponent<SpriteRenderer>() != null
                    ? player.GetComponent<SpriteRenderer>().sprite
                    : ThanhGiongVisualFactory.GetHeroPortraitSprite();
                heroPortrait.sprite = fallback;
                heroPortrait.enabled = heroPortrait.sprite != null;
                heroHeading.text = "CHIẾN BINH RỪNG (MẶC ĐỊNH)";
                heroSubheading.text = "Kiếm Pháp Cơ Bản  •  Kỹ Năng Nguyên Tố Tự Do";
                movesDescription.text =
                    "<b>• Đánh thường [Chuột phải / J]:</b> Chém cận chiến cơ bản.\n" +
                    "<b>• Kỹ năng [Q / E / R / F]:</b> Sử dụng các kỹ năng nguyên tố đã trang bị trong bảng Kỹ Năng [K].\n\n" +
                    "Bạn có thể chuyển sang nhân vật <b>Thánh Gióng</b> bất cứ lúc nào để cưỡi Ngựa Sắt và dùng bộ 5 chiêu Kiếm Sắt & Tre Ngà.";
                for (int i = 0; i < 4; i++)
                {
                    if (skillPreviewIcons[i] != null)
                    {
                        skillPreviewIcons[i].sprite = ThanhGiongVisualFactory.GetSkillIcon(i);
                        skillPreviewIcons[i].enabled = false;
                    }
                }
            }

            bool alreadyActive = activeHero == previewHero;
            if (confirmButton != null)
            {
                confirmButton.interactable = !alreadyActive;
            }

            if (confirmLabel != null)
            {
                confirmLabel.text = alreadyActive
                    ? "ĐANG SỬ DỤNG NHÂN VẬT NÀY"
                    : previewHero == HeroCharacterId.ThanhGiong
                        ? "CHỌN NHÂN VẬT THÁNH GIÓNG"
                        : "CHUYỂN VỀ CHIẾN BINH RỪNG";
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
                "ĐÀI ANH HÙNG", "CHỌN NHÂN VẬT  /  THÁNH GIÓNG", Close);
            TMP_Text hints = window.Find("KeyHints").GetComponent<TMP_Text>();
            hints.text = "CHUỘT / ENTER  Chọn nhân vật   •   Q / E / R / F  4 Chiêu thức   •   H / ESC  Đóng";
            hints.rectTransform.sizeDelta = new Vector2(1272f, 26f);
            closeButton = window.Find("Close").GetComponent<Button>();

            Transform listFrame = F.Frame(window, "HeroList", 24f, 122f, 380f, 724f, F.Panel);
            F.Text(listFrame, "Heading", "DANH SÁCH NHÂN VẬT", 24f, 18f, 332f, 36f, 24f, F.Gold);

            thanhGiongCardButton = F.Button(listFrame, "SelectThanhGiongCard", "", 22f, 76f, 336f, 132f,
                () =>
                {
                    PreviewHero(HeroCharacterId.ThanhGiong);
                    SelectHeroImmediate(HeroCharacterId.ThanhGiong);
                });
            F.Icon(thanhGiongCardButton.transform, "Icon", ThanhGiongVisualFactory.GetHeroPortraitSprite(), 12f, 18f, 92f, 92f);
            F.Text(thanhGiongCardButton.transform, "Name", "THÁNH GIÓNG", 114f, 14f, 210f, 34f, 24f, F.Gold);
            F.Text(thanhGiongCardButton.transform, "Desc", "Ngựa Sắt • Kiếm Sắt • Tre Ngà", 114f, 50f, 210f, 30f, 17f, F.TextColor);
            F.Text(thanhGiongCardButton.transform, "State", "NHẤN ĐỂ CHỌN", 114f, 86f, 210f, 28f, 17f, F.Green);

            forestCardButton = F.Button(listFrame, "SelectForestWarriorCard", "", 22f, 226f, 336f, 120f,
                () =>
                {
                    PreviewHero(HeroCharacterId.ForestWarrior);
                    SelectHeroImmediate(HeroCharacterId.ForestWarrior);
                });
            F.Icon(forestCardButton.transform, "Icon", ThanhGiongVisualFactory.GetSkillIcon(0), 14f, 20f, 76f, 76f);
            F.Text(forestCardButton.transform, "Name", "CHIẾN BINH RỪNG", 104f, 16f, 220f, 32f, 22f, F.TextColor);
            F.Text(forestCardButton.transform, "Desc", "Nhân vật mặc định", 104f, 50f, 220f, 28f, 17f, F.Muted);
            F.Text(forestCardButton.transform, "State", "", 104f, 80f, 220f, 26f, 17f, F.Gold);

            Transform detailFrame = F.Frame(window, "HeroDetails", 424f, 122f, 888f, 724f, F.Panel);
            F.Medallion(detailFrame, 26f, 24f, 180f, F.Gold);
            heroPortrait = F.Icon(detailFrame, "HeroPortrait", ThanhGiongVisualFactory.GetHeroPortraitSprite(), 38f, 36f, 156f, 156f);
            heroHeading = F.Text(detailFrame, "HeroHeading", "", 224f, 26f, 640f, 44f, 30f, F.Gold);
            heroSubheading = F.Text(detailFrame, "HeroSubheading", "", 224f, 74f, 640f, 32f, 20f, F.Green);

            string[] slotTitles =
            {
                "Q: Kiếm + Ngựa Sắt",
                "E: 1 Cây Tre Quật",
                "R: Cưỡi Ngựa Quật 360°",
                "F: Càn Lướt Ngựa + Tre"
            };
            for (int i = 0; i < 4; i++)
            {
                Transform badge = F.Frame(detailFrame, "SkillBadge_" + i, 224f + i * 160f, 118f, 150f, 86f, F.Ink);
                skillPreviewIcons[i] = F.Icon(badge, "Icon", ThanhGiongVisualFactory.GetSkillIcon(i), 8f, 14f, 54f, 54f);
                F.Text(badge, "Label", slotTitles[i], 66f, 10f, 78f, 66f, 15f, F.TextColor);
            }

            movesDescription = F.Text(detailFrame, "MovesDescription", "", 28f, 224f, 832f, 380f, 22f, F.TextColor);
            movesDescription.alignment = TextAlignmentOptions.TopLeft;

            confirmButton = F.Button(detailFrame, "ConfirmHeroButton", "CHỌN NHÂN VẬT THÁNH GIÓNG",
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
