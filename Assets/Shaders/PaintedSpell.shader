Shader "TopDownGame/PaintedSpell"
{
 Properties { [PerRendererData] _MainTex("Painted frames",2D)="black"{} _CellRect("UV rect",Vector)=(0,0,1,1) }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off Blend One One
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _CellRect;
   struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct vary {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
   vary vert(app i){vary o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color;return o;}
   half4 frag(vary i):SV_Target {
    half3 c=tex2D(_MainTex,i.uv).rgb;
    float2 local=(i.uv-_CellRect.xy)/_CellRect.zw;
    float2 border=min(local,1-local);
    float feather=smoothstep(0,.075,min(border.x,border.y));
    // Reject tinted dark backing and fade cell edges rather than showing a rectangular glow.
    float brightness=max(c.r,max(c.g,c.b));
    float alpha=smoothstep(.10,.28,brightness)*feather;
    return half4(max(0,c-.018)*alpha*i.color.rgb*i.color.a,0);
   }
   ENDHLSL
  }
 }
}
