using Eco.Scripts.Upgrades;
using LargeNumbers;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;

namespace Eco.Scripts.Trees
{
    /// <summary>
    /// A plantable tree type. The upgrade tree unlocks it (<see cref="UnlockType"/> in <see cref="UnlockTracker"/>);
    /// planting goes through <see cref="World.TreePlanter.PlantTree(int)"/> with <see cref="PrefabId"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Eco/Tree Definition")]
    public class TreeDefinition : ScriptableObject
    {
        [FormerlySerializedAs("upgradeId")]
        [SerializeField] private string id;
        [FormerlySerializedAs("upgradeLocalizedName")]
        [SerializeField] private LocalizedString localizedName;
        [SerializeField] private Sprite icon;
        [FormerlySerializedAs("upgradeType")]
        [SerializeField] private UnlockableUpgradeType unlockType;
        [SerializeField] private int prefabId;
        [FormerlySerializedAs("baseCost")]
        [Tooltip("Price of planting one, for the planting menu.")]
        [SerializeField] private AlphabeticNotation price;

        public string Id => id;
        public Sprite Icon => icon;
        public UnlockableUpgradeType UnlockType => unlockType;
        public int PrefabId => prefabId;
        public AlphabeticNotation Price => price;
        public bool Unlocked => UnlockTracker.IsUpgradeUnlocked(unlockType);

        public string GetName()
        {
            return localizedName == null || localizedName.IsEmpty ? name : localizedName.GetLocalizedString();
        }
    }
}
