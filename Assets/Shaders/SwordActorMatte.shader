Shader "TopDownGame/SwordActorMatte"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
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
    // Explicit magenta matte, not a fabricated alpha channel in the source file.
    float key=min(c.r,c.b)-c.g;
    float alpha=1-smoothstep(.18,.40,key);
    clip(alpha-.01);
    return half4(c.rgb*i.color.rgb,c.a*alpha*i.color.a);
   }
   ENDHLSL
  }
 }
}
