using TopDownGame.Audio;
using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Skills
{
    /// <summary>One painted sweep per damage beat. Animation and contact feedback share the combat timeline rather than free-running rotation.</summary>
    public sealed class SwordComboVisual : MonoBehaviour
    {
        private SwordComboProfile profile;
        private Transform actor;
        private SpriteRenderer slash;
        private float elapsed,beatDuration,diameter;
        private bool active;
        public int Beats { get; private set; }
        public static event System.Action<bool> FeedbackPlayed;

        public void Initialize(SwordComboProfile data,Transform owner,float radius)
        {
            profile=data;actor=owner;diameter=radius*2.6f;
            var child=new GameObject("PaintedSwordSweep",typeof(SpriteRenderer));
            child.transform.SetParent(transform,false);
            slash=child.GetComponent<SpriteRenderer>();slash.sharedMaterial=data.SlashMaterial;
            slash.sortingOrder=25;slash.enabled=false;
        }

        public void Pulse(int index,float interval,Vector2 aim)
        {
            if(profile==null || actor==null) return;
            Beats++;elapsed=0;beatDuration=Mathf.Max(.06f,interval);active=true;
            actor.GetComponent<PlayerCharacterAnimator>()?.PlaySwordStrike(profile,index,aim,beatDuration);
            float angle=Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg;
            slash.transform.localRotation=Quaternion.Euler(0,0,angle+(index%2==0 ? -22 : 20));
            slash.flipY=index%2==1;
            GameAudioManager.EnsureExists().Play(profile.SwingCue,actor.position);
            FeedbackPlayed?.Invoke(false);
            Present(0);
        }

        public void Contact(Vector3 point)
        {
            SkillEffect.SpawnBurst(point,new Color(1f,.87f,.53f),.38f,.14f);
            GameAudioManager.EnsureExists().Play(profile.HitCue,point);
            FeedbackPlayed?.Invoke(true);
        }

        private void LateUpdate()
        {
            if(actor==null){Destroy(gameObject);return;}
            transform.position=actor.position;
            if(GameplayState.IsInputBlocked || Time.timeScale<=0 || !active) return;
            elapsed+=Time.deltaTime;
            Present(Mathf.Clamp01(elapsed/beatDuration));
        }

        private void Present(float t)
        {
            if(profile.SlashFrames==null || profile.SlashFrames.Length==0) return;
            if(t>=1){slash.enabled=false;active=false;return;}
            int frame=Mathf.Min(5,Mathf.FloorToInt(t*6));
            slash.sprite=profile.SlashFrames[Mathf.Min(frame,profile.SlashFrames.Length-1)];
            slash.enabled=true;
            Vector2 bounds=slash.sprite.bounds.size;
            slash.transform.localScale=new Vector3(diameter/bounds.x,diameter*.80f/bounds.y,1);
            slash.color=new Color(1f,.97f,.88f,.78f);
        }
        private void OnDestroy(){if(actor!=null) actor.GetComponent<PlayerCharacterAnimator>()?.CancelSwordPose();}
    }
}
