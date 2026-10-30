using System.IO;
using System.Linq;
using Eco.Scripts.World;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// Paints the hex world: build a palette of chunks (terrain height and texture, scrap, prefab), paint them onto
    /// the map, and Save to bake the result into the scene terrain. <b>Eco → World Editor</b>, or double-click a
    /// World Data asset.
    /// </summary>
    public class WorldEditorWindow : EditorWindow
    {
        private const string LastWorldKey = "Eco.WorldEditor.LastWorld";
        private const string BrushSizeKey = "Eco.WorldEditor.BrushSize";
        private const string WorldFolder = "Assets/Eco/ContentDescriptions/World";
        private const string ChunkFolder = WorldFolder + "/Chunks";

        private static readonly string[] WorldSettings =
        {
            "hexRadius", "mapRadius", "waterLevel", "tideHeight", "tidePeriod", "heightBlend", "textureBlend",
            "shoreBlend", "shelfWidth", "edgeWobble", "roughnessSize", "grassLayer", "ocean", "scrapItems"
        };

        [SerializeField] private WorldData world;
        [SerializeField] private ChunkDefinition selected;

        private SerializedObject _serializedWorld;
        private ObjectField _worldField;
        private WorldMapCanvas _canvas;
        private ListView _palette;
        private VisualElement _worldSettings;
        private VisualElement _chunkPanel;
        private Label _status;
        private VisualElement _tracker;
        private bool _pileSyncQueued;

        [MenuItem("Eco/World Editor")]
        public static void Open()
        {
            var window = GetWindow<WorldEditorWindow>();
            window.titleContent = new GUIContent("World Editor");
            window.minSize = new Vector2(900, 450);
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(EntityId entityId, int line)
        {
            if (EditorUtility.EntityIdToObject(entityId) is not WorldData worldData)
            {
                return false;
            }

            Open();
            GetWindow<WorldEditorWindow>().SetWorld(worldData);
            return true;
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            var toolbar = new Toolbar();
            _worldField = new ObjectField { objectType = typeof(WorldData), allowSceneObjects = false };
            _worldField.style.width = 220;
            _worldField.RegisterValueChangedCallback(evt => SetWorld(evt.newValue as WorldData));
            toolbar.Add(_worldField);
            toolbar.Add(new ToolbarButton(CreateWorld) { text = "New" });

            toolbar.Add(new ToolbarSpacer());
            var brushSize = new SliderInt("Brush", 0, 6)
            {
                value = EditorPrefs.GetInt(BrushSizeKey, 0),
                showInputField = true
            };
            brushSize.style.width = 220;
            brushSize.labelElement.style.minWidth = 40;
            brushSize.RegisterValueChangedCallback(evt =>
            {
                _canvas.BrushSize = evt.newValue;
                EditorPrefs.SetInt(BrushSizeKey, evt.newValue);
            });
            toolbar.Add(brushSize);
            toolbar.Add(new ToolbarButton(() => _canvas.FrameAll()) { text = "Frame" });

            toolbar.Add(new ToolbarSpacer { flex = true });
            _status = new Label();
            _status.style.unityTextAlign = TextAnchor.MiddleRight;
            _status.style.marginRight = 8;
            toolbar.Add(_status);
            var save = new ToolbarButton(Save) { text = "Save + Bake Terrain" };
            save.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(save);
            root.Add(toolbar);

            var body = new VisualElement();
            body.style.flexGrow = 1;
            body.style.flexDirection = FlexDirection.Row;
            root.Add(body);

            body.Add(CreateLeftPanel());

            _canvas = new WorldMapCanvas { BrushSize = brushSize.value };
            _canvas.Painted += UpdateStatus;
            _canvas.Picked += chunk => Select(chunk != null ? chunk : world != null ? world.Ocean : null);
            _canvas.HoverChanged += OnHoverChanged;
            body.Add(_canvas);

            var right = new ScrollView();
            right.style.width = 320;
            right.style.flexShrink = 0;
            right.style.borderLeftWidth = 1;
            right.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
            _chunkPanel = new VisualElement();
            _chunkPanel.style.paddingLeft = 6;
            _chunkPanel.style.paddingRight = 6;
            _chunkPanel.style.paddingTop = 6;
            right.Add(_chunkPanel);
            body.Add(right);

            if (world == null)
            {
                world = AssetDatabase.LoadAssetAtPath<WorldData>(EditorPrefs.GetString(LastWorldKey, string.Empty));
                if (world == null)
                {
                    var guids = AssetDatabase.FindAssets("t:WorldData");
                    if (guids.Length > 0)
                    {
                        world = AssetDatabase.LoadAssetAtPath<WorldData>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    }
                }
            }

            var current = world;
            world = null;
            SetWorld(current);
        }

        private VisualElement CreateLeftPanel()
        {
            var left = new ScrollView();
            left.style.width = 250;
            left.style.flexShrink = 0;
            left.style.borderRightWidth = 1;
            left.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
            left.contentContainer.style.paddingLeft = 6;
            left.contentContainer.style.paddingRight = 6;
            left.contentContainer.style.paddingTop = 6;

            left.Add(Header("Palette"));
            var hint = new Label("Left-drag paints, right-drag erases to ocean, Ctrl+click picks. " +
                                 "Middle-drag pans, wheel zooms, F frames. Where chunks meet, the lower one in " +
                                 "the list paints over the higher one.");
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.opacity = 0.6f;
            hint.style.marginBottom = 4;
            left.Add(hint);

            _palette = new ListView
            {
                makeItem = MakePaletteItem,
                bindItem = BindPaletteItem,
                fixedItemHeight = 22,
                selectionType = SelectionType.Single,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated
            };
            _palette.style.minHeight = 120;
            _palette.selectionChanged += items => Select(items.FirstOrDefault() as ChunkDefinition, false);
            _palette.itemIndexChanged += (_, _) => EditorUtility.SetDirty(world);
            left.Add(_palette);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexWrap = Wrap.Wrap;
            buttons.Add(new Button(() => AddChunk(false)) { text = "+ Land" });
            buttons.Add(new Button(() => AddChunk(true)) { text = "+ Water" });
            buttons.Add(new Button(DuplicateSelected) { text = "Duplicate" });
            buttons.Add(new Button(DeleteSelected) { text = "Delete" });
            left.Add(buttons);

            left.Add(Header("World"));
            _worldSettings = new VisualElement();
            left.Add(_worldSettings);
            left.Add(new Button(ClearWorld) { text = "Erase Everything" });
            return left;
        }

        private static Label Header(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 6;
            label.style.marginBottom = 2;
            return label;
        }

        private static VisualElement MakePaletteItem()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var swatch = new VisualElement { name = "Swatch" };
            swatch.style.width = 14;
            swatch.style.height = 14;
            swatch.style.marginRight = 6;
            swatch.style.borderTopLeftRadius = swatch.style.borderTopRightRadius =
                swatch.style.borderBottomLeftRadius = swatch.style.borderBottomRightRadius = 3;
            row.Add(swatch);

            row.Add(new Label { name = "Name" });
            var tag = new Label { name = "Tag" };
            tag.style.marginLeft = StyleKeyword.Auto;
            tag.style.opacity = 0.6f;
            row.Add(tag);
            return row;
        }

        private void BindPaletteItem(VisualElement element, int index)
        {
            var chunk = world.Palette[index];
            var color = chunk != null ? chunk.Color : Color.magenta;
            color.a = 1f;
            element.Q("Swatch").style.backgroundColor = color;
            element.Q<Label>("Name").text = chunk != null ? chunk.name : "(missing)";

            string tag = chunk == null ? string.Empty : chunk.IsWater ? "water" : chunk.Scrap.HasPile ? "pile" : "land";
            if (chunk != null && chunk == world.Ocean)
            {
                tag = "ocean";
            }

            element.Q<Label>("Tag").text = tag;
        }

        private void SetWorld(WorldData worldData)
        {
            if (_canvas == null || (worldData == world && _serializedWorld != null))
            {
                return;
            }

            world = worldData;
            _worldField.SetValueWithoutNotify(worldData);
            _serializedWorld = worldData != null ? new SerializedObject(worldData) : null;
            EditorPrefs.SetString(LastWorldKey, worldData != null ? AssetDatabase.GetAssetPath(worldData) : string.Empty);

            // Changes to the world settings (or undo) redraw the map
            _tracker?.RemoveFromHierarchy();
            _tracker = new VisualElement();
            rootVisualElement.Add(_tracker);
            if (_serializedWorld != null)
            {
                _tracker.TrackSerializedObjectValue(_serializedWorld, _ =>
                {
                    _canvas.Refresh();
                    _palette.RefreshItems();
                });
            }

            _worldSettings.Clear();
            if (_serializedWorld != null)
            {
                foreach (var property in WorldSettings)
                {
                    var field = new PropertyField(_serializedWorld.FindProperty(property));
                    field.Bind(_serializedWorld);
                    _worldSettings.Add(field);
                }
            }

            _palette.itemsSource = worldData != null ? worldData.Palette : null;
            _palette.Rebuild();
            _canvas.SetWorld(worldData);

            if (worldData == null || !worldData.Palette.Contains(selected))
            {
                selected = worldData != null ? worldData.Palette.FirstOrDefault(c => c != null) : null;
            }

            Select(selected);
            UpdateStatus();
        }

        private void Select(ChunkDefinition chunk, bool updateList = true)
        {
            selected = chunk;
            _canvas.Brush = chunk;

            if (updateList && world != null)
            {
                int index = world.Palette.IndexOf(chunk);
                if (index >= 0)
                {
                    _palette.SetSelectionWithoutNotify(new[] { index });
                    _palette.ScrollToItem(index);
                }
                else
                {
                    _palette.ClearSelection();
                }
            }

            ShowChunk(chunk);
        }

        private void ShowChunk(ChunkDefinition chunk)
        {
            _chunkPanel.Clear();
            if (chunk == null)
            {
                _chunkPanel.Add(new HelpBox(world == null
                    ? "Pick a World Data asset in the toolbar, or click New."
                    : "Add a chunk to the palette to start painting.", HelpBoxMessageType.Info));
                return;
            }

            var name = new TextField("Name") { value = chunk.name, isDelayed = true };
            name.RegisterValueChangedCallback(evt => Rename(chunk, evt.newValue));
            _chunkPanel.Add(name);

            var warnings = new VisualElement();
            _chunkPanel.Add(warnings);

            var serialized = new SerializedObject(chunk);
            var inspector = new InspectorElement(serialized);
            inspector.style.paddingLeft = 0;
            inspector.style.paddingRight = 0;
            _chunkPanel.Add(inspector);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = 6;
            buttons.Add(new Button(() => OpenPrefab(chunk)) { text = "Open Prefab" });
            buttons.Add(new Button(() => EditorGUIUtility.PingObject(chunk)) { text = "Select Asset" });
            if (world != null && world.Ocean != chunk && chunk.IsWater)
            {
                buttons.Add(new Button(() => SetOcean(chunk)) { text = "Use as Ocean" });
            }

            _chunkPanel.Add(buttons);

            void Refresh()
            {
                ShowChunkWarnings(chunk, warnings);
                _canvas.Refresh();
                _palette.RefreshItems();
                QueuePileSync(chunk);
            }

            inspector.TrackSerializedObjectValue(serialized, _ => Refresh());
            ShowChunkWarnings(chunk, warnings);
        }

        private void ShowChunkWarnings(ChunkDefinition chunk, VisualElement container)
        {
            container.Clear();
            if (!ChunkPrefabFactory.KindMatches(chunk))
            {
                container.Add(new HelpBox($"The prefab is a {(chunk.IsWater ? "land" : "water")} chunk. Recreate it " +
                                          "as a variant of the right base (objects added to it are lost).",
                    HelpBoxMessageType.Warning));
                container.Add(new Button(() =>
                {
                    if (EditorUtility.DisplayDialog("Recreate Prefab",
                            $"Replace the prefab of '{chunk.name}'? Objects added to it are lost.", "Recreate",
                            "Cancel"))
                    {
                        ChunkPrefabFactory.Create(chunk);
                        ShowChunk(chunk);
                    }
                }) { text = "Recreate Prefab" });
            }

            if (world == null)
            {
                return;
            }

            if (chunk.IsWater && chunk.Elevation >= world.WaterLevel)
            {
                container.Add(new HelpBox("The bed of a water chunk should be below the water level " +
                                          $"({world.WaterLevel}).", HelpBoxMessageType.Warning));
            }
            else if (!chunk.IsWater && chunk.Elevation < world.WaterLevel)
            {
                container.Add(new HelpBox("This land is below the water level, so it ends up flooded.",
                    HelpBoxMessageType.Info));
            }

            if (chunk.TerrainLayer == null)
            {
                container.Add(new HelpBox("No terrain layer: the terrain's first layer is used.",
                    HelpBoxMessageType.Info));
            }
        }

        // Adding or removing the pile edits the prefab; not from inside the inspector's change callback
        private void QueuePileSync(ChunkDefinition chunk)
        {
            if (_pileSyncQueued || ChunkPrefabFactory.PileMatches(chunk) || !ChunkPrefabFactory.KindMatches(chunk))
            {
                return;
            }

            _pileSyncQueued = true;
            rootVisualElement.schedule.Execute(() =>
            {
                _pileSyncQueued = false;
                if (chunk != null)
                {
                    ChunkPrefabFactory.Sync(chunk);
                }
            });
        }

        private void OnHoverChanged(Vector2Int? coord)
        {
            UpdateStatus(coord);
        }

        private void UpdateStatus() => UpdateStatus(null);

        private void UpdateStatus(Vector2Int? hover)
        {
            if (_status == null)
            {
                return;
            }

            if (world == null)
            {
                _status.text = string.Empty;
                return;
            }

            string text = $"{world.Cells.Count} painted";
            if (hover.HasValue)
            {
                var chunk = world.GetPainted(hover.Value);
                text = $"({hover.Value.x}, {hover.Value.y}) {(chunk != null ? chunk.name : "ocean")}   " + text;
            }

            _status.text = text;
        }

        private void CreateWorld()
        {
            Directory.CreateDirectory(WorldFolder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{WorldFolder}/WorldData.asset");
            var asset = CreateInstance<WorldData>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            SetWorld(asset);
        }

        private void AddChunk(bool water)
        {
            if (world == null)
            {
                return;
            }

            Directory.CreateDirectory(ChunkFolder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{ChunkFolder}/{(water ? "Water" : "Land")}.asset");
            var chunk = CreateInstance<ChunkDefinition>();
            var serialized = new SerializedObject(chunk);
            serialized.FindProperty("isWater").boolValue = water;
            serialized.FindProperty("color").colorValue =
                water ? new Color(0.2f, 0.45f, 0.8f) : Color.HSVToRGB(Random.Range(0.15f, 0.4f), 0.5f, 0.75f);
            serialized.FindProperty("elevation").floatValue = water ? world.WaterLevel - 2f : 0f;
            serialized.FindProperty("terrainLayer").objectReferenceValue =
                world.Palette.FirstOrDefault(c => c != null && c.IsWater == water && c.TerrainLayer != null)
                    ?.TerrainLayer;
            if (water)
            {
                serialized.FindProperty("scrap").FindPropertyRelative("count").vector2IntValue = Vector2Int.zero;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(chunk, path);
            ChunkPrefabFactory.Create(chunk);
            AssetDatabase.SaveAssetIfDirty(chunk);

            AddToPalette(chunk);
        }

        private void DuplicateSelected()
        {
            if (world == null || selected == null)
            {
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(selected);
            string path = AssetDatabase.GenerateUniqueAssetPath(sourcePath);
            AssetDatabase.CopyAsset(sourcePath, path);
            var copy = AssetDatabase.LoadAssetAtPath<ChunkDefinition>(path);

            // A copy keeps the prefab's added objects but is a prefab of its own, with its own save id
            var serialized = new SerializedObject(copy);
            serialized.FindProperty("id").stringValue = System.Guid.NewGuid().ToString("N");
            if (selected.Prefab != null)
            {
                string prefabSource = AssetDatabase.GetAssetPath(selected.Prefab);
                string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{ChunkPrefabFactory.PrefabFolder}/{copy.name}.prefab");
                AssetDatabase.CopyAsset(prefabSource, prefabPath);
                serialized.FindProperty("prefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<Chunk>();
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(copy);
            AddToPalette(copy);
        }

        private void AddToPalette(ChunkDefinition chunk)
        {
            Undo.RecordObject(world, "Add Chunk");
            world.Palette.Add(chunk);
            EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssetIfDirty(world);
            _palette.RefreshItems();
            Select(chunk);
        }

        private void DeleteSelected()
        {
            if (world == null || selected == null)
            {
                return;
            }

            if (selected == world.Ocean)
            {
                EditorUtility.DisplayDialog("Delete Chunk", $"'{selected.name}' is the ocean. Pick another ocean " +
                                                            "chunk in the World settings first.", "OK");
                return;
            }

            int painted = world.Cells.Count(c => c.chunk == selected);
            if (!EditorUtility.DisplayDialog("Delete Chunk",
                    $"Delete '{selected.name}', its prefab, and erase its {painted} painted hexes? This can't be undone.",
                    "Delete", "Cancel"))
            {
                return;
            }

            var chunk = selected;
            foreach (var cell in world.Cells.Where(c => c.chunk == chunk).ToList())
            {
                world.ClearCell(cell.coord);
            }

            world.Palette.Remove(chunk);
            EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssetIfDirty(world);

            ChunkPrefabFactory.Delete(chunk);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(chunk));

            _palette.RefreshItems();
            _canvas.Refresh();
            Select(world.Palette.FirstOrDefault(c => c != null));
            UpdateStatus();
        }

        private void Rename(ChunkDefinition chunk, string newName)
        {
            newName = newName.Trim();
            if (string.IsNullOrEmpty(newName) || newName == chunk.name)
            {
                return;
            }

            string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(chunk), newName);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning(error);
                ShowChunk(chunk);
                return;
            }

            ChunkPrefabFactory.Rename(chunk);
            _palette.RefreshItems();
        }

        private void SetOcean(ChunkDefinition chunk)
        {
            _serializedWorld.Update();
            _serializedWorld.FindProperty("ocean").objectReferenceValue = chunk;
            _serializedWorld.ApplyModifiedProperties();
            ShowChunk(chunk);
        }

        private static void OpenPrefab(ChunkDefinition chunk)
        {
            ChunkPrefabFactory.Sync(chunk);
            if (chunk.Prefab != null)
            {
                AssetDatabase.OpenAsset(chunk.Prefab.gameObject);
            }
        }

        private void ClearWorld()
        {
            if (world == null || !EditorUtility.DisplayDialog("Erase Everything",
                    "Erase every painted hex back to ocean?", "Erase", "Cancel"))
            {
                return;
            }

            Undo.RecordObject(world, "Erase World");
            world.ClearAllCells();
            EditorUtility.SetDirty(world);
            _canvas.Refresh();
            UpdateStatus();
        }

        private void Save()
        {
            if (world == null)
            {
                return;
            }

            if (world.Ocean == null || !world.Ocean.IsWater)
            {
                EditorUtility.DisplayDialog("Save World", "Pick a water chunk as the ocean in the World settings.",
                    "OK");
                return;
            }

            var terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                EditorUtility.DisplayDialog("Save World", "The open scene has no terrain to bake into.", "OK");
                return;
            }

            foreach (var chunk in world.Palette.Where(c => c != null))
            {
                ChunkPrefabFactory.Sync(chunk);
                AssetDatabase.SaveAssetIfDirty(chunk);
            }

            WorldTerrainBaker.Bake(world, terrain);
            AssetDatabase.SaveAssetIfDirty(world);
            AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
            UseInScene();

            Debug.Log($"[World Editor] Saved '{world.name}' ({world.Cells.Count} painted hexes) and baked it into " +
                      $"'{terrain.terrainData.name}'.");
        }

        // The scene's WorldController must stream the world the terrain was baked from
        private void UseInScene()
        {
            var controller = FindAnyObjectByType<WorldController>();
            if (controller == null || controller.WorldData == world)
            {
                return;
            }

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("worldData").objectReferenceValue = world;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log($"[World Editor] {controller.name} now uses '{world.name}'; save the scene to keep it.",
                controller);
        }

        private void OnUndoRedo()
        {
            if (world == null || _canvas == null)
            {
                return;
            }

            world.RebuildLookup();
            _serializedWorld?.Update();
            _palette.RefreshItems();
            _canvas.Refresh();
            UpdateStatus();
        }
    }
}
