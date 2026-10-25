# CLAUDE.md

Guide for AI agents and contributors working in this repo. See [README.md](README.md) for the overview and [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) for how the systems fit together.

## Project facts

- Unity **6000.5.2f1** (Unity 6.5), URP 17.5, RenderGraph only (no compatibility mode).
- Game code: `Assets/Eco/Scripts/` → `Assembly-CSharp`. Everything else under `Assets/` is third-party unless noted.
- Main/only build scene: `Assets/_Recovery/0 (1).unity`. `Assets/Eco/Scenes/Playground.unity` is a test scene.
- Branch `version-2` is active development; `main` is the base.

## Checking for compile errors

There is no CLI build. With the editor open, Unity recompiles when its window gets focus; errors are written to `Logs/Editor.log` in the project:

```bash
grep -E "error CS" "Logs/Editor.log" | sort -u
```

If the editor is closed, a batch compile works:

```bash
"C:/Program Files/Unity/Hub/Editor/6000.5.2f1-x86_64/Editor/Unity.exe" -batchmode -quit -projectPath . -logFile Logs/batch.log
```

## Code conventions (match existing code)

- Namespaces `Eco.Scripts.<Folder>`; one type per file; Allman braces.
- `[SerializeField] private` fields in camelCase, private fields `_camelCase`, constants PascalCase.
- Dependency injection through VContainer: register services/components in `GameScope.Configure`, receive them with an `[Inject]` method (`Init`/`Initialize`) — not constructors on MonoBehaviours, not `FindObjectOfType`.
- Async with UniTask (`async UniTask`, `.Forget()`, `CancellationToken`) — not coroutines.
- Observable state with R3 (`ReactiveProperty`, `Subject`); dispose subscriptions in `Clear()`/`OnDestroy()`.
- Money/score uses `LargeNumbers.AlphabeticNotation`, never `float`/`int`.
- Gameplay tuning lives in ScriptableObjects under `Assets/Eco/ContentDescriptions/`, not in code.
- Player-facing text goes through Unity Localization tables in `Assets/Eco/LocTables/` (en + ru).
- Pooled objects implement `IPooledObject` and go through `PoolManager` / `ObjectPool<T>`.

## Unity 6.5 API rules

Unity 6.5 turned several deprecations into compile errors (CS0619). In new code and when touching third-party code:

- `GetInstanceID()` → `GetEntityId()`; `EditorUtility.InstanceIDToObject` → `EntityIdToObject`.
- IMGUI `TreeView`/`TreeViewItem`/`TreeViewState` → generic `<int>` versions.
- `EndNameEditAction` → `AssetCreationEndAction`.
- URP render passes: implement `RecordRenderGraph` only; `ScriptableRenderPass.Execute/Configure` no longer exist.
- `FindObjectOfType`/`FindObjectsOfType` still compile (warnings) but prefer `FindAnyObjectByType`/`FindObjectsByType`.

Guard third-party fixes with `#if UNITY_6000_x_OR_NEWER` so the original code path stays intact.

## Third-party code patched locally

Re-apply these if the asset is re-imported/updated (details in [Docs/UNITY_6_5_UPGRADE.md](Docs/UNITY_6_5_UPGRADE.md)):

- `Assets/HighlightPlus/Runtime/Scripts/HighlightEffect.cs` — `GetEntityId()`
- `Assets/HighlightPlus/Runtime/Scripts/HighlightPlusRenderPassFeature.cs` — legacy pass methods compiled out on 6.5
- `Assets/Bitgem/StylisedWater/URP/Scripts/Bitgem/VFX/StylisedWater/WaterVolumeBase.cs` — `GetEntityId()`

## Packages

- Git packages (`Packages/manifest.json`) are pinned to tags: UniTask `#2.5.11`, R3 `#1.3.1`, VContainer `#1.19.0`. Keep them pinned — an unpinned URL silently resolves to whatever commit is locked.
- R3 core and Newtonsoft.Json come from NuGetForUnity (`Assets/packages.config`, `Assets/Packages/`); keep the R3 NuGet version in step with the R3 Unity package.
- Don't edit `Library/PackageCache/` — changes are lost on re-resolve. Upgrade the package or embed it under `Packages/` instead.

## Don't

- Commit `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/` or the generated `.csproj`/`.sln` files.
- Hand-edit `.unity`/`.prefab`/`.asset` YAML unless the change is trivial and well understood; prefer doing it in the editor.
- Rename/move assets outside Unity without their `.meta` files.
