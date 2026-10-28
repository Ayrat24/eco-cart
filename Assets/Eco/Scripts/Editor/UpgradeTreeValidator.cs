using System.Collections.Generic;
using Eco.Scripts.Upgrades;
using Eco.Scripts.Upgrades.Effects;

namespace Eco.Scripts.EditorTools
{
    /// <summary>Finds mistakes in an upgrade tree that the game would trip over or silently ignore.</summary>
    internal static class UpgradeTreeValidator
    {
        /// <summary>One problem per node at most (the first found), keyed by node.</summary>
        public static Dictionary<UpgradeNode, string> Validate(UpgradeTree tree)
        {
            var problems = new Dictionary<UpgradeNode, string>();
            var seen = new HashSet<string>();

            foreach (var node in tree.Nodes)
            {
                string problem = null;
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    problem = "Empty id";
                }
                else if (!seen.Add(node.Id))
                {
                    problem = $"Duplicate id '{node.Id}'";
                }
                else if (node.Effects.Count == 0)
                {
                    problem = "No effects";
                }
                else if (node.Title.IsEmpty)
                {
                    problem = "No name";
                }

                if (problem == null)
                {
                    foreach (var effect in node.Effects)
                    {
                        problem = effect switch
                        {
                            null => "Effect type is missing (renamed or deleted class?)",
                            UnlockTreeEffect { Tree: null } => "Unlock Tree has no tree",
                            UnlockPetEffect { Pet: null } => "Unlock Pet has no pet",
                            _ => null
                        };

                        if (problem != null)
                        {
                            break;
                        }
                    }
                }

                if (problem == null)
                {
                    foreach (var id in node.Requires)
                    {
                        if (tree.Find(id) == null)
                        {
                            problem = $"Requires missing node '{id}'";
                            break;
                        }
                    }
                }

                if (problem != null)
                {
                    problems[node] = problem;
                }
            }

            return problems;
        }
    }
}
