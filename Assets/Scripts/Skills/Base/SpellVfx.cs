using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Skills
{
    public enum SpellVfxStage { Gather, Projectile, Impact, Aura, Warning, Meteor }
    /// <summary>Presentation-only quad with analytic elemental shapes; owns and disposes its material, never deals damage.</summary>
    public sealed class SpellVfx : MonoBehaviour
    {
        private Material material;
        private Transform follow;
        private bool follows;
        private float elapsed,duration;
        private Vector3 anchor;
        private SpellVfxStage stage;
        private SkillPresentationProfile profile;
        private SpriteRenderer painted;
        private MaterialPropertyBlock block;
        private float diameter;
        private Vector3 visualOffset;
        public float Elapsed => elapsed;

        public static SpellVfx Spawn(SkillPresentationProfile profile,SpellVfxStage stage,Vector3 position,
            float diameter,float lifetime,Transform follow=null,Vector2 direction=default)
        {
            Shader shader=profile!=null ? profile.Shader : Shader.Find("TopDownGame/ElementalSpell");
            if(shader==null || !shader.isSupported) return null;
            var go=new GameObject("SpellV2_"+(profile!=null ? profile.Element.ToString() : "Fire")+"_"+stage);
            go.transform.position=position;
            if(follow!=null) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,follow.gameObject.scene);
            var effect=go.AddComponent<SpellVfx>();
            effect.follow=follow;effect.follows=follow!=null;effect.stage=stage;effect.anchor=position;
            effect.duration=Mathf.Max(0.04f,lifetime);
            effect.profile=profile;effect.diameter=diameter;
            if(profile!=null && profile.HasPaintedArt && stage!=SpellVfxStage.Warning && stage!=SpellVfxStage.Gather)
            {
                effect.painted=go.AddComponent<SpriteRenderer>();
                effect.painted.sharedMaterial=profile.PaintedMaterial;
                effect.painted.sortingOrder=profile.Element==SpellElement.Nature || profile.Element==SpellElement.Void ? -1 : 26;
                effect.block=new MaterialPropertyBlock();
                if(profile.Element==SpellElement.Lightning) effect.visualOffset=Vector3.up*diameter*.32f;
                if(stage==SpellVfxStage.Projectile && direction.sqrMagnitude>.001f)
                    go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
                effect.ShowPainted(0);
                return effect;
            }
            go.transform.localScale=new Vector3(diameter,diameter,1);
            if(stage==SpellVfxStage.Gather || stage==SpellVfxStage.Warning)
                go.transform.localScale=new Vector3(diameter,diameter*0.62f,1);
            if(stage==SpellVfxStage.Projectile && direction.sqrMagnitude>0.001f)
                go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            var renderer=go.AddComponent<SpriteRenderer>();
            renderer.sprite=RuntimeSpriteFactory.WhiteSprite;
            renderer.sortingOrder=stage==SpellVfxStage.Gather || stage==SpellVfxStage.Warning ? -1 : 26;
            effect.material=new Material(shader){name="SpellV2_Owned"};
            effect.material.SetFloat("_Element",profile!=null ? (int)profile.Element : 0);
            effect.material.SetFloat("_Stage",(int)stage);
            effect.material.SetColor("_Tint",profile!=null ? profile.Primary : new Color(1,0.3f,0.03f));
            effect.material.SetColor("_Core",profile!=null ? profile.Core : Color.white);
            renderer.sharedMaterial=effect.material;
            return effect;
        }

        private void LateUpdate()
        {
            if(follows && follow==null){Destroy(gameObject);return;}
            if(follow!=null) transform.position=follow.position+visualOffset;
            if(GameplayState.IsInputBlocked || Time.timeScale<=0) return;
            elapsed+=Time.deltaTime;
            float t=Mathf.Clamp01(elapsed/duration);
            if(painted!=null) ShowPainted(t);
            else {material.SetFloat("_Progress",t);material.SetFloat("_Clock",elapsed);}
            if(stage==SpellVfxStage.Meteor) transform.position=anchor+new Vector3(-1.7f,4.5f)*(1-t*t);
            if(t>=1) Destroy(gameObject);
        }
        private void ShowPainted(float t)
        {
            bool flight=stage==SpellVfxStage.Projectile || stage==SpellVfxStage.Meteor;
            int frame;
            if(flight) frame=Mathf.FloorToInt(elapsed*14)%3;
            else if(stage==SpellVfxStage.Aura && profile.Element==SpellElement.Ward)
                frame=t<.1f ? Mathf.Min(2,(int)(t*30)) : t>.9f ? 4+Mathf.Min(1,(int)((t-.9f)*20)) : 2+Mathf.FloorToInt(elapsed*6)%2;
            else if(profile.Element==SpellElement.Fire) frame=3+Mathf.Min(2,(int)(t*3));
            else frame=Mathf.Min(5,(int)(t*6));
            Sprite sprite=profile.PaintedFrames[frame];painted.sprite=sprite;
            Rect rect=sprite.textureRect;
            block.SetVector("_CellRect",new Vector4(rect.x/sprite.texture.width,rect.y/sprite.texture.height,rect.width/sprite.texture.width,rect.height/sprite.texture.height));
            painted.SetPropertyBlock(block);
            transform.localScale=new Vector3(diameter/sprite.bounds.size.x,diameter/sprite.bounds.size.y,1);
            float fade=flight ? 1 : Mathf.Min(1,(1-t)*5);
            painted.color=new Color(1,1,1,fade*.85f);
            if(!follows && stage!=SpellVfxStage.Meteor) transform.position=anchor+visualOffset;
        }
        private void OnDestroy(){if(material!=null) Destroy(material);}
    }
}
