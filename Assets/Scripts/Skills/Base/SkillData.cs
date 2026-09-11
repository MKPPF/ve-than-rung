using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace TopDownGame.Skills
{
    public enum SkillId
    {
        Fireball,
        IceDash,
        SwordSpin,
        LightningStrike,
        Healing,
        Shield,
        ChainLightning,
        MeteorShower,
        VoidPulse
    }

    [CreateAssetMenu(menuName = "Top Down Game/Skill Data", fileName = "SkillData")]
    [MovedFrom(true, "TopDownGame.Skills", "Assembly-CSharp", "SkillData")]
    public sealed class SkillData : ScriptableObject
    {
        [SerializeField] private SkillId skillId;
        [SerializeField] private string displayName = "Skill";
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0f)] private float damage = 30f;
        [SerializeField, Min(0f)] private float manaCost = 20f;
        [SerializeField, Min(0f)] private float cooldown = 4f;
        [SerializeField, Min(0.1f)] private float range = 5f;
        [SerializeField, Min(1)] private int maxLevel = 5;
        [SerializeField] private Color effectColor = Color.white;
        [SerializeField] private SkillPresentationProfile presentation;
        public SkillPresentationProfile Presentation => presentation;

        public SkillId Id => skillId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public float Damage => damage;
        public float ManaCost => manaCost;
        public float Cooldown => cooldown;
        public float Range => range;
        public int MaxLevel => maxLevel;
        public Color EffectColor => effectColor;
    }
}
