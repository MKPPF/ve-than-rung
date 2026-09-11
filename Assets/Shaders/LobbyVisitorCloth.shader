Shader "TopDownGame/LobbyVisitorCloth"
{
    Properties { [PerRendererData] _MainTex("Sprite",2D)="white" {} _Cloth("Cloak",Color)=(0.4,0.65,0.35,1) }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _Cloth;
            struct input {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            half4 frag(output i):SV_Target
            {
                half4 c=tex2D(_MainTex,i.uv)*i.color;
                float mask=smoothstep(0.02,0.14,c.b-c.r)*smoothstep(0.01,0.08,c.b-c.g);
                float brightness=max(c.r,max(c.g,c.b));
                c.rgb=lerp(c.rgb,_Cloth.rgb*brightness,mask);
                return c;
            }
            ENDHLSL
        }
    }
}
