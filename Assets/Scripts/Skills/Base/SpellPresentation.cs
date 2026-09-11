using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Skills
{
    /// <summary>Small reusable sprite layers communicate gathering, release and protection without opaque screen-filling flashes.</summary>
    public sealed class SpellPresentation : MonoBehaviour
    {
        private enum Style { Charge, Burst, Ward, Meteor, Echo }
        private Style style;
        private Transform follow;
        private Vector2 offset;
        private SpriteRenderer[] pieces;
        private Color tint;
        private float duration, radius, elapsed;
        private Vector3 endpoint;
        public float Elapsed => elapsed;

        private static SpellPresentation Create(string label, Vector3 point, Color color,
            float lifetime, float size, Style kind, int count)
        {
            var root = new GameObject(label);
            root.transform.position = point;
            var effect = root.AddComponent<SpellPresentation>();
            effect.style = kind; effect.tint = color; effect.duration = Mathf.Max(0.04f, lifetime);
            effect.radius = Mathf.Max(0.05f, size);
            effect.pieces = new SpriteRenderer[count];
            for (int i=0;i<count;i++)
            {
                var child=new GameObject("Light_"+i,typeof(SpriteRenderer));
                child.transform.SetParent(root.transform,false);
                var renderer=child.GetComponent<SpriteRenderer>();
                renderer.sprite=i==0 ? RuntimeSpriteFactory.RingSprite : RuntimeSpriteFactory.SoftGlowSprite;
                renderer.sortingOrder=i==0 ? -1 : 24;
                renderer.color=Color.clear;
                effect.pieces[i]=renderer;
            }
            return effect;
        }

        public static void Charge(Transform owner, Vector2 direction, Color color, float time)
        {
            var effect=Create("SpellGather",owner.position,color,time,0.48f,Style.Charge,9);
            effect.follow=owner;effect.offset=direction*0.38f+Vector2.up*0.18f;
        }

        public static void Impact(Vector3 point, Color color, float size, float time)
        {
            Create("SpellImpact",point,color,time,size,Style.Burst,13);
        }

        public static void Ward(Transform owner, Color color, float time)
        {
            var effect=Create("ShieldWard",owner.position,color,time,0.75f,Style.Ward,9);
            effect.follow=owner;
        }

        public static void FallingMeteor(Vector2 point, Color color, float time)
        {
            var effect=Create("FallingMeteor",point,color,time,0.35f,Style.Meteor,7);
            effect.endpoint=point;
        }

        public static void Echo(SpriteRenderer source, Color color)
        {
            if(source==null || source.sprite==null) return;
            var effect=Create("IceAfterimage",source.transform.position,color,0.2f,1,Style.Echo,1);
            effect.pieces[0].sprite=source.sprite;
            effect.pieces[0].flipX=source.flipX;
            effect.pieces[0].sortingOrder=source.sortingOrder-1;
            effect.transform.localScale=source.transform.lossyScale;
        }

        private void Update()
        {
            if(GameplayState.IsInputBlocked || Time.timeScale<=0) return;
            if((style==Style.Charge || style==Style.Ward) && follow==null) {Destroy(gameObject);return;}
            if(follow!=null) transform.position=follow.position+(Vector3)offset;
            elapsed+=Time.deltaTime;
            float t=Mathf.Clamp01(elapsed/duration);
            float fade=(1-t)*(1-t);
            for(int i=0;i<pieces.Length;i++)
            {
                var piece=pieces[i];
                float angle=i*2.399963f+elapsed*2;
                Vector2 dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                float alpha=fade*0.7f;
                Vector2 position=Vector2.zero;
                Vector2 scale=Vector2.one*0.06f;
                if(style==Style.Charge)
                {
                    position=dir*radius*(1-t);
                    scale=Vector2.one*(i==0 ? 0.1f+t*0.28f : 0.035f+t*0.04f);
                    alpha=Mathf.Sin(t*Mathf.PI)*0.8f;
                }
                else if(style==Style.Burst)
                {
                    float spread=1-Mathf.Pow(1-t,3);
                    position=dir*radius*spread*(0.35f+(i%4)*0.16f);
                    scale=new Vector2(0.035f+radius*0.08f*(1-t),0.02f+radius*0.025f);
                    piece.transform.localEulerAngles=new Vector3(0,0,angle*Mathf.Rad2Deg);
                    if(i==0) {position=Vector2.zero;scale=new Vector2(radius*2*spread,radius*1.25f*spread);alpha=fade*0.4f;}
                }
                else if(style==Style.Ward)
                {
                    position=dir*radius;position.y*=0.85f;
                    scale=Vector2.one*0.08f;
                    alpha=Mathf.Min(t*12,1)*Mathf.Min((1-t)*12,1)*(0.3f+0.12f*Mathf.Sin(elapsed*3));
                    if(i==0){position=Vector2.up*0.16f;scale=new Vector2(radius*2,radius*2.3f);}
                }
                else if(style==Style.Meteor)
                {
                    float arrival=t*t;
                    transform.position=endpoint+new Vector3(-1.7f,4.5f,0)*(1-arrival);
                    position=new Vector2(-0.075f,0.20f)*i;
                    scale=new Vector2(radius*0.9f,radius*1.9f)*(1-i/8f);
                    piece.transform.localEulerAngles=new Vector3(0,0,21f);
                    alpha=(1-i/8f)*Mathf.Min(1,t*8);
                    piece.sprite=RuntimeSpriteFactory.SoftGlowSprite;
                }
                else {scale=Vector2.one;alpha=fade*0.38f;}
                piece.transform.localPosition=position;
                piece.transform.localScale=new Vector3(scale.x,scale.y,1);
                Color color=Color.Lerp(tint,Color.white,i==1 && style!=Style.Meteor ? 0.65f : 0.1f);
                if(style==Style.Meteor) color=Color.Lerp(new Color(1f,0.91f,0.48f),new Color(1f,0.19f,0.02f),i/6f);
                color.a=alpha;piece.color=color;
            }
            if(t>=1) Destroy(gameObject);
        }
    }
}
