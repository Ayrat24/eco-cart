# Curved world rendering

The Gameplay scene now uses an original radial, parabolic vertex bend. No Curved World asset purchase is required.

Open **Eco → Rendering → World Visual Settings** to edit the shared asset at `Assets/Eco/ContentDescriptions/Rendering/WorldVisualSettings.asset`:

- **World Radius**: approximately `41.67` by default. Larger means a larger-looking, flatter world; smaller means a stronger curve. This changes appearance, not map dimensions.
- **Distortion Strength**: `1` by default; `0` disables bending and `2` doubles it.
- **Distortion Enabled**: toggle the effect globally without losing the chosen radius or strength.
- **Flat Radius**: `6` world units around the player, keeping nearby interactions and the character on flat ground.

Effective curvature is `Distortion Strength / (2 * World Radius)`, preserving the original `0.012` default. All controllers referencing this asset share these values. Rendering, click-to-move, and culling bounds read the same settings. Changes preview live in the editor and during Play mode; as with other tuning assets, edits to the asset during Play mode persist.

The **World → Curved World Controller** still holds its scene-specific **Follow Target** and **Preview In Edit Mode** controls, plus the shared Settings reference. Older scenes without an assigned asset retain their previous serialized curvature until migrated with Apply Curved World.

### Atmosphere and space transition

The **Atmosphere** section in the same settings asset controls a blue rim only at the curved planet's silhouette and the adjoining skybox:

- **Atmosphere Enabled / Strength**: toggle or blend out the effect. Disabling world distortion also skips the atmosphere pass.
- **Horizon Color / Space Color**: the color near the curved surface and the deeper tint as the band fades into the original skybox. Stars remain visible beyond the gradient.
- **Thickness**: apparent angular width in degrees outside the rendered terrain/water silhouette (0.1–20). Width remains consistent across terrain triangles and changes smoothly with camera FOV. The effect is zero outside this band. Orthographic views use the equivalent screen width at their stored FOV.
- **Softness**: falloff exponent within the bounded rim; it never adds a fullscreen haze.

There is no Surface Height setting. Height changes, hills, terrain holes, water waves and finite surface edges come from rendered geometry. The former Start/Full Distance controls were removed: using individual horizon depths for width or brightness caused vertical streaks at grazing camera angles. Existing Thickness numbers are retained, but now specify degrees rather than world units.

`CurvedAtmosphereFeature` runs before post-processing, after the sky and water, on the PC and Mobile gameplay renderers. It alpha-blends into the camera color target and preserves camera alpha. The final pass tests at the far plane against the existing scene depth attachment: only uncovered sky receives the glow, and terrain, scrap, trees, the seabed and the depth-writing player occlude it. The pass does not request, overwrite or reschedule `_CameraDepthTexture`; water retains its original depth/refraction inputs. Overlay cameras, previews and reflections are skipped, and screen-space UI renders afterward. Perspective and orthographic projections are supported.

Terrain and water have an `EcoAtmosphereSurface` pass derived from their existing depth passes, retaining terrain instancing/holes, world bending and water waves. Only these passes draw into a private surface-depth mask at half resolution, capped at 960 pixels wide. The effect seeds surface-to-sky boundaries and propagates the nearest boundary with a bounded jump-flood filter. The composite interpolates distances from four neighboring field samples, giving a smooth falloff without inventing intermediate seed positions. Shading does not depend on seed depth. Objects without the surface pass cannot generate their own halos. Transparent materials that do not write depth allow the glow to show through over uncovered sky, like other geometry behind them. Mask resolution can make very small silhouettes less precise; the scene-depth test remains at camera resolution.

**Eco → Rendering → Apply Curved Atmosphere** installs or repairs the feature, material and surface shader passes without touching vendor/demo assets. `CurvedWorldSetup` and `CurvedWaterSetup` preserve these passes when regenerating shaders. The material references the atmosphere shader so player builds include it. CPU pass recording is tracked by `Eco.CurvedWorld.RecordAtmosphere`; rendering is labeled `Eco.CurvedWorld.Atmosphere` in the GPU profiler.

The deformation is `y -= curvature * max(0, distanceXZFromPlayer - flatRadius)^2`. Colliders, navigation, and saved world coordinates are unchanged. Right-click movement maps the screen ray back into the original physics world with short segments, so the click follows the displayed ground. Positions and normals are bent consistently; this is a curved horizon illusion, not a closed spherical planet.

## Assets and integration

- `Assets/Eco/Data/Shaders/CurvedWorld/CurvedWorld.hlsl`: shared deformation, normal/tangent transformation, and a Shader Graph Custom Function (`EcoCurvedWorld`, Float precision, object-space Position/Normal/Tangent inputs).
- `CurvedLit.shader`: URP Lit adapter for scrap, including transparent glass. Uses URP's original lighting/material properties with bent forward, deferred, shadow, depth, depth-normal, and object motion-vector passes.
- `TerrainLit.shader`, `TerrainLitAdd.shader`, `TerrainLitBase.shader`: terrain layers, add layers, distance basemap, holes, shadows, and depth. Baked/meta passes stay flat. Per-pixel terrain normals are replaced by transformed vertex normals.
- `CurvedVegetation*.shader`: derivatives of the project's existing Polytope shaders. Wind, tinting, foliage cutouts, and snow settings are retained. Bending runs after wind in the rendered geometry passes.
- `CurvedWater.shader`: generated from the installed Bitgem water graph, with bending after waves in each geometry pass. The water chunk uses `WaterCurved.mat`, preserving the original colors, normal texture, foam, refraction, render queue (3001), and disabled motion-vector pass. It follows the same global settings as terrain.
- `Assets/Eco/Data/Materials/CurvedWorld/`: copied materials, including materials embedded in imported models. The 18 gameplay tree/scrap prefabs reference these, so pooled and newly planted objects inherit the effect. The terrain grass prototype's existing material uses the curved Plants shader directly.
- `CurvedWorldBounds`: updates the mesh render bounds analytically after movement, avoiding premature culling. Physics colliders are untouched. Updates allocate no per-frame collections and are profiled under `Eco.CurvedWorld.UpdateBounds`.
- `CurvedTerrainBounds` (on the Terrain): the same for terrain patches. Unity culls each heightmap patch by its flat min/max height, which made patches at the screen corners vanish in squares; this lowers every quadtree patch's bounds by the bend via `TerrainData.OverrideMinMaxPatchHeights`. Bent bounds alone would bring the far side of the planet back into the frustum (about 2.4× the draw calls), so patches past the camera's horizon get empty bounds far below the world. The horizon distance `s + sqrt(H / curvature + (s - flatRadius)^2) + sqrt(h / curvature)` (camera offset `s` from the bend origin, camera height `H` above the lowest terrain, terrain height range `h`) is a conservative bound: past it the sight line passes under the lowest terrain bent by the same parabola. Each override makes Unity rebuild its patch data (about 1 ms), so bounds are built with a 5-unit margin and rebuilt only after the player or camera moves 4 units. It only touches the play-mode TerrainData copy from `TerrainPainter.ResetTerrain`, never the asset, so edit-mode previews still cull flat. Profiled under `Eco.CurvedWorld.UpdateTerrainBounds`. Stress test at render radius 15 (2026-10-30, `stress_20261030_103908` → `_104503`): Traverse average 15.76 → 16.22 ms, Collect 15.36 → 15.96 ms, Idle unchanged.

**Eco → Rendering → Apply Curved World** regenerates the shader derivatives from the installed URP/Polytope source and reapplies the setup to the active gameplay scene. The original shaders are unchanged. Re-run after upgrading URP or the source vegetation assets; changes to generated shaders should instead go in `CurvedWorldSetup`.

Terrain, water, terrain grass, gameplay trees, and scrap/piles are integrated. Helper characters, separate particle effects, and Highlight Plus outline passes have their own shaders and require the same bending hook if they must follow distant terrain. The player remains inside the flat radius. Large curvature values can make these other objects' lack of bending more visible. Terrain picking in Unity's editor still uses the underlying terrain geometry.

**Eco → Rendering → Apply Curved Water** regenerates and reapplies only the water integration; the full Apply Curved World command includes this step too. Water render bounds follow regenerated mesh bounds when pooled chunks rebuild their shoreline foam. The original Bitgem graph and material are unchanged.

The water graph receives the original world-space mesh normal through a separate interpolator for its texture scaling and top/side masks. Its UV scale depends on normal Y; using the bent lighting normal there made the animation slide and stretch as the bend origin moved. URP lighting still receives bent normals/tangents, and foam depth/refraction still use the bent geometry. Make water adapter changes in `CurvedWaterSetup`, not the generated shader.

Vegetation derivatives retain the source asset's existing motion-pass limitations for wind; they add the shared current/previous world-bend transform. This implementation is intended for one active world controller.

## Validation

Validated in the connected Unity 6000.5.2f1 / URP 17.5 editor, including shader compilation, gameplay prefab material assignments, visual captures, and the project's sandboxed stress test (plant, collect, traverse, and chunk churn). Test saves use the existing StressTest sandbox. Captures and performance reports are under `Logs/CurvedWorld/` and `Logs/StressTest/`.

Focused checks: all 24 mesh renderers across the target prefabs use curved materials; all seven shaders report no compilation errors; nine ground-ray targets have a maximum position error of 0.0055 units; 64 transformed tree/scrap bounding-box corners remain inside the updated render bounds. Curvature zero preserves positions exactly.

Water animation regression: a GPU probe renders the generated graph's animated normal texture with fixed geometry and time, then moves the bend origin. Before the normal separation, 16,382 of 16,384 pixels changed; afterward, zero changed. Advancing time still changed 16,379 pixels, confirming animation remains active. The probe isolates texture phase from intended changes in geometry, lighting, and screen-space refraction; its script is retained locally in `Logs/CurvedWorld/verify-water-animation.cs`.

Silhouette atmosphere validation: raised a hill to 8 world units in a temporary runtime terrain copy and placed an opaque object across the horizon about 20 units from the camera. The glow followed the new hill outline and remained behind the foreground object. Frozen on/off captures (`silhouette-uneven-on.png`, `silhouette-uneven-off.png`) had zero changes across all 28,799 foreground-object pixels, 78,000 sampled ocean pixels, and 307,200 foreground pixels. The hill's surrounding sky changed in 13,908 pixels. The test script and pixel comparison are in `Logs/CurvedWorld/test-atmosphere-occlusion.cs` and `verify-silhouette.cs`. Terrain, scene and save assets were not modified by these tests. Atmosphere, water and terrain shaders compiled without errors.

The 2026-10-29 stress comparison completed all phases: `stress_20261029_014542.txt` (previous analytic rim) and `stress_20261029_015319.txt` (surface mask and depth-tested composite). Traverse median was 7.74 → 8.52 ms; Idle median was 7.41 → 8.11 ms. Atmosphere recording averaged about 0.020 ms per Traverse frame. These are whole-frame editor measurements on the same machine, not an isolated GPU measurement or mobile-device benchmark.

On this editor/machine at 3840×2160, median Traverse frame time was 5.70 ms before and 7.01 ms with the bend; maximum rendered triangles increased from about 0.97 M to 1.76 M. These are editor measurements, not a target-device benchmark. The final test's Idle average includes a deliberate screenshot pause and should not be used for comparison. Existing unrelated `UpgradeButton`/Toon package diagnostics were also observed; none were errors in the new shaders.

## Source notices

The generated URP shader derivatives are based on `com.unity.render-pipelines.universal`, copyright © 2020 Unity Technologies ApS, licensed under the Unity Companion License for Unity-dependent projects. See the installed package's `LICENSE.md` and <https://unity.com/legal/licenses/unity-companion-license>.

The generated vegetation shaders derive from the installed Polytope Studio asset and retain its applicable asset license. The shared bend/controller code is project code; the generated derivatives are not standalone replacements for the licensed source assets.

The water derivative comes from the installed Bitgem URP Stylized Water Shader - Proto Series and retains its applicable asset license.
