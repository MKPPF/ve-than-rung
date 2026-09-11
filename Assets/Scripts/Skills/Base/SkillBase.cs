using UnityEngine;

namespace TopDownGame.Skills
{
    public abstract class SkillBase : MonoBehaviour
    {
        [SerializeField] private SkillData data;
        private int level = 1;

        public SkillData Data => data;
        public SkillId ConfiguredId => data != null ? data.Id : SkillId.Fireball;
        public int Level => level;
        public bool IsInitialized => data != null;

        public void Initialize(SkillData skillData, int skillLevel)
        {
            data = skillData;
            level = Mathf.Clamp(skillLevel, 1, data != null ? data.MaxLevel : 1);
        }

        public void SetLevel(int newLevel)
        {
            level = Mathf.Clamp(newLevel, 1, data != null ? data.MaxLevel : 1);
        }

        public abstract void Cast();

        protected float ScaledDamage(float multiplier = 1f)
        {
            return data == null ? 0f : data.Damage * multiplier * (1f + (level - 1) * 0.2f);
        }

        protected float ScaledRange(float multiplier = 1f)
        {
            return data == null ? 0f : data.Range * multiplier * (1f + (level - 1) * 0.1f);
        }
    }
}
