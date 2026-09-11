using System.Collections.Generic;
using TMPro;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    public sealed class SkillSelectionUI : MonoBehaviour
    {
        [SerializeField] private SkillData[] skillDatabase;
        [SerializeField] private GameObject panel;

        private SkillInventory inventory;

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void Show(SkillInventory targetInventory)
        {
            inventory = targetInventory;
            if (inventory == null || skillDatabase == null || skillDatabase.Length == 0)
            {
                return;
            }

            EnsurePanel();
            if (panel == null)
            {
                return;
            }
            panel.SetActive(true);
            Time.timeScale = 0f;

            List<SkillData> choices = new List<SkillData>(skillDatabase);
            while (choices.Count > 3)
            {
                choices.RemoveAt(Random.Range(0, choices.Count));
            }

            for (int i = 0; i < choices.Count; i++)
            {
                SkillData choice = choices[i];
                Button button = CreateButton(panel.transform, choice, i);
                button.onClick.AddListener(() => Select(choice));
            }
        }

        private void Select(SkillData data)
        {
            string id = data.Id.ToString();
            bool alreadyOwned = inventory.Owns(id);
            inventory.AddOrUpgrade(id, data.MaxLevel);

            if (!alreadyOwned)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (inventory.Slots[i].IsEmpty)
                    {
                        inventory.TryEquip(id, i);
                        break;
                    }
                }
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            player?.GetComponent<SkillManager>()?.RefreshSkills();
            panel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void EnsurePanel()
        {
            if (panel != null)
            {
                foreach (Transform child in panel.transform)
                {
                    Destroy(child.gameObject);
                }

                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            panel = new GameObject("SkillSelectionPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800f, 280f);
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.95f);
        }

        private static Button CreateButton(Transform parent, SkillData data, int index)
        {
            GameObject buttonObject = new GameObject(data.DisplayName, typeof(RectTransform),
                typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(230f, 180f);
            rect.anchoredPosition = new Vector2(30f + index * 255f, 0f);
            buttonObject.GetComponent<Image>().color = data.EffectColor;

            GameObject textObject = new GameObject("Description", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 10f);
            textRect.offsetMax = new Vector2(-10f, -10f);
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = data.DisplayName + "\n\n" + data.Description;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 17f;
            text.color = Color.white;
            return buttonObject.GetComponent<Button>();
        }
    }
}
