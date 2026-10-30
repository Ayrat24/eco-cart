#ifndef ECO_TERRAIN_VARIATION_INCLUDED
#define ECO_TERRAIN_VARIATION_INCLUDED

// WorldVisualSettings: x = anti-tiling strength, y = colour strength, z = inverse patch size.
float4 _EcoTerrainVariation;

float2 EcoTerrainHash(float2 cell)
{
    float3 p = frac(float3(cell.xyx) * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.xx + p.yz) * p.zy);
}

#ifndef TERRAIN_SPLAT_BASEPASS
// Blend three independently offset samples on a triangular lattice. Offsets only (no
// rotations) keep tangent-space normals aligned with diffuse and mask maps. Explicit
// gradients prevent the random offsets from causing mip seams at triangle boundaries.
half4 EcoSampleTerrain(TEXTURE2D_PARAM(tex, texSampler), float2 uv)
{
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);
    half4 result = 0;
    UNITY_BRANCH
    if (_EcoTerrainVariation.x <= 0.0)
    {
        result = SAMPLE_TEXTURE2D_GRAD(tex, texSampler, uv, dx, dy);
    }
    else
    {
        float2 skew = mul(float2x2(1.0, 0.0, -0.57735027, 1.15470054), uv * 3.46410162);
        float2 cell = floor(skew);
        float2 f = frac(skew);
        float third = 1.0 - f.x - f.y;
        float2 a, b, c;
        float3 weights;
        if (third > 0.0)
        {
            a = cell;
            b = cell + float2(0, 1);
            c = cell + float2(1, 0);
            weights = float3(third, f.y, f.x);
        }
        else
        {
            a = cell + 1.0;
            b = cell + float2(1, 0);
            c = cell + float2(0, 1);
            weights = float3(-third, 1.0 - f.y, 1.0 - f.x);
        }

        // Narrow the blend regions to retain detail instead of averaging everything to grey.
        weights = weights * weights * weights;
        weights /= dot(weights, float3(1, 1, 1));
        result =
            SAMPLE_TEXTURE2D_GRAD(tex, texSampler, uv + EcoTerrainHash(a), dx, dy) * weights.x +
            SAMPLE_TEXTURE2D_GRAD(tex, texSampler, uv + EcoTerrainHash(b), dx, dy) * weights.y +
            SAMPLE_TEXTURE2D_GRAD(tex, texSampler, uv + EcoTerrainHash(c), dx, dy) * weights.z;

        UNITY_BRANCH
        if (_EcoTerrainVariation.x < 1.0)
            result = lerp(SAMPLE_TEXTURE2D_GRAD(tex, texSampler, uv, dx, dy), result, _EcoTerrainVariation.x);
    }
    return result;
}
#endif

float EcoTerrainNoise(float2 position)
{
    float2 cell = floor(position);
    float2 f = frac(position);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(EcoTerrainHash(cell).x, EcoTerrainHash(cell + float2(1, 0)).x, f.x),
        lerp(EcoTerrainHash(cell + float2(0, 1)).x, EcoTerrainHash(cell + 1.0).x, f.x), f.y);
}

half3 EcoTerrainMacroColour(half3 albedo, float2 worldXZ)
{
    UNITY_BRANCH
    if (_EcoTerrainVariation.y > 0.0)
    {
        // XZ is unchanged by world curvature, so these patches stay fixed as the player moves.
        float2 position = worldXZ * _EcoTerrainVariation.z;
        float brightness = EcoTerrainNoise(position) * 2.0 - 1.0;
        float warmth = EcoTerrainNoise(position * 2.03 + float2(17.1, 9.2)) * 2.0 - 1.0;
        albedo *= (1.0 + _EcoTerrainVariation.y *
            (brightness * 0.75 + warmth * half3(0.3, 0.05, -0.25)));
    }
    return albedo;
}

#endif
