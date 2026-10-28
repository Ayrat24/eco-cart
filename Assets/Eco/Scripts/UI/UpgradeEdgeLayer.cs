using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    /// <summary>Draws the lines between upgrade nodes, under the nodes, in tree coordinates.</summary>
    public class UpgradeEdgeLayer : VisualElement
    {
        public enum EdgeState
        {
            /// <summary>The requirement isn't bought.</summary>
            Locked,
            /// <summary>The requirement is bought; the node it opens isn't.</summary>
            Open,
            /// <summary>Both ends are bought.</summary>
            Owned
        }

        private const float LineWidth = 8f;

        // Palette from BackgroundColors.uss: dark green, light green, faded dark green
        private static readonly Color OwnedColor = new(56 / 255f, 102 / 255f, 65 / 255f);
        private static readonly Color OpenColor = new(167 / 255f, 201 / 255f, 87 / 255f);
        private static readonly Color LockedColor = new(56 / 255f, 102 / 255f, 65 / 255f, 0.25f);

        private readonly List<(Vector2 from, Vector2 to, EdgeState state)> _edges = new();
        private Vector2 _origin;

        public UpgradeEdgeLayer()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            generateVisualContent += Draw;
        }

        /// <summary>The layer covers <paramref name="bounds"/> so it isn't culled while lines are on screen.</summary>
        public void SetBounds(Rect bounds)
        {
            _origin = bounds.position;
            style.left = bounds.x;
            style.top = bounds.y;
            style.width = bounds.width;
            style.height = bounds.height;
        }

        public void ClearEdges()
        {
            _edges.Clear();
        }

        public void AddEdge(Vector2 from, Vector2 to, EdgeState state)
        {
            _edges.Add((from, to, state));
        }

        public void Repaint()
        {
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            painter.lineWidth = LineWidth;
            painter.lineCap = LineCap.Round;

            // Locked lines first so lit ones are drawn over them where they cross
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (var (from, to, state) in _edges)
                {
                    if ((int)state != pass)
                    {
                        continue;
                    }

                    painter.strokeColor = state switch
                    {
                        EdgeState.Owned => OwnedColor,
                        EdgeState.Open => OpenColor,
                        _ => LockedColor
                    };
                    painter.BeginPath();
                    painter.MoveTo(from - _origin);
                    painter.LineTo(to - _origin);
                    painter.Stroke();
                }
            }
        }
    }
}
