Shader "TopDownGame/DeepForestGround"
{
    Properties
    {
        _MainTex ("Leaf soil", 2D) = "white" {}
        _WalkMask ("Walkable blend", 2D) = "white" {}
    }
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
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.color = v.color;
                return o;
            }
            half4 frag(v2f i):SV_Target
            {
                // Continuous world coordinates keep merged floor rectangles invisible.
                half3 a = tex2D(_MainTex, i.world / 6).rgb;
                half3 b = tex2D(_MainTex, float2(-i.world.y, i.world.x) / 9 + 0.31).rgb;
                float edge = tex2D(_WalkMask, i.world / float2(44, 30) + 0.5).r;
                float variation = 0.22 + 0.12 * sin(i.world.x * 0.31) * sin(i.world.y * 0.27);
                half3 tint = lerp(half3(0.39, 0.60, 0.57), half3(1.16, 1.13, 1.01), edge);
                float light = 0.98 + 0.065 * sin(i.world.x * 0.44 + i.world.y * 0.29);
                return half4(lerp(a, b, variation) * tint * light, i.color.a);
            }
            ENDHLSL
        }
    }
}
