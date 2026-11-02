# Profiling

A worst-case stress test plus profiler markers make performance work repeatable. The stress test runs in the editor or in a development build, writes a text report, and never touches the player's real saves.

## When to run it

Run the stress test only when a change could affect performance, or when the user asks for it. Changes to per-frame code, rendering or shaders, chunk streaming, pooling, physics, or save and load qualify. Text, localization, balance values and docs don't.

## Running the stress test

| How | Command |
|---|---|
| Editor menu | **Eco → Profiling → Run Stress Test** (enters play mode, exits when done) |
| Unity CLI (editor open) | `unity command editor invoke_menu --path "Eco/Profiling/Run Stress Test"` |
| Development build | start the player with `-ecoStressTest` (release builds ignore the flag) |

Reports go to `Logs/StressTest/stress_<timestamp>.txt` in the editor and to `<persistentDataPath>/StressTestReports/` in a player. The report is also logged to the console (`unity command console get`). **Eco → Profiling → Open Stress Test Reports** opens the folder.

A full run takes about 2 minutes. Wait for it from a script like this:

```bash
before=$(ls Logs/StressTest/*.txt 2>/dev/null | wc -l)
unity command editor invoke_menu --path "Eco/Profiling/Run Stress Test"
until [ "$(ls Logs/StressTest/*.txt 2>/dev/null | wc -l)" -gt "$before" ]; do sleep 5; done
cat "$(ls -t Logs/StressTest/*.txt | head -1)"
```

## What it sets up

`StressTest` (`Assets/Eco/Scripts/Profiling/`) runs before the first scene loads when a run is requested:

1. Loads `Assets/Eco/ContentDescriptions/Profiling/Resources/StressTestConfig.asset`.
2. Points `SaveManager.SaveDirectory` at a fresh sandbox, `<persistentDataPath>/StressTest/`, so the player's own save is never touched.
3. Writes a save of the config's `world` (the scene's `WorldData`) through `SaveManager`. Every land chunk without a pile gets scrap, trees on a grid and green ground; pile and water chunks generate as usual. The game then loads that save through the normal `LandChunk` load path, so chunk spawning, scrap pooling, tree pooling and terrain/grass painting all run the real code.
   Trees no longer paint grass when restored. The driver explicitly paints a grass clearing around the player during Load so the Plant phase still exercises successful placements.
4. Forces the detail quality from the config (High by default, the densest grass).
5. The scene starts the game as usual, and a `StressTestDriver` runs the phases.

### Phases

| Phase | What happens | Exercises |
|---|---|---|
| Load | Boot until `GameController.Initialized` | Save load, pools, terrain copy, NavMesh build, the first 169 chunks, grass painting |
| Idle | Stand still | Steady-state cost of the full world (render, physics, scripts) |
| Plant | Plant `treesToPlant` trees through `TreePlanter.PlantTree` on a grass clearing prepared during Load | Nearest valid grass search, placement spacing, pooling and tile saves |
| Collect | Drive a small loop at the player's own speed; recycle the stomach when it is full or pickups stall | `ItemCollector` scan, `Stomach.Reserve`, tentacle animations and meshes, stomach floating, `ItemRecycler`, `ScrapItem.Dissolve` |
| Traverse | Drive a hexagonal loop across the world at `moveSpeed` | Chunk streaming one hex at a time (15 spawns and 15 despawns per crossing at render radius 7) |
| Churn | Teleport between two spots `2·renderRadius+1` hexes apart | Worst-case streaming: all 169 chunks despawn and a full new set spawns in one frame |

### Config (`StressTestConfig`)

| Field | Default | Notes |
|---|---|---|
| `world` | `World/WorldData` | The painted world; must be the one the scene's `WorldController` streams and the terrain was baked from |
| `scrapPerChunk` | 10 | The game's chunks spawn 2–3 |
| `treeSpacing` | 6 tiles | Seeded tree grid spacing. Gameplay spacing now comes from the sum of each tree definition's placement radius |
| `clearingRadiusChunks` | 1 | Tree-free hexes around the spawn hex so the Plant phase has ground to plant on |
| `treePrefabs` | Pine, Apple, Pear | Must match `PoolManager.treePrefabs` in the scene |
| `detailQuality` | High | Overrides the saved setting for the run |
| `idleSeconds`, `treesToPlant`, `plantInterval` | 5, 10, 0.5 | |
| `collectSeconds`, `collectLoopSize` | 20, 8 m | |
| `moveSpeed`, `routeRadiusChunks`, `laps`, `maxTraverseSeconds` | 15 m/s, 6, 1, 180 | The loop is clamped to two hexes inside the map radius |
| `churnJumps`, `churnInterval` | 6, 1 s | |
| `profilerCaptureSeconds` | 0 (off) | Records the start of Traverse to `Logs/StressTest/stress_<timestamp>.raw`. Editor captures grow by about 30 MB per second |
| `quitWhenDone` | on | Exit play mode (editor) or quit (player) after the report |

## Reading the report

For each phase the report lists:

- frame time: average, p50, p95, p99, max, and the number of frames over 33 ms and 100 ms
- GC allocations per frame
- max SetPass calls and triangles
- scene counts at the end of the phase: chunks, scrap, trees, rigidbodies awake
- every marker that ran, with its total ms, worst frame, frames it ran in, and call count

The markers live in `EcoProfilerMarkers` and wrap the known heavy paths: save load/save, pools, terrain reset/paint, NavMesh build, world update, chunk spawn/despawn, land chunk init, `SaveTiles`, tree planting, fruit growth, collector scan, stomach reserve and floating, jelly body and tentacle updates, the slime trail, and scrap recycle. They also appear in the Profiler window. To instrument something new, add a marker there; the report picks it up automatically.

Editor numbers include editor overhead such as Game view `OnGUI`. For absolute numbers, run a development build with `-ecoStressTest`. Compare runs on the same machine; run-to-run variation is small (the first two baseline runs were within a few percent).

### Capture summaries

With `profilerCaptureSeconds > 0`, summarize the newest capture as text: main-thread self time and GC allocations per sample, plus the worst frames broken down. Use **Eco → Profiling → Summarize Latest Capture** (logs the summary), or:

```bash
unity command script eval --timeout 300 --code "return Eco.Scripts.EditorTools.ProfilerCaptureSummary.Summarize();"
```

## Results (2026-10-25)

Editor, Intel Ultra 7 265K, RTX 3060 Ti, 1920×1080, default config (629 field chunks, 6,290 scrap, 1,732 trees saved; 121 chunks, about 1,100 scrap and 280 trees loaded at once). "Before" is the first baseline at `c9a7b36`; "after" is `Logs/StressTest/stress_20261025_144341.txt` with the fixes below.

| Phase | avg ms | p99 ms | max ms | frames >33 ms | What changed |
|---|---|---|---|---|---|
| Load | 19.8 → 16.3 | 607 → 581 | 1040 → 1071 | 8 → 8 | SpawnWorld 517 → 306 ms (NavMesh 254 → 32, grass paint 412 → 4). GC.Collect (~390 ms) and the first chunk spawn (~210 ms) are unchanged |
| Idle | 2.75 → 2.59 | 4.3 → 4.7 | 7.3 → 13.8 | 0 → 0 | Noise; nothing heavy runs here |
| Plant | 3.30 → 2.68 | 12.6 → 4.2 | 42.7 → 11.7 | 5 → 0 | One tree costs about 3.5 ms instead of 23 ms |
| Collect | 3.66 → 2.94 | 8.0 → 4.6 | 63.0 → 33.5 | 20 → 1 | One recycled item costs under 2 ms instead of 25–45 ms |
| Traverse | 3.71 → 3.02 | 14.7 → 6.2 | 64.9 → 42.2 | 46 → 2 | Grass paint is about 1.2 ms per crossing instead of 35 ms |
| Churn | 7.75 → 3.05 | 132 → 4.4 | 447 → 136 | 12 → 6 | A full swap costs about 125 ms instead of 450 ms; 13 rigidbodies awake afterwards instead of 442; physics peak 25 → 3 ms |

Run-to-run variation is a few percent on averages; single-frame maxima vary more (Idle's max is one 14 ms frame).

These numbers predate the hex world. The first hex run (`Logs/StressTest/stress_20261029_132712.txt`: the default 275-hex island, 248 seeded land chunks, 169 chunks loaded, 3840×2160 Game view) measured avg 7.7 / 7.5 / 8.6 / 7.4 / 6.1 ms for Idle / Plant / Collect / Traverse / Churn, with Churn's worst frame at 52 ms. It is not directly comparable: a different world, chunk count and resolution. Use it as the new baseline.

### Slime trail (2026-10-30)

Same machine and settings as the hex baseline, render radius 7. Baseline `stress_20261030_112637`; with the trail `stress_20261030_115824` and `_120033`; with the trail but the grass/ground shader hooks compiled out `_120221`.

| Phase | avg ms baseline | with trail (2 runs) | hooks compiled out |
|---|---|---|---|
| Idle | 7.62 | 8.20 / 7.75 | 8.36 |
| Plant | 7.33 | 7.64 / 7.51 | 8.23 |
| Collect | 8.72 | 9.01 / 8.89 | 9.15 |
| Traverse | 7.92 | 8.22 / 8.14 | 8.10 |
| Churn | 6.48 | 6.86 / 6.82 | 6.69 |

`Eco.SlimeTrail.Update` costs about 0.02 ms per frame while moving and nothing standing still; trail grass paints in `Eco.TerrainPainter.PaintPending` average about 0.02 ms each (one per second while moving, plus a ground commit every 2 s). The shader hooks don't measure above run-to-run noise: compiling them out didn't make any phase faster. The differences to the baseline are within the editor's ±0.5 ms spread between runs. Every run has one 2.7–3.2 ms `SlimeTrail.Update` frame in Collect, the first phase where the jelly moves, and none after that (Traverse peaks at 0.12 ms): the first use of the trail's code paths.

Since the trail only paints bare ground, it lays almost nothing in the stress world, which is green apart from the clearing and pile hexes. A run with the trail (`stress_20261030_131111`) and one with its object disabled (`_131527`) match on frame time (Collect 9.23 / 8.99 ms, Traverse 8.40 / 8.40 ms) and GC allocation per phase. `TerrainPainter.ResetTerrain` costs about 27 ms at load against 13 ms before the trail. The extra goes on zeroing the two 1024² growth maps (copied from a cleared native buffer, about 1.4 ms each; a per-texel loop took 7 ms each), the per-cell claim grid, and scanning the baked splat map for existing grass (about 2 ms).

### Terrain anti-tiling (2026-10-30)

Baseline `stress_20261030_161827`; enabled `stress_20261030_162336` and `_162559`.
Same editor, RTX 3060 Ti, 3840×2160, High detail, 279 seeded land chunks / 2,790 scrap / 631 trees.
Anti-tiling strength 1, macro colour strength 0.16, patch size 22 m.

| Phase | baseline avg ms | enabled avg ms (two runs) |
|---|---|---|
| Idle | 8.27 | 8.39 / 8.16 |
| Plant | 7.80 | 7.94 / 7.85 |
| Collect | 9.28 | 9.34 / 9.59 |
| Traverse | 9.07 | 8.68 / 8.81 |
| Churn | 6.60 | 7.71 / 7.75 |

The visual improvement costs about 1.1 ms in this worst-case Churn comparison; other phases
remain close to baseline. These are full-frame editor measurements, not isolated GPU timings.
Each terrain layer uses three samples at full strength instead of one. Intermediate strengths
also sample the original texture (four samples); zero uses the original single sample.
Macro variation adds two procedural noise evaluations per terrain pixel. There are no new
terrain passes or textures. C#, all four terrain shaders and their normal/mask/height-blend
variants compiled without errors; near and forced distant terrain were visually checked.

### Soil pores and grain (2026-10-30)

Baseline with anti-tiling already enabled: `stress_20261030_163608`; with soil surface detail:
`stress_20261030_164112`. Same 3840×2160 / High detail / RTX 3060 Ti setup as above.

| Phase | baseline avg ms | soil detail avg ms |
|---|---|---|
| Idle | 7.91 | 8.16 |
| Plant | 7.63 | 7.74 |
| Collect | 9.49 | 9.46 |
| Traverse | 8.73 | 8.75 |
| Churn | 7.80 | 7.83 |

Non-load averages are within 0.25 ms of baseline in this comparison. This is a full-frame
editor measurement; it does not isolate GPU cost. Soil detail adds two filtered texture
samples to visible dirt, using one 512² linear RGB24 texture with mipmaps (about 1 MiB on
this platform). A uniform strength check and spatial fade skip sampling on grass and far
terrain. The texture is generated offline; there is no runtime texture generation or new
render pass. Normal/mask/height-blend terrain variants compiled without shader messages,
and on/off captures at gameplay distance checked pores and the grass boundary.

Scrap ground stains and static scrap, 2,790 seeded scrap, 1,441 loaded, same setup as above.
Three runs: dropped scrap with no stains (`stress_20261102_190019`), dropped scrap with stains
(`stress_20261102_185650`), and the final static scrap with stains (`stress_20261102_191915`).

| Phase | no stains avg ms | stains avg ms | static + stains avg ms |
|---|---|---|---|
| Idle | 9.95 | 10.25 | 8.62 |
| Plant | 9.18 | 9.80 | 8.25 |
| Collect | 10.46 | 11.47 | 9.86 |
| Traverse | 9.14 | 9.67 | 8.70 |
| Churn | 6.98 | 7.47 | 6.98 |

Stains cost 0.3 to 1 ms a frame, and taking scrap out of the simulation more than pays for them:
physics time about halves (Traverse 3,721 to 1,858 ms in total). One run each, at this density and
resolution. Every terrain pixel pays for one noise sample and four texel loads; stained pixels add
two more samples. `Eco.ScrapStainMap.Upload` only runs in frames where stains changed: at most
7 ms at load and 4 ms in Churn, because a stain of the default 2.2 m radius is about 15 cells per
map. A larger `scrapStainRadius` costs more there. The two maps add about 8 MiB.
Compare runs by seeded scrap count: the stress config's density has changed before.

## Fixed bottlenecks

These were found by the first baseline and fixed on 2026-10-25.

1. **Terrain/grass repainting on every chunk spawn.** `TerrainPainter.PaintTerrainTexture` now queues a circle. On the next frame `PaintPending` paints the whole queue with one `GetAlphamaps`/`SetAlphamaps` and one `GetDetailLayer`/`SetDetailLayer` over the batch's bounding box. Circles already painted since `ClearTerrain` are skipped: the key includes the grass density, so changing the detail quality still repaints. Unity 6.5 quirk: for a non-square region, `GetDetailLayer` returns z-major data in an array declared `[width, height]`, while `SetDetailLayer` expects `[height, width]`. The painter copies the raw buffer across, so don't index the array that `GetDetailLayer` returns directly.
2. **World progress file write on every chunk save.** `ProgressTracker` marks progress dirty and `SaveProgress()` writes it in `GameController.EndGame` and just before the world list opens (`GameUI`, `ProgressDisplay`), which is the only place that reads it.
3. **`Map` saving the whole world per recycled item and per planted tree.** The map no longer calls `SaveWorld()`. It subscribes to `SaveManager.OnChunkUpdated` and redraws only those chunks, once per frame. When it scrolls, only the fields that come into or go out of view change, instead of all 100 being rebuilt. `MapField` reuses its color buffer and returns its tree icons before redrawing; before this, every redraw stacked another icon per tree.
4. **Redundant `SaveTiles` on spawn.** A chunk loaded from its save no longer saves on spawn (its tiles already match), and new field and pile chunks save once.
5. **Load spikes.** The NavMesh is baked over the plateau and its 50 m slope (`CollectObjects.Volume`) instead of the whole 1,050 m terrain. Chunk pools are sized at load for the most chunks of each type visible from anywhere in the world, so walking to the coast doesn't instantiate chunks mid-game. `field_tiles_*.json` is written unindented, which makes the file smaller and quicker to parse. `ClearTerrain` writes fresh maps without reading the old ones first. In the editor, play mode paints and flattens a copy of the TerrainData, so `New Terrain 1.asset` is no longer modified.
6. **Scrap physics on spawn.** (Superseded: scrap is no longer simulated at all, and `ScrapRestingPoses` is gone.) When a field chunk despawns, `ScrapRestingPoses` records each sleeping scrap item's local pose. When the chunk respawns, those items go back to that pose and `Sleep()`, after `Physics.SyncTransforms()` so the transform sync doesn't wake them. The pose is taken before the chunk goes back to the pool, because `ObjectPool.ReturnToPool` deactivates it before `OnDespawn`. Chunks are now placed before `Init`, so scrap spawns at its final position. Scrap in a chunk that has never been loaded this session still drops and settles.
7. **GC churn while moving.** `UpdateWorld` reuses its collections, `SaveTiles` overwrites the chunk's existing save array, and the per-tree terrain arrays and progress JSON are gone. Moving now allocates about 14 KB per frame, against 11 KB standing still (before: 18 KB and 11 KB).
8. **Minor.** `TreePlanter.PlantTree` no longer builds a LINQ dictionary. `Map.Update` writes `anchoredPosition` only when the player moved or the follow position changed. `FieldChunk` creates its debug `GUIStyle` lazily in `OnDrawGizmos` instead of on every spawn.

The stress test's Collect phase now empties the cart before it starts. Scrap that rolls into reach during Idle used to fill the cart without the driver counting it, and that left Collect with 0 pickups in some runs.

## Known bottlenecks

1. **A full chunk swap in one frame (Churn): about 125 ms.** Most of it is activating about 1,100 scrap items, 280 trees and 121 chunks (`SpawnChunk` about 100 ms, `DespawnChunk` about 26 ms). It only happens on a teleport; walking crosses one boundary at a time. Fix: spread spawning and despawning over several frames.
2. **Load.** A single GC.Collect of about 390 ms right after the scene loads, plus the first spawn of 121 chunks (about 210 ms). The load allocates about 106 MB. The fade-in hides it, but it adds to load time.
3. **Chunk crossing: up to about 20 ms.** This is `GameObject` activation (about 3 ms of `ActivateAwakeRecursively`) and the drop of scrap seen for the first time. Occasionally an `Instantiate` adds to it when a scrap pool runs dry, since scrap pools still start at one item per prefab. `Chunk.Setup` still allocates 100 `Tile`s per spawn. Reusing them isn't safe as long as scrap in the cart keeps a reference to its old tile.
4. **GC.Collect of about 19 ms every 12–25 s while moving.** In the editor, about 80% of the idle 11 KB per frame is Game view `OnGUI`, not game code (Traverse capture, 2026-10-25). Measure a development build before chasing the rest.
5. **`NavMeshObstacle` on every tree and loose scrap item:** about 0.15 ms per frame in `NavMeshObstacle.Transform` for 280 trees, and about 1 ms for 1,441 scrap items (see below).

### Scrap obstacles (2026-11-02)

Baseline `stress_20261102_164151`; non-carving `_164357`; carving (`carveOnlyStationary`, not kept) `_164657`. Default config, 1,441 scrap loaded, 3840×2160 / High. Avg (p95) frame ms:

| Phase | baseline | non-carving | carving |
|---|---|---|---|
| Idle | 7.84 (10.0) | 9.29 (12.4) | 10.20 (14.1) |
| Plant | 7.45 (9.1) | 9.95 (12.0) | 10.32 (18.7) |
| Collect | 9.05 (11.1) | 10.00 (12.0) | 10.59 (19.6) |
| Traverse | 7.77 (10.1) | 8.73 (10.8) | failed, 5 crossings |
| Churn | 6.74 (7.9) | 7.13 (8.2) | 7.25 (10.6) |

Non-carving costs about 1 ms at this density, which is three times the game's. Carving almost doubles p95 while anything moves, and Traverse failed within a second with 6 warps. Non-carving obstacles only steer agents, and paths still run through them, so at the stress config's 10 scrap per chunk and 15 m/s the jelly jams: Traverse fell from 44 crossings and 0 warps to 17 and 6, which makes that report's Traverse line a different workload from the baseline. The jam is intended gameplay, not a bug to fix: heavy scrap regions are meant to be hard to push through, and clearing them makes the jelly faster. Warps in Traverse at the default config are expected. At the game's density and speed (`scrapPerChunk` 3, `moveSpeed` 8, `stress_20261102_164835`, 436 scrap loaded) it made 47 crossings with 0 warps. That run has no baseline at the same settings, so scale the 1 ms by scrap count (about 0.3 ms) rather than treating it as measured.


### Tree planting menu (2026-10-31)

Baseline `stress_20261031_003342`; final `stress_20261031_004500`, same 3840×2160 / High setup.
Both planted 10 trees. Average frame times (ms): Idle 7.74 → 7.77, Plant 7.55 → 7.50,
Collect 8.98 → 8.91, Traverse 8.28 → 8.07, Churn 7.60 → 6.63. Final traversal completed
44 chunk crossings with no recovery warps. This compares the changed gameplay workloads:
trees no longer paint grass, and the test explicitly provides a grass clearing during Load.
The final planting marker took 21.5 ms total over 10 calls, worst call 4.37 ms.

Sandbox checks also covered pointer click and drag planting with one charge, blocked UI drops,
Escape cancellation, insufficient funds, live unlock/refund changes, bare ground and water
rejection, growing versus mature trail grass, summed-radius spacing across spatial cells,
reservation release, and saved trees in unloaded chunks. English and Russian HUD layouts were
visually inspected. Unity compilation and runtime console reported zero errors.


### Horizontal tree menu and ghost preview (2026-10-31)

Baseline `stress_20261031_004727`, final `stress_20261031_110006`. Average frame times (ms): Load 46.12 -> 49.09, Idle 7.44 -> 7.09, Plant 7.58 -> 6.72, Collect 8.80 -> 8.20, Traverse 7.89 -> 7.36, Churn 6.77 -> 6.46. Both planted 10 trees; final traversal crossed 44 chunks with no warps. The automated run covers the menu at rest; active preview rendering was checked separately in the sandbox.

Validated icon-and-price-only entries, hidden scrollbars, horizontal wheel scrolling, valid and blocked ghost colors for all three trees, alpha-clipped foliage, one LOD per tree, no gameplay components on previews, cancellation cleanup, and successful pointer drag/release with one charge. Shader and C# compilation succeeded; final runtime console had zero errors.

### Free tree preview and growth (2026-10-31)

Baseline `stress_20261031_114150`; final runs `stress_20261031_194705` and
`stress_20261031_194937`. All planted 10 trees and completed traversal without warps.
The planting marker decreased from 21.7 ms total (4.26 ms maximum) to 9.2 ms
(3.48 ms maximum) and 9.9 ms (4.12 ms maximum).

Overall performance is **not signed off**: baseline average Idle/Plant/Traverse/Churn
was 7.50/8.56/7.74/6.98 ms; final runs measured 9.01/9.72/16.45/96.91 ms and
79.28/111.25/64.09/38.10 ms. These runs were hours apart and the later measurements
show substantial variation even during idle. The cause of the frame-time degradation
has not been isolated; the planting marker alone does not establish overall performance.

Sandbox checks verified that the ghost exactly matches the unsnapped terrain hit,
release plants the chosen cell and charges once, and nearest-cell selection matches
an exhaustive search at 40 positions (including redirecting from an occupied cell).
Water drops are rejected. All tree prefabs have zero particle systems. Growth starts
small, settles to the authored scale/rotation and resets when interrupted by pooling.
Unity reported no compilation or runtime errors in the completed stress runs.
