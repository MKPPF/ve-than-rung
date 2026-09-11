using UnityEngine;

namespace TopDownGame.Skills
{
    /// <summary>
    /// Lets manual and automatic combat feed the same world-space aim point to
    /// skills without teaching SkillManager about every concrete spell type.
    /// </summary>
    public interface ITargetedSkill
    {
        void SetNextTarget(Vector2 targetPosition);
    }
}
