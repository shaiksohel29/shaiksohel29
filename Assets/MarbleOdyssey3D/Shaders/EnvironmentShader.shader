// ============================================================================
// Marble Odyssey 3D — EnvironmentShader.shader
// URP triplanar-mapped environment for platforms/obstacles
// Per-world color tint, edge emission, vertex displacement, mobile-friendly
// ============================================================================
Shader "MarbleOdyssey/Environment"
{
    Properties
    {
        [Header(Base)]
        _BaseColor     ("Base Color",       Color)              = (0.6, 0.65, 0.7, 1)
        _BaseMap       ("Albedo (RGB)",     2D)                 = "white" {}
        _TriplanarScale("Triplanar Scale",  Range(0.1, 10))     = 1.0
        _TriplanarBlend("Triplanar Blend",  Range(1, 8))        = 4.0
        _Metallic      ("Metallic",         Range(0, 1))        = 0.1
        _Smoothness    ("Smoothness",       Range(0, 1))        = 0.5

        [Header(Emission Edge)]
        [HDR] _EdgeEmission ("Edge Emission Color", Color)      = (0, 0.5, 1, 1)
        _EdgeWidth     ("Edge Width",       Range(0, 1))        = 0.15
        _EdgePulseSpeed("Edge Pulse Speed", Range(0, 5))        = 1.0

        [Header(Vertex Displacement)]
        _DisplaceAmount("Displace Amount",  Range(0, 2))        = 0.0
        _DisplaceSpeed ("Displace Speed",   Range(0, 5))        = 1.0
        _DisplaceAxis  ("Displace Axis (XYZ)", Vector)          = (0, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back ZWrite On

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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _TriplanarScale;
                float  _TriplanarBlend;
                float  _Metallic;
                float  _Smoothness;
                float4 _EdgeEmission;
                float  _EdgeWidth;
                float  _EdgePulseSpeed;
                float  _DisplaceAmount;
                float  _DisplaceSpeed;
                float4 _DisplaceAxis;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Vertex displacement (for animated obstacles)
                float3 displaced = IN.positionOS.xyz;
                if (_DisplaceAmount > 0.001)
                {
                    float wave = sin(_Time.y * _DisplaceSpeed + dot(IN.positionOS.xyz, float3(1,1,1)) * 2.0);
                    displaced += _DisplaceAxis.xyz * wave * _DisplaceAmount;
                }

                VertexPositionInputs posInputs = GetVertexPositionInputs(displaced);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = nrmInputs.normalWS;
                OUT.viewDirWS  = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                OUT.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            // Triplanar sampling — no UV seams on procedural geometry
            half3 TriplanarSample(float3 posWS, float3 blendWeights)
            {
                float2 uvX = posWS.yz * _TriplanarScale;
                float2 uvY = posWS.xz * _TriplanarScale;
                float2 uvZ = posWS.xy * _TriplanarScale;

                half3 sX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb;
                half3 sY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY).rgb;
                half3 sZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb;

                return sX * blendWeights.x + sY * blendWeights.y + sZ * blendWeights.z;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 normalWS = normalize(IN.normalWS);
                half3 viewDirWS = normalize(IN.viewDirWS);

                // Triplanar blend weights
                float3 absNrm = abs(normalWS);
                float3 blendW = pow(absNrm, _TriplanarBlend);
                blendW /= (blendW.x + blendW.y + blendW.z + 0.0001);

                half3 triTex = TriplanarSample(IN.positionWS, blendW);
                half3 albedo = triTex * _BaseColor.rgb;

                // Lighting
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight    = GetMainLight(shadowCoord);

                half  NdotL   = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = albedo * mainLight.color * NdotL * mainLight.shadowAttenuation;

                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half  NdotH   = saturate(dot(normalWS, halfDir));
                half  spec    = pow(NdotH, lerp(1, 128, _Smoothness)) * _Metallic;
                half3 specColor = mainLight.color * spec * mainLight.shadowAttenuation;

                half3 ambient = SampleSH(normalWS) * albedo;

                // Edge emission — Fresnel-based glowing edges
                half  fresnel  = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 3.0);
                half  edgeMask = smoothstep(1.0 - _EdgeWidth, 1.0, fresnel);
                half  pulse    = 0.7 + 0.3 * sin(_Time.y * _EdgePulseSpeed * 3.14159);
                half3 edge     = _EdgeEmission.rgb * edgeMask * pulse;

                // Additional lights
                #ifdef _ADDITIONAL_LIGHTS
                uint addCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < addCount; i++)
                {
                    Light aL = GetAdditionalLight(i, IN.positionWS);
                    diffuse += albedo * aL.color * saturate(dot(normalWS, aL.direction))
                             * aL.distanceAttenuation * aL.shadowAttenuation;
                }
                #endif

                half3 color = ambient + diffuse + specColor + edge;
                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual Cull Back
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float3 _LightDirection;
            struct A { float4 posOS : POSITION; float3 nrmOS : NORMAL; };
            struct V { float4 posCS : SV_POSITION; };
            V ShadowVert(A i)
            {
                V o;
                float3 pw = TransformObjectToWorld(i.posOS.xyz);
                float3 nw = TransformObjectToWorldNormal(i.nrmOS);
                pw = ApplyShadowBias(pw, nw, _LightDirection);
                o.posCS = TransformWorldToHClip(pw);
                #if UNITY_REVERSED_Z
                    o.posCS.z = min(o.posCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.posCS.z = max(o.posCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 ShadowFrag(V i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
