Shader "TopDownGame/PaintedForestColor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Original painting", 2D) = "white" {}
        _Strength ("Selective forest grade", Range(0,1)) = 1
        _Detail ("Bounded local detail", Range(0,0.4)) = 0.22
        _Clarity ("Screen-aware luminance clarity", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Strength, _Detail, _Clarity;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            float3 DisplayColor(float3 color)
            {
                #ifndef UNITY_COLORSPACE_GAMMA
                color = LinearToGammaSpace(color);
                #endif
                return color;
            }
            float3 RenderColor(float3 color)
            {
                #ifndef UNITY_COLORSPACE_GAMMA
                color = GammaToLinearSpace(color);
                #endif
                return color;
            }
            half4 frag(v2f i):SV_Target
            {
                float4 source = tex2D(_MainTex, i.uv);
                float3 c = DisplayColor(source.rgb);
                // One screen-pixel neighbourhood, capped to two source texels.
                // Work on luminance, not independent RGB channels: preserve hue.
                float2 radius = clamp(abs(ddx(i.uv)) + abs(ddy(i.uv)),
                    _MainTex_TexelSize.xy, _MainTex_TexelSize.xy * 2);
                float3 weights = float3(0.2126,0.7152,0.0722);
                float center = dot(c,weights);
                float n = dot(DisplayColor(tex2D(_MainTex,i.uv+float2(0,radius.y)).rgb),weights);
                float s = dot(DisplayColor(tex2D(_MainTex,i.uv-float2(0,radius.y)).rgb),weights);
                float e = dot(DisplayColor(tex2D(_MainTex,i.uv+float2(radius.x,0)).rgb),weights);
                float w = dot(DisplayColor(tex2D(_MainTex,i.uv-float2(radius.x,0)).rgb),weights);
                float lo = min(center,min(min(n,s),min(e,w)));
                float hi = max(center,max(max(n,s),max(e,w)));
                float contrast = hi-lo;
                // Suppress flat-area noise and strong edges; limit the added
                // contrast to 2.5% and never overshoot the local luminance range.
                float gate = smoothstep(0.006,0.035,contrast)*(1-smoothstep(0.22,0.5,contrast));
                float delta = clamp(center-(n+s+e+w)*0.25,-0.025,0.025)*_Clarity*gate;
                float resolved = clamp(center+delta,lo,hi);
                c = saturate(c * ((resolved+0.001)/(center+0.001)));
                float luma = dot(c, float3(0.2126, 0.7152, 0.0722));
                // Preserve the already saturated cyan pond and waterfall. Soft
                // chroma masks avoid boundaries or changes to authored geometry.
                float water = smoothstep(0.025, 0.12, c.g-c.r) * smoothstep(0.015, 0.09, c.b-c.r);
                float foliage = smoothstep(0.87, 1.04, c.g/max(c.r,0.03)) *
                    smoothstep(1.12, 1.65, c.g/max(c.b,0.03)) *
                    (1-smoothstep(0.54,0.76,luma)) * (1-water);
                float earth = smoothstep(0.02,0.12,c.r-c.b) *
                    (1-smoothstep(0.99,1.1,c.g/max(c.r,0.03))) * (1-foliage) * (1-water);
                float3 graded = c * lerp(float3(1,1,1),float3(0.70,1.025,1.08),foliage);
                graded *= lerp(float3(1,1,1),float3(0.91,0.96,1.06),earth);
                float luminance = dot(graded,float3(0.2126,0.7152,0.0722));
                graded = lerp(luminance.xxx,graded,1+0.12*foliage);
                // Deeper mid-shadows, with a protected black floor and gentle
                // highlights rather than boosting saturation across the screen.
                graded = (graded-0.43)*1.12+0.43;
                graded = max(graded,c*0.72);
                float2 t = _MainTex_TexelSize.xy;
                float3 neighbours = (DisplayColor(tex2D(_MainTex,i.uv+float2(t.x,0)).rgb)+
                    DisplayColor(tex2D(_MainTex,i.uv-float2(t.x,0)).rgb)+
                    DisplayColor(tex2D(_MainTex,i.uv+float2(0,t.y)).rgb)+
                    DisplayColor(tex2D(_MainTex,i.uv-float2(0,t.y)).rgb))*0.25;
                // A small bounded detail boost is not an invented resolution upgrade.
                graded += clamp(c-neighbours,-0.055,0.055)*_Detail;
                graded = lerp(graded,c,water);
                return half4(RenderColor(saturate(lerp(c,graded,_Strength)))*i.color.rgb,source.a*i.color.a);
            }
            ENDHLSL
        }
    }
}
