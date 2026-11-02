// Semi-transparent jelly for the player creature and its tentacles.
// With _JELLY_REFRACTION the scene behind is read from the camera opaque texture, bent and tinted by how much
// jelly the view ray crosses. Without it (no opaque texture, e.g. the Mobile quality level) the surface
// alpha-blends instead. JellyBody switches the keyword to match the pipeline.
// _JELLY_BODY turns on the wobble / squash / ground contact / mouth deformation driven by JellyBody, and shows the
// swallowed items JellyInsidePass draws into _JellyInsideTexture, tinted the same wherever they are.
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
        _InsideMinDepth("Swallowed Items Min Tint Depth (metres)", Float) = 0.12

        [Header(Water Bank)]
        _WaterFill("Water Fill", Range(0, 1)) = 0
        [HideInInspector] _WaterSurfaceRadius("Water Surface Radius (object space)", Float) = 0.7
        _WaterColor("Water Scatter Colour", Color) = (0.035, 0.35, 0.72, 1)
        _WaterTransmitColor("Water Transmit Colour (per metre)", Color) = (0.25, 0.72, 0.94, 1)
        _WaterlineColor("Waterline Colour", Color) = (0.35, 0.85, 1, 1)
        _WaterWaveHeight("Waterline Wave Height (fraction of body)", Range(0, 0.08)) = 0.045
        _WaterSlosh("Water Wobble Follow", Range(0, 2)) = 1.2
        _WaterWaveSpeed("Water Wave Speed", Range(0, 5)) = 1.8
        _WaterUpperClarity("Upper Jelly Clarity", Range(0, 1)) = 0.75
        _WaterSurfaceOpacity("Interior Water Visibility", Range(0, 1)) = 0.75
        _WaterFoamStrength("Water Foam Brightness", Range(0, 1)) = 0.25
        _WaterTintStrength("Water Tint Strength", Range(0, 1)) = 0.3
        _WaterFoamColor("Water Foam and Bubbles", Color) = (0.65, 0.95, 1, 1)
        _WaterFoamWidth("Water Foam Width (fraction of body)", Range(0, 0.1)) = 0.025
        _WaterBubbleStrength("Water Bubble Strength", Range(0, 1)) = 0.75
        _WaterBubbleRadius("Interior Bubble Radius (fraction of body)", Range(0.005, 0.08)) = 0.035
        _WaterBubbleSpeed("Interior Bubble Rise Speed", Range(0, 1)) = 0.15

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

        [Header(Mouth (set from JellyMouthSettings while playing))]
        _MouthCenter("Mouth Centre (xyz rest position turned to the front, w open)", Vector) = (0, 0.9, 0.8, 0)
        [HideInInspector] _MouthRotation("Mouth Rotation (sin, cos of its angle)", Vector) = (0, 1, 0, 0)
        _MouthShape("Mouth Shape (half width, half height, depth, smile)", Vector) = (0.3, 0.12, 0.25, 0.25)
        _MouthLine("Closed Mouth Line", Range(0, 1)) = 0.3
        _MouthColor("Mouth Colour", Color) = (0.02, 0.12, 0.12, 0.9)
        _MouthThroat("Mouth Throat (darkness, lip highlight)", Vector) = (0.75, 0.6, 0, 0)

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

            // Set by JellyInsidePass
            TEXTURE2D(_JellyInsideTexture);
            SAMPLER(sampler_JellyInsideTexture);
            TEXTURE2D_FLOAT(_JellyInsideDepth);
            SAMPLER(sampler_JellyInsideDepth);

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

            // x = height offset, yz = its analytic slope. The rim and interior use the same surface.
            float3 JellyWaterSurface(float2 p, float time, float amplitude, float2 tilt)
            {
                float3 phase = float3(dot(p, float2(3.8, 2.1)) + time,
                    dot(p, float2(-2.6, 4.3)) - time * 1.3,
                    dot(p, float2(8.5, -6.3)) + time * 1.7);
                float3 waves = sin(phase) * float3(1.0, 0.45, 0.22);
                float3 slopes = cos(phase) * float3(1.0, 0.45, 0.22);
                float height = dot(waves, float3(1.0, 1.0, 1.0)) * amplitude + dot(p, tilt);
                float2 slope = (slopes.x * float2(3.8, 2.1) + slopes.y * float2(-2.6, 4.3)
                    + slopes.z * float2(8.5, -6.3)) * amplitude + tilt;
                return float3(height, slope);
            }

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
                float waterMask = 0.0;
                float waterline = 0.0;
                float waterBubbles = 0.0;
                float waterSurfaceLight = 0.0;
                float waterSurfaceMask = 0.0;
                float waterInteriorDepth = 0.0;
                float waterSurfaceEdge = 0.0;
            #if defined(_JELLY_BODY)
                // Follow the body height and use its spring wobble to rock the water surface.
                float bodyHeight = max(0.001, _JellyHeight * max(0.2, 1.0 + _Squash) - _Sink + _Wobble.y);
                float fill = saturate(_WaterFill);
                float3 rayOrigin = TransformWorldToObject(input.positionWS);
                float3 rayDirection = normalize(TransformWorldToObjectDir(-v, false));
                float waveTime = _Time.y * _WaterWaveSpeed;
                float partialFill = 4.0 * fill * (1.0 - fill);
                float2 surfaceCentre = _Wobble.xz * smoothstep(0.0, 1.0, fill);
                // The water rocks with the same damped spring as the body, and settles when it does.
                float2 tilt = -_Wobble.xz / bodyHeight * _WaterSlosh * partialFill;
                float waveAmplitude = _WaterWaveHeight * partialFill * bodyHeight
                    * (1.0 + min(length(_Wobble.xz) / bodyHeight + abs(_Squash), 0.5));
                float surfaceHeight = fill * bodyHeight;
                float skinWave = JellyWaterSurface(rayOrigin.xz - surfaceCentre, waveTime, waveAmplitude, tilt).x;
                float waterDistance = (input.restOS.w - surfaceHeight - skinWave) / bodyHeight;
                float edge = max(fwidth(waterDistance), 0.004);
                waterMask = 1.0 - smoothstep(-edge, edge, waterDistance);
                // Uneven foam gathers just below the crest, with a fine bright edge above it.
                float foamPatch = 0.65 + 0.35 * sin(dot(input.restOS.xz, float2(18.0, 13.0)) + waveTime);
                float foamWidth = _WaterFoamWidth * foamPatch;
                waterline = smoothstep(-foamWidth - edge, -foamWidth * 0.25, waterDistance)
                    * (1.0 - smoothstep(0.0, edge * 2.0, waterDistance));
                waterline *= step(0.0001, _WaterFoamWidth);
                // Exact endpoints: no puddle at empty, no dry cap or stray line at full.
                waterMask = fill <= 0.0 ? 0.0 : (fill >= 1.0 ? 1.0 : waterMask);
                waterline *= smoothstep(0.0, 0.03, fill) * (1.0 - smoothstep(0.97, 1.0, fill));

                // Looking through the dry upper skin can still hit water inside the body.
                // Intersect a bounded, wavy surface instead of testing only the skin's height.
                float planeDenominator = rayDirection.y - dot(rayDirection.xz, tilt);
                float planeNumerator = surfaceHeight + dot(rayOrigin.xz - surfaceCentre, tilt) - rayOrigin.y;
                float surfaceDistance = planeNumerator / min(planeDenominator, -0.001);
                float3 surfaceHit = rayOrigin + rayDirection * max(0.0, surfaceDistance);
                [unroll]
                for (int waveStep = 0; waveStep < 3; waveStep++)
                {
                    float2 p = surfaceHit.xz - surfaceCentre;
                    float surfaceWave = JellyWaterSurface(p, waveTime, waveAmplitude, tilt).x - dot(p, tilt);
                    surfaceDistance = (planeNumerator + surfaceWave) / min(planeDenominator, -0.001);
                    surfaceHit = rayOrigin + rayDirection * max(0.0, surfaceDistance);
                }
                float radialDistance = length(surfaceHit.xz - surfaceCentre);
                float surfaceAA = max(fwidth(radialDistance), 0.018);
                float surfaceVisible = (1.0 - smoothstep(_WaterSurfaceRadius - surfaceAA,
                    _WaterSurfaceRadius + surfaceAA, radialDistance))
                    // Near grazing angles the skin waterline carries the fill cue; fade the projected
                    // interior before the wave intersection becomes unstable at those angles.
                    * smoothstep(0.3, 0.6, -planeDenominator) * step(0.0, surfaceDistance)
                    * smoothstep(0.0, 0.02, fill) * (1.0 - waterMask);
                surfaceVisible *= exp(-max(surfaceDistance, 0.0) * 0.12);
                float3 surfaceWaveData = JellyWaterSurface(surfaceHit.xz - surfaceCentre, waveTime, waveAmplitude, tilt);
                float2 slope = surfaceWaveData.yz;
                float3 waterNormal = TransformObjectToWorldNormal(normalize(float3(-slope.x, 1.0, -slope.y)));
                float crest = saturate(0.5 + 0.5 * (surfaceWaveData.x - dot(surfaceHit.xz - surfaceCentre, tilt))
                    / max(waveAmplitude * 1.67, 0.001));
                // This is a separate layer behind the upper shell, never a blue stain on its skin.
                waterSurfaceMask = surfaceVisible;
                waterInteriorDepth = min(surfaceHeight / max(-rayDirection.y, 0.15), bodyHeight * 1.5);
                waterSurfaceEdge = smoothstep(0.65, 1.0, radialDistance / max(_WaterSurfaceRadius, 0.001));
                waterSurfaceLight = surfaceVisible * (0.025 * crest
                    + 0.08 * pow(saturate(dot(waterNormal, normalize(l + v))), 16.0));


                // Intersect the view ray with a few spheres suspended in the body volume.
                // Their centres never live on the mesh: camera movement gives real interior parallax,
                // and the water between the skin and each bubble softens its contrast.
                float3 bubbleLight = normalize(TransformWorldToObjectDir(l, false) - rayDirection);
                [unroll]
                for (int bubbleIndex = 0; bubbleIndex < 8; bubbleIndex++)
                {
                    float3 random = JellyHash33(float3(bubbleIndex, 3.0, 11.0));
                    float rise = frac(random.z + _Time.y * _WaterBubbleSpeed * lerp(0.7, 1.2, random.x));
                    float radius = _JellyHeight * _WaterBubbleRadius * lerp(0.65, 1.15, random.y);
                    float y = lerp(radius * 1.5, max(radius * 1.5, fill * bodyHeight - radius * 1.5), rise);
                    float height01 = saturate(y / bodyHeight);
                    float coreWidth = _JellyHeight * 0.28 * sqrt(saturate(1.0 - height01 * height01));
                    float3 centre = float3((random.x - 0.5) * coreWidth * 2.0, y,
                        (random.y - 0.5) * coreWidth * 2.0);
                    centre.x += sin(_Time.y * 1.3 + random.z * 6.28) * radius * 0.5;
                    centre.xz += _Wobble.xz * smoothstep(0.0, 1.0, height01);
                    float3 toCentre = centre - rayOrigin;
                    float alongRay = dot(toCentre, rayDirection);
                    float distanceSquared = dot(toCentre, toCentre) - alongRay * alongRay;
                    float radiusSquared = radius * radius;
                    float aa = max(fwidth(distanceSquared), 0.00001);
                    float coverage = 1.0 - smoothstep(radiusSquared - aa, radiusSquared + aa, distanceSquared);
                    float front = alongRay - sqrt(max(0.0, radiusSquared - distanceSquared));
                    float3 bubbleNormal = normalize(rayOrigin + rayDirection * front - centre);
                    float facing = saturate(dot(bubbleNormal, -rayDirection));
                    float highlight = pow(saturate(dot(bubbleNormal, bubbleLight)), 32.0);
                    float shading = 0.12 + 0.18 * (1.0 - facing) + highlight * 0.85;
                    float lifetime = smoothstep(0.0, 0.12, rise) * (1.0 - smoothstep(0.85, 1.0, rise));
                    waterBubbles += coverage * shading * exp(-max(front, 0.0) * 0.8)
                        * step(radius, front) * lifetime;
                }
                waterBubbles *= max(waterMask, waterSurfaceMask) * _WaterBubbleStrength * smoothstep(0.04, 0.12, fill);
            #endif
                float3 scatter = lerp(_Color.rgb, _WaterColor.rgb, waterMask)
                    * (ambient + lightColor * (wrap + back));

                float3 h = normalize(l + v);
                float3 specular = lightColor * (pow(saturate(dot(n, h)), _SpecPower) * _SpecStrength);
                float3 reflection = GlossyEnvironmentReflection(reflect(-v, n), input.positionWS, _Roughness, 1.0);

                float3 absorb = -log(max(lerp(_TransmitColor.rgb, _WaterTransmitColor.rgb, waterMask), 0.01)) * _Density;
                float viewThickness = _Thickness * max(nDotV, 0.1);
            #if defined(_JELLY_BODY)
                // Keep the empty bank's original look. With water, the dry dome becomes a clear shell.
                float upperClarity = _WaterUpperClarity * (1.0 - waterMask) * step(0.0001, _WaterFill);
                absorb *= 1.0 - upperClarity;
            #endif

                float contactLine = 0.0;
            #if defined(_JELLY_BODY)
                // Denser where the body presses on the ground, with a wet highlight right at the contact
                float height = input.restOS.w;
                absorb *= 2.0 - smoothstep(0.0, _ContactHeight, height);
                contactLine = exp(-height * height / (_ContactHeight * _ContactHeight * 0.1));
            #endif

                float3 color;
                float alpha;
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float fragmentDepth = -TransformWorldToView(input.positionWS).z;
            #if defined(_JELLY_REFRACTION)
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
                float2 uv = screenUV;
                float3 transmittance = exp(-absorb * viewThickness);
                color = scatter;
                float minAlpha = _MinAlpha;
            #if defined(_JELLY_BODY)
                minAlpha *= 1.0 - upperClarity;
            #endif
                alpha = max(minAlpha, 1.0 - dot(transmittance, 1.0 / 3.0));
            #endif

            #if defined(_JELLY_BODY)
                // Shade the liquid volume before the shell highlights and before swallowed items.
                // Darker depth and a soft edge show the gap between the dome and the liquid below.
                color = lerp(color, _WaterColor.rgb * (0.25 + ambient + lightColor * 0.18),
                    waterMask * _WaterTintStrength);
                float3 waterAbsorb = -log(max(_WaterTransmitColor.rgb, 0.01)) * max(_Density, 0.25);
                float3 waterTransmission = exp(-waterAbsorb * waterInteriorDepth * 1.6);
                float3 waterScatter = _WaterColor.rgb * (0.22 + ambient * 0.6 + lightColor * 0.18);
                float3 interiorWater = color * waterTransmission + waterScatter * (1.0 - waterTransmission);
                interiorWater *= 1.0 - waterSurfaceEdge * 0.18;
                color = lerp(color, interiorWater, waterSurfaceMask * _WaterSurfaceOpacity);
                color += _WaterlineColor.rgb * waterSurfaceLight;
                alpha = max(alpha, waterSurfaceMask * _WaterSurfaceOpacity);

                // Swallowed items, drawn on their own by JellyInsidePass (premultiplied colour, alpha = coverage), so
                // transparent ones show too and nothing outside the body is drawn. They are tinted by the jelly in
                // front of them; parts poking out through the surface get the thinnest tint rather than none
                float4 inside = SAMPLE_TEXTURE2D(_JellyInsideTexture, sampler_JellyInsideTexture, uv);
                float insideDepth = LinearEyeDepth(
                    SAMPLE_TEXTURE2D(_JellyInsideDepth, sampler_JellyInsideDepth, uv).r, _ZBufferParams);
                float itemThickness = clamp(insideDepth - fragmentDepth, _InsideMinDepth,
                    max(viewThickness, _InsideMinDepth));
                float3 itemTransmittance = exp(-absorb * itemThickness);
                float3 items = inside.rgb * itemTransmittance + scatter * ((1.0 - itemTransmittance) * inside.a);

                // Items over whatever the jelly shows behind them
                float combinedAlpha = inside.a + alpha * (1.0 - inside.a);
                color = (items + color * (alpha * (1.0 - inside.a))) / max(combinedAlpha, 1e-4);
                alpha = combinedAlpha;
                waterBubbles *= 1.0 - inside.a;

            #endif

                float shine = 1.0;
            #if defined(_JELLY_BODY)
                // Mouth: a thin closed line that widens into a dark opening
                float2 mouthUV = JellyMouthUV(input.restOS.xyz);
                float mouthOpen = _MouthCenter.w;
                float mouthHeight = lerp(_MouthLine, 1.0, mouthOpen);
                float mouthShape = mouthUV.x * mouthUV.x + (mouthUV.y * mouthUV.y) / (mouthHeight * mouthHeight);
                float mouthSide = JellyMouthSide(input.restOS.xyz);

                // The open mouth's steep walls turn the sharp highlight and the edge glow into a saw-tooth along
                // the triangles; the lip highlight below lights the rim instead
                shine = 1.0 - mouthOpen * mouthSide * (1.0 - smoothstep(1.0, 1.4, mouthShape));
            #endif

                float3 surface = (specular + reflection * (_ReflectStrength * (0.04 + 0.96 * fresnel))
                    + _RimColor.rgb * (fresnel * 0.5) * (ambient + lightColor * 0.5)) * shine
                    + _ContactColor.rgb * (contactLine * 0.35);
                color = lerp(color, _WaterFoamColor.rgb * (0.65 + ambient + lightColor * 0.25),
                    saturate(waterline * _WaterFoamStrength + waterBubbles) * shine);
                color += _WaterlineColor.rgb * (waterline * _WaterFoamStrength * 0.08);
                // The strongest highlight belongs to the continuous outer jelly, above all interior layers.
                color += surface;
                alpha = max(alpha, saturate(waterline * _WaterFoamStrength + waterBubbles * 0.65));
                alpha = saturate(alpha + (fresnel * 0.4 + Luminance(specular)) * shine);

            #if defined(_JELLY_BODY)
                float mouth = (1.0 - smoothstep(0.65, 1.0, mouthShape)) * mouthSide * _MouthColor.a;

                // Open, it darkens toward the middle like a deep hole and gets a lighter lip around the rim
                float throat = 1.0 - _MouthThroat.x * mouthOpen * saturate(1.0 - mouthShape);
                color = lerp(color, _MouthColor.rgb * (ambient + lightColor * 0.3) * throat, mouth);
                float lip = smoothstep(0.6, 0.85, mouthShape) * (1.0 - smoothstep(0.85, 1.15, mouthShape));
                color += scatter * (lip * _MouthThroat.y * mouthOpen * mouthSide);
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
