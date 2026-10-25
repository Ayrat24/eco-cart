# Unity 6.5 upgrade notes

The project moved from **6000.2.0f1** to **6000.5.2f1** (URP 17.2 → 17.5). Unity 6.5 turns several long-standing deprecations into compile errors (CS0619) and drops the legacy VR module and URP compatibility mode, so the first open failed to compile. Every error came from packages or third-party assets; the game code in `Assets/Eco/Scripts` needed no changes.

## Errors seen

| Source | Error |
|---|---|
| VContainer 1.16.9 `LifetimeScope.cs` | `Object.GetInstanceID()` is obsolete |
| Cinemachine 3.1.4 `CinemachineStoryboard.cs` | `Object.GetInstanceID()` is obsolete |
| UniTask (unpinned git) `UniTaskTrackerTreeView.cs` | `TreeView`/`TreeViewItem`/`TreeViewState` are obsolete |
| R3 (unpinned git) `ObservableTrackerTreeView.cs` | same `TreeView` errors |
| AI Assistant 1.0.0-pre.12, AI Generators 1.0.0-pre.19, AI Toolkit | `GetInstanceID`, `InstanceIDToObject`, `EndNameEditAction` |
| HighlightPlus `HighlightEffect.cs` | `GetInstanceID()` |
| HighlightPlus `HighlightPlusRenderPassFeature.cs` | `Execute(ScriptableRenderContext, ref RenderingData)` / `Configure(CommandBuffer, …)`: no method to override |
| HighlightPlus `HighlightEffectEditor.cs` | `PlayerSettings.virtualRealitySupported` missing |
| Bitgem StylisedWater `WaterVolumeBase.cs` | `GetInstanceID()` |

## Package changes (`Packages/manifest.json`)

| Package | Before | After | Why |
|---|---|---|---|
| `jp.hadashikick.vcontainer` | `#1.16.9` | `#1.19.0` | uses `GetEntityId` on 6000.4+ |
| `com.cysharp.unitask` | unpinned | `#2.5.11` | generic `TreeView<int>` on 6000.2+ |
| `com.cysharp.r3` | unpinned | `#1.3.1` | generic `TreeView<int>` on 6000.2+ |
| `com.unity.cinemachine` | 3.1.4 | 3.1.7 | `GetEntityId` on 6000.3+ |
| `com.unity.ai.assistant` | 1.0.0-pre.12 | 2.21.0-pre.1 | 6.5-compatible; now bundles the Generators modules |
| `com.unity.ai.generators` | 1.0.0-pre.19 | removed | merged into AI Assistant 2.x; the latest standalone version (1.7.0-pre.1) still fails on 6.5 |
| `com.unity.2d.enhancers` | 1.0.0 | removed | depends on the standalone `com.unity.ai.generators`; editor-only AI tool, not used by the game |

`com.unity.ai.toolkit` was only a dependency of the AI packages and drops out with them.

## Local patches to third-party assets

All guarded with `#if UNITY_6000_x_OR_NEWER` so the original path is kept:

- `Assets/HighlightPlus/Runtime/Scripts/HighlightEffect.cs` — two `gameObject.GetInstanceID()` → `GetEntityId().GetHashCode()` (used for a sorting offset and a combined-mesh hash only).
- `Assets/HighlightPlus/Runtime/Scripts/HighlightPlusRenderPassFeature.cs` — legacy `Configure`/`Execute` compiled out on 6.5; the `RecordRenderGraph` path is used.
- `Assets/HighlightPlus/Editor/HighlightEffectEditor.cs` — `CheckVRSupport` body compiled out on 6.5 (legacy VR settings removed).
- `Assets/Bitgem/StylisedWater/URP/Scripts/Bitgem/VFX/StylisedWater/WaterVolumeBase.cs` — mesh name built from `GetEntityId()`. Existing generated water meshes get renamed once and regenerated.

If HighlightPlus or Bitgem is updated from the Asset Store, check whether the new version already handles 6.5 before re-applying.

## Still to watch

- Right after the package swap, Burst logs `Burst internal compiler error … com.unity.ai.assistant@91c166a13c3b\…\System.Runtime.CompilerServices.Unsafe.dll` (the old package folder). Its compiler server caches assembly paths; restart the editor once to clear it.

- `FindObjectOfType`/`FindObjectsOfType` (`Assets/Editor/FindUIBlockers.cs`, HighlightPlus `Misc.cs`) are deprecated warnings, not errors, in 6.5.
- R3 core is still 1.3.0 from NuGet while the Unity package is 1.3.1; 1.3.1 only changed the Unity editor tree view, so they are compatible.
