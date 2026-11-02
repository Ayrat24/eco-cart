using Eco.Scripts.Scrap;
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

        [SerializeField, Min(0.1f)] private float placementRadius = 1.5f;

        [SerializeField] private TreeGrowthSettings growthSettings;

        [Header("Fruit")]
        [Tooltip("What this tree bears once the upgrade tree unlocks it. Its prefab needs a FruitBearer with spots.")]
        [SerializeField] private ScrapItem fruit;
        [Tooltip("Seconds to grow one fruit, before upgrades.")]
        [SerializeField, Min(1f)] private float fruitInterval = 30f;
        [Tooltip("Most fruit one tree holds; the prefab's fruit spots are a limit as well.")]
        [SerializeField, Min(1)] private int maxFruits = 4;
        [Tooltip("Seconds a new fruit takes to swell to full size. Tentacles can't take it until then.")]
        [SerializeField, Min(0f)] private float ripenDuration = 1.5f;

        public TreeGrowthSettings GrowthSettings => growthSettings;
        public ScrapItem Fruit => fruit;
        public float FruitInterval => fruitInterval;
        public int MaxFruits => maxFruits;
        public float RipenDuration => ripenDuration;

        public float PlacementRadius => Mathf.Max(0.1f, placementRadius);
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
