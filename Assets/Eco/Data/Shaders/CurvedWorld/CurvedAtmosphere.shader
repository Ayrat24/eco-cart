Shader "Eco/Curved World/Atmosphere"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        HLSLINCLUDE
        #pragma target 3.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        TEXTURE2D_X_FLOAT(_AtmosphereSource);
        float4 _AtmosphereFieldSize; // inverse width/height, width/height
        float4 _AtmosphereShape; // radius in field pixels, falloff, strength
        float _AtmosphereStep;
        half4 _AtmosphereHorizonColor;
        half4 _AtmosphereSpaceColor;

        bool Surface(float d)
        {
            #if UNITY_REVERSED_Z
                return d > 0.0000001;
            #else
                return d < 0.9999999;
            #endif
        }

        float4 ReadField(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_AtmosphereSource, sampler_PointClamp, uv, 0);
        }

        float DistanceToEdge(float2 sampleUV, float2 uv)
        {
            float4 edge = ReadField(sampleUV);
            return edge.w > 0 ? length((uv - edge.xy) * _AtmosphereFieldSize.zw) : _AtmosphereShape.x + 2;
        }

        float FilteredDistance(float2 uv)
        {
            // Interpolate distances, not seed coordinates: interpolating two different seeds
            // would invent an edge between them. This also removes half-resolution stepping.
            float2 pixel = uv * _AtmosphereFieldSize.zw - 0.5;
            float2 baseUV = (floor(pixel) + 0.5) * _AtmosphereFieldSize.xy;
            float2 blend = frac(pixel);
            float a = DistanceToEdge(baseUV, uv);
            float b = DistanceToEdge(baseUV + float2(_AtmosphereFieldSize.x, 0), uv);
            float c = DistanceToEdge(baseUV + float2(0, _AtmosphereFieldSize.y), uv);
            float d = DistanceToEdge(baseUV + _AtmosphereFieldSize.xy, uv);
            return lerp(lerp(a, b, blend.x), lerp(c, d, blend.x), blend.y);
        }

        float4 Seed(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;
            float depth = ReadField(uv).r;
            if (!Surface(depth)) return 0;
            bool edge = false;
            [unroll] for (int y = -1; y <= 1; y++)
            [unroll] for (int x = -1; x <= 1; x++)
            {
                float2 neighbor = uv + float2(x,y) * _AtmosphereFieldSize.xy;
                // The image boundary is not a horizon: do not create a frame around the screen.
                if (all(neighbor > 0) && all(neighbor < 1))
                    edge = edge || !Surface(ReadField(neighbor).r);
            }
            return edge ? float4(uv, depth, 1) : 0;
        }

        float4 Propagate(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float4 best = 0;
            float bestDistance = 1e20;
            [unroll] for (int y = -1; y <= 1; y++)
            [unroll] for (int x = -1; x <= 1; x++)
            {
                float2 uv = input.texcoord + float2(x,y) * _AtmosphereFieldSize.xy * _AtmosphereStep;
                if (any(uv < 0) || any(uv > 1)) continue;
                float4 candidate = ReadField(uv);
                float2 offset = (candidate.xy - input.texcoord) * _AtmosphereFieldSize.zw;
                float distanceSquared = dot(offset,offset);
                if (candidate.w > 0 && distanceSquared < bestDistance)
                {
                    best = candidate;
                    bestDistance = distanceSquared;
                }
            }
            return best;
        }

        struct AtmosphereOutput
        {
            half4 color : SV_Target;
            float depth : SV_Depth;
        };

        AtmosphereOutput Composite(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            // Apparent band width must not depend on individual horizon triangles' depths.
            // At grazing angles those depths jump and create visible Voronoi-shaped streaks.
            float band = FilteredDistance(input.texcoord) / max(_AtmosphereShape.x, 0.01);
            clip(1 - band);
            float fade = pow(saturate(1 - smoothstep(0,1,band)), _AtmosphereShape.y);
            AtmosphereOutput output;
            output.color = half4(lerp(_AtmosphereHorizonColor.rgb, _AtmosphereSpaceColor.rgb, smoothstep(0,1,band)),
                saturate(fade * _AtmosphereShape.z));
            // Only shade uncovered sky. Full-resolution scene depth supplies the exact
            // silhouette and foreground occlusion, without changing water's depth texture.
            #if UNITY_REVERSED_Z
                output.depth = 0;
            #else
                output.depth = 1;
            #endif
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "Silhouette Seeds"
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Seed
            ENDHLSL
        }
        Pass
        {
            Name "Nearest Silhouette"
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Propagate
            ENDHLSL
        }
        Pass
        {
            Name "Occluded Atmosphere"
            Cull Off ZWrite Off ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha, Zero One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Composite
            ENDHLSL
        }
    }
}
