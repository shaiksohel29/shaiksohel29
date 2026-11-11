// ============================================================================
// Marble Odyssey 3D — LaserBeam.shader
// Scrolling additive laser beam for obstacles
// Warning flash, intensity pulse
// ============================================================================
Shader "MarbleOdyssey/LaserBeam"
{
    Properties
    {
        [HDR] _Color      ("Beam Color",       Color)            = (4, 0.2, 0.2, 1)
        [HDR] _WarningColor("Warning Flash Color", Color)        = (8, 8, 0, 1)
        _MainTex           ("Beam Texture",    2D)               = "white" {}
        _ScrollSpeed       ("Scroll Speed",    Range(0, 10))     = 3.0
        _PulseSpeed        ("Pulse Speed",     Range(0, 10))     = 2.0
        _PulseMin          ("Pulse Min",       Range(0, 1))      = 0.4
        _WarningInterval   ("Warning Interval (s)", Float)       = 3.0
        _WarningDuration   ("Warning Duration (s)", Float)       = 0.3
        _CoreWidth         ("Core Width",      Range(0.01, 1))   = 0.3
        _GlowFalloff       ("Glow Falloff",    Range(0.5, 8))    = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+10"
        }

        Pass
        {
            Name "LaserBeam"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One  // Additive
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _WarningColor;
                float4 _MainTex_ST;
                float  _ScrollSpeed;
                float  _PulseSpeed;
                float  _PulseMin;
                float  _WarningInterval;
                float  _WarningDuration;
                float  _CoreWidth;
                float  _GlowFalloff;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pi.positionCS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.viewDirWS  = GetWorldSpaceNormalizeViewDir(pi.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Scrolling UV along beam length (U axis)
                float2 scrollUV = IN.uv;
                scrollUV.x += _Time.y * _ScrollSpeed;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrollUV);

                // Core / edge gradient (V = 0 at edges, 0.5 at center)
                float edgeDist   = abs(IN.uv.y - 0.5) * 2.0;
                float coreGlow   = 1.0 - smoothstep(0.0, _CoreWidth, edgeDist);
                float outerGlow  = pow(1.0 - edgeDist, _GlowFalloff);
                float beamShape  = max(coreGlow, outerGlow * 0.4);

                // Intensity pulse
                float pulse = _PulseMin + (1.0 - _PulseMin)
                            * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed * 6.28318));

                // Warning flash: periodic bright flash
                float warnT     = fmod(_Time.y, _WarningInterval);
                float warnFlash = smoothstep(_WarningDuration, 0.0, warnT);

                // Blend colors
                half3 baseColor = lerp(_Color.rgb, _WarningColor.rgb, warnFlash);

                half3 color = baseColor * tex.rgb * beamShape * pulse;

                // View-angle softening (soften edges at grazing angles)
                float viewSoft = saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS)));
                viewSoft = lerp(0.3, 1.0, viewSoft);

                return half4(color * viewSoft, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
