Shader "Roaming/Forest River"
{
    Properties
    {
        _WaveTex ("Wave Texture", 2D) = "gray" {}
        _ShallowColor ("Living Shallow", Color) = (0.12, 0.62, 0.9, 1)
        _DeepColor ("Living Deep", Color) = (0.025, 0.30, 0.66, 1)
        _DeadShallowColor ("Dead Shallow", Color) = (0.055, 0.32, 0.56, 1)
        _DeadDeepColor ("Dead Deep", Color) = (0.014, 0.095, 0.25, 1)
        _FoamColor ("Foam", Color) = (0.72, 0.93, 1, 1)
        _DarkStart ("Dark Start Z", Float) = 100
        _DarkEnd ("Dark End Z", Float) = 350
        _WaveScale ("Wave Scale", Float) = 2.2
        _WaveSpeed ("Flow Speed (negative reverses current)", Float) = -0.16
        _Opacity ("Opacity", Range(0, 1)) = 0.97
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "RiverForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_WaveTex);
            SAMPLER(sampler_WaveTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _WaveTex_ST;
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _DeadShallowColor;
                float4 _DeadDeepColor;
                float4 _FoamColor;
                float _DarkStart;
                float _DarkEnd;
                float _WaveScale;
                float _WaveSpeed;
                float _Opacity;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 uv : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // All patterns share the same longitudinal flow, following the river bends.
                float flow = input.uv.y + _Time.y * _WaveSpeed;
                half edge = saturate(abs(input.uv.x - 0.5) * 2.0);
                float contour = edge + sin(flow * 3.0 + input.uv.x * 8.0) * 0.035;
                float aa = max(fwidth(contour), 0.004);
                // Three broad blue tones instead of cloudy, realistic-looking noise.
                half depth = 1.0 - 0.38 * smoothstep(0.48-aa, 0.48+aa, contour)
                    - 0.62 * smoothstep(0.86-aa, 0.86+aa, contour);
                half darkProgress = saturate((input.positionWS.z - _DarkStart) / max(0.01, _DarkEnd - _DarkStart));
                half3 living = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth);
                half3 dead = lerp(_DeadShallowColor.rgb, _DeadDeepColor.rgb, depth);
                half3 color = lerp(living, dead, smoothstep(0.0, 1.0, darkProgress));

                // Sparse curved foam strokes make the direction easy to read.
                float2 cells = float2(input.uv.x * 5.0, flow * _WaveScale);
                cells.y += floor(cells.x) * 0.37;
                float2 id = floor(cells);
                float random = frac(sin(dot(id, float2(127.1, 311.7))) * 43758.5453);
                float2 local = frac(cells) - 0.5;
                float strokeDistance = local.y - (local.x * local.x * 0.35) - (random - 0.5) * 0.3;
                float lineAA = max(fwidth(strokeDistance), 0.003);
                half stroke = 1.0 - smoothstep(0.019-lineAA, 0.019+lineAA, abs(strokeDistance));
                stroke *= 1.0 - smoothstep(0.23, 0.34, abs(local.x));
                stroke *= step(0.38, random) * (1.0-smoothstep(0.90, 0.98, edge));
                half bankFoam = smoothstep(0.962, 0.99, contour) * 0.32;
                half foamMask = max(stroke * 0.72, bankFoam);
                half3 foam = lerp(_FoamColor.rgb, _FoamColor.rgb * half3(0.4, 0.55, 0.72), darkProgress);
                color = lerp(color, foam, foamMask);
                half alpha = saturate(_Opacity + depth * 0.025 + foamMask * 0.03);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
