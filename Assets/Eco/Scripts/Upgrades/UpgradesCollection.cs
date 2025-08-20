using System.Collections.Generic;
using Eco.Scripts.Helpers;
using Eco.Scripts.Trees;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/UpgradeCollection")]
    public class UpgradesCollection : ScriptableObject
    {
        public List<TrashScoreUpgrade> trashScoreUpgrades;
        public readonly Dictionary<TrashType, TrashScoreUpgrade> TrashScoreUpgrades = new();


        public List<TreeBuyUpgrade> treeBuyUpgrades;
        public List<HelperBuyUpgrade> helperBuyUpgrades;
        public List<CartBuyUpgrade> cartBuyUpgrades;

        private readonly List<Upgrade> _allUpgrades = new();

        public void Load(SaveManager saveManager)
        {
            TrashScoreUpgrades.Clear();
            _allUpgrades.Clear();
            
            foreach (var upgrade in trashScoreUpgrades)
            {
                TrashScoreUpgrades.Add(upgrade.trashType, upgrade);
                _allUpgrades.Add(upgrade);
            }

            foreach (var helper in helperBuyUpgrades)
            {
                _allUpgrades.Add(helper);
            }

            foreach (var tree in treeBuyUpgrades)
            {
                _allUpgrades.Add(tree);
            }
            
            foreach (var cart in cartBuyUpgrades)
            {
                _allUpgrades.Add(cart);
            }
            
            foreach (var u in _allUpgrades)
            {
                u.Init(saveManager.Progress.UpgradeLevels.GetValueOrDefault(u.upgradeName, 0));
            }
        }

        public void Save(SaveManager saveManager)
        {
            Dictionary<string, int> upgrades = new Dictionary<string, int>();
            foreach (var u in _allUpgrades)
            {
                upgrades[u.upgradeName] = u.CurrentLevel.Value;
            }

            saveManager.Progress.UpgradeLevels = upgrades;
        }
    }
}