using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;

namespace Eco.Scripts.Hats
{
    /// <summary>A hat the jelly can wear. Chosen in the equipment menu; <see cref="HatController"/> puts it on.</summary>
    [CreateAssetMenu(menuName = "Eco/Hat Definition")]
    public class HatDefinition : ScriptableObject
    {
        [FormerlySerializedAs("upgradeId")]
        [SerializeField] private string id;
        [FormerlySerializedAs("upgradeLocalizedName")]
        [SerializeField] private LocalizedString localizedName;
        [SerializeField] private Sprite icon;
        [SerializeField] private HatData hatData;

        public string Id => id;
        public Sprite Icon => icon;
        public HatData Data => hatData;

        public string GetName()
        {
            return localizedName == null || localizedName.IsEmpty ? name : localizedName.GetLocalizedString();
        }

        [Serializable]
        public class HatData
        {
            public Hat prefab;
            public Vector3 positionOffset;
        }
    }
}
