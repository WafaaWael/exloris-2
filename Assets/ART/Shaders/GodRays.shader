// Procedural sun shafts: soft light stripes that fan out from the top of the quad, fade toward the bottom,
// and slowly sway and shimmer. Additive, so it only brightens. Works on SpriteRenderers and UI Images.
Shader "BoatGame/GodRays"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (unused)", 2D) = "white" {}
        _Color ("Ray Color", Color) = (1, 0.93, 0.7, 1)
        _Intensity ("Intensity", Range(0, 2)) = 0.35
        _RayCount ("Ray Density", Float) = 7
        _Spread ("Fan Spread", Range(0, 1)) = 0.35
        _Speed ("Sway Speed", Float) = 0.25
        _Softness ("Ray Softness", Range(0.5, 6)) = 2.2
        _FadeLength ("Length Falloff", Range(0.5, 4)) = 1.6
        _FadeWidth ("Width Falloff", Range(0.5, 4)) = 1.8
        _TopBias ("Brighter Toward Top", Range(0, 1)) = 0.4

        // UI masking support
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                float _RayCount;
                float _Spread;
                float _Speed;
                float _Softness;
                float _FadeLength;
                float _FadeWidth;
                float _TopBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float RayPattern(float x, float t)
            {
                // layered sines at unrelated frequencies = irregular, slowly shifting stripes
                float r = sin(x * _RayCount * 6.2831 + t * 0.7) * 0.5 + 0.5;
                r *= sin(x * _RayCount * 2.3 * 6.2831 - t * 1.1 + 1.7) * 0.5 + 0.5;
                r += (sin(x * _RayCount * 0.61 * 6.2831 + t * 0.4 + 4.0) * 0.5 + 0.5) * 0.6;
                return pow(saturate(r * 0.7), _Softness);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 uv = input.uv;
                // fan out: rays converge toward the top edge
                float fanX = (uv.x - 0.5) / lerp(1.0, 1.0 - _Spread, uv.y) + 0.5;
                float rays = RayPattern(fanX, t);

                // sine falloff reaches exactly zero on all four edges, so the quad never shows a hard border
                float along = pow(sin(uv.y * PI), _FadeLength);
                float across = pow(sin(uv.x * PI), _FadeWidth);
                float fade = along * across * lerp(1.0 - _TopBias, 1.0, uv.y);
                float shimmer = 0.85 + 0.15 * sin(_Time.y * 1.3 + uv.x * 3.0);

                half a = saturate(rays * fade * shimmer * _Intensity) * input.color.a;
                return half4(_Color.rgb * input.color.rgb, a);
            }
            ENDHLSL
        }
    }
}
