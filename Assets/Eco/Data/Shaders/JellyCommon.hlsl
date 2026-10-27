#ifndef ECO_JELLY_COMMON_INCLUDED
#define ECO_JELLY_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    float4 _TransmitColor;
    float _Density;
    float _MinAlpha;
    float _Thickness;
    float _Refraction;

    float _Wrap;
    float _SpecPower;
    float _SpecStrength;
    float _ReflectStrength;
    float _Roughness;
    float4 _RimColor;
    float _FresnelPower;
    float _TransStrength;
    float _TransPower;
    float _ShadowReceive;

    float _JellyHeight;
    float4 _Wobble;
    float _Squash;
    float _Sink;
    float _GroundSpread;
    float _ContactHeight;
    float4 _ContactColor;
    float4 _Jiggle;

    float4 _MouthCenter;
    float4 _MouthRotation;
    float4 _MouthShape;
    float _MouthLine;
    float4 _MouthColor;

    float4 _Ripple0;
    float4 _Ripple1;
    float4 _Ripple2;
    float4 _Ripple3;
    float4 _RippleAmp;
    float4 _RippleShape;

    float _Digest;
    float4 _DigestColor;
CBUFFER_END

// Body deformation. Object space: y up, the ground at y = 0, the mouth facing +z.
// Eco.Scripts.Jelly.JellyDeformation mirrors everything here except the jiggle and ripples, so hands and the
// mouth can be placed on the deformed surface from C#. Keep the two in sync.

// The mouth slides around the body. _MouthRotation holds the sin and cos of its angle around y; turning a rest
// position by it puts the mouth in front (+z), where _MouthCenter.xyz describes it.
float3 JellyToMouthSpace(float3 rest)
{
    float s = _MouthRotation.x;
    float c = _MouthRotation.y;
    return float3(rest.x * c - rest.z * s, rest.y, rest.z * c + rest.x * s);
}

float2 JellyMouthUV(float3 rest)
{
    float2 uv = (JellyToMouthSpace(rest).xy - _MouthCenter.xy) / _MouthShape.xy;
    uv.y -= _MouthShape.w * uv.x * uv.x;
    return uv;
}

// 1 on the mouth's side of the body, 0 on the far side
float JellyMouthSide(float3 rest)
{
    return saturate((JellyToMouthSpace(rest).z - _MouthCenter.z * 0.5) * 8.0);
}

float JellyMouthMask(float3 rest)
{
    float2 uv = JellyMouthUV(rest);
    return saturate(1.0 - dot(uv, uv)) * JellyMouthSide(rest);
}

float JellyRipple(float3 rest, float4 ripple, float amplitude)
{
    float age = max(_Time.y - ripple.w, 0.0);
    float k = (distance(rest, ripple.xyz) - age * _RippleShape.x) / _RippleShape.y;
    return amplitude * exp(-age * _RippleShape.z) * exp(-k * k) * cos(k * 3.0);
}

float JellySurfaceOffset(float3 rest, float h01)
{
    float t = _Time.y;
    float jiggle = sin(dot(rest, float3(2.7, 1.9, 2.3)) * _Jiggle.z + t * _Jiggle.y)
                 + 0.6 * sin(dot(rest, float3(-2.1, 2.6, 1.4)) * _Jiggle.z - t * _Jiggle.y * 1.3);
    jiggle *= _Jiggle.x * (0.35 + 0.65 * h01);

    float ripples = JellyRipple(rest, _Ripple0, _RippleAmp.x) + JellyRipple(rest, _Ripple1, _RippleAmp.y)
                  + JellyRipple(rest, _Ripple2, _RippleAmp.z) + JellyRipple(rest, _Ripple3, _RippleAmp.w);
    return jiggle + ripples;
}

float3 JellyDeform(float3 p, float3 n)
{
    float3 rest = p;
    float h01 = saturate(p.y / _JellyHeight);

    // The lips sink inward as the mouth opens
    float mouth = JellyMouthMask(rest);
    p -= n * (_MouthShape.z * _MouthCenter.w * mouth * mouth);

    p += n * JellySurfaceOffset(rest, h01);

    // Squash and stretch around the ground, keeping the volume
    float stretch = max(0.2, 1.0 + _Squash);
    p.y *= stretch;
    p.xz *= rsqrt(stretch);
    p.y -= _Sink;

    // The base stays planted while the top follows the wobble
    p += _Wobble.xyz * (h01 * h01 * (3.0 - 2.0 * h01));

    // Ground contact: whatever ends up below the ground flattens onto it and spreads out
    float under = max(0.0, -p.y);
    p.y += under;
    float radial = length(p.xz);
    p.xz += radial > 1e-4 ? p.xz / radial * under * _GroundSpread : 0.0;
    return p;
}

// Deforms a vertex and rebuilds its normal from two deformed neighbours along the tangent and bitangent
void JellyDeformVertex(float3 positionOS, float3 normalOS, float4 tangentOS, out float3 deformedOS,
    out float3 deformedNormalOS)
{
#if defined(_JELLY_BODY)
    const float e = 0.02;
    float3 bitangent = cross(normalOS, tangentOS.xyz) * tangentOS.w;
    float3 p0 = JellyDeform(positionOS, normalOS);
    float3 pt = JellyDeform(positionOS + tangentOS.xyz * e, normalOS);
    float3 pb = JellyDeform(positionOS + bitangent * e, normalOS);
    float3 n = cross(pt - p0, pb - p0);
    deformedOS = p0;
    deformedNormalOS = dot(n, n) > 1e-12 ? normalize(n) : normalOS;
#else
    deformedOS = positionOS;
    deformedNormalOS = normalOS;
#endif
}

float3 JellyHash33(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx);
}

#endif
