// Soft tinted shadow under the jelly creature, drawn on a ground quad. Light through jelly comes out tinted
// rather than blocked, so the body itself casts no real shadow (it would also darken the items inside it).
// Queued with the opaques so it lands in the camera opaque texture and shows through the refracting body.
Shader "Eco/JellyContactShadow"
{
    Properties
    {
        _Color("Shadow Tint", Color) = (0.3, 0.55, 0.5, 1)
        _Strength("Strength", Range(0, 1)) = 0.65
        _Softness("Softness", Range(0.01, 1)) = 0.55
        _CausticColor("Caustic Colour", Color) = (0.35, 0.6, 0.5, 1)
        _CausticStrength("Caustic Strength", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ContactShadow"
            Tags { "LightMode" = "UniversalForward" }

            // 2x multiply: 0.5 keeps the ground as is, lower darkens, higher brightens
            Blend DstColor SrcColor
            ZWrite Off
            Offset -1, -1
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Strength;
                float _Softness;
                float4 _CausticColor;
                float _CausticStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * 2.0 - 1.0;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float d = length(input.uv);
                float mask = 1.0 - smoothstep(1.0 - _Softness, 1.0, d);

                float t = _Time.y;
                float2 p = input.uv * 6.0;
                float caustic = sin(p.x + sin(p.y * 1.3 + t * 1.1)) * sin(p.y + sin(p.x * 1.7 - t * 0.9));
                caustic = pow(saturate(caustic), 3.0) * saturate(1.0 - d * 1.4) * _CausticStrength;

                float3 factor = lerp(1.0, _Color.rgb, _Strength * mask) + _CausticColor.rgb * (caustic * mask);
                return half4(saturate(factor * 0.5), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
