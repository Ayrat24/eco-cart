using System.Collections.Generic;
using Eco.Scripts.Scrap;
using UnityEngine.Localization;

namespace Eco.Scripts.Upgrades.Effects
{
    /// <summary>Translated scrap type names: keys "scrap-food", "scrap-paper"… in the Upgrades table.</summary>
    public static class ScrapNames
    {
        private static readonly Dictionary<ScrapType, LocalizedString> Names = new();

        public static string Get(ScrapType type)
        {
            if (!Names.TryGetValue(type, out var name))
            {
                name = new LocalizedString("Upgrades", $"scrap-{type.ToString().ToLowerInvariant()}");
                Names[type] = name;
            }

            return name.GetLocalizedString();
        }
    }
}
