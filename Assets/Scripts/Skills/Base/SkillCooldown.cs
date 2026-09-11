using System.Collections.Generic;
using UnityEngine;

namespace TopDownGame.Skills
{
    public sealed class SkillCooldown
    {
        private readonly Dictionary<SkillId, float> readyAt = new Dictionary<SkillId, float>();

        public bool IsReady(SkillId id)
        {
            return !readyAt.TryGetValue(id, out float time) || Time.time >= time;
        }

        public float Remaining(SkillId id)
        {
            if (!readyAt.TryGetValue(id, out float time))
            {
                return 0f;
            }

            return Mathf.Max(0f, time - Time.time);
        }

        public void Start(SkillId id, float duration, float cooldownRate)
        {
            readyAt[id] = Time.time + duration / Mathf.Max(0.1f, cooldownRate);
        }
    }
}
