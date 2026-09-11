using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Skills
{
    /// <summary>Stretches only the longitudinal axis of a painted bolt between two confirmed hit points.</summary>
    public sealed class PaintedLightningLink : MonoBehaviour
    {
        private SkillPresentationProfile profile;
        private SpriteRenderer visual;
        private MaterialPropertyBlock block;
        private float elapsed,duration,length;
        public static void Spawn(SkillPresentationProfile profile,Vector3 from,Vector3 to,float duration)
        {
            var root=new GameObject("PaintedChainLinkV4");root.transform.position=(from+to)*.5f;
            Vector3 delta=to-from;
            root.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-90);
            var link=root.AddComponent<PaintedLightningLink>();link.profile=profile;link.duration=duration;link.length=delta.magnitude;
            link.visual=root.AddComponent<SpriteRenderer>();link.visual.sharedMaterial=profile.PaintedMaterial;link.visual.sortingOrder=27;
            link.block=new MaterialPropertyBlock();link.Present();
        }
        private void Present()
        {
            var sprite=profile.PaintedFrames[Mathf.Min(5,1+(int)(elapsed/duration*5))];visual.sprite=sprite;
            transform.localScale=new Vector3(.9f/sprite.bounds.size.x,length*1.25f/sprite.bounds.size.y,1);
            var r=sprite.textureRect;block.SetVector("_CellRect",new Vector4(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));
            visual.SetPropertyBlock(block);visual.color=new Color(1,1,1,1-elapsed/duration);
        }
        private void Update(){if(GameplayState.IsInputBlocked || Time.timeScale<=0)return;elapsed+=Time.deltaTime;Present();if(elapsed>=duration)Destroy(gameObject);}
    }
}
