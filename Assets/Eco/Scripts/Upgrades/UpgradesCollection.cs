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
                upgrade.Init(saveManager.Progress.UpgradeLevels.GetValueOrDefault(upgrade.upgradeId, 0));
            }
        }

        public List<T> GetUpgradeTypes<T>() where T : Upgrade
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
        
        public T GetUpgradeType<T>() where T : Upgrade
        {
            foreach (var upgrade in upgrades.SelectMany(tab => tab.upgrades))
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

            foreach (var upgrade in upgrades.SelectMany(category => category.upgrades))
            {
                saveData[upgrade.upgradeId] = upgrade.CurrentLevel.Value;
            }

            saveManager.Progress.UpgradeLevels = saveData;
        }

        public void Clear()
        {
            foreach (var tab in upgrades)
            {
                foreach (var upgrade in tab.upgrades)
                {
                    upgrade.Clear();
                }
            }
        }

        [Serializable]
        public class UpgradeTab<T> where T : Upgrade
        {
            public string name;
            public LocalizedString nameLoc;
            public List<T> upgrades;
        }
    }
}