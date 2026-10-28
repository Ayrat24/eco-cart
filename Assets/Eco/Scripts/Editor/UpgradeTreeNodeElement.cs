using System.Text;
using Eco.Scripts.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// A node on the upgrade tree editor canvas, drawn at the same size and place as in the game menu, plus a
    /// summary card below it. The canvas does all picking.
    /// </summary>
    internal class UpgradeTreeNodeElement : VisualElement
    {
        public const float PortRadius = 9f;

        private static readonly Color CircleColor = new(0.95f, 0.91f, 0.81f);
        private static readonly Color BorderColor = new(0.22f, 0.4f, 0.25f);
        private static readonly Color SelectedColor = new(0.25f, 0.6f, 1f);
        private static readonly Color ProblemColor = new(0.9f, 0.25f, 0.25f);

        private readonly VisualElement _circle;
        private readonly VisualElement _icon;
        private readonly VisualElement _port;
        private readonly VisualElement _rootMarker;
        private readonly Label _maxLevel;
        private readonly Label _name;
        private readonly Label _summary;

        public UpgradeNode Node { get; }

        /// <summary>Center of the "connect" handle, in tree coordinates.</summary>
        public Vector2 PortPosition => Node.Position + new Vector2(0f, UpgradeTree.NodeSize / 2f);

        public UpgradeTreeNodeElement(UpgradeNode node)
        {
            Node = node;
            pickingMode = PickingMode.Ignore;
            float size = UpgradeTree.NodeSize;
            style.position = Position.Absolute;
            style.width = size;
            style.height = size;

            _circle = Child(this);
            _circle.style.position = Position.Absolute;
            _circle.style.left = 0;
            _circle.style.top = 0;
            _circle.style.width = size;
            _circle.style.height = size;
            SetRadius(_circle, size / 2f);
            SetBorder(_circle, 4f, BorderColor);
            _circle.style.backgroundColor = CircleColor;
            _circle.style.alignItems = Align.Center;
            _circle.style.justifyContent = Justify.Center;

            _icon = Child(_circle);
            _icon.style.width = Length.Percent(64);
            _icon.style.height = Length.Percent(64);
            _icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);

            _maxLevel = new Label { pickingMode = PickingMode.Ignore };
            _maxLevel.style.position = Position.Absolute;
            _maxLevel.style.top = -6;
            _maxLevel.style.right = -12;
            _maxLevel.style.paddingLeft = 6;
            _maxLevel.style.paddingRight = 6;
            _maxLevel.style.backgroundColor = BorderColor;
            _maxLevel.style.color = CircleColor;
            _maxLevel.style.fontSize = 15;
            _maxLevel.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetRadius(_maxLevel, 8f);
            Add(_maxLevel);

            _rootMarker = Child(this);
            _rootMarker.style.position = Position.Absolute;
            _rootMarker.style.left = -4;
            _rootMarker.style.top = -4;
            _rootMarker.style.width = 18;
            _rootMarker.style.height = 18;
            _rootMarker.style.backgroundColor = new Color(0.65f, 0.79f, 0.34f);
            SetRadius(_rootMarker, 9f);
            SetBorder(_rootMarker, 2f, BorderColor);
            _rootMarker.tooltip = "Available from the start";

            _port = Child(this);
            _port.style.position = Position.Absolute;
            _port.style.left = size / 2f - PortRadius;
            _port.style.top = size - PortRadius;
            _port.style.width = PortRadius * 2f;
            _port.style.height = PortRadius * 2f;
            _port.style.backgroundColor = BorderColor;
            SetRadius(_port, PortRadius);
            SetBorder(_port, 2f, CircleColor);

            var caption = Child(this);
            caption.style.position = Position.Absolute;
            caption.style.top = size + PortRadius + 2f;
            caption.style.left = (size - 220f) / 2f;
            caption.style.width = 220f;
            caption.style.alignItems = Align.Center;

            _name = new Label { pickingMode = PickingMode.Ignore };
            _name.style.fontSize = 16;
            _name.style.unityFontStyleAndWeight = FontStyle.Bold;
            _name.style.whiteSpace = WhiteSpace.Normal;
            _name.style.unityTextAlign = TextAnchor.UpperCenter;
            _name.style.color = Color.white;
            caption.Add(_name);

            _summary = new Label { pickingMode = PickingMode.Ignore };
            _summary.style.fontSize = 12;
            _summary.style.whiteSpace = WhiteSpace.Normal;
            _summary.style.unityTextAlign = TextAnchor.UpperCenter;
            _summary.style.color = new Color(0.8f, 0.8f, 0.8f);
            _summary.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
            _summary.style.paddingLeft = 6;
            _summary.style.paddingRight = 6;
            _summary.style.paddingTop = 2;
            _summary.style.paddingBottom = 2;
            _summary.style.marginTop = 2;
            SetRadius(_summary, 4f);
            caption.Add(_summary);

            Refresh(false, null, false);
        }

        public void Refresh(bool selected, string problem, bool isRoot)
        {
            float size = UpgradeTree.NodeSize;
            style.left = Node.Position.x - size / 2f;
            style.top = Node.Position.y - size / 2f;

            _icon.style.backgroundImage = Node.Icon != null ? new StyleBackground(Node.Icon) : StyleKeyword.None;
            _maxLevel.text = $"max {Node.MaxLevel}";
            _name.text = EditorLocalizedText.Get(Node.Title, Node.Id);
            _rootMarker.style.display = isRoot ? DisplayStyle.Flex : DisplayStyle.None;

            var summary = new StringBuilder();
            foreach (var effect in Node.Effects)
            {
                summary.AppendLine(effect != null ? effect.EditorSummary : "(missing effect type)");
            }

            summary.Append($"{Node.GetCost(0)}$  ×{Node.MaxLevel}");
            if (!string.IsNullOrEmpty(problem))
            {
                summary.Append($"\n<color=#ff7070>{problem}</color>");
            }

            _summary.text = summary.ToString();

            var border = selected ? SelectedColor : string.IsNullOrEmpty(problem) ? BorderColor : ProblemColor;
            SetBorder(_circle, selected ? 6f : 4f, border);
        }

        private static VisualElement Child(VisualElement parent)
        {
            var child = new VisualElement { pickingMode = PickingMode.Ignore };
            parent.Add(child);
            return child;
        }

        private static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        private static void SetBorder(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }
    }
}
