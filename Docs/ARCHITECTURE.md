# Architecture

Eco Cart is a cozy clean-up incremental game: the player is a jelly creature that roams a hand-painted hex world, swallows scrap, recycles it for money, digs out scrap piles, plants trees and buys upgrades/helpers.

All game code lives in `Assets/Eco/Scripts/` under the `Eco.Scripts.*` namespaces and compiles into `Assembly-CSharp` (there is no game asmdef).

## Boot flow

1. The only build scene is `Assets/Eco/Scenes/Gameplay.unity` (index 0). Its `GameController` starts the game in `Start` (`startOnLoad`, off in the `Playground` test scene).
2. Settings → reset progress deletes the save, fades out and reloads the scene, which starts a new game.
3. `GameScope` (VContainer `LifetimeScope`) wires dependencies:
   - Plain C# services, scoped: `SaveManager`, `CurrencyManager`, `PlayerStats`, `UpgradeManager`.
   - Scene components: `GameController`, `WorldController`, `HelperManager`, `Player`.
   - ScriptableObjects: `UpgradeTree`, `BaseStats`, `Settings`.
   MonoBehaviours receive services through `[Inject]` methods.
4. `GameController.StartGameAsync()` (UniTask) runs the ordered init: load save → init pools (the world's scrap items) → load settings/upgrades (which also sets `PlayerStats` and the unlocks) → copy the baked terrain → spawn world (pools, NavMesh, chunks around the start position) → UI → spawn player (saved position if still on the NavMesh, else the spawn hex nearest the centre) → pet → currency → map → fade in.
5. `GameController.EndGame()` (also on `OnApplicationQuit`) saves player, pet, currency, upgrades and world tiles.

## Systems

| Area | Key types | Notes |
|---|---|---|
| World | `WorldController`, `WorldData`, `ChunkDefinition`, `Hex`, `HexTileLayout`, `Chunk` (+ `LandChunk`, `WaterChunk`), `PileSite`, `Tile`, `TileData` | A hand-painted world of pointy-top hex chunks (axial coordinates as `Vector2Int`, radius `WorldData.HexRadius`, 6 m by default). `WorldData` holds a palette of `ChunkDefinition`s (terrain elevation, roughness and layer, scrap count/items, pile size, prefab) and the painted cells; every unpainted hex is the ocean chunk. Paint it in **Eco → World Editor** (round brush); **Save + Bake Terrain** writes heights, splat layers and empty grass into the scene terrain. The bake hides the grid: each painted hex is stamped as a round dab a little bigger than the hex (later palette entries over earlier ones, painted over unpainted), the edges wobble with smooth noise (`EdgeWobble`), and the result is blurred with round kernels (`HeightBlend`, `TextureBlend`; the water outline with the wider `ShoreBlend`). Water depth follows the exact distance from the waterline: the bed falls away over `ShelfWidth` metres (25 by default) and land rises back over `HeightBlend`, both crossing the waterline at a small fixed slope so the 16-bit heights can't make the shoreline blocky, and roughness fades out over the shelf. Shorelines therefore don't follow hexes: every chunk has a `WaterSurface` hexagon and shows it when its terrain dips under the water (all surfaces rise and fall together with a slow tide, `TideHeight`/`TidePeriod`, at most 0.15 m so it stays under the NavMesh and scrap margins; the water shader's own vertex waves are off, `_WaveScale` 0 on `WaterCurved.mat`), and its tiles under the water become water ground (no scrap or trees). Land chunks stand on the baked ground at their centre, and pile sites sit at a fixed random offset and turn per hex. Each definition's prefab is a variant of `Chunks/Base/LandChunkBase` or `WaterChunkBase`, so objects added to it only appear in that chunk; definitions with a pile get a nested `ScrapPileSite`. Chunks within `renderRadius` hexes of the player are pulled from per-definition `ObjectPool<Chunk>`s (sized at load for the most of each visible from any land hex) and saved/returned when out of range. A chunk's tiles are the 1 m cells inside its hex; scrap and trees sit on the baked terrain. The NavMesh is baked at load over the land, clipped just above the water, so lakes and sea are not walkable. `TerrainPainter` works on a runtime copy of the baked TerrainData and queues grass circles, painting each frame's batch with one terrain read/write. `ScrapRestingPoses` lets respawned scrap go back to sleep where it came to rest. |
| Scrap | `ScrapItem`, `ScrapPile`, `ScrapType` | `ScrapItem` is a pooled physics item (`ICartItem`, `ITileItem`, `IPooledObject`). `ScrapPile` is dug with the spade (`DigPowerUpgrade`) and highlighted with HighlightPlus. |
| Collecting | `ItemCollector`, `CollectorHand`, `Stomach`, `ItemRecycler`, `GainedScoreText` | Collector (a `Tool`) scans a sphere for items; a `CollectorHand` grows a tentacle out of the body, carries the item to the mouth and the `Stomach` takes it. Swallowed items float inside and the body grows by their volume (bounding box at swallowed size), while capacity stays weight-based. Recycling dissolves them through the recycler, which pays out via `CurrencyManager`. Stomach capacity (`MaxWeight`), tentacle count (`LimbCount`, extra tentacles are cloned from the first) and reach (`LimbRange`) come from `PlayerStats`. |
| Player | `Player`, `PlayerClickMovement`, `CameraController` | NavMeshAgent click-to-move (ignored while the pointer is over UI). Speed and stomach size follow `PlayerStats`; the hat is a `HatDefinition` from `Player.hats`, put on by `HatController`. |
| Jelly | `JellyBody`, `JellyShape`, `JellyDeformation`, `JellyTentacle`, `JellyHandZone`, shaders `Eco/Jelly` and `Eco/JellyContactShadow` | The player's body. `JellyBody` builds the mesh and runs springs for lean, squash, growth and the mouth; the `Eco/Jelly` shader does the deforming (wobble, ground contact, mouth) and draws the body semi-transparent by refracting the camera opaque texture. `JellyDeformation` mirrors the shader on the CPU so tentacles, the mouth, the hat and the spade sit on the visible surface. The mouth slides around the body to face the camera (within the camera leeway, leaning toward the direction of travel) and opens wide and deep enough for the item a tentacle brings; its settings are in `JellyMouthSettings`. Tentacles grow from spots inside the hand zones (`JellyHandSettings`, shared by the body, every tentacle and the collector hands), never in the mouth's vertical strip, and are hidden when not in use. Arms and carried items follow smooth arcs round the body (`JellyBodyArc`), never through it, and the hand stretches out of the arm's end into the mouth. After delivering, a tentacle keeps that shape and shrinks back along it into its root. Swallowed items move to the `Swallowed Item` layer (13), which both URP renderers (`PC_Renderer`, `Mobile_Renderer`) leave out of their opaque/transparent masks; `JellyInsideFeature` draws that layer into `_JellyInsideTexture`, and the body shader blends it in, so items only show inside the jelly (transparent ones too) and parts poking out get the same tint. A new URP renderer needs the feature and the mask change too. **Eco → Jelly → Bake Body Preview Mesh** refreshes the edit-time mesh. |
| Slime trail | `SlimeTrail`, `SlimeTrailSettings`, `GrassGrowthMap`, `TerrainPainter.GrowGrass`, shaders `Eco/SlimeTrail` and `GrassGrowth.hlsl` | The jelly leaves slime that dries up over `lifetime` (5 s); `growDelay` (3 s) after it was laid, grass details and the terrain's grass layer grow in on it over `growDuration` (10 s). The trail is one world-space strip mesh on its own scene object (`SlimeTrail`, with `CurvedWorldBounds`), a point every `pointSpacing` metres; each vertex carries when it was laid and when it dries, so the shader fades it and the mesh only changes when a point is added or dropped. It is queued just before the jelly (Transparent-1), after the camera opaque texture is taken, so the refracting body hides it instead of showing it through. Its look comes from the settings, not the material. It paints only over bare ground: every step it asks `TerrainPainter.IsGrass` about the ground under the jelly, and over grass (or with `SlimeTrail.CanPaint` off) it lays no slime and grows nothing, ending the strip. `TerrainPainter` keeps a claim per detail cell (bare, grass already from the bake or `PaintGrass`, or the trail point that claimed it); ground claimed by the trail's last few points still counts as bare, because each point's claimed ring reaches ahead of the jelly. For the coming paint budget, `SlimeTrail` exposes `CanPaint`, `IsPainting` and `OnPainted` (metres laid per step). Each point goes to `TerrainPainter.GrowGrass` at once with two radii: blades on cells within `grassDetailWidth` and the grass texture layer within the wider `grassTextureWidth`, so no blades stand on bare ground; blades only grow where the trail claimed the ground. Ground is claimed immediately; the terrain writes wait and go in one batch shortly before the earliest grow start (one detail read/write per batch). Cells are stamped in `GrassGrowthMap`, two GPU-only float textures (blades, ground) with one texel per detail cell holding when it is fully grown, written through a staging row and per-cell `Graphics.CopyTexture`, never a full upload. Separate maps let a cell's blades start later than its ground without either popping. The generated grass shader scales each blade by its cell's growth and the generated terrain passes raise the grass splat weight (`CurvedWorldSetup`); both skip the texture read with a uniform branch while nothing is growing. `GroundCommitInterval` (2 s) after cells are grown, `TerrainPainter` writes their grass layer into the alphamap, which matches the shader's ground pixel for pixel, so the green stays after the shader stops reading the map. Cells that already have grass details are left alone. Trail grass is part of the play session's terrain copy only: it isn't saved and doesn't change tiles' ground type (tree planting still sees bare ground). |
| Tools | `ToolController`, `Tool`, `SpadeController`, `UI/ToolSelector` | Collector hands vs spade. |
| Pets | `HelperManager`, `PetDefinition`, `Helper` (+ `CatHelper`, `DogHelper`, `ChickenHelper`, `CollectorHelper`) | NavMesh AI companions, one at a time. The upgrade tree unlocks them; until a pet menu exists, a newly unlocked pet becomes the active one (`HelperManager.SelectPet`, saved as `selectedPet`). Pet tuning is on the pet prefabs. |
| Trees | `TreePlanter`, `Tree`, `TreeDefinition` | The upgrade tree unlocks tree types (`TreeDefinition.Unlocked`); `TreePlanter.PlantTree(prefabId)` plants one near the player and greens the ground. Planting is not wired to a menu yet, and planted trees no longer pay income. |
| Upgrades | `UpgradeTree` (SO), `UpgradeNode`, `UpgradeEffect` (+ `Upgrades/Effects/*`), `UpgradeManager`, `PlayerStats`, `BaseStats` (SO), `StatSheet`, `UnlockTracker` | A graph of nodes in one asset. A node has a name, icon, max level, price (`baseCost · costGrowth^level`), requirements (Any/All of other nodes; none = available from the start) and `[SerializeReference]` effects. `UpgradeManager` keeps the levels (saved in `PlayerProgress.UpgradeLevels` by node id), buys and refunds them (a refund pays back the level's full price and is refused while another bought upgrade still needs it), and after every change rebuilds `PlayerStats` from `BaseStats` plus every bought effect. Unlock effects set `UnlockTracker` flags (trees, pets, map, spade, flowers, butterflies); a refund locks them again through `UnlockTracker.OnLocked`.A new kind of upgrade is a new `UpgradeEffect` subclass. Edit the tree in **Eco → Upgrade Tree Editor** (or double-click the asset): node positions there are the positions in the game menu. |
| Map | `ProgressionScreen/Map`, `MapField`, `MapInputHandler` | Unlockable minimap built from save data: one texture per visited chunk whose pixels are world-aligned 1 m cells owned by that hex, so fields join seamlessly and show no hex outlines; each pixel shows its nearest tile. |
| Pooling | `PoolManager` (singleton), `ObjectPool<T>`, `IPooledObject` | Scrap and tree prefabs. |
| UI | `GameUI`, `UpgradeTreeMenu` + `UpgradeTreeView` (side panel over the left 40% while the game keeps running; pan/zoom tree, `UpgradeTooltip` on hover, − / + beside the selected node; `CameraController` shifts the view so the jelly stays in the middle of the free part; `UpgradeTree.uss`),`CartStorageDisplay` (stomach fill bar),`SettingsMenu`, `TutorialMenu`, `HudController`, `SceneTransition`, `ScoreGainedPopup` | UI Toolkit (`Assets/Eco/UI/*.uxml/.uss`) for HUD/menus; uGUI for the map. |
| Localization | `LocalizationManager`, `Assets/Eco/LocTables/` | Unity Localization, locales `en` and `ru`. Tables: CartUpgrades, GameUI, Hats, HelpersUpgrades, Tutorial, … |
| Settings | `Settings` (SO) | Detail quality, language, free-upgrades cheat; stored in `PlayerPrefs`. |
| Profiling | `StressTest`, `StressTestSeeder`, `StressTestDriver`, `StressTestConfig`, `EcoProfilerMarkers` | Worst-case stress test and profiler markers on the heavy paths; see [PROFILING.md](PROFILING.md). Editor menus live in `Scripts/Editor/` (`Eco.Scripts.EditorTools`). |

## Terrain appearance

**Eco → Rendering → World Visual Settings** controls terrain anti-tiling and broad colour variation.
`Terrain Anti Tiling` blends three randomly offset samples per layer (zero restores regular tiling;
one is the default). `Terrain Colour Variation` defaults to 0.16, with 22 m `Terrain Colour Patch Size`.
The patches stay fixed in world XZ and do not change the texture's detail scale, terrain painting,
or slime-to-grass growth. The same sampling offsets are used for diffuse, normal and mask maps.

`TerrainVariation.hlsl` contains the sampling and colour functions. `CurvedWorldSetup.GenerateShaders`
injects them into the terrain passes and generates a matching `TerrainLitBasemapGen.shader` for
distant terrain. Macro colour is applied at draw time to both near and distant terrain; changing
anti-tiling invalidates the distant colour cache. `CurvedWorldController` publishes the settings as
shader globals in edit mode and play mode. Regenerate adapters after URP updates instead of editing
the generated passes directly.

## Reactive state

R3 is used for observable state: `CurrencyManager.CurrentMoney` (`ReactiveProperty<AlphabeticNotation>`), the `PlayerStats` stats, `UpgradeManager.OnUpgradeBought`,`SaveManager.OnChunkUpdated`, `UnlockTracker.OnUnlocked`. Subscribe in `Init` and dispose in `Clear`/`OnDestroy`.

Money uses `LargeNumbers.AlphabeticNotation` (`Assets/Scripts/LargeNumbers`, own asmdef `com.votrubec.largenumbers`).

## Save data

Written to `SaveManager.SaveDirectory` with Newtonsoft.Json. That is `Application.persistentDataPath`, or `<persistentDataPath>/StressTest/` during a stress test run:

- `progress.json` — `SaveManager.PlayerProgress` (currency, hat, pet, position, upgrade levels by node id). The position is null in a new game.
- `chunks.json` — per hex `ChunkSaveData` (the `ChunkDefinition.Id` it was made for + its `TileData[]`), keyed `"q,r"`, unindented. Written on `GameController.EndGame`; during play chunks save into `SaveManager.Chunks` in memory. A chunk whose hex was repainted with another definition, or whose tile count changed (hex radius), starts fresh.
- `PlayerPrefs` — settings keys.

Saves of the old multi-world game (`field_tiles_*.json`, `world_progress_*.json`) are ignored and deleted by a progress reset.

`GameController` context menu → **Delete Progress** wipes all of the above.

## Content (data-driven)

`Assets/Eco/ContentDescriptions/`:

- `World/WorldData.asset` — the painted world: grid size, water level, terrain blending, grass layer, scrap items, palette and cells. `World/Chunks/` — the palette's `ChunkDefinition`s. Edit both in **Eco → World Editor**; chunk prefabs are in `Data/Prefabs/Chunks/` (bases in `Chunks/Base/`)
- `Jelly/StomachSettings.asset` — swallowed item size, growth per item volume and max growth, how items float inside
- `Jelly/MouthSettings.asset` — mouth placement and camera facing, closed look, how wide and deep it opens per item
- `Jelly/HandSettings.asset` — where tentacles may grow, how they look, pickup timing and the carry path
- `Jelly/SlimeTrailSettings.asset` — the slime trail: how long it lingers, width, point spacing, its look (colours, gloss, edges, drying), and the grass that grows on it (delay, grow time, blade and grass-texture widths)
- `Upgrades/UpgradeTree.asset` — the upgrade tree; `Upgrades/BaseStats.asset` — stats before upgrades, scrap scores and colors, pile reward and dig power
- `Hats/` (`HatDefinition`), `Helpers/` (`PetDefinition`), `Trees/` (`TreeDefinition`) — equipment and plantable trees
- `TreeCatalogue.asset`, `GameSettings.asset`

Add new content by creating an asset from the matching `CreateAssetMenu` entry and adding it to the relevant list (upgrade tree, `Player.hats`, `HelperManager.pets`, `WorldData` scrap items). Add chunks from the World Editor, which also makes their prefabs.
