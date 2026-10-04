// Stylized side-view water for URP 2D (Nilometer pit).
// Drawn on a tall quad whose top edge is the water surface, on a sorting layer above everything it covers.
// It reads the already-rendered scene (_CameraSortingLayerTexture, enabled on the 2D renderer) and redraws it
// refracted: magnified toward the surface and gently wobbling, tinted from light near the top to deep blue below.
// On top: layered waves with a cartoon outline and foam, soft moving light, rising bubbles and surface twinkles.
Shader "BoatGame/WaterSide2D"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite (shape only)", 2D) = "white" {}
        _SurfaceColor ("Surface Tint", Color) = (0.35, 0.85, 0.95, 0.35)
        _DeepColor ("Deep Tint", Color) = (0.07, 0.36, 0.66, 0.85)
        _DepthFade ("Depth Fade (world units)", Float) = 5
        [Header(Refraction)]
        _Magnify ("Magnification", Range(1, 1.6)) = 1.18
        _WobbleAmount ("Wobble (world units)", Float) = 0.045
        _WobbleScale ("Wobble Scale", Float) = 2.4
        _WobbleSpeed ("Wobble Speed", Float) = 1.6
        [Header(Surface)]
        _SurfaceY ("Surface (object space, quad top)", Float) = 0.5
        _WaveHeight ("Wave Height (world)", Float) = 0.08
        _WaveLength ("Wave Length (world)", Float) = 2.2
        _WaveSpeed ("Wave Speed", Float) = 1.3
        _OutlineColor ("Outline Color", Color) = (0.1, 0.34, 0.5, 1)
        _OutlineWidth ("Outline Width (world)", Float) = 0.035
        _FoamColor ("Foam Color", Color) = (0.95, 1, 1, 1)
        _FoamWidth ("Foam Width (world)", Float) = 0.1
        [Header(Light and bubbles)]
        _LightStrength ("Soft Light Strength", Range(0, 1)) = 0.16
        _BubbleStrength ("Bubble Strength", Range(0, 1)) = 0.75
        _BubbleDensity ("Bubble Columns Per Unit", Float) = 0.9
        _SparkleStrength ("Surface Sparkle Strength", Range(0, 2)) = 0.9
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _SurfaceColor;
                half4 _DeepColor;
                float _DepthFade;
                float _Magnify;
                float _WobbleAmount;
                float _WobbleScale;
                float _WobbleSpeed;
                float _SurfaceY;
                float _WaveHeight;
                float _WaveLength;
                float _WaveSpeed;
                half4 _OutlineColor;
                float _OutlineWidth;
                half4 _FoamColor;
                float _FoamWidth;
                float _LightStrength;
                float _BubbleStrength;
                float _BubbleDensity;
                float _SparkleStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 positionWS : TEXCOORD0;
                float surfaceWS : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS.xy;
                output.surfaceWS = TransformObjectToWorld(float3(0, _SurfaceY, 0)).y;
                output.color = input.color;
                return output;
            }

            float Hash1(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            float Wave(float x, float t)
            {
                float k = 6.2831 / _WaveLength;
                return (sin(x * k + t * _WaveSpeed) * 0.55
                      + sin(x * k * 2.1 - t * _WaveSpeed * 1.3) * 0.25
                      + sin(x * k * 0.53 + t * _WaveSpeed * 0.7) * 0.35) * _WaveHeight;
            }

            // screen UV of a world position (handles platform Y flips)
            float2 WorldToScreenUV(float2 worldPos)
            {
                float4 cs = TransformWorldToHClip(float3(worldPos, 0.0));
                float4 sp = ComputeScreenPos(cs);
                return sp.xy / sp.w;
            }

            // rising bubbles: a few columns per unit, each with one bubble looping upward and swaying a little
            float Bubbles(float2 p, float surface, float t)
            {
                float col = floor(p.x * _BubbleDensity);
                float h = Hash1(col);
                float h2 = Hash1(col + 17.3);
                float speed = 0.35 + h2 * 0.45;
                float range = _DepthFade * 1.2;
                float rise = frac(t * speed / range + h);
                float by = surface - range + rise * range;
                float bx = (col + 0.25 + h * 0.5) / _BubbleDensity + sin(t * 2.0 + h * 6.2831) * 0.05;
                float radius = 0.035 + h2 * 0.05;
                float d = length(p - float2(bx, by));
                float ring = smoothstep(radius, radius * 0.75, d) - smoothstep(radius * 0.7, radius * 0.45, d) * 0.65;
                float shine = smoothstep(radius * 0.35, 0.0, length(p - float2(bx - radius * 0.35, by + radius * 0.35)));
                // fade in at the bottom, out just before reaching the surface
                float life = smoothstep(0.0, 0.15, rise) * (1.0 - smoothstep(0.85, 1.0, rise));
                return saturate(ring + shine) * life;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float2 p = input.positionWS;
                // waves sit just below the quad top so the crest is never clipped
                float surface = input.surfaceWS - _WaveHeight * 1.4 + Wave(p.x, t);
                float depth = surface - p.y;
                clip(depth);
                float depth01 = saturate(depth / _DepthFade);

                // refraction: things under water look closer to the surface (bigger) and sway with the water
                float2 wobble;
                wobble.x = sin(p.y * _WobbleScale + t * _WobbleSpeed) + 0.5 * sin(p.y * _WobbleScale * 2.3 - t * _WobbleSpeed * 1.4);
                wobble.y = sin(p.x * _WobbleScale * 0.8 + t * _WobbleSpeed * 0.9);
                wobble *= _WobbleAmount * saturate(depth * 4.0);
                float2 samplePos = float2(p.x, surface - depth / _Magnify) + wobble;
                half3 scene = SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, WorldToScreenUV(samplePos)).rgb;

                half4 tint = lerp(_SurfaceColor, _DeepColor, depth01 * depth01 * (3.0 - 2.0 * depth01));
                half3 col = lerp(scene, tint.rgb, tint.a);

                // soft drifting light patches near the surface (no hard lines)
                float light = sin(p.x * 1.3 + sin(p.y * 1.7 + t * 0.6) * 1.2 + t * 0.4) * sin(p.y * 2.1 - t * 0.5 + sin(p.x * 0.9 - t * 0.3));
                light = smoothstep(0.2, 1.0, light) * (1.0 - smoothstep(0.0, _DepthFade * 0.7, depth));
                col += light * _LightStrength;

                col += Bubbles(p, surface, t) * _BubbleStrength;
                col += Bubbles(p + float2(0.37, 0.0), surface, t * 0.8 + 3.0) * _BubbleStrength * 0.7;

                // foam band, twinkles on it, then the cartoon outline on the surface itself
                float foam = 1.0 - smoothstep(_OutlineWidth, _OutlineWidth + _FoamWidth, depth);
                col = lerp(col, _FoamColor.rgb, foam * 0.7);

                float sx = floor(p.x * 3.0);
                float sh = Hash1(sx);
                float twinkle = pow(saturate(sin(t * (1.5 + sh * 2.5) + sh * 6.2831)), 16.0);
                float dotMask = (1.0 - smoothstep(0.03, 0.09, abs(frac(p.x * 3.0) - sh * 0.6 - 0.2)))
                              * (1.0 - smoothstep(0.02, 0.07, abs(depth - _OutlineWidth - 0.04)));
                col += twinkle * dotMask * _SparkleStrength;

                float outline = 1.0 - smoothstep(_OutlineWidth * 0.6, _OutlineWidth, depth);
                col = lerp(col, _OutlineColor.rgb, outline);

                return half4(col, 1.0) * input.color;
            }
            ENDHLSL
        }
    }
}
