Shader "TopDownGame/TeleportDeparture"
{
    Properties
    {
        _MainTex("Sprite", 2D) = "white" {}
        _Progress("Departure progress", Float) = 0
        _Clock("Scaled ceremony time", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha One
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Progress, _Clock;
            struct Attributes { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                float x = (input.uv.x - 0.5) * 5.4;
                float y = (input.uv.y - 0.5) * 7.4 + 2.1;
                float t = saturate(_Progress);
                float rise = smoothstep(0.12, 0.58, t);
                float height = lerp(0.1, 5.5, rise);
                float vertical = smoothstep(-0.5, 0.12, y) * (1 - smoothstep(height-0.9, height, y));
                float edge = (1 - smoothstep(2.1, 2.65, abs(x))) *
                    smoothstep(0, 0.06, input.uv.y) * (1 - smoothstep(0.92, 1, input.uv.y));
                float rays = 0;
                [unroll] for (int k = 0; k < 9; k++)
                {
                    float seed = k * 2.399;
                    float anchor = cos(seed) * 1.65;
                    // Parallel shafts: height and time affect brightness only,
                    // never the horizontal position or width of a ray.
                    float centre = anchor;
                    float width = 0.018 + 0.035 * (0.5+0.5*sin(seed));
                    float d = (x-centre) / width;
                    float ribbon = exp(-d*d) + 0.17 * exp(-d*d*0.045);
                    float shimmer = 0.68 + 0.32*sin(y*5-_Clock*7+seed);
                    rays += ribbon * shimmer;
                }
                float mist = exp(-x*x*1.5) * 0.12;
                float column = (rays*0.72 + mist) * vertical * rise;
                // Thin rising elliptical collars around the shaft, not a solid screen-covering flash.
                float collars = 0;
                [unroll] for (int j = 0; j < 3; j++)
                {
                    float phase = frac(_Clock*0.32+j/3.0);
                    float2 q = float2(x/1.75, (y-phase*4.2)/0.38);
                    float rim = abs(length(q)-1);
                    collars += (1-smoothstep(0.008, 0.036, rim)) * sin(phase*3.14159) * 0.30;
                }
                float ignition = smoothstep(0.76, 0.95, t);
                float core = exp(-x*x*12) * vertical * ignition * 0.75;
                float ground = exp(-x*x*0.9-y*y*8) * (rise*0.22+ignition*0.55);
                float alpha = saturate(column + collars*rise + core + ground) * edge;
                half3 tint = lerp(half3(0.22,0.8,0.64), half3(0.87,1,0.86), saturate(rays*0.4+ignition*0.65));
                return half4(tint, alpha * 0.85);
            }
            ENDHLSL
        }
    }
}
