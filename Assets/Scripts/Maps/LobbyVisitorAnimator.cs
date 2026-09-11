using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Owns visitor sprite direction and steps, independently of player input and combat animation.</summary>
    public sealed class LobbyVisitorAnimator : MonoBehaviour
    {
        [SerializeField] private LobbyVisitor movement;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite idle;
        [SerializeField] private Sprite[] rightFrames, upFrames, downFrames;
        [SerializeField, Min(0.01f)] private float strideLength = 0.72f;
        private float walked;
        private Vector3 previous;
        private float stillTime;
        private Vector2 lastDirection=Vector2.right;
        private void OnEnable(){previous=transform.position;}
        private void LateUpdate()
        {
            if(visual==null || movement==null)return;
            Vector2 step=transform.position-previous;previous=transform.position;
            if(Time.timeScale<=0)return;
            if(step.sqrMagnitude<0.000001f)
            {
                stillTime+=Time.deltaTime;
                if(stillTime>0.12f)
                {
                    // Keep the last vertical facing while resting instead of
                    // snapping from a back-facing walk to a front-facing idle.
                    var restFrames=lastDirection.y>0?upFrames:downFrames;
                    if(Mathf.Abs(lastDirection.y)>Mathf.Abs(lastDirection.x)*1.3f && restFrames!=null && restFrames.Length>0)
                        visual.sprite=restFrames[0];
                    else if(idle!=null)visual.sprite=idle;
                }
                return;
            }
            stillTime=0;lastDirection=step.normalized;
            walked+=step.magnitude;
            Sprite[] frames=rightFrames;
            if(Mathf.Abs(step.y)>Mathf.Abs(step.x)*1.3f)
            {
                frames=step.y>0?upFrames:downFrames;visual.flipX=false;
            }
            else visual.flipX=step.x<0;
            if(frames==null || frames.Length==0){if(idle!=null)visual.sprite=idle;return;}
            int frame=Mathf.FloorToInt(walked/strideLength*frames.Length)%frames.Length;
            if(frames[frame]!=null)visual.sprite=frames[frame];
        }
    }
}
