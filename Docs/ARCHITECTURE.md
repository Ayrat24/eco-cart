# Architecture

Eco Cart is a cozy clean-up incremental game: the player is a jelly creature that roams a chunked world, swallows scrap, recycles it for money, digs out scrap piles, plants trees and buys upgrades/helpers until the world is clean and green enough to unlock the next one.

All game code lives in `Assets/Eco/Scripts/` under the `Eco.Scripts.*` namespaces and compiles into `Assembly-CSharp` (there is no game asmdef).

## Boot flow

1. The only build scene is `Assets/_Recovery/0 (1).unity` (index 0). `WorldSelector` (singleton, `DontDestroyOnLoad`) lives there and holds the `WorldPreset[]` list.
2. `WorldSelector` preselects the last played world (`SaveManager.GetLastWorldId()`, else preset 0). Selecting a world saves the running game (`EndGame`), fades out, reloads scene 0 and calls `GameController.StartGame()`.
3. `GameScope` (VContainer `LifetimeScope`) wires dependencies:
   - Plain C# services, scoped: `SaveManager`, `CurrencyManager`, `ProgressTracker`, `PlayerStats`, `UpgradeManager`.
   - Scene components: `GameController`, `WorldController`, `HelperManager`, `Player`.
   - ScriptableObjects: `UpgradeTree`, `BaseStats`, `Settings`.
   MonoBehaviours receive services through `[Inject]` methods.
4. `GameController.StartGameAsync()` (UniTask) runs the ordered init: load save → init pools → load settings/upgrades (which also sets `PlayerStats` and the unlocks) → spawn world → init progress tracking → UI → spawn player → pet → currency → map → fade in.
5. `GameController.EndGame()` (also on `OnApplicationQuit` and before switching worlds) saves player, pet, currency, upgrades and world tiles.

## Systems

| Area | Key types | Notes |
|---|---|---|
| World | `WorldController`, `WorldPreset`, `ChunkGenerationRules`, `Chunk` (+ `FieldChunk`, `PileChunk`, `WaterChunk`, `BeachChunk`), `Tile`, `TileData` | Chunks are 10×10 (`WorldController.ChunkSize`). Map is `(2·WorldSideSize+1)²` chunks, border is always Beach, outside is Water. Chunks within `renderRadius` of the player are pulled from per-type `ObjectPool<Chunk>`s (sized at load for the most of each type visible from anywhere) and saved/returned when out of range. The terrain is flattened into a plateau and the NavMesh baked over the plateau only. `TerrainPainter` queues grass circles and paints each frame's batch with one terrain read/write, skipping circles already painted; in the editor it paints a runtime copy of the TerrainData. `ScrapRestingPoses` lets respawned scrap go back to sleep where it came to rest. |
| Scrap | `ScrapItem`, `ScrapPile`, `ScrapType` | `ScrapItem` is a pooled physics item (`ICartItem`, `ITileItem`, `IPooledObject`). `ScrapPile` is dug with the spade (`DigPowerUpgrade`) and highlighted with HighlightPlus. |
| Collecting | `ItemCollector`, `CollectorHand`, `Stomach`, `ItemRecycler`, `GainedScoreText` | Collector (a `Tool`) scans a sphere for items; a `CollectorHand` grows a tentacle out of the body, carries the item to the mouth and the `Stomach` takes it. Swallowed items float inside and the body grows by their volume (bounding box at swallowed size), while capacity stays weight-based. Recycling dissolves them through the recycler, which pays out via `CurrencyManager`. Stomach capacity (`MaxWeight`), tentacle count (`LimbCount`, extra tentacles are cloned from the first) and reach (`LimbRange`) come from `PlayerStats`. |
| Player | `Player`, `PlayerClickMovement`, `CameraController` | NavMeshAgent click-to-move (ignored while the pointer is over UI). Speed and stomach size follow `PlayerStats`; the hat is a `HatDefinition` from `Player.hats`, put on by `HatController`. |
| Jelly | `JellyBody`, `JellyShape`, `JellyDeformation`, `JellyTentacle`, `JellyHandZone`, shaders `Eco/Jelly` and `Eco/JellyContactShadow` | The player's body. `JellyBody` builds the mesh and runs springs for lean, squash, growth and the mouth; the `Eco/Jelly` shader does the deforming (wobble, ground contact, mouth) and draws the body semi-transparent by refracting the camera opaque texture. `JellyDeformation` mirrors the shader on the CPU so tentacles, the mouth, the hat and the spade sit on the visible surface. The mouth slides around the body to face the camera (within the camera leeway, leaning toward the direction of travel) and opens wide and deep enough for the item a tentacle brings; its settings are in `JellyMouthSettings`. Tentacles grow from spots inside the hand zones (`JellyHandSettings`, shared by the body, every tentacle and the collector hands), never in the mouth's vertical strip, and are hidden when not in use. Arms and carried items follow smooth arcs round the body (`JellyBodyArc`), never through it, and the hand stretches out of the arm's end into the mouth. After delivering, a tentacle keeps that shape and shrinks back along it into its root. Swallowed items move to the `Swallowed Item` layer (13), which both URP renderers (`PC_Renderer`, `Mobile_Renderer`) leave out of their opaque/transparent masks; `JellyInsideFeature` draws that layer into `_JellyInsideTexture`, and the body shader blends it in, so items only show inside the jelly (transparent ones too) and parts poking out get the same tint. A new URP renderer needs the feature and the mask change too. **Eco → Jelly → Bake Body Preview Mesh** refreshes the edit-time mesh. |
| Tools | `ToolController`, `Tool`, `SpadeController`, `UI/ToolSelector` | Collector hands vs spade. |
| Pets | `HelperManager`, `PetDefinition`, `Helper` (+ `CatHelper`, `DogHelper`, `ChickenHelper`, `CollectorHelper`) | NavMesh AI companions, one at a time. The upgrade tree unlocks them; until a pet menu exists, a newly unlocked pet becomes the active one (`HelperManager.SelectPet`, saved as `selectedPet`). Pet tuning is on the pet prefabs. |
| Trees | `TreePlanter`, `Tree`, `TreeDefinition` | The upgrade tree unlocks tree types (`TreeDefinition.Unlocked`); `TreePlanter.PlantTree(prefabId)` plants one near the player and greens the ground. Planting is not wired to a menu yet, and planted trees no longer pay income. |
| Upgrades | `UpgradeTree` (SO), `UpgradeNode`, `UpgradeEffect` (+ `Upgrades/Effects/*`), `UpgradeManager`, `PlayerStats`, `BaseStats` (SO), `StatSheet`, `UnlockTracker` | A graph of nodes in one asset. A node has a name, icon, max level, price (`baseCost · costGrowth^level`), requirements (Any/All of other nodes; none = available from the start) and `[SerializeReference]` effects. `UpgradeManager` keeps the levels (saved in `PlayerProgress.UpgradeLevels` by node id), buys and refunds them (a refund pays back the level's full price and is refused while another bought upgrade still needs it), and after every change rebuilds `PlayerStats` from `BaseStats` plus every bought effect. Unlock effects set `UnlockTracker` flags (trees, pets, map, spade, flowers, butterflies); a refund locks them again through `UnlockTracker.OnLocked`.A new kind of upgrade is a new `UpgradeEffect` subclass. Edit the tree in **Eco → Upgrade Tree Editor** (or double-click the asset): node positions there are the positions in the game menu. |
| Progress | `ProgressTracker`, `WorldProgressData`, `UI/ProgressDisplay`, `UI/NewWorldPopup` | Tracks clear % and green %; at 90% / 90% it raises `OnProgressThresholdReached` → new-world popup. |
| Map | `ProgressionScreen/Map`, `MapField`, `MapInputHandler` | Unlockable minimap built from save data. |
| Pooling | `PoolManager` (singleton), `ObjectPool<T>`, `IPooledObject` | Scrap and tree prefabs. |
| UI | `GameUI`, `UpgradeTreeMenu` + `UpgradeTreeView` (side panel over the left 40% while the game keeps running; pan/zoom tree, `UpgradeTooltip` on hover, − / + beside the selected node; `CameraController` shifts the view so the jelly stays in the middle of the free part; `UpgradeTree.uss`),`CartStorageDisplay` (stomach fill bar),`SettingsMenu`, `TutorialMenu`, `HudController`, `SceneTransition`, `ScoreGainedPopup` | UI Toolkit (`Assets/Eco/UI/*.uxml/.uss`) for HUD/menus; uGUI for the map. |
| Localization | `LocalizationManager`, `Assets/Eco/LocTables/` | Unity Localization, locales `en` and `ru`. Tables: CartUpgrades, GameUI, Hats, HelpersUpgrades, Tutorial, … |
| Settings | `Settings` (SO) | Detail quality, language, free-upgrades cheat; stored in `PlayerPrefs`. |
| Profiling | `StressTest`, `StressTestSeeder`, `StressTestDriver`, `StressTestConfig`, `EcoProfilerMarkers` | Worst-case stress test and profiler markers on the heavy paths; see [PROFILING.md](PROFILING.md). Editor menus live in `Scripts/Editor/` (`Eco.Scripts.EditorTools`). |

## Reactive state

R3 is used for observable state: `CurrencyManager.CurrentMoney` (`ReactiveProperty<AlphabeticNotation>`), the `PlayerStats` stats, `UpgradeManager.OnUpgradeBought`,`ProgressTracker.ClearPercentage/GreenPercentage`, `SaveManager.OnChunkUpdated`, `UnlockTracker.OnUnlocked`. Subscribe in `Init` and dispose in `Clear`/`OnDestroy`.

Money uses `LargeNumbers.AlphabeticNotation` (`Assets/Scripts/LargeNumbers`, own asmdef `com.votrubec.largenumbers`).

## Save data

Written to `SaveManager.SaveDirectory` with Newtonsoft.Json. That is `Application.persistentDataPath`, or `<persistentDataPath>/StressTest/` during a stress test run:

- `progress.json` — `SaveManager.PlayerProgress` (currency, hat, pet, position, upgrade levels by node id, world id).Shared across worlds; position resets when the world id changes.
- `field_tiles_<worldId>.json` — per-chunk `TileData[]`, keyed `"x,y"`, unindented. Written on `GameController.EndGame`; during play chunks save into `SaveManager.FieldTiles` in memory.
- `world_progress_<worldId>.json` — clear/green progress. `ProgressTracker` marks it dirty on chunk saves and writes it on `EndGame` and right before the world list opens.
- `PlayerPrefs` — `last_world_id`, settings keys.

`GameController` context menu → **Delete Progress** wipes all of the above.

## Content (data-driven)

`Assets/Eco/ContentDescriptions/`:

- `Worlds/` — `WorldPreset 1..5`, `ChunkGenerationRules`
- `Jelly/StomachSettings.asset` — swallowed item size, growth per item volume and max growth, how items float inside
- `Jelly/MouthSettings.asset` — mouth placement and camera facing, closed look, how wide and deep it opens per item
- `Jelly/HandSettings.asset` — where tentacles may grow, how they look, pickup timing and the carry path
- `Upgrades/UpgradeTree.asset` — the upgrade tree; `Upgrades/BaseStats.asset` — stats before upgrades, scrap scores and colors, pile reward and dig power
- `Hats/` (`HatDefinition`), `Helpers/` (`PetDefinition`), `Trees/` (`TreeDefinition`) — equipment and plantable trees
- `TreeCatalogue.asset`, `GameSettings.asset`

Add new content by creating an asset from the matching `CreateAssetMenu` entry and adding it to the relevant list (upgrade tree, `Player.hats`, `HelperManager.pets`, world preset).
