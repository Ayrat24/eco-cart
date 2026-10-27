// Semi-transparent jelly for the player creature and its tentacles.
// With _JELLY_REFRACTION the scene behind is read from the camera opaque texture, bent and tinted by how much
// jelly the view ray crosses, so swallowed items read as floating inside. Without it (no opaque texture, e.g. the
// Mobile quality level) the surface alpha-blends instead. JellyBody switches the keyword to match the pipeline.
// _JELLY_BODY turns on the wobble / squash / ground contact / mouth deformation driven by JellyBody.
Shader "Eco/Jelly"
{
    Properties
    {
        [Header(Colour)]
        _Color("Scatter Colour", Color) = (0.08, 0.62, 0.58, 1)
        _TransmitColor("Transmit Colour (per metre)", Color) = (0.42, 0.86, 0.82, 1)
        _Density("Density", Range(0, 4)) = 1
        _MinAlpha("Min Alpha (without refraction)", Range(0, 1)) = 0.35
        _Thickness("Thickness (metres)", Float) = 2
        [Toggle(_JELLY_REFRACTION)] _RefractionOn("Refraction (needs Opaque Texture)", Float) = 1
        _Refraction("Refraction Strength", Range(0, 0.2)) = 0.04

        [Header(Lighting)]
        _Wrap("Diffuse Wrap", Range(0, 1)) = 0.6
        _SpecPower("Specular Power", Range(8, 1024)) = 220
        _SpecStrength("Specular Strength", Range(0, 4)) = 1.6
        _ReflectStrength("Reflection Strength", Range(0, 2)) = 0.6
        _Roughness("Reflection Roughness", Range(0, 1)) = 0.12
        _RimColor("Rim Colour", Color) = (0.55, 1, 0.92, 1)
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3
        _TransStrength("Back Light Strength", Range(0, 2)) = 0.5
        _TransPower("Back Light Power", Range(1, 16)) = 4
        _ShadowReceive("Received Shadow Strength", Range(0, 1)) = 0.7

        [Header(Body)]
        [Toggle(_JELLY_BODY)] _BodyOn("Body Deformation", Float) = 0
        _JellyHeight("Rest Height (object space)", Float) = 1.5
        _Wobble("Wobble Offset", Vector) = (0, 0, 0, 0)
        _Squash("Squash", Float) = 0
        _Sink("Sink Into Ground", Float) = 0.05
        _GroundSpread("Ground Spread", Float) = 0.6
        _ContactHeight("Contact Band Height", Float) = 0.12
        _ContactColor("Contact Line Colour", Color) = (0.7, 1, 0.95, 1)
        _Jiggle("Jiggle (amplitude, speed, frequency)", Vector) = (0.01, 2.5, 1.5, 0)

        [Header(Mouth)]
        _MouthCenter("Mouth Centre (xyz rest position, w open)", Vector) = (0, 0.9, 0.8, 0)
        _MouthShape("Mouth Shape (half width, half height, depth, smile)", Vector) = (0.3, 0.12, 0.25, 0.25)
        _MouthLine("Closed Mouth Line", Range(0, 1)) = 0.3
        _MouthColor("Mouth Colour", Color) = (0.02, 0.12, 0.12, 0.9)

        [Header(Ripples)]
        [HideInInspector] _Ripple0("Ripple 0", Vector) = (0, 0, 0, -1000)
        [HideInInspector] _Ripple1("Ripple 1", Vector) = (0, 0, 0, -1000)
        [HideInInspector] _Ripple2("Ripple 2", Vector) = (0, 0, 0, -1000)
        [HideInInspector] _Ripple3("Ripple 3", Vector) = (0, 0, 0, -1000)
        [HideInInspector] _RippleAmp("Ripple Amplitudes", Vector) = (0, 0, 0, 0)
        _RippleShape("Ripple Shape (speed, width, decay)", Vector) = (2.2, 0.25, 2.5, 0)

        [Header(Digest)]
        _Digest("Digest", Range(0, 1)) = 0
        _DigestColor("Digest Fizz Colour", Color) = (0.6, 1, 0.7, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            // Writes depth so tentacles and items behind or inside the body are hidden by it
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex JellyVertex
            #pragma fragment JellyFragment

            #pragma shader_feature_local _JELLY_BODY
            // Not a shader_feature: JellyBody turns refraction off at runtime when the pipeline has no opaque
            // texture, and no material asset carries that variant for the build to keep
            #pragma multi_compile_local_fragment _ _JELLY_REFRACTION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "JellyCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                // xyz: rest object position (mouth and fizz stay glued to the surface), w: deformed height
                float4 restOS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings JellyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS;
                float3 normalOS;
                JellyDeformVertex(input.positionOS.xyz, input.normalOS, input.tangentOS, positionOS, normalOS);

                VertexPositionInputs positions = GetVertexPositionInputs(positionOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.restOS = float4(input.positionOS.xyz, positionOS.y);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 JellyFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 n = normalize(input.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float nDotV = saturate(dot(n, v));
                float fresnel = pow(1.0 - nDotV, _FresnelPower);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float shadow = lerp(1.0, mainLight.shadowAttenuation, _ShadowReceive);
                float3 lightColor = mainLight.color * (mainLight.distanceAttenuation * shadow);
                float3 l = mainLight.direction;

                // Light scattered inside the jelly: wrapped diffuse plus light shining through from behind
                float wrap = saturate((dot(n, l) + _Wrap) / (1.0 + _Wrap));
                float back = pow(saturate(dot(v, -normalize(l + n * 0.4))), _TransPower) * _TransStrength;
                float3 ambient = SampleSH(n);
                float3 scatter = _Color.rgb * (ambient + lightColor * (wrap + back));

                float3 h = normalize(l + v);
                float3 specular = lightColor * (pow(saturate(dot(n, h)), _SpecPower) * _SpecStrength);
                float3 reflection = GlossyEnvironmentReflection(reflect(-v, n), input.positionWS, _Roughness, 1.0);

                float3 absorb = -log(max(_TransmitColor.rgb, 0.01)) * _Density;
                float viewThickness = _Thickness * max(nDotV, 0.1);

                float contactLine = 0.0;
            #if defined(_JELLY_BODY)
                // Denser where the body presses on the ground, with a wet highlight right at the contact
                float height = input.restOS.w;
                absorb *= 2.0 - smoothstep(0.0, _ContactHeight, height);
                contactLine = exp(-height * height / (_ContactHeight * _ContactHeight * 0.1));
            #endif

                float3 color;
                float alpha;
            #if defined(_JELLY_REFRACTION)
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float fragmentDepth = -TransformWorldToView(input.positionWS).z;
                float2 uv = screenUV + TransformWorldToViewDir(n).xy * (_Refraction * (1.0 - fresnel));
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                if (sceneDepth < fragmentDepth)
                {
                    // Bent onto something in front of the jelly: look straight through instead
                    uv = screenUV;
                    sceneDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                }

                // Items inside sit close to the surface, so they come through clearer than the ground behind
                float thickness = clamp(sceneDepth - fragmentDepth, 0.0, viewThickness);
                float3 transmittance = exp(-absorb * thickness);
                color = SampleSceneColor(uv) * transmittance + scatter * (1.0 - transmittance);
                alpha = 1.0;
            #else
                float3 transmittance = exp(-absorb * viewThickness);
                color = scatter;
                alpha = max(_MinAlpha, 1.0 - dot(transmittance, 1.0 / 3.0));
            #endif

                float3 surface = specular + reflection * (_ReflectStrength * (0.04 + 0.96 * fresnel))
                    + _RimColor.rgb * (fresnel * 0.5) * (ambient + lightColor * 0.5)
                    + _ContactColor.rgb * (contactLine * 0.35);
                color += surface;
                alpha = saturate(alpha + fresnel * 0.4 + Luminance(specular));

            #if defined(_JELLY_BODY)
                // Mouth: a thin closed line that widens into a dark opening
                float2 mouthUV = JellyMouthUV(input.restOS.xyz);
                float mouthHeight = lerp(_MouthLine, 1.0, _MouthCenter.w);
                float mouthShape = mouthUV.x * mouthUV.x + (mouthUV.y * mouthUV.y) / (mouthHeight * mouthHeight);
                float mouth = (1.0 - smoothstep(0.65, 1.0, mouthShape))
                    * saturate((input.restOS.z - _MouthCenter.z * 0.5) * 8.0) * _MouthColor.a;
                color = lerp(color, _MouthColor.rgb * (ambient + lightColor * 0.3), mouth);
                alpha = max(alpha, mouth);

                // Digesting: bubbles fizz up through the body
                if (_Digest > 0.001)
                {
                    float3 q = input.restOS.xyz * 7.0 + float3(0.0, -_Time.y * 2.2, 0.0);
                    float3 cell = floor(q);
                    float3 random = JellyHash33(cell);
                    float distanceToBubble = length(frac(q) - (0.3 + 0.4 * random));
                    float bubble = smoothstep(0.22, 0.12, distanceToBubble) * step(0.55, random.x);
                    float pulse = 0.5 + 0.5 * sin(_Time.y * 6.0 + random.y * 6.2832);
                    color += _DigestColor.rgb * ((bubble * pulse + 0.15) * _Digest);
                }
            #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex JellyShadowVertex
            #pragma fragment JellyShadowFragment

            #pragma shader_feature_local _JELLY_BODY
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "JellyCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings JellyShadowVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionOS;
                float3 normalOS;
                JellyDeformVertex(input.positionOS.xyz, input.normalOS, input.tangentOS, positionOS, normalOS);

                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(positionCS);
                return output;
            }

            half4 JellyShadowFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
