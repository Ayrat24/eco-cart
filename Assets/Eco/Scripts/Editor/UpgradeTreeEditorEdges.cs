using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.EditorTools
{
    /// <summary>
    /// Draws the requirement lines of the tree editor (with an arrow toward the node they open) and the line of a
    /// connection being dragged. Lives in the zoomed content, in tree coordinates.
    /// </summary>
    internal class UpgradeTreeEditorEdges : VisualElement
    {
        private const float LineWidth = 4f;
        private const float ArrowSize = 14f;
        // The layer spans this far around the origin so it is never culled; drawing is offset to match
        private const float Extent = 50000f;
        private static readonly Vector2 Origin = new(-Extent, -Extent);

        private static readonly Color LineColor = new(0.65f, 0.79f, 0.34f);
        private static readonly Color SelectedColor = new(1f, 0.6f, 0.2f);
        private static readonly Color PendingColor = new(1f, 1f, 1f, 0.8f);

        private UpgradeTree _tree;

        public (UpgradeNode parent, UpgradeNode child)? Selected { get; set; }

        /// <summary>A connection being dragged: from a node's port to the pointer, in tree coordinates.</summary>
        public (Vector2 from, Vector2 to)? Pending { get; set; }

        public UpgradeTreeEditorEdges()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = Origin.x;
            style.top = Origin.y;
            style.width = Extent * 2f;
            style.height = Extent * 2f;
            generateVisualContent += Draw;
        }

        public void SetTree(UpgradeTree tree)
        {
            _tree = tree;
            MarkDirtyRepaint();
        }

        /// <summary>Requirement lines as (parent, child) pairs.</summary>
        public static IEnumerable<(UpgradeNode parent, UpgradeNode child)> GetEdges(UpgradeTree tree)
        {
            foreach (var child in tree.Nodes)
            {
                foreach (var id in child.Requires)
                {
                    var parent = tree.Find(id);
                    if (parent != null)
                    {
                        yield return (parent, child);
                    }
                }
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            painter.lineWidth = LineWidth;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;

            if (_tree != null)
            {
                foreach (var (parent, child) in GetEdges(_tree))
                {
                    bool selected = Selected.HasValue && Selected.Value.parent == parent && Selected.Value.child == child;
                    DrawArrow(painter, parent.Position, child.Position, selected ? SelectedColor : LineColor);
                }
            }

            if (Pending.HasValue)
            {
                painter.strokeColor = PendingColor;
                painter.BeginPath();
                painter.MoveTo(Pending.Value.from - Origin);
                painter.LineTo(Pending.Value.to - Origin);
                painter.Stroke();
            }
        }

        private static void DrawArrow(Painter2D painter, Vector2 from, Vector2 to, Color color)
        {
            var direction = to - from;
            float length = direction.magnitude;
            if (length < 1f)
            {
                return;
            }

            direction /= length;
            float radius = UpgradeTree.NodeSize / 2f;
            var start = from - Origin + direction * radius;
            var tip = to - Origin - direction * (radius + 4f);
            if (Vector2.Dot(tip - start, direction) <= 0f)
            {
                return;
            }

            painter.strokeColor = color;
            painter.BeginPath();
            painter.MoveTo(start);
            painter.LineTo(tip);
            painter.Stroke();

            var side = new Vector2(-direction.y, direction.x);
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(tip);
            painter.LineTo(tip - direction * ArrowSize + side * (ArrowSize * 0.6f));
            painter.LineTo(tip - direction * ArrowSize - side * (ArrowSize * 0.6f));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
