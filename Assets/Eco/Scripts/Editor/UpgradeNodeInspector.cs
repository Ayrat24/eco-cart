using System;
using System.Collections.Generic;
using System.Text;
using Eco.Scripts.Upgrades;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// Side panel of the upgrade tree editor: the selected node's data and effects, the selected requirement line,
    /// or (with nothing selected) the tree's problems and the base stats.
    /// </summary>
    internal class UpgradeNodeInspector : VisualElement
    {
        private const string LocTableName = "Upgrades";

        private readonly UpgradeTreeCanvas _canvas;
        private readonly ScrollView _scroll;

        private UpgradeTree _tree;
        private SerializedObject _serializedTree;

        /// <summary>The panel changed the tree in a way the canvas must rebuild for (ids, requirements, effects).</summary>
        public event Action Edited;

        public UpgradeNodeInspector(UpgradeTreeCanvas canvas)
        {
            _canvas = canvas;
            style.width = 380;
            style.minWidth = 300;
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.style.flexGrow = 1;
            _scroll.contentContainer.style.paddingLeft = 8;
            _scroll.contentContainer.style.paddingRight = 8;
            _scroll.contentContainer.style.paddingTop = 8;
            _scroll.contentContainer.style.paddingBottom = 8;
            Add(_scroll);
        }

        public void Show(UpgradeTree tree, SerializedObject serializedTree)
        {
            _tree = tree;
            _serializedTree = serializedTree;
            _scroll.Clear();

            if (tree == null)
            {
                _scroll.Add(new HelpBox("Pick or create an Upgrade Tree asset in the toolbar.", HelpBoxMessageType.Info));
                return;
            }

            serializedTree.Update();
            var selection = _canvas.Selection;
            if (selection.Count == 1)
            {
                BuildNode(selection[0]);
                _scroll.Bind(serializedTree);
            }
            else if(selection.Count > 1)
            {
                BuildMultiple(selection);
            }
            else if (_canvas.SelectedEdge.HasValue)
            {
                BuildEdge(_canvas.SelectedEdge.Value.parent, _canvas.SelectedEdge.Value.child);
            }
            else
            {
                // Not bound to the tree: the base stats inspector binds to its own asset
                BuildOverview();
            }
        }

        private void BuildNode(UpgradeNode node)
        {
            int index = _tree.EditorNodes.IndexOf(node);
            var property = _serializedTree.FindProperty("nodes").GetArrayElementAtIndex(index);

            AddHeader(EditorLocalizedText.Get(node.Title, node.Id));

            if (_canvas.Problems.TryGetValue(node, out var problem))
            {
                _scroll.Add(new HelpBox(problem, HelpBoxMessageType.Warning));
            }

            var idField = new TextField("Id") { value = node.Id, isDelayed = true };
            idField.tooltip = "Save key. Renaming it keeps the requirement lines but resets this upgrade in existing saves.";
            idField.RegisterValueChangedCallback(evt => RenameNode(node, evt.newValue, idField));
            _scroll.Add(idField);

            _scroll.Add(new PropertyField(property.FindPropertyRelative("title"), "Name"));
            if (node.Title.IsEmpty)
            {
                _scroll.Add(new Button(() =>
                {
                    Undo.RecordObject(_tree, "Create Upgrade Name Entry");
                    string english = ObjectNames.NicifyVariableName(node.Id.Replace('-', ' '));
                    if (EditorLocalizedText.Assign(node.Title, LocTableName, $"upgrade-{node.Id}-name", english))
                    {
                        EditorUtility.SetDirty(_tree);
                        Edited?.Invoke();
                    }
                })
                {
                    text = $"Create name entry in the {LocTableName} table",
                    tooltip = "Adds a key for this node's name and fills in the English text; translate it in the field above."
                });
            }

            _scroll.Add(new PropertyField(property.FindPropertyRelative("description")));

            var iconProperty = property.FindPropertyRelative("icon");
            _scroll.Add(new PropertyField(iconProperty));
            var iconPreview = new Image { scaleMode = ScaleMode.ScaleToFit };
            iconPreview.style.height = 64;
            iconPreview.style.marginBottom = 4;
            UpdateIconPreview(iconPreview, iconProperty);
            iconPreview.TrackPropertyValue(iconProperty, changed => UpdateIconPreview(iconPreview, changed));
            _scroll.Add(iconPreview);

            _scroll.Add(new PropertyField(property.FindPropertyRelative("position")));

            AddSection("Price");
            var maxLevel = property.FindPropertyRelative("maxLevel");
            var baseCost = property.FindPropertyRelative("baseCost");
            var costGrowth = property.FindPropertyRelative("costGrowth");
            _scroll.Add(new PropertyField(maxLevel));
            _scroll.Add(new PropertyField(baseCost));
            _scroll.Add(new PropertyField(costGrowth));

            var costPreview = new Label();
            costPreview.style.whiteSpace = WhiteSpace.Normal;
            costPreview.style.color = new Color(0.7f, 0.7f, 0.7f);
            costPreview.style.marginLeft = 3;
            UpdateCostPreview(costPreview, node);
            costPreview.TrackPropertyValue(maxLevel, _ => UpdateCostPreview(costPreview, node));
            costPreview.TrackPropertyValue(baseCost, _ => UpdateCostPreview(costPreview, node));
            costPreview.TrackPropertyValue(costGrowth, _ => UpdateCostPreview(costPreview, node));
            _scroll.Add(costPreview);

            AddSection("Requires");
            _scroll.Add(new PropertyField(property.FindPropertyRelative("requirement")));
            BuildRequirements(node);

            AddSection("Effects");
            BuildEffects(node, property.FindPropertyRelative("effects"));
        }

        private void BuildRequirements(UpgradeNode node)
        {
            if (node.Requires.Count == 0)
            {
                _scroll.Add(new Label("None: available from the start."));
            }

            foreach (var id in node.Requires)
            {
                var parent = _tree.Find(id);
                var row = Row();
                var label = new Label(parent != null ? $"{EditorLocalizedText.Get(parent.Title, id)}  ({id})" : $"{id} (missing)");
                label.style.flexGrow = 1;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                row.Add(label);

                string parentId = id;
                if (parent != null)
                {
                    row.Add(new Button(() => _canvas.Select(parent)) { text = "Select" });
                }

                row.Add(new Button(() =>
                {
                    Undo.RecordObject(_tree, "Remove Upgrade Requirement");
                    node.EditorRequires.Remove(parentId);
                    EditorUtility.SetDirty(_tree);
                    Edited?.Invoke();
                }) { text = "Remove" });
                _scroll.Add(row);
            }

            var add = new Button { text = "Add Requirement…" };
            add.clicked += () =>
            {
                var menu = new GenericMenu();
                foreach (var other in _tree.Nodes)
                {
                    var candidate = other;
                    string name = $"{EditorLocalizedText.Get(other.Title, other.Id)} ({other.Id})";
                    if (node.EditorRequires.Contains(other.Id) || _tree.WouldCreateCycle(other, node))
                    {
                        menu.AddDisabledItem(new GUIContent(name));
                    }
                    else
                    {
                        menu.AddItem(new GUIContent(name), false, () =>
                        {
                            if (_canvas.Connect(candidate, node))
                            {
                                Edited?.Invoke();
                            }
                        });
                    }
                }

                menu.DropDown(add.worldBound);
            };
            _scroll.Add(add);
        }

        private void BuildEffects(UpgradeNode node, SerializedProperty effects)
        {
            for (int i = 0; i < effects.arraySize; i++)
            {
                var element = effects.GetArrayElementAtIndex(i);
                var effect = node.Effects[i];
                int effectIndex = i;

                var box = new VisualElement();
                box.style.marginTop = 4;
                box.style.marginBottom = 4;
                box.style.paddingLeft = 6;
                box.style.paddingRight = 6;
                box.style.paddingTop = 4;
                box.style.paddingBottom = 6;
                box.style.backgroundColor = new Color(0f, 0f, 0f, 0.18f);
                box.style.borderTopLeftRadius = 4;
                box.style.borderTopRightRadius = 4;
                box.style.borderBottomLeftRadius = 4;
                box.style.borderBottomRightRadius = 4;

                var header = Row();
                var title = new Label(effect != null ? UpgradeEffectTypes.GetMenuName(effect.GetType()) : "Missing effect type");
                title.style.flexGrow = 1;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.unityTextAlign = TextAnchor.MiddleLeft;
                header.Add(title);
                header.Add(new Button(() =>
                {
                    Undo.RecordObject(_tree, "Remove Upgrade Effect");
                    node.EditorEffects.RemoveAt(effectIndex);
                    EditorUtility.SetDirty(_tree);
                    Edited?.Invoke();
                }) { text = "Remove" });
                box.Add(header);

                if (effect != null)
                {
                    var end = element.GetEndProperty();
                    var child = element.Copy();
                    bool enterChildren = true;
                    while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
                    {
                        enterChildren = false;
                        box.Add(new PropertyField(child.Copy()));
                    }
                }

                _scroll.Add(box);
            }

            var add = new Button { text = "Add Effect…" };
            add.clicked += () =>
            {
                var menu = new GenericMenu();
                foreach (var type in UpgradeEffectTypes.All)
                {
                    var effectType = type;
                    menu.AddItem(new GUIContent(UpgradeEffectTypes.GetMenuName(type)), false, () =>
                    {
                        Undo.RecordObject(_tree, "Add Upgrade Effect");
                        node.EditorEffects.Add((UpgradeEffect)Activator.CreateInstance(effectType));
                        EditorUtility.SetDirty(_tree);
                        Edited?.Invoke();
                    });
                }

                menu.DropDown(add.worldBound);
            };
            _scroll.Add(add);
        }

        private void BuildMultiple(IReadOnlyList<UpgradeNode> nodes)
        {
            AddHeader($"{nodes.Count} nodes selected");

            var row = Row();
            row.Add(new Button(() => AlignNodes(nodes, true)) { text = "Align in a row" });
            row.Add(new Button(() => AlignNodes(nodes, false)) { text = "Align in a column" });
            _scroll.Add(row);

            var spread = Row();
            spread.Add(new Button(() => Distribute(nodes, true)) { text = "Spread horizontally" });
            spread.Add(new Button(() => Distribute(nodes, false)) { text = "Spread vertically" });
            _scroll.Add(spread);

            _scroll.Add(new Button(_canvas.DeleteSelection) { text = "Delete" });
        }

        private void BuildEdge(UpgradeNode parent, UpgradeNode child)
        {
            AddHeader("Requirement");
            _scroll.Add(new Label($"{EditorLocalizedText.Get(parent.Title, parent.Id)}  →  " +
                                  $"{EditorLocalizedText.Get(child.Title, child.Id)}"));
            _scroll.Add(new Label($"Buying the first opens the second ({child.Requirement} of its requirements)."));
            var row = Row();
            row.Add(new Button(() => _canvas.Select(parent)) { text = "Select requirement" });
            row.Add(new Button(() => _canvas.Select(child)) { text = "Select node" });
            _scroll.Add(row);
            _scroll.Add(new Button(_canvas.DeleteSelection) { text = "Delete requirement" });
        }

        private void BuildOverview()
        {
            AddHeader(_tree.name);
            _scroll.Add(new Label($"{_tree.Nodes.Count} nodes. Positions are menu pixels at zoom 1, y pointing down."));

            _scroll.Add(new HelpBox(
                "Right-click: add nodes (with an effect), child nodes, delete.\n" +
                "Drag a node to move it (snaps to the grid; hold Ctrl for free placement).\n" +
                "Drag from the dot under a node onto another node: buying the first opens the second.\n" +
                "Click a line to select it. Delete removes the selection, Ctrl+D duplicates, F frames.\n" +
                "Middle-drag or Alt-drag pans, the wheel zooms. Drag on empty space to box-select.",
                HelpBoxMessageType.Info));

            AddSection("Problems");
            if (_canvas.Problems.Count == 0)
            {
                _scroll.Add(new Label("None found."));
            }

            foreach (var (node, problem) in _canvas.Problems)
            {
                var row = Row();
                var label = new Label($"{node.Id}: {problem}");
                label.style.flexGrow = 1;
                label.style.whiteSpace = WhiteSpace.Normal;
                row.Add(label);
                row.Add(new Button(() => _canvas.Select(node)) { text = "Select" });
                _scroll.Add(row);
            }

            var baseStats = FindAsset<BaseStats>();
            if (baseStats != null)
            {
                AddSection("Base stats (before upgrades)");
                _scroll.Add(new InspectorElement(baseStats));
            }
        }

        private void RenameNode(UpgradeNode node, string newId, TextField field)
        {
            newId = newId?.Trim();
            if (string.IsNullOrEmpty(newId) || newId == node.Id)
            {
                field.SetValueWithoutNotify(node.Id);
                return;
            }

            if (_tree.Find(newId) != null)
            {
                Debug.LogWarning($"Upgrade id '{newId}' is already used.");
                field.SetValueWithoutNotify(node.Id);
                return;
            }

            Undo.RecordObject(_tree, "Rename Upgrade Node");
            string oldId = node.Id;
            foreach (var other in _tree.EditorNodes)
            {
                for (int i = 0; i < other.EditorRequires.Count; i++)
                {
                    if (other.EditorRequires[i] == oldId)
                    {
                        other.EditorRequires[i] = newId;
                    }
                }
            }

            node.EditorId = newId;
            EditorUtility.SetDirty(_tree);
            Edited?.Invoke();
        }

        private void AlignNodes(IReadOnlyList<UpgradeNode> nodes, bool row)
        {
            Undo.RecordObject(_tree, "Align Upgrade Nodes");
            float sum = 0f;
            foreach (var node in nodes)
            {
                sum += row ? node.Position.y : node.Position.x;
            }

            float value = Mathf.Round(sum / nodes.Count / UpgradeTreeCanvas.GridSize) * UpgradeTreeCanvas.GridSize;
            foreach (var node in nodes)
            {
                node.EditorPosition = row ? new Vector2(node.Position.x, value) : new Vector2(value, node.Position.y);
            }

            EditorUtility.SetDirty(_tree);
            Edited?.Invoke();
        }

        private void Distribute(IReadOnlyList<UpgradeNode> nodes, bool horizontal)
        {
            if (nodes.Count < 3)
            {
                return;
            }

            var sorted = new List<UpgradeNode>(nodes);
            sorted.Sort((a, b) => horizontal ? a.Position.x.CompareTo(b.Position.x) : a.Position.y.CompareTo(b.Position.y));
            float first = horizontal ? sorted[0].Position.x : sorted[0].Position.y;
            float last = horizontal ? sorted[^1].Position.x : sorted[^1].Position.y;

            Undo.RecordObject(_tree, "Spread Upgrade Nodes");
            for (int i = 0; i < sorted.Count; i++)
            {
                float value = Mathf.Lerp(first, last, i / (float)(sorted.Count - 1));
                var position = sorted[i].Position;
                sorted[i].EditorPosition = horizontal ? new Vector2(value, position.y) : new Vector2(position.x, value);
            }

            EditorUtility.SetDirty(_tree);
            Edited?.Invoke();
        }

        private static void UpdateIconPreview(Image image, SerializedProperty iconProperty)
        {
            var sprite = iconProperty.objectReferenceValue as Sprite;
            image.sprite = sprite;
            image.style.display = sprite != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void UpdateCostPreview(Label label, UpgradeNode node)
        {
            var text = new StringBuilder("Level prices: ");
            int shown = Mathf.Min(node.MaxLevel, 10);
            for (int level = 0; level < shown; level++)
            {
                if (level > 0)
                {
                    text.Append(", ");
                }

                text.Append(node.GetCost(level)).Append('$');
            }

            if (node.MaxLevel > shown)
            {
                text.Append(", …");
            }

            label.text = text.ToString();
        }

        private void AddHeader(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 16;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 6;
            label.style.whiteSpace = WhiteSpace.Normal;
            _scroll.Add(label);
        }

        private void AddSection(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 10;
            label.style.marginBottom = 2;
            label.style.borderBottomWidth = 1;
            label.style.borderBottomColor = new Color(1f, 1f, 1f, 0.15f);
            _scroll.Add(label);
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 2;
            return row;
        }

        private static T FindAsset<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }
    }
}
