#ifndef ECO_GRASS_GROWTH_INCLUDED
#define ECO_GRASS_GROWTH_INCLUDED

// Grass the slime trail plants grows in instead of popping up, blades and ground alike. GrassGrowthMap keeps two
// maps with one texel per terrain detail cell, blades and ground, so they can cover different cells: the time
// (URP _TimeParameters.x, which is Time.time while playing) the cell's blades or ground are fully grown. Cells that
// were never stamped hold 0. Included by the generated curved Plants shader, which runs EcoGrassGrowth after wind
// and before the world bend, and by the generated terrain passes, which run EcoGrassGroundGrowth on the splat
// control weights (CurvedWorldSetup).
TEXTURE2D_FLOAT(_EcoGrassGrowthMap);
TEXTURE2D_FLOAT(_EcoGroundGrowthMap);
// xy = terrain origin XZ, zw = detail cells per metre
float4 _EcoGrassGrowthMapTransform;
// x = 1 / grow duration, y = time until which the map matters (the last cell is grown and its ground written into
// the alphamap), zw = map size in cells. Unset (outside play mode) it is all zeros, which skips every lookup.
float4 _EcoGrassGrowth;
// One-hot mask of the terrain splat channel holding the grass layer; zero leaves the ground alone
float4 _EcoGrassGroundMask;

float EcoGrowthCurve(float grownAt, float now)
{
    float growth = saturate(1.0 - (grownAt - now) * _EcoGrassGrowth.x);
    return growth * growth * (3.0 - 2.0 * growth);
}

// Scales an instanced grass blade around its pivot by how far its detail cell has grown
void EcoGrassGrowth(inout float3 positionOS)
{
    float now = _TimeParameters.x;
    // Uniform branch: no texture read at all while nothing is growing
    if (now >= _EcoGrassGrowth.y)
        return;

    float2 pivotXZ = float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m23);
    int2 cell = (int2)floor((pivotXZ - _EcoGrassGrowthMapTransform.xy) * _EcoGrassGrowthMapTransform.zw);
    cell = clamp(cell, 0, (int2)_EcoGrassGrowth.zw - 1);
    positionOS *= EcoGrowthCurve(LOAD_TEXTURE2D_LOD(_EcoGrassGrowthMap, cell, 0).r, now);
}

// Growth of the ground at control texel `texel`: the detail cell under the texel, never-stamped cells excluded
float EcoGroundGrowthAt(int2 texel, float2 cellsPerTexel, float now)
{
    int2 cell = (int2)floor(texel * cellsPerTexel);
    cell = clamp(cell, 0, (int2)_EcoGrassGrowth.zw - 1);
    float grownAt = LOAD_TEXTURE2D_LOD(_EcoGroundGrowthMap, cell, 0).r;
    return grownAt > 0.0 ? EcoGrowthCurve(grownAt, now) : 0.0;
}

// Grows the grass layer into the terrain's splat weights where grass is growing, filtered like the control map
// itself, until TerrainPainter writes the grown cells into the real alphamap. The grass weight only ever rises.
// splatUV and controlSize (_Control_TexelSize.zw) are the terrain shader's.
void EcoGrassGroundGrowth(inout half4 splatControl, float2 splatUV, float2 controlSize)
{
    float now = _TimeParameters.x;
    if (now >= _EcoGrassGrowth.y || dot(_EcoGrassGroundMask, 1.0) == 0.0)
        return;

    // Control texel j sits j / (size - 1) of the way across the terrain
    float2 cellsPerTexel = _EcoGrassGrowth.zw / (controlSize - 1.0);
    float2 t = splatUV * controlSize - 0.5;
    int2 texel = (int2)floor(t);
    float2 f = t - texel;
    float growth = lerp(
        lerp(EcoGroundGrowthAt(texel, cellsPerTexel, now), EcoGroundGrowthAt(texel + int2(1, 0), cellsPerTexel, now), f.x),
        lerp(EcoGroundGrowthAt(texel + int2(0, 1), cellsPerTexel, now), EcoGroundGrowthAt(texel + int2(1, 1), cellsPerTexel, now), f.x),
        f.y);

    half grass = dot(splatControl, _EcoGrassGroundMask);
    if (growth <= grass)
        return;

    // The other layers share what is left
    half rest = (1.0h - growth) / max(1.0h - grass, 0.0001h);
    splatControl = splatControl * rest * (1.0h - _EcoGrassGroundMask) + _EcoGrassGroundMask * growth;
}
#endif
