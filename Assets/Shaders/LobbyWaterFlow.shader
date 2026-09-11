Shader "TopDownGame/LobbyWaterFlow"
{
    Properties
    {
        _MainTex ("Painted water source", 2D) = "white" {}
        _FlowTime ("Local animation time", Float) = 0
        _Fall ("Waterfall", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _FlowTime;
            float _Fall;
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 river : TEXCOORD1;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 river : TEXCOORD1;
            };
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.river = input.river;
                return output;
            }
            half4 frag(v2f input) : SV_Target
            {
                float t = _FlowTime;
                float edge = smoothstep(0,0.2,input.river.x) *
                    smoothstep(0,0.2,1-input.river.x);
                float ends = smoothstep(0,0.13,input.river.y) *
                    smoothstep(0,0.16,1-input.river.y);
                float mask = edge * ends;
                float wave = sin(input.river.x*9 + input.river.y*12-t*2.1);
                float wave2 = sin(input.river.x*15-input.river.y*8+t*1.4);
                float2 displacement = float2(wave*0.0004, wave2*0.0005)*mask;
                displacement.y += sin(input.river.y*18-t*6.5)*0.00065*_Fall*mask;
                half3 original = tex2D(_MainTex,input.uv + displacement).rgb;
                float crest = pow(saturate(sin(input.river.y*32-t*3.0+wave*0.25)), 16);
                float broken = smoothstep(0.4,0.95,sin(input.river.x*25+wave2));
                float cascade = pow(saturate(sin(input.river.x*28 + wave2*0.2)),5) *
                    (0.45 + 0.55*pow(saturate(sin(input.river.y*26-t*9)),3));
                half3 shimmer = half3(0.34,0.66,0.69)*crest*broken*0.065;
                shimmer += half3(0.6,0.84,0.87)*cascade*_Fall*0.15;
                return half4(original + shimmer, mask*0.9);
            }
            ENDHLSL
        }
    }
}
