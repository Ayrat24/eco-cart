#ifndef ECO_CURVED_TERRAIN_INPUT_INCLUDED
#define ECO_CURVED_TERRAIN_INPUT_INCLUDED

// Terrain normals must be transformed at the same position as the terrain vertices.
// Disable URP's unbent per-pixel heightmap-normal shortcut for this shader.
#undef _TERRAIN_INSTANCED_PERPIXEL_NORMAL
#include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
#include "Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl"

void EcoTerrainInstancing(inout float4 positionOS, inout float3 normalOS, inout float2 uv)
{
    TerrainInstancing(positionOS, normalOS, uv);
    EcoBendVertex(positionOS.xyz, normalOS);
}

void EcoTerrainInstancing(inout float4 positionOS, inout float3 normalOS)
{
    float2 uv = 0;
    EcoTerrainInstancing(positionOS, normalOS, uv);
}

void EcoTerrainInstancing(inout float4 positionOS)
{
    float3 normalOS = float3(0, 1, 0);
    EcoTerrainInstancing(positionOS, normalOS);
}

#define TerrainInstancing EcoTerrainInstancing
#endif
