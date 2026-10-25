using System.Collections;
using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Skills
{
    public enum SkillCastPhase { Ready, Windup, Release, Recovery }
    /// <summary>Single owner of the cast timeline. Context is captured at acceptance and released exactly once.</summary>
    public sealed class SkillCastController : MonoBehaviour
    {
        public SkillCastPhase Phase { get; private set; }
        public bool IsCasting => Phase != SkillCastPhase.Ready;
        public Vector2 LockedDirection { get; private set; }
        private SpellVfx gathering;

        public bool Begin(SkillBase skill,int level,Vector2 point,Vector2 direction)
        {
            if(IsCasting || !isActiveAndEnabled || skill==null || GetComponent<PlayerDodge>()?.IsDodging == true) return false;
            StartCoroutine(Execute(skill,level,point,direction));
            return true;
        }

        private IEnumerator Execute(SkillBase skill,int level,Vector2 point,Vector2 direction)
        {
            var profile=skill.Data.Presentation;
            float windup=profile!=null ? profile.Windup : SkillManager.CastWindup;
            float recovery=profile!=null ? profile.Recovery : 0.3f;
            var sword=skill as SwordSpinSkill;
            if(sword!=null && sword.ComboProfile!=null) recovery=sword.Duration;
            var health=GetComponent<PlayerHealth>();
            LockedDirection=direction;
            Phase=SkillCastPhase.Windup;
            GetComponent<PlayerMovement>()?.LockMovement(windup+recovery);
            if(sword!=null && sword.ComboProfile!=null)
                GetComponent<PlayerCharacterAnimator>()?.PlaySwordWindup(sword.ComboProfile,direction,windup);
            else
            {
                GetComponent<PlayerCharacterAnimator>()?.PlayPaintedCast(profile,direction,windup+recovery,windup);
                gathering=SpellVfx.Spawn(profile,SpellVfxStage.Gather,transform.position,1.45f,windup,transform,direction);
            }
            float time=0;
            try
            {
                while(time<windup)
                {
                    if(health!=null && !health.IsAlive) yield break;
                    yield return null;
                    if(!GameplayState.IsInputBlocked) time+=Time.deltaTime;
                }
                if(skill==null || (health!=null && !health.IsAlive)) yield break;
                Phase=SkillCastPhase.Release;
                GetComponent<PlayerCombat>()?.SetAimDirection(direction);
                if(skill is ITargetedSkill targeted) targeted.SetNextTarget(point);
                skill.SetLevel(level);
                skill.Cast();
                Phase=SkillCastPhase.Recovery;
                time=0;
                while(time<recovery)
                {
                    yield return null;
                    if(!GameplayState.IsInputBlocked) time+=Time.deltaTime;
                }
            }
            finally {Phase=SkillCastPhase.Ready;if(gathering!=null) Destroy(gathering.gameObject);}
        }
        public void Cancel()
        {
            StopAllCoroutines();Phase=SkillCastPhase.Ready;
            GetComponent<PlayerCharacterAnimator>()?.CancelSwordPose();
            GetComponent<PlayerCharacterAnimator>()?.CancelMagicPose();
            if(gathering!=null) Destroy(gathering.gameObject);
        }
        private void OnDisable() => Cancel();
    }
}
