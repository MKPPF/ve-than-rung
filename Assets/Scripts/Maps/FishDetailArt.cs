using TopDownGame.Core;
using UnityEngine;

namespace TopDownGame.Maps
{
    /// <summary>Adds bounded anatomical markings to existing fish rigs; reuses shared sprites, never owns gameplay or animation.</summary>
    public static class FishDetailArt
    {
        public static void Add(Transform fish, float length, Color pigment, int order, int variant)
        {
            var details=new GameObject("FishDetails");
            details.transform.SetParent(fish,false);fish=details.transform;
            Color shade=Color.Lerp(pigment,new Color(0.07f,0.23f,0.22f,pigment.a),0.48f);
            Color pearl=new Color(0.92f,0.89f,0.73f,pigment.a*0.85f);
            // All parts stay inside the existing length/2 containment radius.
            Oval(fish,"Spine",new Vector2(0.01f,0.022f)*length,new Vector2(0.53f,0.11f)*length,shade,order);
            Oval(fish,"BellyLight",new Vector2(0.045f,-0.031f)*length,new Vector2(0.42f,0.085f)*length,pearl,order);
            for(int side=-1;side<=1;side+=2)
            {
                var fin=Oval(fish,"PectoralFin",new Vector2(0.035f,side*0.14f)*length,new Vector2(0.18f,0.095f)*length,
                    new Color(pearl.r,pearl.g,pearl.b,0.42f),order-2);
                fin.transform.localEulerAngles=new Vector3(0,0,side*34);
                for(int ray=0;ray<3;ray++)
                {
                    var rib=Oval(fin.transform,"FinRay",Vector2.zero,new Vector2(0.70f,0.08f),new Color(shade.r,shade.g,shade.b,0.22f),order-1);
                    rib.transform.localEulerAngles=new Vector3(0,0,(ray-1)*18);
                }
                var gill=Oval(fish,"Gill",new Vector2(0.21f,side*0.044f)*length,new Vector2(0.023f,0.105f)*length,shade,order+2);
                gill.transform.localEulerAngles=new Vector3(0,0,side*16);
                Oval(fish,"EyeRim",new Vector2(0.29f,side*0.055f)*length,Vector2.one*length*0.043f,pearl,order+3);
                Oval(fish,"Pupil",new Vector2(0.295f,side*0.055f)*length,Vector2.one*length*0.024f,new Color(0.025f,0.055f,0.05f,0.9f),order+4);
            }
            for(int patch=0;patch<3;patch++)
            {
                float x=-0.19f+patch*0.14f;
                Color tone=(patch+variant)%2==0?pigment:pearl;
                var mark=Oval(fish,"ColorPatch",new Vector2(x,(patch%2==0?0.028f:-0.018f))*length,
                    new Vector2(0.115f,0.13f-patch*0.013f)*length,tone,order+1);
                mark.transform.localEulerAngles=new Vector3(0,0,patch*23+variant*11);
            }
            for(int row=0;row<2;row++)for(int scale=0;scale<4;scale++)
                Oval(fish,"ScaleGlint",new Vector2(-0.20f+scale*0.095f+row*0.018f,(row==0?0.044f:-0.045f))*length,
                    new Vector2(0.020f,0.008f)*length,new Color(1,0.95f,0.80f,0.24f),order+2);
            Oval(fish,"Mouth",new Vector2(0.355f,0)*length,new Vector2(0.012f,0.030f)*length,shade,order+3);
        }

        public static SpriteRenderer Oval(Transform parent,string name,Vector2 position,Vector2 size,Color color,int order)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;obj.transform.localScale=new Vector3(size.x,size.y,1);
            var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=RuntimeSpriteFactory.CircleSprite;
            renderer.color=color;renderer.sortingOrder=order;return renderer;
        }
    }
}
