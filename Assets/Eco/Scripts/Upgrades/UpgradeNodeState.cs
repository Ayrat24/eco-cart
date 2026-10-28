namespace Eco.Scripts.Upgrades
{
    public enum UpgradeNodeState
    {
        /// <summary>Its requirements aren't bought yet.</summary>
        Locked,
        /// <summary>Can be bought, has no level yet.</summary>
        Available,
        /// <summary>Has at least one level and can go higher.</summary>
        Owned,
        /// <summary>At max level.</summary>
        Maxed
    }
}
