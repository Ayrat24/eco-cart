using System.Collections.Generic;
using UnityEngine;
using VContainer;

[CreateAssetMenu(menuName = "Upgrade/TrashStats")]
public class TrashStats : ScriptableObject
{
    [SerializeField] private List<TrashScoreUpgrade> upgrades;
    public Dictionary<TrashType, TrashScoreUpgrade> Upgrades = new();

    [Inject]
    public void Initialize()
    {
        foreach (var upgrade in upgrades)
        {
            upgrade.Init();
            Upgrades.Add(upgrade.trashType, upgrade);
        }
    }
}