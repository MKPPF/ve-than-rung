using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Skills
{
    public sealed class HealingSkill : SkillBase
    {
        [SerializeField, Min(1f)] private float healAmount = 35f;

        public override void Cast()
        {
            PlayerHealth health = GetComponent<PlayerHealth>();
            health?.Heal(healAmount * (1f + (Level - 1) * 0.2f));
            SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Aura,transform.position,2.5f,0.95f,transform);
        }
    }
}
