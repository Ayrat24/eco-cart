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
| Collect | Drive a small loop at the cart's own speed; empty the cart into the recycler when it is full or pickups stall | `ItemCollector` scan, `Cart.AddToCart`, hand animations, `ItemRecycler`, `ScrapItem.Recycle` |
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

The markers live in `EcoProfilerMarkers` and wrap the known heavy paths: save load/save, pools, terrain clear/flatten/paint, NavMesh build, world update, chunk spawn/despawn, chunk init, `SaveTiles`, progress tracking and progress file write, tree planting, collector scan, cart add, and scrap recycle. They also appear in the Profiler window. To instrument something new, add a marker there; the report picks it up automatically.

Editor numbers include editor overhead such as Game view `OnGUI`. For absolute numbers, run a development build with `-ecoStressTest`. Compare runs on the same machine; run-to-run variation is small (the first two baseline runs were within a few percent).

### Capture summaries

With `profilerCaptureSeconds > 0`, summarize the newest capture as text: main-thread self time and GC allocations per sample, plus the worst frames broken down. Use **Eco → Profiling → Summarize Latest Capture** (logs the summary), or:

```bash
unity command script eval --timeout 300 --code "return Eco.Scripts.EditorTools.ProfilerCaptureSummary.Summarize();"
```

## Baseline (2026-10-25)

Editor, Intel Ultra 7 265K, RTX 3060 Ti, 1920×1080, default config (629 field chunks, 6,290 scrap, 1,732 trees saved; 121 chunks, about 1,100 scrap and 280 trees loaded at once).

| Phase | avg ms | p99 ms | max ms | frames >33 ms | Main cost |
|---|---|---|---|---|---|
| Load | — | 607 | 1040 | 8 | SpawnWorld 517 ms (NavMesh 254, first chunks 210), grass paint 416 ms, GC.Collect 399 ms, save load 129 ms |
| Idle | 2.75 | 4.3 | 7.3 | 0 | Physics about 0.65 ms per step with ~1,100 scrap rigidbodies |
| Plant | 3.30 | 12.6 | 42.7 | 5 | about 23 ms per tree: a full `SaveWorld` per planted tree |
| Collect | 3.66 | 8.0 | 63.0 | 20 | about 25 ms per recycled item (up to 45): a full `SaveWorld` per item |
| Traverse | 3.71 | 14.7 | 64.9 | 46 | grass repaint about 35 ms per chunk crossing (up to 58) |
| Churn | 7.75 | 132 | 447 | 12 | grass repaint 430 ms, chunk spawn 134 ms, despawn 62 ms per full swap |

Steady-state rendering is not the problem on this hardware (Idle averages 2.75 ms). The game hitches because main-thread work happens all at once in a single frame.

## Bottlenecks found

Ranked by impact on hitches. None are fixed yet; these are the starting points for optimization.

1. **Terrain/grass repainting on every chunk spawn.** `FieldChunk.Init` → `MakeGrass` calls `TerrainPainter.PaintTerrainTexture` once per tree. Each call does `GetAlphamaps`/`SetAlphamaps` and `GetDetailLayer`/`SetDetailLayer` on a 26×26 region, and every `Set*` re-uploads terrain data. A chunk crossing costs about 35 ms; a full swap costs 430 ms. The paint already persists in `TerrainData` after the chunk unloads (only `ClearTerrain` at game start resets it), so repainting areas that are already painted is wasted work. Fixes:
   - remember which trees or areas are painted and skip them
   - batch one read/modify/write per chunk instead of one per tree
   - spread painting over frames
2. **A world progress file write on every chunk save.** `Chunk.SaveTiles` → `SaveManager.OnChunkUpdated` → `ProgressTracker.OnChunkUpdated` → `WorldProgressData.SaveForWorld` serializes JSON and calls `File.WriteAllText` synchronously, every time. That is about 0.2 ms per call, and calls come in bursts:
   - ~20 per chunk crossing
   - 630 in `ProgressTracker.Init` (one per saved chunk)
   - 1,400 per full chunk swap
   
   Fix: mark progress dirty and write it once, on `EndGame` or on a timer.
3. **`Map` saves the whole world per recycled item and per planted tree.** `Map.OnScrapRecycled` and `Map.OnTreePlanted` call `WorldController.SaveWorld()`, which runs `SaveTiles` on all 121 loaded chunks. Each of those does a progress file write (finding 2), so one item or tree costs about 120 file writes and about 25 ms. The recycled tile's chunk has already saved itself in `ScrapItem.ResetTile`, and `TreePlanter` saves through `MakeGrass`, so `UpdateVisibleFields()` alone should be enough. This is the cheapest fix with the biggest gameplay impact: hitches during the core loop.
4. **Redundant `SaveTiles` on spawn.** `FieldChunk.Init` saves twice (in `MakeGrass()` and again when there is no save), and `PileChunk.Init` also saves twice. Together with finding 2 this makes spawning cost more than it should.
5. **Load spikes.**
   - `RebuildNavMesh` takes 254 ms.
   - GC.Collect takes 400 ms after the save JSON is parsed. Load allocates 129 MB; the save is written with `Formatting.Indented`, and each chunk allocates `new Tile` ×100 in `Chunk.Setup`.
   - Pools start too small: chunk pool `renderRadius²` = 25 versus 121 needed, and scrap pools at 1 item. The first spawns `Instantiate`.
6. **Scrap physics on spawn.** Scrap spawns 0.9 m up with a random rotation, so every newly spawned item falls and wakes. After a full swap, 444 rigidbodies are awake and physics peaks at 24 ms. Spawning on the ground and calling `Rigidbody.Sleep()` would avoid it.
7. **Steady GC churn of about 18 KB per frame** while moving (`UpdateWorld` allocations, `SaveTiles` arrays, JSON, terrain arrays) causes a roughly 19 ms GC.Collect every 25 s or so.
8. **Minor:**
   - `NavMeshObstacle` on every tree costs about 0.2 ms per frame in transform sync.
   - `Map.Update` sets `anchoredPosition` every frame (about 0.15 ms).
   - `TreePlanter.PlantTree` builds a LINQ dictionary of active chunks for each tree.
   - In the editor, `TerrainPainter` edits the `New Terrain 1.asset` TerrainData in place, so a later **File → Save Project** writes painted grass into the asset. Instantiating the TerrainData at runtime would avoid that.
