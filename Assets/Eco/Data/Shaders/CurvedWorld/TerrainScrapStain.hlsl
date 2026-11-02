#ifndef ECO_TERRAIN_SCRAP_STAIN_INCLUDED
#define ECO_TERRAIN_SCRAP_STAIN_INCLUDED

// Dead grey ground under loose scrap, painted over the finished layers so the natural ground returns untouched when
// the stain goes. ScrapStainMap keeps one texel per terrain detail cell: 0 never stained, else the time (URP
// _TimeParameters.x, which is Time.time while playing) the ground was cleaned, far in the future while scrap lies on it.
TEXTURE2D_FLOAT(_EcoScrapStainMap);
// Same cells, filtered: a = stain strength, 1 at the stain's centre falling to 0 at its radius; rgb = the linear colour
// of the scrap that stained them, premultiplied by a
TEXTURE2D(_EcoScrapTintMap);
SAMPLER(sampler_EcoScrapTintMap);
// Linear data texture: R = grime brightness, GB = slow noise that warps the outline, A = noise that frays it.
TEXTURE2D(_EcoScrapGroundMap);
SAMPLER(sampler_EcoScrapGroundMap);
// xy = terrain origin XZ, zw = detail cells per metre
float4 _EcoScrapStainMapTransform;
// x = 1 while there is a map, zw = map size in cells. Unset (outside play mode) it is all zeros, which skips the stain.
float4 _EcoScrapStain;
// x = inverse tile size, y = strength, z = outline warp in metres, w = share of the radius the edge fades over (0..1)
float4 _EcoScrapGround;
// x = 1 / seconds the ground takes to heal
float4 _EcoScrapGroundFade;
// Linear colours of the grime's dark and light ends; a = smoothness left on stained ground
float4 _EcoScrapGroundDark;
float4 _EcoScrapGroundLight;
// How far the colour of the scrap colours the dead ground (0..1)
float _EcoScrapGroundTint;

// How stained detail cell `cell` is right now
float EcoScrapCellStain(int2 cell, float now)
{
    cell = clamp(cell, 0, (int2)_EcoScrapStain.zw - 1);
    float cleanedAt = LOAD_TEXTURE2D_LOD(_EcoScrapStainMap, cell, 0).r;
    return cleanedAt > 0.0 ? saturate(1.0 - (now - cleanedAt) * _EcoScrapGroundFade.x) : 0.0;
}

// Paints stained ground into albedo and takes the shine off it. Returns how stained the pixel is.
half EcoApplyScrapStain(float2 worldXZ, inout half3 albedo, inout half smoothness)
{
    // Always evaluate derivatives before the spatial branch
    float2 uv = worldXZ * _EcoScrapGround.x;
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);

    half stain = 0;
    UNITY_BRANCH
    if (_EcoScrapStain.x > 0.0)
    {
        // Smooth blend between cell centres, on a position the slow noise pushes around, so blobs lose the cell grid
        half2 warp = SAMPLE_TEXTURE2D_GRAD(_EcoScrapGroundMap, sampler_EcoScrapGroundMap, uv * 0.31, dx * 0.31, dy * 0.31).gb;
        float2 position = worldXZ + (warp - 0.5) * 2.0 * _EcoScrapGround.z;
        float2 t = (position - _EcoScrapStainMapTransform.xy) * _EcoScrapStainMapTransform.zw - 0.5;
        int2 cell = (int2)floor(t);
        float2 f = t - cell;
        f = f * f * (3.0 - 2.0 * f);
        float now = _TimeParameters.x;
        float field = lerp(
            lerp(EcoScrapCellStain(cell, now), EcoScrapCellStain(cell + int2(1, 0), now), f.x),
            lerp(EcoScrapCellStain(cell + int2(0, 1), now), EcoScrapCellStain(cell + int2(1, 1), now), f.x),
            f.y);

        UNITY_BRANCH
        if (field > 0.001)
        {
            half4 grime = SAMPLE_TEXTURE2D_GRAD(_EcoScrapGroundMap, sampler_EcoScrapGroundMap, uv, dx, dy);
            float2 tintUV = (position - _EcoScrapStainMapTransform.xy) * _EcoScrapStainMapTransform.zw / _EcoScrapStain.zw;
            half4 tint = SAMPLE_TEXTURE2D_LOD(_EcoScrapTintMap, sampler_EcoScrapTintMap, tintUV, 0);
            half3 hue = lerp(half3(1, 1, 1), tint.rgb / max(tint.a, 0.004h), _EcoScrapGroundTint);

            // A transparent gradient to the rim. The noise only mottles it, so the edge has no line to read as a puddle.
            half coverage = smoothstep(0.0, max(_EcoScrapGround.w, 0.01), saturate(tint.a * (0.6 + 0.8 * grime.a)));
            // A healing stain breaks up into islands instead of just getting paler
            half heal = saturate(field - (1.0 - grime.a) * (1.0 - field));
            stain = coverage * heal * _EcoScrapGround.y;

            albedo = lerp(albedo, lerp(_EcoScrapGroundDark.rgb, _EcoScrapGroundLight.rgb, grime.r) * hue, stain);
            smoothness = lerp(smoothness, _EcoScrapGroundLight.a, stain);
        }
    }
    return stain;
}

#endif
