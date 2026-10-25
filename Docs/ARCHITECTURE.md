# Architecture

Eco Cart is a cozy clean-up incremental game: the player pushes a cart around a chunked world, collects scrap, recycles it for money, digs out scrap piles, plants trees and buys upgrades/helpers until the world is clean and green enough to unlock the next one.

All game code lives in `Assets/Eco/Scripts/` under the `Eco.Scripts.*` namespaces and compiles into `Assembly-CSharp` (there is no game asmdef).

## Boot flow

1. The only build scene is `Assets/_Recovery/0 (1).unity` (index 0). `WorldSelector` (singleton, `DontDestroyOnLoad`) lives there and holds the `WorldPreset[]` list.
2. `WorldSelector` preselects the last played world (`SaveManager.GetLastWorldId()`, else preset 0). Selecting a world saves the running game (`EndGame`), fades out, reloads scene 0 and calls `GameController.StartGame()`.
3. `GameScope` (VContainer `LifetimeScope`) wires dependencies:
   - Plain C# services, scoped: `SaveManager`, `CurrencyManager`, `TreeCurrencyEarner`, `ProgressTracker`, `ScoreStats`.
   - Scene components: `GameController`, `WorldController`, `HelperManager`, `Player`.
   - ScriptableObjects: `UpgradesCollection`, `Settings`.
   MonoBehaviours receive services through `[Inject]` methods.
4. `GameController.StartGameAsync()` (UniTask) runs the ordered init: load save → init pools → load settings/upgrades → spawn world → init progress tracking → UI/camera → spawn player → helpers → currency → tree income → map → fade in.
5. `GameController.EndGame()` (also on `OnApplicationQuit` and before switching worlds) saves player, currency, upgrades and world tiles.

## Systems

| Area | Key types | Notes |
|---|---|---|
| World | `WorldController`, `WorldPreset`, `ChunkGenerationRules`, `Chunk` (+ `FieldChunk`, `PileChunk`, `WaterChunk`, `BeachChunk`), `Tile`, `TileData` | Chunks are 10×10 (`WorldController.ChunkSize`). Map is `(2·WorldSideSize+1)²` chunks, border is always Beach, outside is Water. Chunks within `renderRadius` of the player are pulled from per-type `ObjectPool<Chunk>`s and saved/returned when out of range. The terrain is flattened into a plateau and the NavMesh rebuilt on spawn. |
| Scrap | `ScrapItem`, `ScrapPile`, `ScrapType` | `ScrapItem` is a pooled physics item (`ICartItem`, `ITileItem`, `IPooledObject`). `ScrapPile` is dug with the spade (`DigPowerUpgrade`) and highlighted with HighlightPlus. |
| Collecting | `ItemCollector`, `CollectorHand`, `Cart`, `ItemRecycler`, `GainedScoreText` | Collector (a `Tool`) scans a sphere for items and drops them into the cart; the cart empties into the recycler which pays out via `CurrencyManager`. |
| Player | `Player`, `PlayerClickMovement`, `PlayerAnimation`, `CameraController` | NavMeshAgent click-to-move. Cart and hat are swapped via upgrades. |
| Tools | `ToolController`, `Tool`, `SpadeController`, `UI/ToolSelector` | Collector hands vs spade. |
| Helpers | `HelperManager`, `Helper` (+ `CatHelper`, `DogHelper`, `ChickenHelper`, `CollectorHelper`), `HelperUpgrade` | NavMesh AI companions bought as upgrades. |
| Trees | `TreePlanter`, `Tree`, `TreeCatalogue`, `TreeCurrencyEarner`, `Tree*Upgrade` | Planted trees turn ground green and generate passive income. |
| Upgrades | `Upgrade` (abstract SO), `UpgradesCollection`, `UnlockTracker`, `UnlockableUpgrade`, `SelectableUpgrade`, `ScoreStats` | Each upgrade is a ScriptableObject in `Assets/Eco/ContentDescriptions/`; level is saved in `PlayerProgress.UpgradeLevels`. Cost = `baseCost · costGrowth^level`. `needsUpgrade` gates visibility through `UnlockTracker`. |
| Progress | `ProgressTracker`, `WorldProgressData`, `UI/ProgressDisplay`, `UI/NewWorldPopup` | Tracks clear % and green %; at 90% / 90% it raises `OnProgressThresholdReached` → new-world popup. |
| Map | `ProgressionScreen/Map`, `MapField`, `MapInputHandler` | Unlockable minimap built from save data. |
| Pooling | `PoolManager` (singleton), `ObjectPool<T>`, `IPooledObject` | Scrap and tree prefabs. |
| UI | `GameUI`, `UpgradeMenu`, `CartStorageDisplay`, `SettingsMenu`, `TutorialMenu`, `HudController`, `SceneTransition`, `ScoreGainedPopup` | UI Toolkit (`Assets/Eco/UI/*.uxml/.uss`) for HUD/menus; uGUI for the map. |
| Localization | `LocalizationManager`, `Assets/Eco/LocTables/` | Unity Localization, locales `en` and `ru`. Tables: CartUpgrades, GameUI, Hats, HelpersUpgrades, Tutorial, … |
| Settings | `Settings` (SO) | Detail quality, language, free-upgrades cheat; stored in `PlayerPrefs`. |

## Reactive state

R3 is used for observable state: `CurrencyManager.CurrentMoney` (`ReactiveProperty<AlphabeticNotation>`), `ProgressTracker.ClearPercentage/GreenPercentage`, `SaveManager.OnChunkUpdated`, `UnlockTracker.OnUnlocked`. Subscribe in `Init` and dispose in `Clear`/`OnDestroy`.

Money uses `LargeNumbers.AlphabeticNotation` (`Assets/Scripts/LargeNumbers`, own asmdef `com.votrubec.largenumbers`).

## Save data

Written to `Application.persistentDataPath` with Newtonsoft.Json:

- `progress.json` — `SaveManager.PlayerProgress` (currency, cart, hat, position, upgrade levels, world id). Shared across worlds; position resets when the world id changes.
- `field_tiles_<worldId>.json` — per-chunk `TileData[]`, keyed `"x,y"`.
- `world_progress_<worldId>.json` — clear/green progress.
- `PlayerPrefs` — `last_world_id`, settings keys.

`GameController` context menu → **Delete Progress** wipes all of the above.

## Content (data-driven)

`Assets/Eco/ContentDescriptions/`:

- `Worlds/` — `WorldPreset 1..5`, `ChunkGenerationRules`
- `Carts/`, `Hats/`, `Helpers/`, `Trees/`, `ScrapUpgrades/`, `Unlocks/` — upgrade assets
- `UpgradeCollection.asset` — tabs/groups shown in the upgrade menu
- `TreeCatalogue.asset`, `GameSettings.asset`

Add new content by creating an asset from the matching `CreateAssetMenu` entry and adding it to `UpgradeCollection` / the relevant preset.
