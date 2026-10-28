using System;
using System.Collections.Generic;
using System.Linq;
using Eco.Scripts.Upgrades;
using UnityEditor;

namespace Eco.Scripts.EditorTools
{
    /// <summary>Every concrete <see cref="UpgradeEffect"/>, for the editor's "Add Effect" menus.</summary>
    internal static class UpgradeEffectTypes
    {
        private static List<Type> _all;

        public static IReadOnlyList<Type> All => _all ??= TypeCache.GetTypesDerivedFrom<UpgradeEffect>()
            .Where(type => !type.IsAbstract && !type.IsGenericType && type.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(GetMenuName)
            .ToList();

        /// <summary>"MaxWeightEffect" → "Max Weight".</summary>
        public static string GetMenuName(Type type)
        {
            string name = type.Name.EndsWith("Effect") ? type.Name[..^"Effect".Length] : type.Name;
            return ObjectNames.NicifyVariableName(name);
        }
    }
}
