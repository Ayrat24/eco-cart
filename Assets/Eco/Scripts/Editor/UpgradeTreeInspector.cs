using Eco.Scripts.Upgrades;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Eco.Scripts.EditorTools
{
    /// <summary>The Upgrade Tree asset is edited in its own window; the inspector just links there.</summary>
    [CustomEditor(typeof(UpgradeTree))]
    public class UpgradeTreeInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var tree = (UpgradeTree)target;
            var root = new VisualElement();
            root.Add(new HelpBox($"{tree.Nodes.Count} upgrade nodes. Edit them in the Upgrade Tree Editor.",
                HelpBoxMessageType.Info));
            root.Add(new Button(() => UpgradeTreeEditorWindow.Open(tree)) { text = "Open Upgrade Tree Editor" });

            var raw = new Foldout { text = "Raw data", value = false };
            raw.Add(new PropertyField(serializedObject.FindProperty("nodes")));
            root.Add(raw);
            return root;
        }
    }
}
