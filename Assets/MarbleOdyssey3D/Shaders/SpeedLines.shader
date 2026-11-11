// ============================================================================
// Marble Odyssey 3D — SpeedLines.shader
// Screen-space radial speed lines overlay
// Intensity driven by marble speed, color tint
// ============================================================================
Shader "MarbleOdyssey/SpeedLines"
{
    Properties
    {
        [HDR] _Color     ("Line Color",    Color)            = (1, 1, 1, 0.5)
        _Intensity       ("Intensity",     Range(0, 2))      = 0.0  // Set by script
        _LineCount       ("Line Count",    Range(4, 64))     = 24
        _LineWidth       ("Line Width",    Range(0.001, 0.1)) = 0.02
        _InnerRadius     ("Inner Radius",  Range(0, 1))      = 0.3
        _ScrollSpeed     ("Scroll Speed",  Range(0, 20))     = 8.0
        _FadeEdge        ("Fade Edge",     Range(0.01, 0.5)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Overlay"
        }

        Pass
        {
            Name "SpeedLines"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Intensity;
                float  _LineCount;
                float  _LineWidth;
                float  _InnerRadius;
                float  _ScrollSpeed;
                float  _FadeEdge;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (_Intensity < 0.01) return half4(0, 0, 0, 0);

                // Center the UV
                float2 centeredUV = IN.uv - 0.5;

                // Polar coordinates
                float dist  = length(centeredUV) * 2.0;
                float angle = atan2(centeredUV.y, centeredUV.x);

                // Radial lines via angular repetition
                float lineAngle = angle * _LineCount / 6.28318;
                float lineFrac  = frac(lineAngle + _Time.y * _ScrollSpeed * 0.1);
                float line      = smoothstep(0.5 - _LineWidth, 0.5, lineFrac)
                                * smoothstep(0.5 + _LineWidth, 0.5, lineFrac);

                // Only show lines beyond inner radius
                float radialMask = smoothstep(_InnerRadius, _InnerRadius + _FadeEdge, dist);

                // Fade out at screen edges gently
                float edgeFade = smoothstep(1.0, 0.85, dist);

                float alpha = line * radialMask * edgeFade * _Intensity * _Color.a;

                return half4(_Color.rgb, saturate(alpha));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
