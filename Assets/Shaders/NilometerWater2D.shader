// Nilometer 2D Water – sprite shader for Unity (Built-in RP and URP / URP 2D Renderer).
// Put it on a SpriteRenderer that uses the white "WaterQuad" sprite (or any sprite),
// stretched over the column. Drive _FillLevel from NilometerWaterLevel.cs.
//
// All sizes (wave height, foam width, depth fade, bubbles) are measured in
// "water widths": 1.0 = the width of the water sprite. _Aspect (height/width)
// is set automatically by the script so nothing stretches on a tall column.

Shader "Nilometer/Water2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Level)]
        _FillLevel   ("Fill Level (0 = empty, 1 = full)", Range(0, 1)) = 0.5
        _Aspect      ("Aspect (height / width) – set by script", Float) = 1

        [Header(Colour)]
        _ShallowColor ("Shallow Colour", Color) = (0.50, 0.88, 0.93, 0.50)
        _DeepColor    ("Deep Colour",    Color) = (0.10, 0.42, 0.60, 0.72)
        _DepthFade    ("Depth Fade (widths)", Float) = 4.0

        [Header(Waves)]
        _WaveAmp   ("Wave Height (widths)", Range(0, 0.2)) = 0.035
        _WaveFreq  ("Waves Across Width", Float) = 1.3
        _WaveSpeed ("Wave Speed", Float) = 2.2
        _SplashAmp ("Splash (set by script)", Range(0, 0.3)) = 0

        [Header(Foam)]
        _FoamColor ("Foam Colour", Color) = (1, 1, 1, 1)
        _FoamWidth ("Foam Width (widths)", Range(0, 0.2)) = 0.045
        _LineColor ("Under-foam Line Colour", Color) = (0.05, 0.30, 0.45, 0.6)

        [Header(Shimmer)]
        _ShimmerStrength ("Shimmer Strength", Range(0, 1)) = 0.35
        _ShimmerScale    ("Shimmer Scale", Float) = 3.0
        _ShimmerSpeed    ("Shimmer Speed", Float) = 0.5

        [Header(Bubbles)]
        _BubbleAmount ("Bubble Amount", Range(0, 1)) = 0.35
        _BubbleScale  ("Bubble Grid (per width)", Float) = 4.0
        _BubbleSpeed  ("Bubble Rise Speed", Float) = 0.6
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float  _FillLevel, _Aspect;
            fixed4 _ShallowColor, _DeepColor;
            float  _DepthFade;
            float  _WaveAmp, _WaveFreq, _WaveSpeed, _SplashAmp;
            fixed4 _FoamColor, _LineColor;
            float  _FoamWidth;
            float  _ShimmerStrength, _ShimmerScale, _ShimmerSpeed;
            float  _BubbleAmount, _BubbleScale, _BubbleSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            float hash21 (float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise (float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                // q: x in [0,1] across the width, y in widths (0.._Aspect)
                float2 q = float2(i.uv.x, i.uv.y * _Aspect);

                // ---------- surface ----------
                float k = 6.28318 * _WaveFreq;
                float calm = sin(q.x * k        + t * _WaveSpeed)        * 0.60
                           + sin(q.x * k * 2.3  - t * _WaveSpeed * 1.4)  * 0.25
                           + sin(q.x * k * 0.55 + t * _WaveSpeed * 0.7)  * 0.15;
                // splash: faster, choppier sway plus a side-to-side slosh
                float chop = sin(q.x * k * 3.1 - t * 9.0) * 0.5
                           + (q.x - 0.5) * 2.0 * sin(t * 7.0) * 0.5;
                float wave = (calm * _WaveAmp + chop * _SplashAmp) * saturate(_FillLevel * 25.0); // flat when empty
                float surface = _FillLevel * _Aspect + wave;

                float d  = surface - q.y;                    // >0 = under water (in widths)
                float aa = max(fwidth(q.y) * 1.5, 1e-4);     // anti-aliased edge
                float body = smoothstep(0.0, aa, d);
                if (_FillLevel <= 0.0001) body = 0.0;

                // ---------- colour by depth ----------
                float depth01 = saturate(d / max(_DepthFade, 1e-3));
                fixed4 col = lerp(_ShallowColor, _DeepColor, depth01);

                // ---------- shimmer (horizontal light streaks) ----------
                float2 sp = float2(q.x * _ShimmerScale * 0.6 + t * _ShimmerSpeed,
                                   q.y * _ShimmerScale * 2.5 - t * _ShimmerSpeed * 0.3);
                float n = valueNoise(sp) * 0.65 + valueNoise(sp * 2.1 + 7.3) * 0.35;
                float streak = smoothstep(0.62, 0.78, n) * (1.0 - depth01 * 0.6);
                col.rgb += streak * _ShimmerStrength;

                // ---------- bubbles ----------
                float2 bp   = float2(q.x * _BubbleScale, q.y * _BubbleScale - t * _BubbleSpeed * _BubbleScale * 0.5);
                float2 cell = floor(bp);
                float2 f    = frac(bp);
                float  h    = hash21(cell);
                float2 c    = float2(0.25 + 0.5 * hash21(cell + 3.7) + sin(t * 2.0 + h * 6.28) * 0.08, 0.5);
                float  r    = 0.07 + 0.08 * hash21(cell + 9.1);
                float  len  = length(f - c);
                float  ring = smoothstep(r, r - 0.03, len) - 0.6 * smoothstep(r - 0.03, r - 0.06, len);
                float  bubble = ring * step(h, _BubbleAmount) * smoothstep(0.02, 0.25, d); // fade near surface
                col.rgb = lerp(col.rgb, float3(1, 1, 1), bubble * 0.55);

                // ---------- foam line on top + darker line under it ----------
                float foam = 1.0 - smoothstep(_FoamWidth - aa, _FoamWidth, d);
                float line2 = (1.0 - smoothstep(_FoamWidth * 1.9 - aa, _FoamWidth * 1.9, d)) * (1.0 - foam);
                col.rgb = lerp(col.rgb, _LineColor.rgb, line2 * _LineColor.a);
                col = lerp(col, _FoamColor, foam * _FoamColor.a);

                // ---------- sprite texture + vertex tint ----------
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;
                col.rgb *= tex.rgb;
                col.a   *= tex.a * body;
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
