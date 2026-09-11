using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Skills;
using TopDownGame.UI;
using UnityEngine;

namespace TopDownGame.Maps
{
    public sealed class RewardChest : MonoBehaviour, IInteractable
    {
        [SerializeField] private int goldReward = 25;
        [SerializeField] private int experienceReward = 35;
        [SerializeField, Range(0f, 1f)] private float healthRestorePercent = 0.35f;
        [SerializeField, Min(0f)] private float manaRestore = 30f;
        [SerializeField] private bool offersSkillChoice = true;
        [SerializeField] private SpriteRenderer visual;

        private bool unlocked;
        private bool opened;

        public bool CanInteract => unlocked && !opened;
        public string InteractionPrompt => CanInteract
            ? "E / Nhấp đúp - Mở rương thưởng"
            : "";

        private void Start()
        {
            if (visual == null)
            {
                visual = GetComponent<SpriteRenderer>();
            }
        }

        public void Unlock()
        {
            unlocked = true;
            if (visual != null)
            {
                visual.color = new Color(1f, 0.75f, 0.1f);
            }
        }

        public void Interact(PlayerInteraction player)
        {
            if (!CanInteract || player == null)
            {
                return;
            }

            opened = true;
            PlayerStats stats = player.GetComponent<PlayerStats>();
            stats?.AddGold(goldReward);
            stats?.AddExperience(experienceReward);
            stats?.RestoreMana(manaRestore);
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && stats != null)
            {
                health.Heal(stats.MaxHealth * healthRestorePercent);
            }

            if (offersSkillChoice)
            {
                SkillSelectionUI selection = FindAnyObjectByType<SkillSelectionUI>();
                selection?.Show(player.GetComponent<SkillInventory>());
            }

            if (visual != null)
            {
                visual.color = Color.gray;
            }
        }
    }
}
