using System.Collections.Generic;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    /// <summary>
    /// The upgrade tree: nodes, where they sit in the menu and which nodes open up which.
    /// Edit it in <b>Eco → Upgrade Tree Editor</b> (or double-click the asset); node positions there are
    /// the positions in the gameplay menu.
    /// </summary>
    [CreateAssetMenu(menuName = "Upgrade/Upgrade Tree")]
    public class UpgradeTree : ScriptableObject
    {
        /// <summary>Diameter of a node, in menu pixels at zoom 1. Shared by the menu and the editor.</summary>
        public const float NodeSize = 110f;

        [SerializeField] private List<UpgradeNode> nodes = new();

        public IReadOnlyList<UpgradeNode> Nodes => nodes;

        public UpgradeNode Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var node in nodes)
            {
                if (node.Id == id)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>Nodes that list this one as a requirement.</summary>
        public IEnumerable<UpgradeNode> GetChildren(UpgradeNode parent)
        {
            foreach (var node in nodes)
            {
                foreach (var id in node.Requires)
                {
                    if (id == parent.Id)
                    {
                        yield return node;
                        break;
                    }
                }
            }
        }

        public IEnumerable<T> GetEffects<T>() where T : UpgradeEffect
        {
            foreach (var node in nodes)
            {
                foreach (var effect in node.Effects)
                {
                    if (effect is T typed)
                    {
                        yield return typed;
                    }
                }
            }
        }

        /// <summary>Bounds of all node centers, grown by half a node on every side.</summary>
        public Rect GetBounds()
        {
            if (nodes.Count == 0)
            {
                return new Rect(-NodeSize / 2f, -NodeSize / 2f, NodeSize, NodeSize);
            }

            var min = nodes[0].Position;
            var max = nodes[0].Position;
            foreach (var node in nodes)
            {
                min = Vector2.Min(min, node.Position);
                max = Vector2.Max(max, node.Position);
            }

            var half = Vector2.one * (NodeSize / 2f);
            return Rect.MinMaxRect(min.x - half.x, min.y - half.y, max.x + half.x, max.y + half.y);
        }

#if UNITY_EDITOR
        public List<UpgradeNode> EditorNodes => nodes;

        /// <summary>True if making <paramref name="parent"/> a requirement of <paramref name="child"/> would loop.</summary>
        public bool WouldCreateCycle(UpgradeNode parent, UpgradeNode child)
        {
            if (parent == child)
            {
                return true;
            }

            // Walk up from the parent: reaching the child means the child already leads to the parent
            var visited = new HashSet<string>();
            var stack = new Stack<UpgradeNode>();
            stack.Push(parent);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == child)
                {
                    return true;
                }

                if (!visited.Add(current.Id))
                {
                    continue;
                }

                foreach (var id in current.Requires)
                {
                    var next = Find(id);
                    if (next != null)
                    {
                        stack.Push(next);
                    }
                }
            }

            return false;
        }

        public string MakeUniqueId(string prefix)
        {
            for (int i = 1; ; i++)
            {
                string id = $"{prefix}-{i}";
                if (Find(id) == null)
                {
                    return id;
                }
            }
        }
#endif
    }
}
