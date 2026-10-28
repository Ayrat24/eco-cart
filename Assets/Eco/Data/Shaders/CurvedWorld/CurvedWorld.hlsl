#ifndef ECO_CURVED_WORLD_INCLUDED
#define ECO_CURVED_WORLD_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// xy = player world XZ, z = curvature, w = flat interaction radius.
float4 _EcoWorldBend;
float4 _EcoWorldBendPrevious;

float EcoBendDrop(float3 positionWS, float4 settings)
{
    float distanceBeyond = max(0.0, length(positionWS.xz - settings.xy) - settings.w);
    return settings.z * distanceBeyond * distanceBeyond;
}

float3 EcoBendWorldPosition(float3 positionWS, float4 settings)
{
    positionWS.y -= EcoBendDrop(positionWS, settings);
    return positionWS;
}

float2 EcoBendGradient(float3 positionWS)
{
    float2 offset = positionWS.xz - _EcoWorldBend.xy;
    float radius = length(offset);
    return 2.0 * _EcoWorldBend.z * max(0.0, radius - _EcoWorldBend.w) * offset / max(radius, 0.0001);
}

void EcoBendPosition(inout float3 positionOS)
{
    positionOS = TransformWorldToObject(EcoBendWorldPosition(TransformObjectToWorld(positionOS), _EcoWorldBend));
}

void EcoBendVertex(inout float3 positionOS, inout float3 normalOS)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
    float2 gradient = EcoBendGradient(positionWS);
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
    // Inverse transpose of the deformation Jacobian (not just a position offset).
    normalWS.xz += gradient * normalWS.y;
    normalOS = TransformWorldToObjectNormal(normalize(normalWS));
    positionOS = TransformWorldToObject(EcoBendWorldPosition(positionWS, _EcoWorldBend));
}

void EcoBendVertex(inout float3 positionOS, inout float3 normalOS, inout float4 tangentOS)
{
    float3 tangentWS = TransformObjectToWorldDir(tangentOS.xyz);
    tangentWS.y -= dot(EcoBendGradient(TransformObjectToWorld(positionOS)), tangentWS.xz);
    tangentOS.xyz = TransformWorldToObjectDir(normalize(tangentWS));
    EcoBendVertex(positionOS, normalOS);
}

// Shader Graph Custom Function entry point; run after wind/other vertex animation.
void EcoCurvedWorld_float(float3 Position, float3 Normal, float3 Tangent,
    out float3 BentPosition, out float3 BentNormal, out float3 BentTangent)
{
    BentPosition = Position;
    BentNormal = Normal;
    float4 tangent = float4(Tangent, 1);
    EcoBendVertex(BentPosition, BentNormal, tangent);
    BentTangent = tangent.xyz;
}
#endif
