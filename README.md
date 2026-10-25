# Eco Cart

A game made for GameDeve Goethe 2025. Theme is ecology.

Push your cart around an overgrown, littered island, collect and recycle scrap, dig out scrap piles, plant trees and hire animal helpers until the world is clean and green — then move on to the next world.

My code is in `Assets/Eco/Scripts/`.

## Requirements

- Unity **6000.5.2f1** (Unity 6.5), URP
- Git (several packages are pulled from GitHub)

## Getting started

1. Open the project folder in Unity Hub with 6000.5.2f1.
2. Wait for package resolution and the first import (the `Library/` folder is not committed).
3. Open `Assets/_Recovery/0 (1).unity` — it is the only scene in Build Settings and the game entry point.
4. Press Play. Save data goes to `Application.persistentDataPath`; use the **Delete Progress** context menu on `GameController` to reset.

## Project layout

| Path | Contents |
|---|---|
| `Assets/Eco/Scripts/` | Game code (`Eco.Scripts.*`) |
| `Assets/Eco/ContentDescriptions/` | Data assets: worlds, upgrades, carts, hats, helpers, trees |
| `Assets/Eco/UI/` | UI Toolkit `.uxml` / `.uss` |
| `Assets/Eco/LocTables/`, `Assets/Locales/` | Localization (en, ru) |
| `Assets/Eco/Data/` | Game models, prefabs, materials, shaders, animations |
| `Assets/Scripts/LargeNumbers/` | Large number formatting library (`AlphabeticNotation`) |
| `Assets/Packages/` | NuGet packages (NuGetForUnity): R3 core, Newtonsoft.Json, … |
| `Assets/_Recovery/` | Scenes, including the main build scene |
| Everything else in `Assets/` | Third-party art/tools (HighlightPlus, Bitgem water, Polytope, FinalIK `Plugins/RootMotion`, PicoChan, …) |
| `Docs/` | Project documentation |

## Key dependencies

- **VContainer** — dependency injection (`GameScope`)
- **UniTask** — async/await
- **R3** — reactive properties/events (Unity package from Git + core from NuGet)
- **PrimeTween** — tweening
- **Newtonsoft.Json** — save files
- Unity: URP, AI Navigation (NavMesh), Input System, Localization, Cinemachine, UI Toolkit, Addressables

## Docs

- [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) — systems, boot flow, save data
- [Docs/UNITY_6_5_UPGRADE.md](Docs/UNITY_6_5_UPGRADE.md) — package and code changes made for Unity 6.5
- [CLAUDE.md](CLAUDE.md) — conventions and gotchas for contributors and AI agents
