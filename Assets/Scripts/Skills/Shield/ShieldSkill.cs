using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Skills
{
    public sealed class ShieldSkill : SkillBase
    {
        [SerializeField, Range(0f, 1f)] private float damageReduction = 0.65f;
        [SerializeField, Min(0.1f)] private float duration = 4f;

        public override void Cast()
        {
            PlayerHealth health = GetComponent<PlayerHealth>();
            health?.ActivateShield(damageReduction, duration + (Level - 1) * 0.5f);
            SpellVfx.Spawn(Data.Presentation,SpellVfxStage.Aura,transform.position,2.2f,duration+(Level-1)*0.5f,transform);
        }
    }
}
