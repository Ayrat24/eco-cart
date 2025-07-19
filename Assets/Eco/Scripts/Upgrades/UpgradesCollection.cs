using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrade/UpgradeCollection")]
public class UpgradesCollection : ScriptableObject
{
    public List<Upgrade> upgrades;
}
