// UI sprite with a glossy highlight band that sweeps across it every few seconds (stars, buttons, coins).
// Behaves like UI/Default otherwise (vertex color, masking, alpha from the sprite).
Shader "BoatGame/UIShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShineColor ("Shine Color", Color) = (1, 1, 0.9, 1)
        _ShineWidth ("Shine Width", Range(0.02, 0.5)) = 0.12
        _ShineStrength ("Shine Strength", Range(0, 2)) = 0.9
        _ShineInterval ("Seconds Between Sweeps", Float) = 2.8
        _ShineDuration ("Sweep Duration", Float) = 0.7
        _ShineAngle ("Slant", Range(-1, 1)) = 0.45
        _Phase ("Phase Offset", Float) = 0

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "CanUseSpriteAtlas" = "True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Blend SrcAlpha OneMinusSrcAlpha
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _ShineColor;
                float _ShineWidth;
                half _ShineStrength;
                float _ShineInterval;
                float _ShineDuration;
                float _ShineAngle;
                float _Phase;
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

            half4 Frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

                // sweep position runs from -0.5 to 1.5 during the sweep, then waits off-sprite until the next one
                float cycle = fmod(_Time.y + _Phase, _ShineInterval);
                float pos = lerp(-0.5, 1.5, saturate(cycle / _ShineDuration));
                float d = abs((input.uv.x + input.uv.y * _ShineAngle) - pos * (1.0 + abs(_ShineAngle)));
                half band = smoothstep(_ShineWidth, 0.0, d) * _ShineStrength;

                col.rgb += _ShineColor.rgb * band * col.a;
                return col;
            }
            ENDHLSL
        }
    }
}
