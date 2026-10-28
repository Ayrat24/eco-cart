using System.Collections.Generic;
using LargeNumbers;
using R3;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    /// <summary>
    /// Owns the bought level of every <see cref="UpgradeTree"/> node: what can be bought, buying, refunding and
    /// saving. After each change it rebuilds <see cref="PlayerStats"/> from scratch.
    /// Levels are saved in <see cref="SaveManager.PlayerProgress.UpgradeLevels"/> keyed by node id.
    /// </summary>
    public class UpgradeManager
    {
        private readonly UpgradeTree _tree;
        private readonly PlayerStats _stats;
        private readonly CurrencyManager _currencyManager;
        private readonly Dictionary<string, int> _levels = new();

        public readonly Subject<UpgradeNode> OnUpgradeBought = new();
        public readonly Subject<UpgradeNode> OnUpgradeRefunded = new();

        public UpgradeTree Tree => _tree;

        public UpgradeManager(UpgradeTree tree, PlayerStats stats, CurrencyManager currencyManager)
        {
            _tree = tree;
            _stats = stats;
            _currencyManager = currencyManager;
        }

        public void Load(SaveManager saveManager)
        {
            _levels.Clear();
            var saved = saveManager.Progress.UpgradeLevels;

            // Keys of removed nodes (and of the old upgrade system) are dropped here and on the next save
            foreach (var node in _tree.Nodes)
            {
                if (saved != null && saved.TryGetValue(node.Id, out int level) && level > 0)
                {
                    _levels[node.Id] = Mathf.Min(level, node.MaxLevel);
                }
            }

            RecalculateStats(false);
        }

        public void Save(SaveManager saveManager)
        {
            saveManager.Progress.UpgradeLevels = new Dictionary<string, int>(_levels);
        }

        public int GetLevel(UpgradeNode node)
        {
            return _levels.GetValueOrDefault(node.Id, 0);
        }

        public bool IsMaxed(UpgradeNode node)
        {
            return GetLevel(node) >= node.MaxLevel;
        }

        public bool AreRequirementsMet(UpgradeNode node)
        {
            bool anyKnown = false;
            bool anyBought = false;
            bool allBought = true;

            foreach (var id in node.Requires)
            {
                var parent = _tree.Find(id);
                if (parent == null)
                {
                    continue;
                }

                anyKnown = true;
                bool bought = GetLevel(parent) > 0;
                anyBought |= bought;
                allBought &= bought;
            }

            if (!anyKnown)
            {
                return true;
            }

            return node.Requirement == UpgradeRequirement.All ? allBought : anyBought;
        }

        public UpgradeNodeState GetState(UpgradeNode node)
        {
            int level = GetLevel(node);
            if (level >= node.MaxLevel)
            {
                return UpgradeNodeState.Maxed;
            }

            if (level > 0)
            {
                return UpgradeNodeState.Owned;
            }

            return AreRequirementsMet(node) ? UpgradeNodeState.Available : UpgradeNodeState.Locked;
        }

        public AlphabeticNotation GetCost(UpgradeNode node)
        {
            return node.GetCost(GetLevel(node));
        }

        public bool CanAfford(UpgradeNode node)
        {
            return Settings.FreeUpgrades || _currencyManager.CurrentMoney.Value >= GetCost(node);
        }

        public bool CanBuy(UpgradeNode node)
        {
            var state = GetState(node);
            return (state == UpgradeNodeState.Available || state == UpgradeNodeState.Owned) && CanAfford(node);
        }

        public bool TryBuy(UpgradeNode node)
        {
            if (!CanBuy(node))
            {
                return false;
            }

            if (Settings.FreeUpgrades)
            {
                Debug.Log($"[DEBUG] Free upgrade purchased: {node.Id}");
            }
            else
            {
                _currencyManager.RemoveMoney(GetCost(node));
            }

            _levels[node.Id] = GetLevel(node) + 1;
            RecalculateStats(true);
            OnUpgradeBought.OnNext(node);
            return true;
        }

        /// <summary>Money back for the last bought level: what it cost.</summary>
        public AlphabeticNotation GetRefund(UpgradeNode node)
        {
            return node.GetCost(Mathf.Max(0, GetLevel(node) - 1));
        }

        /// <summary>
        /// A level can be refunded unless that would take away the last requirement of another bought upgrade;
        /// those have to be refunded first.
        /// </summary>
        public bool CanRefund(UpgradeNode node)
        {
            int level = GetLevel(node);
            if (level == 0)
            {
                return false;
            }

            if (level > 1)
            {
                return true;
            }

            _levels.Remove(node.Id);
            bool blocksOthers = false;
            foreach (var other in _tree.Nodes)
            {
                if (other != node && GetLevel(other) > 0 && !AreRequirementsMet(other))
                {
                    blocksOthers = true;
                    break;
                }
            }

            _levels[node.Id] = level;
            return !blocksOthers;
        }

        public bool TryRefund(UpgradeNode node)
        {
            if (!CanRefund(node))
            {
                return false;
            }

            // Free upgrades cost nothing, so they pay nothing back
            if (!Settings.FreeUpgrades)
            {
                _currencyManager.AddMoney(GetRefund(node));
            }

            int level = GetLevel(node) - 1;
            if (level > 0)
            {
                _levels[node.Id] = level;
            }
            else
            {
                _levels.Remove(node.Id);
            }

            RecalculateStats(true);
            OnUpgradeRefunded.OnNext(node);
            return true;
        }

        /// <summary>Stats with every node at its bought level, or <paramref name="preview"/> one level higher.</summary>
        public StatSheet BuildStats(UpgradeNode preview = null)
        {
            var sheet = _stats.CreateBaseSheet();
            foreach (var node in _tree.Nodes)
            {
                int level = GetLevel(node);
                if (node == preview)
                {
                    level++;
                }

                node.Apply(sheet, level);
            }

            return sheet;
        }

        private void RecalculateStats(bool announceUnlocks)
        {
            _stats.Apply(BuildStats(), announceUnlocks);
        }
    }
}
