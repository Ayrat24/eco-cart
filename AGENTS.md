# AGENTS.md

Guide for AI agents and contributors working in this repo. See [README.md](README.md) for the overview and [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) for how the systems fit together.

## Project facts

- Unity **6000.5.2f1** (Unity 6.5), URP 17.5, RenderGraph only (no compatibility mode).
- Game code: `Assets/Eco/Scripts/` → `Assembly-CSharp`. Everything else under `Assets/` is third-party unless noted.
- Main/only build scene: `Assets/_Recovery/0 (1).unity`. `Assets/Eco/Scenes/Playground.unity` is a test scene.
- Branch `version-2` is active development; `main` is the base.

## Unity CLI (driving the open editor)

Use the `unity` CLI (Unity CLI beta, `C:\Users\<user>\AppData\Local\Unity\bin\unity.exe`, on PATH) whenever you need the editor to do something: recompile, read the console, enter/exit play mode, run menu items, run C#. It talks to the open editor through the `com.unity.pipeline` package (in `Packages/manifest.json`). Prefer it over focusing the editor window or tailing `Editor.log`.

```bash
unity status                                     # is the editor reachable? (port, state)
unity command                                    # list every command group
unity command editor status                      # compiling? play mode?
unity command script recompile                   # works while the editor is unfocused
unity command script recompile_status            # poll until "completed"
unity command console status                     # compilationFailed flag + counts
unity command console get                        # console entries (Editor or play mode)
unity command editor invoke_menu --path "Eco/Profiling/Run Stress Test"
unity command editor.playmode start              # / stop / pause
unity command script eval --code "return UnityEngine.Application.dataPath;"
unity command script eval_file --file path/to/snippet.cs   # longer C#; last statement returns
```

Add `--json` for machine-readable output. Many write commands (settings, delete, build target switch) need `--confirm true` and support `--dry_run true`. Command reference: `Library/PackageCache/com.unity.pipeline@*/Documentation~/index.md`.

Gotchas:

- If `unity status` lists nothing, the editor isn't open, is mid domain reload, or its server is down. Wait a few seconds; if it stays empty, use **Window → Pipeline → Start Server** in the editor.
- `Assets/Eco/Scripts/Editor/PipelineDescriptorFix.cs` works around a Pipeline 0.9.1 bug on Unity 6.5: without it the CLI can't discover the editor (`Failed to start publishing the descriptor` in the log). Delete it once the package fixes the bug.
- The editor idles while unfocused. Run `unity command editor set_auto_tick --enable true` once per session so commands and play mode keep running in the background.
- `unity command script eval` runs arbitrary code in the editor. Use it for reads and one-off asset authoring.
- When saving from eval, call `AssetDatabase.SaveAssetIfDirty(asset)` and **never `AssetDatabase.SaveAssets()`**. The editor holds pending Unity 6.5 reserialization of URP/ProjectSettings assets, plus `New Terrain 1.asset` painted by older play sessions (play mode now paints a runtime copy), and `SaveAssets()` writes all of it to disk.

Fallbacks without the CLI: compile errors are in `Logs/Editor.log` (`grep -E "error CS" Logs/Editor.log | sort -u`). With the editor closed, run a batch compile:

```bash
"C:/Program Files/Unity/Hub/Editor/6000.5.2f1-x86_64/Editor/Unity.exe" -batchmode -quit -projectPath . -logFile Logs/batch.log
```

## Profiling

Run the worst-case stress test (**Eco → Profiling → Run Stress Test**, or the `invoke_menu` command above) before and after any performance change, and compare the reports in `Logs/StressTest/`. Wrap new heavy code in a marker from `EcoProfilerMarkers` so it shows up in the report. See [Docs/PROFILING.md](Docs/PROFILING.md) for phases, config, baseline numbers and known bottlenecks.

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
- Editor-only code goes in `Assets/Eco/Scripts/Editor/` with namespace `Eco.Scripts.EditorTools`. A namespace ending in `.Editor` would hide `UnityEditor.Editor` from every `Eco.Scripts.*` editor class.
- Save files are read and written under `SaveManager.SaveDirectory`, never `Application.persistentDataPath` directly; the stress test redirects it to a sandbox.

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
- `com.unity.pipeline` (experimental) is the editor side of the `unity` CLI. Install or upgrade it with `unity pipeline install` / `unity pipeline upgrade`. It is editor tooling only and isn't enabled in player builds by default.
- R3 core and Newtonsoft.Json come from NuGetForUnity (`Assets/packages.config`, `Assets/Packages/`); keep the R3 NuGet version in step with the R3 Unity package.
- Don't edit `Library/PackageCache/` — changes are lost on re-resolve. Upgrade the package or embed it under `Packages/` instead.

## Don't

- Commit `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/` or the generated `.csproj`/`.sln` files.
- Hand-edit `.unity`/`.prefab`/`.asset` YAML unless the change is trivial and well understood; prefer doing it in the editor.
- Rename/move assets outside Unity without their `.meta` files.

## Git

- Commit directly to the current branch (normally `main`). Don't create a feature branch unless the user names one; this overrides any default "branch first" behaviour.
- Commit or push only when asked.
