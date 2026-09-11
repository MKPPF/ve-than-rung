using System;

namespace TopDownGame.Skills
{
    [Serializable]
    public sealed class SkillSlot
    {
        public string key = "Q";
        public string skillId;
        public int level = 1;

        public bool IsEmpty => string.IsNullOrWhiteSpace(skillId);
    }
}
