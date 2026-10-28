Shader "Eco/Curved World/Atmosphere"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Atmosphere"
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha, Zero One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _AtmosphereBend;
            float4 _AtmosphereShape; // thickness, softness, strength, reference surface height
            float4 _AtmosphereDistance; // start, full strength
            half4 _AtmosphereHorizonColor;
            half4 _AtmosphereSpaceColor;

            // Minimum height of a view ray above the same parabolic surface used by world shaders.
            // Four Newton steps include the flat interaction radius without ray marching a volume.
            float ClosestApproach(float3 origin, float3 direction)
            {
                float2 offset = origin.xz - _AtmosphereBend.xy;
                float horizontalSpeed2 = max(dot(direction.xz, direction.xz), 0.00001);
                float curvature = _AtmosphereBend.z;
                float flatRadius = _AtmosphereBend.w;
                float t = max(0, (-direction.y / (2 * curvature) - dot(offset, direction.xz)) / horizontalSpeed2
                    + flatRadius * rsqrt(horizontalSpeed2));
                t = min(t, _ProjectionParams.z);
                [unroll] for (int i = 0; i < 4; i++)
                {
                    float2 p = offset + direction.xz * t;
                    float radius = max(length(p), 0.0001);
                    float radialSpeed = dot(p, direction.xz) / radius;
                    float beyond = max(0, radius - flatRadius);
                    float derivative = direction.y + 2 * curvature * beyond * radialSpeed;
                    float secondDerivative = 2 * curvature * (radialSpeed * radialSpeed
                        + beyond / radius * (horizontalSpeed2 - radialSpeed * radialSpeed));
                    t = clamp(t - derivative / max(secondDerivative, 0.00001), 0, _ProjectionParams.z);
                }
                return t;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                #if UNITY_REVERSED_Z
                    float nearDepth = 1;
                    float farDepth = 0;
                #else
                    float nearDepth = UNITY_NEAR_CLIP_VALUE;
                    float farDepth = 1;
                #endif
                float3 nearWS = ComputeWorldSpacePosition(uv, nearDepth, UNITY_MATRIX_I_VP);
                // Avoid an infinite far-plane homogeneous divide.
                float3 farWS = ComputeWorldSpacePosition(uv, lerp(farDepth, nearDepth, 0.001), UNITY_MATRIX_I_VP);
                float3 direction = normalize(farWS - nearWS);
                float3 origin = unity_OrthoParams.w > 0.5 ? nearWS : GetCameraPositionWS();
                // Use the complete ray's tangent height, never its intersection with terrain
                // or the seabed. Clamping to scene depth turned every visible surface into fog.
                float t = ClosestApproach(origin, direction);
                float3 sampleWS = origin + direction * t;
                float distanceBeyond = max(0, length(sampleWS.xz - _AtmosphereBend.xy) - _AtmosphereBend.w);
                float height = sampleWS.y - _AtmosphereShape.w + _AtmosphereBend.z * distanceBeyond * distanceBeyond;
                float width = max(_AtmosphereShape.x, 0.01);
                // Compact support: exactly zero outside the rim. Only a quarter of the
                // thickness extends onto the planet; most of the gradient is in the sky.
                float outer = 1 - smoothstep(0, width, max(0, height));
                float inner = 1 - smoothstep(0, width * 0.25, max(0, -height));
                float fade = pow(outer * inner, _AtmosphereShape.y);
                float distanceFade = smoothstep(_AtmosphereDistance.x, _AtmosphereDistance.y, t);
                half3 color = lerp(_AtmosphereHorizonColor.rgb, _AtmosphereSpaceColor.rgb,
                    smoothstep(0, 1, max(0, height) / width));
                return half4(color, saturate(fade * distanceFade * _AtmosphereShape.z));
            }
            ENDHLSL
        }
    }
}
