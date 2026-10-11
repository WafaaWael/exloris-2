// Stylized top-down river water for URP 2D.
// Samples the water texture in world space (seamless no matter how the sprite is tiled or moved),
// blends two layers flowing at different speeds, wobbles the UVs with sine ripples and adds glints on the foam lines.
Shader "BoatGame/Water2D"
{
    Properties
    {
        [MainTexture] _MainTex ("Water Texture", 2D) = "white" {}
        _TileSize ("Tile Size (world units, xy)", Vector) = (4.017, 4.214, 0, 0)
        [Header(Flow)]
        _FlowA ("Layer A Flow (xy)", Vector) = (0, -0.12, 0, 0)
        _FlowB ("Layer B Flow (xy)", Vector) = (0.03, -0.2, 0, 0)
        _LayerBScale ("Layer B Scale", Float) = 1.37
        _LayerBBlend ("Layer B Blend", Range(0, 1)) = 0.45
        [Header(Ripples)]
        _RippleStrength ("Ripple Strength", Range(0, 0.2)) = 0.035
        _RippleFrequency ("Ripple Frequency", Float) = 1.6
        _RippleSpeed ("Ripple Speed", Float) = 1.2
        [Header(Color)]
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _DeepColor ("Deep Tint", Color) = (0.55, 0.8, 1, 1)
        _DeepAmount ("Deep Amount", Range(0, 1)) = 0.15
        _GlintIntensity ("Glint Intensity", Range(0, 2)) = 0.6
        _GlintSpeed ("Glint Speed", Float) = 2.5
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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TileSize;
                float4 _FlowA;
                float4 _FlowB;
                float _LayerBScale;
                float _LayerBBlend;
                float _RippleStrength;
                float _RippleFrequency;
                float _RippleSpeed;
                half4 _Tint;
                half4 _DeepColor;
                float _DeepAmount;
                float _GlintIntensity;
                float _GlintSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 worldUV : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.worldUV = positionWS.xy / _TileSize.xy;
                output.color = input.color;
                return output;
            }

            float2 Ripple(float2 uv, float t)
            {
                float f = _RippleFrequency * 6.2831;
                float2 offset;
                offset.x = sin(uv.y * f + t * _RippleSpeed) + 0.5 * sin(uv.y * f * 2.3 - t * _RippleSpeed * 1.7);
                offset.y = cos(uv.x * f * 1.3 + t * _RippleSpeed * 0.8) + 0.5 * cos(uv.x * f * 2.9 + t * _RippleSpeed * 1.3);
                return offset * _RippleStrength;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float2 uv = input.worldUV;
                float2 ripple = Ripple(uv, t);

                half4 a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + _FlowA.xy * t + ripple);
                half4 b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv * _LayerBScale + _FlowB.xy * t - ripple * 0.7 + 0.37);

                // "lighten" blend lets the second layer's bright foam lines drift over the first
                half3 col = lerp(a.rgb, max(a.rgb, b.rgb), _LayerBBlend);

                // slow large-scale variation so the river never looks like one repeated tile
                float deep = 0.5 + 0.5 * sin(uv.x * 0.9 + t * 0.15) * sin(uv.y * 0.7 - t * 0.1);
                col = lerp(col, col * _DeepColor.rgb, deep * _DeepAmount);

                // glints where both layers are bright (foam crossings), twinkling over time
                half lumA = dot(a.rgb, half3(0.299, 0.587, 0.114));
                half lumB = dot(b.rgb, half3(0.299, 0.587, 0.114));
                half glintMask = saturate((lumA + lumB - 1.65) * 4.0);
                half twinkle = 0.5 + 0.5 * sin(t * _GlintSpeed + uv.x * 17.0 + uv.y * 11.0);
                col += glintMask * twinkle * _GlintIntensity;

                return half4(col * _Tint.rgb * input.color.rgb, _Tint.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
