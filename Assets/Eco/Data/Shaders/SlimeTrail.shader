// Slime the jelly leaves on the ground (SlimeTrail): a strip mesh laid on the terrain. Every vertex carries when its
// point was laid and when it dries up, so drying and fading run here and the mesh only changes when a point is
// added or dropped. Wet sheen from the main light over a little noise. Queued with the opaques, before the grass
// and the jelly's contact shadow, so it lands in the camera opaque texture and shows through the refracting body.
// SlimeTrail sets the properties from SlimeTrailSettings; the defaults here only show in the material inspector.
Shader "Eco/SlimeTrail"
{
    Properties
    {
        _Color("Colour", Color) = (0.1, 0.62, 0.56, 0.35)
        _RimColor("Rim Colour", Color) = (0.06, 0.48, 0.44, 0.65)
        _RimWidth("Rim Width", Range(0, 1)) = 0.3
        _EdgeSoftness("Edge Softness", Range(0.01, 1)) = 0.25
        _EdgeWobble("Edge Wobble", Range(0, 0.5)) = 0.2
        _EdgeWobbleScale("Edge Wobble Scale", Float) = 1.3
        _DryShrink("Edges Pull In While Drying", Range(0, 1)) = 0.45
        _FadeTime("Fade Seconds Before Dry", Float) = 1.5
        _Gloss("Gloss", Range(4, 256)) = 60
        _Specular("Specular", Range(0, 4)) = 1.2
        _Fresnel("Fresnel", Range(0, 2)) = 0.5
        _DryGloss("Gloss Lost Drying", Range(0, 1)) = 0.7
        _BumpScale("Lump Frequency", Float) = 7
        _BumpStrength("Lump Strength", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "AlphaTest-10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SlimeTrail"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1, -1
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _RimColor;
                float _RimWidth;
                float _EdgeSoftness;
                float _EdgeWobble;
                float _EdgeWobbleScale;
                float _DryShrink;
                float _FadeTime;
                float _Gloss;
                float _Specular;
                float _Fresnel;
                float _DryGloss;
                float _BumpScale;
                float _BumpStrength;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                // x = across (-1 left edge, 1 right edge), y = metres along, z = laid at, w = dries up at
                float4 data : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 data : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            float SlimeHash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float SlimeNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                // Quintic: smooth slopes across cell borders, so no grid shows in the highlights
                f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float a = SlimeHash(i);
                float b = SlimeHash(i + float2(1, 0));
                float c = SlimeHash(i + float2(0, 1));
                float d = SlimeHash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Soft lumps in the slime's surface: two octaves of noise, the second turned, so nothing repeats
            float SlimeLumps(float2 p)
            {
                float2 turned = float2(0.8 * p.x - 0.6 * p.y, 0.6 * p.x + 0.8 * p.y);
                return SlimeNoise(p) + 0.5 * SlimeNoise(turned * 2.03 + 17.1);
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = EcoBendWorldPosition(TransformObjectToWorld(input.positionOS), _EcoWorldBend);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.data = input.data;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float now = _TimeParameters.x;
                float laidAt = input.data.z;
                float driesAt = input.data.w;
                float dryness = saturate((now - laidAt) / max(driesAt - laidAt, 0.001));

                // The edges wander along the trail and pull in as it dries
                float side = input.data.x > 0.0 ? 7.31 : -3.17;
                float wobble = (SlimeNoise(float2(input.data.y * _EdgeWobbleScale, side)) - 0.5) * 2.0 * _EdgeWobble;
                float edge = 1.0 - _DryShrink * dryness + wobble;
                float d = abs(input.data.x);
                float body = 1.0 - smoothstep(edge - _EdgeSoftness, edge, d);
                float alpha = body * saturate((driesAt - now) / max(_FadeTime, 0.001));
                clip(alpha - 0.004);

                // Slime gathers thicker along the rim
                float rim = smoothstep(edge - _EdgeSoftness - _RimWidth, edge - _EdgeSoftness * 0.5, d);

                const float step = 0.1;
                float2 p = input.positionWS.xz * _BumpScale;
                float h = SlimeLumps(p);
                float slopeX = (h - SlimeLumps(p + float2(step, 0.0))) / step;
                float slopeZ = (h - SlimeLumps(p + float2(0.0, step))) / step;
                float3 normalWS = normalize(float3(slopeX * _BumpStrength, 1.0, slopeZ * _BumpStrength));

                Light light = GetMainLight();
                float3 viewWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(light.direction + viewWS);
                float wet = 1.0 - dryness * _DryGloss;
                float specular = pow(saturate(dot(normalWS, halfDir)), _Gloss) * _Specular * wet;
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewWS)), 4.0) * _Fresnel * wet;

                float4 tint = lerp(_Color, _RimColor, rim);
                half3 color = tint.rgb * (light.color * saturate(dot(normalWS, light.direction)) + SampleSH(normalWS));
                color += light.color * (specular + fresnel);
                color = MixFog(color, input.fogFactor);
                return half4(color, saturate(alpha * (tint.a + specular + fresnel)));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
