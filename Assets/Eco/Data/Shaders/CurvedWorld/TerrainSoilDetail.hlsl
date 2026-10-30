#ifndef ECO_TERRAIN_SOIL_DETAIL_INCLUDED
#define ECO_TERRAIN_SOIL_DETAIL_INCLUDED

// Linear data texture: RG = signed height gradient, B = cavity, A unused.
TEXTURE2D(_EcoSoilDetailMap);
SAMPLER(sampler_EcoSoilDetailMap);
// x = relief strength, y = cavity strength, z = inverse tile size, w = fade distance.
float4 _EcoSoilDetail;
float4 _EcoSoilLayerMask;

void EcoApplySoilDetail(float3 positionWS, half soilWeight, inout half3 normalWS, inout half3 albedo)
{
    // Always evaluate derivatives before the spatial branch. Fade details once a feature
    // approaches two pixels, independently of the explicit distance fade.
    float2 uv = positionWS.xz * _EcoSoilDetail.z;
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);
    float footprint = max(length(dx), length(dy));
    float distanceFade = 1.0 - smoothstep(_EcoSoilDetail.w * 0.65, _EcoSoilDetail.w,
        distance(GetCameraPositionWS(), positionWS));
    float coarseFade = (1.0 - smoothstep(0.012, 0.035, footprint)) * distanceFade * soilWeight;
    float fineFade = (1.0 - smoothstep(0.004, 0.012, footprint)) * distanceFade * soilWeight;

    UNITY_BRANCH
    if (coarseFade > 0.001 && (_EcoSoilDetail.x > 0.0 || _EcoSoilDetail.y > 0.0))
    {
        half4 coarse = SAMPLE_TEXTURE2D_GRAD(_EcoSoilDetailMap, sampler_EcoSoilDetailMap, uv, dx, dy);
        // A second, rotated scale gives fine grain without aligning with the larger pits.
        float2 fineUV = float2(0.8 * uv.x - 0.6 * uv.y, 0.6 * uv.x + 0.8 * uv.y) * 3.17 + 0.37;
        float2 fineDx = float2(0.8 * dx.x - 0.6 * dx.y, 0.6 * dx.x + 0.8 * dx.y) * 3.17;
        float2 fineDy = float2(0.8 * dy.x - 0.6 * dy.y, 0.6 * dy.x + 0.8 * dy.y) * 3.17;
        half4 fine = SAMPLE_TEXTURE2D_GRAD(_EcoSoilDetailMap, sampler_EcoSoilDetailMap, fineUV, fineDx, fineDy);
        float2 coarseSlope = coarse.rg * 2.0 - 1.0;
        float2 fineSlope = fine.rg * 2.0 - 1.0;
        // Rotate gradients back into world XZ (transpose of the UV rotation).
        fineSlope = float2(0.8 * fineSlope.x + 0.6 * fineSlope.y, -0.6 * fineSlope.x + 0.8 * fineSlope.y);
        float2 slope = coarseSlope * coarseFade + fineSlope * (0.35 * fineFade);
        float3 tangentX = SafeNormalize(float3(1, 0, 0) - normalWS * normalWS.x);
        float3 tangentZ = SafeNormalize(cross(tangentX, normalWS));
        normalWS = normalize(normalWS - (tangentX * slope.x + tangentZ * slope.y) * _EcoSoilDetail.x);
        half cavity = saturate(coarse.b * coarseFade + fine.b * (0.3 * fineFade));
        albedo *= 1.0 - cavity * _EcoSoilDetail.y;
    }
}

#endif
