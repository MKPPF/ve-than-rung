using TopDownGame.Enemies;
using TopDownGame.Enemies.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>
    /// Small world-space health bar shown above every enemy.
    /// It is created at runtime so enemy prefabs stay lightweight and reusable.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(1.15f, 0.12f);
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.95f, 0f);

        private EnemyHealth health;
        private Slider slider;
        private Canvas canvas;
        private TMP_Text levelLabel;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            BuildBar();
        }

        private void OnEnable()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            health.HealthChanged += Refresh;
            Refresh(health.CurrentHealth, health.MaxHealth);
            RefreshLabel();
        }

        private void Start()
        {
            Refresh(health.CurrentHealth, health.MaxHealth);
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HealthChanged -= Refresh;
            }
        }

        private void LateUpdate()
        {
            if (canvas != null)
            {
                canvas.transform.localPosition = offset;
                canvas.transform.localRotation = Quaternion.identity;
            }

            if (health != null && !health.IsAlive && canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        private void Refresh(float current, float maximum)
        {
            if (slider == null)
            {
                return;
            }

            slider.maxValue = Mathf.Max(1f, maximum);
            slider.value = Mathf.Clamp(current, 0f, slider.maxValue);
            slider.gameObject.SetActive(health == null || health.IsAlive);
            RefreshLabel();
        }

        private void BuildBar()
        {
            // The hunting pool clones initialized scene enemies, including their
            // generated children. Rebind that bar so it has one owner and its
            // label follows this clone's profile rather than the seed's snapshot.
            Transform existingBar = transform.Find("EnemyHealthBar");
            if (existingBar != null)
            {
                canvas = existingBar.GetComponent<Canvas>();
                slider = existingBar.GetComponent<Slider>();
                levelLabel = existingBar.GetComponentInChildren<TMP_Text>(true);
                if (canvas != null && slider != null && levelLabel != null) return;
            }

            GameObject barObject = new GameObject("EnemyHealthBar", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            barObject.transform.SetParent(transform, false);
            barObject.transform.localPosition = offset;
            barObject.transform.localScale = Vector3.one * 0.01f;

            canvas = barObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 40;
            CanvasScaler scaler = barObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100f;

            RectTransform barRect = barObject.GetComponent<RectTransform>();
            barRect.sizeDelta = size * 100f;

            slider = barObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.transition = Selectable.Transition.None;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(barObject.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            Stretch(backgroundRect);
            background.GetComponent<Image>().color = new Color(0.03f, 0.02f, 0.02f, 0.92f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(barObject.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            Stretch(fillRect);
            fill.GetComponent<Image>().color = new Color(0.9f, 0.12f, 0.1f, 1f);
            slider.fillRect = fillRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.handleRect = null;

            GameObject labelObject = new GameObject("EnemyLevelLabel",
                typeof(RectTransform));
            labelObject.transform.SetParent(barObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 5f);
            labelRect.sizeDelta = new Vector2(340f, 28f);
            levelLabel = labelObject.AddComponent<TextMeshProUGUI>();
            levelLabel.alignment = TextAlignmentOptions.Center;
            levelLabel.fontSize = 17f;
            levelLabel.textWrappingMode = TextWrappingModes.NoWrap;
            levelLabel.overflowMode = TextOverflowModes.Ellipsis;
            levelLabel.color = Color.white;
            levelLabel.outlineWidth = 0.16f;
            levelLabel.outlineColor = Color.black;
            levelLabel.raycastTarget = false;
        }

        private void RefreshLabel()
        {
            if (levelLabel == null || health == null) return;
            EnemyRuntimeProfile profile = health.Profile;
            levelLabel.text = profile != null
                ? "Cấp " + profile.Level + "  " + profile.DisplayName
                : "Cấp 1  Địch";
            levelLabel.color = profile != null && profile.IsElite
                ? new Color(1f, 0.68f, 0.18f)
                : Color.white;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
