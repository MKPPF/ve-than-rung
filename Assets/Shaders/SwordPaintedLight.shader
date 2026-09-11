Shader "TopDownGame/SwordPaintedLight"
{
 Properties { [PerRendererData] _MainTex("Painted slash",2D)="black"{} }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off Blend One One
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct vary {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   vary vert(app i){vary o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color;return o;}
   half4 frag(vary i):SV_Target {
    half4 c=tex2D(_MainTex,i.uv);
    half3 light=max(0,c.rgb-.008)*i.color.rgb*i.color.a;
    return half4(light,0);
   }
   ENDHLSL
  }
 }
}
