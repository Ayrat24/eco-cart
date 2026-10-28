using Eco.Scripts.Upgrades;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;

namespace Eco.Scripts.Helpers
{
    /// <summary>
    /// A pet that follows the jelly and helps. The upgrade tree unlocks it (<see cref="UnlockType"/> in
    /// <see cref="UnlockTracker"/>); <see cref="HelperManager"/> spawns the selected one.
    /// </summary>
    [CreateAssetMenu(fileName = "Pet", menuName = "Eco/Pet Definition")]
    public class PetDefinition : ScriptableObject
    {
        [FormerlySerializedAs("upgradeId")]
        [SerializeField] private string id;
        [FormerlySerializedAs("upgradeLocalizedName")]
        [SerializeField] private LocalizedString localizedName;
        [FormerlySerializedAs("upgradeLocalizedDescription")]
        [SerializeField] private LocalizedString localizedDescription;
        [SerializeField] private Sprite icon;
        [FormerlySerializedAs("upgradeType")]
        [SerializeField] private UnlockableUpgradeType unlockType;
        [SerializeField] private Helper prefab;

        public string Id => id;
        public LocalizedString LocalizedDescription => localizedDescription;
        public Sprite Icon => icon;
        public UnlockableUpgradeType UnlockType => unlockType;
        public Helper Prefab => prefab;
        public bool Unlocked => UnlockTracker.IsUpgradeUnlocked(unlockType);

        public string GetName()
        {
            return localizedName == null || localizedName.IsEmpty ? name : localizedName.GetLocalizedString();
        }
    }
}
