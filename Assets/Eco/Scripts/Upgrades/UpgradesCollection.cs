using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrade/UpgradeCollection")]
    public class UpgradesCollection : ScriptableObject
    {
        public List<UpgradeTab> upgrades = new();
        public readonly Dictionary<TrashType, TrashScoreUpgrade> TrashScoreUpgrades = new();

        public void Load(SaveManager saveManager)
        {
            TrashScoreUpgrades.Clear();

            foreach (var upgradeGroup in upgrades.SelectMany(tab => tab.upgradeGroups))
            {
                foreach (var upgrade in upgradeGroup.upgrades)
                {
                    if (upgrade is TrashScoreUpgrade trashScoreUpgrade)
                    {
                        TrashScoreUpgrades.Add(trashScoreUpgrade.trashType, trashScoreUpgrade);
                    }
                }
            }

            foreach (var upgradeGroup in upgrades.SelectMany(category => category.upgradeGroups))
            {
                foreach (var upgrade in upgradeGroup.upgrades)
                {
                    upgrade.Init(saveManager.Progress.UpgradeLevels.GetValueOrDefault(upgrade.upgradeId, 0));
                }
            }
        }

        public List<T> GetUpgradeTypes<T>() where T : Upgrade
        {
            List<T> list = new();
            foreach (var upgradeGroup in upgrades.SelectMany(tab => tab.upgradeGroups))
            {
                foreach (var upgrade in upgradeGroup.upgrades)
                {
                    if (upgrade is T u)
                    {
                        list.Add(u);
                    }
                }
            }

            return list;
        }

        public T GetUpgradeType<T>() where T : Upgrade
        {
            foreach (var upgrade in upgrades.SelectMany(tab => tab.upgradeGroups))
            {
                if (upgrade is T u)
                {
                    return u;
                }
            }

            return null;
        }

        public void Save(SaveManager saveManager)
        {
            Dictionary<string, int> saveData = new Dictionary<string, int>();

            foreach (var upgradeGroup in upgrades.SelectMany(category => category.upgradeGroups))
            {
                foreach (var upgrade in upgradeGroup.upgrades)
                {
                    saveData[upgrade.upgradeId] = upgrade.CurrentLevel.Value;
                }
            }

            saveManager.Progress.UpgradeLevels = saveData;
        }

        public void Clear()
        {
            foreach (var tab in upgrades)
            {
                foreach (var upgradeGroup in tab.upgradeGroups)
                {
                    foreach (var upgrade in upgradeGroup.upgrades)
                    {
                        upgrade.Clear();
                    }
                }
            }
        }

        [Serializable]
        public class UpgradeTab
        {
            public string name;
            public LocalizedString nameLoc;
            public List<UpgradeGroup> upgradeGroups;
        }

        [Serializable]
        public class UpgradeGroup
        {
            public List<Upgrade> upgrades;
        }
    }
}