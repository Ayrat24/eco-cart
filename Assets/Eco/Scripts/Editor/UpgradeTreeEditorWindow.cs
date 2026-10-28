using Eco.Scripts.Upgrades;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// Graph editor for <see cref="UpgradeTree"/>: lay out nodes where they appear in the game menu, connect
    /// requirements, and edit each node's name, icon, price, max level and effects. <b>Eco → Upgrade Tree Editor</b>,
    /// or double-click an Upgrade Tree asset.
    /// </summary>
    public class UpgradeTreeEditorWindow : EditorWindow
    {
        private const string LastTreeKey = "Eco.UpgradeTreeEditor.LastTree";
        private const string SnapKey = "Eco.UpgradeTreeEditor.Snap";

        [SerializeField] private UpgradeTree tree;

        private SerializedObject _serializedTree;
        private UpgradeTreeCanvas _canvas;
        private UpgradeNodeInspector _inspector;
        private ObjectField _treeField;
        private VisualElement _tracker;
        private bool _inspectorDirty;

        [MenuItem("Eco/Upgrade Tree Editor")]
        public static void Open()
        {
            var window = GetWindow<UpgradeTreeEditorWindow>();
            window.titleContent = new GUIContent("Upgrade Tree");
            window.minSize = new Vector2(800, 400);
        }

        public static void Open(UpgradeTree upgradeTree)
        {
            Open();
            GetWindow<UpgradeTreeEditorWindow>().SetTree(upgradeTree);
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(EntityId entityId, int line)
        {
            if (EditorUtility.EntityIdToObject(entityId) is not UpgradeTree upgradeTree)
            {
                return false;
            }

            Open(upgradeTree);
            return true;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            var toolbar = new Toolbar();
            _treeField = new ObjectField { objectType = typeof(UpgradeTree), allowSceneObjects = false };
            _treeField.style.width = 260;
            _treeField.RegisterValueChangedCallback(evt => SetTree(evt.newValue as UpgradeTree));
            toolbar.Add(_treeField);

            toolbar.Add(new ToolbarButton(AddNodeAtCenter) { text = "Add Node" });
            toolbar.Add(new ToolbarButton(() => _canvas.FrameAll()) { text = "Frame" });

            var snap = new ToolbarToggle { text = "Snap", value = EditorPrefs.GetBool(SnapKey, true) };
            snap.RegisterValueChangedCallback(evt =>
            {
                _canvas.Snap = evt.newValue;
                EditorPrefs.SetBool(SnapKey, evt.newValue);
            });
            toolbar.Add(snap);

            toolbar.Add(new ToolbarSpacer { flex = true });
            toolbar.Add(new ToolbarButton(Save) { text = "Save" });
            root.Add(toolbar);

            var body = new VisualElement();
            body.style.flexGrow = 1;
            body.style.flexDirection = FlexDirection.Row;
            root.Add(body);

            _canvas = new UpgradeTreeCanvas { Snap = snap.value };
            _canvas.SelectionChanged += RefreshInspector;
            _canvas.TreeEdited += OnCanvasEdited;
            body.Add(_canvas);

            _inspector = new UpgradeNodeInspector(_canvas);
            _inspector.Edited += OnInspectorEdited;
            body.Add(_inspector);

            // Re-binding happens here, outside the change callbacks that triggered it
            root.schedule.Execute(() =>
            {
                if (_inspectorDirty)
                {
                    _inspectorDirty = false;
                    _inspector.Show(tree, _serializedTree);
                }
            }).Every(50);

            if (tree == null)
            {
                string path = EditorPrefs.GetString(LastTreeKey, string.Empty);
                tree = AssetDatabase.LoadAssetAtPath<UpgradeTree>(path);
                if (tree == null)
                {
                    var guids = AssetDatabase.FindAssets("t:UpgradeTree");
                    if (guids.Length > 0)
                    {
                        tree = AssetDatabase.LoadAssetAtPath<UpgradeTree>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    }
                }
            }

            var current = tree;
            tree = null;
            SetTree(current);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void SetTree(UpgradeTree upgradeTree)
        {
            if (_canvas == null || (upgradeTree == tree && _serializedTree != null))
            {
                return;
            }

            tree = upgradeTree;
            _treeField.SetValueWithoutNotify(upgradeTree);
            _serializedTree = upgradeTree != null ? new SerializedObject(upgradeTree) : null;
            EditorPrefs.SetString(LastTreeKey, upgradeTree != null ? AssetDatabase.GetAssetPath(upgradeTree) : string.Empty);

            // Any change to the asset (inspector fields, undo) redraws the canvas
            _tracker?.RemoveFromHierarchy();
            _tracker = new VisualElement();
            rootVisualElement.Add(_tracker);
            if (_serializedTree != null)
            {
                _tracker.TrackSerializedObjectValue(_serializedTree, _ => _canvas.Refresh());
            }

            _canvas.SetTree(upgradeTree);
            _inspector.Show(upgradeTree, _serializedTree);
        }

        private void AddNodeAtCenter()
        {
            if (tree == null)
            {
                return;
            }

            // Next to the selection, or at the origin
            var position = _canvas.Selection.Count > 0
                ? _canvas.Selection[0].Position + new Vector2(UpgradeTree.NodeSize * 2f, 0f)
                : Vector2.zero;
            _canvas.AddNode(position, null);
        }

        private void OnCanvasEdited()
        {
            _serializedTree?.Update();
            _inspectorDirty = true;
        }

        private void OnInspectorEdited()
        {
            _serializedTree?.Update();
            _canvas.Rebuild();
            _inspectorDirty = true;
        }

        private void RefreshInspector()
        {
            _inspectorDirty = true;
        }

        private void OnUndoRedo()
        {
            if (tree == null || _canvas == null)
            {
                return;
            }

            _serializedTree?.Update();
            _canvas.Rebuild();
            _inspectorDirty = true;
        }

        private void Save()
        {
            if (tree == null)
            {
                return;
            }

            AssetDatabase.SaveAssetIfDirty(tree);
            foreach (var node in tree.Nodes)
            {
                EditorLocalizedText.SaveTables(node.Title);
                EditorLocalizedText.SaveTables(node.Description);
            }
        }
    }
}
