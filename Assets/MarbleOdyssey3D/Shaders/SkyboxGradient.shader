// ============================================================================
// Marble Odyssey 3D — SkyboxGradient.shader
// Custom gradient skybox with star field overlay, per-world customizable
// ============================================================================
Shader "MarbleOdyssey/SkyboxGradient"
{
    Properties
    {
        [Header(Gradient)]
        _TopColor     ("Top Color",      Color) = (0.05, 0.05, 0.2, 1)
        _HorizonColor ("Horizon Color",  Color) = (0.4, 0.2, 0.6, 1)
        _BottomColor  ("Bottom Color",   Color) = (0.02, 0.02, 0.05, 1)
        _GradientPower("Gradient Power", Range(0.2, 5)) = 1.5
        _HorizonOffset("Horizon Offset", Range(-0.5, 0.5)) = 0.0

        [Header(Stars)]
        _StarDensity  ("Star Density",   Range(0, 500))   = 200
        _StarBrightness("Star Brightness",Range(0, 3))    = 1.0
        _StarTwinkle  ("Twinkle Speed",  Range(0, 5))     = 1.5
        _StarSize     ("Star Size",      Range(0.5, 5))   = 1.5

        [Header(Animation)]
        _RotationSpeed("Rotation Speed", Range(0, 0.1))   = 0.005
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Background"
            "Queue"          = "Background"
            "PreviewType"    = "Skybox"
        }

        Pass
        {
            Name "SkyboxGradient"
            Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir    : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _TopColor;
                float4 _HorizonColor;
                float4 _BottomColor;
                float  _GradientPower;
                float  _HorizonOffset;
                float  _StarDensity;
                float  _StarBrightness;
                float  _StarTwinkle;
                float  _StarSize;
                float  _RotationSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.viewDir    = IN.positionOS.xyz;
                return OUT;
            }

            // Hash functions for procedural star field
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Stars(float3 dir, float density, float size)
            {
                // Apply slow rotation
                float angle = _Time.y * _RotationSpeed;
                float cosA = cos(angle);
                float sinA = sin(angle);
                float3 rotDir = float3(
                    dir.x * cosA - dir.z * sinA,
                    dir.y,
                    dir.x * sinA + dir.z * cosA
                );

                // Convert to spherical-ish grid
                float2 uv = float2(
                    atan2(rotDir.z, rotDir.x) / 6.28318 + 0.5,
                    asin(saturate(rotDir.y)) / 1.5708
                );

                float2 gridUV = uv * density;
                float2 gridID = floor(gridUV);
                float2 gridFrac = frac(gridUV) - 0.5;

                float star = 0.0;
                // Check neighbors for smoother stars
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y);
                        float2 cellID = gridID + offset;
                        float  rnd    = hash21(cellID);

                        // Random position within cell
                        float2 starPos = offset + float2(
                            hash21(cellID + 100.0) - 0.5,
                            hash21(cellID + 200.0) - 0.5
                        ) * 0.8;

                        float d = length(gridFrac - starPos);

                        // Star brightness based on random seed
                        float brightness = smoothstep(size * 0.01, 0.0, d);
                        brightness *= rnd;

                        // Twinkle
                        float twinkle = sin(_Time.y * _StarTwinkle * (rnd * 4.0 + 1.0) + rnd * 6.28);
                        brightness *= 0.6 + 0.4 * twinkle;

                        star += brightness;
                    }
                }
                return star;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.viewDir);

                // Vertical gradient: y ranges -1 to 1
                float  t      = dir.y + _HorizonOffset;
                float  topMix = saturate(pow(saturate(t), _GradientPower));
                float  botMix = saturate(pow(saturate(-t), _GradientPower));

                half3 gradient = lerp(_HorizonColor.rgb, _TopColor.rgb, topMix);
                gradient       = lerp(gradient, _BottomColor.rgb, botMix);

                // Stars (only above horizon)
                float starMask = smoothstep(-0.1, 0.2, dir.y);
                float starField = Stars(dir, _StarDensity, _StarSize);
                half3 stars = starField * _StarBrightness * starMask;

                return half4(gradient + stars, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
