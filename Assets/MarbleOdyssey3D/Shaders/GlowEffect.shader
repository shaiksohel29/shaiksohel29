// ============================================================================
// Marble Odyssey 3D — GlowEffect.shader
// URP Fresnel-based glow for collectibles, checkpoints, finish zones
// Pulse animation, alpha distance fade
// ============================================================================
Shader "MarbleOdyssey/GlowEffect"
{
    Properties
    {
        [HDR] _GlowColor  ("Glow Color",      Color)          = (0, 2, 4, 1)
        _GlowPower        ("Glow Fresnel Power", Range(0.5, 8)) = 2.5
        _PulseSpeed        ("Pulse Speed",     Range(0, 10))    = 2.0
        _PulseMin          ("Pulse Min",       Range(0, 1))     = 0.3
        _BaseAlpha         ("Base Alpha",      Range(0, 1))     = 0.6
        _FadeStartDist     ("Fade Start Dist", Float)           = 30.0
        _FadeEndDist       ("Fade End Dist",   Float)           = 50.0
        _CoreBrightness    ("Core Brightness", Range(0, 2))     = 0.3
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
            Name "GlowForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float  distToCamera : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float  _GlowPower;
                float  _PulseSpeed;
                float  _PulseMin;
                float  _BaseAlpha;
                float  _FadeStartDist;
                float  _FadeEndDist;
                float  _CoreBrightness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   ni = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS  = pi.positionCS;
                OUT.normalWS    = ni.normalWS;
                OUT.viewDirWS   = GetWorldSpaceNormalizeViewDir(pi.positionWS);
                OUT.distToCamera = distance(pi.positionWS, _WorldSpaceCameraPos);
                OUT.fogFactor   = ComputeFogFactor(pi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 normal  = normalize(IN.normalWS);
                half3 viewDir = normalize(IN.viewDirWS);

                // Fresnel glow
                half  fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _GlowPower);

                // Pulse animation
                half  pulse = _PulseMin + (1.0 - _PulseMin)
                            * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed * 6.28318));

                // Core brightness so inner area isn't black
                half  glow = max(fresnel, _CoreBrightness) * pulse;

                half3 color = _GlowColor.rgb * glow;

                // Distance fade
                half  distFade = 1.0 - saturate(
                    (IN.distToCamera - _FadeStartDist) / max(_FadeEndDist - _FadeStartDist, 0.01));

                half  alpha = glow * _BaseAlpha * distFade;

                color = MixFog(color, IN.fogFactor);
                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
