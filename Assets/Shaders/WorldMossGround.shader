Shader "TopDownGame/WorldMossGround"
{
    Properties { _MainTex ("Ground", 2D) = "white" {} _WalkMask ("Walkable blend", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _WalkMask;
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 world:TEXCOORD0; float4 color:COLOR; };
            v2f vert(appdata v) {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xy; o.color=v.color; return o;
            }
            half4 frag(v2f i):SV_Target {
                // Six world units per tile, shared coordinates across all floor rectangles.
                half3 a=tex2D(_MainTex,i.world/6).rgb;
                half3 b=tex2D(_MainTex,float2(-i.world.y,i.world.x)/9+0.31).rgb;
                float blend=0.2+0.13*sin(i.world.x*0.37)*sin(i.world.y*0.29);
                float2 uv=i.world/float2(44,30)+0.5;
                float edge=tex2D(_WalkMask,uv).r;
                float dapple=0.97+0.055*sin(i.world.x*0.61+i.world.y*0.39);
                half3 tint=lerp(half3(0.47,0.66,0.59),half3(1.08,1.12,0.99),edge);
                return half4(lerp(a,b,blend)*tint*dapple,i.color.a);
            }
            ENDHLSL
        }
    }
}
