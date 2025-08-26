using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/UpgradeCollection")]
    public class UpgradesCollection : ScriptableObject
    {
        public List<UpgradeTab<Upgrade>> upgrades = new();
        public readonly Dictionary<TrashType, TrashScoreUpgrade> TrashScoreUpgrades = new();

        public void Load(SaveManager saveManager)
        {
            TrashScoreUpgrades.Clear();

            foreach (var upgrade in upgrades.SelectMany(tab => tab.upgrades))
            {
                if (upgrade is TrashScoreUpgrade trashScoreUpgrade)
                {
                    TrashScoreUpgrades.Add(trashScoreUpgrade.trashType, trashScoreUpgrade);
                }
            }

            foreach (var upgrade in upgrades.SelectMany(category => category.upgrades))
            {
                upgrade.Init(saveManager.Progress.UpgradeLevels.GetValueOrDefault(upgrade.upgradeName, 0));
            }
        }

        public List<T> GetUpgradeType<T>() where T : Upgrade
        {
            List<T> list = new();
            foreach (var upgrade in upgrades.SelectMany(tab => tab.upgrades))
            {
                if (upgrade is T u)
                {
                    list.Add(u);
                }
            }

            return list;
        }

        public void Save(SaveManager saveManager)
        {
            Dictionary<string, int> saveData = new Dictionary<string, int>();

            foreach (var upgrade in upgrades.SelectMany(category => category.upgrades))
            {
                saveData[upgrade.upgradeName] = upgrade.CurrentLevel.Value;
            }

            saveManager.Progress.UpgradeLevels = saveData;
        }

        [Serializable]
        public class UpgradeTab<T> where T : Upgrade
        {
            public string name;
            public List<T> upgrades;
        }
    }
}