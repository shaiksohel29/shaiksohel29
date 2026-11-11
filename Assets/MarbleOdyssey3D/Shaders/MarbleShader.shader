// ============================================================================
// Marble Odyssey 3D — MarbleShader.shader
// URP PBR marble with fresnel rim, emission, glass transparency, reflections
// Mobile-optimized — no raymarching
// ============================================================================
Shader "MarbleOdyssey/Marble"
{
    Properties
    {
        [Header(Base)]
        _BaseColor     ("Base Color",       Color)          = (0.8, 0.85, 0.95, 1)
        _BaseMap       ("Albedo (RGB)",     2D)             = "white" {}
        _Metallic      ("Metallic",         Range(0,1))     = 0.3
        _Smoothness    ("Smoothness",       Range(0,1))     = 0.9

        [Header(Normal)]
        _BumpMap       ("Normal Map",       2D)             = "bump" {}
        _BumpScale     ("Normal Scale",     Range(0,2))     = 1.0

        [Header(Emission)]
        [HDR] _EmissionColor ("Emission Color", Color)      = (0,0,0,1)
        _EmissionMap   ("Emission Map",     2D)             = "black" {}

        [Header(Fresnel Rim)]
        [HDR] _RimColor ("Rim Color",       Color)          = (0.4, 0.7, 1.0, 1)
        _RimPower      ("Rim Power",        Range(0.5, 8))  = 3.0
        _RimIntensity  ("Rim Intensity",    Range(0, 3))    = 1.0

        [Header(Transparency)]
        _Alpha         ("Alpha",            Range(0,1))     = 1.0

        [Header(Reflection)]
        _ReflectionStrength ("Reflection Strength", Range(0,1)) = 0.5

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite ("Z Write", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 viewDirWS   : TEXCOORD5;
                float  fogFactor   : TEXCOORD6;
            };

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);     SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _Metallic;
                float  _Smoothness;
                float  _BumpScale;
                float4 _EmissionColor;
                float4 _RimColor;
                float  _RimPower;
                float  _RimIntensity;
                float  _Alpha;
                float  _ReflectionStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS  = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = nrmInputs.normalWS;
                OUT.tangentWS   = nrmInputs.tangentWS;
                OUT.bitangentWS = nrmInputs.bitangentWS;
                OUT.viewDirWS   = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Albedo
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 albedo  = baseMap.rgb * _BaseColor.rgb;

                // Normal mapping
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                float3x3 TBN = float3x3(
                    normalize(IN.tangentWS),
                    normalize(IN.bitangentWS),
                    normalize(IN.normalWS));
                half3 normalWS = normalize(mul(normalTS, TBN));

                // View direction
                half3 viewDirWS = normalize(IN.viewDirWS);

                // Main light
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight    = GetMainLight(shadowCoord);

                // Simple PBR (mobile-friendly)
                half  NdotL    = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse  = albedo * mainLight.color * NdotL * mainLight.shadowAttenuation;
                half3 halfDir  = normalize(mainLight.direction + viewDirWS);
                half  NdotH    = saturate(dot(normalWS, halfDir));
                half  specular = pow(NdotH, lerp(1, 256, _Smoothness)) * _Metallic;
                half3 spec     = mainLight.color * specular * mainLight.shadowAttenuation;

                // Ambient / SH
                half3 ambient = SampleSH(normalWS) * albedo;

                // Reflection probe
                half3 reflDir   = reflect(-viewDirWS, normalWS);
                half  mip       = (1.0 - _Smoothness) * 6.0;
                half3 envSample = GlossyEnvironmentReflection(reflDir, mip, 1.0);
                half3 reflection = envSample * _ReflectionStrength * lerp(half3(1,1,1), albedo, _Metallic);

                // Fresnel rim
                half fresnel   = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower);
                half3 rimColor = _RimColor.rgb * fresnel * _RimIntensity;

                // Emission
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb
                               * _EmissionColor.rgb;

                // Additional lights (simple loop)
                #ifdef _ADDITIONAL_LIGHTS
                uint addCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < addCount; i++)
                {
                    Light addLight = GetAdditionalLight(i, IN.positionWS);
                    half  aNdotL   = saturate(dot(normalWS, addLight.direction));
                    diffuse += albedo * addLight.color * aNdotL
                             * addLight.distanceAttenuation * addLight.shadowAttenuation;
                }
                #endif

                half3 color = ambient + diffuse + spec + reflection + rimColor + emission;
                color = MixFog(color, IN.fogFactor);

                half  alpha = _Alpha * _BaseColor.a * baseMap.a;
                return half4(color, alpha);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual Cull Back
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nrmWS = TransformObjectToWorldNormal(IN.normalOS);
                posWS = ApplyShadowBias(posWS, nrmWS, _LightDirection);
                OUT.positionCS = TransformWorldToHClip(posWS);
                #if UNITY_REVERSED_Z
                    OUT.positionCS.z = min(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    OUT.positionCS.z = max(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
