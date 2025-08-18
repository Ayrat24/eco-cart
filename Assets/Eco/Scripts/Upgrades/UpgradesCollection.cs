using System.Collections.Generic;
using Eco.Scripts.Helpers;
using Eco.Scripts.Trees;
using Eco.Scripts.Upgrades;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

[CreateAssetMenu(menuName = "Upgrade/UpgradeCollection")]
public class UpgradesCollection : ScriptableObject
{
    public List<TrashScoreUpgrade> trashScoreUpgrades;
    public Dictionary<TrashType, TrashScoreUpgrade> TrashScoreUpgrades = new();

    
    public List<TreeBuyUpgrade> treeBuyUpgrades;
    public List<HelperBuyUpgrade> helperBuyUpgrades;
    
    [Inject]
    public void Initialize()
    {
        foreach (var upgrade in trashScoreUpgrades)
        {
            upgrade.Init();
            TrashScoreUpgrades.Add(upgrade.trashType, upgrade);
        }
    }
}
