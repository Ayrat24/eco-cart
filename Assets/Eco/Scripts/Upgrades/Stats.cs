using System.Collections.Generic;
using R3;

namespace Eco.Scripts.Upgrades
{
    public static class Stats
    {
        public static readonly Dictionary<UnlockableUpgradeType, bool> UnlockableUpgrades = new();
        public static readonly Subject<UnlockableUpgradeType> OnUnlocked = new();
        
        public static bool IsUpgradeUnlocked(UnlockableUpgradeType upgrade)
        {
            return UnlockableUpgrades.GetValueOrDefault(upgrade, false);
        }

        public static void AddUnlockableUpgrade(UnlockableUpgradeType upgrade, bool unlocked)
        {
            UnlockableUpgrades.Add(upgrade, unlocked);
        }

        public static void UnlockUpgrade(UnlockableUpgradeType upgrade)
        {
            UnlockableUpgrades[upgrade] = true;
            OnUnlocked.OnNext(upgrade);
        }
    }

    public enum UnlockableUpgradeType
    {
        None,
        Map,
    }
}
