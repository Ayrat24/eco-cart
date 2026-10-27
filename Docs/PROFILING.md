# Profiling

A worst-case stress test plus profiler markers make performance work repeatable. The stress test runs in the editor or in a development build, writes a text report, and never touches the player's real saves.

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
2. Points `SaveManager.SaveDirectory` at a fresh sandbox, `<persistentDataPath>/StressTest/`. `last_world_id` is not written, so the real game still opens the world you last played.
3. Clones the base `WorldPreset` (World 5 by default) into a `stress` world and writes its save through `SaveManager`. Every field chunk gets scrap, trees on a grid and green ground; pile, beach and water chunks generate as usual. The game then loads that save through the normal `FieldChunk.LoadSave` path, so chunk spawning, scrap pooling, tree pooling and terrain/grass painting all run the real code.
4. Forces the detail quality from the config (High by default, the densest grass).
5. `WorldSelector` loads the stress world instead of showing the world list, and a `StressTestDriver` runs the phases.

### Phases

| Phase | What happens | Exercises |
|---|---|---|
| Load | Boot until `GameController.Initialized` | Save load, pools, terrain flatten, NavMesh build, the first 121 chunks, grass painting |
| Idle | Stand still | Steady-state cost of the full world (render, physics, scripts) |
| Plant | Buy `treesToPlant` trees through `TreeBuyUpgrade.OnPurchase` | `TreePlanter`, ground marking, grass repaint |
| Collect | Drive a small loop at the player's own speed; recycle the stomach when it is full or pickups stall | `ItemCollector` scan, `Stomach.Reserve`, tentacle animations and meshes, stomach floating, `ItemRecycler`, `ScrapItem.Dissolve` |
| Traverse | Drive a square loop across the world at `moveSpeed` | Chunk streaming one boundary at a time (11 spawns and 11 despawns per crossing) |
| Churn | Teleport between two spots `2·renderRadius+1` chunks apart | Worst-case streaming: all 121 chunks despawn and a full new set spawns in one frame |

### Config (`StressTestConfig`)

| Field | Default | Notes |
|---|---|---|
| `basePreset` | World 5 | Chunk types, scrap prefabs, difficulty |
| `worldSideSize` | 14 | World is `(2·n+1)²` chunks. The terrain is sized for 14, the biggest shipped world |
| `scrapPerChunk` | 10 | Shipped worlds use 2–5 |
| `treeSpacing` | 6 tiles | A real fully green world has trees about 12 tiles apart, because each tree greens a 12-tile circle and the next can only be planted on non-green ground. 6 is about 4× real density |
| `clearingRadiusChunks` | 1 | Tree-free chunks around spawn so the Plant phase has ground to plant on |
| `treePrefabs` | Pine, Apple, Pear | Must match `PoolManager.treePrefabs` in the scene |
| `detailQuality` | High | Overrides the saved setting for the run |
| `idleSeconds`, `treesToPlant`, `plantInterval` | 5, 10, 0.5 | |
| `collectSeconds`, `collectLoopSize` | 20, 8 m | |
| `moveSpeed`, `routeRadiusChunks`, `laps`, `maxTraverseSeconds` | 15 m/s, 10, 1, 180 | |
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

The markers live in `EcoProfilerMarkers` and wrap the known heavy paths: save load/save, pools, terrain clear/flatten/paint, NavMesh build, world update, chunk spawn/despawn, chunk init, `SaveTiles`, progress tracking and progress file write, tree planting, collector scan, stomach reserve and floating, jelly body and tentacle updates, and scrap recycle. They also appear in the Profiler window. To instrument something new, add a marker there; the report picks it up automatically.

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

## Fixed bottlenecks

These were found by the first baseline and fixed on 2026-10-25.

1. **Terrain/grass repainting on every chunk spawn.** `TerrainPainter.PaintTerrainTexture` now queues a circle. On the next frame `PaintPending` paints the whole queue with one `GetAlphamaps`/`SetAlphamaps` and one `GetDetailLayer`/`SetDetailLayer` over the batch's bounding box. Circles already painted since `ClearTerrain` are skipped: the key includes the grass density, so changing the detail quality still repaints. Unity 6.5 quirk: for a non-square region, `GetDetailLayer` returns z-major data in an array declared `[width, height]`, while `SetDetailLayer` expects `[height, width]`. The painter copies the raw buffer across, so don't index the array that `GetDetailLayer` returns directly.
2. **World progress file write on every chunk save.** `ProgressTracker` marks progress dirty and `SaveProgress()` writes it in `GameController.EndGame` and just before the world list opens (`GameUI`, `ProgressDisplay`), which is the only place that reads it.
3. **`Map` saving the whole world per recycled item and per planted tree.** The map no longer calls `SaveWorld()`. It subscribes to `SaveManager.OnChunkUpdated` and redraws only those chunks, once per frame. When it scrolls, only the fields that come into or go out of view change, instead of all 100 being rebuilt. `MapField` reuses its color buffer and returns its tree icons before redrawing; before this, every redraw stacked another icon per tree.
4. **Redundant `SaveTiles` on spawn.** A chunk loaded from its save no longer saves on spawn (its tiles already match), and new field and pile chunks save once.
5. **Load spikes.** The NavMesh is baked over the plateau and its 50 m slope (`CollectObjects.Volume`) instead of the whole 1,050 m terrain. Chunk pools are sized at load for the most chunks of each type visible from anywhere in the world, so walking to the coast doesn't instantiate chunks mid-game. `field_tiles_*.json` is written unindented, which makes the file smaller and quicker to parse. `ClearTerrain` writes fresh maps without reading the old ones first. In the editor, play mode paints and flattens a copy of the TerrainData, so `New Terrain 1.asset` is no longer modified.
6. **Scrap physics on spawn.** When a field chunk despawns, `ScrapRestingPoses` records each sleeping scrap item's local pose. When the chunk respawns, those items go back to that pose and `Sleep()`, after `Physics.SyncTransforms()` so the transform sync doesn't wake them. The pose is taken before the chunk goes back to the pool, because `ObjectPool.ReturnToPool` deactivates it before `OnDespawn`. Chunks are now placed before `Init`, so scrap spawns at its final position. Scrap in a chunk that has never been loaded this session still drops and settles.
7. **GC churn while moving.** `UpdateWorld` reuses its collections, `SaveTiles` overwrites the chunk's existing save array, and the per-tree terrain arrays and progress JSON are gone. Moving now allocates about 14 KB per frame, against 11 KB standing still (before: 18 KB and 11 KB).
8. **Minor.** `TreePlanter.PlantTree` no longer builds a LINQ dictionary. `Map.Update` writes `anchoredPosition` only when the player moved or the follow position changed. `FieldChunk` creates its debug `GUIStyle` lazily in `OnDrawGizmos` instead of on every spawn.

The stress test's Collect phase now empties the cart before it starts. Scrap that rolls into reach during Idle used to fill the cart without the driver counting it, and that left Collect with 0 pickups in some runs.

## Known bottlenecks

1. **A full chunk swap in one frame (Churn): about 125 ms.** Most of it is activating about 1,100 scrap items, 280 trees and 121 chunks (`SpawnChunk` about 100 ms, `DespawnChunk` about 26 ms). It only happens on a teleport; walking crosses one boundary at a time. Fix: spread spawning and despawning over several frames.
2. **Load.** A single GC.Collect of about 390 ms right after the scene loads, plus the first spawn of 121 chunks (about 210 ms). The load allocates about 106 MB. The fade-in hides it, but it adds to load time.
3. **Chunk crossing: up to about 20 ms.** This is `GameObject` activation (about 3 ms of `ActivateAwakeRecursively`) and the drop of scrap seen for the first time. Occasionally an `Instantiate` adds to it when a scrap pool runs dry, since scrap pools still start at one item per prefab. `Chunk.Setup` still allocates 100 `Tile`s per spawn. Reusing them isn't safe as long as scrap in the cart keeps a reference to its old tile.
4. **GC.Collect of about 19 ms every 12–25 s while moving.** In the editor, about 80% of the idle 11 KB per frame is Game view `OnGUI`, not game code (Traverse capture, 2026-10-25). Measure a development build before chasing the rest.
5. **`NavMeshObstacle` on every tree:** about 0.15 ms per frame in `NavMeshObstacle.Transform`.
