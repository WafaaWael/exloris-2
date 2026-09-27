// Made with Amplify Shader Editor v1.9.9.9
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cainos/Interactive Pixel Water/Pixel Water"
{
	Properties
	{
		_WaterColorShallow( "Water Color Shallow", Color ) = ( 0.7450981, 0.9333333, 0.827451, 0 )
		_WaterColorDeep( "Water Color Deep", Color ) = ( 0.4431373, 0.6117647, 0.5215687, 0.3764706 )
		_UnderwaterTintShallow( "Underwater Tint Shallow", Color ) = ( 0.9098039, 0.9921569, 0.9686275, 1 )
		_UnderwaterTintDeep( "Underwater Tint Deep", Color ) = ( 0.3568628, 0.5843138, 0.4509804, 1 )
		_SurfaceColorUpper( "Surface Color Upper", Color ) = ( 0.8901961, 0.9921569, 1, 0.1960784 )
		_SurfaceColorLower( "Surface Color Lower", Color ) = ( 0.2039216, 0.3686275, 0.2941177, 0.03921569 )
		_SurfaceThicknessUpper( "Surface Thickness Upper", Float ) = 1
		_SurfaceThicknessLower( "Surface Thickness Lower", Float ) = 1
		_SurfaceDistortionMul( "Surface Distortion Mul", Float ) = 5
		_DistortionSpeed( "Distortion Speed", Float ) = 1
		_DistortionScale( "Distortion Scale", Float ) = 0.7
		_DistortionStrength( "Distortion Strength", Float ) = 0.5
		_BlurAmountShallow( "Blur Amount Shallow", Float ) = 2
		_BlurAmountDeep( "Blur Amount Deep", Float ) = 12
		_LightShaftColor( "Light Shaft Color", Color ) = ( 0.2235294, 0.3215686, 0.2745098, 0.3921569 )
		_LightShaftScale( "Light Shaft Scale", Float ) = 1.4
		_LightShaftPower( "Light Shaft Power", Float ) = 2
		_LightShaftTilt( "Light Shaft Tilt", Float ) = -0.2
		_LightShaftDepth( "Light Shaft Depth", Float ) = 2
		_LightShaftSpeed( "Light Shaft Speed", Float ) = 0.7
		_AmbientWaveMul( "Ambient Wave Mul", Float ) = 1
		_AmbientWaveSpeed( "Ambient Wave Speed", Vector ) = ( 0, 0, 0, 0 )
		_AmbientWaveFrequency( "Ambient Wave Frequency", Vector ) = ( 0, 0, 0, 0 )
		_AmbientWaveAmptitude( "Ambient Wave Amptitude", Vector ) = ( 0, 0, 0, 0 )
		_StencilReference( "Stencil Reference", Int ) = 123


		[HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
	}

	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.universal": "[17.0,18.0]"
		}

		

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Lit" "ShaderGraphShader"="true" }

	LOD 0

		Cull Off

		HLSLINCLUDE
		#pragma target 2.0
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		ENDHLSL

		
		Pass
		{
			Name "Sprite Lit"
            Tags { "LightMode"="Universal2D" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite On
			Offset 0 , 0
			ColorMask RGBA

			Stencil
			{
				Ref [_StencilReference]
				Pass Replace
			}

			HLSLPROGRAM

			#define _DISABLE_COLOR_TINT
			#define ASE_VERSION 19909
			#define ASE_SRP_VERSION 170400


			#if ( UNITY_VERSION >= 60010000 )
			#pragma multi_compile_instancing
			#endif

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

			#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_0
			#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_1
			#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_2
			#pragma multi_compile _ USE_SHAPE_LIGHT_TYPE_3

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define VARYINGS_NEED_SCREENPOSITION
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITELIT

			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if ( UNITY_VERSION < 60030000 )
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			#if USE_SHAPE_LIGHT_TYPE_0
			SHAPE_LIGHT(0)
			#endif

			#if USE_SHAPE_LIGHT_TYPE_1
			SHAPE_LIGHT(1)
			#endif

			#if USE_SHAPE_LIGHT_TYPE_2
			SHAPE_LIGHT(2)
			#endif

			#if USE_SHAPE_LIGHT_TYPE_3
			SHAPE_LIGHT(3)
			#endif
			#endif

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			half4 _RendererColor;

			sampler2D _BehindWaterTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _AmbientWaveAmptitude;
			float4 _AmbientWaveFrequency;
			float4 _AmbientWaveSpeed;
			float4 _SurfaceColorLower;
			float4 _SurfaceColorUpper;
			float4 _WaterColorDeep;
			float4 _WaterColorShallow;
			float4 _LightShaftColor;
			float4 _UnderwaterTintShallow;
			float4 _UnderwaterTintDeep;
			int _StencilReference;
			float _LightShaftPower;
			float _LightShaftScale;
			float _LightShaftSpeed;
			float _LightShaftTilt;
			float _BlurAmountDeep;
			float _SurfaceThicknessLower;
			float _SurfaceThicknessUpper;
			float _SurfaceDistortionMul;
			float _DistortionSpeed;
			float _DistortionScale;
			float _DistortionStrength;
			float _AmbientWaveMul;
			float _BlurAmountShallow;
			float _LightShaftDepth;
			CBUFFER_END


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float4 screenPosition : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
				float _EnableAlphaTexture;
			#endif

			float AmbientWaveSum( float4 WaveAmplitude, float4 WaveFrequency, float4 WaveSpeed, float Time, float3 VertexPos )
			{
				float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);
				float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);
				float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);
				return wave1 + wave2 + wave3;
			}
			
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
					float2 voronoihash35( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi35( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash35( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
			float2 ASESafeNormalize(float2 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float4 GaussianBlur( float4 GrabScreen, float2 TexelSize, float2 UV, float Blur )
			{
				    float4 col = float4(0.0, 0.0, 0.0, 0.0);
				    float kernelSum = 0.0;
				    int upper = ((Blur - 1) / 2);
				    int lower = -upper;
				    for (int x = lower; x <= upper; ++x)
				    {
				        for (int y = lower; y <= upper; ++y)
				        {
				            kernelSum ++;
				            float2 offset = float2(TexelSize.x * x, TexelSize.y * y);
				            col += tex2Dlod(_BehindWaterTex, float4(UV + offset,0,0));
				        }
				    }
				    col /= kernelSum;
				    return float4(col.r, col.g, col.b, col.a);
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				SetUpSpriteInstanceProperties();
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 P_Ambient_Wave_Amptitude239 = _AmbientWaveAmptitude;
				float4 WaveAmplitude251 = P_Ambient_Wave_Amptitude239;
				float4 P_Ambient_Wave_Frequency260 = _AmbientWaveFrequency;
				float4 WaveFrequency251 = P_Ambient_Wave_Frequency260;
				float4 P_Ambient_Wave_Speed228 = _AmbientWaveSpeed;
				float4 WaveSpeed251 = P_Ambient_Wave_Speed228;
				float Time251 = _TimeParameters.x;
				float3 VertexPos251 = v.positionOS;
				float localAmbientWaveSum251 = AmbientWaveSum( WaveAmplitude251 , WaveFrequency251 , WaveSpeed251 , Time251 , VertexPos251 );
				float temp_output_281_0 = ( _TimeParameters.x * P_Ambient_Wave_Speed228.w );
				float2 appendResult288 = (float2(temp_output_281_0 , temp_output_281_0));
				float3 ase_positionWS = TransformObjectToWorld( ( v.positionOS ).xyz );
				float simplePerlin2D275 = snoise( ( float3( appendResult288 ,  0.0 ) + ase_positionWS ).xy*float4( 0,0,0,0 ).w );
				simplePerlin2D275 = simplePerlin2D275*0.5 + 0.5;
				float P_Ambient_Wave_Mul267 = _AmbientWaveMul;
				float2 appendResult243 = (float2(0.0 , ( ( localAmbientWaveSum251 + ( simplePerlin2D275 * P_Ambient_Wave_Amptitude239.w ) ) * ( 1.0 - v.ase_texcoord1.y ) * v.uv0.x * P_Ambient_Wave_Mul267 )));
				float2 M_Ambient_Wave_Offset242 = appendResult243;
				
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord4 = screenPos;
				
				o.ase_texcoord5.xy = v.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord5.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = float3( M_Ambient_Wave_Offset242 ,  0.0 );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				o.positionWS = vertexInput.positionWS;
				o.texCoord0 = v.uv0;
				o.color = v.color * _RendererColor * unity_SpriteColor;
				o.screenPosition = vertexInput.positionNDC;
				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 positionCS = IN.positionCS;
				float3 positionWS = IN.positionWS;

				float4 screenPos = IN.ase_texcoord4;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 GrabScreen84 = tex2D( _BehindWaterTex, ase_grabScreenPosNorm.xy );
				float2 TexelSize84 = ( 1.0 / _ScaledScreenParams ).xy;
				float P_Distortion_Strength52 = _DistortionStrength;
				float P_Distortion_Scale46 = _DistortionScale;
				float P_Distortion_Speed42 = _DistortionSpeed;
				float time35 = ( _TimeParameters.x * P_Distortion_Speed42 );
				float2 voronoiSmoothId35 = 0;
				float2 P_Distortion_Direction39 = float2( 1,1 );
				float2 normalizeResult78 = ASESafeNormalize( P_Distortion_Direction39 );
				float2 coords35 = (positionWS*1.0 + float3( ( normalizeResult78 * _TimeParameters.x ) ,  0.0 )).xy * ( 1.0 / P_Distortion_Scale46 );
				float2 id35 = 0;
				float2 uv35 = 0;
				float voroi35 = voronoi35( coords35, time35, id35, uv35, 0, voronoiSmoothId35 );
				float P_Surface_Distortion_Mul126 = _SurfaceDistortionMul;
				float2 texCoord12 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Upper11 = _SurfaceThicknessUpper;
				float M_Surface_Mask_01121 = step( texCoord12.y , ( P_Surface_Thickness_Upper11 * 0.03125 ) );
				float2 texCoord129 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Lower151 = _SurfaceThicknessLower;
				float M_Surface_Mask_02133 = ( step( texCoord129.y , ( 0.03125 * ( P_Surface_Thickness_Lower151 + P_Surface_Thickness_Upper11 ) ) ) + ( M_Surface_Mask_01121 * -1.0 ) );
				float M_Surface_Mask_Combined155 = ( M_Surface_Mask_01121 + M_Surface_Mask_02133 );
				float lerpResult123 = lerp( 1.0 , P_Surface_Distortion_Mul126 , M_Surface_Mask_Combined155);
				float M_Distortion_Offset106 = ( 0.01 * P_Distortion_Strength52 * voroi35 * lerpResult123 );
				float2 UV84 = ( ase_grabScreenPosNorm + M_Distortion_Offset106 ).xy;
				float P_Blur_Amount_Shallow104 = _BlurAmountShallow;
				float P_Blur_Amount_Deep205 = _BlurAmountDeep;
				float M_Depth211 = IN.ase_texcoord5.xy.y;
				float lerpResult203 = lerp( P_Blur_Amount_Shallow104 , P_Blur_Amount_Deep205 , M_Depth211);
				float Blur84 = lerpResult203;
				float4 localGaussianBlur84 = GaussianBlur( GrabScreen84 , TexelSize84 , UV84 , Blur84 );
				float4 M_Under_Water_Blur88 = localGaussianBlur84;
				float4 P_Underwater_Tint_Shallow194 = _UnderwaterTintShallow;
				float4 P_Underwater_Tint_Deep196 = _UnderwaterTintDeep;
				float4 lerpResult200 = lerp( P_Underwater_Tint_Shallow194 , P_Underwater_Tint_Deep196 , M_Depth211);
				float4 M_Under_Water_Color201 = lerpResult200;
				float4 P_Water_Color_Shallow9 = _WaterColorShallow;
				float4 P_Water_Color_Deep168 = _WaterColorDeep;
				float4 lerpResult181 = lerp( P_Water_Color_Shallow9 , P_Water_Color_Deep168 , M_Depth211);
				float4 M_Water_Color183 = lerpResult181;
				float4 P_Surface_Color_Upper10 = _SurfaceColorUpper;
				float4 lerpResult157 = lerp( M_Water_Color183 , P_Surface_Color_Upper10 , M_Surface_Mask_01121);
				float4 P_Surface_Color_Lower165 = _SurfaceColorLower;
				float4 lerpResult161 = lerp( lerpResult157 , P_Surface_Color_Lower165 , M_Surface_Mask_02133);
				float4 temp_output_2_0_g3 = lerpResult161;
				float4 lerpResult65 = lerp( ( M_Under_Water_Blur88 * M_Under_Water_Color201 ) , float4( (temp_output_2_0_g3).rgb , 0.0 ) , (temp_output_2_0_g3).a);
				float P_Light_Shaft_Tilt350 = _LightShaftTilt;
				float P_Light_Shaft_Speed317 = _LightShaftSpeed;
				float temp_output_324_0 = ( _TimeParameters.x * ( P_Light_Shaft_Speed317 * -0.75 ) );
				float2 appendResult325 = (float2(( ( positionWS.x + ( positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_324_0 ) , ( temp_output_324_0 * -0.5 )));
				float P_Light_Shaft_Scale307 = _LightShaftScale;
				float simplePerlin2D320 = snoise( appendResult325*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D320 = simplePerlin2D320*0.5 + 0.5;
				float P_Light_Shaft_Power310 = _LightShaftPower;
				float temp_output_315_0 = ( _TimeParameters.x * P_Light_Shaft_Speed317 );
				float2 appendResult300 = (float2(( ( positionWS.x + ( positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_315_0 ) , ( temp_output_315_0 * -1.0 )));
				float simplePerlin2D299 = snoise( appendResult300*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D299 = simplePerlin2D299*0.5 + 0.5;
				float4 P_Light_Shaft_Color359 = _LightShaftColor;
				float4 temp_output_2_0_g2 = P_Light_Shaft_Color359;
				float P_Light_Shaft_Depth347 = _LightShaftDepth;
				float clampResult343 = clamp( ( IN.texCoord0.xy.y / P_Light_Shaft_Depth347 ) , 0.0 , 1.0 );
				float4 M_Light_Shaft_Color_Final296 = ( ( pow( simplePerlin2D320 , P_Light_Shaft_Power310 ) + pow( simplePerlin2D299 , P_Light_Shaft_Power310 ) ) * P_Light_Shaft_Color359 * (temp_output_2_0_g2).a * ( 1.0 - clampResult343 ) );
				float4 break67 = ( lerpResult65 + M_Light_Shaft_Color_Final296 );
				float4 appendResult68 = (float4(break67.x , break67.y , break67.z , 1.0));
				

				float4 Color = appendResult68;
				float4 Mask = float4(1,1,1,1);
				float3 Normal = float3( 0, 0, 1 );
				float AlphaClipThreshold = 0.5;

			#if defined( ALPHA_CLIP_THRESHOLD )
				clip( Color.a - AlphaClipThreshold );
			#endif

			#if ETC1_EXTERNAL_ALPHA
				float4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, IN.texCoord0.xy);
				Color.a = lerp( Color.a, alpha.r, _EnableAlphaTexture);
			#endif

			#if !defined( _DISABLE_COLOR_TINT )
				Color *= IN.color;
			#endif

				SurfaceData2D surfaceData;
				InitializeSurfaceData(Color.rgb, Color.a, Mask, surfaceData);
				InputData2D inputData;
				InitializeInputData(IN.texCoord0.xy, half2(IN.screenPosition.xy / IN.screenPosition.w), inputData);

			#if defined(DEBUG_DISPLAY)
				SETUP_DEBUG_DATA_2D(inputData, positionWS, positionCS);
			#endif

				return CombinedShapeLightShared(surfaceData, inputData);
			}

			ENDHLSL
		}

		
		Pass
		{
			
            Name "Sprite Normal"
            Tags { "LightMode"="NormalsRendering" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite On
			Offset 0 , 0
			ColorMask RGBA

			Stencil
			{
				Ref [_StencilReference]
				Pass Replace
			}

			HLSLPROGRAM

			#define _DISABLE_COLOR_TINT
			#define ASE_VERSION 19909
			#define ASE_SRP_VERSION 170400


			#if ( UNITY_VERSION >= 60010000 )
				#pragma multi_compile_instancing
			#endif

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ SKINNED_SPRITE

			#define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITENORMAL

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			sampler2D _BehindWaterTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _AmbientWaveAmptitude;
			float4 _AmbientWaveFrequency;
			float4 _AmbientWaveSpeed;
			float4 _SurfaceColorLower;
			float4 _SurfaceColorUpper;
			float4 _WaterColorDeep;
			float4 _WaterColorShallow;
			float4 _LightShaftColor;
			float4 _UnderwaterTintShallow;
			float4 _UnderwaterTintDeep;
			int _StencilReference;
			float _LightShaftPower;
			float _LightShaftScale;
			float _LightShaftSpeed;
			float _LightShaftTilt;
			float _BlurAmountDeep;
			float _SurfaceThicknessLower;
			float _SurfaceThicknessUpper;
			float _SurfaceDistortionMul;
			float _DistortionSpeed;
			float _DistortionScale;
			float _DistortionStrength;
			float _AmbientWaveMul;
			float _BlurAmountShallow;
			float _LightShaftDepth;
			CBUFFER_END


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float4 tangentWS : TEXCOORD3;
				float3 bitangentWS : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float AmbientWaveSum( float4 WaveAmplitude, float4 WaveFrequency, float4 WaveSpeed, float Time, float3 VertexPos )
			{
				float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);
				float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);
				float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);
				return wave1 + wave2 + wave3;
			}
			
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
					float2 voronoihash35( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi35( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash35( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
			float2 ASESafeNormalize(float2 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float4 GaussianBlur( float4 GrabScreen, float2 TexelSize, float2 UV, float Blur )
			{
				    float4 col = float4(0.0, 0.0, 0.0, 0.0);
				    float kernelSum = 0.0;
				    int upper = ((Blur - 1) / 2);
				    int lower = -upper;
				    for (int x = lower; x <= upper; ++x)
				    {
				        for (int y = lower; y <= upper; ++y)
				        {
				            kernelSum ++;
				            float2 offset = float2(TexelSize.x * x, TexelSize.y * y);
				            col += tex2Dlod(_BehindWaterTex, float4(UV + offset,0,0));
				        }
				    }
				    col /= kernelSum;
				    return float4(col.r, col.g, col.b, col.a);
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				SetUpSpriteInstanceProperties();
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 P_Ambient_Wave_Amptitude239 = _AmbientWaveAmptitude;
				float4 WaveAmplitude251 = P_Ambient_Wave_Amptitude239;
				float4 P_Ambient_Wave_Frequency260 = _AmbientWaveFrequency;
				float4 WaveFrequency251 = P_Ambient_Wave_Frequency260;
				float4 P_Ambient_Wave_Speed228 = _AmbientWaveSpeed;
				float4 WaveSpeed251 = P_Ambient_Wave_Speed228;
				float Time251 = _TimeParameters.x;
				float3 VertexPos251 = v.positionOS;
				float localAmbientWaveSum251 = AmbientWaveSum( WaveAmplitude251 , WaveFrequency251 , WaveSpeed251 , Time251 , VertexPos251 );
				float temp_output_281_0 = ( _TimeParameters.x * P_Ambient_Wave_Speed228.w );
				float2 appendResult288 = (float2(temp_output_281_0 , temp_output_281_0));
				float3 ase_positionWS = TransformObjectToWorld( ( v.positionOS ).xyz );
				float simplePerlin2D275 = snoise( ( float3( appendResult288 ,  0.0 ) + ase_positionWS ).xy*float4( 0,0,0,0 ).w );
				simplePerlin2D275 = simplePerlin2D275*0.5 + 0.5;
				float P_Ambient_Wave_Mul267 = _AmbientWaveMul;
				float2 appendResult243 = (float2(0.0 , ( ( localAmbientWaveSum251 + ( simplePerlin2D275 * P_Ambient_Wave_Amptitude239.w ) ) * ( 1.0 - v.ase_texcoord1.y ) * v.uv0.x * P_Ambient_Wave_Mul267 )));
				float2 M_Ambient_Wave_Offset242 = appendResult243;
				
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord5 = screenPos;
				o.ase_texcoord6.xyz = ase_positionWS;
				
				o.ase_texcoord7.xy = v.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord6.w = 0;
				o.ase_texcoord7.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = float3( M_Ambient_Wave_Offset242 ,  0.0 );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.texCoord0 = v.uv0;
				o.color = v.color * unity_SpriteColor;
				o.positionCS = vertexInput.positionCS;

				float3 normalWS = TransformObjectToWorldNormal(v.normal);
				o.normalWS = -GetViewForwardDir();
				float4 tangentWS = float4( TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w);
				o.tangentWS = normalize(tangentWS);
				half crossSign = (tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
				o.bitangentWS = crossSign * cross(normalWS, tangentWS.xyz) * tangentWS.w;
				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 screenPos = IN.ase_texcoord5;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 GrabScreen84 = tex2D( _BehindWaterTex, ase_grabScreenPosNorm.xy );
				float2 TexelSize84 = ( 1.0 / _ScaledScreenParams ).xy;
				float P_Distortion_Strength52 = _DistortionStrength;
				float P_Distortion_Scale46 = _DistortionScale;
				float P_Distortion_Speed42 = _DistortionSpeed;
				float time35 = ( _TimeParameters.x * P_Distortion_Speed42 );
				float2 voronoiSmoothId35 = 0;
				float3 ase_positionWS = IN.ase_texcoord6.xyz;
				float2 P_Distortion_Direction39 = float2( 1,1 );
				float2 normalizeResult78 = ASESafeNormalize( P_Distortion_Direction39 );
				float2 coords35 = (ase_positionWS*1.0 + float3( ( normalizeResult78 * _TimeParameters.x ) ,  0.0 )).xy * ( 1.0 / P_Distortion_Scale46 );
				float2 id35 = 0;
				float2 uv35 = 0;
				float voroi35 = voronoi35( coords35, time35, id35, uv35, 0, voronoiSmoothId35 );
				float P_Surface_Distortion_Mul126 = _SurfaceDistortionMul;
				float2 texCoord12 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Upper11 = _SurfaceThicknessUpper;
				float M_Surface_Mask_01121 = step( texCoord12.y , ( P_Surface_Thickness_Upper11 * 0.03125 ) );
				float2 texCoord129 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Lower151 = _SurfaceThicknessLower;
				float M_Surface_Mask_02133 = ( step( texCoord129.y , ( 0.03125 * ( P_Surface_Thickness_Lower151 + P_Surface_Thickness_Upper11 ) ) ) + ( M_Surface_Mask_01121 * -1.0 ) );
				float M_Surface_Mask_Combined155 = ( M_Surface_Mask_01121 + M_Surface_Mask_02133 );
				float lerpResult123 = lerp( 1.0 , P_Surface_Distortion_Mul126 , M_Surface_Mask_Combined155);
				float M_Distortion_Offset106 = ( 0.01 * P_Distortion_Strength52 * voroi35 * lerpResult123 );
				float2 UV84 = ( ase_grabScreenPosNorm + M_Distortion_Offset106 ).xy;
				float P_Blur_Amount_Shallow104 = _BlurAmountShallow;
				float P_Blur_Amount_Deep205 = _BlurAmountDeep;
				float M_Depth211 = IN.ase_texcoord7.xy.y;
				float lerpResult203 = lerp( P_Blur_Amount_Shallow104 , P_Blur_Amount_Deep205 , M_Depth211);
				float Blur84 = lerpResult203;
				float4 localGaussianBlur84 = GaussianBlur( GrabScreen84 , TexelSize84 , UV84 , Blur84 );
				float4 M_Under_Water_Blur88 = localGaussianBlur84;
				float4 P_Underwater_Tint_Shallow194 = _UnderwaterTintShallow;
				float4 P_Underwater_Tint_Deep196 = _UnderwaterTintDeep;
				float4 lerpResult200 = lerp( P_Underwater_Tint_Shallow194 , P_Underwater_Tint_Deep196 , M_Depth211);
				float4 M_Under_Water_Color201 = lerpResult200;
				float4 P_Water_Color_Shallow9 = _WaterColorShallow;
				float4 P_Water_Color_Deep168 = _WaterColorDeep;
				float4 lerpResult181 = lerp( P_Water_Color_Shallow9 , P_Water_Color_Deep168 , M_Depth211);
				float4 M_Water_Color183 = lerpResult181;
				float4 P_Surface_Color_Upper10 = _SurfaceColorUpper;
				float4 lerpResult157 = lerp( M_Water_Color183 , P_Surface_Color_Upper10 , M_Surface_Mask_01121);
				float4 P_Surface_Color_Lower165 = _SurfaceColorLower;
				float4 lerpResult161 = lerp( lerpResult157 , P_Surface_Color_Lower165 , M_Surface_Mask_02133);
				float4 temp_output_2_0_g3 = lerpResult161;
				float4 lerpResult65 = lerp( ( M_Under_Water_Blur88 * M_Under_Water_Color201 ) , float4( (temp_output_2_0_g3).rgb , 0.0 ) , (temp_output_2_0_g3).a);
				float P_Light_Shaft_Tilt350 = _LightShaftTilt;
				float P_Light_Shaft_Speed317 = _LightShaftSpeed;
				float temp_output_324_0 = ( _TimeParameters.x * ( P_Light_Shaft_Speed317 * -0.75 ) );
				float2 appendResult325 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_324_0 ) , ( temp_output_324_0 * -0.5 )));
				float P_Light_Shaft_Scale307 = _LightShaftScale;
				float simplePerlin2D320 = snoise( appendResult325*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D320 = simplePerlin2D320*0.5 + 0.5;
				float P_Light_Shaft_Power310 = _LightShaftPower;
				float temp_output_315_0 = ( _TimeParameters.x * P_Light_Shaft_Speed317 );
				float2 appendResult300 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_315_0 ) , ( temp_output_315_0 * -1.0 )));
				float simplePerlin2D299 = snoise( appendResult300*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D299 = simplePerlin2D299*0.5 + 0.5;
				float4 P_Light_Shaft_Color359 = _LightShaftColor;
				float4 temp_output_2_0_g2 = P_Light_Shaft_Color359;
				float P_Light_Shaft_Depth347 = _LightShaftDepth;
				float clampResult343 = clamp( ( IN.texCoord0.xy.y / P_Light_Shaft_Depth347 ) , 0.0 , 1.0 );
				float4 M_Light_Shaft_Color_Final296 = ( ( pow( simplePerlin2D320 , P_Light_Shaft_Power310 ) + pow( simplePerlin2D299 , P_Light_Shaft_Power310 ) ) * P_Light_Shaft_Color359 * (temp_output_2_0_g2).a * ( 1.0 - clampResult343 ) );
				float4 break67 = ( lerpResult65 + M_Light_Shaft_Color_Final296 );
				float4 appendResult68 = (float4(break67.x , break67.y , break67.z , 1.0));
				

				float4 Color = appendResult68;
				float3 Normal = float3( 0, 0, 1 );
				float AlphaClipThreshold = 0.5;

				Color = half4(1.0,1.0,1.0, Color.a);

				#if defined( ALPHA_CLIP_THRESHOLD )
					clip( Color.a - AlphaClipThreshold );
				#endif

				return NormalsRenderingShared(Color, Normal, IN.tangentWS.xyz, IN.bitangentWS, IN.normalWS);
			}

			ENDHLSL
		}

		
		Pass
		{
			
            Name "Sprite Forward"
            Tags { "LightMode"="UniversalForward" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite On
			Offset 0 , 0
			ColorMask RGBA

			Stencil
			{
				Ref [_StencilReference]
				Pass Replace
			}

			HLSLPROGRAM

			#define _DISABLE_COLOR_TINT
			#define ASE_VERSION 19909
			#define ASE_SRP_VERSION 170400


			#if ( UNITY_VERSION >= 60010000 )
				#pragma multi_compile_instancing
			#endif

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX

			#define SHADERPASS SHADERPASS_SPRITEFORWARD

			#if ( UNITY_VERSION >= 60010000 )
				#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#endif
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			half4 _RendererColor;

			sampler2D _BehindWaterTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _AmbientWaveAmptitude;
			float4 _AmbientWaveFrequency;
			float4 _AmbientWaveSpeed;
			float4 _SurfaceColorLower;
			float4 _SurfaceColorUpper;
			float4 _WaterColorDeep;
			float4 _WaterColorShallow;
			float4 _LightShaftColor;
			float4 _UnderwaterTintShallow;
			float4 _UnderwaterTintDeep;
			int _StencilReference;
			float _LightShaftPower;
			float _LightShaftScale;
			float _LightShaftSpeed;
			float _LightShaftTilt;
			float _BlurAmountDeep;
			float _SurfaceThicknessLower;
			float _SurfaceThicknessUpper;
			float _SurfaceDistortionMul;
			float _DistortionSpeed;
			float _DistortionScale;
			float _DistortionStrength;
			float _AmbientWaveMul;
			float _BlurAmountShallow;
			float _LightShaftDepth;
			CBUFFER_END


			struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D( _AlphaTex ); SAMPLER( sampler_AlphaTex );
				float _EnableAlphaTexture;
			#endif

			float AmbientWaveSum( float4 WaveAmplitude, float4 WaveFrequency, float4 WaveSpeed, float Time, float3 VertexPos )
			{
				float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);
				float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);
				float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);
				return wave1 + wave2 + wave3;
			}
			
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
					float2 voronoihash35( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi35( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash35( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
			float2 ASESafeNormalize(float2 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float4 GaussianBlur( float4 GrabScreen, float2 TexelSize, float2 UV, float Blur )
			{
				    float4 col = float4(0.0, 0.0, 0.0, 0.0);
				    float kernelSum = 0.0;
				    int upper = ((Blur - 1) / 2);
				    int lower = -upper;
				    for (int x = lower; x <= upper; ++x)
				    {
				        for (int y = lower; y <= upper; ++y)
				        {
				            kernelSum ++;
				            float2 offset = float2(TexelSize.x * x, TexelSize.y * y);
				            col += tex2Dlod(_BehindWaterTex, float4(UV + offset,0,0));
				        }
				    }
				    col /= kernelSum;
				    return float4(col.r, col.g, col.b, col.a);
			}
			

			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				SetUpSpriteInstanceProperties();
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 P_Ambient_Wave_Amptitude239 = _AmbientWaveAmptitude;
				float4 WaveAmplitude251 = P_Ambient_Wave_Amptitude239;
				float4 P_Ambient_Wave_Frequency260 = _AmbientWaveFrequency;
				float4 WaveFrequency251 = P_Ambient_Wave_Frequency260;
				float4 P_Ambient_Wave_Speed228 = _AmbientWaveSpeed;
				float4 WaveSpeed251 = P_Ambient_Wave_Speed228;
				float Time251 = _TimeParameters.x;
				float3 VertexPos251 = v.positionOS;
				float localAmbientWaveSum251 = AmbientWaveSum( WaveAmplitude251 , WaveFrequency251 , WaveSpeed251 , Time251 , VertexPos251 );
				float temp_output_281_0 = ( _TimeParameters.x * P_Ambient_Wave_Speed228.w );
				float2 appendResult288 = (float2(temp_output_281_0 , temp_output_281_0));
				float3 ase_positionWS = TransformObjectToWorld( ( v.positionOS ).xyz );
				float simplePerlin2D275 = snoise( ( float3( appendResult288 ,  0.0 ) + ase_positionWS ).xy*float4( 0,0,0,0 ).w );
				simplePerlin2D275 = simplePerlin2D275*0.5 + 0.5;
				float P_Ambient_Wave_Mul267 = _AmbientWaveMul;
				float2 appendResult243 = (float2(0.0 , ( ( localAmbientWaveSum251 + ( simplePerlin2D275 * P_Ambient_Wave_Amptitude239.w ) ) * ( 1.0 - v.ase_texcoord1.y ) * v.uv0.x * P_Ambient_Wave_Mul267 )));
				float2 M_Ambient_Wave_Offset242 = appendResult243;
				
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord3 = screenPos;
				
				o.ase_texcoord4.xy = v.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord4.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3( 0, 0, 0 );
				#endif
				float3 vertexValue = float3( M_Ambient_Wave_Offset242 ,  0.0 );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				o.positionWS = vertexInput.positionWS;
				o.texCoord0 = v.uv0;
				o.color = v.color * _RendererColor * unity_SpriteColor;
				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 positionCS = IN.positionCS;
				float3 positionWS = IN.positionWS;

				float4 screenPos = IN.ase_texcoord3;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 GrabScreen84 = tex2D( _BehindWaterTex, ase_grabScreenPosNorm.xy );
				float2 TexelSize84 = ( 1.0 / _ScaledScreenParams ).xy;
				float P_Distortion_Strength52 = _DistortionStrength;
				float P_Distortion_Scale46 = _DistortionScale;
				float P_Distortion_Speed42 = _DistortionSpeed;
				float time35 = ( _TimeParameters.x * P_Distortion_Speed42 );
				float2 voronoiSmoothId35 = 0;
				float2 P_Distortion_Direction39 = float2( 1,1 );
				float2 normalizeResult78 = ASESafeNormalize( P_Distortion_Direction39 );
				float2 coords35 = (positionWS*1.0 + float3( ( normalizeResult78 * _TimeParameters.x ) ,  0.0 )).xy * ( 1.0 / P_Distortion_Scale46 );
				float2 id35 = 0;
				float2 uv35 = 0;
				float voroi35 = voronoi35( coords35, time35, id35, uv35, 0, voronoiSmoothId35 );
				float P_Surface_Distortion_Mul126 = _SurfaceDistortionMul;
				float2 texCoord12 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Upper11 = _SurfaceThicknessUpper;
				float M_Surface_Mask_01121 = step( texCoord12.y , ( P_Surface_Thickness_Upper11 * 0.03125 ) );
				float2 texCoord129 = IN.texCoord0.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Lower151 = _SurfaceThicknessLower;
				float M_Surface_Mask_02133 = ( step( texCoord129.y , ( 0.03125 * ( P_Surface_Thickness_Lower151 + P_Surface_Thickness_Upper11 ) ) ) + ( M_Surface_Mask_01121 * -1.0 ) );
				float M_Surface_Mask_Combined155 = ( M_Surface_Mask_01121 + M_Surface_Mask_02133 );
				float lerpResult123 = lerp( 1.0 , P_Surface_Distortion_Mul126 , M_Surface_Mask_Combined155);
				float M_Distortion_Offset106 = ( 0.01 * P_Distortion_Strength52 * voroi35 * lerpResult123 );
				float2 UV84 = ( ase_grabScreenPosNorm + M_Distortion_Offset106 ).xy;
				float P_Blur_Amount_Shallow104 = _BlurAmountShallow;
				float P_Blur_Amount_Deep205 = _BlurAmountDeep;
				float M_Depth211 = IN.ase_texcoord4.xy.y;
				float lerpResult203 = lerp( P_Blur_Amount_Shallow104 , P_Blur_Amount_Deep205 , M_Depth211);
				float Blur84 = lerpResult203;
				float4 localGaussianBlur84 = GaussianBlur( GrabScreen84 , TexelSize84 , UV84 , Blur84 );
				float4 M_Under_Water_Blur88 = localGaussianBlur84;
				float4 P_Underwater_Tint_Shallow194 = _UnderwaterTintShallow;
				float4 P_Underwater_Tint_Deep196 = _UnderwaterTintDeep;
				float4 lerpResult200 = lerp( P_Underwater_Tint_Shallow194 , P_Underwater_Tint_Deep196 , M_Depth211);
				float4 M_Under_Water_Color201 = lerpResult200;
				float4 P_Water_Color_Shallow9 = _WaterColorShallow;
				float4 P_Water_Color_Deep168 = _WaterColorDeep;
				float4 lerpResult181 = lerp( P_Water_Color_Shallow9 , P_Water_Color_Deep168 , M_Depth211);
				float4 M_Water_Color183 = lerpResult181;
				float4 P_Surface_Color_Upper10 = _SurfaceColorUpper;
				float4 lerpResult157 = lerp( M_Water_Color183 , P_Surface_Color_Upper10 , M_Surface_Mask_01121);
				float4 P_Surface_Color_Lower165 = _SurfaceColorLower;
				float4 lerpResult161 = lerp( lerpResult157 , P_Surface_Color_Lower165 , M_Surface_Mask_02133);
				float4 temp_output_2_0_g3 = lerpResult161;
				float4 lerpResult65 = lerp( ( M_Under_Water_Blur88 * M_Under_Water_Color201 ) , float4( (temp_output_2_0_g3).rgb , 0.0 ) , (temp_output_2_0_g3).a);
				float P_Light_Shaft_Tilt350 = _LightShaftTilt;
				float P_Light_Shaft_Speed317 = _LightShaftSpeed;
				float temp_output_324_0 = ( _TimeParameters.x * ( P_Light_Shaft_Speed317 * -0.75 ) );
				float2 appendResult325 = (float2(( ( positionWS.x + ( positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_324_0 ) , ( temp_output_324_0 * -0.5 )));
				float P_Light_Shaft_Scale307 = _LightShaftScale;
				float simplePerlin2D320 = snoise( appendResult325*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D320 = simplePerlin2D320*0.5 + 0.5;
				float P_Light_Shaft_Power310 = _LightShaftPower;
				float temp_output_315_0 = ( _TimeParameters.x * P_Light_Shaft_Speed317 );
				float2 appendResult300 = (float2(( ( positionWS.x + ( positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_315_0 ) , ( temp_output_315_0 * -1.0 )));
				float simplePerlin2D299 = snoise( appendResult300*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D299 = simplePerlin2D299*0.5 + 0.5;
				float4 P_Light_Shaft_Color359 = _LightShaftColor;
				float4 temp_output_2_0_g2 = P_Light_Shaft_Color359;
				float P_Light_Shaft_Depth347 = _LightShaftDepth;
				float clampResult343 = clamp( ( IN.texCoord0.xy.y / P_Light_Shaft_Depth347 ) , 0.0 , 1.0 );
				float4 M_Light_Shaft_Color_Final296 = ( ( pow( simplePerlin2D320 , P_Light_Shaft_Power310 ) + pow( simplePerlin2D299 , P_Light_Shaft_Power310 ) ) * P_Light_Shaft_Color359 * (temp_output_2_0_g2).a * ( 1.0 - clampResult343 ) );
				float4 break67 = ( lerpResult65 + M_Light_Shaft_Color_Final296 );
				float4 appendResult68 = (float4(break67.x , break67.y , break67.z , 1.0));
				

				float4 Color = appendResult68;
				float3 Normal = float3( 0, 0, 1 );
				float AlphaClipThreshold = 0.5;

			#if defined( ALPHA_CLIP_THRESHOLD )
				clip( Color.a - AlphaClipThreshold );
			#endif

			#if defined(DEBUG_DISPLAY)
				SurfaceData2D surfaceData;
				InitializeSurfaceData(Color.rgb, Color.a, surfaceData);
				InputData2D inputData;
				InitializeInputData(positionWS.xy, half2(IN.texCoord0.xy), inputData);
				half4 debugColor = 0;

				SETUP_DEBUG_DATA_2D(inputData, positionWS, positionCS);

				if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
				{
					return debugColor;
				}
			#endif

			#if ETC1_EXTERNAL_ALPHA
				float4 alpha = SAMPLE_TEXTURE2D( _AlphaTex, sampler_AlphaTex, IN.texCoord0.xy );
				Color.a = lerp( Color.a, alpha.r, _EnableAlphaTexture );
			#endif

			#if !defined( _DISABLE_COLOR_TINT )
				Color *= IN.color;
			#endif

				return Color;
			}

			ENDHLSL
		}

		
        Pass
        {
			
            Name "SceneSelectionPass"
            Tags { "LightMode"="SceneSelectionPass" }

            Cull Off

            HLSLPROGRAM

			#define _DISABLE_COLOR_TINT
			#define ASE_VERSION 19909
			#define ASE_SRP_VERSION 170400


			#if ( UNITY_VERSION >= 60010000 )
				#pragma multi_compile_instancing
			#endif

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENESELECTIONPASS 1

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			sampler2D _BehindWaterTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _AmbientWaveAmptitude;
			float4 _AmbientWaveFrequency;
			float4 _AmbientWaveSpeed;
			float4 _SurfaceColorLower;
			float4 _SurfaceColorUpper;
			float4 _WaterColorDeep;
			float4 _WaterColorShallow;
			float4 _LightShaftColor;
			float4 _UnderwaterTintShallow;
			float4 _UnderwaterTintDeep;
			int _StencilReference;
			float _LightShaftPower;
			float _LightShaftScale;
			float _LightShaftSpeed;
			float _LightShaftTilt;
			float _BlurAmountDeep;
			float _SurfaceThicknessLower;
			float _SurfaceThicknessUpper;
			float _SurfaceDistortionMul;
			float _DistortionSpeed;
			float _DistortionScale;
			float _DistortionStrength;
			float _AmbientWaveMul;
			float _BlurAmountShallow;
			float _LightShaftDepth;
			CBUFFER_END


            struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            int _ObjectId;
            int _PassValue;

			float AmbientWaveSum( float4 WaveAmplitude, float4 WaveFrequency, float4 WaveSpeed, float Time, float3 VertexPos )
			{
				float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);
				float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);
				float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);
				return wave1 + wave2 + wave3;
			}
			
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
					float2 voronoihash35( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi35( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash35( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
			float2 ASESafeNormalize(float2 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float4 GaussianBlur( float4 GrabScreen, float2 TexelSize, float2 UV, float Blur )
			{
				    float4 col = float4(0.0, 0.0, 0.0, 0.0);
				    float kernelSum = 0.0;
				    int upper = ((Blur - 1) / 2);
				    int lower = -upper;
				    for (int x = lower; x <= upper; ++x)
				    {
				        for (int y = lower; y <= upper; ++y)
				        {
				            kernelSum ++;
				            float2 offset = float2(TexelSize.x * x, TexelSize.y * y);
				            col += tex2Dlod(_BehindWaterTex, float4(UV + offset,0,0));
				        }
				    }
				    col /= kernelSum;
				    return float4(col.r, col.g, col.b, col.a);
			}
			

			VertexOutput vert(VertexInput v )
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				SetUpSpriteInstanceProperties();
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 P_Ambient_Wave_Amptitude239 = _AmbientWaveAmptitude;
				float4 WaveAmplitude251 = P_Ambient_Wave_Amptitude239;
				float4 P_Ambient_Wave_Frequency260 = _AmbientWaveFrequency;
				float4 WaveFrequency251 = P_Ambient_Wave_Frequency260;
				float4 P_Ambient_Wave_Speed228 = _AmbientWaveSpeed;
				float4 WaveSpeed251 = P_Ambient_Wave_Speed228;
				float Time251 = _TimeParameters.x;
				float3 VertexPos251 = v.positionOS;
				float localAmbientWaveSum251 = AmbientWaveSum( WaveAmplitude251 , WaveFrequency251 , WaveSpeed251 , Time251 , VertexPos251 );
				float temp_output_281_0 = ( _TimeParameters.x * P_Ambient_Wave_Speed228.w );
				float2 appendResult288 = (float2(temp_output_281_0 , temp_output_281_0));
				float3 ase_positionWS = TransformObjectToWorld( ( v.positionOS ).xyz );
				float simplePerlin2D275 = snoise( ( float3( appendResult288 ,  0.0 ) + ase_positionWS ).xy*float4( 0,0,0,0 ).w );
				simplePerlin2D275 = simplePerlin2D275*0.5 + 0.5;
				float P_Ambient_Wave_Mul267 = _AmbientWaveMul;
				float2 appendResult243 = (float2(0.0 , ( ( localAmbientWaveSum251 + ( simplePerlin2D275 * P_Ambient_Wave_Amptitude239.w ) ) * ( 1.0 - v.ase_texcoord1.y ) * v.ase_texcoord.x * P_Ambient_Wave_Mul267 )));
				float2 M_Ambient_Wave_Offset242 = appendResult243;
				
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord = screenPos;
				o.ase_texcoord1.xyz = ase_positionWS;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				o.ase_texcoord2.zw = v.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.w = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = float3( M_Ambient_Wave_Offset242 ,  0.0 );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				return o;
			}

			half4 frag(VertexOutput IN) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 screenPos = IN.ase_texcoord;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 GrabScreen84 = tex2D( _BehindWaterTex, ase_grabScreenPosNorm.xy );
				float2 TexelSize84 = ( 1.0 / _ScaledScreenParams ).xy;
				float P_Distortion_Strength52 = _DistortionStrength;
				float P_Distortion_Scale46 = _DistortionScale;
				float P_Distortion_Speed42 = _DistortionSpeed;
				float time35 = ( _TimeParameters.x * P_Distortion_Speed42 );
				float2 voronoiSmoothId35 = 0;
				float3 ase_positionWS = IN.ase_texcoord1.xyz;
				float2 P_Distortion_Direction39 = float2( 1,1 );
				float2 normalizeResult78 = ASESafeNormalize( P_Distortion_Direction39 );
				float2 coords35 = (ase_positionWS*1.0 + float3( ( normalizeResult78 * _TimeParameters.x ) ,  0.0 )).xy * ( 1.0 / P_Distortion_Scale46 );
				float2 id35 = 0;
				float2 uv35 = 0;
				float voroi35 = voronoi35( coords35, time35, id35, uv35, 0, voronoiSmoothId35 );
				float P_Surface_Distortion_Mul126 = _SurfaceDistortionMul;
				float2 texCoord12 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Upper11 = _SurfaceThicknessUpper;
				float M_Surface_Mask_01121 = step( texCoord12.y , ( P_Surface_Thickness_Upper11 * 0.03125 ) );
				float2 texCoord129 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Lower151 = _SurfaceThicknessLower;
				float M_Surface_Mask_02133 = ( step( texCoord129.y , ( 0.03125 * ( P_Surface_Thickness_Lower151 + P_Surface_Thickness_Upper11 ) ) ) + ( M_Surface_Mask_01121 * -1.0 ) );
				float M_Surface_Mask_Combined155 = ( M_Surface_Mask_01121 + M_Surface_Mask_02133 );
				float lerpResult123 = lerp( 1.0 , P_Surface_Distortion_Mul126 , M_Surface_Mask_Combined155);
				float M_Distortion_Offset106 = ( 0.01 * P_Distortion_Strength52 * voroi35 * lerpResult123 );
				float2 UV84 = ( ase_grabScreenPosNorm + M_Distortion_Offset106 ).xy;
				float P_Blur_Amount_Shallow104 = _BlurAmountShallow;
				float P_Blur_Amount_Deep205 = _BlurAmountDeep;
				float M_Depth211 = IN.ase_texcoord2.zw.y;
				float lerpResult203 = lerp( P_Blur_Amount_Shallow104 , P_Blur_Amount_Deep205 , M_Depth211);
				float Blur84 = lerpResult203;
				float4 localGaussianBlur84 = GaussianBlur( GrabScreen84 , TexelSize84 , UV84 , Blur84 );
				float4 M_Under_Water_Blur88 = localGaussianBlur84;
				float4 P_Underwater_Tint_Shallow194 = _UnderwaterTintShallow;
				float4 P_Underwater_Tint_Deep196 = _UnderwaterTintDeep;
				float4 lerpResult200 = lerp( P_Underwater_Tint_Shallow194 , P_Underwater_Tint_Deep196 , M_Depth211);
				float4 M_Under_Water_Color201 = lerpResult200;
				float4 P_Water_Color_Shallow9 = _WaterColorShallow;
				float4 P_Water_Color_Deep168 = _WaterColorDeep;
				float4 lerpResult181 = lerp( P_Water_Color_Shallow9 , P_Water_Color_Deep168 , M_Depth211);
				float4 M_Water_Color183 = lerpResult181;
				float4 P_Surface_Color_Upper10 = _SurfaceColorUpper;
				float4 lerpResult157 = lerp( M_Water_Color183 , P_Surface_Color_Upper10 , M_Surface_Mask_01121);
				float4 P_Surface_Color_Lower165 = _SurfaceColorLower;
				float4 lerpResult161 = lerp( lerpResult157 , P_Surface_Color_Lower165 , M_Surface_Mask_02133);
				float4 temp_output_2_0_g3 = lerpResult161;
				float4 lerpResult65 = lerp( ( M_Under_Water_Blur88 * M_Under_Water_Color201 ) , float4( (temp_output_2_0_g3).rgb , 0.0 ) , (temp_output_2_0_g3).a);
				float P_Light_Shaft_Tilt350 = _LightShaftTilt;
				float P_Light_Shaft_Speed317 = _LightShaftSpeed;
				float temp_output_324_0 = ( _TimeParameters.x * ( P_Light_Shaft_Speed317 * -0.75 ) );
				float2 appendResult325 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_324_0 ) , ( temp_output_324_0 * -0.5 )));
				float P_Light_Shaft_Scale307 = _LightShaftScale;
				float simplePerlin2D320 = snoise( appendResult325*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D320 = simplePerlin2D320*0.5 + 0.5;
				float P_Light_Shaft_Power310 = _LightShaftPower;
				float temp_output_315_0 = ( _TimeParameters.x * P_Light_Shaft_Speed317 );
				float2 appendResult300 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_315_0 ) , ( temp_output_315_0 * -1.0 )));
				float simplePerlin2D299 = snoise( appendResult300*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D299 = simplePerlin2D299*0.5 + 0.5;
				float4 P_Light_Shaft_Color359 = _LightShaftColor;
				float4 temp_output_2_0_g2 = P_Light_Shaft_Color359;
				float P_Light_Shaft_Depth347 = _LightShaftDepth;
				float clampResult343 = clamp( ( IN.ase_texcoord2.xy.y / P_Light_Shaft_Depth347 ) , 0.0 , 1.0 );
				float4 M_Light_Shaft_Color_Final296 = ( ( pow( simplePerlin2D320 , P_Light_Shaft_Power310 ) + pow( simplePerlin2D299 , P_Light_Shaft_Power310 ) ) * P_Light_Shaft_Color359 * (temp_output_2_0_g2).a * ( 1.0 - clampResult343 ) );
				float4 break67 = ( lerpResult65 + M_Light_Shaft_Color_Final296 );
				float4 appendResult68 = (float4(break67.x , break67.y , break67.z , 1.0));
				

				float4 Color = appendResult68;
				float AlphaClipThreshold = 0.5;

				#if defined( ALPHA_CLIP_THRESHOLD )
					clip( Color.a - AlphaClipThreshold );
				#endif

				return half4(_ObjectId, _PassValue, 1.0, 1.0);
			}

            ENDHLSL
        }

		
        Pass
        {
			
            Name "ScenePickingPass"
            Tags { "LightMode"="Picking" }

			Cull Off

            HLSLPROGRAM

			#define _DISABLE_COLOR_TINT
			#define ASE_VERSION 19909
			#define ASE_SRP_VERSION 170400


			#if ( UNITY_VERSION >= 60010000 )
				#pragma multi_compile_instancing
			#endif

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define FEATURES_GRAPH_VERTEX

            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENEPICKINGPASS 1

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

        	#define ASE_NEEDS_VERT_POSITION
        	#define ASE_NEEDS_TEXTURE_COORDINATES1
        	#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
        	#define ASE_NEEDS_TEXTURE_COORDINATES0
        	#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
        	#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
        	#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1


			sampler2D _BehindWaterTex;
			CBUFFER_START( UnityPerMaterial )
			float4 _AmbientWaveAmptitude;
			float4 _AmbientWaveFrequency;
			float4 _AmbientWaveSpeed;
			float4 _SurfaceColorLower;
			float4 _SurfaceColorUpper;
			float4 _WaterColorDeep;
			float4 _WaterColorShallow;
			float4 _LightShaftColor;
			float4 _UnderwaterTintShallow;
			float4 _UnderwaterTintDeep;
			int _StencilReference;
			float _LightShaftPower;
			float _LightShaftScale;
			float _LightShaftSpeed;
			float _LightShaftTilt;
			float _BlurAmountDeep;
			float _SurfaceThicknessLower;
			float _SurfaceThicknessUpper;
			float _SurfaceDistortionMul;
			float _DistortionSpeed;
			float _DistortionScale;
			float _DistortionStrength;
			float _AmbientWaveMul;
			float _BlurAmountShallow;
			float _LightShaftDepth;
			CBUFFER_END


            struct VertexInput
			{
				float3 positionOS : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_SKINNED_VERTEX_INPUTS
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            float4 _SelectionID;

			float AmbientWaveSum( float4 WaveAmplitude, float4 WaveFrequency, float4 WaveSpeed, float Time, float3 VertexPos )
			{
				float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x);
				float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y);
				float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z);
				return wave1 + wave2 + wave3;
			}
			
			float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
			float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
			float snoise( float2 v )
			{
				const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
				float2 i = floor( v + dot( v, C.yy ) );
				float2 x0 = v - i + dot( i, C.xx );
				float2 i1;
				i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
				float4 x12 = x0.xyxy + C.xxzz;
				x12.xy -= i1;
				i = mod2D289( i );
				float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
				float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
				m = m * m;
				m = m * m;
				float3 x = 2.0 * frac( p * C.www ) - 1.0;
				float3 h = abs( x ) - 0.5;
				float3 ox = floor( x + 0.5 );
				float3 a0 = x - ox;
				m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
				float3 g;
				g.x = a0.x * x0.x + h.x * x0.y;
				g.yz = a0.yz * x12.xz + h.yz * x12.yw;
				return 130.0 * dot( m, g );
			}
			
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
					float2 voronoihash35( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi35( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0; int i, j;
						for ( j = -1; j <= 1; j++ )
						{
							for ( i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash35( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			
			float2 ASESafeNormalize(float2 inVec)
			{
				float dp3 = max(1.175494351e-38, dot(inVec, inVec));
				return inVec* rsqrt(dp3);
			}
			
			float4 GaussianBlur( float4 GrabScreen, float2 TexelSize, float2 UV, float Blur )
			{
				    float4 col = float4(0.0, 0.0, 0.0, 0.0);
				    float kernelSum = 0.0;
				    int upper = ((Blur - 1) / 2);
				    int lower = -upper;
				    for (int x = lower; x <= upper; ++x)
				    {
				        for (int y = lower; y <= upper; ++y)
				        {
				            kernelSum ++;
				            float2 offset = float2(TexelSize.x * x, TexelSize.y * y);
				            col += tex2Dlod(_BehindWaterTex, float4(UV + offset,0,0));
				        }
				    }
				    col /= kernelSum;
				    return float4(col.r, col.g, col.b, col.a);
			}
			

			VertexOutput vert(VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_SKINNED_VERTEX_COMPUTE(v);

				SetUpSpriteInstanceProperties();
				v.positionOS = UnityFlipSprite( v.positionOS, unity_SpriteProps.xy );

				float4 P_Ambient_Wave_Amptitude239 = _AmbientWaveAmptitude;
				float4 WaveAmplitude251 = P_Ambient_Wave_Amptitude239;
				float4 P_Ambient_Wave_Frequency260 = _AmbientWaveFrequency;
				float4 WaveFrequency251 = P_Ambient_Wave_Frequency260;
				float4 P_Ambient_Wave_Speed228 = _AmbientWaveSpeed;
				float4 WaveSpeed251 = P_Ambient_Wave_Speed228;
				float Time251 = _TimeParameters.x;
				float3 VertexPos251 = v.positionOS;
				float localAmbientWaveSum251 = AmbientWaveSum( WaveAmplitude251 , WaveFrequency251 , WaveSpeed251 , Time251 , VertexPos251 );
				float temp_output_281_0 = ( _TimeParameters.x * P_Ambient_Wave_Speed228.w );
				float2 appendResult288 = (float2(temp_output_281_0 , temp_output_281_0));
				float3 ase_positionWS = TransformObjectToWorld( ( v.positionOS ).xyz );
				float simplePerlin2D275 = snoise( ( float3( appendResult288 ,  0.0 ) + ase_positionWS ).xy*float4( 0,0,0,0 ).w );
				simplePerlin2D275 = simplePerlin2D275*0.5 + 0.5;
				float P_Ambient_Wave_Mul267 = _AmbientWaveMul;
				float2 appendResult243 = (float2(0.0 , ( ( localAmbientWaveSum251 + ( simplePerlin2D275 * P_Ambient_Wave_Amptitude239.w ) ) * ( 1.0 - v.ase_texcoord1.y ) * v.ase_texcoord.x * P_Ambient_Wave_Mul267 )));
				float2 M_Ambient_Wave_Offset242 = appendResult243;
				
				float4 ase_positionCS = TransformObjectToHClip( ( v.positionOS ).xyz );
				float4 screenPos = ComputeScreenPos( ase_positionCS );
				o.ase_texcoord = screenPos;
				o.ase_texcoord1.xyz = ase_positionWS;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				o.ase_texcoord2.zw = v.ase_texcoord1.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.positionOS;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = float3( M_Ambient_Wave_Offset242 ,  0.0 );
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.positionOS = vertexValue;
				#else
					v.positionOS += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);

				o.positionCS = vertexInput.positionCS;
				return o;
			}

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float4 screenPos = IN.ase_texcoord;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float4 GrabScreen84 = tex2D( _BehindWaterTex, ase_grabScreenPosNorm.xy );
				float2 TexelSize84 = ( 1.0 / _ScaledScreenParams ).xy;
				float P_Distortion_Strength52 = _DistortionStrength;
				float P_Distortion_Scale46 = _DistortionScale;
				float P_Distortion_Speed42 = _DistortionSpeed;
				float time35 = ( _TimeParameters.x * P_Distortion_Speed42 );
				float2 voronoiSmoothId35 = 0;
				float3 ase_positionWS = IN.ase_texcoord1.xyz;
				float2 P_Distortion_Direction39 = float2( 1,1 );
				float2 normalizeResult78 = ASESafeNormalize( P_Distortion_Direction39 );
				float2 coords35 = (ase_positionWS*1.0 + float3( ( normalizeResult78 * _TimeParameters.x ) ,  0.0 )).xy * ( 1.0 / P_Distortion_Scale46 );
				float2 id35 = 0;
				float2 uv35 = 0;
				float voroi35 = voronoi35( coords35, time35, id35, uv35, 0, voronoiSmoothId35 );
				float P_Surface_Distortion_Mul126 = _SurfaceDistortionMul;
				float2 texCoord12 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Upper11 = _SurfaceThicknessUpper;
				float M_Surface_Mask_01121 = step( texCoord12.y , ( P_Surface_Thickness_Upper11 * 0.03125 ) );
				float2 texCoord129 = IN.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float P_Surface_Thickness_Lower151 = _SurfaceThicknessLower;
				float M_Surface_Mask_02133 = ( step( texCoord129.y , ( 0.03125 * ( P_Surface_Thickness_Lower151 + P_Surface_Thickness_Upper11 ) ) ) + ( M_Surface_Mask_01121 * -1.0 ) );
				float M_Surface_Mask_Combined155 = ( M_Surface_Mask_01121 + M_Surface_Mask_02133 );
				float lerpResult123 = lerp( 1.0 , P_Surface_Distortion_Mul126 , M_Surface_Mask_Combined155);
				float M_Distortion_Offset106 = ( 0.01 * P_Distortion_Strength52 * voroi35 * lerpResult123 );
				float2 UV84 = ( ase_grabScreenPosNorm + M_Distortion_Offset106 ).xy;
				float P_Blur_Amount_Shallow104 = _BlurAmountShallow;
				float P_Blur_Amount_Deep205 = _BlurAmountDeep;
				float M_Depth211 = IN.ase_texcoord2.zw.y;
				float lerpResult203 = lerp( P_Blur_Amount_Shallow104 , P_Blur_Amount_Deep205 , M_Depth211);
				float Blur84 = lerpResult203;
				float4 localGaussianBlur84 = GaussianBlur( GrabScreen84 , TexelSize84 , UV84 , Blur84 );
				float4 M_Under_Water_Blur88 = localGaussianBlur84;
				float4 P_Underwater_Tint_Shallow194 = _UnderwaterTintShallow;
				float4 P_Underwater_Tint_Deep196 = _UnderwaterTintDeep;
				float4 lerpResult200 = lerp( P_Underwater_Tint_Shallow194 , P_Underwater_Tint_Deep196 , M_Depth211);
				float4 M_Under_Water_Color201 = lerpResult200;
				float4 P_Water_Color_Shallow9 = _WaterColorShallow;
				float4 P_Water_Color_Deep168 = _WaterColorDeep;
				float4 lerpResult181 = lerp( P_Water_Color_Shallow9 , P_Water_Color_Deep168 , M_Depth211);
				float4 M_Water_Color183 = lerpResult181;
				float4 P_Surface_Color_Upper10 = _SurfaceColorUpper;
				float4 lerpResult157 = lerp( M_Water_Color183 , P_Surface_Color_Upper10 , M_Surface_Mask_01121);
				float4 P_Surface_Color_Lower165 = _SurfaceColorLower;
				float4 lerpResult161 = lerp( lerpResult157 , P_Surface_Color_Lower165 , M_Surface_Mask_02133);
				float4 temp_output_2_0_g3 = lerpResult161;
				float4 lerpResult65 = lerp( ( M_Under_Water_Blur88 * M_Under_Water_Color201 ) , float4( (temp_output_2_0_g3).rgb , 0.0 ) , (temp_output_2_0_g3).a);
				float P_Light_Shaft_Tilt350 = _LightShaftTilt;
				float P_Light_Shaft_Speed317 = _LightShaftSpeed;
				float temp_output_324_0 = ( _TimeParameters.x * ( P_Light_Shaft_Speed317 * -0.75 ) );
				float2 appendResult325 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_324_0 ) , ( temp_output_324_0 * -0.5 )));
				float P_Light_Shaft_Scale307 = _LightShaftScale;
				float simplePerlin2D320 = snoise( appendResult325*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D320 = simplePerlin2D320*0.5 + 0.5;
				float P_Light_Shaft_Power310 = _LightShaftPower;
				float temp_output_315_0 = ( _TimeParameters.x * P_Light_Shaft_Speed317 );
				float2 appendResult300 = (float2(( ( ase_positionWS.x + ( ase_positionWS.y * P_Light_Shaft_Tilt350 ) ) + temp_output_315_0 ) , ( temp_output_315_0 * -1.0 )));
				float simplePerlin2D299 = snoise( appendResult300*( 1.0 / P_Light_Shaft_Scale307 ) );
				simplePerlin2D299 = simplePerlin2D299*0.5 + 0.5;
				float4 P_Light_Shaft_Color359 = _LightShaftColor;
				float4 temp_output_2_0_g2 = P_Light_Shaft_Color359;
				float P_Light_Shaft_Depth347 = _LightShaftDepth;
				float clampResult343 = clamp( ( IN.ase_texcoord2.xy.y / P_Light_Shaft_Depth347 ) , 0.0 , 1.0 );
				float4 M_Light_Shaft_Color_Final296 = ( ( pow( simplePerlin2D320 , P_Light_Shaft_Power310 ) + pow( simplePerlin2D299 , P_Light_Shaft_Power310 ) ) * P_Light_Shaft_Color359 * (temp_output_2_0_g2).a * ( 1.0 - clampResult343 ) );
				float4 break67 = ( lerpResult65 + M_Light_Shaft_Color_Final296 );
				float4 appendResult68 = (float4(break67.x , break67.y , break67.z , 1.0));
				

				float4 Color = appendResult68;
				float AlphaClipThreshold = 0.5;

				#if defined( ALPHA_CLIP_THRESHOLD )
					clip( Color.a - AlphaClipThreshold );
				#endif

				return unity_SelectionID;
			}

            ENDHLSL
        }
		
	}
	

	

	CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19909
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;31;-3344,-2704;Inherit;False;2788.886;2008.525;;51;194;193;165;164;10;7;104;102;168;9;167;6;52;126;51;46;125;45;42;41;39;40;151;152;11;143;195;196;204;205;228;239;258;259;260;261;266;267;307;308;309;310;316;317;346;347;349;350;358;359;366;PARAMETER;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;152;-3264,-1104;Inherit;False;Property;_SurfaceThicknessLower;Surface Thickness Lower;7;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;34;-3296,-528;Inherit;False;2716.396;1388.472;;19;121;133;153;145;138;147;148;149;132;131;130;129;14;116;13;15;12;154;155;SURFACE MASK;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;151;-2976,-1104;Inherit;False;P Surface Thickness Lower;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;138;-3264,336;Inherit;False;11;P Surface Thickness Upper;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;153;-3280,256;Inherit;False;151;P Surface Thickness Lower;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;143;-3264,-1200;Inherit;False;Property;_SurfaceThicknessUpper;Surface Thickness Upper;6;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;129;-3168,112;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;145;-3040,288;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;11;-2992,-1200;Inherit;False;P Surface Thickness Upper;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;12;-3248,-304;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;15;-3248,-176;Inherit;False;11;P Surface Thickness Upper;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;130;-2896,112;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;131;-2896,240;Inherit;False;2;2;0;FLOAT;0.03125;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;148;-2864,400;Inherit;False;121;M Surface Mask 01;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;13;-2976,-304;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;116;-2976,-176;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.03125;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;132;-2720,144;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;149;-2624,400;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;40;-2640,-2592;Inherit;False;Constant;_DistortionDirection;Distortion Direction;9;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;62;-3280,2368;Inherit;False;2701.329;1082.82;;19;53;35;54;73;48;47;36;49;61;44;37;78;77;106;123;124;127;128;369;DISTORTION;1,1,1,1;0;0
Node;AmplifyShaderEditor.StepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;14;-2736,-288;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;147;-2416,288;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;39;-2368,-2592;Inherit;False;P Distortion Direction;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;121;-2320,-192;Inherit;False;M Surface Mask 01;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;133;-2288,192;Inherit;False;M Surface Mask 02;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;41;-2624,-2352;Inherit;False;Property;_DistortionSpeed;Distortion Speed;9;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;77;-3152,2640;Inherit;False;39;P Distortion Direction;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;45;-2624,-2256;Inherit;False;Property;_DistortionScale;Distortion Scale;10;0;Create;True;0;0;0;False;0;False;0.7;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;154;-1952,-32;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;42;-2352,-2352;Inherit;False;P Distortion Speed;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;78;-2912,2640;Inherit;False;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;37;-2976,2720;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;125;-3264,-1008;Inherit;False;Property;_SurfaceDistortionMul;Surface Distortion Mul;8;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;46;-2352,-2256;Inherit;False;P Distortion Scale;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;316;-1792,-1616;Inherit;False;Property;_LightShaftSpeed;Light Shaft Speed;19;0;Create;True;0;0;0;False;0;False;0.7;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;349;-1760,-1200;Inherit;False;Property;_LightShaftTilt;Light Shaft Tilt;17;0;Create;True;0;0;0;False;0;False;-0.2;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;368;-96,3424;Inherit;False;2785.836;1946.747;;41;296;344;364;345;332;360;343;331;306;342;320;330;312;299;348;341;300;325;321;336;338;319;339;326;315;324;355;353;318;314;328;322;327;298;356;354;323;370;371;375;376;LIGHT SHAFT;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;155;-1792,-32;Inherit;False;M Surface Mask Combined;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;51;-2624,-2128;Inherit;False;Property;_DistortionStrength;Distortion Strength;11;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;44;-2768,2656;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;61;-2784,2496;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;126;-2992,-1008;Inherit;False;P Surface Distortion Mul;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;49;-2928,2832;Inherit;False;42;P Distortion Speed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;47;-2704,2928;Inherit;False;46;P Distortion Scale;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;317;-1536,-1616;Inherit;False;P Light Shaft Speed;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;350;-1536,-1200;Inherit;False;P Light Shaft Tilt;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;52;-2352,-2128;Inherit;False;P Distortion Strength;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;36;-2576,2608;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;48;-2640,2768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;124;-2480,3184;Inherit;False;155;M Surface Mask Combined;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;127;-2464,3104;Inherit;False;126;P Surface Distortion Mul;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;369;-2480,2880;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;323;-48,3872;Inherit;False;317;P Light Shaft Speed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;354;-32,4320;Inherit;False;350;P Light Shaft Tilt;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;356;-64,3680;Inherit;False;350;P Light Shaft Tilt;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;292;-162,1486;Inherit;False;2852;1411;Comment;27;282;280;283;281;277;288;290;284;278;285;275;286;264;257;263;265;256;247;291;251;248;268;250;287;245;243;242;AMBIENT WAVE;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;182;-3328,1088;Inherit;False;2762.934;1091.725;Comment;13;181;179;178;183;197;198;200;201;207;211;212;213;224;WATER COLOR & UNDER WATER COLOR;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;73;-2208,2544;Inherit;False;Constant;_Float0;Float 0;7;0;Create;True;0;0;0;False;0;False;0.01;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;54;-2304,2624;Inherit;False;52;P Distortion Strength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;123;-2192,3072;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;5;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VoronoiNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;35;-2304,2736;Inherit;True;0;0;1;0;1;False;3;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;322;144,3776;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;328;208,3872;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.75;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;314;192,4448;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;318;128,4544;Inherit;False;317;P Light Shaft Speed;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;167;-3280,-2448;Inherit;False;Property;_WaterColorDeep;Water Color Deep;1;0;Create;True;0;0;0;False;0;False;0.4431373,0.6117647,0.5215687,0.3764706;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;6;-3280,-2656;Inherit;False;Property;_WaterColorShallow;Water Color Shallow;0;0;Create;False;0;0;0;False;0;False;0.7450981,0.9333333,0.827451,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;308;-1776,-1520;Inherit;False;Property;_LightShaftScale;Light Shaft Scale;15;0;Create;True;0;0;0;False;0;False;1.4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;298;-32,4128;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;355;288,3584;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;327;-32,3472;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;353;288,4240;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;282;-112,2256;Inherit;False;228;P Ambient Wave Speed;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;207;-3232,1344;Inherit;False;1;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;53;-2016,2688;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;102;-2576,-1728;Inherit;False;Property;_BlurAmountShallow;Blur Amount Shallow;12;0;Create;True;0;0;0;False;0;False;2;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;204;-2576,-1616;Inherit;False;Property;_BlurAmountDeep;Blur Amount Deep;13;0;Create;True;0;0;0;False;0;False;12;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;324;368,3840;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;315;384,4464;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;9;-3008,-2640;Inherit;False;P Water Color Shallow;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;168;-2976,-2448;Inherit;False;P Water Color Deep;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;307;-1552,-1520;Inherit;False;P Light Shaft Scale;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;346;-1776,-1312;Inherit;False;Property;_LightShaftDepth;Light Shaft Depth;18;0;Create;True;0;0;0;False;0;False;2;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;375;448,3504;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;376;464,4144;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;283;160,2256;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;280;160,2144;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;112;-3328,3792;Inherit;False;1992.883;1045.441;Comment;13;88;84;105;107;100;109;86;99;101;203;206;214;388;UNDER WATER BLUR;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;211;-2928,1392;Inherit;False;M Depth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;106;-1792,2688;Inherit;False;M Distortion Offset;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;104;-2352,-1728;Inherit;False;P Blur Amount Shallow;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;205;-2352,-1616;Inherit;False;P Blur Amount Deep;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;179;-2112,1376;Inherit;False;168;P Water Color Deep;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;178;-2112,1296;Inherit;False;9;P Water Color Shallow;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;212;-2112,1456;Inherit;False;211;M Depth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;347;-1504,-1312;Inherit;False;P Light Shaft Depth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;326;592,3824;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;339;544,3952;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;319;560,4352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;338;560,4528;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;193;-3248,-2208;Inherit;False;Property;_UnderwaterTintShallow;Underwater Tint Shallow;2;0;Create;True;0;0;0;False;0;False;0.9098039,0.9921569,0.9686275,1;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;195;-3248,-1952;Inherit;False;Property;_UnderwaterTintDeep;Underwater Tint Deep;3;0;Create;True;0;0;0;False;0;False;0.3568628,0.5843138,0.4509804,1;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;321;624,4080;Inherit;False;307;P Light Shaft Scale;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;336;608,4672;Inherit;False;307;P Light Shaft Scale;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;309;-1776,-1424;Inherit;False;Property;_LightShaftPower;Light Shaft Power;16;0;Create;True;0;0;0;False;0;False;2;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;281;368,2256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GrabScreenPosition, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;86;-3216,4144;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;101;-2912,4112;Inherit;False;Constant;_Float2;Float 2;8;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;109;-3200,4592;Inherit;False;106;M Distortion Offset;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;105;-2656,4384;Inherit;False;104;P Blur Amount Shallow;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;206;-2656,4464;Inherit;False;205;P Blur Amount Deep;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;214;-2624,4560;Inherit;False;211;M Depth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenParams, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;99;-2944,4192;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;181;-1808,1328;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;310;-1552,-1424;Inherit;False;P Light Shaft Power;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;325;736,3824;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;300;752,4416;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;341;752,4928;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;348;704,5088;Inherit;False;347;P Light Shaft Depth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;196;-2944,-1952;Inherit;False;P Underwater Tint Deep;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;194;-2960,-2192;Inherit;False;P Underwater Tint Shallow;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;7;-3264,-1680;Inherit;False;Property;_SurfaceColorUpper;Surface Color Upper;4;0;Create;True;0;0;0;False;0;False;0.8901961,0.9921569,1,0.1960784;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;370;848,3984;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;371;848,4560;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;258;-1840,-2448;Inherit;False;Property;_AmbientWaveSpeed;Ambient Wave Speed;21;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;261;-1840,-2016;Inherit;False;Property;_AmbientWaveAmptitude;Ambient Wave Amptitude;23;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;259;-1824,-2240;Inherit;False;Property;_AmbientWaveFrequency;Ambient Wave Frequency;22;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;288;544,2272;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;290;464,2416;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;388;-2816,3904;Inherit;True;Global;_BehindWaterTex;Behind Water Tex;25;0;Create;True;0;0;0;False;0;False;-1;None;;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;69;114,-656;Inherit;False;2928.262;1673.109;;17;297;244;68;67;65;160;191;192;63;161;162;163;157;156;159;184;363;FINAL;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;100;-2688,4160;Inherit;False;2;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;107;-2896,4384;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;203;-2384,4432;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;10;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;183;-1568,1344;Inherit;False;M Water Color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;197;-2112,1680;Inherit;False;194;P Underwater Tint Shallow;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;198;-2112,1760;Inherit;False;196;P Underwater Tint Deep;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;213;-2096,1840;Inherit;False;211;M Depth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;299;976,4432;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;312;976,4064;Inherit;False;310;P Light Shaft Power;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;330;992,4544;Inherit;False;310;P Light Shaft Power;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;320;960,3888;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;342;976,4976;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;358;-1760,-1088;Inherit;False;Property;_LightShaftColor;Light Shaft Color;14;0;Create;True;0;0;0;False;0;False;0.2235294,0.3215686,0.2745098,0.3921569;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;164;-3264,-1456;Inherit;False;Property;_SurfaceColorLower;Surface Color Lower;5;0;Create;True;0;0;0;False;0;False;0.2039216,0.3686275,0.2941177,0.03921569;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;10;-3008,-1664;Inherit;False;P Surface Color Upper;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;228;-1552,-2432;Inherit;False;P Ambient Wave Speed;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;239;-1552,-2016;Inherit;False;P Ambient Wave Amptitude;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;260;-1536,-2240;Inherit;False;P Ambient Wave Frequency;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;284;768,2368;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;278;576,2592;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;285;528,2784;Inherit;False;239;P Ambient Wave Amptitude;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;84;-2400,4128;Inherit;False;    float4 col = float4(0.0, 0.0, 0.0, 0.0)@$    float kernelSum = 0.0@$$    int upper = ((Blur - 1) / 2)@$    int lower = -upper@$$    for (int x = lower@ x <= upper@ ++x)$    {$        for (int y = lower@ y <= upper@ ++y)$        {$            kernelSum ++@$$            float2 offset = float2(TexelSize.x * x, TexelSize.y * y)@$            col += tex2Dlod(_BehindWaterTex, float4(UV + offset,0,0))@$        }$    }$$    col /= kernelSum@$    return float4(col.r, col.g, col.b, col.a)@;4;Create;4;False;GrabScreen;FLOAT4;0,0,0,0;In;;Inherit;False;True;TexelSize;FLOAT2;0.001,0.001;In;;Inherit;False;False;UV;FLOAT2;0,0;In;;Inherit;False;False;Blur;FLOAT;1;In;;Inherit;False;Gaussian Blur;False;False;0;;False;4;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0.001,0.001;False;2;FLOAT2;0,0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;359;-1520,-1088;Inherit;False;P Light Shaft Color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;159;626,464;Inherit;False;10;P Surface Color Upper;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;156;626,544;Inherit;False;121;M Surface Mask 01;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;200;-1776,1728;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;306;1216,3968;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;331;1232,4448;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;343;1104,4976;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;165;-3008,-1456;Inherit;False;P Surface Color Lower;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;184;624,384;Inherit;False;183;M Water Color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;266;-1809.884,-2576.872;Inherit;False;Property;_AmbientWaveMul;Ambient Wave Mul;20;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;286;848,2640;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;264;640,1712;Inherit;False;228;P Ambient Wave Speed;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;257;704,1904;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;263;608,1536;Inherit;False;239;P Ambient Wave Amptitude;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;265;608,1632;Inherit;False;260;P Ambient Wave Frequency;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;256;704,1808;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;275;928,2384;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;157;946,416;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;163;898,656;Inherit;False;165;P Surface Color Lower;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;162;866,736;Inherit;False;133;M Surface Mask 02;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;360;1408,4576;Inherit;False;359;P Light Shaft Color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;332;1472,4176;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;345;1248,4976;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;364;1648,4624;Inherit;False;Alpha Split;-1;;2;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;201;-1536,1744;Inherit;False;M Under Water Color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;88;-2064,4144;Inherit;False;M Under Water Blur;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;267;-1552,-2560;Inherit;False;P Ambient Wave Mul;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;247;1584,2144;Inherit;False;1;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;291;1152,2416;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;251;960,1632;Inherit;False;float wave1 = WaveAmplitude.x * sin(VertexPos.x * WaveFrequency.x + Time * WaveSpeed.x)@$float wave2 = WaveAmplitude.y * sin(VertexPos.x * WaveFrequency.y + Time * WaveSpeed.y)@$float wave3 = WaveAmplitude.z * sin(VertexPos.x * WaveFrequency.z + Time * WaveSpeed.z)@$$return wave1 + wave2 + wave3@;1;Create;5;False;WaveAmplitude;FLOAT4;0,0,0,0;In;;Inherit;False;False;WaveFrequency;FLOAT4;0,0,0,0;In;;Inherit;False;False;WaveSpeed;FLOAT4;0,0,0,0;In;;Inherit;False;False;Time;FLOAT;0;In;;Inherit;False;True;VertexPos;FLOAT3;0,0,0;In;;Inherit;False;Ambient Wave Sum;False;False;0;;False;5;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT;0;False;4;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;161;1218,560;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;344;1904,4528;Inherit;False;4;4;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;192;1106,272;Inherit;False;201;M Under Water Color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;63;1106,192;Inherit;False;88;M Under Water Blur;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;248;1808,2208;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;268;1712,2432;Inherit;False;267;P Ambient Wave Mul;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;250;1744,2288;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;287;1360,2000;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;191;1378,208;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;160;1378,384;Inherit;False;Alpha Split;-1;;3;07dab7960105b86429ac8eebd729ed6d;0;1;2;COLOR;0,0,0,0;False;2;FLOAT3;0;FLOAT;6
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;296;2096,4528;Inherit;False;M Light Shaft Color Final;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;245;2064,2128;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;65;1650,272;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;297;1616,480;Inherit;False;296;M Light Shaft Color Final;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;243;2256,2128;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;363;1888,288;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;242;2416,2128;Inherit;False;M Ambient Wave Offset;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;67;2096,272;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;277;256,2592;Inherit;False;260;P Ambient Wave Frequency;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StickyNoteNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;128;-2448,3280;Inherit;False;374;108;New Note;;1,1,1,1;Given water surface a higher distortion value;0;0
Node;AmplifyShaderEditor.StickyNoteNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;224;-3280,1136;Inherit;False;742.6313;164.6506;New Note;;1,1,1,1;Use y of uv on channel 1 to get water depth info.;0;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;68;2272,272;Inherit;False;COLOR;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.IntNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;366;-1072,-2560;Inherit;False;Property;_StencilReference;Stencil Reference;24;0;Create;True;0;0;0;True;0;False;123;0;False;0;0;0;1;INT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;244;2192,464;Inherit;False;242;M Ambient Wave Offset;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;389;2512,272;Float;False;True;-1;3;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;17;Cainos/Interactive Pixel Water/Pixel Water;199187dac283dbe4a8cb1ea611d70c58;True;Sprite Lit;0;0;Sprite Lit;7;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;UniversalMaterialType=Lit;ShaderGraphShader=true;True;0;True;14;all;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;True;True;True;0;True;_StencilReference;255;False;;255;False;;0;False;;3;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;True;True;1;False;;True;3;False;;True;True;0;False;;0;False;;False;True;1;LightMode=Universal2D;False;False;0;;0;0;Standard;5;Alpha Clipping;0;0;Disable Color Tint;1;0;Vertex Position;1;0;Debug Display;0;0;External Alpha;0;0;0;5;True;True;True;True;True;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;390;2512,272;Float;False;False;-1;3;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;199187dac283dbe4a8cb1ea611d70c58;True;Sprite Normal;0;1;Sprite Normal;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;UniversalMaterialType=Lit;ShaderGraphShader=true;True;0;True;14;all;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;True;0;True;;255;False;;255;False;;0;False;;3;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;False;True;1;LightMode=NormalsRendering;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;391;2512,272;Float;False;False;-1;3;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;199187dac283dbe4a8cb1ea611d70c58;True;Sprite Forward;0;2;Sprite Forward;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;UniversalMaterialType=Lit;ShaderGraphShader=true;True;0;True;14;all;0;False;True;2;5;False;;10;False;;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;True;0;True;;255;False;;255;False;;0;False;;3;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;False;True;1;LightMode=UniversalForward;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;392;2512,272;Float;False;False;-1;3;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;199187dac283dbe4a8cb1ea611d70c58;True;SceneSelectionPass;0;3;SceneSelectionPass;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;UniversalMaterialType=Lit;ShaderGraphShader=true;True;0;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=SceneSelectionPass;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;393;2512,272;Float;False;False;-1;3;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;199187dac283dbe4a8cb1ea611d70c58;True;ScenePickingPass;0;4;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;UniversalMaterialType=Lit;ShaderGraphShader=true;True;0;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;False;0;;0;0;Standard;0;False;0
WireConnection;151;0;152;0
WireConnection;145;0;153;0
WireConnection;145;1;138;0
WireConnection;11;0;143;0
WireConnection;130;0;129;0
WireConnection;131;1;145;0
WireConnection;13;0;12;0
WireConnection;116;0;15;0
WireConnection;132;0;130;1
WireConnection;132;1;131;0
WireConnection;149;0;148;0
WireConnection;14;0;13;1
WireConnection;14;1;116;0
WireConnection;147;0;132;0
WireConnection;147;1;149;0
WireConnection;39;0;40;0
WireConnection;121;0;14;0
WireConnection;133;0;147;0
WireConnection;154;0;121;0
WireConnection;154;1;133;0
WireConnection;42;0;41;0
WireConnection;78;0;77;0
WireConnection;46;0;45;0
WireConnection;155;0;154;0
WireConnection;44;0;78;0
WireConnection;44;1;37;0
WireConnection;126;0;125;0
WireConnection;317;0;316;0
WireConnection;350;0;349;0
WireConnection;52;0;51;0
WireConnection;36;0;61;0
WireConnection;36;2;44;0
WireConnection;48;0;37;0
WireConnection;48;1;49;0
WireConnection;369;1;47;0
WireConnection;123;1;127;0
WireConnection;123;2;124;0
WireConnection;35;0;36;0
WireConnection;35;1;48;0
WireConnection;35;2;369;0
WireConnection;328;0;323;0
WireConnection;355;0;327;2
WireConnection;355;1;356;0
WireConnection;353;0;298;2
WireConnection;353;1;354;0
WireConnection;53;0;73;0
WireConnection;53;1;54;0
WireConnection;53;2;35;0
WireConnection;53;3;123;0
WireConnection;324;0;322;0
WireConnection;324;1;328;0
WireConnection;315;0;314;0
WireConnection;315;1;318;0
WireConnection;9;0;6;0
WireConnection;168;0;167;0
WireConnection;307;0;308;0
WireConnection;375;0;327;1
WireConnection;375;1;355;0
WireConnection;376;0;298;1
WireConnection;376;1;353;0
WireConnection;283;0;282;0
WireConnection;211;0;207;2
WireConnection;106;0;53;0
WireConnection;104;0;102;0
WireConnection;205;0;204;0
WireConnection;347;0;346;0
WireConnection;326;0;375;0
WireConnection;326;1;324;0
WireConnection;339;0;324;0
WireConnection;319;0;376;0
WireConnection;319;1;315;0
WireConnection;338;0;315;0
WireConnection;281;0;280;0
WireConnection;281;1;283;3
WireConnection;181;0;178;0
WireConnection;181;1;179;0
WireConnection;181;2;212;0
WireConnection;310;0;309;0
WireConnection;325;0;326;0
WireConnection;325;1;339;0
WireConnection;300;0;319;0
WireConnection;300;1;338;0
WireConnection;196;0;195;0
WireConnection;194;0;193;0
WireConnection;370;1;321;0
WireConnection;371;1;336;0
WireConnection;288;0;281;0
WireConnection;288;1;281;0
WireConnection;388;1;86;0
WireConnection;100;0;101;0
WireConnection;100;1;99;0
WireConnection;107;0;86;0
WireConnection;107;1;109;0
WireConnection;203;0;105;0
WireConnection;203;1;206;0
WireConnection;203;2;214;0
WireConnection;183;0;181;0
WireConnection;299;0;300;0
WireConnection;299;1;371;0
WireConnection;320;0;325;0
WireConnection;320;1;370;0
WireConnection;342;0;341;2
WireConnection;342;1;348;0
WireConnection;10;0;7;0
WireConnection;228;0;258;0
WireConnection;239;0;261;0
WireConnection;260;0;259;0
WireConnection;284;0;288;0
WireConnection;284;1;290;0
WireConnection;84;0;388;0
WireConnection;84;1;100;0
WireConnection;84;2;107;0
WireConnection;84;3;203;0
WireConnection;359;0;358;0
WireConnection;200;0;197;0
WireConnection;200;1;198;0
WireConnection;200;2;213;0
WireConnection;306;0;320;0
WireConnection;306;1;312;0
WireConnection;331;0;299;0
WireConnection;331;1;330;0
WireConnection;343;0;342;0
WireConnection;165;0;164;0
WireConnection;286;0;285;0
WireConnection;275;0;284;0
WireConnection;275;1;278;3
WireConnection;157;0;184;0
WireConnection;157;1;159;0
WireConnection;157;2;156;0
WireConnection;332;0;306;0
WireConnection;332;1;331;0
WireConnection;345;0;343;0
WireConnection;364;2;360;0
WireConnection;201;0;200;0
WireConnection;88;0;84;0
WireConnection;267;0;266;0
WireConnection;291;0;275;0
WireConnection;291;1;286;3
WireConnection;251;0;263;0
WireConnection;251;1;265;0
WireConnection;251;2;264;0
WireConnection;251;3;256;0
WireConnection;251;4;257;0
WireConnection;161;0;157;0
WireConnection;161;1;163;0
WireConnection;161;2;162;0
WireConnection;344;0;332;0
WireConnection;344;1;360;0
WireConnection;344;2;364;6
WireConnection;344;3;345;0
WireConnection;248;0;247;2
WireConnection;287;0;251;0
WireConnection;287;1;291;0
WireConnection;191;0;63;0
WireConnection;191;1;192;0
WireConnection;160;2;161;0
WireConnection;296;0;344;0
WireConnection;245;0;287;0
WireConnection;245;1;248;0
WireConnection;245;2;250;1
WireConnection;245;3;268;0
WireConnection;65;0;191;0
WireConnection;65;1;160;0
WireConnection;65;2;160;6
WireConnection;243;1;245;0
WireConnection;363;0;65;0
WireConnection;363;1;297;0
WireConnection;242;0;243;0
WireConnection;67;0;363;0
WireConnection;68;0;67;0
WireConnection;68;1;67;1
WireConnection;68;2;67;2
WireConnection;389;0;68;0
WireConnection;389;4;244;0
ASEEND*/
//CHKSM=1C981FC7287B5DFD31D6F6BBBADB543F7FD17528